"""Compare the native blade geometry against the character's +Z facing arrow."""
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont

folder = Path('.codex/twin-blade-v273')
font = ImageFont.truetype('C:/Windows/Fonts/msyh.ttc', 24)
sheet = Image.new('RGB', (1280, 524), '#131b25')
draw = ImageDraw.Draw(sheet)
for column, (label, title) in enumerate((('before', '调整前：主要扫向侧面'), ('after', '调整后：斜斩收向面前'))):
    with Image.open(folder / f'{label}-0-44-top.png') as frame:
        sheet.paste(frame, (640 * column, 44))
    draw.text((640 * column + 20, 10), title, fill='white', font=font)
sheet.save(folder / 'comparison.jpg', quality=94)
print(folder / 'comparison.jpg')
