"""Assemble the Dark Brine_Rot trailer.

Real gameplay frames captured from the Unity editor are mixed with AI
generated shots, graded to one palette, and topped with the graphic cards.

    python build_trailer.py            # full cut
    python build_trailer.py --preview  # 960x540, faster
"""
from __future__ import annotations

import json
import subprocess
import sys
from pathlib import Path

import imageio_ffmpeg

ROOT = Path(__file__).resolve().parent
ASSETS = ROOT / "assets"
CARDS = ROOT / "out" / "cards"
WORK = ROOT / "out" / "build"
FFMPEG = imageio_ffmpeg.get_ffmpeg_exe()

FPS = 24
W, H = 1920, 1080

# --------------------------------------------------------------------------
# The cut. ``kind`` is video (ai clip), still (gameplay frame) or card.
# ``start``/``dur`` trim the source; ``dur`` is the length on the timeline.
# --------------------------------------------------------------------------
SHOTS = [
    {"kind": "video", "src": ASSETS / "ai" / "test_ocean_01.mp4", "start": 0.4, "dur": 4.0},
    {"kind": "video", "src": ASSETS / "ai" / "wreck_fog.mp4", "start": 0.3, "dur": 3.6},
    {"kind": "video", "src": ASSETS / "ai" / "hailuo_ocean.mp4", "start": 0.4, "dur": 3.2},
    {"kind": "video", "src": ASSETS / "ai" / "i2v_island.mp4", "start": 0.2, "dur": 3.6, "dark": 0.17},
    {"kind": "still", "src": ASSETS / "play" / "live_a.png", "dur": 1.0, "zoom": 1.10},
    {"kind": "video", "src": ASSETS / "ai" / "shark_fin.mp4", "start": 0.2, "dur": 4.0},
    {"kind": "video", "src": ASSETS / "ai" / "beast_rise.mp4", "start": 0.2, "dur": 3.6},
    {"kind": "video", "src": ASSETS / "ai" / "island_approach_fast.mp4", "start": 0.2, "dur": 3.0, "dark": 0.07},
    {"kind": "card", "src": CARDS / "card_club.png", "dur": 1.4},
    {"kind": "card", "src": CARDS / "card_blades.png", "dur": 1.4},
    {"kind": "card", "src": CARDS / "card_rifle.png", "dur": 1.4},
    {"kind": "video", "src": ASSETS / "ai" / "brine_waves.mp4", "start": 0.2, "dur": 2.6, "dark": 0.11},
    {"kind": "card", "src": CARDS / "title.png", "dur": 3.0},
    {"kind": "card", "src": CARDS / "end.png", "dur": 2.4},
]

# Caption overlays on the finished timeline.
OVERLAYS = [
    ("cap_crossing", 0.8, 3.8),
    ("cap_already", 5.2, 7.4),
    ("cap_land", 11.2, 14.0),
    ("cap_boss", 19.8, 22.6),
    ("cap_conspiracy", 30.6, 32.6),
]

GRADE = ("eq=contrast=1.08:saturation=0.80:brightness=-0.035:gamma=0.97,"
         "colorchannelmixer=rr=0.95:gg=0.97:bb=1.12:bg=0.05:br=0.02,"
         "vignette=PI/4.6")


def run(args: list[str]) -> None:
    proc = subprocess.run(args, capture_output=True, text=True, errors="replace")
    if proc.returncode != 0:
        tail = (proc.stderr or "").strip().splitlines()[-12:]
        raise RuntimeError("ffmpeg failed:\n" + "\n".join(tail))


def has_audio(path: Path) -> bool:
    proc = subprocess.run([FFMPEG, "-hide_banner", "-i", str(path)], capture_output=True, text=True, errors="replace")
    return "Audio:" in (proc.stderr or "")


def build_segment(shot: dict, index: int, width: int, height: int) -> Path:
    out = WORK / f"seg_{index:02d}.mp4"
    dur = float(shot["dur"])
    fade = 0.3 if shot["kind"] != "card" else 0.25

    common = ["-y", "-c:v", "libx264", "-preset", "medium", "-crf", "16",
              "-pix_fmt", "yuv420p", "-r", str(FPS), "-c:a", "aac", "-b:a", "192k",
              "-ar", "48000", "-ac", "2", str(out)]

    if shot["kind"] == "video":
        src = Path(shot["src"])
        extra = ""
        dark = float(shot.get("dark", 0.0))
        if dark > 0.0:
            extra = f"eq=contrast=1.05:saturation=0.70:brightness=-{dark:.3f},"
        vf = (f"scale={width}:{height}:force_original_aspect_ratio=increase,"
              f"crop={width}:{height},fps={FPS},setsar=1,{extra}"
              f"fade=t=in:st=0:d={fade},fade=t=out:st={dur - fade:.2f}:d={fade},format=yuv420p")
        args = [FFMPEG, "-v", "error", "-ss", str(shot.get("start", 0)), "-t", str(dur), "-i", str(src)]
        if has_audio(src):
            args += ["-filter_complex", f"[0:v]{vf}[v]", "-map", "[v]", "-map", "0:a:0"]
        else:
            args += ["-filter_complex", f"[0:v]{vf}[v];anullsrc=r=48000:cl=stereo:d={dur}[a]",
                     "-map", "[v]", "-map", "[a]"]
        args += common

    elif shot["kind"] == "still":
        src = Path(shot["src"])
        frames = int(round(dur * FPS))
        zoom = float(shot.get("zoom", 1.12))
        step = (zoom - 1.0) / max(1, frames)
        # d=1 keeps zoompan one output frame per input frame, so the clip length
        # stays exactly dur instead of being multiplied by d.
        vf = (f"scale={int(width * 1.5)}:{int(height * 1.5)},"
              f"zoompan=z='min(zoom+{step:.6f},{zoom})':d=1:"
              f"x='iw/2-(iw/zoom/2)':y='ih/2-(ih/zoom/2)':s={width}x{height}:fps={FPS},"
              f"fade=t=in:st=0:d={fade},fade=t=out:st={dur - fade:.2f}:d={fade},format=yuv420p")
        args = [FFMPEG, "-v", "error", "-loop", "1", "-t", str(dur), "-i", str(src),
                "-f", "lavfi", "-t", str(dur), "-i", "anullsrc=r=48000:cl=stereo",
                "-filter_complex", f"[0:v]{vf}[v];[1:a]anull[a]",
                "-map", "[v]", "-map", "[a]", "-t", f"{dur:.2f}"] + common

    else:  # card
        src = Path(shot["src"])
        vf = (f"scale={width}:{height},fps={FPS},setsar=1,"
              f"fade=t=in:st=0:d={fade},fade=t=out:st={dur - fade:.2f}:d={fade},format=yuv420p")
        args = [FFMPEG, "-v", "error", "-loop", "1", "-t", str(dur), "-i", str(src),
                "-f", "lavfi", "-t", str(dur), "-i", "anullsrc=r=48000:cl=stereo",
                "-filter_complex", f"[0:v]{vf}[v];[1:a]anull[a]",
                "-map", "[v]", "-map", "[a]", "-t", f"{dur:.2f}"] + common

    run(args)
    return out


def concat(segments: list[Path], out: Path) -> None:
    listing = WORK / "concat.txt"
    listing.write_text("".join(f"file '{p.name}'\n" for p in segments), encoding="utf-8")
    run([FFMPEG, "-v", "error", "-y", "-f", "concat", "-safe", "0", "-i", str(listing),
         "-c", "copy", str(out)])

    # A mismatched segment length silently desyncs the whole cut, so verify.
    proc = subprocess.run([FFMPEG, "-hide_banner", "-i", str(out)], capture_output=True, text=True, errors="replace")
    import re
    m = re.search(r"Duration:\s*(\d+):(\d+):(\d+\.\d+)", proc.stderr or "")
    if not m:
        raise RuntimeError("could not read concat duration")
    actual = int(m.group(1)) * 3600 + int(m.group(2)) * 60 + float(m.group(3))
    return actual


def apply_overlays(base: Path, out: Path, width: int, height: int, total: float) -> None:
    args = [FFMPEG, "-v", "error", "-y", "-i", str(base)]
    chains = []
    for i, (name, t0, t1) in enumerate(OVERLAYS):
        card = CARDS / f"{name}.png"
        if not card.exists():
            continue
        args += ["-loop", "1", "-t", f"{total:.2f}", "-i", str(card)]
        # Shift each card onto the main timeline so its fade in/out happens at
        # the right moment instead of right after the image starts.
        chains.append(
            f"[{i + 1}:v]format=rgba,scale={width}:{height},"
            f"setpts=PTS-STARTPTS+{t0:.2f}/TB,"
            f"fade=t=in:st={t0:.2f}:d=0.4:alpha=1,"
            f"fade=t=out:st={max(t0, t1 - 0.4):.2f}:d=0.4:alpha=1[c{i}]"
        )

    if not chains:
        run([FFMPEG, "-v", "error", "-y", "-i", str(base), "-c", "copy", str(out)])
        return

    last = "0:v"
    for i, (name, t0, t1) in enumerate(OVERLAYS):
        if not (CARDS / f"{name}.png").exists():
            continue
        chains.append(
            f"[{last}][c{i}]overlay=0:0:enable='between(t,{t0},{t1})'[v{i}]"
        )
        last = f"v{i}"

    # grade and overlay in a single encode pass
    chains.append(f"[{last}]{GRADE}[graded]")

    args += ["-filter_complex", ";".join(chains), "-map", "[graded]", "-map", "0:a",
             "-c:v", "libx264", "-preset", "veryfast", "-crf", "18", "-pix_fmt", "yuv420p",
             "-c:a", "copy", "-t", f"{total:.2f}", str(out)]
    run(args)


def grade(src: Path, out: Path) -> None:
    run([FFMPEG, "-v", "error", "-y", "-i", str(src),
         "-vf", GRADE, "-c:v", "libx264", "-preset", "fast", "-crf", "17",
         "-pix_fmt", "yuv420p", "-c:a", "copy", str(out)])


def mix_audio(video: Path, score: Path, out: Path) -> None:
    if not score.exists():
        run([FFMPEG, "-v", "error", "-y", "-i", str(video), "-c", "copy", str(out)])
        return

    run([FFMPEG, "-v", "error", "-y", "-i", str(video), "-i", str(score),
         "-filter_complex",
         "[1:a]volume=0.92[score];[0:a]volume=0.24[ambience];"
         "[score][ambience]amix=inputs=2:duration=first:normalize=0,"
         "alimiter=limit=0.89:attack=5:release=60:level=disabled[aout]",
         "-map", "0:v", "-map", "[aout]", "-c:v", "copy",
         "-c:a", "aac", "-b:a", "192k", "-ar", "48000", "-ac", "2", str(out)])


def main() -> int:
    preview = "--preview" in sys.argv
    width, height = (960, 540) if preview else (W, H)
    WORK.mkdir(parents=True, exist_ok=True)

    segments = []
    for i, shot in enumerate(SHOTS):
        seg = build_segment(shot, i, width, height)
        segments.append(seg)
        print(f"seg {i:02d} {shot['kind']:5s} {shot['dur']:.1f}s  {Path(shot['src']).name}")

    total = sum(float(s["dur"]) for s in SHOTS)

    base = WORK / "base.mp4"
    actual = concat(segments, base)
    if abs(actual - total) > 0.75:
        raise RuntimeError(f"concat length mismatch: {actual:.2f}s vs expected {total:.2f}s")
    print(f"concatenated ({actual:.1f}s)", flush=True)

    final_work = WORK / "graded.mp4"
    apply_overlays(base, final_work, width, height, total)
    print(f"overlays + grade applied ({total:.1f}s)", flush=True)

    score = ROOT / "out" / "score_40s.wav"
    final = ROOT / "out" / ("Dark_Brine_Rot_Trailer_preview.mp4" if preview
                            else "Dark_Brine_Rot_Trailer.mp4")
    mix_audio(final_work, score, final)
    print(f"wrote {final}", flush=True)

    info = subprocess.run([FFMPEG, "-hide_banner", "-i", str(final)], capture_output=True, text=True, errors="replace")
    for line in (info.stderr or "").splitlines():
        if "Duration" in line or "Stream #" in line:
            print(line.strip())
    return 0


if __name__ == "__main__":
    sys.exit(main())
