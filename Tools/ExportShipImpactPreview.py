"""Encode the real Unity ship-impact capture using its game-time timestamps."""
import json
import subprocess
from pathlib import Path
import imageio_ffmpeg

root = Path(__file__).resolve().parents[1] / '.codex' / 'ship-tail-strike'
times = json.loads((root / 'times.json').read_text())
end = json.loads((root / 'verification.json').read_text())['duration']
lines = []
for index, stamp in enumerate(times):
    path = (root / 'frames' / f'{index:04d}.png').resolve().as_posix()
    duration = max(.025, (times[index + 1] if index + 1 < len(times) else end) - stamp)
    lines.extend([f"file '{path}'", f'duration {duration:.6f}'])
lines.append(lines[-2])
listing = root / 'concat.txt'
listing.write_text('\n'.join(lines), encoding='utf-8')
output = root / 'shark-tail-ship-break.mp4'
subprocess.run([imageio_ffmpeg.get_ffmpeg_exe(), '-hide_banner', '-loglevel', 'error',
                '-y', '-f', 'concat', '-safe', '0', '-i', str(listing), '-vf', 'fps=30,format=yuv420p',
                '-c:v', 'libx264', '-preset', 'fast', '-crf', '20', '-movflags', '+faststart', str(output)], check=True)
print(output)
