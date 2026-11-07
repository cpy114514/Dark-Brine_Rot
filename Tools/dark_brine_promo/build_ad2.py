"""The brainstick ad, v2 — new AI shots, faster grid, louder repetition.

Eight bars of 132 BPM. The hook is announced one word per bar for four bars,
then the whole sentence lands twice in a row on bars six and seven. The red
eye motif returns on bar one, bar two and bar seven, so the picture keeps
coming back to the same image — that is the repetition that sticks.

    python build_ad2.py              # 1920x1080
    python build_ad2.py --vertical   # 1080x1920
"""
from __future__ import annotations

import subprocess
import sys
from pathlib import Path

import imageio_ffmpeg

import build_ad
from promo import ad_cards2, loop_score_v2

ROOT = Path(__file__).resolve().parent
ASSETS = ROOT / "assets"
AI2 = ASSETS / "ai2"
AI1 = ASSETS / "ai"
FFMPEG = imageio_ffmpeg.get_ffmpeg_exe()

FPS = 24
BAR = loop_score_v2.BAR       # 1.8182 s
BEAT = loop_score_v2.BEAT     # 0.4545 s


def _ai2(name: str) -> Path:
    return AI2 / f"{name}.mp4"


def _ai1(name: str) -> Path:
    return AI1 / f"{name}.mp4"


# The edit. Every entry is one shot on the timeline.
SHOTS = [
    # bars 1-4: the sentence arrives one word at a time
    {"kind": "video", "src": _ai2("eyes_1"), "start": 2.0, "dur": BAR, "dark": 0.0, "lift": 0.05},
    {"kind": "video", "src": _ai2("eyes_2"), "start": 2.0, "dur": BAR, "dark": 0.0, "lift": 0.05},
    {"kind": "video", "src": _ai2("eye_open"), "start": 1.2, "dur": BAR, "dark": 0.0,
     "lift": 0.04, "crop_x": 0.72},
    {"kind": "video", "src": _ai2("eyes_3"), "start": 2.0, "dur": BAR, "dark": 0.0, "lift": 0.05},

    # bar 5: four cuts on the beat — hard cuts, no fades, so it reads as a burst
    {"kind": "video", "src": _ai2("lamp"), "start": 1.5, "dur": BEAT, "dark": 0.0, "fade": 0.04},
    {"kind": "video", "src": _ai2("claw"), "start": 0.8, "dur": BEAT, "dark": 0.0, "fade": 0.04},
    {"kind": "video", "src": _ai2("surf_raid"), "start": 1.1, "dur": BEAT, "dark": 0.0, "fade": 0.04},
    {"kind": "video", "src": _ai2("pulse"), "start": 0.6, "dur": BEAT, "dark": 0.0, "fade": 0.04},

    # bar 6: the sentence, first time, whole
    {"kind": "video", "src": _ai2("stare"), "start": 1.2, "dur": BAR, "dark": 0.0},

    # bar 7: the motif comes back, the sentence a second time
    {"kind": "video", "src": _ai2("eyes_1"), "start": 3.0, "dur": BAR, "dark": 0.0, "lift": 0.05},

    {"kind": "card", "src": "logo", "dur": BAR / 2},
    {"kind": "card", "src": "cta", "dur": BAR / 2},
]

# Text cards, in seconds from the start of the film.
OVERLAYS = [
    ("w_it", 0.08, BAR - 0.18),
    ("w_knows", BAR + 0.08, 2 * BAR - 0.18),
    ("w_your", 2 * BAR + 0.08, 3 * BAR - 0.18),
    ("w_name", 3 * BAR + 0.08, 4 * BAR - 0.18),
    ("run", 4 * BAR + 0.42, 4 * BAR + 1.30),
    ("hook_full", 5 * BAR + 0.05, 6 * BAR - 0.10),
    ("hook_full2", 6 * BAR + 0.05, 7 * BAR - 0.10),
]

GRADE = ("eq=contrast=1.18:saturation=1.02:brightness=-0.02:gamma=0.98,"
         "colorchannelmixer=rr=0.94:gg=0.97:bb=1.16:bg=0.05:br=0.03,"
         "vignette=PI/4.0")


def main() -> int:
    vertical = "--vertical" in sys.argv
    w, h = (1080, 1920) if vertical else (1920, 1080)
    tag = "vertical" if vertical else "wide"
    build_ad.WORK = ROOT / "out" / "ad_build2" / tag
    build_ad.WORK.mkdir(parents=True, exist_ok=True)

    cards = ad_cards2.build_all(w, h)
    cards_dir = ad_cards2.OUT / f"{w}x{h}"

    score = ROOT / "out" / f"ad_loop_score_v2_{tag}.wav"
    loop_score_v2.write_score(score)

    total = sum(float(s.get("dur", BAR)) for s in SHOTS)

    segments = []
    for i, shot in enumerate(SHOTS):
        src = shot.get("src")
        if shot["kind"] == "video" and not Path(src).exists():
            print(f"skip {i}: missing {src}", flush=True)
            continue
        seg = build_ad.build_segment(shot, i, w, h, cards_dir)
        segments.append(seg)
        print(f"seg {i} {shot['kind']:5s} {shot.get('dur', BAR):.2f}s", flush=True)

    base = build_ad.WORK / "base.mp4"
    build_ad.concat(segments, base, total)
    print(f"concatenated ({total:.2f}s)", flush=True)

    build_ad.OVERLAYS = OVERLAYS
    build_ad.GRADE_WIDE = GRADE
    graded = build_ad.WORK / "graded.mp4"
    build_ad.apply_cards(base, graded, w, h, cards_dir, total)
    print("cards + grade applied", flush=True)

    final = ROOT / "out" / f"Dark_Brine_Rot_Ad_v2_{tag}.mp4"
    build_ad.mix(graded, score, final)
    print(f"wrote {final}", flush=True)

    info = subprocess.run([FFMPEG, "-hide_banner", "-i", str(final)],
                          capture_output=True, text=True, errors="replace")
    for line in (info.stderr or "").splitlines():
        if "Duration" in line or "Stream #" in line:
            print(line.strip())
    return 0


if __name__ == "__main__":
    sys.exit(main())
