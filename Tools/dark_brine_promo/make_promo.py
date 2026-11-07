#!/usr/bin/env python3
"""Build the Dark Brine_Rot cinematic teaser end to end.

Examples
--------
    python make_promo.py                      # full 1080p MP4 with audio
    python make_promo.py --scale 0.5          # fast half-res preview
    python make_promo.py --stills 3.4,9.0,19.4  # PNG stills only
    python make_promo.py --audio-only         # regenerate the WAV
"""
from __future__ import annotations

import argparse
import sys
import time
from pathlib import Path

HERE = Path(__file__).resolve().parent
sys.path.insert(0, str(HERE))

from promo import audio, config as C  # noqa: E402
from promo.render import Renderer  # noqa: E402


def main(argv=None):
    ap = argparse.ArgumentParser(description="Dark Brine_Rot teaser renderer")
    ap.add_argument("--out", type=Path, default=C.OUT_DIR / "Dark_Brine_Rot_Teaser.mp4")
    ap.add_argument("--scale", type=float, default=1.0, help="resolution multiplier")
    ap.add_argument("--fps", type=int, default=C.FPS)
    ap.add_argument("--stills", type=str, default="", help="comma separated seconds")
    ap.add_argument("--still-dir", type=Path, default=C.OUT_DIR / "stills")
    ap.add_argument("--start", type=float, default=0.0)
    ap.add_argument("--end", type=float, default=C.DURATION)
    ap.add_argument("--no-audio", action="store_true")
    ap.add_argument("--audio-only", action="store_true")
    ap.add_argument("--crf", type=int, default=18)
    ap.add_argument("--preset", type=str, default="medium")
    args = ap.parse_args(argv)

    if args.audio_only:
        path = audio.write_soundtrack(C.OUT_DIR / "soundtrack.wav", C.DURATION)
        print("audio  -> %s" % path)
        return 0

    scale = max(0.25, float(args.scale))
    width = int(round(C.WIDTH * scale / 2)) * 2
    height = int(round(C.HEIGHT * scale / 2)) * 2
    fps = max(1, int(args.fps))
    start = max(0.0, float(args.start))
    end = min(float(args.end), C.DURATION)
    if end <= start:
        ap.error("--end must be greater than --start")

    t0 = time.time()
    renderer = Renderer(width, height, fps)
    print("render %dx%d @ %dfps  %.2fs" % (width, height, fps, end - start))

    if args.stills:
        times = [float(x) for x in args.stills.split(",") if x.strip()]
        for p in renderer.save_stills(times, args.still_dir):
            print("still  -> %s" % p)
        print("done in %.1fs" % (time.time() - t0))
        return 0

    audio_path = None
    if not args.no_audio:
        audio_path = audio.write_soundtrack(C.OUT_DIR / "soundtrack.wav", C.DURATION)
        print("audio  -> %s" % audio_path)

    full = start <= 0.0 and end >= C.DURATION
    out = renderer.render_video(
        args.out,
        audio_path=audio_path if full else None,
        start=start,
        end=end,
        crf=max(0, int(args.crf)),
        preset=str(args.preset),
    )
    size_mb = out.stat().st_size / 1e6
    print("\nvideo  -> %s  (%.1f MB) in %.1fs" % (out, size_mb, time.time() - t0))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
