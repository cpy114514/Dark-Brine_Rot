import json, shutil
from pathlib import Path
from PIL import Image, ImageDraw
ROOT=Path(__file__).resolve().parents[2]
source=ROOT/'.codex/shark-finale-preview'
target=ROOT/'.codex/encounter-v001/finale-final';target.mkdir(parents=True,exist_ok=True)
times=json.loads((source/'frame-times.json').read_text())
groups=[[.3,1.2,1.8,2,3.5,4],[4.7,5.5,6.6,7.8,8.9,9.2],[9.7,10.8,11.6,12.1,13,14]]
for n,group in enumerate(groups):
    sheet=Image.new('RGB',(1440,1284),(18,22,28));draw=ImageDraw.Draw(sheet)
    for k,t in enumerate(group):
        index=min(range(len(times)),key=lambda i:abs(times[i]-t));path=source/'frames'/f'{index:04}.png'
        image=Image.open(path).resize((720,405));x=k%2*720;y=k//2*428
        sheet.paste(image,(x,y+23));draw.text((x+8,y+6),f't={times[index]:.3f}s / frame {index}',fill='white')
        shutil.copy2(path,target/f't-{t:.1f}.png')
    sheet.save(target/f'sheet-{n+1}.jpg')
shutil.copy2(source/'frame-times.json',target/'frame-times.json')
print(target)

review=ROOT/'.codex/encounter-v001/multiview'
review_times=json.loads((review/'times.json').read_text())
key_times=[.3,1.2,1.8,2,3.65,4,4.7,5.5,6.6,8.9,9.2,10.8,11.6,12.3,13.3,14.2]
for view in ('front','side','three-quarter'):
    sheet=Image.new('RGB',(1920,1172),(18,22,28));draw=ImageDraw.Draw(sheet)
    for k,t in enumerate(key_times):
        index=min(range(len(review_times)),key=lambda i:abs(review_times[i]-t))
        image=Image.open(review/f'{view}-{index:04}.png').resize((480,270))
        x=k%4*480;y=k//4*293;sheet.paste(image,(x,y+23));draw.text((x+8,y+6),f'{view} t={review_times[index]:.3f}s',fill='white')
    sheet.save(review/f'{view}-sheet.jpg')
impact_times=json.loads((source/'impact-times.json').read_text())
sheet=Image.new('RGB',(1440,1284),(18,22,28));draw=ImageDraw.Draw(sheet)
for k,t in enumerate([1.1,1.55,1.85,1.95,2.1,3.2]):
    index=min(range(len(impact_times)),key=lambda i:abs(impact_times[i]-t))
    image=Image.open(source/'impact-frames'/f'{index:04}.png');x=k%2*720;y=k//2*428
    sheet.paste(image,(x,y+23));draw.text((x+8,y+6),f'tail/ship t={impact_times[index]:.3f}s',fill='white')
sheet.save(target/'ship-impact-sheet.jpg')
