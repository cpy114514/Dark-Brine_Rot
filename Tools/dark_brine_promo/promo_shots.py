"""Record real gameplay shots from the running game window.

The island is generated at runtime, so promo footage has to be taken from the
running game. This script drives the real client with synthetic input (the
same keys a player uses), grabs the window with GDI, and verifies every shot
before it is allowed into the edit.

    python promo_shots.py shots.json out_dir

shots.json is a list of shots:

    {"name": "walk", "keys": ["w"], "seconds": 4, "look": [6, 0]}
    {"name": "combat", "attack": true, "seconds": 4}
    {"name": "gear", "press": ["i"], "seconds": 2}

Each shot writes name_0000.jpg ... and a per shot report with the measured
capture rate, the mean brightness and how much the picture actually moved.
A shot that is black, frozen or empty fails the check and is reported as such
instead of quietly ending up in the trailer.
"""
from __future__ import annotations

import json
import sys
import time
from pathlib import Path

import numpy as np
from PIL import Image

from capture_window import (capture_window, enable_dpi_awareness, find_windows,
                            hold_keys, release_keys, mouse_click, move_mouse,
                            move_mouse_abs, focus_window, minimize_window,
                            restore_window, send_key, key_code, user32, wintypes)

TITLE_HINT = "DarkBrineRot_Playtest"


def press(name: str) -> None:
    vk = key_code(name)
    if vk:
        send_key(vk)
        time.sleep(0.08)
        send_key(vk, up=True)


def is_paused(rect: tuple[int, int, int, int], hwnd: int) -> bool:
    """A paused game shows a dark, frozen picture; a dark night scene still moves."""
    x, y, width, height = rect
    try:
        first = capture_window(hwnd, width, height).crop((0, 45, width, min(height, 45 + 1080)))
        time.sleep(0.25)
        second = capture_window(hwnd, width, height).crop((0, 45, width, min(height, 45 + 1080)))
    except Exception:
        return False
    a = np.asarray(first.convert("L"), dtype=np.float32)
    b = np.asarray(second.convert("L"), dtype=np.float32)
    if a.size == 0 or b.size != a.size:
        return False
    dark = float(a.mean()) < 50.0 and float(b.mean()) < 50.0
    still = float(np.abs(a - b).mean()) < 0.3
    return dark and still


def resume_if_paused(rect, hwnd) -> bool:
    """The game pauses itself when it loses the foreground. Recover from that.

    ESC walks back out of whatever menu is open (settings -> pause -> play),
    so it is pressed until the picture is alive again.
    """
    if not is_paused(rect, hwnd):
        return False
    for _ in range(3):
        press("escape")
        time.sleep(0.8)
        if not is_paused(rect, hwnd):
            print("  (resumed the game)")
            return True
    print("  (WARNING: the game still looks paused)")
    return True


def analyse(paths: list[Path], ui: bool = False) -> dict:
    """Brightness and motion statistics for one shot."""
    means, diffs = [], []
    previous = None
    for path in paths:
        array = np.asarray(Image.open(path).convert("L"), dtype=np.int16)
        means.append(float(array.mean()))
        if previous is not None:
            diffs.append(float(np.abs(array - previous).mean()))
        previous = array
    report = {
        "frames": len(paths),
        "mean_brightness": round(float(np.mean(means)), 1) if means else 0.0,
        "min_brightness": round(float(np.min(means)), 1) if means else 0.0,
        "max_brightness": round(float(np.max(means)), 1) if means else 0.0,
        "motion_between_frames": round(float(np.mean(diffs)), 2) if diffs else 0.0,
    }
    report["black"] = report["mean_brightness"] < 6.0
    # A menu shot is supposed to be still, so only the black check applies.
    report["frozen"] = report["motion_between_frames"] < 0.05 and len(paths) > 3 and not ui
    report["ok"] = not report["black"] and not report["frozen"] and len(paths) > 1
    return report


def run_shot(shot: dict, hwnd: int, rect: tuple[int, int, int, int], out_dir: Path,
             fps: float, index: int) -> dict:
    x, y, width, height = rect
    name = shot.get("name", f"shot{index:02d}")
    keys = [k for k in shot.get("keys", []) if k]
    seconds = float(shot.get("seconds", 4))
    look = shot.get("look", [0, 0])
    attack = bool(shot.get("attack"))
    presses = shot.get("press", [])
    folder = out_dir / name
    folder.mkdir(parents=True, exist_ok=True)
    for old in folder.glob("*.jpg"):
        old.unlink()

    if keys:
        hold_keys(keys)
    time.sleep(0.3)
    for key in presses:
        press(key)
        time.sleep(0.2)

    # Clicks must land inside the game window: a click outside it would hand
    # the foreground to another application and the game would pause itself.
    click_x = x + width // 2
    click_y = y + 45 + (min(height, 45 + 1080) - 45) // 2

    interval = 1.0 / max(1.0, fps)
    start = time.time()
    next_frame = start
    next_focus_check = start
    count = 0
    while time.time() - start < seconds:
        now = time.time()
        # The game pauses itself the moment it loses the foreground, so the
        # focus is held for the whole shot and a pause is undone right away.
        if now >= next_focus_check:
            next_focus_check = now + 0.7
            if user32.GetForegroundWindow() != hwnd:
                focus_window(hwnd)
                if is_paused(rect, hwnd):
                    press("escape")
                    time.sleep(0.4)
        if now >= next_frame:
            if attack:
                move_mouse_abs(click_x, click_y)
                mouse_click("left")
            if look[0] or look[1]:
                move_mouse(int(look[0] / fps), int(look[1] / fps))
            try:
                image = capture_window(hwnd, width, height)
                # the window frame is not game: keep only the client area
                image = image.crop((0, 45, width, min(height, 45 + 1080)))
                image.save(folder / f"cap_{count:04d}.jpg", quality=92)
                count += 1
            except Exception as e:  # keep going, the report shows the gap
                print(f"  capture failed: {e}")
            next_frame = now + interval
        time.sleep(0.004)

    if keys:
        release_keys(keys)

    files = sorted(folder.glob("*.jpg"))
    report = analyse(files, ui=bool(shot.get("ui")))
    report["name"] = name
    report["seconds"] = round(seconds, 1)
    report["achieved_fps"] = round(count / seconds, 2) if seconds else 0
    report["folder"] = str(folder)
    report["keys"] = keys
    report["attack"] = attack
    report["press"] = presses
    return report


def main() -> int:
    if len(sys.argv) < 3:
        print(__doc__)
        return 2

    shots = json.loads(Path(sys.argv[1]).read_text(encoding="utf-8"))
    out_dir = Path(sys.argv[2])
    out_dir.mkdir(parents=True, exist_ok=True)
    fps = float(sys.argv[3]) if len(sys.argv) > 3 else 12.0

    enable_dpi_awareness()
    windows = find_windows()
    target = None
    for hwnd, title, x, y, w, h in windows:
        if TITLE_HINT.lower() in title.lower():
            target = (hwnd, (x, y, w, h), title)
            break
    if target is None:
        print("NO_GAME_WINDOW")
        return 1

    hwnd, rect, title = target
    print(f"GAME {title} rect={rect}")

    # The Unity editor is usually maximized over the game. Every click that
    # lands on it would steal the foreground, so it goes out of the way for
    # the duration of the capture and comes back afterwards.
    editor = None
    for other in find_windows(restore=False):
        if other[0] != hwnd and "Unity" in other[1]:
            editor = other[0]
            break
    if editor:
        minimize_window(editor)
        time.sleep(0.4)
        print(f"EDITOR minimized (hwnd={editor})")

    try:
        if not focus_window(hwnd):
            print("WARNING: the game window did not take the foreground")
        reports = []
        for index, shot in enumerate(shots):
            print(f"SHOT {shot.get('name', index)}: {shot}")
            # Every shot starts from a known state: the game owns the foreground
            # and is not sitting in its own pause menu.
            focus_window(hwnd)
            resume_if_paused(rect, hwnd)
            report = run_shot(shot, hwnd, rect, out_dir, fps, index)
            reports.append(report)
            print(f"  -> frames={report['frames']} fps={report['achieved_fps']} "
                  f"bright={report['mean_brightness']} motion={report['motion_between_frames']} "
                  f"ok={report['ok']}")
    finally:
        if editor:
            restore_window(editor)
            print("EDITOR restored")

    summary = out_dir / "shots_report.json"
    summary.write_text(json.dumps(reports, indent=1), encoding="utf-8")
    print(f"WROTE {summary}")
    failed = [r["name"] for r in reports if not r["ok"]]
    if failed:
        print("FAILED_SHOTS " + ",".join(failed))
    return 0


if __name__ == "__main__":
    sys.exit(main())
