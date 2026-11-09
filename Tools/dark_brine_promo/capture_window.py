"""Capture the game window and drive it with real input.

The promo needs real gameplay footage. The project ships a standalone player
build, so the footage is taken from the running game window instead of the
editor: full framerate, the real HUD, the real lighting.

    python capture_window.py probe            # list candidate windows
    python capture_window.py shot out.png     # one frame of the game window
    python capture_window.py session <dir> --seconds 12 --keys w
    python capture_window.py session <dir> --seconds 12 --keys w --sprint

Windows are captured with GDI: PrintWindow first (it follows the window even
when it is not the top window), and a screen-region grab as the fallback.
DPI awareness is set before any of it, or the grab comes back in logical
pixels and the image is cropped.
"""
from __future__ import annotations

import argparse
import ctypes
import ctypes.wintypes as wintypes
import os
import sys
import time
from pathlib import Path

from PIL import Image

user32 = ctypes.windll.user32
gdi32 = ctypes.windll.gdi32
try:
    shcore = ctypes.windll.shcore
except OSError:
    shcore = None

TITLE_HINTS = ("DarkBrineRot", "Dark Brine")


def enable_dpi_awareness() -> None:
    """Make the process see physical pixels, not the scaled logical ones."""
    try:
        user32.SetProcessDpiAwarenessContext(ctypes.c_void_p(-4))  # PER_MONITOR_AWARE_V2
        return
    except Exception:
        pass
    try:
        if shcore is not None:
            shcore.SetProcessDpiAwareness(2)  # PROCESS_PER_MONITOR_DPI_AWARE
            return
    except Exception:
        pass
    try:
        user32.SetProcessDPIAware()
    except Exception:
        pass


class BITMAPINFOHEADER(ctypes.Structure):
    _fields_ = [
        ("biSize", wintypes.DWORD),
        ("biWidth", ctypes.c_long),
        ("biHeight", ctypes.c_long),
        ("biPlanes", wintypes.WORD),
        ("biBitCount", wintypes.WORD),
        ("biCompression", wintypes.DWORD),
        ("biSizeImage", wintypes.DWORD),
        ("biXPelsPerMeter", ctypes.c_long),
        ("biYPelsPerMeter", ctypes.c_long),
        ("biClrUsed", wintypes.DWORD),
        ("biClrImportant", wintypes.DWORD),
    ]


class BITMAPINFO(ctypes.Structure):
    _fields_ = [("bmiHeader", BITMAPINFOHEADER), ("bmiColors", wintypes.DWORD * 3)]


def find_windows(restore: bool = True) -> list[tuple[int, str, int, int, int, int]]:
    """Return (hwnd, title, x, y, w, h) for every visible top level window.

    A minimized window has a tiny rectangle and would be filtered out, so it
    is restored first: the game auto-pauses when it loses the foreground and
    a minimized window never renders anything.
    """
    found = []
    EnumProc = ctypes.WINFUNCTYPE(wintypes.BOOL, wintypes.HWND, wintypes.LPARAM)

    def callback(hwnd, _lparam):
        length = user32.GetWindowTextLengthW(hwnd)
        if length == 0:
            return True
        buf = ctypes.create_unicode_buffer(length + 1)
        user32.GetWindowTextW(hwnd, buf, length + 1)
        title = buf.value
        if not any(hint.lower() in title.lower() for hint in TITLE_HINTS):
            return True

        if restore and user32.IsIconic(hwnd):
            user32.ShowWindow(hwnd, 9)  # SW_RESTORE
            time.sleep(0.4)

        if not user32.IsWindowVisible(hwnd):
            return True
        rect = wintypes.RECT()
        user32.GetWindowRect(hwnd, ctypes.byref(rect))
        w = rect.right - rect.left
        h = rect.bottom - rect.top
        if w > 200 and h > 200:
            found.append((hwnd, title, rect.left, rect.top, w, h))
        return True

    user32.EnumWindows(EnumProc(callback), 0)
    return found


def window_pid(hwnd: int) -> int:
    pid = wintypes.DWORD(0)
    user32.GetWindowThreadProcessId(hwnd, ctypes.byref(pid))
    return int(pid.value)


def capture_window(hwnd: int, width: int, height: int) -> Image.Image:
    """Grab a window with PrintWindow (PW_RENDERFULLCONTENT)."""
    hdc = user32.GetWindowDC(hwnd)
    mem_dc = gdi32.CreateCompatibleDC(hdc)
    bmp = gdi32.CreateCompatibleBitmap(hdc, width, height)
    gdi32.SelectObject(mem_dc, bmp)
    # 2 == PW_RENDERFULLCONTENT, needed for DirectX windows.
    ok = user32.PrintWindow(hwnd, mem_dc, 2)
    if not ok:
        user32.PrintWindow(hwnd, mem_dc, 0)

    info = BITMAPINFO()
    info.bmiHeader.biSize = ctypes.sizeof(BITMAPINFOHEADER)
    info.bmiHeader.biWidth = width
    info.bmiHeader.biHeight = -height
    info.bmiHeader.biPlanes = 1
    info.bmiHeader.biBitCount = 32
    info.bmiHeader.biCompression = 0

    buffer = ctypes.create_string_buffer(width * height * 4)
    gdi32.GetDIBits(mem_dc, bmp, 0, height, buffer, ctypes.byref(info), 0)

    gdi32.DeleteObject(bmp)
    gdi32.DeleteDC(mem_dc)
    user32.ReleaseDC(hwnd, hdc)

    image = Image.frombuffer("RGBA", (width, height), buffer, "raw", "BGRA", 0, 1)
    return image.convert("RGB")


def capture_screen_region(x: int, y: int, width: int, height: int) -> Image.Image:
    hdc = user32.GetDC(0)
    mem_dc = gdi32.CreateCompatibleDC(hdc)
    bmp = gdi32.CreateCompatibleBitmap(hdc, width, height)
    gdi32.SelectObject(mem_dc, bmp)

    class SRCT(ctypes.Structure):
        _fields_ = [("x", ctypes.c_long), ("y", ctypes.c_long),
                    ("cx", ctypes.c_long), ("cy", ctypes.c_long)]

    src = SRCT(x, y, width, height)
    dst = SRCT(0, 0, width, height)
    gdi32.BitBlt(mem_dc, 0, 0, width, height, hdc, x, y, 0x00CC0020)

    info = BITMAPINFO()
    info.bmiHeader.biSize = ctypes.sizeof(BITMAPINFOHEADER)
    info.bmiHeader.biWidth = width
    info.bmiHeader.biHeight = -height
    info.bmiHeader.biPlanes = 1
    info.bmiHeader.biBitCount = 32
    info.bmiHeader.biCompression = 0
    buffer = ctypes.create_string_buffer(width * height * 4)
    gdi32.GetDIBits(mem_dc, bmp, 0, height, buffer, ctypes.byref(info), 0)

    gdi32.DeleteObject(bmp)
    gdi32.DeleteDC(mem_dc)
    user32.ReleaseDC(0, hdc)
    image = Image.frombuffer("RGBA", (width, height), buffer, "raw", "BGRA", 0, 1)
    return image.convert("RGB")


# --------------------------------------------------------------------------
# Synthetic input
# --------------------------------------------------------------------------
KEY_CODES = {
    "w": 0x57, "a": 0x41, "s": 0x53, "d": 0x44,
    "shift": 0x10, "space": 0x20, "ctrl": 0x11, "e": 0x45, "h": 0x48, "m": 0x4D,
    "i": 0x49, "r": 0x52, "f": 0x46, "q": 0x51, "g": 0x47, "p": 0x50, "c": 0x43,
    "left": 0x25, "up": 0x26, "right": 0x27, "down": 0x28,
    "escape": 0x1B, "esc": 0x1B, "tab": 0x09, "enter": 0x0D,
}


def key_code(name: str) -> int:
    """Virtual key code for a named key or a single letter."""
    if not name:
        return 0
    low = name.lower()
    if low in KEY_CODES:
        return KEY_CODES[low]
    if len(low) == 1 and "a" <= low <= "z":
        return ord(low.upper())
    if len(low) == 1 and "0" <= low <= "9":
        return ord(low)
    return 0

INPUT_KEYBOARD = 1
INPUT_MOUSE = 0
KEYEVENTF_KEYUP = 0x0002
MOUSEEVENTF_MOVE = 0x0001
MOUSEEVENTF_LEFTDOWN = 0x0002
MOUSEEVENTF_LEFTUP = 0x0004
MOUSEEVENTF_RIGHTDOWN = 0x0008
MOUSEEVENTF_RIGHTUP = 0x0010


class KEYBDINPUT(ctypes.Structure):
    _fields_ = [("wVk", wintypes.WORD), ("wScan", wintypes.WORD),
                ("dwFlags", wintypes.DWORD), ("time", wintypes.DWORD),
                ("dwExtraInfo", ctypes.POINTER(ctypes.c_ulong))]


class MOUSEINPUT(ctypes.Structure):
    _fields_ = [("dx", ctypes.c_long), ("dy", ctypes.c_long),
                ("mouseData", wintypes.DWORD), ("dwFlags", wintypes.DWORD),
                ("time", wintypes.DWORD), ("dwExtraInfo", ctypes.POINTER(ctypes.c_ulong))]


class HARDWAREINPUT(ctypes.Structure):
    _fields_ = [("uMsg", wintypes.DWORD), ("wParamL", wintypes.WORD),
                ("wParamH", wintypes.WORD)]


class INPUTUNION(ctypes.Union):
    _fields_ = [("ki", KEYBDINPUT), ("mi", MOUSEINPUT), ("hi", HARDWAREINPUT)]


class INPUT(ctypes.Structure):
    _fields_ = [("type", wintypes.DWORD), ("union", INPUTUNION)]


def send_key(vk: int, up: bool = False) -> None:
    extra = ctypes.POINTER(ctypes.c_ulong)()
    code = INPUT()
    code.type = INPUT_KEYBOARD
    code.union.ki = KEYBDINPUT(wVk=vk, wScan=0, dwFlags=KEYEVENTF_KEYUP if up else 0,
                                time=0, dwExtraInfo=extra)
    user32.SendInput(1, ctypes.byref(code), ctypes.sizeof(INPUT))


def hold_keys(names: list[str]) -> None:
    for name in names:
        vk = key_code(name)
        if vk:
            send_key(vk)


def release_keys(names: list[str]) -> None:
    for name in names:
        vk = key_code(name)
        if vk:
            send_key(vk, up=True)


def mouse_click(button: str = "left") -> None:
    down, up = (MOUSEEVENTF_LEFTDOWN, MOUSEEVENTF_LEFTUP) if button == "left" \
        else (MOUSEEVENTF_RIGHTDOWN, MOUSEEVENTF_RIGHTUP)
    for flags in (down, up):
        code = INPUT()
        code.type = INPUT_MOUSE
        code.union.mi = MOUSEINPUT(dx=0, dy=0, mouseData=0, dwFlags=flags,
                                   time=0, dwExtraInfo=ctypes.POINTER(ctypes.c_ulong)())
        user32.SendInput(1, ctypes.byref(code), ctypes.sizeof(INPUT))


def move_mouse(dx: int, dy: int) -> None:
    code = INPUT()
    code.type = INPUT_MOUSE
    code.union.mi = MOUSEINPUT(dx=dx, dy=dy, mouseData=0, dwFlags=MOUSEEVENTF_MOVE,
                               time=0, dwExtraInfo=ctypes.POINTER(ctypes.c_ulong)())
    user32.SendInput(1, ctypes.byref(code), ctypes.sizeof(INPUT))


MOUSEEVENTF_ABSOLUTE = 0x8000


def move_mouse_abs(sx: int, sy: int) -> None:
    """Move the pointer to a screen pixel (physical pixels, DPI aware)."""
    screen_w = user32.GetSystemMetrics(0)
    screen_h = user32.GetSystemMetrics(1)
    if screen_w <= 0 or screen_h <= 0:
        return
    nx = int(sx * 65535 / max(1, screen_w - 1))
    ny = int(sy * 65535 / max(1, screen_h - 1))
    code = INPUT()
    code.type = INPUT_MOUSE
    code.union.mi = MOUSEINPUT(dx=nx, dy=ny, mouseData=0,
                               dwFlags=MOUSEEVENTF_MOVE | MOUSEEVENTF_ABSOLUTE,
                               time=0, dwExtraInfo=ctypes.POINTER(ctypes.c_ulong)())
    user32.SendInput(1, ctypes.byref(code), ctypes.sizeof(INPUT))


def focus_window(hwnd: int) -> bool:
    """Give a window the foreground. Windows locks foreground changes made by
    a background process, so ALT is tapped first to release the lock.

    The window is also raised: the Unity editor tends to sit maximized on top
    of the game, and a click that lands on the editor both steals the
    foreground and moves the game out of focus, which pauses the game.
    """
    user32.keybd_event(0x12, 0, 0, ctypes.c_void_p(0))
    user32.BringWindowToTop(hwnd)
    user32.SetForegroundWindow(hwnd)
    user32.keybd_event(0x12, 0, 2, ctypes.c_void_p(0))
    time.sleep(0.35)
    return user32.GetForegroundWindow() == hwnd


def minimize_window(hwnd: int) -> None:
    user32.ShowWindow(hwnd, 6)  # SW_MINIMIZE


def restore_window(hwnd: int) -> None:
    user32.ShowWindow(hwnd, 9)  # SW_RESTORE


# --------------------------------------------------------------------------
def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("mode", choices=["probe", "shot", "session"])
    parser.add_argument("target", nargs="?", default="")
    parser.add_argument("--seconds", type=float, default=10.0)
    parser.add_argument("--fps", type=float, default=15.0)
    parser.add_argument("--keys", default="", help="comma separated keys to hold")
    parser.add_argument("--sprint", action="store_true")
    parser.add_argument("--attack", action="store_true")
    parser.add_argument("--look", default="", help="dx,dy mouse movement per second")
    parser.add_argument("--method", default="print", choices=["print", "screen"])
    args = parser.parse_args()

    enable_dpi_awareness()

    windows = find_windows()
    if not windows:
        print("NO_WINDOW")
        return 1
    for hwnd, title, x, y, w, h in windows:
        print(f"WINDOW hwnd={hwnd} title={title!r} rect=({x},{y}) size={w}x{h}")

    if args.mode == "probe":
        return 0

    hwnd, title, x, y, w, h = windows[0]
    print(f"TARGET {title} {w}x{h}")

    if args.mode == "shot":
        image = capture_window(hwnd, w, h) if args.method == "print" \
            else capture_screen_region(x, y, w, h)
        out = Path(args.target or "window.png")
        out.parent.mkdir(parents=True, exist_ok=True)
        image.save(out)
        print(f"WROTE {out} size={image.size}")
        return 0

    # session: drive the game and grab frames while it plays
    out_dir = Path(args.target)
    out_dir.mkdir(parents=True, exist_ok=True)
    for old in out_dir.glob("*.jpg"):
        old.unlink()

    keys = [k for k in args.keys.split(",") if k.strip()]
    if args.sprint:
        keys.append("shift")
    interval = 1.0 / max(1.0, args.fps)
    look = [0, 0]
    if args.look:
        parts = args.look.split(",")
        look = [int(float(parts[0])), int(float(parts[1])) if len(parts) > 1 else 0]

    print(f"SESSION seconds={args.seconds} fps={args.fps} keys={keys} attack={args.attack}")
    if keys:
        hold_keys(keys)
    time.sleep(0.4)

    start = time.time()
    index = 0
    next_frame = start
    try:
        while time.time() - start < args.seconds:
            now = time.time()
            if now >= next_frame:
                if args.attack:
                    mouse_click("left")
                if look[0] or look[1]:
                    move_mouse(int(look[0] / max(1.0, args.fps)),
                               int(look[1] / max(1.0, args.fps)))
                try:
                    image = capture_window(hwnd, w, h) if args.method == "print" \
                        else capture_screen_region(x, y, w, h)
                    image.save(out_dir / f"cap_{index:04d}.jpg", quality=92)
                    index += 1
                except Exception as e:
                    print(f"CAPTURE_FAIL {e}")
                next_frame = now + interval
            time.sleep(0.005)
    finally:
        if keys:
            release_keys(keys)

    print(f"WROTE {index} frames to {out_dir}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
