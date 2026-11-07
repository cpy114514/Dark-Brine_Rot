"""Drive the Unity editor capture hook.

Usage:
    python promo_cmd.py ping
    python promo_cmd.py shots <shots.json>       # shots.json holds {"outDir":..., "shots":[...]}
    python promo_cmd.py raw <command.json>

Writes <project>/Temp/promo_cmd.json, waits for the editor to answer in
<project>/Temp/promo_out.json, and prints the report.
"""

from __future__ import annotations

import json
import sys
import time
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
CMD = ROOT / "Temp" / "promo_cmd.json"
OUT = ROOT / "Temp" / "promo_out.json"
LOG = ROOT / "Temp" / "promo_log.txt"
TIMEOUT = float(sys.argv[1]) if len(sys.argv) > 1 and sys.argv[1].replace(".", "").isdigit() else 300.0


def send(payload: dict) -> str:
    CMD.parent.mkdir(parents=True, exist_ok=True)
    if OUT.exists():
        OUT.unlink()
    CMD.write_text(json.dumps(payload), encoding="utf-8")
    deadline = time.time() + TIMEOUT
    while time.time() < deadline:
        if OUT.exists():
            text = OUT.read_text(encoding="utf-8", errors="replace")
            if text.strip():
                return text
        time.sleep(0.4)
    return "TIMEOUT waiting for editor response"


def main() -> int:
    mode = sys.argv[2] if len(sys.argv) > 2 else "ping"
    if mode == "ping":
        print(send({"cmd": "ping"}))
    elif mode == "shots":
        payload = json.loads(Path(sys.argv[3]).read_text(encoding="utf-8"))
        print(send(payload))
    elif mode == "raw":
        payload = json.loads(Path(sys.argv[3]).read_text(encoding="utf-8"))
        print(send(payload))
    else:
        print("unknown mode")
        return 2
    if LOG.exists():
        print("--- log tail ---")
        print("\n".join(LOG.read_text(encoding="utf-8", errors="replace").splitlines()[-20:]))
    return 0


if __name__ == "__main__":
    sys.exit(main())
