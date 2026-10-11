"""Compare actual original-Avatar meshes, fixed-camera spacing and contact."""
import json, math, subprocess
from pathlib import Path
from PIL import Image, ImageDraw
import imageio_ffmpeg
root=Path(__file__).resolve().parents[2]
folder=root/'.codex/encounter-v036/native-continuous'
rows=json.loads((folder/'native-samples.json').read_text())
def distance(a,b):return math.sqrt(sum((x-y)**2 for x,y in zip(a,b)))
def angle(a,b):
    return math.degrees(math.acos(max(-1,min(1,sum(x*y for x,y in zip(a,b))))))
report={}
for version in ('v033','v036'):
    samples=[r for r in rows if r['name']==f'CH1_SahurSlash_{version}']
    if len(samples)!=85:raise ValueError(f'Incomplete native samples: {version}')
    steps=[dict(frame=i+1,seconds=samples[i]['time'],
                hand_speed=distance(samples[i]['rightHand'],samples[i-1]['rightHand'])*60,
                shaft_degrees=angle(samples[i]['shaftDirection'],samples[i-1]['shaftDirection']))
           for i in range(1,85)]
    preparation=[s for s in steps if .15<s['seconds']<.55]
    report[version]=dict(preparation_peak_shaft_degrees_per_frame=max(s['shaft_degrees'] for s in preparation),
        preparation_peak_hand_metres_per_second=max(s['hand_speed'] for s in preparation),
        steps=steps)
    for view in ('front','side','three-quarter'):
        for page in range(3):
            sheet=Image.new('RGB',(1200,1100),(18,22,28));draw=ImageDraw.Draw(sheet)
            for k,i in enumerate(range(page*30,min(85,page*30+30))):
                path=folder/f'CH1_SahurSlash_{version}-{view}-{i:02d}.png'
                frame=Image.open(path).resize((200,200));x=k%6*200;y=k//6*220
                sheet.paste(frame,(x,y+20));draw.text((x+4,y+3),f'{version} {view} F{i+1} {i/60:.3f}s',fill='white')
            sheet.save(folder/f'{version}-{view}-sequence-{page}.jpg')
        output=folder/f'{version}-{view}-60fps-review.mp4'
        subprocess.run([imageio_ffmpeg.get_ffmpeg_exe(),'-hide_banner','-loglevel','error','-y',
            '-framerate','60','-i',str(folder/f'CH1_SahurSlash_{version}-{view}-%02d.png'),
            '-frames:v','84','-c:v','libx264','-crf','18','-pix_fmt','yuv420p',
            '-movflags','+faststart',str(output)],check=True)
old=[r for r in rows if r['name']=='CH1_SahurSlash_v033']
new=[r for r in rows if r['name']=='CH1_SahurSlash_v036']
# F39 (.6333s) is the first 60 FPS sample following the exact .63s cue.
report['contact_near_cue_difference']={key:distance(old[38][key],new[38][key])
    for key in ('rightHand','rightElbow','shaftDirection','leftFoot','rightFoot','hips')}
report['post_contact_max_position_difference']=max(distance(a[key],b[key])
    for a,b in zip(old[38:],new[38:]) for key in ('rightHand','leftFoot','rightFoot','hips'))
(folder/'native-comparison.json').write_text(json.dumps(report,indent=2))
print(json.dumps({k:({x:v for x,v in r.items() if x!='steps'} if isinstance(r,dict) else r)
                  for k,r in report.items()},indent=2))
