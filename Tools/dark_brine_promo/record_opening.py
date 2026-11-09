"""Record the opening sequence from the standalone build.

The build ships an opt-in replay tool (OpeningTailReview): it loads the real
Story1 scene, drives the tutorial with synthetic input, renders the gameplay
camera every frame and writes the frames as JPGs. That is real footage of the
game, not a mock-up.

    python record_opening.py out/opening --fps 30

The tool disables physical input devices while it runs and excludes the player
from saving, so it cannot disturb a keyboard or overwrite a save file. It
quits by itself when the sequence ends.
"""
from __future__ import annotations

import json
import subprocess
import sys
import time
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
BUILD = ROOT / ".codex" / "first-island-playtest-20261107" / "build" / "development"
EXE = BUILD / "DarkBrineRot_Playtest.exe"


def main() -> int:
    out = Path(sys.argv[1] if len(sys.argv) > 1 else ROOT / "Tools/dark_brine_promo/out/opening")
    fps = sys.argv[2] if len(sys.argv) > 2 else "30"
    out.parent.mkdir(parents=True, exist_ok=True)
    report = out.with_suffix(".json")

    if not EXE.exists():
        print(f"MISSING_BUILD {EXE}")
        return 1

    for old in report.parent.glob(report.name):
        old.unlink()

    args = [str(EXE), "--tail-review", str(report), "--tail-review-record",
            f"--tail-review-fps={fps}", "-screen-width", "1920", "-screen-height", "1080",
            "-screen-fullscreen", "0"]
    print("LAUNCH " + " ".join(args))
    process = subprocess.Popen(args, cwd=str(BUILD))
    print(f"PID {process.pid}")

    deadline = time.time() + 900
    while time.time() < deadline:
        if report.exists():
            time.sleep(1.5)
            break
        if process.poll() is not None:
            print(f"PROCESS_EXITED {process.returncode}")
            break
        time.sleep(2)

    if not report.exists():
        print("NO_REPORT")
        try:
            process.terminate()
        except Exception:
            pass
        return 1

    try:
        data = json.loads(report.read_text(encoding="utf-8"))
        print(json.dumps({k: data.get(k) for k in
                          ("passed", "error", "frame", "replayFps", "renderedFrames", "images")},
                         indent=1))
    except Exception as e:
        print(f"REPORT_UNREADABLE {e}")

    frames = sorted((report.parent / (report.stem + "-images")).glob("*.jpg"))
    print(f"FRAMES {len(frames)} in {report.parent / (report.stem + '-images')}")
    if frames:
        print(f"FIRST {frames[0].name} LAST {frames[-1].name}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
