import json
import subprocess
from pathlib import Path
import imageio_ffmpeg
from PIL import Image, ImageDraw

root = Path('D:/indiegameDev/unity/Dark Brine_Rot/.codex/finale-admission-20261014/after-entry')
report = json.loads((root/'result.json').read_text(encoding='utf-8-sig'))
subprocess.run([imageio_ffmpeg.get_ffmpeg_exe(), '-hide_banner', '-loglevel', 'error', '-y',
                '-framerate', '30', '-i', str(root/'game-%04d.png'), '-vf', 'format=yuv420p',
                '-c:v', 'libx264', '-preset', 'fast', '-crf', '20', '-movflags', '+faststart',
                str(root/'full-film.mp4')], check=True)
sheet = Image.new('RGB', (1920,1200), (24,24,24))
draw = ImageDraw.Draw(sheet)
for i,t in enumerate((0,1.18,2.43,4,6,7.03,8.94,11.6,13,14.57,18.05,20.5)):
    frame = min(report['filmFrames']-1, max(0, round((t-report['firstFilm'])*30)))
    im = Image.open(root/f'game-{frame:04d}.png'); im.thumbnail((640,360))
    x,y=(i%3)*640,(i//3)*300
    im.thumbnail((640,275)); sheet.paste(im,(x,y+25))
    draw.text((x+6,y+5),f'{t:.2f}s',fill='white')
sheet.save(root/'contact-sheet.jpg',quality=94)
print(json.dumps({k:v for k,v in report.items() if k!='boards'},indent=2))
