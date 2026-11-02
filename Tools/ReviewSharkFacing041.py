"""Compare consecutive, native Unity captures; no synthesized animation frames."""
import json
import subprocess
import sys
from pathlib import Path

import imageio_ffmpeg
from PIL import Image, ImageDraw

ROOT = Path('.codex/shark041')


def load(folder):
    return json.loads((folder / 'review.json-frames.json').read_text(encoding='utf-8-sig'))


def image_at(folder, index):
    return Image.open(folder / 'review.json-images' / f'{index:05d}.jpg')


def main(label):
    before, after = ROOT / 'before', ROOT / label
    old, new = load(before), load(after)
    canvas = Image.new('RGB', (960, 5 * 290), '#12171e')
    draw = ImageDraw.Draw(canvas)
    for row, time in enumerate([18.05, 18.65, 19.1, 19.85, 20.75]):
        for col, (folder, frames) in enumerate([(before, old), (after, new)]):
            i = min((i for i, r in enumerate(frames) if r['stage'] == 'Duel'),
                    key=lambda i: abs(frames[i]['t'] - time))
            r = frames[i]
            with image_at(folder, i) as image:
                canvas.paste(image.resize((480, 270)), (col * 480, row * 290 + 20))
            slope = f" slope {r['sharkFacing'][1]:.1f}" if 'sharkFacing' in r else ''
            draw.text((col * 480 + 5, row * 290 + 3),
                      f"{'BEFORE' if col == 0 else 'AFTER'} {r['t']:.2f}s shot {r['shot']}{slope}",
                      fill='#eee4c8')
    canvas.save(after / 'BeforeAfter.jpg', quality=94)
    # Review the whole changed interval and its adjoining shots at 0.2s spacing.
    interval = [i for i, r in enumerate(new) if r['stage'] == 'Duel' and 17.5 <= r['t'] <= 22.5]
    picks = [interval[0]]
    for i in interval[1:]:
        if new[i]['t'] - new[picks[-1]]['t'] >= .19:
            picks.append(i)
    height = ((len(picks) + 3) // 4) * 223
    sheet = Image.new('RGB', (1440, height), '#12171e')
    d = ImageDraw.Draw(sheet)
    for j, i in enumerate(picks):
        x, y = (j % 4) * 360, (j // 4) * 223
        with image_at(after, i) as image:
            sheet.paste(image.resize((360, 203)), (x, y + 20))
        r = new[i]
        d.text((x + 4, y + 3), f"{r['t']:.2f}s shot {r['shot']} slope {r['sharkFacing'][1]:.1f}", fill='#eee4c8')
    sheet.save(after / 'TurnSequence.jpg', quality=93)
    result = json.loads((after / 'review.json').read_text(encoding='utf-8-sig'))
    changes = [r for r in new if r['stage'] == 'Duel' and r['shot'] in [38, 39, 40]]
    faces = [r for r in changes if r['sharkFacing'][2] > .3]
    film = [r for r in new if r['stage'] in ['Pursuit', 'Duel']]
    result_summary = {k: result[k] for k in ['passed', 'frame', 'replayFps', 'contacts', 'tutorialHits', 'health', 'errors']}
    result_summary.update(changedFrames=len(changes),
                          changedMaxHeadWorldRoll=max(abs(r['sharkFacing'][0]) for r in changes),
                          changedMedianFaceSlope=sorted(abs(r['sharkFacing'][1]) for r in changes)[len(changes) // 2],
                          changedMaxFaceSlope=max(abs(r['sharkFacing'][1]) for r in changes),
                          frontFacingFrames=len(faces),
                          frontFacingMaxSlope=max(abs(r['sharkFacing'][1]) for r in faces),
                          filmMaxHeadWorldRoll=max(abs(r['sharkFacing'][0]) for r in film),
                          changedLensInsideWood=sum(r['lensInsideWood'] for r in changes),
                          changedMinLensWater=min(r['lensWater'] for r in changes))
    (after / 'metrics.json').write_text(json.dumps(result_summary, indent=2), encoding='utf-8')
    first = next(i for i, r in enumerate(new) if r['stage'] == 'Pursuit')
    subprocess.run([imageio_ffmpeg.get_ffmpeg_exe(), '-hide_banner', '-loglevel', 'error', '-y',
                    '-framerate', str(result['replayFps']), '-start_number', str(first),
                    '-i', str(after / 'review.json-images' / '%05d.jpg'), '-c:v', 'libx264',
                    '-crf', '20', '-preset', 'fast', '-pix_fmt', 'yuv420p', '-movflags', '+faststart',
                    str(after / 'SharkFight.mp4')], check=True)
    print(json.dumps(result_summary))


if __name__ == '__main__':
    main(sys.argv[1])
