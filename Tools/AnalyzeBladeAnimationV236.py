"""Inspect final native poses and preserve a continuous, normally timed review video."""
import json
import math
import subprocess
import sys
from pathlib import Path
from PIL import Image, ImageDraw

root = Path.cwd()
folder = root / '.codex/blade-animation-polish-20261021' / sys.argv[1]
data = json.loads((folder / 'review.json').read_text())
frames = data['frames']
bones = data['bones']

def angle(a, b):
    dot = abs(sum(x * y for x, y in zip(a, b)))
    return math.degrees(2 * math.acos(min(1, dot)))

def delta(i, bone):
    return angle(frames[i - 1]['rotations'][bones.index(bone)], frames[i]['rotations'][bones.index(bone)])

targets = {}
metrics = []
for section in dict.fromkeys(f['section'] for f in frames):
    indices = [i for i, f in enumerate(frames) if f['section'] == section]
    valid = [i for i in indices if i > 0 and frames[i - 1]['section'] == section]
    for flag in ('charging', 'guard', 'spin'):
        for i in valid:
            if frames[i - 1][flag] and not frames[i][flag]:
                targets[section + ' ' + flag + ' release'] = i
                # Measure entry and the first 0.1s after the ownership change.
                near = [j for j in valid if i <= j < i + 7]
                metrics.append(dict(section=section, event=flag + ' release', frame=i + 1,
                                    max_hand_step=max(delta(j, b) for j in near for b in ('LeftHand', 'RightHand')),
                                    max_arm_step=max(delta(j, b) for j in near for b in ('LeftUpperArm', 'RightUpperArm', 'LeftLowerArm', 'RightLowerArm'))))
    if section == 'Idle to movement':
        peak = max(valid, key=lambda i: max(delta(i, b) for b in ('LeftHand', 'RightHand')))
        targets['movement wrist discontinuity'] = peak
    if section == 'Three hit combo':
        for stage in (1, 2):
            targets['combo stage ' + str(stage + 1)] = next(i for i in indices if frames[i]['stage'] == stage)
    if section == 'Confirmed blade contact':
        stops = [i for i in indices if frames[i]['stop']]
        if stops:
            targets['confirmed hit'] = stops[0]

for label, center in targets.items():
    sheet = Image.new('RGB', (1920, 936), (20, 21, 24))
    draw = ImageDraw.Draw(sheet)
    indices = range(max(0, center - 3), min(len(frames), center + 9))
    for n, i in enumerate(indices):
        x, y = n % 4 * 480, n // 4 * 312
        with Image.open(folder / 'frames' / f'{i + 1:05d}.png') as im:
            sheet.paste(im.resize((480, 270)), (x, y))
        f = frames[i]
        draw.text((x + 5, y + 274), f'{i + 1} | {f["time"]:.3f}s | {label}', fill='white')
        draw.text((x + 5, y + 291), f'phase {f["phase"]:.3f} | charge layer {f["chargeWeight"]:.2f}', fill='white')
    name = label.replace(' ', '-').lower()
    sheet.save(folder / (name + '.jpg'))

(folder / 'metrics.json').write_text(json.dumps(metrics, indent=2))
print(json.dumps(dict(passed=data['passed'], frames=len(frames), checks=len(data['checks']), metrics=metrics), indent=2))
sys.path.insert(0, str(root / '.codex/encounter-redesign-20261017/python-deps'))
import imageio_ffmpeg
subprocess.run([imageio_ffmpeg.get_ffmpeg_exe(), '-hide_banner', '-loglevel', 'error', '-y', '-framerate', str(data['simulationHz']),
                '-i', str(folder / 'frames/%05d.png'), '-frames:v', str(len(frames)), '-c:v', 'libx264', '-threads', '2',
                '-crf', '19', '-pix_fmt', 'yuv420p', '-movflags', '+faststart', str(folder / ('BladeAnimation-' + Path(sys.argv[1]).name + '.mp4'))],
               check=True, creationflags=subprocess.CREATE_NO_WINDOW)
