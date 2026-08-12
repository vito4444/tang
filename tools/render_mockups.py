#!/usr/bin/env python3
"""界面设计稿渲染器 + 画面指标输出。

一条命令：python3 tools/render_mockups.py
输出：artifacts/screenshots/*.png 与 metrics.json，终端打印每屏明度分布与饱和度。

定位（诚实声明）：这不是引擎内截图。文案、术语、色板、字体、布局比例与游戏同源
（strings.json / glossary.json / InkPalette / 屏幕布局参数 / 霞鹜文楷 TTF），
但光栅化走 FreeType，与 Unity TMP 的 SDF 渲染存在字形微差。
引擎内截图在 Unity 许可就位后由同一套指标口径复核。
"""
import colorsys
import json
import math
import os
import random
import sys

import numpy as np
from PIL import Image, ImageDraw, ImageFilter, ImageFont

REPO = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
DATA = os.path.join(REPO, "LingyanUnity", "Assets", "Resources", "Data")
FONT_PATH = os.path.join(REPO, "LingyanUnity", "Assets", "Resources", "Fonts",
                         "LXGWWenKai-Regular.ttf")
OUT_DIR = os.path.join(REPO, "artifacts", "screenshots")

W, H = 1920, 1080

# 与 Assets/Scripts/Game/UI/InkPalette.cs 保持同值（改这里必须同步 C#）
PALETTE = {
    "void":       (0x14, 0x12, 0x0F),
    "void_top":   (0x22, 0x1D, 0x16),
    "paper":      (0xD9, 0xCF, 0xBA),
    "faint":      (0x8F, 0x84, 0x6D),
    "seal":       (0xB0, 0x44, 0x2F),
    "panel":      (0xE8, 0xDE, 0xC7),   # 5% alpha 使用
    "disabled":   (0x5C, 0x55, 0x4A),
    "good":       (0x7A, 0x9E, 0x71),
    "bad":        (0xA8, 0x52, 0x4A),
    "seal_paper": (0xED, 0xE4, 0xD2),
}

ROBE_HEX = {"white": "E8E2D5"}


def load_strings():
    with open(os.path.join(DATA, "strings.json"), encoding="utf-8") as fh:
        return json.load(fh)


STR = load_strings()


def tr(key, locale="zh"):
    entry = STR.get(key)
    if not entry:
        return "\u27e6" + key + "\u27e7"
    return entry["zh" if locale == "zh" else "en"]


_font_cache = {}


def font(px):
    if px not in _font_cache:
        _font_cache[px] = ImageFont.truetype(FONT_PATH, px)
    return _font_cache[px]


# ---------------- 背景：水墨渐变 + 远山淡影 + 纸纹 + 晕影 ----------------

def _ridge_line(width, segments, amp, seed):
    """中点位移 + 汉宁窗平滑：圆缓的水墨山脊，不出针叶林锯齿。"""
    rng = random.Random(seed)
    pts = [0.5, 0.5]
    while len(pts) - 1 < segments:
        nxt = [pts[0]]
        scale = amp / len(pts)
        for i in range(len(pts) - 1):
            mid = (pts[i] + pts[i + 1]) / 2 + rng.uniform(-scale, scale) * 3
            nxt.extend([mid, pts[i + 1]])
        pts = nxt
    xs = np.linspace(0, width - 1, len(pts))
    y = np.interp(np.arange(width), xs, np.array(pts))
    kernel = np.hanning(61)
    kernel /= kernel.sum()
    return np.convolve(np.pad(y, 30, mode="edge"), kernel, mode="valid")[:width]


def make_background(seed=7):
    top = np.array(PALETTE["void_top"], dtype=np.float64)
    bottom = np.array(PALETTE["void"], dtype=np.float64)

    t = (np.arange(H) / (H - 1))[:, None]
    # 顶部略亮、往下沉：模拟宿墨在纸上的洇散
    grad = top[None, None, :] * (1 - t)[..., None] + bottom[None, None, :] * t[..., None]
    img = np.broadcast_to(grad, (H, W, 3)).copy()

    # 三层远山淡影（极低对比，只做氛围，不抢 UI）
    layers = [
        (0.680, 20, +6.0, 11),
        (0.765, 30, +4.0, 23),
        (0.845, 44, +2.2, 37),
    ]
    yy = np.arange(H)[:, None]
    for base, rough, delta, seed_i in layers:
        ridge = _ridge_line(W, 128, 1.0, seed * 100 + seed_i)
        ridge_y = (base + (ridge - 0.5) * (rough / 100.0)) * H
        # 脊线以下 24px 内渐入（墨晕软边），山体向下缓缓沉没
        rise = np.clip((yy - ridge_y[None, :]) / 24.0, 0, 1)
        fall = np.clip((yy - ridge_y[None, :]) / (H * 0.55), 0, 1)
        strength = rise * (1.0 - fall * 0.85)
        img += strength[..., None] * delta

    # 纸纹颗粒
    rng = np.random.default_rng(seed)
    grain = rng.normal(0, 2.2, (H, W, 1))
    img += grain

    # 晕影
    xx = np.arange(W)[None, :]
    cx, cy = W / 2, H * 0.46
    dist = np.sqrt(((xx - cx) / (W * 0.72)) ** 2 + ((yy - cy) / (H * 0.72)) ** 2)
    img *= (1.0 - np.clip(dist - 0.55, 0, 1) * 0.22)[..., None]

    return Image.fromarray(np.clip(img, 0, 255).astype(np.uint8), "RGB")


_BG = None


def background():
    global _BG
    if _BG is None:
        _BG = make_background()
    return _BG.copy()


# ---------------- 基础绘制 ----------------

def rgba(name, alpha=255):
    r, g, b = PALETTE[name]
    return (r, g, b, alpha)


def panel(draw, box, fill_alpha=13, border_alpha=64):
    x0, y0, x1, y1 = box
    draw.rectangle(box, fill=rgba("panel", fill_alpha))
    draw.rectangle(box, outline=rgba("faint", border_alpha), width=1)
    # 内衬细线（唐卷轴裱边的克制暗示）
    draw.rectangle((x0 + 6, y0 + 6, x1 - 6, y1 - 6),
                   outline=rgba("faint", 26), width=1)


def hairline(draw, x0, x1, y, alpha=70):
    draw.line((x0, y, x1, y), fill=rgba("faint", alpha), width=1)
    mid = (x0 + x1) // 2
    draw.polygon((mid - 4, y, mid, y - 3, mid + 4, y, mid, y + 3),
                 fill=rgba("faint", alpha + 30))


def text(draw, xy, s, px, color, anchor="la", alpha=255, spacing_px=0):
    if isinstance(color, str):
        col = rgba(color, alpha)
    else:
        col = color
    if spacing_px and len(s) > 1:
        # 手动字距（题字用）
        x, y = xy
        widths = []
        f = font(px)
        for ch in s:
            bb = draw.textbbox((0, 0), ch, font=f)
            widths.append(bb[2] - bb[0])
        total = sum(widths) + spacing_px * (len(s) - 1)
        if anchor[0] == "m":
            x -= total / 2
        for ch, w_ch in zip(s, widths):
            draw.text((x, y), ch, font=f, fill=col, anchor="l" + anchor[1])
            x += w_ch + spacing_px
        return
    draw.text(xy, s, font=font(px), fill=col, anchor=anchor)


def wrap_cjk(draw, s, px, max_width):
    f = font(px)
    lines, cur = [], ""
    for ch in s:
        probe = cur + ch
        if draw.textlength(probe, font=f) > max_width and cur:
            lines.append(cur)
            cur = ch
        else:
            cur = probe
    if cur:
        lines.append(cur)
    return lines


def seal_stamp(img, cx, cy, size=88, rotation=-4.0):
    """朱砂印：微旋转、印泥孔隙、白文。"""
    pad = 20
    layer = Image.new("RGBA", (size + pad * 2, size + pad * 2), (0, 0, 0, 0))
    d = ImageDraw.Draw(layer)
    d.rounded_rectangle((pad, pad, pad + size, pad + size), radius=7,
                        fill=rgba("seal", 235))
    d.rounded_rectangle((pad + 4, pad + 4, pad + size - 4, pad + size - 4),
                        radius=5, outline=rgba("seal_paper", 90), width=1)
    fs = int(size * 0.34)
    d.text((pad + size / 2, pad + size * 0.31), "凌", font=font(fs),
           fill=rgba("seal_paper", 240), anchor="mm")
    d.text((pad + size / 2, pad + size * 0.71), "烟", font=font(fs),
           fill=rgba("seal_paper", 240), anchor="mm")

    # 印泥孔隙：随机蚀去小点
    rng = np.random.default_rng(42)
    arr = np.array(layer)
    holes = rng.random((arr.shape[0], arr.shape[1])) > 0.985
    holes &= arr[..., 3] > 0
    arr[holes, 3] = (arr[holes, 3] * 0.35).astype(np.uint8)
    layer = Image.fromarray(arr)

    layer = layer.rotate(rotation, resample=Image.BICUBIC, expand=True)
    pos = (int(cx - layer.width / 2), int(cy - layer.height / 2))
    img.paste(layer, pos, layer)


def button(draw, cx, cy, label, px=30, state="normal", brackets=False):
    color = {"normal": "paper", "hover": "seal",
             "disabled": "disabled"}.get(state, "paper")
    s = f"「 {label} 」" if brackets else label
    text(draw, (cx, cy), s, px, color, anchor="mm")


def row(draw, x_label, x_value, y, label, value, px=26,
        label_color="faint", value_color="paper"):
    text(draw, (x_label, y), label, px, label_color, anchor="lm")
    text(draw, (x_value, y), value, px + 2, value_color, anchor="lm")


def section(draw, cx, y, title, px=30):
    text(draw, (cx, y), title, px, "seal", anchor="mm")
    hairline(draw, cx - 210, cx + 210, y + 26, alpha=44)


def footer(draw, locale):
    text(draw, (W * 0.12, H * 0.945), tr("menu.language", locale), 24,
         "faint", anchor="mm")
    text(draw, (W * 0.5, H * 0.945),
         tr("game.working_title_note", locale) + " · v0.1.0", 22,
         "disabled", anchor="mm")
    text(draw, (W * 0.87, H * 0.945), tr("footer.font_credit", locale), 22,
         "disabled", anchor="mm")


def new_canvas():
    # RGB 底 + RGBA 画笔：半透明填充走 alpha 混合而非直接覆盖
    img = background()
    return img, ImageDraw.Draw(img, "RGBA")


# ---------------- 各屏 ----------------

def render_menu(locale="zh"):
    img, draw = new_canvas()

    ty = H * 0.265
    if locale == "zh":
        text(draw, (W / 2, ty), tr("game.title", locale), 168, "paper",
             anchor="mm", spacing_px=46)
    else:
        text(draw, (W / 2, ty), tr("game.title", locale), 128, "paper",
             anchor="mm", spacing_px=26)
    hairline(draw, W / 2 - 330, W / 2 + 330, ty + 118, alpha=80)
    text(draw, (W / 2, ty + 160), tr("game.subtitle", locale), 27, "faint",
         anchor="mm")
    # 钤印：中文贴题字右侧压角，英文题字长、挪到末端之外
    seal_stamp(img, W / 2 + (315 if locale == "zh" else 585), ty - 40)
    draw = ImageDraw.Draw(img, "RGBA")

    y = H * 0.575
    step = H * 0.072
    button(draw, W / 2, y, tr("menu.new", locale), 33, "hover", brackets=True)
    button(draw, W / 2, y + step, tr("menu.continue", locale), 33, "disabled")
    text(draw, (W / 2, y + step + 34), tr("menu.no_save_hint", locale), 22,
         "disabled", anchor="mm")
    button(draw, W / 2, y + step * 2 + 26, tr("menu.settings", locale), 33)
    button(draw, W / 2, y + step * 3 + 26, tr("menu.quit", locale), 33)

    footer(draw, locale)
    return img


def render_creation(locale="zh"):
    img, draw = new_canvas()
    text(draw, (W / 2, H * 0.055), tr("creation.title", locale), 44, "paper",
         anchor="mm", spacing_px=10)
    hairline(draw, W / 2 - 260, W / 2 + 260, H * 0.055 + 46, alpha=60)

    # 左：五主角
    lx0, ly0, lx1, ly1 = W * 0.035, H * 0.115, W * 0.300, H * 0.875
    panel(draw, (lx0, ly0, lx1, ly1))
    origins = [
        ("mingjing", False), ("baishen", True), ("shuzu", False),
        ("nvguan", False), ("hushang", False),
    ]
    oy = ly0 + 64
    for key, selected in origins:
        marker = "◉ " if selected else "○ "
        col = "seal" if selected else "paper"
        text(draw, ((lx0 + lx1) / 2, oy), marker + tr(f"protagonist.{key}.title", locale),
             30, col, anchor="mm")
        role_lines = wrap_cjk(draw, tr(f"protagonist.{key}.role", locale), 22,
                              (lx1 - lx0) - 80)
        ry = oy + 36
        for line in role_lines[:3]:
            text(draw, ((lx0 + lx1) / 2, ry), line, 22,
                 "paper" if selected else "faint", anchor="mm",
                 alpha=255 if selected else 205)
            ry += 27
        oy += 158

    # 右：详情（白身）
    rx0, ry0, rx1, ry1 = W * 0.330, H * 0.115, W * 0.965, H * 0.875
    panel(draw, (rx0, ry0, rx1, ry1))
    pad = 46
    cy = ry0 + 62

    text(draw, (rx0 + pad, cy), tr("creation.name_label", locale), 26, "faint",
         anchor="lm")
    nb = (rx0 + pad + 110, cy - 26, rx0 + pad + 420, cy + 26)
    draw.rectangle(nb, fill=rgba("panel", 24))
    draw.rectangle(nb, outline=rgba("faint", 70), width=1)
    text(draw, (nb[0] + 16, cy), "柳七", 28, "paper", anchor="lm")
    draw.line((nb[0] + 16 + draw.textlength("柳七", font=font(28)) + 4, cy - 16,
               nb[0] + 16 + draw.textlength("柳七", font=font(28)) + 4, cy + 16),
              fill=rgba("seal", 200), width=2)

    cy += 66
    for line in wrap_cjk(draw, tr("protagonist.baishen.blurb", locale), 25,
                         (rx1 - rx0) - pad * 2):
        text(draw, (rx0 + pad, cy), line, 25, "paper", anchor="lm", alpha=235)
        cy += 36

    # 四维（分配中：体6 命4 力4 智10，余 2）
    cy += 26
    values = {"stamina": 6, "health": 4, "strength": 4, "wisdom": 10}
    for key, val in values.items():
        text(draw, (rx0 + pad, cy), tr(f"attr.{key}", locale), 28, "paper",
             anchor="lm")
        text(draw, (rx0 + pad + 240, cy), str(val), 32, "paper", anchor="mm")
        text(draw, (rx0 + pad + 330, cy), "−", 34, "paper", anchor="mm")
        text(draw, (rx0 + pad + 410, cy), "+", 34,
             "seal" if key == "wisdom" else "paper", anchor="mm")
        # 数值条（十二格，双编码：格数 + 实心/空心）
        bx = rx0 + pad + 480
        for i in range(12):
            x = bx + i * 24
            box = (x, cy - 9, x + 14, cy + 9)
            if i < val:
                draw.rectangle(box, fill=rgba("paper", 190))
            else:
                draw.rectangle(box, outline=rgba("faint", 90), width=1)
        cy += 58

    text(draw, (rx0 + pad, cy + 4),
         tr("creation.points_remaining", locale).format(2), 27, "seal",
         anchor="lm")

    # 入仕之途
    cy += 62
    text(draw, (rx0 + pad, cy), tr("creation.entry_path", locale), 28, "paper",
         anchor="lm")
    cy += 46
    entries = [("keju_mingjing", False), ("keju_jinshi", True), ("toujun", False)]
    for key, selected in entries:
        marker = "◉ " if selected else "○ "
        text(draw, (rx0 + pad + 18, cy),
             marker + tr(f"creation.entry.{key}", locale), 24,
             "seal" if selected else "paper", anchor="lm",
             alpha=255 if selected else 215)
        cy += 40

    # 右列：起点信息
    ix = rx0 + (rx1 - rx0) * 0.72
    iy = ry0 + 330
    for label_key, value in [
        ("creation.start_money", "3 贯" if locale == "zh" else "3 guan"),
        ("creation.start_status", tr("status.commoner", locale)),
        ("creation.career_line", tr("line.undecided", locale)),
    ]:
        text(draw, (ix, iy), tr(label_key, locale), 24, "faint", anchor="lm")
        for i, line in enumerate(wrap_cjk(draw, value, 26, (rx1 - ix) - 60)[:2]):
            text(draw, (ix, iy + 34 + i * 30), line, 26, "paper", anchor="lm")
        iy += 104

    button(draw, W * 0.09, H * 0.928, tr("creation.back", locale), 30)
    button(draw, W * 0.905, H * 0.928, tr("creation.confirm", locale), 32,
           "hover", brackets=True)
    return img


def render_study(locale="zh"):
    img, draw = new_canvas()
    text(draw, (W / 2, H * 0.058), tr("study.title", locale), 46, "paper",
         anchor="mm", spacing_px=12)
    hairline(draw, W / 2 - 240, W / 2 + 240, H * 0.058 + 40, alpha=60)
    who = "沈知白 · " + tr("protagonist.mingjing.title", locale) + " · " \
        + tr("status.liuwai_clerk", locale)
    if locale != "zh":
        who = "Shen Zhibai · " + tr("protagonist.mingjing.title", locale)
    text(draw, (W / 2, H * 0.121), who, 26, "faint", anchor="mm")

    # 左面板：四维 + 名声
    lx0, ly0, lx1, ly1 = W * 0.060, H * 0.165, W * 0.470, H * 0.815
    panel(draw, (lx0, ly0, lx1, ly1))
    cx = (lx0 + lx1) / 2
    y = ly0 + 66
    section(draw, cx, y, "四维" if locale == "zh" else "Attributes")
    y += 72
    attr_vals = [("stamina", 6), ("health", 6), ("strength", 4), ("wisdom", 12)]
    for key, val in attr_vals:
        row(draw, lx0 + 70, lx0 + 330, y, tr(f"attr.{key}", locale), str(val), 27)
        bx = lx0 + 420
        for i in range(12):
            x = bx + i * 24
            box = (x, y - 8, x + 15, y + 8)
            if i < val:
                draw.rectangle(box, fill=rgba("paper", 185))
            else:
                draw.rectangle(box, outline=rgba("faint", 80), width=1)
        y += 62

    y += 30
    section(draw, cx, y, tr("study.reputation", locale))
    y += 72
    reps = [("rep.guansheng", 15), ("rep.minwang", 20), ("rep.jianghu", 5)]
    for key, val in reps:
        row(draw, lx0 + 70, lx0 + 330, y, tr(key, locale), str(val), 27)
        bar_w = 300
        draw.rectangle((lx0 + 420, y - 6, lx0 + 420 + bar_w, y + 6),
                       outline=rgba("faint", 80), width=1)
        draw.rectangle((lx0 + 420, y - 6, lx0 + 420 + bar_w * val / 100, y + 6),
                       fill=rgba("seal", 170))
        y += 62

    # 右面板：四轨 + 服色 + 钱 + 时日
    rx0, ry0, rx1, ry1 = W * 0.510, H * 0.165, W * 0.940, H * 0.815
    panel(draw, (rx0, ry0, rx1, ry1))
    cx2 = (rx0 + rx1) / 2
    y = ry0 + 66
    section(draw, cx2, y, tr("study.four_tracks", locale))
    y += 72
    none = tr("study.none", locale)
    for key, val in [("track.zhishi", none), ("track.sanguan", none),
                     ("track.xunguan", tr("study.no_xun", locale)),
                     ("track.jue", none)]:
        row(draw, rx0 + 70, rx0 + 300, y, tr(key, locale), val, 27,
            value_color="disabled")
        y += 60

    y += 26
    text(draw, (rx0 + 70, y), tr("study.robe", locale), 26, "faint", anchor="lm")
    r, g, b = tuple(int(ROBE_HEX["white"][i:i + 2], 16) for i in (0, 2, 4))
    draw.rectangle((rx0 + 300, y - 19, rx0 + 338, y + 19), fill=(r, g, b, 255))
    draw.rectangle((rx0 + 300, y - 19, rx0 + 338, y + 19),
                   outline=rgba("faint", 120), width=1)
    text(draw, (rx0 + 366, y), tr("robe.white", locale), 28, "paper", anchor="lm")

    y += 74
    row(draw, rx0 + 70, rx0 + 300, y, tr("study.money", locale),
        "6 贯" if locale == "zh" else "6 guan", 27)
    y += 60
    row(draw, rx0 + 70, rx0 + 300, y, tr("study.date", locale),
        "垂拱四年 三月十七 · 巳时" if locale == "zh"
        else "Chuigong 4 · Month 3, Day 17 · Hour of Si (9–11 a.m.)", 27)
    y += 60
    row(draw, rx0 + 70, rx0 + 300, y, tr("study.solar_term", locale),
        "谷雨" if locale == "zh" else "Grain Rain", 27)

    button(draw, W * 0.30, H * 0.917, tr("study.save_and_menu", locale), 30)
    button(draw, W * 0.70, H * 0.917, tr("study.chapter_locked", locale), 30,
           "disabled")
    text(draw, (W * 0.94, H * 0.058), tr("menu.settings", locale), 26, "faint",
         anchor="mm")
    return img


def render_settings(locale="zh"):
    img, draw = new_canvas()
    text(draw, (W / 2, H * 0.10), tr("settings.title", locale), 46, "paper",
         anchor="mm", spacing_px=12)
    hairline(draw, W / 2 - 240, W / 2 + 240, H * 0.10 + 40, alpha=60)

    x0, y0, x1, y1 = W * 0.24, H * 0.20, W * 0.76, H * 0.74
    panel(draw, (x0, y0, x1, y1))

    y = y0 + 96
    text(draw, (x0 + 90, y), tr("settings.language", locale), 30, "faint",
         anchor="lm")
    text(draw, (x0 + 420, y), "◉ " + tr("settings.language.zh", locale), 28,
         "seal" if locale == "zh" else "paper", anchor="lm")
    text(draw, (x0 + 730, y), "○ " + tr("settings.language.en", locale), 28,
         "paper" if locale == "zh" else "seal", anchor="lm")

    y += 118
    text(draw, (x0 + 90, y), tr("settings.font_scale", locale), 30, "faint",
         anchor="lm")
    text(draw, (x0 + 430, y), "−", 36, "disabled", anchor="mm")
    text(draw, (x0 + 620, y), tr("settings.font_scale_value", locale)
         .format(100, 22), 28, "paper", anchor="mm")
    text(draw, (x0 + 810, y), "+", 36, "paper", anchor="mm")

    y += 118
    text(draw, ((x0 + x1) / 2, y), tr("settings.font_scale_note", locale), 24,
         "faint", anchor="mm")
    y += 56
    text(draw, ((x0 + x1) / 2, y), "✓ " + tr("settings.colorblind_note", locale),
         24, "good", anchor="mm")

    button(draw, W / 2, y1 - 64, tr("settings.back", locale), 30, "hover",
           brackets=True)
    return img


# ---------------- 指标（规格第十四节：可比较的数字） ----------------

def metrics(img):
    arr = np.asarray(img.convert("RGB"), dtype=np.float64) / 255.0
    mx = arr.max(axis=2)
    mn = arr.min(axis=2)
    v = mx
    s = np.where(mx > 0, (mx - mn) / np.maximum(mx, 1e-9), 0)
    return {
        "value_p5": round(float(np.percentile(v, 5)), 4),
        "value_median": round(float(np.percentile(v, 50)), 4),
        "value_p95": round(float(np.percentile(v, 95)), 4),
        "value_mean": round(float(v.mean()), 4),
        "saturation_mean": round(float(s.mean()), 4),
        "saturation_p95": round(float(np.percentile(s, 95)), 4),
    }


def main():
    os.makedirs(OUT_DIR, exist_ok=True)
    jobs = [
        ("menu_zh.png", lambda: render_menu("zh")),
        ("menu_en.png", lambda: render_menu("en")),
        ("creation_zh.png", lambda: render_creation("zh")),
        ("study_zh.png", lambda: render_study("zh")),
        ("settings_zh.png", lambda: render_settings("zh")),
    ]
    all_metrics = {}
    for name, fn in jobs:
        img = fn().convert("RGB")
        path = os.path.join(OUT_DIR, name)
        img.save(path, optimize=True)
        m = metrics(img)
        all_metrics[name] = m
        print(f"{name:18s} 明度 P5/中位/P95 = {m['value_p5']:.3f}/"
              f"{m['value_median']:.3f}/{m['value_p95']:.3f}  "
              f"饱和度均值 = {m['saturation_mean']:.3f}")
    with open(os.path.join(OUT_DIR, "metrics.json"), "w", encoding="utf-8") as fh:
        json.dump(all_metrics, fh, ensure_ascii=False, indent=2)
    print(f"输出目录: {OUT_DIR}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
