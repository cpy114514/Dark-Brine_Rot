"""Contact sheets from actual final camera frames; no synthesized poses."""
import json
import math
import sys
from pathlib import Path
from PIL import Image, ImageDraw

folder = Path('.codex/shark-direction-20261023') / sys.argv[1]
file = folder / ('partial.json' if '--partial' in sys.argv else 'result.json')
data = json.loads(file.read_text())
rows = data['rows']
mapping = {m['frame']: m['imageIndex'] for m in data['frameMap']}
by_frame = {r['frame']: r for r in rows}
sections = [
    ('walk', lambda r: r['pose'] == 'PLAYER_Walk_v241', None),
    ('dodge', lambda r: r['stage'] == 'Tutorial' and r['lesson'] == 'Dodge' and r['executing'], [0, .15, .4, .65, .9, 1.1, 1.3, 1.5]),
    ('climb', lambda r: r['stage'] == 'Pursuit', [8.95, 9.15, 9.4, 9.8, 10.2, 10.8, 11.5, 12.3]),
    ('overhead', lambda r: r['stage'] == 'Duel', [3, 4.4, 4.75, 5, 5.2, 5.3, 5.5, 6.2]),
    ('rear-dodge', lambda r: r['stage'] == 'Duel', [9.9, 10.15, 10.35, 10.55, 10.8, 11.2, 12, 12.9]),
    ('get-up-throw', lambda r: r['stage'] == 'Duel', [13.05, 13.4, 14, 14.7, 15.3, 15.7, 16, 16.3]),
    ('tail-flight', lambda r: r['stage'] == 'Duel', [19.5, 20.2, 20.9, 21.3, 21.6, 21.75, 22, 22.5]),
]
for name, predicate, times in sections:
    candidates = [r for r in rows if predicate(r)]
    if not candidates:
        continue
    clock = 'lessonTime' if name == 'dodge' else 'localTime'
    picks = ([candidates[min(len(candidates)-1, int(i*(len(candidates)-1)/7))] for i in range(8)]
             if times is None else [min(candidates, key=lambda r: abs(r[clock]-t)) for t in times])
    for view in ('game', 'front', 'side'):
        sheet = Image.new('RGB', (4*320, 2*270), (20, 23, 26))
        draw = ImageDraw.Draw(sheet)
        for i, row in enumerate(picks):
            if view == 'game':
                path = folder / ('frame-%04d.png' % mapping.get(row['frame'], 0))
            else:
                options = list(folder.glob('detail-*-'+('0' if view == 'front' else '1')+'.png'))
                same_stage = [p for p in options if by_frame.get(int(p.stem.split('-')[1]), {}).get('stage') == row['stage']]
                same_clip = [p for p in same_stage if by_frame[int(p.stem.split('-')[1])]['pose'] == row['pose']]
                path = min(same_clip or same_stage, key=lambda p: abs(int(p.stem.split('-')[1])-row['frame']))
                row = by_frame[int(path.stem.split('-')[1])]
            with Image.open(path) as im:
                sheet.paste(im.resize((320, 240)), (i%4*320, i//4*270))
            draw.text((i%4*320+4, i//4*270+242), f'{name} {row[clock]:.3f}s / frame {row["frame"]}', fill='white')
            draw.text((i%4*320+4, i//4*270+255), row['pose'], fill='white')
        sheet.save(folder / ('poses-'+name+'-'+view+'.jpg'))
