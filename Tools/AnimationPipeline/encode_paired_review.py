"""Encode native paired review frames at 30 FPS and make a contact sheet."""

import argparse
import json
from pathlib import Path
import subprocess

import imageio_ffmpeg
from PIL import Image, ImageDraw


VIEWS = ("game", "front", "side", "three-quarter")
SAMPLE_TIMES = (0.1, 1.184, 2.43, 4.55, 7.03, 8.94, 12.8, 14.572, 18.05, 20.7)
SUMMARY_FIELDS = (
    "sourceFPS",
    "recordedFPS",
    "frames",
    "ExitReason",
    "HeroCounterCount",
    "SharkCounterCount",
    "BoardJumpCount",
    "ContactAttemptGaps",
    "LargestFootHeightError",
    "LargestPlantSlide",
)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument(
        "review_directory", type=Path, help="directory containing review.json and the captured PNG frames"
    )
    directory = parser.parse_args().review_directory
    report = json.loads((directory / "review.json").read_text(encoding="utf-8"))

    for view in VIEWS:
        subprocess.run(
            [
                imageio_ffmpeg.get_ffmpeg_exe(),
                "-hide_banner", "-loglevel", "error", "-y",
                "-framerate", "30", "-i", str(directory / f"{view}-%04d.png"),
                "-vf", "format=yuv420p", "-c:v", "libx264", "-preset", "fast",
                "-crf", "20", "-movflags", "+faststart", str(directory / f"{view}-review.mp4"),
            ],
            check=True,
        )

    grid = Image.new("RGB", (len(VIEWS) * 480, len(SAMPLE_TIMES) * 295), (25, 25, 25))
    draw = ImageDraw.Draw(grid)
    for r, time in enumerate(SAMPLE_TIMES):
        row = min(report["rows"], key=lambda sample: abs(sample["time"] - time))
        frame = str(row["frame"]).zfill(4)
        for c, view in enumerate(VIEWS):
            with Image.open(directory / f"{view}-{frame}.png") as image:
                image.thumbnail((480, 270))
                grid.paste(image, (c * 480, r * 295 + 25))
            draw.text((c * 480 + 4, r * 295 + 5), f'{view} {row["time"]:.3f}s', fill="white")
    grid.save(directory / "contact-sheet.jpg", quality=92)

    print(json.dumps({key: report[key] for key in SUMMARY_FIELDS}, indent=2))


if __name__ == "__main__":
    main()
