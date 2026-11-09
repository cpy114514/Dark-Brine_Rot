"""Summarize comparable native Player captures produced by RunSystemPerformance."""
import argparse
import json
from math import ceil
from pathlib import Path
from statistics import mean


PHASES = ("exploration", "coast", "forest", "cappuccino", "flock", "nailong", "boss-effects")


def read_run(directory):
    report = json.loads((directory / "report.json").read_text(encoding="utf-8-sig"))
    counters = json.loads((directory / "report.json.counters.json").read_text(encoding="utf-8-sig"))
    if report["status"] != "completed" or report["editor"] or not report["renderProofs"]:
        raise ValueError(f"{directory} has no completed standalone render capture")
    timings = {}
    for phase in PHASES:
        rows = [f for f in counters["frames"] if PHASES[f["phase"]] == phase]
        if not rows:
            continue
        focused_share = mean(f["focused"] for f in rows) if "focused" in rows[0] else None
        if focused_share:
            rows = [f for f in rows if f["focused"]]
        wall = sorted(row["wall"] for row in rows)
        count = len(wall)
        result = {
            "frames": count, "averageFps": 1000 / mean(wall), "averageMs": mean(wall),
            "onePercentLowFps": 1000 / mean(wall[-ceil(count / 100):]),
            "p95Ms": wall[ceil(count * .95) - 1], "p99Ms": wall[ceil(count * .99) - 1],
            "spikes50ms": sum(ms > 50 for ms in wall), "focusedShare": focused_share,
            "bossActiveShare": mean(f["bossActive"] for f in rows) if "bossActive" in rows[0] else None
        }
        for key in ("cpu", "main", "render", "wait", "gpu", "gc", "draw", "setPass", "batches", "triangles", "grassVisibility", "grassSubmit", "physics", "enemies"):
            values = [f[key] for f in rows if f[key] >= 0]
            if key in ("cpu", "gpu"):
                values = [v for v in values if 0 < v < 1000]
            result[key] = round(mean(values), 4) if values else None
            if key in ("gpu", "gc"):
                result[f"{key}Samples"] = len(values)
        timings[phase] = result
    conditions = {k: report[k] for k in ("unity", "cpu", "gpu", "os", "quality", "graphicsApi", "width", "height", "renderScale", "shadowDistance", "lodBias", "vsync", "targetFps", "developmentBuild")}
    conditions.update(msaa=counters["msaa"], manual=counters["systemManualRender"])
    return {"conditions": conditions, "errors": report["errors"], "loadSeconds": report["loadSeconds"],
            "memoryMb": report["memoryMb"], "memory": counters["memory"], "phases": timings,
            "skipped": report["skippedPhases"], "gcPositiveControlPassed": report["gcCounterAvailable"]}


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("before", type=Path)
    parser.add_argument("after", type=Path, nargs="?")
    parser.add_argument("--output", type=Path)
    args = parser.parse_args()
    before = read_run(args.before)
    output = {"before": before}
    if args.after:
        after = read_run(args.after)
        output["after"] = after
        output["conditionsMatch"] = before["conditions"] == after["conditions"]
        output["delta"] = {
            name: {k: round(row[k] - before["phases"][name][k], 4)
                   for k in ("averageMs", "p95Ms", "averageFps", "onePercentLowFps")}
            for name, row in after["phases"].items() if name in before["phases"]
        }
    result = json.dumps(output, ensure_ascii=False, indent=2)
    if args.output:
        args.output.write_text(result, encoding="utf-8")
    else:
        print(result)


if __name__ == "__main__":
    main()
