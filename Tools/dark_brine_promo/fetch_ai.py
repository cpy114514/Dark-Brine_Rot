"""Query finished AI video tasks by id and download the results."""

from __future__ import annotations

import json
import os
import subprocess
import sys
import time
import urllib.request
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
WORK = ROOT / "Tools" / "dark_brine_promo" / "out" / "ai"
DEST = ROOT / "Tools" / "dark_brine_promo" / "assets" / "ai"
ENV = dict(os.environ)
ENV["MCODE_REGION"] = "cn"

TASKS = {
    "test_ocean_01": ("459975718998251", "MiniMax-H3-Max"),
    "shark_fin": ("459977605681404", "MiniMax-H3"),
    "brine_waves": ("459978021060841", "MiniMax-H3-Max"),
    "hailuo_ocean": ("459981436092638", "MiniMax-Hailuo-2.3"),
    "wreck_fog": ("459981895430345", "MiniMax-Hailuo-2.3"),
    "beast_rise": ("459981353431278", "MiniMax-Hailuo-2.3"),
    "island_approach": ("459981225685621", "MiniMax-Hailuo-2.3"),
    "island_approach_fast": ("459981591921257", "MiniMax-Hailuo-2.3"),
    "i2v_island": ("459984036880611", "MiniMax-Hailuo-2.3"),
}


def run(args: list[str]) -> dict:
    proc = subprocess.run(args, capture_output=True, text=True, errors="replace", env=ENV, shell=True)
    raw = proc.stdout.strip()
    try:
        return json.loads(raw)
    except Exception:
        return {"status": "error", "stdout": raw[:400], "stderr": proc.stderr[-300:]}


def query(task_id: str, model: str) -> dict:
    args_file = WORK / f"q_{task_id}.json"
    args_file.write_text(json.dumps({"task_id": task_id, "model": model}), encoding="utf-8")
    return run([
        "mcode-tools", "connector", "call",
        "connector__matrix__query_video_generation",
        "--args-file", str(args_file),
    ])


def main() -> int:
    WORK.mkdir(parents=True, exist_ok=True)
    DEST.mkdir(parents=True, exist_ok=True)
    manifest = {}
    manifest_path = WORK / "manifest.json"

    for name, (task_id, model) in TASKS.items():
        target = DEST / f"{name}.mp4"
        if target.exists() and target.stat().st_size > 50_000:
            manifest[name] = {"task_id": task_id, "file": str(target), "cached": True}
            print(f"{name}: cached")
            continue

        res = query(task_id, model)
        status = res.get("status")
        if status != "succeeded":
            manifest[name] = {"task_id": task_id, "status": status}
            print(f"{name}: {status}")
            continue

        url = res.get("video_url")
        req = urllib.request.Request(url, headers={"User-Agent": "promo/1.0"})
        with urllib.request.urlopen(req, timeout=240) as resp, open(target, "wb") as fh:
            fh.write(resp.read())
        manifest[name] = {
            "task_id": task_id,
            "status": "succeeded",
            "file": str(target),
            "resolution": res.get("resolution"),
            "duration": res.get("duration"),
        }
        print(f"{name}: downloaded {target.stat().st_size // 1024} KB {res.get('resolution')}")

    manifest_path.write_text(json.dumps(manifest, indent=1), encoding="utf-8")
    return 0


if __name__ == "__main__":
    sys.exit(main())
