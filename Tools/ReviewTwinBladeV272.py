"""Compose native before/after and two-turn finisher review frames."""
from pathlib import Path
from PIL import Image, ImageDraw

folder = Path('.codex/twin-blade-v272')
sheet = Image.new('RGB', (1600, 1248), '#171d25')
draw = ImageDraw.Draw(sheet)
for row, label in enumerate(('before', 'after')):
    for i in range(5):
        with Image.open(folder / f'30-{label}-{i}.png') as frame:
            sheet.paste(frame.resize((320, 240)), (i * 320, row * 272 + 24))
        draw.text((i * 320 + 8, row * 272 + 8), f'{label} | slash {i + 1}', fill='white')
for i in range(10):
    x, y = i % 5 * 320, 568 + i // 5 * 272
    with Image.open(folder / f'30-spin-{i}.png') as frame:
        sheet.paste(frame.resize((320, 240)), (x, y + 24))
    draw.text((x + 8, y + 8), f'two-turn finisher | {i + 1}', fill='white')
sheet.save(folder / 'review.jpg', quality=92)
print(folder / 'review.jpg')
