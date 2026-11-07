"""Math, color and image helpers for the Dark Brine_Rot teaser.

Everything here is pure numpy + Pillow so the renderer has no native
dependencies beyond the ffmpeg binary used by ``promo.encode``.
"""
from __future__ import annotations

import math
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw, ImageFilter, ImageFont

Color = tuple[int, int, int]

# --------------------------------------------------------------- scalars

def clamp(x, lo=0.0, hi=1.0):
    if isinstance(x, np.ndarray):
        return np.clip(x, lo, hi)
    return lo if x < lo else hi if x > hi else x


def lerp(a, b, t):
    return a + (b - a) * t


def inv_lerp(a, b, x):
    if b == a:
        return np.zeros_like(x, dtype=np.float32) if isinstance(x, np.ndarray) else 0.0
    return clamp((x - a) / (b - a))


def smoothstep(e0, e1, x):
    t = inv_lerp(e0, e1, x)
    return t * t * (3.0 - 2.0 * t)


def ease_in_quad(t):
    return t * t


def ease_out_quad(t):
    return 1.0 - (1.0 - t) ** 2


def ease_in_out(t):
    return 2 * t * t if t < 0.5 else 1.0 - (-2 * t + 2) ** 2 / 2.0


def ease_out_cubic(t):
    return 1.0 - (1.0 - t) ** 3


def ease_in_cubic(t):
    return t ** 3


def ease_out_expo(t):
    return 1.0 if t >= 1.0 else 1.0 - 2.0 ** (-10.0 * t)


def ease_in_expo(t):
    return 0.0 if t <= 0.0 else 2.0 ** (10.0 * t - 10.0)


def ease_out_back(t, s=1.9):
    t -= 1.0
    return t * t * ((s + 1.0) * t + s) + 1.0


def pulse(t, center, width):
    if width <= 0.0:
        return 0.0
    return math.exp(-((t - center) / width) ** 2)


# ---------------------------------------------------------------- colors

def mix(c1, c2, t):
    t = clamp(t)
    return tuple(int(round(a + (b - a) * t)) for a, b in zip(c1, c2))


def shade(c, k):
    return tuple(int(clamp(v * k, 0, 255)) for v in c)


def unit(c):
    return np.array(c, dtype=np.float32) / 255.0


# ----------------------------------------------------------------- noise

def value_noise(h, w, seed=0, cells=8):
    rng = np.random.default_rng(seed)
    gh = gw = max(2, int(cells))
    grid = rng.random((gh + 1, gw + 1), dtype=np.float32)
    img = Image.fromarray((grid * 255).astype(np.uint8), "L")
    img = img.resize((int(w), int(h)), Image.BICUBIC)
    return np.asarray(img, dtype=np.float32) / 255.0


def fbm(h, w, seed=0, octaves=4, cells=4, persistence=0.5, lacunarity=2.0):
    total = np.zeros((int(h), int(w)), dtype=np.float32)
    amp, norm, c = 1.0, 0.0, float(cells)
    for o in range(octaves):
        total += amp * value_noise(h, w, seed + o * 17, c)
        norm += amp
        amp *= persistence
        c *= lacunarity
    return total / max(1e-6, norm)


# ------------------------------------------------------------ conversion

def arr_to_img(a):
    return Image.fromarray(np.clip(a, 0, 255).astype(np.uint8), "RGB")


def img_to_arr(im):
    return np.asarray(im.convert("RGB"), dtype=np.float32)


def vertical_gradient(w, h, stops):
    """stops = [(0..1, (r,g,b)), ...] -> float32 (h, w, 3) in 0..255."""
    pos = np.array([p for p, _ in stops], dtype=np.float32)
    cols = np.array([c for _, c in stops], dtype=np.float32)
    ys = np.linspace(0.0, 1.0, int(h), dtype=np.float32)
    band = np.stack([np.interp(ys, pos, cols[:, i]) for i in range(3)], axis=1)
    return np.repeat(band[:, None, :], int(w), axis=1)


# ---------------------------------------------------------------- sprites
_SPRITES: dict = {}


def radial_sprite(diameter, color, falloff=2.2, aspect=1.0):
    """Soft additive light blob, cached by rounded size/colour."""
    d = max(8, int(round(diameter / 8.0)) * 8)
    key = (d, tuple(int(v) for v in color), round(float(falloff), 2), round(float(aspect), 2))
    cached = _SPRITES.get(key)
    if cached is not None:
        return cached

    y, x = np.mgrid[0:d, 0:d].astype(np.float32)
    cx = cy = (d - 1) / 2.0
    dx = (x - cx) / max(1.0, cx)
    dy = (y - cy) / max(1.0, cy) / max(1e-3, aspect)
    r = np.sqrt(dx * dx + dy * dy)
    a = np.clip(1.0 - r, 0.0, 1.0) ** falloff
    sprite = (a[..., None] * np.array(color, dtype=np.float32)).astype(np.float32)

    if len(_SPRITES) > 96:
        _SPRITES.clear()
    _SPRITES[key] = sprite
    return sprite


def add_sprite(arr, sprite, cx, cy, opacity=1.0):
    if opacity <= 0.001:
        return arr
    h, w = arr.shape[:2]
    sh, sw = sprite.shape[:2]
    x0 = int(round(cx - sw / 2.0))
    y0 = int(round(cy - sh / 2.0))
    ax0, ay0 = max(0, x0), max(0, y0)
    ax1, ay1 = min(w, x0 + sw), min(h, y0 + sh)
    if ax1 <= ax0 or ay1 <= ay0:
        return arr
    sx0, sy0 = ax0 - x0, ay0 - y0
    arr[ay0:ay1, ax0:ax1] += sprite[sy0:sy0 + (ay1 - ay0), sx0:sx0 + (ax1 - ax0)] * opacity
    return arr


def add_glow(arr, cx, cy, radius, color, strength=1.0, falloff=2.0):
    return add_sprite(arr, radial_sprite(radius * 2.0, color, falloff), cx, cy, strength)


# ------------------------------------------------------------ post tools

def vignette_mask(w, h, strength=0.6, power=2.0):
    y, x = np.mgrid[0:int(h), 0:int(w)].astype(np.float32)
    nx = (x - w / 2.0) / max(1.0, w / 2.0)
    ny = (y - h / 2.0) / max(1.0, h / 2.0)
    r = np.sqrt(nx * nx + ny * ny) / math.sqrt(2.0)
    v = 1.0 - strength * np.clip(r, 0.0, 1.0) ** power
    return np.clip(v, 0.0, 1.0)[..., None]


def grain_bank(count, w, h, seed=0, scale=2):
    rng = np.random.default_rng(seed)
    gw = max(2, int(w) // max(1, scale))
    gh = max(2, int(h) // max(1, scale))
    bank = []
    for i in range(int(count)):
        n = rng.normal(0.0, 1.0, (gh, gw)).astype(np.float32)
        g = np.clip(n * 0.5 + 0.5, 0.0, 1.0) * 255.0
        img = Image.fromarray(g.astype(np.uint8), "L").resize((int(w), int(h)), Image.BILINEAR)
        bank.append(np.asarray(img, dtype=np.float32))
    return bank


def chromatic(im, amount=2):
    amount = int(round(amount))
    if amount <= 0:
        return im
    a = img_to_arr(im)
    out = a.copy()
    out[..., 0] = np.roll(a[..., 0], -amount, axis=1)
    out[..., 2] = np.roll(a[..., 2], amount, axis=1)
    return arr_to_img(out)


def glow_image(im, radius=24, strength=1.0, threshold=140):
    a = img_to_arr(im)
    lum = a.max(axis=2)
    mask = np.clip((lum - threshold) / max(1.0, 255.0 - threshold), 0.0, 1.0)[..., None]
    bright = np.clip(a * mask, 0.0, 255.0).astype(np.uint8)
    blurred = Image.fromarray(bright, "RGB").filter(ImageFilter.GaussianBlur(radius))
    out = a + img_to_arr(blurred) * strength
    return arr_to_img(out)


# ----------------------------------------------------------------- fonts
_FONT_DIRS = [
    Path(r"C:\Windows\Fonts"),
    Path("/System/Library/Fonts"),
    Path("/System/Library/Fonts/Supplemental"),
    Path("/usr/share/fonts/truetype/dejavu"),
    Path("/usr/share/fonts/truetype/liberation"),
]

FONT_SETS = {
    "display": [
        "impact.ttf", "ariblk.ttf", "bahnschrift.ttf", "segoeuib.ttf",
        "arialbd.ttf", "DejaVuSans-Bold.ttf", "LiberationSans-Bold.ttf",
    ],
    "condensed": [
        "bahnschrift.ttf", "segoeuib.ttf", "arialbd.ttf",
        "DejaVuSans-Bold.ttf", "LiberationSans-Bold.ttf",
    ],
    "mono": [
        "consola.ttf", "courbd.ttf", "lucon.ttf",
        "DejaVuSansMono-Bold.ttf", "LiberationMono-Bold.ttf",
    ],
}

_FONT_CACHE: dict = {}


def font(size, kind="display"):
    size = max(6, int(round(size)))
    key = (size, kind)
    cached = _FONT_CACHE.get(key)
    if cached is not None:
        return cached

    chosen = None
    for name in FONT_SETS.get(kind, FONT_SETS["display"]):
        for directory in _FONT_DIRS:
            path = directory / name
            if path.exists():
                try:
                    chosen = ImageFont.truetype(str(path), size)
                    break
                except OSError:
                    chosen = None
        if chosen is not None:
            break

    if chosen is None:
        try:
            chosen = ImageFont.load_default(size=size)
        except TypeError:
            chosen = ImageFont.load_default()

    _FONT_CACHE[key] = chosen
    return chosen


def text_width(draw, text, fnt, tracking=0.0):
    if not text:
        return 0.0
    total = sum(draw.textlength(ch, font=fnt) for ch in text)
    return total + tracking * (len(text) - 1)


def draw_tracked(draw, xy, text, fnt, fill, tracking=0.0, anchor="mm"):
    """Draw letter-spaced text; xy is the box anchor (m/l/r + m/t/b)."""
    x, y = float(xy[0]), float(xy[1])
    total = text_width(draw, text, fnt, tracking)
    if anchor[0] == "m":
        x -= total / 2.0
    elif anchor[0] == "r":
        x -= total

    for ch in text:
        draw.text((x, y), ch, font=fnt, fill=fill, anchor="lm")
        x += draw.textlength(ch, font=fnt) + tracking
    return total


def fit_font(draw, text, kind, max_width, start_size, tracking=0.0, min_size=14):
    size = int(start_size)
    while size > min_size:
        fnt = font(size, kind)
        if text_width(draw, text, fnt, tracking) <= max_width:
            return fnt
        size = max(min_size, int(size * 0.94))
    return font(min_size, kind)
