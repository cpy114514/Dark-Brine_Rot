"""Render the five Dark Brine_Rot brainstick ads.

    python make_ads.py                  # all five, full resolution
    python make_ads.py --only ad1_chant # one of them
    python make_ads.py --preview        # half resolution, quick look
    python make_ads.py --stills ad1_chant:1.0,2.0,4.0

Every ad is rendered from a beat grid, so the chant, the drums and the
cuts are locked together by construction.
"""
from __future__ import annotations

import argparse
import sys
import time
from pathlib import Path

from promo import ad as AD
from promo import adscore
from promo import encode

ROOT = Path(__file__).resolve().parent
OUT = ROOT / "out" / "ads"


def render_one(ad: AD.Ad, *, scale: float = 1.0, crf: int = 18,
               preset: str = "medium", with_audio: bool = True) -> Path:
    OUT.mkdir(parents=True, exist_ok=True)

    w = int(round(ad.width * scale))
    h = int(round(ad.height * scale))

    # the renderer always works in the ad's native size; the scale is
    # applied at encode time so the layout maths stay in one space.
    renderer = AD.AdRenderer(ad)

    wav = None
    if with_audio:
        wav = OUT / f"{ad.key}.wav"
        adscore.write_score(wav, ad)
        print(f"  score  {wav.name}  ({ad.duration:.2f}s)", flush=True)

    out = OUT / f"{ad.key}.mp4"
    print(f"  render {ad.key}: {w}x{h} {ad.fps}fps "
          f"{ad.bars} bars @ {ad.bpm:g}bpm = {ad.duration:.2f}s", flush=True)

    t0 = time.time()
    if abs(scale - 1.0) < 0.001:
        frames = renderer.iter_frames()
    else:
        from PIL import Image

        def frames():
            for im in renderer.iter_frames():
                yield im.resize((w, h), Image.LANCZOS)

    encode.encode_video(frames, out, w, h, ad.fps, audio_path=wav,
                        crf=crf, preset=preset, total_frames=ad.frames)
    print(f"  done   {out.name}  ({out.stat().st_size / 1e6:.1f} MB, "
          f"{time.time() - t0:.0f}s)", flush=True)
    return out


def save_stills(key: str, times: list[float]) -> list[Path]:
    from PIL import Image
    import numpy as np

    recipe = {r.key: r for r in AD.get_recipes()}
    ad = recipe[key]
    renderer = AD.AdRenderer(ad)
    folder = OUT / "stills"
    folder.mkdir(parents=True, exist_ok=True)
    made = []
    for t in times:
        arr = np.clip(renderer.frame(t), 0, 255).astype("uint8")
        path = folder / f"{key}_t{t:05.2f}.png"
        Image.fromarray(arr, "RGB").save(path)
        made.append(path)
        print(f"  still  {path}")
    return made


def main() -> int:
    ap = argparse.ArgumentParser(description="Dark Brine_Rot ad renderer")
    ap.add_argument("--only", action="append", default=None,
                    help="recipe key to render (repeatable)")
    ap.add_argument("--preview", action="store_true", help="half resolution")
    ap.add_argument("--scale", type=float, default=1.0)
    ap.add_argument("--crf", type=int, default=18)
    ap.add_argument("--preset", default="medium")
    ap.add_argument("--no-audio", action="store_true")
    ap.add_argument("--stills", default=None,
                    help="recipe_key:t1,t2,t3 - write PNGs instead of video")
    args = ap.parse_args()

    if args.stills:
        key, _, times = args.stills.partition(":")
        save_stills(key, [float(x) for x in times.split(",") if x.strip()])
        return 0

    recipes = AD.get_recipes()
    if args.only:
        recipes = [r for r in recipes if r.key in args.only]
        if not recipes:
            print("no matching recipe; known keys:", ", ".join(
                r.key for r in AD.get_recipes()))
            return 2

    scale = 0.5 if args.preview else args.scale
    print(f"rendering {len(recipes)} ad(s) at scale {scale}", flush=True)

    for ad in recipes:
        print(f"\n=== {ad.key}  ({ad.title}) ===", flush=True)
        render_one(ad, scale=scale, crf=args.crf, preset=args.preset,
                   with_audio=not args.no_audio)

    print("\nall done ->", OUT)
    return 0


if __name__ == "__main__":
    sys.exit(main())
