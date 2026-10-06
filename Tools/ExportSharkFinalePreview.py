"""Encode actual Unity camera captures with their in-game frame times."""
import json
from pathlib import Path
import subprocess
import imageio_ffmpeg

root = Path(__file__).resolve().parents[1] / '.codex' / 'shark-finale-preview'
lines = []
for folder, timing, end in [('impact-frames', 'impact-times.json', 2.05),
                            ('frames', 'frame-times.json', 14.4)]:
    times = json.loads((root / timing).read_text())
    for i, time in enumerate(times):
        path = (root / folder / f'{i:04d}.png').resolve().as_posix()
        duration = max(.025, (times[i + 1] if i + 1 < len(times) else end) - time)
        lines.extend([f"file '{path}'", f'duration {duration:.6f}'])
lines.append(lines[-2])
listing = root / 'preview-concat.txt'
listing.write_text('\n'.join(lines), encoding='utf-8')
video = root / 'shark-wreck-finale.mp4'
subprocess.run([imageio_ffmpeg.get_ffmpeg_exe(), '-hide_banner', '-loglevel', 'error',
                '-y', '-f', 'concat', '-safe', '0', '-i', str(listing),
                '-vf', 'fps=30,pad=720:406:0:0,format=yuv420p',
                '-c:v', 'libx264', '-preset', 'fast', '-crf', '21',
                '-movflags', '+faststart', str(video)], check=True)
print(video)
