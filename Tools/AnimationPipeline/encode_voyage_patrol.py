"""Encode continuous native Editor game/wide review; no performance claim."""
from pathlib import Path
import json
import subprocess
import imageio_ffmpeg
from PIL import Image, ImageDraw

root = Path(r'D:/indiegameDev/unity/Dark Brine_Rot/.codex/shark-voyage-patrol-20261013/native-review')
report = json.loads((root / 'review.json').read_text(encoding='utf-8-sig'))
for view in ('game', 'wide'):
    subprocess.run([imageio_ffmpeg.get_ffmpeg_exe(), '-hide_banner', '-loglevel', 'error', '-y',
                    '-framerate', str(report['recordedFPS']), '-i', str(root / (view + '-%04d.png')),
                    '-vf', 'format=yuv420p', '-c:v', 'libx264', '-preset', 'fast', '-crf', '20',
                    '-movflags', '+faststart', str(root / (view + '-review.mp4'))], check=True)
times = [1, *[t + 2 for t in report['breachTimes']], report['patrolDuration'] * .5,
         report['patrolDuration'] - .2, report['impactTime']]
grid = Image.new('RGB', (960 * 2, 565 * len(times)), (24, 24, 24))
draw = ImageDraw.Draw(grid)
for row, time in enumerate(times):
    frame = min(report['recordedFrames'], key=lambda frame: abs(frame['time'] - time))
    for column, view in enumerate(('game', 'wide')):
        picture = Image.open(root / f'{view}-{frame["image"]:04d}.png')
        grid.paste(picture, (column * 960, row * 565 + 25))
        draw.text((column * 960 + 8, row * 565 + 5), f'{view}: {frame["time"]:.3f}s', fill='white')
grid.save(root / 'contact-sheet.jpg', quality=92)
print(json.dumps({key: report[key] for key in ['images', 'orbitDegrees', 'breaches', 'breachTimes',
                 'visibleBreachFrames', 'minimumBoundsClearance', 'pausedFrames', 'phase', 'tailGap']}, indent=2))
