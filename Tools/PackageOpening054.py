"""Build native story review sheets and measured timing/contact observations."""
import argparse,json,math,subprocess
from pathlib import Path
import imageio_ffmpeg
from PIL import Image,ImageDraw

parser=argparse.ArgumentParser();parser.add_argument('take',type=Path);args=parser.parse_args();root=args.take.resolve()
rows=json.loads((root/'review.json-frames.json').read_text(encoding='utf-8-sig'))
result=json.loads((root/'review.json').read_text(encoding='utf-8-sig'))
def angle(a,b):
    return math.degrees(2*math.acos(min(1,abs(sum(x*y for x,y in zip(a,b))))))
def choose(stage,t):
    items=[(i,r) for i,r in enumerate(rows) if r['stage']==stage]
    return min(items,key=lambda x:abs(x[1]['t']-t))
def sheet(name,choices):
    out=Image.new('RGB',(1440,300*((len(choices)+2)//3)),(18,23,28));draw=ImageDraw.Draw(out)
    for k,(title,i) in enumerate(choices):
        im=Image.open(root/f'review.json-images/{i:05}.jpg').resize((480,270));x=k%3*480;y=k//3*300;out.paste(im,(x,y));draw.text((x+6,y+278),f'{title} / frame {rows[i]["frame"]}',fill='white')
    out.save(root/name,quality=95)
choices=[]
for lesson in ['ChargeHold','ChargeRelease','Special','Jump']:
    items=[(i,r) for i,r in enumerate(rows) if r['stage']=='Tutorial' and r['lesson']==lesson and r['waiting']]
    if items:
        for i,r in [items[0],items[len(items)//2],items[-1]]:choices.append((lesson+' input gate',i))
sheet('Tutorial-Review.jpg',choices)
choices=[]
for stage,times in [('Pursuit',[0.2,2,4,6,8.5,9.5,10.5,11.5,12.2]),('Duel',[10.2,10.7,11,11.3,11.5,11.7,12,12.3,12.6])]:
    for t in times:
        i,r=choose(stage,t);choices.append((f'{stage} {r["t"]:.2f}s',i))
sheet('Chase-Tail-Review.jpg',choices)
tail=[r for r in rows if r['stage']=='Duel' and 10.95<r['t']<12.6 and 'tailPosition' in r]
closest=min(tail,key=lambda r:math.dist([r['tailPosition'][0],r['tailPosition'][2]],[r['headPosition'][0],r['headPosition'][2]])) if tail else None
def release_changes():
    changes=[]
    for a,b in zip(rows,rows[1:]):
        if a['stage']=='Tutorial' and b['stage']=='Tutorial' and a['lesson']=='ChargeRelease' and a['waiting'] and not b['waiting']:
            changes.append({k:angle(a[k],b[k]) for k in ['rightUpperArmRotation','rightLowerArmRotation','rightHandRotation'] if k in a and k in b})
    return changes
metric=dict(passed=result['passed'],images=len(rows),tutorialHits=result['tutorialHits'],contacts=result['contacts'],health=result['health'],
            walkSeconds=max((r.get('walkControl',0) for r in rows),default=0),releaseEdgeDegrees=release_changes())
if closest:
    metric['tailOverHead']=dict(t=closest['t'],horizontalDistance=math.dist([closest['tailPosition'][0],closest['tailPosition'][2]],[closest['headPosition'][0],closest['headPosition'][2]]),verticalGap=closest['tailPosition'][1]-closest['headPosition'][1],viewport=closest['tailView'])
    i=rows.index(closest);sheet('Tail-Closest-Review.jpg',[(f'Tail at {rows[j]["t"]:.3f}s',j) for j in range(max(0,i-3),min(len(rows),i+6))])
metric['surfaceRollDegreesMax']=max((abs(r['sharkFacing'][0]) for r in rows if r['stage'] in ['Tutorial','Pursuit']),default=0)
(root/'metrics.json').write_text(json.dumps(metric,indent=2),encoding='utf-8')
# Stage excerpts use their actual captured frame spacing, including input gates.
items=[(i,r) for i,r in enumerate(rows) if r['stage'] in ['Pursuit','Duel'] and (r['stage']=='Pursuit' or 9.7<=r['t']<=13.5)]
lines=[]
for i,r in items:
    path=(root/f'review.json-images/{i:05}.jpg').as_posix();lines.extend(["file '"+path.replace("'","'\\''")+"'",f"duration {1/result['replayFps']:.9f}"])
if lines:
    lines.append(lines[-2]);(root/'review.ffconcat').write_text('\n'.join(lines),encoding='utf-8')
    subprocess.run([imageio_ffmpeg.get_ffmpeg_exe(),'-hide_banner','-loglevel','error','-y','-f','concat','-safe','0','-i',str(root/'review.ffconcat'),'-r','30','-c:v','libx264','-crf','18','-pix_fmt','yuv420p','-movflags','+faststart',str(root/'Chase-Tail-Native.mp4')],check=True)
print(json.dumps(metric))
