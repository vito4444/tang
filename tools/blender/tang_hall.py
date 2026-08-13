#!/usr/bin/env python3
"""程序化唐构大殿精模（Blender 4.x, headless）。

用法:
    blender -b -P tools/blender/tang_hall.py -- --out artifacts/blender-preview

与 LingyanUnity/Assets/Scripts/Core/Architecture/TangArchitectureSpec.cs 同步的红线：
    举高/进深 = 1/6（举折凹曲，檐缓脊陡）
    出檐/柱高 = 0.55
    铺作层高/柱高 = 0.5
    鸱尾：卷尾鳍形、有肋、无兽头（早唐至盛唐形制）
形制参照：佛光寺东大殿、南禅寺大殿（悬山三间试制；庑殿四阿留待下版）。
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

# ---------------- 尺寸（米，Blender Z 朝上） ----------------
W = 9.0        # 面阔（x）
D = 6.0        # 进深（y）
CH = 3.6       # 柱高
PLINTH_H = 0.55
GABLE_OVERHANG = 1.0   # 悬山出际

EAVE = CH * EAVE_PER_COL
BRACKET_H = CH * BRACKET_PER_COL
RISE = D * RISE_PER_DEPTH

BASE_TOP = PLINTH_H
COL_TOP = BASE_TOP + CH
EAVE_Y = COL_TOP + BRACKET_H          # 檐口标高（铺作层顶）
HALF_SPAN = D / 2.0 + EAVE            # 檐口至脊的水平半跨
ROOF_W = W + GABLE_OVERHANG * 2.0

# ---------------- 材质 ----------------
_mats = {}


def mat(name, rgb, rough=0.85, tint=0.0):
    key = name
    if key in _mats:
        return _mats[key]
    m = bpy.data.materials.new(name)
    m.use_nodes = True
    bsdf = m.node_tree.nodes["Principled BSDF"]
    bsdf.inputs["Base Color"].default_value = (*rgb, 1.0)
    bsdf.inputs["Roughness"].default_value = rough
    if tint > 0:
        # 每物体随机明度差，瓦垄不至于死板
        nodes = m.node_tree.nodes
        links = m.node_tree.links
        info = nodes.new("ShaderNodeObjectInfo")
        ramp = nodes.new("ShaderNodeValToRGB")
        ramp.color_ramp.elements[0].color = (*[c * (1 - tint) for c in rgb], 1)
        ramp.color_ramp.elements[1].color = (*[min(1, c * (1 + tint)) for c in rgb], 1)
        links.new(info.outputs["Random"], ramp.inputs["Fac"])
        links.new(ramp.outputs["Color"], bsdf.inputs["Base Color"])
    _mats[key] = m
    return m


def m_timber():
    return mat("timber", (0.400, 0.155, 0.092), 0.72)


def m_timber_dark():
    return mat("timber_dark", (0.300, 0.118, 0.078), 0.75)


def m_wall():
    return mat("wall", (0.760, 0.715, 0.615), 0.95)


def m_tile():
    return mat("tile", (0.148, 0.156, 0.175), 0.88, tint=0.12)


def m_ridge():
    return mat("ridge", (0.095, 0.100, 0.112), 0.85)


def m_stone():
    return mat("stone", (0.545, 0.525, 0.475), 0.92)


def m_door():
    return mat("door", (0.26, 0.185, 0.125), 0.78)


def m_ground():
    return mat("ground", (0.44, 0.385, 0.30), 0.98)


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


# ---------------- 举折屋面剖面 ----------------

def roof_profile(t):
    """t: 0=脊 → 1=檐。返回 (水平距, 相对檐口的高度)。
    举折：凹曲屋面，近脊陡、近檐缓（唐制观感的关键一笔）。"""
    y = HALF_SPAN * t
    z = RISE * (1.0 - t) ** 1.45
    return y, z


PROFILE_STEPS = 9


# ---------------- 构件 ----------------

def build_plinth():
    add_box("plinth", (W + 2.2, D + 2.2, PLINTH_H - 0.012),
            (0, 0, (PLINTH_H - 0.012) / 2), m_stone(), bevel=0.03)
    # 阶沿压边（顶面略高，避免与台基顶共面）
    add_box("plinth_edge", (W + 2.35, D + 2.35, 0.12),
            (0, 0, PLINTH_H - 0.054), m_stone(), bevel=0.02)
    # 南向踏道三级
    for i in range(3):
        add_box(f"step{i}", (2.4, 0.42, PLINTH_H / 3),
                (0, -(D + 2.2) / 2 - 0.21 - i * 0.42,
                 PLINTH_H - (i + 0.5) * PLINTH_H / 3),
                m_stone(), bevel=0.02)


def column_xs():
    return [-W / 2, -W / 6, W / 6, W / 2]


def build_columns():
    r = CH * 0.072  # 柱径约 1/7 柱高，唐柱粗壮
    for x in column_xs():
        for ys in (-1, 1):
            y = ys * D / 2
            ob = add_cylinder("column", r, CH, (x, y, BASE_TOP + CH / 2),
                              m_timber(), verts=28)
            # 卷杀：柱上段渐收
            bm = bmesh.new()
            bm.from_mesh(ob.data)
            for v in bm.verts:
                zt = (v.co.z + CH / 2) / CH  # 0 底 → 1 顶
                if zt > 0.62:
                    k = 1.0 - 0.10 * ((zt - 0.62) / 0.38) ** 1.6
                    v.co.x *= k
                    v.co.y *= k
            bm.to_mesh(ob.data)
            bm.free()
            for p in ob.data.polygons:
                p.use_smooth = True
            # 覆盆柱础
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
    """一攒铺作：栌斗+两跳华拱+下昂+令拱+散斗。ys=±1 檐向。"""
    y0 = ys * D / 2
    z0 = COL_TOP
    g = bpy.data.collections.get("brackets")

    def bx(name, size, loc, material=None, rot=(0, 0, 0)):
        return add_box(name, size, loc, material or m_timber(), bevel=0.018, rot=rot)

    # 栌斗（斗形：上大下小两段）
    bx("ludou_a", (0.44, 0.44, 0.14), (x, y0, z0 + 0.07))
    bx("ludou_b", (0.34, 0.34, 0.12), (x, y0, z0 + 0.18))
    # 华拱两跳（沿檐向外伸，跳长收紧）
    bx("huagong1", (0.24, 0.72, 0.16), (x, y0 + ys * 0.15, z0 + 0.33))
    bx("huagong2", (0.24, 1.15, 0.16), (x, y0 + ys * 0.34, z0 + 0.62))
    # 交互斗
    for dy, dz in ((0.36, 0.48), (0.62, 0.77)):
        bx("jiaohudou", (0.26, 0.26, 0.10), (x, y0 + ys * dy, z0 + dz))
    # 下昂：斜杆（琴面以端部斜切近似），昂尖出檐下、昂身醒目
    ang_len = EAVE * 1.05
    pitch = math.radians(23)
    cy = y0 + ys * ang_len * 0.34
    cz = z0 + BRACKET_H * 0.50 - math.sin(pitch) * ang_len * 0.20
    bx("xiaang", (0.17, ang_len, 0.12), (x, cy, cz), m_timber_dark(),
       rot=(ys * pitch, 0, 0))
    # 令拱（面阔向）与散斗
    bx("linggong", (0.95, 0.20, 0.15), (x, y0 + ys * 0.34, z0 + 0.90))
    for dx in (-0.38, 0.38):
        bx("sandou", (0.22, 0.22, 0.10), (x + dx, y0 + ys * 0.34, z0 + 1.02))


def build_brackets():
    xs = column_xs()
    pu_xs = xs + [(xs[i] + xs[i + 1]) / 2 for i in range(len(xs) - 1)]
    for x in pu_xs:
        for ys in (-1, 1):
            build_bracket_set(x, ys)
    # 拱眼壁（攒间白灰，退到攒身之后）
    for ys in (-1, 1):
        add_box("gongyan", (W + 0.3, 0.14, BRACKET_H * 0.58),
                (0, ys * (D / 2 - 0.16), COL_TOP + BRACKET_H * 0.42), m_wall(),
                bevel=0.0)
    # 通长素枋两道：把攒串成一层"铺作"，不再是漂浮积木
    for ys in (-1, 1):
        add_box("zhengxinfang", (W + 0.6, 0.22, 0.14),
                (0, ys * D / 2, COL_TOP + 0.50), m_timber())
        add_box("luohanfang", (W + 0.6, 0.20, 0.13),
                (0, ys * (D / 2 + 0.34), COL_TOP + 1.14), m_timber())
    # 檐檩
    for ys in (-1, 1):
        add_cylinder("purlin", 0.11, ROOF_W - 0.4,
                     (0, ys * (D / 2 + EAVE * 0.30), COL_TOP + BRACKET_H - 0.06),
                     m_timber(), rot=(0, math.pi / 2, 0))


def build_rafters():
    """檐椽：沿屋面最下段坡度，自铺作层内侧伸出檐口之下。"""
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
    """一坡望板：举折凹曲网格（ys=-1 前坡 / +1 后坡）。"""
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
    ob = mesh_from_pydata(f"roof_deck_{'b' if ys>0 else 'f'}", verts, faces,
                          m_tile(), smooth=True)
    mod = ob.modifiers.new("sol", "SOLIDIFY")
    mod.thickness = 0.10
    return ob


def tile_run_curve(ys, x):
    """一垄筒瓦：沿举折剖面的圆管曲线。"""
    cu = bpy.data.curves.new("tile_run", "CURVE")
    cu.dimensions = "3D"
    cu.bevel_depth = 0.072
    cu.bevel_resolution = 3
    sp = cu.splines.new("POLY")
    sp.points.add(PROFILE_STEPS)
    for j in range(PROFILE_STEPS + 1):
        # 自 t=0.05 起，脊端留给正脊压盖，垄头不穿脊
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
        # 筒瓦垄（更密更细，读出真瓦面）
        step = 0.44
        n = int((ROOF_W - 0.5) / step)
        for i in range(n + 1):
            x = -(ROOF_W - 0.5) / 2 + (ROOF_W - 0.5) * i / n
            tile_run_curve(ys, x)
            # 瓦当（檐口圆盘）
            y_edge, z_edge = roof_profile(1.0)
            pitch = math.atan2(
                roof_profile(0.85)[1] - z_edge, (1 - 0.85) * HALF_SPAN)
            add_cylinder("wadang", 0.075, 0.05,
                         (x, ys * (y_edge + 0.12), EAVE_Y + 0.085),
                         m_ridge(), verts=16,
                         rot=(ys * (math.pi / 2 - pitch), 0, 0))

    # 正脊：两层叠瓦 + 顶部圆脊筒，端部止于鸱尾之内
    ridge_len = (ROOF_W / 2 - 0.55) * 2 - 0.15
    add_box("ridge0", (ridge_len, 0.52, 0.15),
            (0, 0, EAVE_Y + RISE + 0.10), m_ridge(), bevel=0.02)
    add_box("ridge1", (ridge_len, 0.40, 0.14),
            (0, 0, EAVE_Y + RISE + 0.24), m_ridge(), bevel=0.02)
    add_cylinder("ridge_cap", 0.16, ridge_len,
                 (0, 0, EAVE_Y + RISE + 0.36), m_ridge(),
                 verts=20, rot=(0, math.pi / 2, 0))
    build_bofeng()
    build_chiwei(-1)
    build_chiwei(1)


def build_bofeng():
    """博风板：悬山山面沿坡缘的护板（跟随举折曲线的短板带）。"""
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
    """鸱尾轮廓（与 C# ChiweiProfile 同构）：卷尾鳍，无兽头。XY→(x,z)。"""
    w = h * 0.52

    def bez(a, b, c, t):
        u = 1 - t
        return u * u * a + 2 * u * t * b + t * t * c

    pts = [(w * 0.95, 0.0), (w * 0.95, h * 0.14)]
    steps = 9
    for i in range(1, steps + 1):
        t = i / steps
        pts.append((bez(w * 0.95, w * 1.02, w * 0.34, t),
                    bez(h * 0.14, h * 0.72, h, t)))
    for i in range(1, steps + 1):
        t = i / steps
        pts.append((bez(w * 0.34, w * 0.10, w * 0.22, t),
                    bez(h, h * 0.97, h * 0.80, t)))
    pts += [(w * 0.16, h * 0.55), (0.0, h * 0.10), (0.0, 0.0)]
    return pts


def build_chiwei(xs):
    h = CH * 0.55
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
    base_x = xs * (ROOF_W / 2 - 0.55)
    base_z = EAVE_Y + RISE + 0.06
    ob = mesh_from_pydata("chiwei", verts, faces, m_ridge())
    ob.location = (base_x, 0, base_z)
    # 肋纹：三条沿外缘走向的斜棱，贴在轮廓面上（不是浮块）
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


def build_walls_and_openings():
    wall_t = 0.26
    wall_h = CH - 0.1
    zc = BASE_TOP + wall_h / 2
    # 后墙
    add_box("wall_back", (W - 0.3, wall_t, wall_h), (0, D / 2 - 0.22, zc), m_wall(), 0.0)
    # 山墙：一直封到檐口标高（连铺作层侧面一起），山尖再封到脊下
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
    # 前墙两段 + 直棂窗
    door_w, door_h = 1.7, 2.3
    seg_w = (W - 0.3 - door_w) / 2
    for xsn in (-1, 1):
        cx = xsn * (door_w / 2 + seg_w / 2)
        add_box("wall_front", (seg_w, wall_t, wall_h),
                (cx, -D / 2 + 0.22, zc), m_wall(), 0.0)
        build_lattice_window(cx, -D / 2 + 0.22 - 0.16, BASE_TOP + CH * 0.55)
    add_box("wall_overdoor", (door_w + 0.2, wall_t, wall_h - door_h),
            (0, -D / 2 + 0.22, BASE_TOP + door_h + (wall_h - door_h) / 2), m_wall(), 0.0)
    # 板门两扇 + 门钉
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


def build_ground():
    bpy.ops.mesh.primitive_plane_add(size=420, location=(0, 0, 0))
    ob = bpy.context.object
    ob.name = "ground"
    ob.data.materials.append(m_ground())


# ---------------- 环境 / 相机 / 渲染 ----------------

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

    # 曝光与视图变换：Filmic 保色相，压回被 AgX 洗白的问题
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
    scene.cycles.use_denoising = False  # apt 版 Blender 未编 OIDN，靠采样压噪
    scene.render.resolution_x = w
    scene.render.resolution_y = h
    scene.render.filepath = path
    bpy.ops.render.render(write_still=True)
    print("[tang_hall] 渲染:", path)


def main():
    out_dir = "artifacts/blender-preview"
    if "--" in sys.argv:
        args = sys.argv[sys.argv.index("--") + 1:]
        for i, a in enumerate(args):
            if a == "--out" and i + 1 < len(args):
                out_dir = args[i + 1]
    os.makedirs(out_dir, exist_ok=True)

    # 清场
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete()

    build_ground()
    build_plinth()
    build_columns()
    build_lintels()
    build_brackets()
    build_rafters()
    build_roof()
    build_walls_and_openings()
    setup_world()

    cam1 = add_camera("cam_front", (15.5, -18.5, 7.0), (0, 0.4, 3.6), fov=36)
    cam2 = add_camera("cam_eave", (7.6, -9.2, 4.4), (2.0, -2.4, 5.4), fov=28)
    cam3 = add_camera("cam_ridge", (-11.5, -10.0, 9.6), (-3.6, 0.4, 6.2), fov=30)

    render(cam1, os.path.join(out_dir, "hall_front.png"))
    render(cam2, os.path.join(out_dir, "hall_eave.png"))
    render(cam3, os.path.join(out_dir, "hall_ridge.png"))

    # 落一份 blend 供后续导出 FBX/glTF
    bpy.ops.wm.save_as_mainfile(filepath=os.path.join(out_dir, "tang_hall.blend"))


main()
