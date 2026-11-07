"""MP4 encoding through the ffmpeg binary bundled with imageio-ffmpeg."""
from __future__ import annotations

import shutil
import subprocess
import sys
from pathlib import Path
from typing import Iterable, Optional


def ffmpeg_exe() -> str:
    """Return a usable ffmpeg path (bundled binary first, then PATH)."""
    try:
        import imageio_ffmpeg

        exe = imageio_ffmpeg.get_ffmpeg_exe()
        if exe and Path(exe).exists():
            return exe
    except Exception:
        pass

    found = shutil.which("ffmpeg")
    if found:
        return found

    raise RuntimeError(
        "ffmpeg not found. Install it with:  python -m pip install imageio-ffmpeg"
    )


def encode_video(
    frames: Iterable,
    out_path: Path,
    width: int,
    height: int,
    fps: int,
    audio_path: Optional[Path] = None,
    crf: int = 18,
    preset: str = "medium",
    total_frames: Optional[int] = None,
    log_every: int = 24,
) -> Path:
    """Pipe RGB PIL frames into ffmpeg and write an H.264 MP4.

    ``frames`` must yield PIL images of exactly ``width`` x ``height``.
    If ``audio_path`` is given it is muxed as AAC and trimmed with -shortest.
    """
    exe = ffmpeg_exe()
    out_path = Path(out_path)
    out_path.parent.mkdir(parents=True, exist_ok=True)

    cmd = [
        exe,
        "-y",
        "-hide_banner",
        "-loglevel",
        "error",
        "-f",
        "rawvideo",
        "-vcodec",
        "rawvideo",
        "-pix_fmt",
        "rgb24",
        "-s",
        f"{width}x{height}",
        "-r",
        str(fps),
        "-i",
        "-",
    ]
    if audio_path is not None and Path(audio_path).exists():
        cmd += ["-i", str(audio_path)]

    cmd += [
        "-c:v",
        "libx264",
        "-preset",
        preset,
        "-crf",
        str(crf),
        "-pix_fmt",
        "yuv420p",
        "-movflags",
        "+faststart",
    ]
    if audio_path is not None and Path(audio_path).exists():
        cmd += ["-c:a", "aac", "-b:a", "192k", "-shortest"]
    cmd += [str(out_path)]

    proc = subprocess.Popen(
        cmd,
        stdin=subprocess.PIPE,
        stdout=subprocess.DEVNULL,
        stderr=subprocess.PIPE,
    )

    written = 0
    try:
        assert proc.stdin is not None
        for i, frame in enumerate(frames):
            if frame.mode != "RGB":
                frame = frame.convert("RGB")
            if frame.size != (width, height):
                frame = frame.resize((width, height))
            proc.stdin.write(frame.tobytes())
            written = i + 1
            if log_every and written % log_every == 0:
                if total_frames:
                    pct = 100.0 * written / total_frames
                    sys.stdout.write(f"\r  frames {written}/{total_frames}  {pct:5.1f}%")
                else:
                    sys.stdout.write(f"\r  frames {written}")
                sys.stdout.flush()
        proc.stdin.close()
        proc.wait(timeout=600)
    except BrokenPipeError:
        pass
    finally:
        if proc.poll() is None:
            proc.kill()

    sys.stdout.write("\n")
    if proc.returncode not in (0, None):
        err = proc.stderr.read().decode("utf-8", "replace") if proc.stderr else ""
        raise RuntimeError(f"ffmpeg failed ({proc.returncode}):\n{err[-4000:]}")
    if written == 0:
        raise RuntimeError("no frames were written")
    return out_path
