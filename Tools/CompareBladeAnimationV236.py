"""Align the two native recordings around the reviewed input/ownership changes."""
import json
import math
import subprocess
import sys
from pathlib import Path
from PIL import Image, ImageDraw

root = Path.cwd()
folder = root / '.codex/blade-animation-polish-20261021'
before = json.loads((folder / 'baseline-60/review.json').read_text())
after = json.loads((folder / 'after-60/review.json').read_text())
metrics_before = json.loads((folder / 'baseline-60/metrics.json').read_text())
metrics_after = json.loads((folder / 'after-60/metrics.json').read_text())

def movement_step(data):
    frames = data['frames']
    hands = [data['bones'].index(b) for b in ('LeftHand', 'RightHand')]
    return max(math.degrees(2 * math.acos(min(1, abs(sum(a * b for a, b in zip(frames[i-1]['rotations'][hand], frames[i]['rotations'][hand]))))))
               for i in range(1, len(frames)) for hand in hands
               if frames[i-1]['section'] == frames[i]['section'] == 'Idle to movement')

result = dict(release_before=metrics_before, release_after=metrics_after,
              movement_hand_step_before=movement_step(before), movement_hand_step_after=movement_step(after))
result['passed'] = before['passed'] and after['passed'] and all(
    metrics_after[i]['max_arm_step'] < metrics_before[i]['max_arm_step'] * .25 for i in (0, 1)) and result['movement_hand_step_after'] < result['movement_hand_step_before'] * .3
(folder / 'comparison.json').write_text(json.dumps(result, indent=2))
print(json.dumps(result, indent=2))
if not result['passed']:
    raise SystemExit('Transition improvement check failed')

sys.path.insert(0, str(root / '.codex/encounter-redesign-20261017/python-deps'))
import imageio_ffmpeg
cmd = [imageio_ffmpeg.get_ffmpeg_exe(), '-hide_banner', '-loglevel', 'error', '-y', '-f', 'rawvideo', '-pixel_format', 'rgb24',
       '-video_size', '1280x400', '-framerate', '60', '-i', '-', '-an', '-c:v', 'libx264', '-threads', '2', '-crf', '19',
       '-pix_fmt', 'yuv420p', '-movflags', '+faststart', str(folder / 'BladeAnimation-BeforeAfter.mp4')]
proc = subprocess.Popen(cmd, stdin=subprocess.PIPE, creationflags=subprocess.CREATE_NO_WINDOW)
for name, start_offset, end_offset in [('Charged rush', -18, 65), ('Early charge cancel', -18, 50), ('Idle to movement', 0, 100)]:
    centers = []
    for data in (before, after):
        frames = data['frames']
        if name == 'Idle to movement':
            center = next(i for i, f in enumerate(frames) if f['section'] == name)
        else:
            center = next(i for i, f in enumerate(frames) if i > 0 and f['section'] == name and frames[i-1]['charging'] and not f['charging'])
        centers.append(center)
    for offset in range(start_offset, end_offset):
        canvas = Image.new('RGB', (1280, 400), (19, 20, 23))
        draw = ImageDraw.Draw(canvas)
        for column, (take, center) in enumerate(zip(('baseline-60', 'after-60'), centers)):
            with Image.open(folder / take / 'frames' / f'{center + offset + 1:05d}.png') as im:
                canvas.paste(im.resize((640, 360)), (column * 640, 30))
            draw.text((column * 640 + 10, 9), ('BEFORE' if column == 0 else 'AFTER') + ' | ' + name, fill='white')
        proc.stdin.write(canvas.tobytes())
proc.stdin.close()
if proc.wait() != 0:
    raise SystemExit('Comparison encoding failed')
