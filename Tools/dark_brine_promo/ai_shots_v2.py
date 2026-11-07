"""Generate a fresh AI shot list for the brainstick ad.

The ad leans on one visual motif that comes back four times on the beat, so
the shot list deliberately contains several clips made from the *same* prompt:
near-identical frames read as a repetition, which is what makes the piece
stick. Everything is Hailuo-2.3 at 1080P / 6 s — the cheapest model that still
carries a usable resolution.

    python ai_shots_v2.py              # submit + poll + download
    python ai_shots_v2.py eyes_1       # one shot only
"""
from __future__ import annotations

import json
import os
import subprocess
import sys
import time
import urllib.request
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
BASE = ROOT / "Tools" / "dark_brine_promo"
WORK = BASE / "out" / "ai2"
DEST = BASE / "assets" / "ai2"

ENV = dict(os.environ)
ENV["MCODE_REGION"] = "cn"

LOOK = (
    ", cinematic, photoreal, cold teal and deep blue night palette, volumetric fog, "
    "heavy film grain, high contrast, ominous horror atmosphere, no text, no watermark, no logo"
)

EYES = (
    "Cinematic ultra close-up of two small glowing crimson eyes floating in pitch black fog "
    "above dark ocean water, the lights slowly growing larger and brighter as something "
    "approaches from beneath the surface, faint ripples, mist catching the red glow" + LOOK
)

SHOTS = [
    # the motif, three times — these are the frames that come back on every beat
    {"name": "eyes_1", "model": "MiniMax-Hailuo-2.3", "duration": 6,
     "resolution": "1080P", "prompt": EYES},
    {"name": "eyes_2", "model": "MiniMax-Hailuo-2.3", "duration": 6,
     "resolution": "1080P", "prompt": EYES},
    {"name": "eyes_3", "model": "MiniMax-Hailuo-2.3", "duration": 6,
     "resolution": "1080P", "prompt": EYES},
    {"name": "eye_open", "model": "MiniMax-Hailuo-2.3", "duration": 6,
     "resolution": "1080P",
     "prompt": (
         "Cinematic extreme close-up of a single inhuman eye opening slowly in complete darkness, "
         "wet reflective surface, a thin slit of cold blue light catching the iris, the eyelid "
         "lifting to reveal nothing but black" + LOOK)},
    {"name": "lamp", "model": "MiniMax-Hailuo-2.3", "duration": 6,
     "resolution": "1080P",
     "prompt": (
         "Cinematic shot of a rusted caged ship lamp swinging in thick night fog on a dark ocean, "
         "the flame guttering and flaring, weak amber light cutting through the mist, water and "
         "railing barely visible" + LOOK)},
    {"name": "claw", "model": "MiniMax-Hailuo-2.3", "duration": 6,
     "resolution": "1080P",
     "prompt": (
         "Cinematic shot of a colossal barnacle covered claw sweeping across the frame in the dark, "
         "slick wet texture, water streaming off it, only rim light to see it by, fast movement"
         + LOOK)},
    {"name": "stare", "model": "MiniMax-Hailuo-2.3", "duration": 6,
     "resolution": "1080P",
     "prompt": (
         "Cinematic shot of a tall thin silhouette standing motionless in the fog at the edge of a "
         "dark island, facing the camera directly, completely still, only its outline visible, "
         "slow creeping push in, deeply unsettling" + LOOK)},
    {"name": "pulse", "model": "MiniMax-Hailuo-2.3", "duration": 6,
     "resolution": "1080P",
     "prompt": (
         "Cinematic macro shot of a wet organic surface pulsing with a slow heartbeat, dark veins "
         "under translucent flesh, dim crimson glow rising and falling, viscous, unnatural" + LOOK)},
    {"name": "surf_raid", "model": "MiniMax-Hailuo-2.3", "duration": 6,
     "resolution": "1080P",
     "prompt": (
         "Cinematic low angle of a huge black wave crashing over a wooden raft in a night storm, "
         "spray exploding toward the lens, the raft breaking apart, handheld camera, violent "
         "and immediate" + LOOK)},
]


def run(args: list[str]) -> dict:
    proc = subprocess.run(args, capture_output=True, text=True, errors="replace", env=ENV, shell=True)
    raw = proc.stdout.strip()
    try:
        return json.loads(raw)
    except Exception:
        return {"status": "error", "stdout": raw[:400], "stderr": proc.stderr[-400:]}


def submit(shot: dict) -> dict:
    payload = {k: v for k, v in shot.items() if k != "name"}
    args_file = WORK / f"submit_{shot['name']}.json"
    args_file.write_text(json.dumps(payload, ensure_ascii=False), encoding="utf-8")
    return run(["mcode-tools", "connector", "call",
                "connector__matrix__submit_video_generation",
                "--args-file", str(args_file)])


def query(task_id: str, model: str) -> dict:
    args_file = WORK / f"query_{task_id}.json"
    args_file.write_text(json.dumps({"task_id": task_id, "model": model}), encoding="utf-8")
    return run(["mcode-tools", "connector", "call",
                "connector__matrix__query_video_generation",
                "--args-file", str(args_file)])


def asset_url(node_id: str) -> str:
    res = run(["mcode-tools", "get_asset_url", node_id])
    return res.get("download_url", "")


def download(name: str, res: dict) -> Path:
    target = DEST / f"{name}.mp4"
    url = res.get("video_url")
    if not url and res.get("node_id"):
        url = asset_url(str(res["node_id"]))
    if not url:
        raise RuntimeError(f"{name}: no video url")
    req = urllib.request.Request(url, headers={"User-Agent": "promo/2.0"})
    with urllib.request.urlopen(req, timeout=300) as resp, open(target, "wb") as fh:
        fh.write(resp.read())
    return target


def main() -> int:
    WORK.mkdir(parents=True, exist_ok=True)
    DEST.mkdir(parents=True, exist_ok=True)
    only = sys.argv[1:] or None

    state_path = WORK / "state.json"
    state = json.loads(state_path.read_text(encoding="utf-8")) if state_path.exists() else {}

    for shot in SHOTS:
        if only and shot["name"] not in only:
            continue
        if state.get(shot["name"], {}).get("task_id"):
            continue
        res = submit(shot)
        print(f"submit {shot['name']}: {res.get('status')} {res.get('task_id')} {res.get('message', '')}",
              flush=True)
        state[shot["name"]] = {"task_id": res.get("task_id"), "model": shot["model"],
                               "status": res.get("status")}
        state_path.write_text(json.dumps(state, indent=1), encoding="utf-8")
        time.sleep(2)

    deadline = time.time() + float(os.environ.get("AI_POLL_SECONDS", "2400"))
    while time.time() < deadline:
        pending = [k for k, v in state.items() if v.get("status") not in ("succeeded", "failed", "cancelled")]
        if not pending:
            break
        for key in pending:
            res = query(state[key]["task_id"], state[key]["model"])
            status = res.get("status")
            if status != state[key].get("status"):
                print(f"{key}: {state[key].get('status')} -> {status}", flush=True)
            state[key]["status"] = status
            if status == "succeeded":
                state[key]["node_id"] = res.get("node_id")
                state[key]["resolution"] = res.get("resolution")
                target = DEST / f"{key}.mp4"
                if not (target.exists() and target.stat().st_size > 50_000):
                    try:
                        download(key, res)
                        print(f"{key}: downloaded {target.stat().st_size // 1024} KB", flush=True)
                    except Exception as exc:  # noqa: BLE001 - keep polling the rest
                        print(f"{key}: download failed {exc}", flush=True)
            elif status in ("failed", "cancelled"):
                state[key]["message"] = res.get("message")
        state_path.write_text(json.dumps(state, indent=1), encoding="utf-8")
        time.sleep(15)

    print(json.dumps({k: v.get("status") for k, v in state.items()}, indent=1))
    return 0


if __name__ == "__main__":
    sys.exit(main())
