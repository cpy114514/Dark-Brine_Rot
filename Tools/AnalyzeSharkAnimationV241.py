"""Review the final camera/pose map, without treating simulation rate as hardware FPS."""
import json
import math
import subprocess
import sys
from pathlib import Path
from PIL import Image, ImageDraw

root = Path.cwd()
folder = root / '.codex/shark-direction-20261023' / sys.argv[1]
data = json.loads((folder / ('partial.json' if '--partial' in sys.argv else 'result.json')).read_text())
rows = data['rows']
mapping = {r['frame']: r['imageIndex'] for r in data['frameMap']}

def distance(a, b):
    return math.sqrt(sum((x - y)**2 for x, y in zip(a, b)))

def angle(a, b):
    return math.degrees(2 * math.acos(min(1, abs(sum(x * y for x, y in zip(a, b))))))

steps = []
for i in range(1, len(rows)):
    a, b = rows[i-1], rows[i]
    if b['stage'] not in ('Pursuit', 'Duel'):
        continue
    steps.append(dict(index=i, frame=b['frame'], stage=b['stage'], time=b['localTime'], shot=b['shot'],
                      pose=b['pose'], previous_pose=a['pose'], nose_step=distance(a['nose'], b['nose']),
                      hero_step=distance(a['hero'], b['hero']), shark_turn=angle(a['sharkRotation'], b['sharkRotation']),
                      arm_step=max(angle(x['rotation'], y['rotation']) for x, y in zip(a['bones'], b['bones']) if x['name'] in ('LeftUpperArm', 'RightUpperArm', 'LeftHand', 'RightHand'))))
ranked = {key: sorted(steps, key=lambda r: r[key], reverse=True)[:12] for key in ('nose_step', 'hero_step', 'arm_step', 'shark_turn')}
summary = dict(frames=data['frame'], images=len(data['frameMap']), health=data['health'], events=data['events'],
               ranked=ranked, scales=list({tuple(r['scale']) for r in rows}),
               fractures=max(r['RedesignSolidFractures'] for r in rows), stages=list(dict.fromkeys(r['stage'] for r in rows)))
(folder / 'summary.json').write_text(json.dumps(summary, indent=2))
print(json.dumps({k: v for k, v in summary.items() if k not in ('events', 'ranked')}, indent=2))

targets = {}
for key, values in ranked.items():
    for n, row in enumerate(values[:3]):
        targets[f'{key}-{n+1}'] = row['index']
for stage, times in [('Pursuit', [3.9, 7.5, 9, 11.95]), ('Duel', [3, 5.3, 6.8, 10.35, 13, 15.5, 16.8, 17.8, 18, 19.5, 21.65])]:
    options = [i for i, r in enumerate(rows) if r['stage'] == stage and r['frame'] in mapping]
    if not options:
        continue
    for time in times:
        targets[f'{stage}-{time}'] = min(options, key=lambda i: abs(rows[i]['localTime']-time))
for label, center in targets.items():
    sheet = Image.new('RGB', (1920, 936), (20, 21, 24)); draw = ImageDraw.Draw(sheet)
    options = [i for i in range(max(0, center-4), min(len(rows), center+8)) if rows[i]['frame'] in mapping]
    for n, i in enumerate(options):
        row = rows[i]; x, y = n % 4 * 480, n // 4 * 312
        with Image.open(folder / ('frame-%04d.png' % mapping[row['frame']])) as im:
            sheet.paste(im.resize((480, 270)), (x, y))
        draw.text((x+5, y+273), f'{row["frame"]} | {row["stage"]} {row["localTime"]:.3f}s | shot {row["shot"]}', fill='white')
        draw.text((x+5, y+290), row['pose'] + f' {row["poseSeconds"]:.3f}s', fill='white')
    sheet.save(folder / ('review-' + label + '.jpg'))

if '--no-video' not in sys.argv:
    sys.path.insert(0, str(root / '.codex/encounter-redesign-20261017/python-deps'))
    import imageio_ffmpeg
    subprocess.run([imageio_ffmpeg.get_ffmpeg_exe(), '-hide_banner', '-loglevel', 'error', '-y', '-framerate', str(data['fps']),
                    '-start_number', '0', '-i', str(folder / 'frame-%04d.png'), '-frames:v', str(len(data['frameMap'])),
                    '-c:v', 'libx264', '-threads', '2', '-crf', '20', '-pix_fmt', 'yuv420p', '-movflags', '+faststart',
                    str(folder / ('SahurShark-' + Path(sys.argv[1]).name + '.mp4'))], check=True,
                   creationflags=subprocess.CREATE_NO_WINDOW)
