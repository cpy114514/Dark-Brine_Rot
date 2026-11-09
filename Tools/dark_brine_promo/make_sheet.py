"""Contact sheet of a captured frame folder.

    python make_sheet.py out/raw/island_walk out/raw/island_walk_sheet.png

One row per folder keeps the overview readable: each row is a time slice of
one shot, so a bad take is visible without opening a single frame.
"""
from __future__ import annotations

import math
import sys
from pathlib import Path

from PIL import Image, ImageDraw


def sheet_for(folder: Path, columns: int = 8, width: int = 300) -> Image.Image:
    files = sorted(folder.glob("cap_*.jpg"))
    if not files:
        raise SystemExit(f"NO_FRAMES {folder}")
    step = max(1, len(files) // columns)
    picks = files[::step][:columns]
    height = int(width * 9 / 16)
    rows = 1
    sheet = Image.new("RGB", (columns * width, rows * height), (10, 10, 12))
    draw = ImageDraw.Draw(sheet)
    for index, path in enumerate(picks):
        image = Image.open(path).resize((width, height))
        x = index * width
        sheet.paste(image, (x, 0))
        draw.text((x + 6, 6), f"{path.stem.split('_')[-1]}", fill=(255, 214, 120))
    return sheet


def main() -> int:
    if len(sys.argv) < 3:
        print(__doc__)
        return 2
    folder = Path(sys.argv[1])
    out = Path(sys.argv[2])
    out.parent.mkdir(parents=True, exist_ok=True)
    sheet = sheet_for(folder)
    sheet.save(out)
    print(f"wrote {out} {sheet.size}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
