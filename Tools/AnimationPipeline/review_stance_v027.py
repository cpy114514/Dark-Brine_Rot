from pathlib import Path
from PIL import Image,ImageDraw
root=Path(__file__).resolve().parents[2]/'.codex/encounter-v027/source-preview'
sheet=Image.new('RGB',(1920,1612),(20,23,27));draw=ImageDraw.Draw(sheet)
for row,action in enumerate(('Guard','Parry','Slash','Brace')):
    for column,path in enumerate(sorted((root/action).glob('three-quarter-*.png'))):
        sheet.paste(Image.open(path).convert('RGB'),(column*384,row*403+19))
        draw.text((column*384+8,row*403+3),action+' '+path.stem,fill='white')
sheet.save(root/'diagnostic-stances.jpg')
