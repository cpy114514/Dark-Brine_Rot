"""Consecutive actual camera frames; these sheets are review aids, not retimed motion."""
import json
import sys
from pathlib import Path
from PIL import Image, ImageDraw

folder = Path('.codex/animation-performance-20261019') / sys.argv[1]
result = json.loads((folder / 'result.json').read_text())
film = json.loads((folder / 'film-solids.json').read_text())
rows = {row['frame']: row for row in result['rows']}
targets = [('Pursuit', 8.5, 'climb-entry'), ('Pursuit', 10.0, 'climb-exit'),
           ('Duel', 5.9, 'overhead'), ('Duel', 11.6, 'prone'),
           ('Duel', 13.5, 'get-up'), ('Duel', 21.6, 'tail-fracture')]
for stage, time, name in targets:
    event_text = {'overhead': 'Overhead contact;', 'tail-fracture': 'Final tail contact;'}.get(name)
    if event_text:
        for event in result.get('RedesignEvents', []):
            if event.startswith(stage + ' ') and event_text in event:
                time = float(event.split()[1])
                break
    candidates = [sample for sample in film if sample['stage'] == stage]
    if not candidates:
        continue
    center = min(candidates, key=lambda sample: abs(sample['localTime'] - time))['frame']
    selected = [frame for frame in range(center - 5, center + 7) if frame in rows]
    sheet = Image.new('RGB', (1920, 390 * ((len(selected) + 2) // 3)), (16, 16, 16))
    draw = ImageDraw.Draw(sheet)
    for index, frame in enumerate(selected):
        x, y = index % 3 * 640, index // 3 * 390
        with Image.open(folder / f'game-{frame:05d}.png') as image:
            sheet.paste(image, (x, y))
        row = rows[frame]
        draw.text((x + 6, y + 363), f"{frame} | {row['stage']} | clock {row['clock']:.3f}s", fill='white')
    sheet.save(folder / f'transition-{name}.jpg')
    print(name, selected)
