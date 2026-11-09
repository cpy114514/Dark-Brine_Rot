"""Record promo footage from one game process, driven by a small action list.

    python promo_capture.py --pid 12345 flow.json out/raw

Actions in flow.json:

    {"do": "capture", "name": "menu"}                 one frame, saved as a png
    {"do": "click", "x": 960, "y": 540}               click inside the game view
    {"do": "key", "name": "escape", "times": 2}
    {"do": "wait", "seconds": 5}
    {"do": "record", "name": "opening", "seconds": 20}
    {"do": "shots", "name": "walk", "keys": ["w"], "seconds": 4, "look": [3, 0],
     "attack": false, "fps": 12}

Coordinates are given in the game view, with (0, 0) at the top left of the
picture, not the window frame. Every recorded shot is checked afterwards: a
black picture or a frozen one is reported as a failure instead of quietly
becoming part of the trailer.
"""
from __future__ import annotations

import argparse
import json
import sys
import time
from pathlib import Path

import numpy as np
from PIL import Image

from capture_window import (capture_window, enable_dpi_awareness, find_windows,
                            focus_window, hold_keys, key_code, move_mouse,
                            move_mouse_abs, mouse_click, release_keys, send_key,
                            window_pid)

TITLE_BAR = 45          # the window frame above the picture
CLIENT_HEIGHT = 1080    # the picture is 1080 rows high


class Recorder:
    def __init__(self, pid: int | None, out_dir: Path):
        self.out = out_dir
        self.out.mkdir(parents=True, exist_ok=True)
        self.hwnd = None
        self.rect = None
        self.windows = []
        self.select(pid)

    def select(self, pid: int | None) -> None:
        self.windows = find_windows()
        games = [w for w in self.windows if "Playtest" in w[1]]
        if pid:
            games = [w for w in games if window_pid(w[0]) == pid]
        if not games:
            raise SystemExit("NO_GAME_WINDOW")
        # the newest game window is the one that was started last
        chosen = games[-1] if pid is None else games[0]
        self.hwnd, title, x, y, w, h = chosen
        self.rect = (x, y, w, h)
        self.pid = window_pid(self.hwnd)
        print(f"TARGET pid={self.pid} hwnd={self.hwnd} title={title!r} rect={self.rect}",
              flush=True)

    # -- helpers ---------------------------------------------------------
    def focus(self) -> None:
        focus_window(self.hwnd)

    def picture(self) -> Image.Image:
        x, y, w, h = self.rect
        image = capture_window(self.hwnd, w, h)
        return image.crop((0, TITLE_BAR, w, min(h, TITLE_BAR + CLIENT_HEIGHT)))

    def click(self, px: int, py: int) -> None:
        x, y, w, h = self.rect
        self.focus()
        move_mouse_abs(x + px, y + TITLE_BAR + py)
        time.sleep(0.15)
        mouse_click("left")
        print(f"  click ({px},{py})", flush=True)

    def key(self, name: str, times: int = 1) -> None:
        vk = key_code(name)
        if not vk:
            print(f"  unknown key {name}", flush=True)
            return
        self.focus()
        for _ in range(times):
            send_key(vk)
            time.sleep(0.08)
            send_key(vk, up=True)
            time.sleep(0.25)
        print(f"  key {name} x{times}", flush=True)

    # -- actions ---------------------------------------------------------
    def act_capture(self, action: dict) -> None:
        name = action.get("name", "frame")
        image = self.picture()
        path = self.out / f"{name}.png"
        image.save(path)
        array = np.asarray(image.convert("L"), dtype=np.float32)
        print(f"  captured {path} mean={array.mean():.1f}", flush=True)

    def act_record(self, action: dict) -> None:
        name = action.get("name", "rec")
        seconds = float(action.get("seconds", 10))
        fps = float(action.get("fps", 12))
        folder = self.out / name
        folder.mkdir(parents=True, exist_ok=True)
        for old in folder.glob("*.jpg"):
            old.unlink()
        self.focus()
        interval = 1.0 / fps
        start = time.time()
        next_frame = start
        next_focus = start
        count = 0
        while time.time() - start < seconds:
            now = time.time()
            if now >= next_focus:
                next_focus = now + 0.7
                from capture_window import user32
                if user32.GetForegroundWindow() != self.hwnd:
                    self.focus()
            if now >= next_frame:
                try:
                    self.picture().save(folder / f"cap_{count:04d}.jpg", quality=92)
                    count += 1
                except Exception as e:
                    print(f"  capture failed: {e}", flush=True)
                next_frame = now + interval
            time.sleep(0.004)
        print(f"  recorded {count} frames of {name}", flush=True)

    def act_shots(self, action: dict) -> None:
        name = action.get("name", "shot")
        seconds = float(action.get("seconds", 4))
        fps = float(action.get("fps", 12))
        keys = action.get("keys", [])
        look = action.get("look", [0, 0])
        attack = bool(action.get("attack"))
        presses = action.get("press", [])
        folder = self.out / name
        folder.mkdir(parents=True, exist_ok=True)
        for old in folder.glob("*.jpg"):
            old.unlink()

        self.focus()
        x, y, w, h = self.rect
        for key in presses:
            self.key(key)
            time.sleep(float(action.get("press_delay", 0.6)))
        if keys:
            hold_keys(keys)
            time.sleep(0.3)
        interval = 1.0 / fps
        start = time.time()
        next_frame = start
        count = 0
        while time.time() - start < seconds:
            now = time.time()
            if now >= next_frame:
                if attack:
                    move_mouse_abs(x + w // 2, y + TITLE_BAR + CLIENT_HEIGHT // 2)
                    mouse_click("left")
                if look[0] or look[1]:
                    move_mouse(int(look[0] / fps), int(look[1] / fps))
                try:
                    self.picture().save(folder / f"cap_{count:04d}.jpg", quality=92)
                    count += 1
                except Exception as e:
                    print(f"  capture failed: {e}", flush=True)
                next_frame = now + interval
            time.sleep(0.004)
        if keys:
            release_keys(keys)
        print(f"  shot {name}: {count} frames", flush=True)

    def act_click(self, action: dict) -> None:
        self.click(int(action.get("x", 0)), int(action.get("y", 0)))
        time.sleep(float(action.get("delay", 1.0)))

    def act_key(self, action: dict) -> None:
        self.key(action.get("name", "escape"), int(action.get("times", 1)))
        time.sleep(float(action.get("delay", 0.5)))

    def act_wait(self, action: dict) -> None:
        time.sleep(float(action.get("seconds", 1)))

    def act_focus(self, action: dict) -> None:
        self.focus()

    def run(self, flow: list[dict]) -> None:
        handlers = {
            "capture": self.act_capture,
            "record": self.act_record,
            "shots": self.act_shots,
            "click": self.act_click,
            "key": self.act_key,
            "wait": self.act_wait,
            "focus": self.act_focus,
        }
        for index, action in enumerate(flow):
            kind = action.get("do")
            handler = handlers.get(kind)
            print(f"[{index}] {kind} {action}", flush=True)
            if handler is None:
                print("  unknown action", flush=True)
                continue
            handler(action)


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("flow")
    parser.add_argument("out")
    parser.add_argument("--pid", type=int, default=None)
    args = parser.parse_args()

    enable_dpi_awareness()
    flow = json.loads(Path(args.flow).read_text(encoding="utf-8"))
    recorder = Recorder(args.pid, Path(args.out))
    recorder.run(flow)
    print("DONE", flush=True)
    return 0


if __name__ == "__main__":
    sys.exit(main())
