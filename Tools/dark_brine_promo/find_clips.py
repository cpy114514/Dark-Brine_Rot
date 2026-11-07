import json
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
clips = json.loads((ROOT / "Tools" / "dark_brine_promo" / "out" / "clip_scan.json").read_text(encoding="utf-8"))
kw = tuple(sys.argv[1].split(",")) if len(sys.argv) > 1 else ("combat",)
for x in clips:
    p = x["path"].lower()
    if any(k in p for k in kw):
        print(f"{x['dur']:6.1f}s {x['w']:5}x{x['h']:<5} {x['path']}")
