"""Assemble the short looping ad.

Eight bars of 124 BPM. Every shot starts on a bar line, so the picture cuts
land on the kick drum, and the last bar falls back into the first one for a
seamless loop.

    python build_ad.py                 # 1920x1080
    python build_ad.py --vertical      # 1080x1920
"""
from __future__ import annotations

import subprocess
import sys
from pathlib import Path

import imageio_ffmpeg

from promo import ad_cards, loop_score

ROOT = Path(__file__).resolve().parent
ASSETS = ROOT / "assets"
WORK = ROOT / "out" / "ad_build"
FFMPEG = imageio_ffmpeg.get_ffmpeg_exe()

FPS = 24
BAR = loop_score.BAR          # 1.9355 s
SEG = BAR                     # every shot is exactly one bar

# One bar per shot; the cuts therefore land on the beat.
# "crop_x" moves the 9:16 window sideways (0 = far left, 1 = far right) so the
# subject survives the portrait crop; the shark fin and the breaching whale
# both live on the right of their 16:9 frames.
SHOTS = [
    {"kind": "video", "src": ASSETS / "ai" / "test_ocean_01.mp4", "start": 0.5, "dur": SEG, "dark": 0.0},
    {"kind": "video", "src": ASSETS / "ai" / "shark_fin.mp4", "start": 0.3, "dur": SEG, "dark": 0.0, "crop_x": 0.85},
    {"kind": "video", "src": ASSETS / "ai" / "beast_rise.mp4", "start": 0.3, "dur": SEG, "dark": 0.0, "crop_x": 0.75},
    {"kind": "video", "src": ASSETS / "ai" / "i2v_island.mp4", "start": 0.2, "dur": SEG, "dark": 0.46, "sat": 0.22, "tint": (0.58, 0.78, 1.18)},
    {"kind": "video", "src": ASSETS / "ai" / "brine_waves.mp4", "start": 0.2, "dur": SEG, "dark": 0.14, "sat": 0.78},
    {"kind": "video", "src": ASSETS / "ai" / "hailuo_ocean.mp4", "start": 0.5, "dur": SEG, "dark": 0.0},
    {"kind": "card", "src": "logo"},
    {"kind": "card", "src": "cta"},
]

# Cards dropped on top of the picture, in seconds from the start.
OVERLAYS = [
    ("hook1", 0.15, 1.80),
    ("hook2", BAR + 0.15, BAR + 1.80),
    ("hook3", BAR * 2 + 0.15, BAR * 2 + 1.80),
    ("word_run", BAR * 3 + 0.15, BAR * 3 + 1.80),
    ("word_fight", BAR * 4 + 0.15, BAR * 4 + 1.80),
    ("word_survive", BAR * 5 + 0.15, BAR * 5 + 1.80),
]

GRADE_WIDE = ("eq=contrast=1.14:saturation=1.04:brightness=-0.015:gamma=0.99,"
              "colorchannelmixer=rr=0.95:gg=0.98:bb=1.14:bg=0.06:br=0.03,"
              "vignette=PI/4.2")


def run(args: list[str]) -> None:
    proc = subprocess.run(args, capture_output=True, text=True, errors="replace")
    if proc.returncode != 0:
        tail = (proc.stderr or "").strip().splitlines()[-12:]
        raise RuntimeError("ffmpeg failed:\n" + "\n".join(tail))


def has_audio(path: Path) -> bool:
    proc = subprocess.run([FFMPEG, "-hide_banner", "-i", str(path)], capture_output=True, text=True, errors="replace")
    return "Audio:" in (proc.stderr or "")


def build_segment(shot: dict, index: int, w: int, h: int, cards_dir: Path) -> Path:
    out = WORK / f"seg_{index:02d}.mp4"
    dur = float(shot.get("dur", SEG))
    fade = float(shot.get("fade", 0.16))
    common = ["-y", "-c:v", "libx264", "-preset", "veryfast", "-crf", "17",
              "-pix_fmt", "yuv420p", "-r", str(FPS), "-c:a", "aac", "-b:a", "160k",
              "-ar", "48000", "-ac", "2", "-t", f"{dur:.2f}", str(out)]

    if shot["kind"] == "video":
        src = Path(shot["src"])
        # portrait cuts scale by height and crop a 9:16 window out of the middle
        off = float(shot.get("crop_x", 0.5))
        if h > w:
            fit = f"scale=-2:{h},crop={w}:{h}:x='(in_w-{w})*{off:.3f}':y=0"
        else:
            fit = f"scale={w}:{h}"
        extra = ""
        dark = float(shot.get("dark", 0.0))
        lift = float(shot.get("lift", 0.0))
        sat = float(shot.get("sat", 0.72))
        if dark > 0.0 or lift > 0.0:
            extra = (f"eq=contrast=1.04:saturation={sat:.2f}:"
                     f"brightness={lift - dark:.3f},")
        if shot.get("tint"):
            # pull the picture towards the cold night palette of the rest of the ad
            extra += (f"colorchannelmixer=rr={shot['tint'][0]:.2f}:gg={shot['tint'][1]:.2f}"
                      f":bb={shot['tint'][2]:.2f},")
        vf = (f"{fit},{extra}fps={FPS},setsar=1,"
              f"fade=t=in:st=0:d={fade},fade=t=out:st={dur - fade:.2f}:d={fade},format=yuv420p")
        args = [FFMPEG, "-v", "error", "-ss", str(shot.get("start", 0)), "-t", str(dur), "-i", str(src)]
        if has_audio(src):
            args += ["-filter_complex", f"[0:v]{vf}[v]", "-map", "[v]", "-map", "0:a:0"]
        else:
            args += ["-filter_complex", f"[0:v]{vf}[v];anullsrc=r=48000:cl=stereo:d={dur}[a]",
                     "-map", "[v]", "-map", "[a]"]
        args += common
    else:
        src = cards_dir / f"{shot['src']}.png"
        vf = (f"scale={w}:{h},fps={FPS},setsar=1,"
              f"fade=t=in:st=0:d={fade},fade=t=out:st={dur - fade:.2f}:d={fade},format=yuv420p")
        args = [FFMPEG, "-v", "error", "-loop", "1", "-t", str(dur), "-i", str(src),
                "-f", "lavfi", "-t", str(dur), "-i", "anullsrc=r=48000:cl=stereo",
                "-filter_complex", f"[0:v]{vf}[v];[1:a]anull[a]",
                "-map", "[v]", "-map", "[a]"] + common

    run(args)
    return out


def concat(segments: list[Path], out: Path, total: float) -> None:
    listing = WORK / "concat.txt"
    listing.write_text("".join(f"file '{p.name}'\n" for p in segments), encoding="utf-8")
    run([FFMPEG, "-v", "error", "-y", "-f", "concat", "-safe", "0", "-i", str(listing),
         "-c", "copy", str(out)])

    proc = subprocess.run([FFMPEG, "-hide_banner", "-i", str(out)], capture_output=True, text=True, errors="replace")
    import re
    m = re.search(r"Duration:\s*(\d+):(\d+):(\d+\.\d+)", proc.stderr or "")
    if not m:
        raise RuntimeError("could not read concat duration")
    actual = int(m.group(1)) * 3600 + int(m.group(2)) * 60 + float(m.group(3))
    if abs(actual - total) > 0.4:
        raise RuntimeError(f"concat length mismatch: {actual:.2f}s vs {total:.2f}s")


def apply_cards(base: Path, out: Path, w: int, h: int, cards_dir: Path, total: float) -> None:
    args = [FFMPEG, "-v", "error", "-y", "-i", str(base)]
    chains = []
    for i, (name, t0, t1) in enumerate(OVERLAYS):
        card = cards_dir / f"{name}.png"
        if not card.exists():
            continue
        args += ["-loop", "1", "-t", f"{total:.2f}", "-i", str(card)]
        chains.append(
            f"[{i + 1}:v]format=rgba,scale={w}:{h},"
            f"setpts=PTS-STARTPTS+{t0:.2f}/TB,"
            f"fade=t=in:st={t0:.2f}:d=0.18:alpha=1,"
            f"fade=t=out:st={max(t0, t1 - 0.18):.2f}:d=0.18:alpha=1[c{i}]"
        )

    last = "0:v"
    for i, (name, t0, t1) in enumerate(OVERLAYS):
        if not (cards_dir / f"{name}.png").exists():
            continue
        chains.append(f"[{last}][c{i}]overlay=0:0:enable='between(t,{t0},{t1})'[v{i}]")
        last = f"v{i}"

    chains.append(f"[{last}]{GRADE_WIDE}[graded]")
    args += ["-filter_complex", ";".join(chains), "-map", "[graded]", "-map", "0:a",
             "-c:v", "libx264", "-preset", "veryfast", "-crf", "18", "-pix_fmt", "yuv420p",
             "-c:a", "copy", "-t", f"{total:.2f}", str(out)]
    run(args)


def mix(video: Path, score: Path, out: Path) -> None:
    # loudnorm alone leaves peaks above 0 dBFS when the generated clips carry
    # their own audio, so the chain ends on a limiter as well.
    run([FFMPEG, "-v", "error", "-y", "-i", str(video), "-i", str(score),
         "-filter_complex",
         "[1:a]volume=1.0[score];[0:a]volume=0.16[ambience];"
         "[score][ambience]amix=inputs=2:duration=first:normalize=0,"
         "loudnorm=I=-13:TP=-2.0:LRA=9,"
         "alimiter=limit=0.891:attack=5:release=50:level=disabled[aout]",
         "-map", "0:v", "-map", "[aout]", "-c:v", "copy",
         "-c:a", "aac", "-b:a", "192k", "-ar", "48000", "-ac", "2", str(out)])


def main() -> int:
    global WORK
    vertical = "--vertical" in sys.argv
    w, h = (1080, 1920) if vertical else (1920, 1080)
    tag = "vertical" if vertical else "wide"
    WORK = ROOT / "out" / "ad_build" / tag
    WORK.mkdir(parents=True, exist_ok=True)

    cards_dir = ad_cards.OUT / f"{w}x{h}"
    ad_cards.build_all(w, h, tag)

    score = ROOT / "out" / f"ad_loop_score_{tag}.wav"
    loop_score.write_score(score)

    total = len(SHOTS) * SEG
    segments = []
    for i, shot in enumerate(SHOTS):
        seg = build_segment(shot, i, w, h, cards_dir)
        segments.append(seg)
        print(f"seg {i} {shot['kind']:5s} {shot.get('dur', SEG):.2f}s", flush=True)

    base = WORK / "base.mp4"
    concat(segments, base, total)
    print(f"concatenated ({total:.2f}s)", flush=True)

    graded = WORK / "graded.mp4"
    apply_cards(base, graded, w, h, cards_dir, total)
    print("cards + grade applied", flush=True)

    final = ROOT / "out" / f"Dark_Brine_Rot_Ad_{tag}.mp4"
    mix(graded, score, final)
    print(f"wrote {final}", flush=True)

    info = subprocess.run([FFMPEG, "-hide_banner", "-i", str(final)], capture_output=True, text=True, errors="replace")
    for line in (info.stderr or "").splitlines():
        if "Duration" in line or "Stream #" in line:
            print(line.strip())
    return 0


if __name__ == "__main__":
    sys.exit(main())
