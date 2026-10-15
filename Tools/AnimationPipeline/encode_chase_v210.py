"""Encode consecutive native pursuit/fight captures; no synthetic camera or poses."""
import json,sys,subprocess
from pathlib import Path
import imageio_ffmpeg
from PIL import Image,ImageDraw
label=sys.argv[1];fps=int(sys.argv[2])
root=Path('D:/indiegameDev/unity/Dark Brine_Rot/.codex/chase-entry-20261014')/label
report=json.loads((root/'result.json').read_text(encoding='utf-8-sig'))
subprocess.run([imageio_ffmpeg.get_ffmpeg_exe(),'-hide_banner','-loglevel','error','-y','-framerate',str(fps),'-i',str(root/'game-%04d.png'),'-vf','format=yuv420p','-c:v','libx264','-preset','fast','-crf','20','-movflags','+faststart',str(root/'pursuit-and-fight.mp4')],check=True)
for view in ('front','side','three-quarter'):
    if (root/(view+'-0000.png')).exists():
        subprocess.run([imageio_ffmpeg.get_ffmpeg_exe(),'-hide_banner','-loglevel','error','-y','-framerate',str(fps),'-i',str(root/(view+'-%04d.png')),'-vf','format=yuv420p','-c:v','libx264','-preset','fast','-crf','20','-movflags','+faststart',str(root/(view+'.mp4'))],check=True)
count=report['filmFrames'];main_start=count-report['mainFrames'];indices=[0,int(main_start*.25),int(main_start*.5),max(0,main_start-5)]
indices += [min(count-1,main_start+round(t*fps)) for t in (0,1.184,2.43,6,7.03,8.94,13,14.572,16,18.05,19,20.5)]
sheet=Image.new('RGB',(1920,1200),(20,20,20));draw=ImageDraw.Draw(sheet)
for i,frame in enumerate(indices):
    im=Image.open(root/f'game-{frame:04d}.png');im.thumbnail((480,270));x=(i%4)*480;y=(i//4)*300;sheet.paste(im,(x,y+25))
    draw.text((x+5,y+5),f'frame{frame}, '+('Pursuit/entry' if frame<main_start else f'fight {(frame-main_start)/fps:.2f}s'),fill='white')
sheet.save(root/'contact-sheet.jpg',quality=94)
print(json.dumps({k:v for k,v in report.items() if k!='boards'},indent=2))
