import json
from pathlib import Path
from PIL import Image, ImageDraw
root=Path('.codex/tutorial-cap-v263')
p=root/'opening-final'/'review.json-frames.json'
meta=json.loads(p.read_text(encoding="utf-8"))
picks=[]
for lesson in ('Move','Sprint','Attack','Dodge','Special','Jump','Swim'):
 rows=[(i,r) for i,r in enumerate(meta) if r['stage']=='Tutorial' and r['lesson']==lesson]
 if rows:
  waits=[x for x in rows if x[1]['waiting']]
  picks.append((waits[len(waits)//2] if waits else rows[len(rows)//2]))
  if lesson=='Special':picks+=rows[len(rows)//3::max(1,len(rows)//3)][:2]
w,h=480,270
im=Image.new('RGB',(w*3,(h+26)*((len(picks)+2)//3)),(15,15,15));d=ImageDraw.Draw(im)
for n,(i,r) in enumerate(picks):
 x=n%3*w;y=n//3*(h+26)
 im.paste(Image.open(root/'opening-final'/'review.json-images'/f'{i:05d}.jpg').resize((w,h)),(x,y+26))
 d.text((x+6,y+5),f"{r['lesson']} t={r['t']:.2f} {'WAITING' if r['waiting'] else ''}",fill='white')
im.save(root/'tutorial-final-sheet.jpg')
print(picks and [(i,r['lesson'],round(r['t'],2)) for i,r in picks])

