"""Encode real consecutive Unity frames and review by actual recorded clock."""
import json,sys,subprocess
from pathlib import Path
from PIL import Image,ImageDraw
import imageio_ffmpeg
root=Path('D:/indiegameDev/unity/Dark Brine_Rot/.codex/chase-entry-20261014')/sys.argv[1]
fps=int(sys.argv[2]); rows=json.loads((root/'hydro.json').read_text()); report=json.loads((root/'result.json').read_text())
for prefix,out in [('game','pursuit-and-fight'),('front','front'),('side','side'),('three-quarter','three-quarter')]:
 if (root/f'{prefix}-0000.png').exists():
  subprocess.run([imageio_ffmpeg.get_ffmpeg_exe(),'-hide_banner','-loglevel','error','-y','-framerate',str(fps),'-i',str(root/(prefix+'-%04d.png')),'-vf','format=yuv420p','-c:v','libx264','-preset','fast','-crf','20','-movflags','+faststart',str(root/(out+'.mp4'))],check=True)
main=[r for r in rows if r['clock']>=0]
pre=[r for r in rows if r['frame']<main[0]['frame']] if main else rows
selected=[pre[min(len(pre)-1,round(t*(len(pre)-1)))] for t in [0,.25,.5,1]]
selected += [min(main,key=lambda r:abs(r['clock']-t)) for t in [0,1.184,2.43,6,7.03,8.94,13,14.572,16,18.05,19,20.5]] if main else []
def sheet(samples,name,cols=4,width=480):
 h=width*9//16+30;im=Image.new('RGB',(cols*width,((len(samples)+cols-1)//cols)*h),(20,20,20));d=ImageDraw.Draw(im)
 for i,r in enumerate(samples):
  p=Image.open(root/f"game-{r['frame']:04d}.png");p.thumbnail((width,width*9//16));x=i%cols*width;y=i//cols*h;im.paste(p,(x,y+28));d.text((x+5,y+5),f"f{r['frame']} t{r['clock']:.2f} head {r['headDry']:.2f}m tilt {r['boardTilt']:.1f}",fill='white')
 im.save(root/name,quality=93)
sheet(selected,'contact-sheet.jpg')
if pre:sheet([pre[i] for i in range(0,len(pre),max(1,round(fps*.35)))],'pursuit-motion.jpg',5,320)
if main:sheet([min(main,key=lambda r:abs(r['clock']-t/5)) for t in range(0,91)],'fight-motion.jpg',7,320)
q=[r for r in rows if 0<=r['clock']<18]
summary={k:{'minimum':min(r[k] for r in q),'maximum':max(r[k] for r in q),'belowZeroFrames':sum(r[k]<0 for r in q)} for k in ['headDry','chestDry','leftDry','rightDry','boardVerticalSpeed','boardTilt']} if q else {}
(root/'hydro-summary.json').write_text(json.dumps(summary,indent=2));print(json.dumps(summary,indent=2))
