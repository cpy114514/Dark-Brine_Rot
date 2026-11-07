"""Download every finished AI shot listed in out/ai/ai_shots.json."""

from __future__ import annotations

import json
import urllib.request
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
STATE = ROOT / "Tools" / "dark_brine_promo" / "out" / "ai" / "ai_shots.json"
DEST = ROOT / "Tools" / "dark_brine_promo" / "assets" / "ai"


def main() -> int:
    DEST.mkdir(parents=True, exist_ok=True)
    state = json.loads(STATE.read_text(encoding="utf-8"))
    for name, info in state.items():
        url = info.get("video_url")
        if not url:
            print(f"{name}: {info.get('status')} (no url)")
            continue
        ext = ".mp4"
        target = DEST / f"{name}{ext}"
        if target.exists() and target.stat().st_size > 100_000:
            print(f"{name}: cached {target.stat().st_size // 1024} KB")
            continue
        req = urllib.request.Request(url, headers={"User-Agent": "promo/1.0"})
        with urllib.request.urlopen(req, timeout=180) as resp, open(target, "wb") as fh:
            fh.write(resp.read())
        print(f"{name}: downloaded {target.stat().st_size // 1024} KB ({info.get('resolution')})")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
