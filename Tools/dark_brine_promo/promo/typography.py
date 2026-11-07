"""Cinematic typography: captions, hype cards, the title and end card."""
from __future__ import annotations

import numpy as np
from PIL import Image, ImageDraw, ImageFilter

from . import config as C
from .util import (clamp, draw_tracked, ease_out_cubic, ease_out_expo, font,
                   lerp, smoothstep, text_width)


def _alpha(layer, radius=0.0):
    a = layer.split()[3]
    if radius > 0.0:
        a = a.filter(ImageFilter.GaussianBlur(float(radius)))
    return np.asarray(a, dtype=np.float32) / 255.0


def compose(arr, layer, opacity=1.0, glow=0.0, glow_color=None):
    opacity = float(clamp(opacity))
    if opacity <= 0.001:
        return arr
    if glow > 0.0:
        g = _alpha(layer, glow)
        gc = np.asarray(glow_color or C.FOAM, dtype=np.float32)
        arr = arr + g[..., None] * gc * (0.55 * opacity * min(1.0, glow / 20.0))
    a = _alpha(layer)[..., None] * opacity
    rgb = np.asarray(layer.convert("RGB"), dtype=np.float32)
    return arr * (1.0 - a) + rgb * a


def text_layer(w, h, text, fnt, color, tracking, y):
    layer = Image.new("RGBA", (int(w), int(h)), (0, 0, 0, 0))
    draw = ImageDraw.Draw(layer)
    draw_tracked(draw, (w / 2.0, y), text, fnt, tuple(color) + (255,), tracking, anchor="mm")
    return layer, draw


def caption(arr, text, *, y=0.5, size=52, tracking=12.0, color=C.BONE,
            opacity=1.0, kind="condensed", glow=0.0, glow_color=None,
            max_width=None):
    h, w = arr.shape[:2]
    probe = ImageDraw.Draw(Image.new("L", (1, 1)))
    fnt = font(size, kind)
    if max_width is not None:
        while size > 12 and text_width(probe, text, fnt, tracking) > max_width:
            size = int(size * 0.92)
            fnt = font(size, kind)
    layer, _ = text_layer(w, h, text, fnt, color, tracking, h * float(y))
    return compose(arr, layer, opacity, glow, glow_color)


def headline(arr, text, p, *, y=0.5, size=92, tracking=4.0, opacity=1.0,
             accent=C.RUST, bar=True, glow_color=C.TEAL):
    h, w = arr.shape[:2]
    p = float(clamp(p))
    e = ease_out_cubic(p)
    fnt = font(int(size * lerp(0.95, 1.0, e)), "condensed")
    layer, draw = text_layer(w, h, text, fnt, C.BONE, tracking, h * float(y))
    arr = compose(arr, layer, opacity * smoothstep(0.0, 0.45, p), glow=10.0,
                  glow_color=glow_color)
    if bar and e > 0.02:
        total = text_width(draw, text, fnt, tracking)
        bw = max(2, int(total * e))
        bh = max(2, int(size * 0.035))
        by = int(h * float(y) + size * 0.56)
        x0 = int(w / 2 - bw / 2)
        arr[by:by + bh, x0:x0 + bw] = np.asarray(accent, np.float32) * opacity
    return arr


def main_title(arr, p, *, y=0.50, scale=1.0):
    """Final title reveal: spaced-out tracking settling into the logo.

    ``BRINE`` and ``ROT`` are drawn as two runs with a drawn underscore
    between them, so the separator in ``Dark Brine_Rot`` stays visible at
    every size instead of dissolving into the font's baseline glyph.
    """
    h, w = arr.shape[:2]
    p = float(clamp(p))
    e = ease_out_expo(p)
    alpha = smoothstep(0.0, 0.28, p)

    layer = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    draw = ImageDraw.Draw(layer)

    small = font(int(44 * scale), "condensed")
    draw_tracked(draw, (w / 2.0, h * (y - 0.176)), "DARK",
                 small, tuple(C.TEAL) + (int(235 * alpha),),
                 lerp(34.0, 16.0, e) * scale, anchor="mm")

    big = font(int(188 * scale), "display")
    tr = lerp(30.0, 8.0, e) * scale
    left, right = "BRINE", "ROT"
    gap = 34.0 * scale + tr * 2.0
    wl = text_width(draw, left, big, tr)
    wr = text_width(draw, right, big, tr)
    total = wl + gap + wr
    x = w / 2.0 - total / 2.0
    cy = h * float(y) + 18.0 * scale
    fill = tuple(C.BONE) + (int(255 * alpha),)

    for ch in left:
        draw.text((x, cy), ch, font=big, fill=fill, anchor="lm")
        x += draw.textlength(ch, font=big) + tr

    ub = max(3, int(18 * scale))
    uy = int(cy + 0.28 * big.size)
    draw.rectangle([int(x - tr * 0.35), uy,
                    int(x + gap + tr * 0.35), uy + ub],
                   fill=tuple(C.TEAL) + (int(240 * alpha),))

    xr = x + gap
    for ch in right:
        draw.text((xr, cy), ch, font=big, fill=fill, anchor="lm")
        xr += draw.textlength(ch, font=big) + tr

    arr = compose(arr, layer, 1.0, glow=14.0 * (0.4 + 0.6 * e), glow_color=C.FOAM)

    if e > 0.05:
        bw = max(2, int(total * 0.60 * e))
        bh = max(2, int(3 * scale))
        by = int(cy + 0.60 * big.size)
        x0 = int(w / 2 - bw / 2)
        arr[by:by + bh, x0:x0 + bw] = np.asarray(C.RUST, np.float32)
    return arr


def end_card(arr, p, *, tagline=C.TAGLINE, cta="COMING SOON", scale=1.0):
    h, w = arr.shape[:2]
    p = float(clamp(p))
    a1 = smoothstep(0.00, 0.35, p)
    a2 = smoothstep(0.25, 0.65, p)
    a3 = smoothstep(0.55, 1.00, p)

    layer = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    draw = ImageDraw.Draw(layer)

    title = font(int(86 * scale), "display")
    draw_tracked(draw, (w / 2.0, h * 0.38), "DARK BRINE_ROT",
                 title, tuple(C.BONE) + (int(255 * a1),), 10.0, anchor="mm")

    line = font(int(40 * scale), "condensed")
    draw_tracked(draw, (w / 2.0, h * 0.53), tagline,
                 line, tuple(C.FOAM) + (int(255 * a2),), 9.0 * scale, anchor="mm")

    rule_w = int(150 * scale)
    rule_h = max(2, int(2 * scale))
    rule_y = int(h * 0.605)
    draw.rectangle([w / 2 - rule_w / 2, rule_y,
                    w / 2 + rule_w / 2, rule_y + rule_h],
                   fill=tuple(C.RUST) + (int(220 * a3),))

    cta_font = font(int(26 * scale), "mono")
    draw_tracked(draw, (w / 2.0, h * 0.652), cta,
                 cta_font, tuple(C.EMBER) + (int(255 * a3),), 12.0 * scale, anchor="mm")

    return compose(arr, layer, 1.0, glow=12.0, glow_color=C.TEAL)
