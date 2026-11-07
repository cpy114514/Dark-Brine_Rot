"""Tile every PNG under AUTO_QA/Evidence into contact sheets for review."""

from __future__ import annotations

import subprocess
import sys
from pathlib import Path

import imageio_ffmpeg
from PIL import Image, ImageDraw

FFMPEG = imageio_ffmpeg.get_ffmpeg_exe()
ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / "Tools" / "dark_brine_promo" / "out" / "sheets"

TW, TH = 240, 135
COLS, ROWS = 8, 8
PAD = 4
LABEL_H = 24


def main() -> int:
    src_dir = Path(sys.argv[1]) if len(sys.argv) > 1 else ROOT / "AUTO_QA" / "Evidence"
    limit = int(sys.argv[2]) if len(sys.argv) > 2 else 0
    files = sorted(p for p in src_dir.rglob("*.png") if p.stat().st_size > 4000)
    if limit:
        files = files[:limit]
    OUT.mkdir(parents=True, exist_ok=True)
    per_sheet = COLS * ROWS
    sheet_w = COLS * (TW + PAD) + PAD
    sheet_h = ROWS * (TH + LABEL_H + PAD) + PAD
    index = 0
    for s in range((len(files) + per_sheet - 1) // per_sheet):
        chunk = files[s * per_sheet:(s + 1) * per_sheet]
        sheet = Image.new("RGB", (sheet_w, sheet_h), (18, 18, 20))
        draw = ImageDraw.Draw(sheet)
        for i, f in enumerate(chunk):
            try:
                img = Image.open(f).convert("RGB")
            except Exception:
                continue
            w, h = img.size
            scale = max(TW / w, TH / h)
            img = img.resize((max(1, int(w * scale)), max(1, int(h * scale))), Image.LANCZOS)
            left = (img.width - TW) // 2
            top = (img.height - TH) // 2
            img = img.crop((left, top, left + TW, top + TH))
            x = PAD + (i % COLS) * (TW + PAD)
            y = PAD + (i // COLS) * (TH + LABEL_H + PAD)
            sheet.paste(img, (x, y))
            draw.text((x + 2, y + TH + 3), f"{s}:{i} {f.name[:26]}", fill=(150, 190, 160))
        out = OUT / f"png_{s:02d}.png"
        sheet.save(out)
        print(f"{out.name}: {len(chunk)} files")
    print(f"total {len(files)}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
