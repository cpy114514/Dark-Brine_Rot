import json,sys
from pathlib import Path
from PIL import Image,ImageDraw
p=Path('.codex/encounter-redesign-20261017')/sys.argv[1]; r=json.loads((p/'result.json').read_text())
rows=r['rows']; selected=[]
for stage,targets in [('Pursuit',[1.8,2.7,5.5,9.8,11.5]),('Duel',[0.5,4.7,5.4,10.6,11.6,13.6,16.8,20.9])]:
 s=[x for x in rows if x['stage']==stage]; origin=s[0]['clock']
 for t in targets: selected.append(min(s,key=lambda x:abs(x['clock']-origin-t)))
out=Image.new('RGB',(1280,390*((len(selected)+1)//2)),(20,20,20)); d=ImageDraw.Draw(out)
for i,row in enumerate(selected):
 im=Image.open(p/('game-%05d.png'%row['frame'])); x=(i%2)*640;y=(i//2)*390;out.paste(im,(x,y));d.text((x+8,y+362),f"{row['stage']} {row['clock']:.2f}s frame {row['frame']}: {row['beat']}",fill='white')
out.save(p/'baseline-sheet.jpg')
print([(s['frame'],s['beat']) for s in selected])
