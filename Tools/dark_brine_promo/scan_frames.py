"""Extract one representative frame per clip and lay them out as contact sheets.

The repo carries 619 review recordings. Reading them one by one is hopeless, so
this grabs a mid-clip frame from each and tiles them into labelled sheets that
can be eyeballed quickly.
"""

from __future__ import annotations

import json
import subprocess
import sys
from pathlib import Path

import imageio_ffmpeg
import numpy as np
from PIL import Image, ImageDraw

FFMPEG = imageio_ffmpeg.get_ffmpeg_exe()
ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / "Tools" / "dark_brine_promo" / "out" / "sheets"

TW, TH = 240, 135
COLS, ROWS = 8, 8
PAD = 4
LABEL_H = 26


def grab(path: Path, t: float) -> np.ndarray | None:
    cmd = [
        FFMPEG, "-v", "error",
        "-ss", f"{max(t, 0.05):.2f}", "-i", str(path),
        "-frames:v", "1",
        "-vf", f"scale={TW}:{TH}:force_original_aspect_ratio=increase,crop={TW}:{TH}",
        "-f", "rawvideo", "-pix_fmt", "rgb24", "pipe:1",
    ]
    proc = subprocess.run(cmd, capture_output=True)
    raw = proc.stdout
    if len(raw) < TW * TH * 3:
        return None
    return np.frombuffer(raw, dtype=np.uint8).reshape(TH, TW, 3)


def main() -> int:
    limit = int(sys.argv[1]) if len(sys.argv) > 1 else 0
    clips = json.loads((ROOT / "Tools" / "dark_brine_promo" / "out" / "clip_scan.json").read_text(encoding="utf-8"))
    clips = [c for c in clips if "dark_brine_promo" not in c["path"]]
    if limit:
        clips = clips[:limit]
    OUT.mkdir(parents=True, exist_ok=True)

    per_sheet = COLS * ROWS
    sheet_w = COLS * (TW + PAD) + PAD
    sheet_h = ROWS * (TH + LABEL_H + PAD) + PAD
    made = []
    for s in range((len(clips) + per_sheet - 1) // per_sheet):
        chunk = clips[s * per_sheet:(s + 1) * per_sheet]
        sheet = Image.new("RGB", (sheet_w, sheet_h), (18, 18, 20))
        draw = ImageDraw.Draw(sheet)
        for i, c in enumerate(chunk):
            frame = grab(ROOT / c["path"], c["dur"] * 0.5)
            if frame is None:
                continue
            x = PAD + (i % COLS) * (TW + PAD)
            y = PAD + (i // COLS) * (TH + LABEL_H + PAD)
            sheet.paste(Image.fromarray(frame), (x, y))
            name = Path(c["path"]).name[:30]
            draw.text((x + 2, y + TH + 4), f"{s}:{i} {name}", fill=(150, 190, 160))
        out = OUT / f"sheet_{s:02d}.png"
        sheet.save(out)
        made.append(out)
        print(f"sheet {s}: {len(chunk)} clips -> {out.name}")
    print(json.dumps({"sheets": [str(m) for m in made]}, indent=1))
    return 0


if __name__ == "__main__":
    sys.exit(main())
