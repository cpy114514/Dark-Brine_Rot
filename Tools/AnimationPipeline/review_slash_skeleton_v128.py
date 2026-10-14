from pathlib import Path
from PIL import Image,ImageDraw
root=Path(r'D:/indiegameDev/unity/Dark Brine_Rot/.codex/opening-optimization-20261012/slash-skeleton-v128')
frames=[1,15,28,35,39,47,58,73,85]
sheet=Image.new('RGB',(9*220,3*240),(36,38,40));d=ImageDraw.Draw(sheet)
for y,view in enumerate(['front','side','three-quarter']):
    for x,f in enumerate(frames):
        im=Image.open(root/f'{view}-{f:07.2f}.png').resize((220,220))
        sheet.paste(im,(220*x,240*y));d.text((220*x+3,240*y+220),f'{view} F{f} {(f-1)/60:.2f}s',fill='white')
sheet.save(root/'contact-sheet.jpg')

