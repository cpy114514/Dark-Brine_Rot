"""Encode untouched consecutive native camera frames; inspect dense timing sheets."""
import json,sys,subprocess
from pathlib import Path
from PIL import Image,ImageDraw
root=Path.cwd();folder=root/'.codex/dual-blade-rush-guard-20261019'/sys.argv[1]
d=json.loads((folder/'tests.json').read_text());frames=d['frames']
print(json.dumps(dict(passed=d['passed'],checks=len(d['checks']),frames=len(frames),metrics=[r for r in d['checks'] if 'passed' not in r]),indent=2))
targets={}
for label,section,phase in [('entry','Long charge and 16m rush',.20),('contact','Fully charged rush contact',.36),('release','Long charge and 16m rush',.56),('wall','Wall-resolved rush',.40)]:
 pool=[i for i,f in enumerate(frames) if f['section']==section and f['heavy']]
 if pool:targets[label]=min(pool,key=lambda i:abs(frames[i]['phase']-phase))
for i,f in enumerate(frames):
 if i and f['section']=='All frontal damage guard' and f['stamina']<frames[i-1]['stamina']-5:
  targets.setdefault('block',i)
for label,center in targets.items():
 sheet=Image.new('RGB',(1920,1164),(18,18,22));draw=ImageDraw.Draw(sheet)
 for n,i in enumerate(range(max(0,center-4),min(len(frames),center+5))):
  x,y=n%3*640,n//3*388
  with Image.open(folder/'frames'/f'{i+1:05d}.png') as im:sheet.paste(im,(x,y))
  f=frames[i];draw.text((x+6,y+361),f'{i+1} | {f["time"]:.3f}s | phase {f["phase"]:.3f} | immune {f["immune"]}',fill='white')
 sheet.save(folder/f'review-{label}.jpg')
sys.path.insert(0,str(root/'.codex/encounter-redesign-20261017/python-deps'));import imageio_ffmpeg
subprocess.run([imageio_ffmpeg.get_ffmpeg_exe(),'-hide_banner','-loglevel','error','-y','-framerate',str(d['simulationHz']),'-i',str(folder/'frames/%05d.png'),'-frames:v',str(len(frames)),'-c:v','libx264','-threads','2','-crf','20','-pix_fmt','yuv420p','-movflags','+faststart',str(folder/'rush-guard-review.mp4')],check=True)
