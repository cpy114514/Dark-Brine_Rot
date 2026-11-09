"""Cut a time range out of a captured frame folder.

    python slice_frames.py out/raw/03_opening out/raw/boss_fight 0 7.5

The frames were captured at a fixed rate, so a time range is just a range of
file names. The result is a folder the trailer builder can treat as its own
shot.
"""
from __future__ import annotations

import shutil
import sys
from pathlib import Path


def main() -> int:
    if len(sys.argv) < 5:
        print(__doc__)
        return 2
    src = Path(sys.argv[1])
    dst = Path(sys.argv[2])
    start = float(sys.argv[3])
    end = float(sys.argv[4])
    fps = float(sys.argv[5]) if len(sys.argv) > 5 else 12.0

    files = sorted(src.glob("cap_*.jpg"))
    if not files:
        print(f"NO_FRAMES {src}")
        return 1

    first = max(0, int(round(start * fps)))
    last = min(len(files), int(round(end * fps)))
    picked = files[first:last]
    if not picked:
        print("EMPTY_RANGE")
        return 1

    dst.mkdir(parents=True, exist_ok=True)
    for old in dst.glob("cap_*.jpg"):
        old.unlink()
    for index, path in enumerate(picked):
        shutil.copyfile(path, dst / f"cap_{index:04d}.jpg")
    print(f"COPIED {len(picked)} frames -> {dst} ({len(picked) / fps:.1f}s)")
    return 0


if __name__ == "__main__":
    sys.exit(main())
