"""Contact sheet for a finished ad: one frame per bar, labelled with time.

    python strip.py out/Dark_Brine_Rot_Ad_vertical.mp4 out/ad_vertical_strip.png
"""
from __future__ import annotations

import subprocess
import sys
from pathlib import Path

import imageio_ffmpeg
from PIL import Image, ImageDraw

FFMPEG = imageio_ffmpeg.get_ffmpeg_exe()

BG = (10, 12, 16)
LABEL = (214, 224, 234)


def duration(path: Path) -> float:
    proc = subprocess.run([FFMPEG, "-hide_banner", "-i", str(path)],
                          capture_output=True, text=True, errors="replace")
    import re
    m = re.search(r"Duration:\s*(\d+):(\d+):(\d+\.\d+)", proc.stderr or "")
    if not m:
        raise RuntimeError("no duration")
    return int(m.group(1)) * 3600 + int(m.group(2)) * 60 + float(m.group(3))


def main() -> int:
    src = Path(sys.argv[1])
    dst = Path(sys.argv[2])
    cols = int(sys.argv[3]) if len(sys.argv) > 3 else 4
    total = duration(src)
    count = cols * 2
    times = [total * (i + 0.5) / count for i in range(count)]

    tmp = dst.parent / "_strip_frames"
    tmp.mkdir(parents=True, exist_ok=True)
    frames = []
    for i, t in enumerate(times):
        png = tmp / f"f{i:02d}.png"
        subprocess.run([FFMPEG, "-v", "error", "-y", "-ss", f"{t:.2f}", "-i", str(src),
                        "-frames:v", "1", str(png)])
        frames.append((png, t))

    first = Image.open(frames[0][0])
    cell_w = 480
    cell_h = int(cell_w * first.height / first.width)
    rows = (len(frames) + cols - 1) // cols
    pad = 4
    label_h = 20
    sheet = Image.new("RGB", (cols * (cell_w + pad) + pad,
                              rows * (cell_h + label_h + pad) + pad), BG)
    draw = ImageDraw.Draw(sheet)
    for i, (png, t) in enumerate(frames):
        im = Image.open(png).convert("RGB").resize((cell_w, cell_h), Image.LANCZOS)
        x = pad + (i % cols) * (cell_w + pad)
        y = pad + (i // cols) * (cell_h + label_h + pad)
        sheet.paste(im, (x, y))
        draw.text((x + 4, y + cell_h + 4), f"{t:.1f}s", fill=LABEL)
    sheet.save(dst)
    print(dst)
    return 0


if __name__ == "__main__":
    sys.exit(main())
