'''Contact sheet of every figure in the library.

Rendering the whole cast side by side is the fastest way to judge whether
the silhouettes actually read as characters, so it ships as a dev tool:

    python -m promo.sheet            -> out/figure_sheet.png
'''
from __future__ import annotations

from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw

from . import config as C
from . import entities


def _cell(w, h):
    base = np.zeros((h, w, 3), dtype=np.float32)
    yy = np.linspace(0.0, 1.0, h, dtype=np.float32)[:, None]
    base += (np.array((16, 30, 34), np.float32)[None, None, :] * (1.0 - yy[..., None]))
    base += (np.array((4, 10, 13), np.float32)[None, None, :] * yy[..., None])
    glow = np.exp(-(((yy - 0.62) / 0.30) ** 2))
    base += glow[..., None] * np.array((26, 64, 66), np.float32) * 0.55
    return base


def build(path=None, cell=460, pad=18):
    items = [
        ("SAHUR", lambda s: entities.sahur("club", s, C.TEAL)),
        ("CLUB", lambda s: entities.club(s, C.TEAL)),
        ("DUAL BLADES", lambda s: entities.blades(s, C.TEAL)),
        ("M4A1", lambda s: entities.rifle(s, C.TEAL)),
        ("CAPPUCCINO", lambda s: entities.cappuccino(s, C.EMBER)),
        ("NAILONG", lambda s: entities.nailong(s, C.TEAL)),
        ("CROCODILO", lambda s: entities.crocodilo(s, C.EMBER)),
    ]

    cols = 4
    rows = (len(items) + cols - 1) // cols
    cw = cell
    ch = int(cell * 1.18)
    sheet_w = cols * (cw + pad) + pad
    sheet_h = rows * (ch + pad + 30) + pad
    sheet = Image.new("RGB", (sheet_w, sheet_h), (2, 5, 7))
    draw = ImageDraw.Draw(sheet)

    for i, (name, maker) in enumerate(items):
        r, c = divmod(i, cols)
        x = pad + c * (cw + pad)
        y = pad + r * (ch + pad + 30)

        bg = Image.fromarray(np.clip(_cell(cw, ch), 0, 255).astype(np.uint8), "RGB")
        sprite = maker(cw)
        arr = np.asarray(bg, dtype=np.float32)
        arr = entities.blit(arr, sprite, cw / 2.0, ch / 2.0, opacity=1.0)
        sheet.paste(Image.fromarray(np.clip(arr, 0, 255).astype(np.uint8), "RGB"), (x, y))

        draw.text((x + 4, y + ch + 8), name, fill=(150, 190, 190))

    path = Path(path or (C.OUT_DIR / "figure_sheet.png"))
    path.parent.mkdir(parents=True, exist_ok=True)
    sheet.save(path)
    return path


if __name__ == "__main__":
    print("sheet ->", build())
