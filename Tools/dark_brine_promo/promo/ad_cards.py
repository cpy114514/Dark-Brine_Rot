"""Graphic cards for the short looping ad.

Sized in pixels so the same card code serves the 16:9 cut and the 9:16 cut.
Everything is transparent RGBA, ready to be overlaid by ffmpeg.
"""
from __future__ import annotations

from pathlib import Path

from PIL import Image, ImageDraw, ImageFilter

from . import config as C
from .util import draw_tracked, font, text_width

OUT = Path(__file__).resolve().parents[1] / "out" / "ad_cards"

HOOK = "IT KNOWS YOUR NAME"


def _blank(w: int, h: int) -> Image.Image:
    return Image.new("RGBA", (w, h), (0, 0, 0, 0))


def _shadow_text(layer, xy, text, fnt, fill, tracking, anchor="mm", blur=14, alpha=150):
    shadow = Image.new("RGBA", layer.size, (0, 0, 0, 0))
    d = ImageDraw.Draw(shadow)
    draw_tracked(d, xy, text, fnt, (0, 0, 0, alpha), tracking, anchor=anchor)
    shadow = shadow.filter(ImageFilter.GaussianBlur(blur))
    layer.alpha_composite(shadow)

    front = ImageDraw.Draw(layer)
    draw_tracked(front, xy, text, fnt, fill, tracking, anchor=anchor)


def _fit(draw, text, kind, max_width, start):
    size = start
    fnt = font(size, kind)
    while size > 12 and text_width(draw, text, fnt, 4.0) > max_width:
        size = int(size * 0.9)
        fnt = font(size, kind)
    return fnt


def hook(name: str, text: str = HOOK, *, w: int = 1920, h: int = 1080,
         y: float = 0.5, scale: float = 1.0, kicker: str | None = None,
         accent=C.TEAL, out_dir: Path | None = None,
         lines: list[str] | None = None, kicker_y: float | None = None) -> Path:
    """The repeating earworm line, at one of three escalating sizes.

    Portrait cards break the line in two so the type can stay huge.
    """
    layer = _blank(w, h)
    draw = ImageDraw.Draw(layer)

    base = int(min(w, h) * 0.115 * scale)
    fnt = _fit(draw, text, "display", w * 0.88, base)
    tracking = 6.0 * scale

    if kicker:
        small = font(int(min(w, h) * 0.030), "mono")
        ky = h * (y - 0.115) if kicker_y is None else h * kicker_y
        _shadow_text(layer, (w / 2, ky), kicker, small,
                     tuple(C.EMBER) + (240,), 10.0 * scale, blur=8)

    if lines:
        step = base * 1.06
        for i, line in enumerate(lines):
            ly = h * y + (i - (len(lines) - 1) / 2.0) * step
            _shadow_text(layer, (w / 2, ly), line, fnt, tuple(C.BONE) + (255,),
                         tracking, blur=18)
        last_y = h * y + (len(lines) - 1) / 2.0 * step
        total = max(text_width(draw, line, fnt, tracking) for line in lines)
        ry = int(last_y + base * 0.72)
    else:
        _shadow_text(layer, (w / 2, h * y), text, fnt, tuple(C.BONE) + (255,), tracking, blur=18)
        total = text_width(draw, text, fnt, tracking)
        ry = int(h * y + base * 0.72)

    rule_w = int(total * 0.7)
    rule_h = max(2, int(base * 0.05))
    draw.rectangle([w / 2 - rule_w / 2, ry, w / 2 + rule_w / 2, ry + rule_h],
                   fill=tuple(accent) + (240,))

    out = (out_dir or OUT) / f"{name}.png"
    out.parent.mkdir(parents=True, exist_ok=True)
    layer.save(out)
    return out


def word(name: str, text: str, *, w: int = 1920, h: int = 1080,
         y: float = 0.5, accent=C.RUST, out_dir: Path | None = None) -> Path:
    """One loud verb: RUN. / FIGHT. / SURVIVE."""
    layer = _blank(w, h)
    draw = ImageDraw.Draw(layer)

    base = int(min(w, h) * 0.185)
    fnt = _fit(draw, text, "display", w * 0.84, base)
    _shadow_text(layer, (w / 2, h * y), text, fnt, tuple(C.BONE) + (255,), 8.0, blur=20)

    total = text_width(draw, text, fnt, 8.0)
    rule_w = int(total * 0.9)
    rule_h = max(2, int(base * 0.05))
    ry = int(h * y + base * 0.62)
    draw.rectangle([w / 2 - rule_w / 2, ry, w / 2 + rule_w / 2, ry + rule_h],
                   fill=tuple(accent) + (245,))

    out = (out_dir or OUT) / f"{name}.png"
    out.parent.mkdir(parents=True, exist_ok=True)
    layer.save(out)
    return out


def logo(name: str = "logo", *, w: int = 1920, h: int = 1080,
         tagline: str = C.TAGLINE, out_dir: Path | None = None) -> Path:
    layer = _blank(w, h)
    draw = ImageDraw.Draw(layer)

    small = font(int(min(w, h) * 0.052), "condensed")
    big = font(int(min(w, h) * 0.150), "display")
    tracking = 10.0
    gap = min(w, h) * 0.05

    _shadow_text(layer, (w / 2, h * 0.40), "DARK", small, tuple(C.TEAL) + (240,), 26.0, blur=14)

    left, right = "BRINE", "ROT"
    wl = text_width(draw, left, big, tracking)
    wr = text_width(draw, right, big, tracking)
    total = wl + gap + wr
    x = w / 2.0 - total / 2.0
    cy = h * 0.52
    fill = tuple(C.BONE) + (255,)

    for ch in left:
        _shadow_text(layer, (x, cy), ch, big, fill, 0.0, anchor="lm", blur=18)
        x += draw.textlength(ch, font=big) + tracking

    ub = max(3, int(min(w, h) * 0.022))
    uy = int(cy + 0.30 * big.size)
    draw.rectangle([int(x - tracking * 0.35), uy, int(x + gap + tracking * 0.35), uy + ub],
                   fill=tuple(C.TEAL) + (245,))

    xr = x + gap
    for ch in right:
        _shadow_text(layer, (xr, cy), ch, big, fill, 0.0, anchor="lm", blur=18)
        xr += draw.textlength(ch, font=big) + tracking

    rule_w = int(total * 0.56)
    draw.rectangle([w / 2 - rule_w / 2, int(cy + 0.62 * big.size), w / 2 + rule_w / 2,
                    int(cy + 0.62 * big.size) + max(2, int(min(w, h) * 0.004))],
                   fill=tuple(C.RUST) + (235,))

    line = font(int(min(w, h) * 0.046), "condensed")
    _shadow_text(layer, (w / 2, h * 0.66), tagline, line, tuple(C.FOAM) + (235,), 12.0, blur=12)

    out = (out_dir or OUT) / f"{name}.png"
    out.parent.mkdir(parents=True, exist_ok=True)
    layer.save(out)
    return out


def cta(name: str = "cta", *, w: int = 1920, h: int = 1080,
        url: str = "cpy111.itch.io/dark-brine-rot",
        out_dir: Path | None = None) -> Path:
    layer = _blank(w, h)
    draw = ImageDraw.Draw(layer)

    wash = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    wd = ImageDraw.Draw(wash)
    for i in range(0, h, 6):
        a = int(190 * (1.0 - abs(i - h / 2) / (h / 2)) ** 0.6)
        wd.rectangle([0, i, w, i + 6], fill=(3, 7, 11, a))
    layer.alpha_composite(wash)

    line = font(int(min(w, h) * 0.040), "condensed")
    mono = font(int(min(w, h) * 0.032), "mono")
    brand = font(int(min(w, h) * 0.062), "display")

    _shadow_text(layer, (w / 2, h * 0.28), "DARK BRINE_ROT", brand,
                 tuple(C.BONE) + (245,), 10.0, blur=16)
    _shadow_text(layer, (w / 2, h * 0.40), "PLAY FREE ON ITCH.IO", line,
                 tuple(C.EMBER) + (245,), 12.0, blur=12)
    _shadow_text(layer, (w / 2, h * 0.49), url, mono, tuple(C.BONE) + (225,), 5.0, blur=10)

    out = (out_dir or OUT) / f"{name}.png"
    out.parent.mkdir(parents=True, exist_ok=True)
    layer.save(out)
    return out


def build_all(w: int = 1920, h: int = 1080, tag: str = "wide") -> list[Path]:
    """Rebuild every card for one aspect ratio.

    Each aspect ratio gets its own folder, so the 16:9 and 9:16 cuts can be
    rendered at the same time without writing over each other.
    """
    out_dir = OUT / f"{w}x{h}"
    out_dir.mkdir(parents=True, exist_ok=True)
    if h > w:
        # portrait: two big lines instead of one long one
        lines = ["IT KNOWS", "YOUR NAME"]
        return [
            hook("hook1", w=w, h=h, y=0.42, scale=0.85, lines=lines,
                 kicker="FIRST ISLAND · 03:12 AM", kicker_y=0.90, out_dir=out_dir),
            hook("hook2", w=w, h=h, y=0.48, scale=1.05, lines=lines, out_dir=out_dir),
            hook("hook3", w=w, h=h, y=0.54, scale=1.25, lines=lines, out_dir=out_dir),
            word("word_run", "RUN.", w=w, h=h, y=0.52, out_dir=out_dir),
            word("word_fight", "FIGHT.", w=w, h=h, y=0.52, out_dir=out_dir),
            word("word_survive", "SURVIVE.", w=w, h=h, y=0.52, out_dir=out_dir),
            logo("logo", w=w, h=h, out_dir=out_dir),
            cta("cta", w=w, h=h, out_dir=out_dir),
        ]
    return [
        hook("hook1", w=w, h=h, y=0.42, scale=0.80, kicker="FIRST ISLAND · 03:12 AM",
             kicker_y=0.90, out_dir=out_dir),
        hook("hook2", w=w, h=h, y=0.48, scale=1.00, out_dir=out_dir),
        hook("hook3", w=w, h=h, y=0.54, scale=1.30, out_dir=out_dir),
        word("word_run", "RUN.", w=w, h=h, y=0.52, out_dir=out_dir),
        word("word_fight", "FIGHT.", w=w, h=h, y=0.52, out_dir=out_dir),
        word("word_survive", "SURVIVE.", w=w, h=h, y=0.52, out_dir=out_dir),
        logo("logo", w=w, h=h, out_dir=out_dir),
        cta("cta", w=w, h=h, out_dir=out_dir),
    ]
