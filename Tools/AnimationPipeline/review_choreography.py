"""Contact sheets labelled in main-film time, preserving separate physical entry."""
import json,sys,math
from pathlib import Path
from PIL import Image,ImageDraw
ROOT=Path(__file__).resolve().parents[2]
folder=ROOT/'.codex'/('encounter-'+sys.argv[1])
metrics=json.loads((folder/'preview/pose-metrics.json').read_text())
lead=metrics['LeadInSeconds']
beats=[.6,1.184,2.43,4.2,5.5,7.03,8.94,10.8,12.2,13.1,14.572,16.7,18.05,18.5,19.7,20.7]
for view in ('game','front','side','three-quarter'):
    timing=folder/('preview/frame-times.json' if view=='game' else 'multiview/times.json')
    if not timing.exists():continue
    times=json.loads(timing.read_text());sheet=Image.new('RGB',(1920,1172),(18,22,28));draw=ImageDraw.Draw(sheet)
    for k,t in enumerate(beats):
        i=min(range(len(times)),key=lambda i:abs(times[i]-lead-t))
        path=folder/('preview/frames/'+f'{i:04}.png' if view=='game' else f'multiview/{view}-{i:04}.png')
        im=Image.open(path).resize((480,270));x=k%4*480;y=k//4*293
        sheet.paste(im,(x,y+23));draw.text((x+8,y+6),f'{view} main {times[i]-lead:.3f}s',fill='white')
    sheet.save(folder/(view+'-main-sheet.jpg'))
print(folder)
