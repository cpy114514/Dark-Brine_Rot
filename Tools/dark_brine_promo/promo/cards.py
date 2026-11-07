"""Graphic cards for the trailer: captions, weapon cards, title and end card.

Everything is drawn as a transparent 1920x1080 RGBA PNG so ffmpeg can overlay
it on the picture without re-rendering the frame.
"""
from __future__ import annotations

from pathlib import Path

from PIL import Image, ImageDraw, ImageFilter

from . import config as C
from .util import draw_tracked, font, text_width

W, H = 1920, 1080
OUT = Path(__file__).resolve().parents[1] / "out" / "cards"


def _blank() -> Image.Image:
    return Image.new("RGBA", (W, H), (0, 0, 0, 0))


def _shadow_text(layer: Image.Image, xy, text, fnt, fill, tracking, anchor="mm", blur=14, alpha=150):
    shadow = Image.new("RGBA", layer.size, (0, 0, 0, 0))
    d = ImageDraw.Draw(shadow)
    draw_tracked(d, xy, text, fnt, (0, 0, 0, alpha), tracking, anchor=anchor)
    shadow = shadow.filter(ImageFilter.GaussianBlur(blur))
    layer.alpha_composite(shadow)

    front = ImageDraw.Draw(layer)
    draw_tracked(front, xy, text, fnt, fill, tracking, anchor=anchor)


def caption(name: str, text: str, *, sub: str | None = None, y: float = 0.74,
            size: int = 58, tracking: float = 10.0) -> Path:
    """A single line of caption with a rust rule under it."""
    layer = _blank()
    draw = ImageDraw.Draw(layer)

    fnt = font(size, "condensed")
    cy = H * y
    _shadow_text(layer, (W / 2, cy), text, fnt, tuple(C.BONE) + (255,), tracking)

    total = text_width(draw, text, fnt, tracking)
    rule_w = max(120, int(total * 0.42))
    rule_h = 3
    ry = int(cy + size * 0.80)
    draw.rectangle([W / 2 - rule_w / 2, ry, W / 2 + rule_w / 2, ry + rule_h],
                   fill=tuple(C.RUST) + (235,))

    if sub:
        small = font(30, "mono")
        _shadow_text(layer, (W / 2, ry + 64), sub, small, tuple(C.FOAM) + (235,), 8.0, blur=9)

    out = OUT / f"{name}.png"
    layer.save(out)
    return out


def weapon_card(name: str, title: str, sub: str, *, tint=C.TEAL) -> Path:
    """Full-frame weapon card: big name, rule, small description."""
    layer = _blank()
    draw = ImageDraw.Draw(layer)

    # soft dark wash so the card reads over any frame
    wash = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    wd = ImageDraw.Draw(wash)
    for i in range(0, H, 6):
        a = int(120 * (1.0 - abs(i - H / 2) / (H / 2)) ** 0.6)
        wd.rectangle([0, i, W, i + 6], fill=(4, 10, 14, a))
    layer.alpha_composite(wash)

    big = font(132, "display")
    small = font(34, "mono")
    ty = H * 0.46
    _shadow_text(layer, (W / 2, ty), title, big, tuple(C.BONE) + (255,), 16.0, blur=22)

    total = text_width(draw, title, big, 16.0)
    draw.rectangle([W / 2 - total / 2, ty + 104, W / 2 + total / 2, ty + 108],
                   fill=tuple(tint) + (240,))

    _shadow_text(layer, (W / 2, ty + 150), sub, small, tuple(C.FOAM) + (230,), 9.0, blur=10)

    out = OUT / f"{name}.png"
    layer.save(out)
    return out


def title_card(name: str = "title", *, tagline: str = C.TAGLINE,
               subtitle: str | None = None) -> Path:
    """The reveal: DARK over BRINE_ROT with a drawn underscore."""
    layer = _blank()
    draw = ImageDraw.Draw(layer)

    top = font(46, "condensed")
    big = font(190, "display")

    _shadow_text(layer, (W / 2, H * 0.33), "DARK", top, tuple(C.TEAL) + (240,), 30.0, blur=16)

    left, right = "BRINE", "ROT"
    tracking = 12.0
    gap = 46.0
    wl = text_width(draw, left, big, tracking)
    wr = text_width(draw, right, big, tracking)
    total = wl + gap + wr
    x = W / 2.0 - total / 2.0
    cy = H * 0.52

    fill = tuple(C.BONE) + (255,)
    for ch in left:
        _shadow_text(layer, (x, cy), ch, big, fill, 0.0, anchor="lm", blur=20)
        x += draw.textlength(ch, font=big) + tracking

    ub = 20
    uy = int(cy + 0.30 * big.size)
    draw.rectangle([int(x - tracking * 0.35), uy, int(x + gap + tracking * 0.35), uy + ub],
                   fill=tuple(C.TEAL) + (245,))

    xr = x + gap
    for ch in right:
        _shadow_text(layer, (xr, cy), ch, big, fill, 0.0, anchor="lm", blur=20)
        xr += draw.textlength(ch, font=big) + tracking

    rule_w = int(total * 0.56)
    draw.rectangle([W / 2 - rule_w / 2, int(cy + 0.62 * big.size), W / 2 + rule_w / 2,
                    int(cy + 0.62 * big.size) + 4], fill=tuple(C.RUST) + (235,))

    if tagline:
        small = font(38, "condensed")
        _shadow_text(layer, (W / 2, H * 0.70), tagline, small, tuple(C.FOAM) + (235,), 12.0, blur=12)

    if subtitle:
        mono = font(28, "mono")
        _shadow_text(layer, (W / 2, H * 0.76), subtitle, mono, tuple(C.EMBER) + (235,), 10.0, blur=10)

    out = OUT / f"{name}.png"
    layer.save(out)
    return out


def end_card(name: str = "end", *, tagline: str = C.TAGLINE,
             url: str = "cpy111.itch.io/dark-brine-rot") -> Path:
    layer = _blank()
    draw = ImageDraw.Draw(layer)

    wash = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    wd = ImageDraw.Draw(wash)
    for i in range(18):
        a = int(200 * (1.0 - abs(i - 9) / 9.0) ** 1.3)
        wd.rectangle([0, i * 6, W, i * 6 + 6], fill=(3, 7, 11, a))
    layer.alpha_composite(wash)

    title = font(96, "display")
    line = font(40, "condensed")
    mono = font(30, "mono")

    _shadow_text(layer, (W / 2, H * 0.36), "DARK BRINE_ROT", title, tuple(C.BONE) + (255,), 12.0, blur=22)
    _shadow_text(layer, (W / 2, H * 0.52), tagline, line, tuple(C.FOAM) + (235,), 10.0, blur=14)

    draw.rectangle([W / 2 - 150, H * 0.60, W / 2 + 150, H * 0.60 + 3], fill=tuple(C.RUST) + (230,))

    _shadow_text(layer, (W / 2, H * 0.665), "PLAY FREE ON ITCH.IO", mono, tuple(C.EMBER) + (240,), 12.0, blur=12)
    _shadow_text(layer, (W / 2, H * 0.72), url, mono, tuple(C.BONE) + (220,), 6.0, blur=10)

    out = OUT / f"{name}.png"
    layer.save(out)
    return out


def build_all() -> list[Path]:
    OUT.mkdir(parents=True, exist_ok=True)
    made = [
        caption("cap_crossing", "THE CROSSING WAS SUPPOSED TO BE SILENT."),
        caption("cap_already", "SOMETHING WAS ALREADY HERE.", sub="FIRST ISLAND - 03:12 AM"),
        caption("cap_land", "LAND. SHOOT. SURVIVE.", sub="COMBOS \u00b7 DODGES \u00b7 THE MAP KNOWS"),
        caption("cap_boss", "THREE THINGS GUARD THIS ISLAND.", sub="CAPPUCCINO \u00b7 NAILONG \u00b7 BOMBARDINO"),
        caption("cap_conspiracy", "EVERY ISLAND HIDES SOMETHING.", sub="FIND OUT WHAT HAPPENED TO YOU"),
        weapon_card("card_club", "CLUB", "MELEE \u00b7 SLOW \u00b7 DEVASTATING"),
        weapon_card("card_blades", "TWIN BLADES", "FAST \u00b7 AERIAL RUSH \u00b7 COMBO FODDER"),
        weapon_card("card_rifle", "M4A1", "RANGED \u00b7 UNLOCKED \u00b7 HIGH DAMAGE"),
        title_card(),
        end_card(),
    ]
    return made


if __name__ == "__main__":
    for p in build_all():
        print(p)
