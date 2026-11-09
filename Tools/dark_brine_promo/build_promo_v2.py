"""Assemble the Dark Brine_Rot promo from real gameplay and AI shots.

Real footage comes from the standalone build (out/raw/<name>/cap_*.jpg), AI
shots live in assets/ai and assets/ai2, and the graphic cards in out/cards.

    python build_promo_v2.py            # full 1920x1080 cut
    python build_promo_v2.py --preview  # 960x540, faster

The cut is defined in one place: SHOTS and OVERLAYS at the top of this file.
"""
from __future__ import annotations

import json
import re
import subprocess
import sys
from pathlib import Path

import imageio_ffmpeg

ROOT = Path(__file__).resolve().parent
ASSETS = ROOT / "assets"
CARDS = ROOT / "out" / "cards"
RAW = ROOT / "out" / "raw"
WORK = ROOT / "out" / "build_v2"
FFMPEG = imageio_ffmpeg.get_ffmpeg_exe()

FPS = 24
W, H = 1920, 1080

# --------------------------------------------------------------------------
# The cut.
#   kind "seq"    real gameplay frames, a folder of jpgs under out/raw
#   kind "video"  an AI generated clip
#   kind "still"  a single real frame, pushed in with a slow zoom
#   kind "card"   a graphic card
#   skip          the first N seconds of the source
#   dur           the length on the timeline
# --------------------------------------------------------------------------
SHOTS = [
    # the crossing: the sea, the wreck, something following the raft
    {"kind": "video", "src": ASSETS / "ai" / "test_ocean_01.mp4", "start": 0.4, "dur": 3.6},
    {"kind": "video", "src": ASSETS / "ai" / "wreck_fog.mp4", "start": 0.3, "dur": 3.6},
    {"kind": "video", "src": ASSETS / "ai" / "shark_fin.mp4", "start": 0.2, "dur": 3.8},
    # landfall: the island, in the real game
    {"kind": "seq", "src": "island_walk", "dur": 2.8, "look": "dusk"},
    {"kind": "seq", "src": "island_sprint", "dur": 2.8, "look": "dusk"},
    # the fight: the real first boss, then the real player
    {"kind": "seq", "src": "boss_fight", "dur": 5.4, "look": "dusk", "smooth": "mci"},
    {"kind": "seq", "src": "island_fight", "dur": 3.4, "look": "dusk", "smooth": "mci"},
    {"kind": "video", "src": ASSETS / "ai2" / "claw.mp4", "start": 0.0, "dur": 2.6},
    {"kind": "video", "src": ASSETS / "ai2" / "stare.mp4", "start": 0.0, "dur": 1.8},
    # the arsenal
    {"kind": "card", "src": CARDS / "card_club.png", "dur": 1.3},
    {"kind": "card", "src": CARDS / "card_blades.png", "dur": 1.3},
    {"kind": "card", "src": CARDS / "card_rifle.png", "dur": 1.3},
    {"kind": "video", "src": ASSETS / "ai" / "brine_waves.mp4", "start": 0.2, "dur": 2.2, "dark": 0.11},
    # title and link
    {"kind": "card", "src": CARDS / "title.png", "dur": 2.4},
    {"kind": "card", "src": CARDS / "end.png", "dur": 1.6},
]

OVERLAYS = [
    ("cap_crossing", 0.8, 4.0),
    ("cap_already", 4.4, 7.0),
    ("cap_land", 12.0, 14.6),
    ("cap_boss", 19.4, 22.4),
    ("cap_conspiracy", 25.6, 28.8),
]

GRADE = ("eq=contrast=1.08:saturation=0.80:brightness=-0.035:gamma=0.97,"
         "colorchannelmixer=rr=0.95:gg=0.97:bb=1.12:bg=0.05:br=0.02,"
         "vignette=PI/4.6")

# The island is shot in daylight while the sea is shot at night. This pulls
# the daylight footage toward the trailer's palette instead of cutting black
# to bright in the middle of the story.
DUSK = ("eq=contrast=1.06:saturation=0.74:brightness=-0.075:gamma=0.94,"
        "colorbalance=rs=-0.06:gs=-0.02:bs=0.06:rm=-0.03:gm=0.01:bm=0.10,"
        "colorchannelmixer=rr=0.93:gg=0.98:bb=1.10")


def run(args: list[str]) -> None:
    proc = subprocess.run(args, capture_output=True, text=True, errors="replace")
    if proc.returncode != 0:
        tail = (proc.stderr or "").strip().splitlines()[-14:]
        raise RuntimeError("ffmpeg failed:\n" + "\n".join(tail))


def has_audio(path: Path) -> bool:
    proc = subprocess.run([FFMPEG, "-hide_banner", "-i", str(path)], capture_output=True,
                          text=True, errors="replace")
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

    elif shot["kind"] == "seq":
        folder = Path(shot["src"])
        if not folder.is_absolute():
            folder = RAW / folder
        frames = sorted(folder.glob("cap_*.jpg"))
        if not frames:
            raise RuntimeError(f"no frames in {folder}")
        source_fps = float(shot.get("fps", 12))
        # a sequence is captured at a lower rate than the cut, so the frames
        # are interpolated up to the timeline rate
        mode = shot.get("smooth", "mci" if dur <= 4 else "blend")
        smooth = (f"minterpolate=fps={FPS}:mi_mode=mci:mc_mode=aobmc:me_mode=bidir"
                  if mode == "mci" else f"minterpolate=fps={FPS}:mi_mode=blend")
        look = DUSK + "," if shot.get("look") == "dusk" else ""
        vf = (f"scale=-2:{height},crop={width}:{height},fps={FPS},"
              + smooth + ",setsar=1," + look +
              f"fade=t=in:st=0:d={fade},fade=t=out:st={dur - fade:.2f}:d={fade},format=yuv420p")
        args = [FFMPEG, "-v", "error", "-framerate", str(source_fps),
                "-start_number", str(frames[0].stem.split("_")[-1]),
                "-i", str(folder / "cap_%04d.jpg"),
                "-filter_complex", f"[0:v]{vf}[v];anullsrc=r=48000:cl=stereo:d={dur}[a]",
                "-map", "[v]", "-map", "[a]", "-t", f"{dur:.2f}"] + common

    elif shot["kind"] == "still":
        src = Path(shot["src"])
        frames = int(round(dur * FPS))
        zoom = float(shot.get("zoom", 1.12))
        step = (zoom - 1.0) / max(1, frames)
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


def concat(segments: list[Path], out: Path) -> float:
    listing = WORK / "concat.txt"
    listing.write_text("".join(f"file '{p.name}'\n" for p in segments), encoding="utf-8")
    run([FFMPEG, "-v", "error", "-y", "-f", "concat", "-safe", "0", "-i", str(listing),
         "-c", "copy", str(out)])
    proc = subprocess.run([FFMPEG, "-hide_banner", "-i", str(out)], capture_output=True,
                          text=True, errors="replace")
    m = re.search(r"Duration:\s*(\d+):(\d+):(\d+\.\d+)", proc.stderr or "")
    if not m:
        raise RuntimeError("could not read concat duration")
    return int(m.group(1)) * 3600 + int(m.group(2)) * 60 + float(m.group(3))


def apply_overlays(base: Path, out: Path, width: int, height: int, total: float) -> None:
    args = [FFMPEG, "-v", "error", "-y", "-i", str(base)]
    chains = []
    used = []
    for i, (name, t0, t1) in enumerate(OVERLAYS):
        card = CARDS / f"{name}.png"
        if not card.exists():
            continue
        used.append((name, t0, t1))
        args += ["-loop", "1", "-t", f"{total:.2f}", "-i", str(card)]
        chains.append(
            f"[{len(used)}:v]format=rgba,scale={width}:{height},"
            f"setpts=PTS-STARTPTS+{t0:.2f}/TB,"
            f"fade=t=in:st={t0:.2f}:d=0.4:alpha=1,"
            f"fade=t=out:st={max(t0, t1 - 0.4):.2f}:d=0.4:alpha=1[c{len(used) - 1}]"
        )

    if not chains:
        run([FFMPEG, "-v", "error", "-y", "-i", str(base), "-c", "copy", str(out)])
        return

    # A dim plate under every caption: the type is light on transparent, and
    # the island footage is bright enough to swallow it otherwise.
    black_index = len(used) + 1
    args += ["-f", "lavfi", "-t", f"{total:.2f}",
             "-i", f"color=c=black:s={width}x{height}:r={FPS}"]
    chains.append(f"[{black_index}:v]format=rgba,colorchannelmixer=aa=0.30[plate]")

    last = "0:v"
    for i in range(len(used)):
        name, t0, t1 = used[i]
        chains.append(
            f"[{last}][plate]overlay=0:0:enable='between(t,{t0},{t1})'[s{i}]"
        )
        chains.append(f"[s{i}][c{i}]overlay=0:0:enable='between(t,{t0},{t1})'[v{i}]")
        last = f"v{i}"
    chains.append(f"[{last}]{GRADE}[graded]")

    args += ["-filter_complex", ";".join(chains), "-map", "[graded]", "-map", "0:a",
             "-c:v", "libx264", "-preset", "veryfast", "-crf", "18", "-pix_fmt", "yuv420p",
             "-c:a", "copy", "-t", f"{total:.2f}", str(out)]
    run(args)


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


def wav_length(path: Path) -> float:
    import wave
    with wave.open(str(path), "rb") as handle:
        return handle.getnframes() / float(handle.getframerate())


def main() -> int:
    preview = "--preview" in sys.argv
    width, height = (960, 540) if preview else (W, H)
    WORK.mkdir(parents=True, exist_ok=True)

    segments = []
    for i, shot in enumerate(SHOTS):
        seg = build_segment(shot, i, width, height)
        segments.append(seg)
        print(f"seg {i:02d} {shot['kind']:5s} {shot['dur']:.1f}s  {Path(str(shot['src'])).name}",
              flush=True)

    total = sum(float(s["dur"]) for s in SHOTS)
    score = ROOT / "out" / "score_40s.wav"
    if score.exists():
        # the score is a fixed bed: a cut that does not match it would drift
        # out of the music, so the length is checked before anything is built
        seconds = wav_length(score)
        if abs(seconds - total) > 0.3:
            raise RuntimeError(f"cut is {total:.2f}s but the score is {seconds:.2f}s")
    base = WORK / "base.mp4"
    actual = concat(segments, base)
    if abs(actual - total) > 0.75:
        raise RuntimeError(f"concat length mismatch: {actual:.2f}s vs expected {total:.2f}s")
    print(f"concatenated ({actual:.1f}s)", flush=True)

    graded = WORK / "graded.mp4"
    apply_overlays(base, graded, width, height, total)
    print(f"overlays + grade applied ({total:.1f}s)", flush=True)

    score = ROOT / "out" / "score_40s.wav"
    final = ROOT / "out" / ("Dark_Brine_Rot_Promo_v2_preview.mp4" if preview
                            else "Dark_Brine_Rot_Promo_v2.mp4")
    mix_audio(graded, score, final)
    print(f"wrote {final}", flush=True)

    info = subprocess.run([FFMPEG, "-hide_banner", "-i", str(final)], capture_output=True,
                          text=True, errors="replace")
    for line in (info.stderr or "").splitlines():
        if "Duration" in line or "Stream #" in line:
            print(line.strip())
    return 0


if __name__ == "__main__":
    sys.exit(main())
