"""Dump evenly spaced frames of specific clips into a labelled strip per clip."""

from __future__ import annotations

import subprocess
import sys
from pathlib import Path

import imageio_ffmpeg
import numpy as np
from PIL import Image, ImageDraw

FFMPEG = imageio_ffmpeg.get_ffmpeg_exe()
ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / "Tools" / "dark_brine_promo" / "out" / "strips"

TW, TH = 320, 180
FRAMES = 8


def dur_of(path: Path) -> float:
    proc = subprocess.run([FFMPEG, "-hide_banner", "-i", str(path)], capture_output=True, text=True, errors="replace")
    import re
    m = re.search(r"Duration:\s*(\d+):(\d+):(\d+\.\d+)", proc.stderr)
    if not m:
        return 0.0
    return int(m.group(1)) * 3600 + int(m.group(2)) * 60 + float(m.group(3))


def grab(path: Path, t: float) -> np.ndarray | None:
    cmd = [
        FFMPEG, "-v", "error", "-ss", f"{max(t, 0.05):.2f}", "-i", str(path),
        "-frames:v", "1", "-vf", f"scale={TW}:{TH}:force_original_aspect_ratio=increase,crop={TW}:{TH}",
        "-f", "rawvideo", "-pix_fmt", "rgb24", "pipe:1",
    ]
    raw = subprocess.run(cmd, capture_output=True).stdout
    if len(raw) < TW * TH * 3:
        return None
    return np.frombuffer(raw, dtype=np.uint8).reshape(TH, TW, 3)


def main() -> int:
    clips = [a for a in sys.argv[1:] if a]
    OUT.mkdir(parents=True, exist_ok=True)
    for raw in clips:
        path = ROOT / raw
        if not path.exists():
            print(f"missing: {raw}")
            continue
        d = dur_of(path)
        strip = Image.new("RGB", (FRAMES * (TW + 4) + 4, TH + 26), (16, 16, 18))
        draw = ImageDraw.Draw(strip)
        for i in range(FRAMES):
            t = d * (i + 0.5) / FRAMES
            frame = grab(path, t)
            if frame is None:
                continue
            strip.paste(Image.fromarray(frame), (4 + i * (TW + 4), 4))
            draw.text((6 + i * (TW + 4), TH + 8), f"{t:.1f}s", fill=(160, 200, 170))
        name = Path(raw).stem[:28].replace(" ", "_")
        out = OUT / f"{name}.png"
        strip.save(out)
        print(f"{out.name}  dur={d:.1f}s  <- {raw}")


if __name__ == "__main__":
    main()
