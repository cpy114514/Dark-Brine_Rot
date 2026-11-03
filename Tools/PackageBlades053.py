"""Package diagnostic native frames at their recorded simulation timing."""
import argparse
import json
import subprocess
from pathlib import Path
import imageio_ffmpeg
from PIL import Image, ImageDraw

parser = argparse.ArgumentParser()
parser.add_argument('take', type=Path)
root = parser.parse_args().take.resolve()
rows = json.loads((root / 'frames.json').read_text())
result = json.loads((root / 'result.json').read_text())
hz = result['hz']
sections = ['SahurRifle aerial', 'Rush through flock', 'Wall behind flock', 'Boss body contact']
lines, labels, offset = [], [], 0
for section in sections:
    items = [r for r in rows if r['section'] == section]
    if not items:
        continue
    first = offset
    for i, row in enumerate(items):
        path = (root / 'frames' / f'{row["image"]:05d}.jpg').as_posix()
        count = items[i + 1]['frame'] - row['frame'] if i + 1 < len(items) else 3
        lines += ["file '" + path.replace("'", "'\\''") + "'", f'duration {count/hz:.9f}']
        offset += count / hz
    labels.append((first, offset, section))
lines.append(lines[-2])
(root / 'native-timing.ffconcat').write_text('\n'.join(lines), encoding='utf-8')
filters = ["drawtext=fontfile='C\\:/Windows/Fonts/segoeui.ttf':text='" + title +
           f"':fontsize=20:fontcolor=white:box=1:boxcolor=black@0.6:boxborderw=6:x=18:y=24:enable='between(t,{a:.6f},{b:.6f})'"
           for a, b, title in labels]
output = root / 'Blades-Aerial-Rush-Native.mp4'
subprocess.run([imageio_ffmpeg.get_ffmpeg_exe(), '-hide_banner', '-loglevel', 'error', '-y',
                '-f', 'concat', '-safe', '0', '-i', str(root / 'native-timing.ffconcat'),
                '-vf', ','.join(filters), '-r', '30', '-c:v', 'libx264', '-crf', '18',
                '-pix_fmt', 'yuv420p', '-movflags', '+faststart', str(output)], check=True)
choices = []
for stage in range(3):
    items = [r for r in rows if r['section'] == 'SahurRifle aerial' and r['air'] == stage]
    if items:
        choices.append((f'Air stage {stage + 1}', items[len(items)//2]))
for title, condition in [('Rush anticipation', lambda r: r['phase'] < .2),
                         ('Rush contact', lambda r: r['stopped']),
                         ('Rush follow-through', lambda r: r['phase'] > .65)]:
    items = [r for r in rows if r['section'] == 'Rush through flock' and r['heavy'] and condition(r)]
    if items:
        choices.append((title, items[len(items)//2]))
for section in ['Wall behind flock', 'Boss body contact']:
    items = [r for r in rows if r['section'] == section and r['heavy'] and r['phase'] > .6]
    if items:
        choices.append((section, items[0]))
sheet = Image.new('RGB', (1440, 3 * 300), (18, 23, 29))
draw = ImageDraw.Draw(sheet)
for i, (title, row) in enumerate(choices):
    im = Image.open(root / 'frames' / f'{row["image"]:05d}.jpg').resize((480, 270))
    x, y = (i % 3)*480, (i//3)*300
    sheet.paste(im, (x,y)); draw.text((x+8,y+277), title, fill='white')
sheet.save(root / 'Native-Review.jpg', quality=95)
print(json.dumps({'video':str(output), 'seconds':offset, 'simulationHz':hz, 'passed':result['passed']}))
