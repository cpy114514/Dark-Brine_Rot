"""Preview how the 16:9 AI shots survive the 9:16 centre crop.

Builds, for one source frame, the portrait window at several horizontal
offsets so the crop can be chosen by eye instead of by guesswork.
"""
from __future__ import annotations

import sys
from pathlib import Path

from PIL import Image, ImageDraw

ROOT = Path(__file__).resolve().parent
OUT = ROOT / "out" / "crop_preview"
OUT.mkdir(parents=True, exist_ok=True)

src = Path(sys.argv[1]) if len(sys.argv) > 1 else ROOT / "out" / "ai" / "shark_frames" / "sh_1.5.png"
times = sys.argv[2].split(",") if len(sys.argv) > 2 else ["1.5"]

TARGET_H = 1920
TARGET_W = 1080
THUMB_H = 480
THUMB_W = 270

offsets = [0.0, 0.25, 0.5, 0.72, 0.85, 1.0]

for t in times:
    frame = ROOT / "out" / "ai" / "shark_frames" / f"sh_{t}.png"
    if not frame.exists():
        continue
    im = Image.open(frame).convert("RGB")
    scaled_h = TARGET_H
    scaled_w = int(im.width * TARGET_H / im.height)
    if scaled_w < TARGET_W:
        scaled_w = TARGET_W
        scaled_h = int(im.height * TARGET_W / im.width)
    big = im.resize((scaled_w, scaled_h), Image.LANCZOS)
    strip = Image.new("RGB", (THUMB_W * len(offsets), THUMB_H + 26), (12, 14, 18))
    draw = ImageDraw.Draw(strip)
    for i, off in enumerate(offsets):
        x = int((scaled_w - TARGET_W) * off)
        x = max(0, min(scaled_w - TARGET_W, x))
        crop = big.crop((x, 0, x + TARGET_W, TARGET_H)).resize((THUMB_W, THUMB_H), Image.LANCZOS)
        strip.paste(crop, (i * THUMB_W, 26))
        draw.text((i * THUMB_W + 6, 6), f"x={off}", fill=(220, 230, 240))
    dst = OUT / f"crop_{Path(src).stem}_{t}.png"
    strip.save(dst)
    print(dst)
