"""Analyze and encode consecutive native frames without retiming the moves."""
import json, math, sys, subprocess
from pathlib import Path
from PIL import Image, ImageDraw
root=Path.cwd();folder=root/'.codex/dual-blade-polish-20261019'/sys.argv[1]
data=json.loads((folder/'tests.json').read_text());frames=data['frames']
spin=[f for f in frames if f['section']=='Blade combo3' and f['stage']==2 and .27<=f['phase']<=.78]
yaws=[math.degrees(math.atan2(f['handLine'][2],f['handLine'][0])) for f in spin]
turn=sum((b-a+180)%360-180 for a,b in zip(yaws,yaws[1:]))
vertical=max(abs(f[key][1]) for f in spin for key in ('leftTip','rightTip'))
rush=[f for f in frames if f['section']=='Blade X charge and rush' and .5<=f['phase']<=.9]
back=[]
for f in rush:
    yaw=math.radians(f['rootYaw'])
    back.extend(-math.sin(yaw)*f[key][0]-math.cos(yaw)*f[key][2] for key in ('leftTip','rightTip'))
result=dict(passed=data['passed'] and 350<abs(turn)<370 and vertical<.03 and min(back)>.25 and max(f[key][1] for f in rush for key in ('leftTip','rightTip'))<-.5,
            simulation_hz=data['simulationHz'],checks=len(data['checks']),frames=len(frames),turn_degrees=turn,max_blade_vertical_component=vertical,
            minimum_followthrough_backward_component=min(back),maximum_followthrough_vertical_component=max(f[key][1] for f in rush for key in ('leftTip','rightTip')),
            notes='Native visible mesh directions use the actual vertex handle/tip and complete world matrix, not TransformDirection on a scaled hand.')
(folder/'measured-poses.json').write_text(json.dumps(result,indent=2));print(json.dumps(result,indent=2))
if not result['passed']:raise SystemExit('Native pose acceptance failed')
targets={
    'finisher':min((i for i,f in enumerate(frames) if f['section']=='Blade combo3' and f['stage']==2),key=lambda i:abs(frames[i]['phase']-.5)),
    'rush':min((i for i,f in enumerate(frames) if f['section']=='Blade X charge and rush'),key=lambda i:abs(frames[i]['phase']-.65))}
for section in ('Nailong light recoil','Nailong heavy recoil','Capri light recoil','Capri heavy recoil'):
    selected=[i for i,f in enumerate(frames) if f['section']==section];peak=max(frames[i]['recoilDegrees'] for i in selected)
    targets[section.replace(' ','-')]=next(i for i in selected if frames[i]['recoilDegrees']>=peak*.99)
for name,center in targets.items():
    indices=range(max(0,center-4),min(len(frames),center+5));sheet=Image.new('RGB',(1920,1152),(18,18,21));draw=ImageDraw.Draw(sheet)
    for n,i in enumerate(indices):
        x,y=n%3*640,n//3*384
        with Image.open(folder/'frames'/f'{i+1:05d}.png') as im:sheet.paste(im,(x,y))
        draw.text((x+6,y+362),f'{i+1} | {frames[i]["time"]:.3f}s | {name}',fill='white')
    sheet.save(folder/f'review-{name}.jpg')
sys.path.insert(0,str(root/'.codex/encounter-redesign-20261017/python-deps'));import imageio_ffmpeg
subprocess.run([imageio_ffmpeg.get_ffmpeg_exe(),'-hide_banner','-loglevel','error','-y','-framerate',str(data['simulationHz']),'-i',str(folder/'frames/%05d.png'),'-frames:v',str(len(frames)),'-c:v','libx264','-threads','2','-crf','20','-pix_fmt','yuv420p','-movflags','+faststart',str(folder/'dual-blade-review.mp4')],check=True)
