"""Package native frames using their recorded simulation-frame timestamps."""
import argparse
import json
import subprocess
from pathlib import Path
import imageio_ffmpeg
from PIL import Image, ImageDraw, ImageFont

parser=argparse.ArgumentParser()
parser.add_argument('take',type=Path)
args=parser.parse_args();root=args.take.resolve()
rows=json.loads((root/'frames.json').read_text());result=json.loads((root/'result.json').read_text());hz=result['hz']
labels={'approach':'APPROACH','grasp':'GRASP / CONTACT','climb':'SUSPENDED CLIMB','transit':'TRANSIT','island-pass':'SECOND ISLAND / RELEASE PASS','release':'FALL','impact':'IMPACT / BLACKOUT','eyes':'AWAKENING','get-up':'GET UP','dialogue':'FAIRY / DIALOGUE','gameplay':'CONTROL RESTORED'}
lines=[]
for i,row in enumerate(rows):
    path=(root/'frames'/f'{row["image"]:05d}.jpg').as_posix()
    lines.append("file '"+path.replace("'","'\\''")+"'")
    count=rows[i+1]['frame']-row['frame'] if i+1<len(rows) else 2
    lines.append(f'duration {count/hz:.9f}')
lines.append(lines[-2]);(root/'native-timing.ffconcat').write_text('\n'.join(lines),encoding='utf-8')
filters=[]
for phase,label in labels.items():
    items=[r for r in rows if r['phase']==phase]
    if items:
        a=(items[0]['frame']-rows[0]['frame'])/hz;b=(items[-1]['frame']-rows[0]['frame']+1)/hz
        filters.append("drawtext=fontfile='C\\:/Windows/Fonts/segoeui.ttf':text='"+label+f"':fontsize=22:fontcolor=white:box=1:boxcolor=black@0.5:boxborderw=8:x=24:y=48:enable='between(t,{a:.6f},{b:.6f})'")
subprocess.run([imageio_ffmpeg.get_ffmpeg_exe(),'-hide_banner','-loglevel','error','-y','-f','concat','-safe','0','-i',str(root/'native-timing.ffconcat'),'-vf',','.join(filters),'-r','30','-c:v','libx264','-crf','18','-pix_fmt','yuv420p','-movflags','+faststart',str(root/'Abduction-To-Island2-Native.mp4')],check=True)
phases=['approach','grasp','climb','island-pass','release','impact','eyes','get-up','dialogue']
out=Image.new('RGB',(1280,840),(15,20,25));draw=ImageDraw.Draw(out);font=ImageFont.truetype('C:/Windows/Fonts/segoeui.ttf',15)
for j,phase in enumerate(phases):
    items=[r for r in rows if r['phase']==phase]
    if not items:continue
    row=items[len(items)//2];im=Image.open(root/'frames'/f'{row["image"]:05d}.jpg').resize((426,240))
    x=j%3*426;y=j//3*280;out.paste(im,(x,y));draw.text((x+10,y+245),labels[phase],fill=(235,235,235),font=font)
out.save(root/'Native-Review.jpg',quality=95)
print(json.dumps({'video':str(root/'Abduction-To-Island2-Native.mp4'),'sourceImages':len(rows),'simulationHz':hz,'seconds':(rows[-1]['frame']-rows[0]['frame']+2)/hz}))
