"""Review native Unity frame captures and clock/weapon metadata."""
import json
import subprocess
import sys
from pathlib import Path
from PIL import Image, ImageDraw
import imageio_ffmpeg

p=Path('.codex/perfect-dodge043')/sys.argv[1]
rows=json.loads((p/'frames.json').read_text(encoding='utf-8-sig'))
fps=json.loads((p/'result.json').read_text(encoding='utf-8-sig'))['fps']
sections=['keyboard buffered counter','gamepad immediate counter','twin blade counter','native Nailong counter']
selected=[]
for section in sections:
    candidates=[r for r in rows if r['section']==section and r['step']]
    if not candidates:continue
    start=candidates[0]['time']
    picks=[]
    for t in [start-.05,start+.035,start+.10,start+.18,start+.25,start+.31,start+.38,start+.45,start+.52,start+.60,start+.72,start+.9]:
        r=min((r for r in rows if (p/'frames'/f"{r['frame']:05d}.jpg").exists()),key=lambda r:abs(r['time']-t))
        picks.append(r)
    canvas=Image.new('RGB',(1440,3*223),'#11171b');draw=ImageDraw.Draw(canvas)
    for j,r in enumerate(picks):
        x,y=(j%4)*360,(j//4)*223
        with Image.open(p/'frames'/f"{r['frame']:05d}.jpg") as image:
            canvas.paste(image.resize((360,203)),(x,y+20))
        draw.text((x+3,y+3),f"{r['time']-start:+.3f}s {'step' if r['step'] else 'sweep' if r['counter'] else 'recover'}",fill='#ead9b2')
    out=section.replace(' ','-')+'.jpg';canvas.save(p/out,quality=93);selected.extend(picks)
images=sorted((p/'frames').glob('*.jpg'))
if images:
    subprocess.run([imageio_ffmpeg.get_ffmpeg_exe(),'-hide_banner','-loglevel','error','-y','-framerate',str(fps),'-start_number',str(int(images[0].stem)),'-i',str(p/'frames/%05d.jpg'),'-c:v','libx264','-crf','20','-preset','fast','-pix_fmt','yuv420p','-movflags','+faststart',str(p/'PerfectDodgeCounter.mp4')],check=True)
    print(json.dumps({'frames':len(images),'fps':fps,'movie':str(p/'PerfectDodgeCounter.mp4')}))
