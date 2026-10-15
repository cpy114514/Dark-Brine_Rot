"""Encode the actual native camera review. Screen-space overlays are excluded."""
import json
import subprocess
from pathlib import Path
import imageio_ffmpeg
from PIL import Image, ImageDraw

root = Path('D:/indiegameDev/unity/Dark Brine_Rot/.codex/shipbreak-camera-20261014/default-30-final')
report = json.loads((root/'review.json').read_text(encoding='utf-8-sig'))
subprocess.run([imageio_ffmpeg.get_ffmpeg_exe(), '-hide_banner', '-loglevel', 'error', '-y',
                '-framerate', str(report['fps']), '-i', str(root/'game-%04d.png'),
                '-vf', 'format=yuv420p', '-c:v', 'libx264', '-preset', 'fast', '-crf', '20',
                '-movflags', '+faststart', str(root/'camera-review.mp4')], check=True)
sheet = Image.new('RGB', (1920,600), (24,24,24))
draw = ImageDraw.Draw(sheet)
for i,t in enumerate((1.4,1.8,2.7,3.5,3.8,4.7,6.0,6.8)):
    row = min(report['rows'],key=lambda x:abs(x['time']-t))
    im = Image.open(root/f"game-{row['frame']:04d}.png")
    im.thumbnail((480,270))
    x,y=(i%4)*480,(i//4)*300
    sheet.paste(im,(x,y+25))
    draw.text((x+6,y+5),f"{row['time']:.2f}s {row['phase']}",fill='white')
sheet.save(root/'contact-sheet.jpg',quality=93)
print(json.dumps({k:v for k,v in report.items() if k!='rows'},indent=2))
