#!/usr/bin/env python3
"""程序化平铺贴图（写实化地表/坊墙）：周期值噪声 FBM，四方连续无缝。

用法:
    python3 tools/gen_textures.py LingyanUnity/Assets/Resources/Textures

输出（1024²，PNG）:
    ground_dirt.png   坊内夯土地面
    lane_dirt.png     踩实土便道（更浅、更匀）
    plaster.png       灰泥墙面（暖白带污渍）
    earth_wall.png    坊墙夯土（水平夯层）
"""
import os
import sys

import numpy as np


def _lattice(seed, period):
    rng = np.random.default_rng(seed)
    return rng.random((period, period))


def periodic_value_noise(size, period, seed):
    """周期格点值噪声：格点取模 → 四方连续。双三次插值近似（smoothstep）。"""
    grid = _lattice(seed, period)
    xs = np.linspace(0, period, size, endpoint=False)
    x0 = np.floor(xs).astype(int) % period
    x1 = (x0 + 1) % period
    tx = xs - np.floor(xs)
    tx = tx * tx * (3 - 2 * tx)

    g00 = grid[np.ix_(x0, x0)]
    g10 = grid[np.ix_(x1, x0)]
    g01 = grid[np.ix_(x0, x1)]
    g11 = grid[np.ix_(x1, x1)]
    a = g00 + (g10 - g00) * tx[:, None]
    b = g01 + (g11 - g01) * tx[:, None]
    return a + (b - a) * tx[None, :]


def fbm(size, base_period, octaves, seed, gain=0.5):
    total = np.zeros((size, size))
    amp = 1.0
    norm = 0.0
    for o in range(octaves):
        total += amp * periodic_value_noise(size, base_period * (2 ** o), seed + o)
        norm += amp
        amp *= gain
    return total / norm


def to_png(path, rgb, alpha=None):
    """无依赖 PNG 写出（灰度/RGB/RGBA，8bit）。"""
    import struct
    import zlib

    h, w = rgb.shape[:2]
    img = np.clip(rgb * 255.0 + 0.5, 0, 255).astype(np.uint8)
    if alpha is not None:
        a = np.clip(alpha * 255.0 + 0.5, 0, 255).astype(np.uint8)
        img = np.dstack([img, a])
        color_type = 6
    else:
        color_type = 2
    raw = b"".join(b"\x00" + img[y].tobytes() for y in range(h))

    def chunk(tag, data):
        c = tag + data
        return struct.pack(">I", len(data)) + c + struct.pack(">I", zlib.crc32(c))

    png = b"\x89PNG\r\n\x1a\n"
    png += chunk(b"IHDR", struct.pack(">IIBBBBB", w, h, 8, color_type, 0, 0, 0))
    png += chunk(b"IDAT", zlib.compress(raw, 9))
    png += chunk(b"IEND", b"")
    with open(path, "wb") as f:
        f.write(png)
    print("[gen_textures]", path)


def colorize(noise, dark, light):
    dark = np.asarray(dark)
    light = np.asarray(light)
    return dark[None, None, :] + (light - dark)[None, None, :] * noise[:, :, None]


def ground_dirt(size):
    big = fbm(size, 4, 5, seed=11)
    fine = fbm(size, 64, 3, seed=13)
    mix = np.clip(big * 0.72 + fine * 0.28, 0, 1)
    rgb = colorize(mix, (0.360, 0.305, 0.228), (0.500, 0.440, 0.340))
    # 零星石粒提亮
    speck = fbm(size, 128, 2, seed=17)
    rgb += ((speck > 0.78) * 0.05)[:, :, None]
    return np.clip(rgb, 0, 1)


def lane_dirt(size):
    big = fbm(size, 6, 4, seed=23)
    fine = fbm(size, 96, 2, seed=29)
    mix = np.clip(big * 0.6 + fine * 0.4, 0, 1)
    return np.clip(colorize(mix, (0.545, 0.488, 0.385), (0.660, 0.600, 0.480)), 0, 1)


def plaster(size):
    stain = fbm(size, 3, 5, seed=31)
    grain = fbm(size, 128, 2, seed=37)
    mix = np.clip(stain * 0.8 + grain * 0.2, 0, 1)
    rgb = colorize(mix, (0.665, 0.618, 0.520), (0.790, 0.750, 0.660))
    return np.clip(rgb, 0, 1)


def earth_wall(size):
    # 水平夯层：Y 向条带 + 噪声扰动
    y = np.linspace(0, 14 * 2 * np.pi, size)
    bands = 0.5 + 0.5 * np.sin(y)[:, None] * np.ones((1, size))
    wobble = fbm(size, 8, 4, seed=41)
    mix = np.clip(bands * 0.35 + wobble * 0.65, 0, 1)
    rgb = colorize(mix, (0.470, 0.385, 0.268), (0.590, 0.500, 0.368))
    return np.clip(rgb, 0, 1)


def foliage(size):
    # 槐树冠：高频叶簇噪声，深绿到黄绿——树冠球贴上即碎叶感
    fine = fbm(size, 96, 3, seed=53, gain=0.6)
    big = fbm(size, 8, 3, seed=59)
    mix = np.clip(fine * 0.62 + big * 0.38, 0, 1)
    rgb = colorize(mix, (0.130, 0.185, 0.085), (0.335, 0.430, 0.195))
    # 零星亮叶
    sparkle = fbm(size, 160, 2, seed=61)
    rgb += ((sparkle > 0.82) * 0.06)[:, :, None]
    return np.clip(rgb, 0, 1)


def bark(size):
    # 槐树皮：竖向沟壑（X 向条带扰动）+ 褐灰
    x = np.linspace(0, 26 * 2 * np.pi, size)
    ridges = 0.5 + 0.5 * np.sin(x)[None, :] * np.ones((size, 1))
    wobble = fbm(size, 12, 4, seed=67)
    mix = np.clip(ridges * 0.42 + wobble * 0.58, 0, 1)
    rgb = colorize(mix, (0.205, 0.165, 0.125), (0.360, 0.305, 0.240))
    return np.clip(rgb, 0, 1)


def main():
    out_dir = sys.argv[1] if len(sys.argv) > 1 else "LingyanUnity/Assets/Resources/Textures"
    os.makedirs(out_dir, exist_ok=True)
    size = 1024
    to_png(os.path.join(out_dir, "ground_dirt.png"), ground_dirt(size))
    to_png(os.path.join(out_dir, "lane_dirt.png"), lane_dirt(size))
    to_png(os.path.join(out_dir, "plaster.png"), plaster(size))
    to_png(os.path.join(out_dir, "earth_wall.png"), earth_wall(size))
    to_png(os.path.join(out_dir, "foliage.png"), foliage(size))
    to_png(os.path.join(out_dir, "bark.png"), bark(size))


if __name__ == "__main__":
    main()
