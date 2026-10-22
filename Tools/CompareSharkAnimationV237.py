"""Align the native before/after camera recordings at the reviewed movement seams."""
import json
import math
import subprocess
import sys
from pathlib import Path
from PIL import Image, ImageDraw

root = Path.cwd()
folder = root / '.codex/shark-animation-polish-20261021'
labels = ['baseline-30', sys.argv[1]]
data = [json.loads((folder / label / 'result.json').read_text()) for label in labels]
maps = [{x['frame']: x['imageIndex'] for x in d['frameMap']} for d in data]
seams = [('Pursuit', 0, 1, 'Swim handoff'), ('Pursuit', 3.85, 4.75, 'Side bite transition'),
         ('Pursuit', 7.35, 8.25, 'Climb approach'), ('Duel', 12.85, 15.45, 'Retreat and turn')]
comparison = []
for stage, low, high, name in seams:
    values = []
    for d in data:
        rows = d['rows']
        pairs = [(a, b) for a, b in zip(rows, rows[1:]) if b['stage'] == stage and low <= b['localTime'] <= high]
        values.append(max(math.dist(a['nose'], b['nose']) for a, b in pairs))
    comparison.append(dict(name=name, before_maximum_nose_step=values[0], after_maximum_nose_step=values[1]))
(folder / 'comparison.json').write_text(json.dumps(comparison, indent=2))
print(json.dumps(comparison, indent=2))

sys.path.insert(0, str(root / '.codex/encounter-redesign-20261017/python-deps'))
import imageio_ffmpeg
proc = subprocess.Popen([imageio_ffmpeg.get_ffmpeg_exe(), '-hide_banner', '-loglevel', 'error', '-y', '-f', 'rawvideo',
                         '-pixel_format', 'rgb24', '-video_size', '1280x400', '-framerate', '30', '-i', '-', '-an', '-c:v', 'libx264',
                         '-threads', '2', '-crf', '19', '-pix_fmt', 'yuv420p', '-movflags', '+faststart',
                         str(folder / 'SahurShark-BeforeAfter-v237.mp4')], stdin=subprocess.PIPE, creationflags=subprocess.CREATE_NO_WINDOW)
for stage, low, high, name in seams:
    sections = [[r for r in d['rows'] if r['stage'] == stage and r['frame'] in mapping] for d, mapping in zip(data, maps)]
    for n in range(round((high-low)*30)):
        time = low + n / 30
        canvas = Image.new('RGB', (1280, 400), (19, 20, 23)); draw = ImageDraw.Draw(canvas)
        for column in (0, 1):
            row = min(sections[column], key=lambda r: abs(r['localTime']-time))
            path = folder / labels[column] / ('frame-%04d.png' % maps[column][row['frame']])
            with Image.open(path) as im:
                canvas.paste(im.resize((640, 360)), (column * 640, 30))
            draw.text((column*640+10, 9), ('BEFORE' if column == 0 else 'AFTER') + ' | ' + name, fill='white')
        proc.stdin.write(canvas.tobytes())
proc.stdin.close()
if proc.wait() != 0:
    raise SystemExit('Comparison encoding failed')
