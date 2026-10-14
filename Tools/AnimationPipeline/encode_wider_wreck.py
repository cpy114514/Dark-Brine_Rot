from pathlib import Path
import json
import subprocess
import imageio_ffmpeg
from PIL import Image, ImageDraw

root = Path(__file__).resolve().parents[2] / '.codex/wider-wreck-impact-20261013'
report = json.loads((root / 'comparison.json').read_text(encoding='utf-8-sig'))
if report['error']:
    raise RuntimeError(report['error'])
for mode in ['baseline', 'stronger']:
    folder = root / mode
    subprocess.run([imageio_ffmpeg.get_ffmpeg_exe(), '-hide_banner', '-loglevel', 'error', '-y',
                    '-framerate', '15', '-i', str(folder / 'game-%04d.png'),
                    '-c:v', 'libx264', '-preset', 'fast', '-crf', '21', '-pix_fmt', 'yuv420p',
                    '-movflags', '+faststart', str(folder / 'impact-preview.mp4')], check=True)
sheet = Image.new('RGB', (1920, 1140), '#151515')
draw = ImageDraw.Draw(sheet)
for col, mode in enumerate(['baseline', 'stronger']):
    draw.text((col * 960 + 20, 15), 'Original launch strength (activation fixed)' if col == 0 else 'Stronger tail strike', fill='white')
    for row, seconds in enumerate([3, 12]):
        sheet.paste(Image.open(root / mode / f'layout-{seconds}.png').convert('RGB'), (col * 960, 35 + row * 550))
        draw.text((col * 960 + 20, 45 + row * 550), f'{seconds}s after fracture / same top-view scale', fill='white', stroke_width=2, stroke_fill='black')
sheet.save(root / 'layout-comparison.jpg', quality=90)
print(json.dumps({'movies': [str(root / mode / 'impact-preview.mp4') for mode in ['baseline', 'stronger']],
                  'comparison': str(root / 'layout-comparison.jpg')}))
