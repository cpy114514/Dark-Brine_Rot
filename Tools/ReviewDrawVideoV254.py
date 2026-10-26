"""Encode the native story camera take and retain its review contact sheet."""
from pathlib import Path
import json
import subprocess
import sys
import imageio_ffmpeg
from PIL import Image, ImageDraw

root = Path(__file__).resolve().parents[1] / '.codex/draw-retrieve-v254'
take = root / (sys.argv[1] if len(sys.argv)>1 else 'native-take-02')
frames = sorted(take.glob('frame-*.png'))
result = json.loads((take / 'result.json').read_text())
assert frames and len(frames) == result['images']
video = root / f'climb-and-draw-v254-{take.name}.mp4'
if not video.exists():
    subprocess.run([imageio_ffmpeg.get_ffmpeg_exe(), '-hide_banner', '-loglevel', 'error',
                    '-framerate', '30', '-i', str(take / 'frame-%04d.png'),
                    '-c:v', 'libx264', '-crf', '18', '-pix_fmt', 'yuv420p',
                    '-movflags', '+faststart', str(video)], check=True)
indices = list(range(0, len(frames), 15))
sheet = Image.new('RGB', (960, ((len(indices)+2)//3)*202), '#15171b')
draw = ImageDraw.Draw(sheet)
for n, index in enumerate(indices):
    thumb = Image.open(frames[index]).resize((320, 180))
    x, y = (n % 3)*320, (n//3)*202
    sheet.paste(thumb, (x,y))
    draw.text((x+8,y+183), f'{index/30:.2f} s / frame {index}', fill='white')
sheet.save(root / 'native-review.jpg', quality=94)
print(json.dumps({k:v for k,v in result.items() if k!='rows'}, indent=2))
print(str(video))
