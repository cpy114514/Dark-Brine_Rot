"""Cards for the brainstick ad, v2.

Same drawing primitives as `ad_cards.py`, but every card takes its own text
and can break into several lines, because the v2 hook is built from single
words that land one per bar.
"""
from __future__ import annotations

from pathlib import Path

from PIL import Image, ImageDraw

from . import config as C
from . import ad_cards as AC
from .util import font, text_width

OUT = Path(__file__).resolve().parents[1] / "out" / "ad_cards2"


def _fit(draw, text, kind, max_width, start):
    size = start
    fnt = font(size, kind)
    while size > 12 and text_width(draw, text, fnt, 4.0) > max_width:
        size = int(size * 0.92)
        fnt = font(size, kind)
    return fnt


def big_text(name: str, lines: list[str], *, w: int = 1920, h: int = 1080,
             y: float = 0.5, size: float = 0.155, accent=C.TEAL,
             out_dir: Path | None = None, kicker: str | None = None,
             kicker_y: float | None = None, rule: bool = True,
             glow: bool = True, dots: tuple[int, int] | None = None) -> Path:
    """One loud text card, one or more lines, sized to the frame.

    `dots` draws a small progress indicator: (index, total).
    """
    layer = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    draw = ImageDraw.Draw(layer)

    base = int(min(w, h) * size)
    tracking = 7.0 * (size / 0.155)
    fnt = _fit(draw, max(lines, key=len), "display", w * 0.86, base)

    if kicker:
        small = font(int(min(w, h) * 0.028), "mono")
        ky = h * (y - 0.13) if kicker_y is None else h * kicker_y
        AC._shadow_text(layer, (w / 2, ky), kicker, small,
                        tuple(C.EMBER) + (240,), 10.0, blur=8)

    step = base * 1.04
    for i, line in enumerate(lines):
        ly = h * y + (i - (len(lines) - 1) / 2.0) * step
        if glow:
            AC._shadow_text(layer, (w / 2, ly), line, fnt, tuple(C.BONE) + (255,),
                            tracking, blur=20)
        else:
            AC._shadow_text(layer, (w / 2, ly), line, fnt, tuple(C.BONE) + (255,),
                            tracking, blur=10)

    last_y = h * y + (len(lines) - 1) / 2.0 * step
    if rule:
        total = max(text_width(draw, line, fnt, tracking) for line in lines)
        ry = int(last_y + base * 0.70)
        rule_w = int(total * 0.72)
        rule_h = max(3, int(base * 0.055))
        draw.rectangle([w / 2 - rule_w / 2, ry, w / 2 + rule_w / 2, ry + rule_h],
                       fill=tuple(accent) + (240,))

    if dots:
        index, total_dots = dots
        size_d = max(6, int(min(w, h) * 0.012))
        gap = size_d * 3
        y_d = int(last_y + base * (1.25 if rule else 0.75))
        x0 = w / 2 - (total_dots - 1) * gap / 2.0
        for k in range(total_dots):
            col = tuple(accent) + (235,) if k == index else (70, 78, 88, 190)
            x = x0 + k * gap
            draw.rectangle([x - size_d / 2, y_d, x + size_d / 2, y_d + size_d], fill=col)

    out = (out_dir or OUT) / f"{name}.png"
    out.parent.mkdir(parents=True, exist_ok=True)
    layer.save(out)
    return out


def build_all(w: int = 1920, h: int = 1080) -> dict[str, Path]:
    """Every card the v2 edit needs, drawn for one aspect ratio."""
    out_dir = OUT / f"{w}x{h}"
    out_dir.mkdir(parents=True, exist_ok=True)
    portrait = h > w

    def lines_for(text: str) -> list[str]:
        """Portrait splits the long hook into two lines so it stays huge."""
        if portrait and len(text) > 12:
            words = text.split()
            half = len(words) // 2
            return [" ".join(words[:half]), " ".join(words[half:])]
        return [text]

    cards: dict[str, Path] = {}

    # one word per bar, the sentence announced piece by piece.
    # The words sit at slightly different heights so the row never feels static.
    cards["w_it"] = big_text("w_it", ["IT"], w=w, h=h, y=0.44, size=0.34,
                             accent=C.TEAL, out_dir=out_dir, rule=False, glow=False,
                             dots=(0, 4))
    cards["w_knows"] = big_text("w_knows", ["KNOWS"], w=w, h=h, y=0.52, size=0.26,
                                accent=C.TEAL, out_dir=out_dir, rule=False,
                                dots=(1, 4))
    cards["w_your"] = big_text("w_your", ["YOUR"], w=w, h=h, y=0.46, size=0.26,
                               accent=C.TEAL, out_dir=out_dir, rule=False,
                               dots=(2, 4))
    cards["w_name"] = big_text("w_name", ["NAME."], w=w, h=h, y=0.54, size=0.26,
                               accent=C.EMBER, out_dir=out_dir, rule=False,
                               dots=(3, 4))

    # the whole sentence, twice, huge
    cards["hook_full"] = big_text("hook_full", lines_for("IT KNOWS YOUR NAME."),
                                  w=w, h=h, y=0.46, size=0.185, accent=C.TEAL,
                                  out_dir=out_dir)
    cards["hook_full2"] = big_text("hook_full2", lines_for("IT KNOWS YOUR NAME."),
                                   w=w, h=h, y=0.44, size=0.215, accent=C.EMBER,
                                   out_dir=out_dir)

    cards["run"] = big_text("run", ["RUN."], w=w, h=h, y=0.52, size=0.26,
                            accent=C.RUST, out_dir=out_dir)
    cards["survive"] = big_text("survive", ["SURVIVE."], w=w, h=h, y=0.52, size=0.22,
                                accent=C.RUST, out_dir=out_dir)

    cards["logo"] = AC.logo("logo", w=w, h=h, out_dir=out_dir)
    cards["cta"] = AC.cta("cta", w=w, h=h, out_dir=out_dir)
    return cards
