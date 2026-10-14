import json,math,subprocess
from pathlib import Path
from PIL import Image,ImageDraw
import imageio_ffmpeg
root=Path(r'D:/indiegameDev/unity/Dark Brine_Rot/.codex/opening-optimization-20261012/native-framed-slash-v128')
rows=json.loads((root/'native-samples.json').read_text());clips=('CH1_SahurSlash_v051','CH1_SahurSlash_v128');frames=[0,14,27,34,38,46,57,72,84]
samples={name:[r for r in rows if r['name']==name] for name in clips}
distance=lambda a,b:math.sqrt(sum((x-y)**2 for x,y in zip(a,b)))
def angle(a,b):return math.degrees(math.acos(max(-1,min(1,sum(x*y for x,y in zip(a,b))))))
metrics={}
for name in clips:
    s=samples[name]
    metrics[name]=dict(max_shaft_step_degrees=max(angle(a['shaftDirection'],b['shaftDirection']) for a,b in zip(s,s[1:])),
        max_hand_step_m=max(distance(a['rightHand'],b['rightHand']) for a,b in zip(s,s[1:])))
impact=[]
for a,b in zip(samples[clips[0]],samples[clips[1]]):
    if .60<=a['time']<=.67:
        impact.append(dict(time=a['time'],hand_delta_m=distance(a['rightHand'],b['rightHand']),shaft_angle_degrees=angle(a['shaftDirection'],b['shaftDirection']),
            left_foot_delta_m=distance(a['leftFoot'],b['leftFoot']),right_foot_delta_m=distance(a['rightFoot'],b['rightFoot'])))
report=dict(original_avatar=True,source_fps=60,duration=1.4,metrics=metrics,contact_band=impact,staged_only=True,game_contact_acceptance=False)
(root/'comparison.json').write_text(json.dumps(report,indent=2));print(json.dumps(report,indent=2))
sheet=Image.new('RGB',(len(frames)*240,6*262),(26,28,30));d=ImageDraw.Draw(sheet)
for r,(name,view) in enumerate((n,v) for n in clips for v in ('front','side','three-quarter')):
    for c,f in enumerate(frames):
        im=Image.open(root/f'{name}-{view}-{f:03d}.png').resize((240,240));sheet.paste(im,(240*c,262*r));d.text((240*c+3,262*r+241),f'{name[-4:]} {view} {f/60:.2f}s',fill='white')
sheet.save(root/'contact-sheet.jpg')
for name in clips:
    for view in ('front','side','three-quarter'):
        subprocess.run([imageio_ffmpeg.get_ffmpeg_exe(),'-hide_banner','-loglevel','error','-framerate','60','-i',str(root/f'{name}-{view}-%03d.png'),
            '-vf','format=yuv420p','-c:v','libx264','-preset','fast','-crf','20','-movflags','+faststart',str(root/f'{name}-{view}-review.mp4')],check=True)
