#!/usr/bin/env python3
"""程序化唐构资产管线（Blender 4.x, headless）。

用法:
    # 渲染预览（大殿三机位）
    blender -b -P tools/blender/tang_hall.py -- --render artifacts/blender-preview
    # 导出 Unity 资产（五变体 FBX，不烘焙——占位/调试用）
    blender -b -P tools/blender/tang_hall.py -- --export LingyanUnity/Assets/Resources/Models
    # 写实管线：图集 UV + 烘 albedo×AO/smoothness + FBX（发行路径）
    blender -b -P tools/blender/tang_hall.py -- --bake LingyanUnity/Assets/Resources/Models

与 LingyanUnity/Assets/Scripts/Core/Architecture/TangArchitectureSpec.cs 同步的红线：
    举高/进深 = 1/6（举折凹曲，檐缓脊陡）；出檐/柱高 = 0.55；铺作层高/柱高 = 0.5；
    鸱尾：卷尾鳍形、有肋、无兽头（早唐至盛唐形制）。
形制参照：佛光寺东大殿、南禅寺大殿。
"""
import math
import os
import sys

import bpy
import bmesh
from mathutils import Euler, Vector

# ---------------- 红线常数（与 C# 同步，改必两边一起） ----------------
RISE_PER_DEPTH = 1.0 / 6.0
EAVE_PER_COL = 0.55
BRACKET_PER_COL = 0.50

# ---------------- 当前构件尺寸（set_dims 重算派生量） ----------------
W = D = CH = PLINTH_H = GABLE_OVERHANG = 0.0
EAVE = BRACKET_H = RISE = BASE_TOP = COL_TOP = EAVE_Y = HALF_SPAN = ROOF_W = 0.0


def set_dims(w, d, ch, plinth=0.55, gable=1.0):
    global W, D, CH, PLINTH_H, GABLE_OVERHANG
    global EAVE, BRACKET_H, RISE, BASE_TOP, COL_TOP, EAVE_Y, HALF_SPAN, ROOF_W
    W, D, CH, PLINTH_H, GABLE_OVERHANG = w, d, ch, plinth, gable
    EAVE = CH * EAVE_PER_COL
    BRACKET_H = CH * BRACKET_PER_COL
    RISE = D * RISE_PER_DEPTH
    BASE_TOP = PLINTH_H
    COL_TOP = BASE_TOP + CH
    EAVE_Y = COL_TOP + BRACKET_H
    HALF_SPAN = D / 2.0 + EAVE
    ROOF_W = W + GABLE_OVERHANG * 2.0


set_dims(9.0, 6.0, 3.6)

PROFILE_STEPS = 9

# ---------------- 材质（程序化 PBR：色层/粗糙度/凹凸全节点，可烘焙） ----------------
_mats = {}


def _base(name):
    """新建节点材质，返回 (材质, nodes, links, BSDF, Object 坐标输出)。"""
    m = bpy.data.materials.new(name)
    m.use_nodes = True
    nodes = m.node_tree.nodes
    links = m.node_tree.links
    bsdf = nodes["Principled BSDF"]
    coord = nodes.new("ShaderNodeTexCoord")
    return m, nodes, links, bsdf, coord.outputs["Object"]


def _noise(nodes, links, vec, scale, detail=2.0, rough=0.5, distortion=0.0):
    n = nodes.new("ShaderNodeTexNoise")
    n.inputs["Scale"].default_value = scale
    n.inputs["Detail"].default_value = detail
    n.inputs["Roughness"].default_value = rough
    n.inputs["Distortion"].default_value = distortion
    links.new(vec, n.inputs["Vector"])
    return n


def _mix_color(nodes, links, fac, a, b):
    mix = nodes.new("ShaderNodeMix")
    mix.data_type = "RGBA"
    links.new(fac, mix.inputs["Factor"])
    if hasattr(a, "default_value") or isinstance(a, tuple):
        mix.inputs[6].default_value = a if isinstance(a, tuple) else a.default_value
    else:
        links.new(a, mix.inputs[6])
    if isinstance(b, tuple):
        mix.inputs[7].default_value = b
    else:
        links.new(b, mix.inputs[7])
    return mix


def _ramp(nodes, links, fac, pos0, col0, pos1, col1):
    ramp = nodes.new("ShaderNodeValToRGB")
    ramp.color_ramp.elements[0].position = pos0
    ramp.color_ramp.elements[0].color = col0
    ramp.color_ramp.elements[1].position = pos1
    ramp.color_ramp.elements[1].color = col1
    links.new(fac, ramp.inputs["Fac"])
    return ramp


def _rough_noise(nodes, links, bsdf, vec, base, spread, scale=24.0):
    """粗糙度 = base ± spread×噪声。"""
    n = _noise(nodes, links, vec, scale, detail=3.0)
    ramp = _ramp(nodes, links, n.outputs["Fac"],
                 0.0, (base - spread,) * 3 + (1,),
                 1.0, (base + spread,) * 3 + (1,))
    links.new(ramp.outputs["Color"], bsdf.inputs["Roughness"])


def _bump(nodes, links, bsdf, height_out, strength):
    bump = nodes.new("ShaderNodeBump")
    bump.inputs["Strength"].default_value = strength
    links.new(height_out, bump.inputs["Height"])
    links.new(bump.outputs["Normal"], bsdf.inputs["Normal"])


def _per_object_vary(nodes, links, color_out, amount):
    """ObjectInfo.Random 每件微调明暗（瓦垄/构件不千篇一律）。"""
    info = nodes.new("ShaderNodeObjectInfo")
    ramp = _ramp(nodes, links, info.outputs["Random"],
                 0.0, (1 - amount,) * 3 + (1,), 1.0, (1 + amount,) * 3 + (1,))
    mul = nodes.new("ShaderNodeMix")
    mul.data_type = "RGBA"
    mul.blend_type = "MULTIPLY"
    mul.inputs["Factor"].default_value = 1.0
    links.new(color_out, mul.inputs[6])
    links.new(ramp.outputs["Color"], mul.inputs[7])
    return mul


def _cached(name, build):
    if name in _mats:
        return _mats[name]
    m = build()
    _mats[name] = m
    return m


def m_timber():
    """土朱漆木：漆色明暗云斑 + 细木纹 + 弱凹凸。"""
    def build():
        m, nodes, links, bsdf, vec = _base("timber")
        cloud = _noise(nodes, links, vec, 1.6, detail=3.0)
        base = _ramp(nodes, links, cloud.outputs["Fac"],
                     0.25, (0.235, 0.072, 0.042, 1), 0.75, (0.330, 0.115, 0.062, 1))
        grain = _noise(nodes, links, vec, 34.0, detail=6.0, rough=0.7, distortion=0.4)
        mix = _mix_color(nodes, links, grain.outputs["Fac"],
                         base.outputs["Color"], (0.190, 0.060, 0.036, 1))
        mix.inputs["Factor"].default_value = 0.0
        links.new(grain.outputs["Fac"], mix.inputs["Factor"])
        mix2 = nodes.new("ShaderNodeMix")
        mix2.data_type = "RGBA"
        mix2.inputs["Factor"].default_value = 0.16
        links.new(base.outputs["Color"], mix2.inputs[6])
        links.new(mix.outputs[2], mix2.inputs[7])
        vary = _per_object_vary(nodes, links, mix2.outputs[2], 0.05)
        links.new(vary.outputs[2], bsdf.inputs["Base Color"])
        _rough_noise(nodes, links, bsdf, vec, 0.68, 0.10, scale=30.0)
        _bump(nodes, links, bsdf, grain.outputs["Fac"], 0.04)
        return m
    return _cached("timber", build)


def m_timber_dark():
    def build():
        m, nodes, links, bsdf, vec = _base("timber_dark")
        cloud = _noise(nodes, links, vec, 2.0, detail=3.0)
        base = _ramp(nodes, links, cloud.outputs["Fac"],
                     0.2, (0.165, 0.060, 0.038, 1), 0.8, (0.245, 0.092, 0.055, 1))
        vary = _per_object_vary(nodes, links, base.outputs["Color"], 0.06)
        links.new(vary.outputs[2], bsdf.inputs["Base Color"])
        _rough_noise(nodes, links, bsdf, vec, 0.72, 0.08)
        return m
    return _cached("timber_dark", build)


def m_wall():
    """灰泥墙：暖白 + 大块污渍 + 底部返潮 + 抹灰颗粒。"""
    def build():
        m, nodes, links, bsdf, vec = _base("wall")
        stain = _noise(nodes, links, vec, 0.9, detail=4.0, rough=0.65)
        base = _ramp(nodes, links, stain.outputs["Fac"],
                     0.2, (0.700, 0.652, 0.552, 1), 0.8, (0.800, 0.760, 0.668, 1))
        sep = nodes.new("ShaderNodeSeparateXYZ")
        links.new(vec, sep.inputs["Vector"])
        damp = _ramp(nodes, links, sep.outputs["Z"],
                     0.0, (0.62, 0.565, 0.46, 1), 0.75, (1, 1, 1, 1))
        mul = nodes.new("ShaderNodeMix")
        mul.data_type = "RGBA"
        mul.blend_type = "MULTIPLY"
        mul.inputs["Factor"].default_value = 0.55
        links.new(base.outputs["Color"], mul.inputs[6])
        links.new(damp.outputs["Color"], mul.inputs[7])
        links.new(mul.outputs[2], bsdf.inputs["Base Color"])
        grain = _noise(nodes, links, vec, 160.0, detail=2.0)
        _rough_noise(nodes, links, bsdf, vec, 0.93, 0.05, scale=90.0)
        _bump(nodes, links, bsdf, grain.outputs["Fac"], 0.025)
        return m
    return _cached("wall", build)


def m_tile():
    """筒瓦青灰：每垄随机深浅 + 风化白霜 + 湿釉般的低粗糙。"""
    def build():
        m, nodes, links, bsdf, vec = _base("tile")
        cloud = _noise(nodes, links, vec, 3.0, detail=3.0)
        base = _ramp(nodes, links, cloud.outputs["Fac"],
                     0.2, (0.118, 0.126, 0.148, 1), 0.8, (0.185, 0.196, 0.220, 1))
        frost = _noise(nodes, links, vec, 22.0, detail=5.0, rough=0.7)
        frost_mask = _ramp(nodes, links, frost.outputs["Fac"],
                           0.78, (0, 0, 0, 1), 0.95, (1, 1, 1, 1))
        mix = nodes.new("ShaderNodeMix")
        mix.data_type = "RGBA"
        links.new(frost_mask.outputs["Color"], mix.inputs["Factor"])
        links.new(base.outputs["Color"], mix.inputs[6])
        mix.inputs[7].default_value = (0.32, 0.335, 0.33, 1)
        vary = _per_object_vary(nodes, links, mix.outputs[2], 0.12)
        links.new(vary.outputs[2], bsdf.inputs["Base Color"])
        _rough_noise(nodes, links, bsdf, vec, 0.58, 0.12, scale=18.0)
        _bump(nodes, links, bsdf, frost.outputs["Fac"], 0.03)
        return m
    return _cached("tile", build)


def m_ridge():
    """脊饰/鸱尾：近黑陶，微光泽，风化色差。"""
    def build():
        m, nodes, links, bsdf, vec = _base("ridge")
        cloud = _noise(nodes, links, vec, 5.0, detail=3.0)
        base = _ramp(nodes, links, cloud.outputs["Fac"],
                     0.2, (0.052, 0.056, 0.066, 1), 0.8, (0.095, 0.100, 0.112, 1))
        links.new(base.outputs["Color"], bsdf.inputs["Base Color"])
        _rough_noise(nodes, links, bsdf, vec, 0.66, 0.10, scale=12.0)
        return m
    return _cached("ridge", build)


def m_stone():
    """石灰岩台基/柱础：Voronoi 石斑 + 色噪 + 颗粒凹凸。"""
    def build():
        m, nodes, links, bsdf, vec = _base("stone")
        # 条石接缝：低频 Voronoi 细黑缝，不做碎拼
        voro = nodes.new("ShaderNodeTexVoronoi")
        voro.feature = "DISTANCE_TO_EDGE"
        voro.inputs["Scale"].default_value = 1.6
        links.new(vec, voro.inputs["Vector"])
        crack = _ramp(nodes, links, voro.outputs["Distance"],
                      0.0, (0.62, 0.60, 0.56, 1), 0.035, (1, 1, 1, 1))
        cloud = _noise(nodes, links, vec, 2.4, detail=4.0)
        tone = _ramp(nodes, links, cloud.outputs["Fac"],
                     0.2, (0.455, 0.438, 0.400, 1), 0.8, (0.525, 0.508, 0.468, 1))
        mul = nodes.new("ShaderNodeMix")
        mul.data_type = "RGBA"
        mul.blend_type = "MULTIPLY"
        mul.inputs["Factor"].default_value = 1.0
        links.new(tone.outputs["Color"], mul.inputs[6])
        links.new(crack.outputs["Color"], mul.inputs[7])
        links.new(mul.outputs[2], bsdf.inputs["Base Color"])
        grain = _noise(nodes, links, vec, 90.0, detail=3.0)
        _rough_noise(nodes, links, bsdf, vec, 0.86, 0.06, scale=60.0)
        _bump(nodes, links, bsdf, grain.outputs["Fac"], 0.04)
        return m
    return _cached("stone", build)


def m_door():
    """板门：竖向拼板木纹 + 深棕漆。"""
    def build():
        m, nodes, links, bsdf, vec = _base("door")
        wave = nodes.new("ShaderNodeTexWave")
        wave.wave_type = "BANDS"
        wave.bands_direction = "X"
        wave.inputs["Scale"].default_value = 5.5
        wave.inputs["Distortion"].default_value = 3.5
        wave.inputs["Detail"].default_value = 6.0
        links.new(vec, wave.inputs["Vector"])
        base = _ramp(nodes, links, wave.outputs["Fac"],
                     0.15, (0.165, 0.112, 0.072, 1), 0.85, (0.245, 0.170, 0.110, 1))
        vary = _per_object_vary(nodes, links, base.outputs["Color"], 0.05)
        links.new(vary.outputs[2], bsdf.inputs["Base Color"])
        _rough_noise(nodes, links, bsdf, vec, 0.60, 0.10, scale=26.0)
        _bump(nodes, links, bsdf, wave.outputs["Fac"], 0.05)
        return m
    return _cached("door", build)


def m_earth():
    """夯土墩：土黄 + 水平夯层 + 风化噪。"""
    def build():
        m, nodes, links, bsdf, vec = _base("earth")
        wave = nodes.new("ShaderNodeTexWave")
        wave.wave_type = "BANDS"
        wave.bands_direction = "Z"
        wave.inputs["Scale"].default_value = 6.0
        wave.inputs["Distortion"].default_value = 1.6
        links.new(vec, wave.inputs["Vector"])
        layer = _ramp(nodes, links, wave.outputs["Fac"],
                      0.2, (0.500, 0.408, 0.285, 1), 0.8, (0.585, 0.492, 0.360, 1))
        cloud = _noise(nodes, links, vec, 8.0, detail=4.0)
        mix = nodes.new("ShaderNodeMix")
        mix.data_type = "RGBA"
        mix.inputs["Factor"].default_value = 0.35
        links.new(layer.outputs["Color"], mix.inputs[6])
        stain = _ramp(nodes, links, cloud.outputs["Fac"],
                      0.2, (0.46, 0.372, 0.255, 1), 0.8, (0.60, 0.51, 0.38, 1))
        links.new(stain.outputs["Color"], mix.inputs[7])
        links.new(mix.outputs[2], bsdf.inputs["Base Color"])
        grain = _noise(nodes, links, vec, 70.0, detail=3.0)
        _rough_noise(nodes, links, bsdf, vec, 0.94, 0.04, scale=50.0)
        _bump(nodes, links, bsdf, grain.outputs["Fac"], 0.06)
        return m
    return _cached("earth", build)


def m_ground():
    """夯土地面（预览渲染用）。"""
    def build():
        m, nodes, links, bsdf, vec = _base("ground")
        cloud = _noise(nodes, links, vec, 0.35, detail=6.0, rough=0.6)
        base = _ramp(nodes, links, cloud.outputs["Fac"],
                     0.2, (0.400, 0.345, 0.262, 1), 0.8, (0.482, 0.425, 0.330, 1))
        links.new(base.outputs["Color"], bsdf.inputs["Base Color"])
        grain = _noise(nodes, links, vec, 24.0, detail=4.0)
        _rough_noise(nodes, links, bsdf, vec, 0.95, 0.04, scale=20.0)
        _bump(nodes, links, bsdf, grain.outputs["Fac"], 0.08)
        return m
    return _cached("ground", build)


def mat(name, rgb, rough=0.85, tint=0.0):
    """兜底纯色材质（个别小件仍在用）。"""
    def build():
        m, nodes, links, bsdf, vec = _base(name)
        bsdf.inputs["Base Color"].default_value = (*rgb, 1.0)
        bsdf.inputs["Roughness"].default_value = rough
        return m
    return _cached(name, build)


# ---------------- 基础件 ----------------

def add_box(name, size, loc, material, bevel=0.02, rot=(0, 0, 0)):
    bpy.ops.mesh.primitive_cube_add(size=1, location=loc, rotation=rot)
    ob = bpy.context.object
    ob.name = name
    ob.scale = (size[0], size[1], size[2])
    bpy.ops.object.transform_apply(scale=True)
    if bevel > 0:
        mod = ob.modifiers.new("bev", "BEVEL")
        mod.width = bevel
        mod.segments = 2
        mod.limit_method = "ANGLE"
    ob.data.materials.append(material)
    return ob


def add_cylinder(name, r, depth, loc, material, verts=24, rot=(0, 0, 0)):
    bpy.ops.mesh.primitive_cylinder_add(
        vertices=verts, radius=r, depth=depth, location=loc, rotation=rot)
    ob = bpy.context.object
    ob.name = name
    ob.data.materials.append(material)
    return ob


def mesh_from_pydata(name, verts, faces, material, smooth=False):
    me = bpy.data.meshes.new(name)
    me.from_pydata(verts, [], faces)
    me.update()
    ob = bpy.data.objects.new(name, me)
    bpy.context.collection.objects.link(ob)
    ob.data.materials.append(material)
    if smooth:
        for p in me.polygons:
            p.use_smooth = True
    return ob


def roof_profile(t):
    """t: 0=脊 → 1=檐。举折凹曲：近脊陡、近檐缓。"""
    y = HALF_SPAN * t
    z = RISE * (1.0 - t) ** 1.45
    return y, z


# ---------------- 大木作构件 ----------------

def build_plinth():
    add_box("plinth", (W + 2.2, D + 2.2, PLINTH_H - 0.012),
            (0, 0, (PLINTH_H - 0.012) / 2), m_stone(), bevel=0.03)
    add_box("plinth_edge", (W + 2.35, D + 2.35, 0.12),
            (0, 0, PLINTH_H - 0.054), m_stone(), bevel=0.02)
    for i in range(3):
        add_box(f"step{i}", (2.4, 0.42, PLINTH_H / 3),
                (0, -(D + 2.2) / 2 - 0.21 - i * 0.42,
                 PLINTH_H - (i + 0.5) * PLINTH_H / 3),
                m_stone(), bevel=0.02)


def column_xs():
    return [-W / 2, -W / 6, W / 6, W / 2]


def build_columns():
    r = CH * 0.072
    for x in column_xs():
        for ys in (-1, 1):
            y = ys * D / 2
            ob = add_cylinder("column", r, CH, (x, y, BASE_TOP + CH / 2),
                              m_timber(), verts=28)
            bm = bmesh.new()
            bm.from_mesh(ob.data)
            for v in bm.verts:
                zt = (v.co.z + CH / 2) / CH
                if zt > 0.62:
                    k = 1.0 - 0.10 * ((zt - 0.62) / 0.38) ** 1.6
                    v.co.x *= k
                    v.co.y *= k
            bm.to_mesh(ob.data)
            bm.free()
            for p in ob.data.polygons:
                p.use_smooth = True
            add_cylinder("col_base", r * 1.55, 0.14,
                         (x, y, BASE_TOP + 0.07), m_stone(), verts=28)
            add_cylinder("col_base2", r * 1.25, 0.10,
                         (x, y, BASE_TOP + 0.19), m_stone(), verts=28)


def build_lintels():
    z = COL_TOP - 0.14
    add_box("lintel_f", (W + 0.5, 0.24, 0.30), (0, -D / 2, z), m_timber())
    add_box("lintel_b", (W + 0.5, 0.24, 0.30), (0, D / 2, z), m_timber())
    add_box("lintel_l", (0.24, D, 0.30), (-W / 2, 0, z), m_timber())
    add_box("lintel_r", (0.24, D, 0.30), (W / 2, 0, z), m_timber())


def build_bracket_set(x, ys):
    y0 = ys * D / 2
    z0 = COL_TOP

    def bx(name, size, loc, material=None, rot=(0, 0, 0)):
        return add_box(name, size, loc, material or m_timber(), bevel=0.018, rot=rot)

    bx("ludou_a", (0.44, 0.44, 0.14), (x, y0, z0 + 0.07))
    bx("ludou_b", (0.34, 0.34, 0.12), (x, y0, z0 + 0.18))
    bx("huagong1", (0.24, 0.72, 0.16), (x, y0 + ys * 0.15, z0 + 0.33))
    bx("huagong2", (0.24, 1.15, 0.16), (x, y0 + ys * 0.34, z0 + 0.62))
    for dy, dz in ((0.36, 0.48), (0.62, 0.77)):
        bx("jiaohudou", (0.26, 0.26, 0.10), (x, y0 + ys * dy, z0 + dz))
    ang_len = EAVE * 1.05
    pitch = math.radians(23)
    cy = y0 + ys * ang_len * 0.34
    cz = z0 + BRACKET_H * 0.50 - math.sin(pitch) * ang_len * 0.20
    bx("xiaang", (0.17, ang_len, 0.12), (x, cy, cz), m_timber_dark(),
       rot=(ys * pitch, 0, 0))
    bx("linggong", (0.95, 0.20, 0.15), (x, y0 + ys * 0.34, z0 + 0.90))
    for dx in (-0.38, 0.38):
        bx("sandou", (0.22, 0.22, 0.10), (x + dx, y0 + ys * 0.34, z0 + 1.02))


def build_brackets():
    xs = column_xs()
    pu_xs = xs + [(xs[i] + xs[i + 1]) / 2 for i in range(len(xs) - 1)]
    for x in pu_xs:
        for ys in (-1, 1):
            build_bracket_set(x, ys)
    for ys in (-1, 1):
        add_box("gongyan", (W + 0.3, 0.14, BRACKET_H * 0.58),
                (0, ys * (D / 2 - 0.16), COL_TOP + BRACKET_H * 0.42), m_wall(),
                bevel=0.0)
        add_box("zhengxinfang", (W + 0.6, 0.22, 0.14),
                (0, ys * D / 2, COL_TOP + 0.50), m_timber())
        add_box("luohanfang", (W + 0.6, 0.20, 0.13),
                (0, ys * (D / 2 + 0.34), COL_TOP + 1.14), m_timber())
        add_cylinder("purlin", 0.11, ROOF_W - 0.4,
                     (0, ys * (D / 2 + EAVE * 0.30), COL_TOP + BRACKET_H - 0.06),
                     m_timber(), rot=(0, math.pi / 2, 0))
    for xsn in (-1, 1):
        for z in (-D / 6, D / 6):
            add_box("bracket_side", (0.34, 0.42, BRACKET_H * 0.6),
                    (xsn * W / 2, z, COL_TOP + BRACKET_H * 0.3), m_timber_dark())


def build_rafters():
    step = 0.34
    n = int(ROOF_W / step)
    y_in, z_in = roof_profile(0.62)
    y_out, z_out = roof_profile(1.0)
    for i in range(n + 1):
        x = -ROOF_W / 2 + 0.2 + (ROOF_W - 0.4) * i / n
        for ys in (-1, 1):
            inner = Vector((x, ys * y_in, EAVE_Y + z_in - 0.03))
            outer = Vector((x, ys * (y_out + 0.30), EAVE_Y + z_out - 0.10))
            mid = (inner + outer) / 2
            length = (outer - inner).length
            pitch = math.atan2(inner.z - outer.z, abs(outer.y - inner.y))
            add_cylinder("rafter", 0.048, length,
                         (mid.x, mid.y, mid.z), m_timber_dark(),
                         verts=10, rot=(ys * (math.pi / 2 + pitch), 0, 0))


def roof_slope_mesh(ys):
    verts, faces = [], []
    nx = 12
    for j in range(PROFILE_STEPS + 1):
        t = j / PROFILE_STEPS
        y, z = roof_profile(t)
        for i in range(nx + 1):
            x = -ROOF_W / 2 + ROOF_W * i / nx
            verts.append((x, ys * y, EAVE_Y + z))
    for j in range(PROFILE_STEPS):
        for i in range(nx):
            a = j * (nx + 1) + i
            b = a + 1
            c = a + (nx + 1) + 1
            d = a + (nx + 1)
            faces.append((a, b, c, d) if ys > 0 else (a, d, c, b))
    ob = mesh_from_pydata(f"roof_deck_{'b' if ys > 0 else 'f'}", verts, faces,
                          m_tile(), smooth=True)
    mod = ob.modifiers.new("sol", "SOLIDIFY")
    mod.thickness = 0.10
    return ob


def tile_run_curve(ys, x):
    cu = bpy.data.curves.new("tile_run", "CURVE")
    cu.dimensions = "3D"
    cu.bevel_depth = 0.072
    cu.bevel_resolution = 2
    sp = cu.splines.new("POLY")
    sp.points.add(PROFILE_STEPS)
    for j in range(PROFILE_STEPS + 1):
        t = 0.05 + 0.95 * j / PROFILE_STEPS
        y, z = roof_profile(t)
        sp.points[j].co = (x, ys * (y + (0.10 if j == PROFILE_STEPS else 0.0)),
                           EAVE_Y + z + 0.10, 1)
    ob = bpy.data.objects.new("tile_run", cu)
    bpy.context.collection.objects.link(ob)
    ob.data.materials.append(m_tile())
    return ob


def build_roof():
    for ys in (-1, 1):
        roof_slope_mesh(ys)
        step = 0.44
        n = int((ROOF_W - 0.5) / step)
        for i in range(n + 1):
            x = -(ROOF_W - 0.5) / 2 + (ROOF_W - 0.5) * i / n
            tile_run_curve(ys, x)
            y_edge, z_edge = roof_profile(1.0)
            pitch = math.atan2(
                roof_profile(0.85)[1] - z_edge, (1 - 0.85) * HALF_SPAN)
            add_cylinder("wadang", 0.075, 0.05,
                         (x, ys * (y_edge + 0.12), EAVE_Y + 0.085),
                         m_ridge(), verts=16,
                         rot=(ys * (math.pi / 2 - pitch), 0, 0))

    ridge_len = (ROOF_W / 2 - 0.55) * 2 - 0.15
    add_box("ridge0", (ridge_len, 0.52, 0.15),
            (0, 0, EAVE_Y + RISE + 0.10), m_ridge(), bevel=0.02)
    add_box("ridge1", (ridge_len, 0.40, 0.14),
            (0, 0, EAVE_Y + RISE + 0.24), m_ridge(), bevel=0.02)
    add_cylinder("ridge_cap", 0.16, ridge_len,
                 (0, 0, EAVE_Y + RISE + 0.36), m_ridge(),
                 verts=20, rot=(0, math.pi / 2, 0))
    build_bofeng()
    build_chiwei(-1, ROOF_W / 2 - 0.55, EAVE_Y + RISE + 0.06, CH * 0.55)
    build_chiwei(1, ROOF_W / 2 - 0.55, EAVE_Y + RISE + 0.06, CH * 0.55)


def build_bofeng():
    seg = 8
    for xsn in (-1, 1):
        for ys in (-1, 1):
            for j in range(seg):
                t0 = j / seg
                t1 = (j + 1) / seg
                y0, z0 = roof_profile(t0)
                y1, z1 = roof_profile(t1)
                mid_y = ys * (y0 + y1) / 2
                mid_z = EAVE_Y + (z0 + z1) / 2 + 0.06
                length = math.hypot(y1 - y0, z1 - z0) + 0.06
                pitch = math.atan2(z0 - z1, y1 - y0)
                add_box("bofeng", (0.07, length, 0.34),
                        (xsn * (ROOF_W / 2 - 0.02), mid_y, mid_z),
                        m_timber_dark(), bevel=0.012,
                        rot=(ys * -pitch, 0, 0))


def chiwei_profile(h):
    """鸱尾轮廓 v2：卷首加深的尾鳍，无兽头。与 C# ChiweiProfile 同步。"""
    w = h * 0.52

    def bez(a, b, c, t):
        u = 1 - t
        return u * u * a + 2 * u * t * b + t * t * c

    pts = [(w * 0.95, 0.0), (w * 0.95, h * 0.14)]
    steps = 9
    # 外缘大弧：基座外沿 → 顶
    for i in range(1, steps + 1):
        t = i / steps
        pts.append((bez(w * 0.95, w * 1.04, w * 0.30, t),
                    bez(h * 0.14, h * 0.74, h, t)))
    # 卷首：更大的内卷弧，钩尖收到内下方
    for i in range(1, steps + 1):
        t = i / steps
        pts.append((bez(w * 0.30, w * 0.02, w * 0.24, t),
                    bez(h, h * 0.98, h * 0.72, t)))
    pts += [(w * 0.14, h * 0.50), (0.0, h * 0.10), (0.0, 0.0)]
    return pts


def build_chiwei(xs, ridge_half, base_z, h):
    prof = chiwei_profile(h)
    thick = 0.20
    verts, faces = [], []
    n = len(prof)
    for side in (-1, 1):
        for (px, pz) in prof:
            verts.append((xs * px, side * thick / 2, pz))
    for i in range(1, n - 1):
        faces.append((0, i, i + 1))
        faces.append((n, n + i + 1, n + i))
    for i in range(n):
        j = (i + 1) % n
        faces.append((i, j, n + j, n + i))
    base_x = xs * ridge_half
    ob = mesh_from_pydata("chiwei", verts, faces, m_ridge())
    ob.location = (base_x, 0, base_z)
    outer = [prof[3], prof[6], prof[9]]
    inner_dir = (prof[12][0] - prof[6][0], prof[12][1] - prof[6][1])
    norm = math.hypot(*inner_dir)
    inner_dir = (inner_dir[0] / norm, inner_dir[1] / norm)
    for k, (px, pz) in enumerate(outer):
        rib_len = h * (0.34 - k * 0.05)
        cx = px + inner_dir[0] * rib_len * 0.42
        cz = pz + inner_dir[1] * rib_len * 0.42
        angle = math.atan2(inner_dir[1], xs * inner_dir[0])
        add_box("chiwei_rib", (rib_len, thick + 0.05, 0.055),
                (base_x + xs * cx, 0, base_z + cz),
                m_ridge(), bevel=0.01, rot=(0, -angle, 0))


def build_lattice_window(cx, y, cz):
    ww, wh, fb = 1.6, 1.35, 0.10
    for zs in (-1, 1):
        add_box("win_frame_h", (ww, 0.09, fb),
                (cx, y, cz + zs * (wh / 2 - fb / 2)), m_door())
    for xsn in (-1, 1):
        add_box("win_frame_v", (fb, 0.09, wh - fb * 2),
                (cx + xsn * (ww / 2 - fb / 2), y, cz), m_door())
    clear = ww - fb * 2
    count = int(clear / 0.16) - 1
    span = clear / (count + 1)
    for i in range(count):
        ox = span * (i + 1) - clear / 2
        add_box("lattice", (0.045, 0.06, wh - fb * 2),
                (cx + ox, y, cz), m_door(), bevel=0.008)


def build_walls_and_openings(door_w=1.7, door_h=2.3, windows=True):
    wall_t = 0.26
    wall_h = CH - 0.1
    zc = BASE_TOP + wall_h / 2
    add_box("wall_back", (W - 0.3, wall_t, wall_h), (0, D / 2 - 0.22, zc), m_wall(), 0.0)
    gable_h = EAVE_Y - BASE_TOP - 0.04
    for xsn in (-1, 1):
        add_box("wall_gable", (wall_t, D - 0.3, gable_h),
                (xsn * (W / 2 - 0.18), 0, BASE_TOP + gable_h / 2), m_wall(), 0.0)
        verts = [(0, -D / 2 + 0.10, EAVE_Y - 0.05),
                 (0, D / 2 - 0.10, EAVE_Y - 0.05),
                 (0, 0, EAVE_Y + RISE * 0.90)]
        ob = mesh_from_pydata("gable_tri", verts, [(0, 1, 2)], m_wall())
        ob.location.x = xsn * (W / 2 - 0.18)
        mod = ob.modifiers.new("sol", "SOLIDIFY")
        mod.thickness = wall_t
    seg_w = (W - 0.3 - door_w) / 2
    for xsn in (-1, 1):
        cx = xsn * (door_w / 2 + seg_w / 2)
        add_box("wall_front", (seg_w, wall_t, wall_h),
                (cx, -D / 2 + 0.22, zc), m_wall(), 0.0)
        if windows:
            build_lattice_window(cx, -D / 2 + 0.22 - 0.16, BASE_TOP + CH * 0.55)
    add_box("wall_overdoor", (door_w + 0.2, wall_t, wall_h - door_h),
            (0, -D / 2 + 0.22, BASE_TOP + door_h + (wall_h - door_h) / 2), m_wall(), 0.0)
    for xsn in (-1, 1):
        add_box("door_leaf", (door_w / 2 - 0.03, 0.10, door_h),
                (xsn * door_w / 4, -D / 2 + 0.12, BASE_TOP + door_h / 2), m_door())
        for r in range(4):
            for c in range(3):
                add_cylinder("door_nail", 0.028, 0.03,
                             (xsn * (door_w / 4 - 0.24 + c * 0.24),
                              -D / 2 + 0.06,
                              BASE_TOP + 0.45 + r * 0.52),
                             m_ridge(), verts=10, rot=(math.pi / 2, 0, 0))


def build_hall(w, d, ch, door_w=1.7, windows=True):
    set_dims(w, d, ch)
    build_plinth()
    build_columns()
    build_lintels()
    build_brackets()
    build_rafters()
    build_roof()
    build_walls_and_openings(door_w=door_w, windows=windows)


# ---------------- 门楼与井亭 ----------------

def small_roof(width, depth, base_z, rise, tile_step=0.5, thick=0.10):
    """小型两坡屋面（门楼/井亭用）：直坡+瓦垄+脊。"""
    half = depth / 2
    slope_len = math.hypot(half, rise) + 0.15
    pitch = math.atan2(rise, half)
    for ys in (-1, 1):
        add_box("sroof", (width, slope_len, thick),
                (0, ys * half / 2, base_z + rise / 2),
                m_tile(), bevel=0.015, rot=(ys * -pitch, 0, 0))
        n = int(width / tile_step)
        span = width - 0.3
        for i in range(n + 1):
            x = -span / 2 + span * i / n
            add_cylinder("sroof_tile", 0.06, slope_len - 0.05,
                         (x, ys * half / 2, base_z + rise / 2 + thick * 0.8),
                         m_tile(), verts=10, rot=(ys * -pitch + math.pi / 2, 0, 0))
    add_box("sroof_ridge", (width - 0.1, 0.30, 0.13),
            (0, 0, base_z + rise + 0.07), m_ridge(), bevel=0.015)
    add_cylinder("sroof_cap", 0.10, width - 0.2,
                 (0, 0, base_z + rise + 0.17), m_ridge(),
                 verts=16, rot=(0, math.pi / 2, 0))


def build_gate():
    """坊门楼：门墩、过梁、小屋面带鸱尾、独立铰点门扇（GateLeaf_L/R）。"""
    for xsn in (-1, 1):
        add_box("gate_pier", (2.6, 1.9, 3.6), (xsn * 4.2, 0, 1.8),
                m_earth(), bevel=0.03)
        add_box("gate_pier_cap", (2.8, 2.1, 0.18), (xsn * 4.2, 0, 3.66),
                m_tile(), bevel=0.02)
    add_box("gate_lintel", (11.6, 1.9, 0.7), (0, 0, 4.05), m_timber(), bevel=0.03)
    # 檐下小铺作意象
    for x in (-3.4, 0, 3.4):
        add_box("gate_dou", (0.4, 0.4, 0.14), (x, 0, 4.47), m_timber())
        add_box("gate_gong", (1.0, 0.5, 0.14), (x, 0, 4.62), m_timber())
    small_roof(12.6, 2.9, 4.75, 0.55)
    for xsn in (-1, 1):
        build_chiwei(xsn, 6.0, 5.35, 0.85)
    # 门扇：原点在铰边（Unity 里旋转即开闭）
    for xsn in (-1, 1):
        name = "GateLeaf_L" if xsn < 0 else "GateLeaf_R"
        bpy.ops.mesh.primitive_cube_add(size=1, location=(xsn * 2.9, 0, 0))
        leaf = bpy.context.object
        leaf.name = name
        leaf.scale = (2.9, 0.16, 3.4)
        bpy.ops.object.transform_apply(scale=True)
        # 把网格移到铰点一侧：物体原点保持在铰线上
        bm = bmesh.new()
        bm.from_mesh(leaf.data)
        for v in bm.verts:
            v.co.x -= xsn * 1.45
            v.co.z += 1.7
        bm.to_mesh(leaf.data)
        bm.free()
        leaf.data.materials.append(m_door())
        # 门钉
        for r in range(4):
            for c in range(4):
                add_cylinder("gate_nail", 0.045, 0.05,
                             (xsn * (0.5 + c * 0.62), -0.10, 0.6 + r * 0.72),
                             m_ridge(), verts=10, rot=(math.pi / 2, 0, 0))


def build_well():
    """井亭：石井圈、口沿、四柱、小两坡顶。"""
    add_cylinder("well_ring", 0.62, 0.9, (0, 0, 0.45), m_stone(), verts=28)
    bpy.ops.mesh.primitive_torus_add(
        major_radius=0.56, minor_radius=0.07, location=(0, 0, 0.90))
    torus = bpy.context.object
    torus.name = "well_rim"
    torus.data.materials.append(m_stone())
    add_cylinder("well_mouth", 0.45, 0.06, (0, 0, 0.92),
                 mat("well_dark", (0.06, 0.06, 0.07), 0.9), verts=24)
    for x, y in ((-0.95, -0.95), (-0.95, 0.95), (0.95, -0.95), (0.95, 0.95)):
        add_cylinder("well_col", 0.10, 2.5, (x, y, 1.25), m_timber(), verts=16)
    for ys in (-1, 1):
        add_box("well_fang", (2.4, 0.14, 0.16), (0, ys * 0.95, 2.45), m_timber())
    small_roof(3.0, 2.6, 2.55, 0.42, tile_step=0.42, thick=0.08)


# ---------------- 环境 / 相机 / 渲染 ----------------

def clear_scene():
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete()
    _mats.clear()


def build_ground():
    bpy.ops.mesh.primitive_plane_add(size=420, location=(0, 0, 0))
    ob = bpy.context.object
    ob.name = "ground"
    ob.data.materials.append(m_ground())


def setup_world():
    sun = bpy.data.lights.new("sun", "SUN")
    sun.energy = 1.4
    sun.angle = math.radians(1.6)
    sun.color = (1.0, 0.95, 0.86)
    sun_ob = bpy.data.objects.new("sun", sun)
    bpy.context.collection.objects.link(sun_ob)
    sun_ob.rotation_euler = Euler((math.radians(90 - 52), 0, math.radians(-35)))

    world = bpy.data.worlds.new("world")
    bpy.context.scene.world = world
    world.use_nodes = True
    nodes = world.node_tree.nodes
    links = world.node_tree.links
    sky = nodes.new("ShaderNodeTexSky")
    sky.sky_type = "NISHITA"
    sky.sun_elevation = math.radians(52)
    sky.sun_rotation = math.radians(145)
    sky.sun_intensity = 0.10
    bg = nodes["Background"]
    bg.inputs["Strength"].default_value = 0.22
    links.new(sky.outputs["Color"], bg.inputs["Color"])

    scene = bpy.context.scene
    scene.view_settings.view_transform = "Filmic"
    scene.view_settings.look = "Medium High Contrast"
    scene.view_settings.exposure = -0.65


def add_camera(name, loc, look_at, fov=38):
    cam_data = bpy.data.cameras.new(name)
    cam_data.angle = math.radians(fov)
    cam = bpy.data.objects.new(name, cam_data)
    bpy.context.collection.objects.link(cam)
    cam.location = loc
    direction = Vector(look_at) - Vector(loc)
    cam.rotation_euler = direction.to_track_quat("-Z", "Y").to_euler()
    return cam


def render(cam, path, w=1600, h=900, samples=224):
    scene = bpy.context.scene
    scene.camera = cam
    scene.render.engine = "CYCLES"
    scene.cycles.samples = samples
    scene.cycles.use_denoising = False
    scene.render.resolution_x = w
    scene.render.resolution_y = h
    scene.render.filepath = path
    bpy.ops.render.render(write_still=True)
    print("[tang_hall] 渲染:", path)


def export_fbx(path):
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.export_scene.fbx(
        filepath=path,
        use_selection=True,
        apply_scale_options="FBX_SCALE_ALL",
        axis_forward="-Z",
        axis_up="Y",
        use_mesh_modifiers=True,
        bake_space_transform=True,
        path_mode="STRIP",
        add_leaf_bones=False)
    print("[tang_hall] 导出:", path)


# ---------------- 烘焙管线（写实化：albedo×AO 进图集，随 FBX 出） ----------------

def _scene_meshes():
    return [o for o in bpy.context.scene.objects if o.type == "MESH"]


def _select_meshes(meshes):
    bpy.ops.object.select_all(action="DESELECT")
    for ob in meshes:
        ob.select_set(True)
    bpy.context.view_layer.objects.active = meshes[0]


def uv_unwrap_all():
    """多对象一次 Smart UV Project：全部岛共享打包进同一 0-1（图集）。"""
    meshes = _scene_meshes()
    _select_meshes(meshes)
    bpy.ops.object.mode_set(mode="EDIT")
    bpy.ops.mesh.select_all(action="SELECT")
    bpy.ops.uv.smart_project(island_margin=0.004, correct_aspect=True)
    bpy.ops.object.mode_set(mode="OBJECT")


def _scene_materials():
    seen = []
    for ob in _scene_meshes():
        for slot in ob.material_slots:
            if slot.material is not None and slot.material not in seen:
                seen.append(slot.material)
    return seen


def _attach_bake_targets(image):
    """每个在用材质挂一个指向同一图集的 Image 节点并设为 active（bake 落点）。"""
    nodes_added = []
    for m in _scene_materials():
        nodes = m.node_tree.nodes
        node = nodes.get("_bake_target")
        if node is None:
            node = nodes.new("ShaderNodeTexImage")
            node.name = "_bake_target"
        node.image = image
        node.select = True
        nodes.active = node
        nodes_added.append((m, node))
    return nodes_added


def _detach_bake_targets():
    for m in _scene_materials():
        node = m.node_tree.nodes.get("_bake_target")
        if node is not None:
            m.node_tree.nodes.remove(node)


def _new_image(name, size, noncolor=False):
    img = bpy.data.images.new(name, size, size, alpha=False, float_buffer=True)
    if noncolor:
        img.colorspace_settings.name = "Non-Color"
    return img


def bake_atlas(name, out_dir, size):
    """烘 DIFFUSE 色/AO/粗糙度 → 合成 RGB=albedo×软化AO、A=1-rough 的单图集。"""
    import numpy as np

    scene = bpy.context.scene
    scene.render.engine = "CYCLES"
    scene.cycles.device = "CPU"
    scene.render.bake.margin = 6

    # AO 遮蔽用临时地面（不选中不烘焙，只当遮挡体——檐下自然变暗）
    bpy.ops.mesh.primitive_plane_add(size=200, location=(0, 0, -0.01))
    ao_ground = bpy.context.object
    ao_ground.name = "_ao_ground"

    meshes = [o for o in _scene_meshes() if o.name != "_ao_ground"]
    _select_meshes(meshes)

    img_alb = _new_image(name + "_alb", size)
    img_ao = _new_image(name + "_ao", size, noncolor=True)
    img_rough = _new_image(name + "_rough", size, noncolor=True)

    _attach_bake_targets(img_alb)
    scene.cycles.samples = 1
    scene.render.bake.use_pass_direct = False
    scene.render.bake.use_pass_indirect = False
    scene.render.bake.use_pass_color = True
    bpy.ops.object.bake(type="DIFFUSE")
    print("[tang_hall] 烘焙 albedo 完成:", name)

    _attach_bake_targets(img_rough)
    bpy.ops.object.bake(type="ROUGHNESS")
    print("[tang_hall] 烘焙 roughness 完成:", name)

    _attach_bake_targets(img_ao)
    scene.cycles.samples = 48
    bpy.ops.object.bake(type="AO")
    print("[tang_hall] 烘焙 AO 完成:", name)

    _detach_bake_targets()
    bpy.data.objects.remove(ao_ground, do_unlink=True)

    n = size * size * 4
    alb = np.empty(n, dtype=np.float32)
    ao = np.empty(n, dtype=np.float32)
    rough = np.empty(n, dtype=np.float32)
    img_alb.pixels.foreach_get(alb)
    img_ao.pixels.foreach_get(ao)
    img_rough.pixels.foreach_get(rough)
    alb = alb.reshape(-1, 4)
    ao_soft = ao.reshape(-1, 4)[:, 0] * 0.72 + 0.28  # AO 柔化：留环境光余地
    out = np.empty_like(alb)
    out[:, 0] = alb[:, 0] * ao_soft
    out[:, 1] = alb[:, 1] * ao_soft
    out[:, 2] = alb[:, 2] * ao_soft
    out[:, 3] = 1.0 - rough.reshape(-1, 4)[:, 0]  # A 通道 = smoothness

    img_out = bpy.data.images.new(name + "_albedo", size, size,
                                  alpha=True, float_buffer=True)
    img_out.pixels.foreach_set(out.ravel())
    img_out.filepath_raw = os.path.join(out_dir, name + "_albedo.png")
    img_out.file_format = "PNG"
    img_out.save()
    print("[tang_hall] 图集落盘:", img_out.filepath_raw)

    for img in (img_alb, img_ao, img_rough, img_out):
        bpy.data.images.remove(img)


BAKE_SIZE = {
    "tang_hall_large": 2048,
    "tang_gate": 2048,
    "tang_hall_small": 1024,
    "tang_hall_post": 1024,
    "tang_well": 1024,
}


ASSETS = {
    "tang_hall_large": lambda: build_hall(9.0, 6.0, 3.6),
    "tang_hall_small": lambda: build_hall(6.0, 4.5, 3.0, door_w=1.4),
    "tang_hall_post": lambda: build_hall(4.5, 3.5, 2.8, door_w=1.2, windows=False),
    "tang_gate": build_gate,
    "tang_well": build_well,
}


def main():
    mode = "--render"
    out_dir = "artifacts/blender-preview"
    if "--" in sys.argv:
        args = sys.argv[sys.argv.index("--") + 1:]
        if args:
            mode = args[0]
        if len(args) > 1:
            out_dir = args[1]
    os.makedirs(out_dir, exist_ok=True)

    if mode == "--export":
        for name, builder in ASSETS.items():
            clear_scene()
            builder()
            export_fbx(os.path.join(out_dir, name + ".fbx"))
        return

    if mode == "--bake":
        # 写实管线：建模 → 图集 UV → 烘 albedo×AO+smoothness → FBX+贴图成对出
        for name, builder in ASSETS.items():
            clear_scene()
            builder()
            uv_unwrap_all()
            bake_atlas(name, out_dir, BAKE_SIZE[name])
            export_fbx(os.path.join(out_dir, name + ".fbx"))
        return

    clear_scene()
    build_ground()
    build_hall(9.0, 6.0, 3.6)
    setup_world()
    cam1 = add_camera("cam_front", (15.5, -18.5, 7.0), (0, 0.4, 3.6), fov=36)
    cam2 = add_camera("cam_eave", (7.6, -9.2, 4.4), (2.0, -2.4, 5.4), fov=28)
    cam3 = add_camera("cam_ridge", (-11.5, -10.0, 9.6), (-3.6, 0.4, 6.2), fov=30)
    render(cam1, os.path.join(out_dir, "hall_front.png"))
    render(cam2, os.path.join(out_dir, "hall_eave.png"))
    render(cam3, os.path.join(out_dir, "hall_ridge.png"))
    bpy.ops.wm.save_as_mainfile(filepath=os.path.join(out_dir, "tang_hall.blend"))


main()
