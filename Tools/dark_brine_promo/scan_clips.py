"""Scan every video in the repo and report duration / resolution / fps / size.

Uses the ffmpeg binary shipped with imageio-ffmpeg (no ffprobe needed) and
parses the header line out of stderr.
"""

from __future__ import annotations

import json
import re
import subprocess
import sys
from pathlib import Path

import imageio_ffmpeg

FFMPEG = imageio_ffmpeg.get_ffmpeg_exe()
ROOT = Path(__file__).resolve().parents[2]
SKIP_DIRS = {"Library", "Temp", ".git", "NVIDIA Corporation", "obj", "__pycache__"}

DUR_RE = re.compile(r"Duration:\s*(\d+):(\d+):(\d+\.\d+)")
VID_RE = re.compile(r"Video:\s*(\w+).*?,\s*(\d+)x(\d+)")
FPS_RE = re.compile(r"(\d+(?:\.\d+)?)\s*fps")


def probe(path: Path) -> dict | None:
    proc = subprocess.run(
        [FFMPEG, "-hide_banner", "-i", str(path)],
        capture_output=True,
        text=True,
        errors="replace",
    )
    err = proc.stderr
    dur = vid = fps = None
    m = DUR_RE.search(err)
    if m:
        dur = int(m.group(1)) * 3600 + int(m.group(2)) * 60 + float(m.group(3))
    m = VID_RE.search(err)
    if m:
        vid = (int(m.group(2)), int(m.group(3)))
    m = FPS_RE.search(err)
    if m:
        fps = float(m.group(1))
    if dur is None:
        return None
    return {
        "path": str(path.relative_to(ROOT)),
        "dur": round(dur, 2),
        "w": vid[0] if vid else 0,
        "h": vid[1] if vid else 0,
        "fps": fps,
        "mb": round(path.stat().st_size / 1048576, 2),
    }


def main() -> int:
    clips: list[dict] = []
    for dirpath, dirnames, filenames in os_walk(ROOT):
        dirnames[:] = [d for d in dirnames if d not in SKIP_DIRS]
        for name in filenames:
            if name.lower().endswith((".mp4", ".mov", ".webm", ".avi", ".mkv")):
                p = Path(dirpath) / name
                info = probe(p)
                if info:
                    clips.append(info)
    clips.sort(key=lambda c: -c["dur"])
    out = ROOT / "Tools" / "dark_brine_promo" / "out" / "clip_scan.json"
    out.write_text(json.dumps(clips, indent=1), encoding="utf-8")
    print(f"{len(clips)} clips -> {out}")
    for c in clips[:40]:
        print(f"{c['dur']:7.2f}s {c['w']:5}x{c['h']:<5} {c['fps']}fps {c['mb']:8.2f}MB  {c['path']}")
    return 0


def os_walk(root: Path):
    import os

    return os.walk(root)


if __name__ == "__main__":
    sys.exit(main())
