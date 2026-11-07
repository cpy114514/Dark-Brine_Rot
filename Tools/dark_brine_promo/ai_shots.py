"""Submit the AI-generated shot list for the trailer and poll each task.

Writes one JSON args file per shot, submits them, then polls the query tool
until every task settles. Results (video URLs) land in out/ai_shots.json.
"""

from __future__ import annotations

import json
import os
import subprocess
import sys
import time
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
WORK = ROOT / "Tools" / "dark_brine_promo" / "out" / "ai"
ASSETS = ROOT / "Tools" / "dark_brine_promo" / "assets" / "ai"

ENV = dict(os.environ)
ENV["MCODE_REGION"] = "cn"

SHOTS = [
    {
        "name": "wreck_fog",
        "model": "MiniMax-Hailuo-2.3",
        "duration": 6,
        "prompt": (
            "Cinematic shot of a half sunken wooden sailing shipwreck emerging from dense night fog on a "
            "black sea, broken masts and tattered sails, cold blue moonlight, drifting particles, slow lateral "
            "camera drift, eerie abandoned atmosphere, photoreal, film grain, no text"
        ),
    },
    {
        "name": "beast_rise",
        "model": "MiniMax-Hailuo-2.3",
        "duration": 6,
        "prompt": (
            "Cinematic low angle of a colossal pale sea creature rising out of dark ocean water toward the moon, "
            "huge glistening back breaking the surface, spray and foam, teal and cold blue palette, fog, "
            "slow upward camera tilt, terrifying boss reveal, photoreal, film grain, no text"
        ),
    },
    {
        "name": "island_approach",
        "model": "MiniMax-Hailuo-2.3",
        "duration": 6,
        "prompt": (
            "Cinematic aerial push over a lonely grassy island at dawn, wind bent grass on rolling hills, "
            "pale sand beach meeting a dark teal ocean, low drifting mist, soft cold sunlight, stylized "
            "low poly game art look, moody survival adventure, no text"
        ),
    },
    {
        "name": "island_approach_fast",
        "model": "MiniMax-Hailuo-2.3",
        "duration": 6,
        "prompt": (
            "Cinematic aerial push over a lonely grassy island at dawn, wind bent grass on rolling hills, "
            "pale sand beach meeting a dark teal ocean, low drifting mist, soft cold sunlight, stylized "
            "low poly game art look, moody survival adventure, no text"
        ),
    },
    {
        "name": "boss_gate",
        "model": "MiniMax-Hailuo-2.3",
        "duration": 6,
        "prompt": (
            "Cinematic low angle of a strange round white coffee-cream monster with glowing orange eyes and a "
            "wide grin standing in a foggy dark island clearing, cartoon stylized game art, rim lit by cold "
            "moonlight, slow push in, creepy boss reveal, no text"
        ),
    },
    {
        "name": "island_grass",
        "model": "MiniMax-Hailuo-2.3",
        "duration": 6,
        "prompt": (
            "Cinematic low tracking shot through tall wind bent green grass on a dark island at dusk, pale fog "
            "over the ground, distant ocean horizon, stylized low poly game art, moody survival horror, "
            "slow camera glide, no text"
        ),
    },
    {
        "name": "combat_blades",
        "model": "MiniMax-Hailuo-2.3",
        "duration": 6,
        "prompt": (
            "Cinematic dynamic shot of two curved glowing blades slashing through darkness with bright motion "
            "trails and sparks, teal and orange rim light, fog, aggressive fast camera movement, stylized "
            "low poly game art combat, no text"
        ),
    },
]


def run(args: list[str]) -> dict:
    proc = subprocess.run(args, capture_output=True, text=True, errors="replace", env=ENV, shell=True)
    raw = proc.stdout.strip()
    try:
        return json.loads(raw)
    except Exception:
        return {"code": -1, "stdout": raw, "stderr": proc.stderr[-800:]}


def submit(shot: dict) -> dict:
    payload = {k: v for k, v in shot.items() if k != "name"}
    args_file = WORK / f"submit_{shot['name']}.json"
    args_file.write_text(json.dumps(payload, ensure_ascii=False), encoding="utf-8")
    res = run([
        "mcode-tools", "connector", "call",
        "connector__matrix__submit_video_generation",
        "--args-file", str(args_file),
    ])
    return res


def query(task_id: str, model: str) -> dict:
    args_file = WORK / f"query_{task_id}.json"
    args_file.write_text(json.dumps({"task_id": task_id, "model": model}), encoding="utf-8")
    return run([
        "mcode-tools", "connector", "call",
        "connector__matrix__query_video_generation",
        "--args-file", str(args_file),
    ])


def main() -> int:
    WORK.mkdir(parents=True, exist_ok=True)
    ASSETS.mkdir(parents=True, exist_ok=True)
    only = sys.argv[1:] or None

    state_file = WORK / "ai_shots.json"
    state = json.loads(state_file.read_text(encoding="utf-8")) if state_file.exists() else {}

    todo = [s for s in SHOTS if not only or s["name"] in only]
    for shot in todo:
        if state.get(shot["name"], {}).get("task_id"):
            continue
        res = submit(shot)
        print(f"submit {shot['name']}: {res.get('status')} {res.get('task_id')} {res.get('message', '')}")
        state[shot["name"]] = {"task_id": res.get("task_id"), "model": shot["model"], "status": res.get("status")}
        state_file.write_text(json.dumps(state, indent=1), encoding="utf-8")

    deadline = time.time() + float(os.environ.get("AI_POLL_SECONDS", "2400"))
    while time.time() < deadline:
        pending = [k for k, v in state.items() if v.get("status") not in ("succeeded", "failed", "cancelled")]
        if not pending:
            break
        for key in pending:
            res = query(state[key]["task_id"], state[key]["model"])
            status = res.get("status")
            if status != state[key].get("status"):
                print(f"{key}: {state[key].get('status')} -> {status}")
            state[key]["status"] = status
            if status == "succeeded":
                state[key]["video_url"] = res.get("video_url")
                state[key]["node_id"] = res.get("node_id")
                state[key]["resolution"] = res.get("resolution")
            elif status in ("failed", "cancelled"):
                state[key]["message"] = res.get("message")
        state_file.write_text(json.dumps(state, indent=1), encoding="utf-8")
        time.sleep(15)

    print(json.dumps({k: v.get("status") for k, v in state.items()}, indent=1))
    return 0


if __name__ == "__main__":
    sys.exit(main())
