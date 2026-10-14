"""Temporal still review of actual natural ship impact; not FPS evidence."""
import json
from pathlib import Path
from PIL import Image, ImageDraw
root=Path(r'D:/indiegameDev/unity/Dark Brine_Rot/.codex/ship-tail-strike-v111')
times=json.loads((root/'times.json').read_text())
choices=[0,.6,1.2,1.8,2.0,2.2,2.4,2.7,3.1,3.6,4.2,4.6]
sheet=Image.new('RGB',(1440,4*295),(20,20,20))
draw=ImageDraw.Draw(sheet)
for n,t in enumerate(choices):
    i=min(range(len(times)),key=lambda i:abs(times[i]-t))
    im=Image.open(root/'frames'/f'{i:04d}.png');im.thumbnail((480,270))
    x=(n%3)*480;y=(n//3)*295
    sheet.paste(im,(x,y+25));draw.text((x+4,y+5),f'{times[i]:.3f}s',fill='white')
sheet.save(root/'contact-sheet.jpg',quality=93)
print(str(root/'contact-sheet.jpg'))
