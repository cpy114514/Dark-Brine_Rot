"""Summarize native attack takes; never infer artistic acceptance from measurements."""
import json,sys,collections
from pathlib import Path
from PIL import Image,ImageDraw
p=Path(sys.argv[1]);d=json.loads((p/'review.json').read_text());rows=[]
for i,f in enumerate(d['frames']):
 for w in f.get('geometry') or []:rows.append({'frame':i+1,'section':f['section'],'phase':f['phase'],**w})
hits=[r for r in rows if r['crossings']]
summary=dict(passed=d['passed'],checks=len(d['checks']),nativeFrames=len(d['frames']),sampledWeaponPoses=len(rows),testedRays=sum(r['rays'] for r in rows),surfaceCrossings=sum(r['crossings'] for r in rows),crossingPoses=hits,bodyParts=dict(collections.Counter(part for r in hits for part in r.get('parts',[]))))
(p/'geometry-summary.json').write_text(json.dumps(summary,indent=2));print(json.dumps({k:v for k,v in summary.items() if k!='crossingPoses'},indent=2))
sections=['Stick early chop','CapriTwinBlades early chop'];times=[.12,.24,.39,.51,.67,.82]
out=Image.new('RGB',(1440,848),'black');draw=ImageDraw.Draw(out)
for row,section in enumerate(sections):
 for col,phase in enumerate(times):
  candidates=[(i,f) for i,f in enumerate(d['frames']) if f['section']==section];i,f=min(candidates,key=lambda x:abs(x[1]['phase']-phase));x=(col%3)*480;y=(row*2+col//3)*212
  out.paste(Image.open(p/'frames'/f'{i+1:05}.png').resize((480,192)),(x,y+20));draw.text((x+5,y+3),section+' '+str(round(f['phase'],2)),fill='white')
out.save(p/'native-sheet.jpg')
