"""Review final native captures; reject model-only snapshots as rendered-pose evidence."""
from pathlib import Path
import json, math, sys, subprocess
from PIL import Image,ImageDraw,ImageFont

root=Path.cwd(); folder=root/'.codex/jump-transitions-v276'; take=folder/'final-bound-30'
data=json.loads((take/'review.json').read_text()); frames=data['frames']
font=ImageFont.truetype('C:/Windows/Fonts/msyh.ttc',18)
sections=['Stick early chop','Stick moving recovery','CapriTwinBlades early chop','CapriTwinBlades moving recovery']
sheet=Image.new('RGB',(1920,1200),'#131b25'); draw=ImageDraw.Draw(sheet)
for row,section in enumerate(sections):
    indices=[i for i,f in enumerate(frames) if f['section']==section]
    active=[i for i in indices if frames[i]['phase']>0]
    chosen=[min(active,key=lambda i:abs(frames[i]['phase']-phase)) for phase in [.08,.25,.43,.61,.83]]
    recovery=[i for i in indices if frames[i].get('recovering')]
    chosen.append(min(indices[-1],recovery[-1]+3) if recovery else indices[-1])
    for col,index in enumerate(chosen):
        image=Image.open(take/'frames'/f'{index+1:05d}.png').resize((320,180))
        x,y=col*320,row*300+30;sheet.paste(image,(x,y))
        title=('木棍' if section.startswith('Stick') else '双刀')+(' · 接回跑动' if 'moving' in section else ' · 起跳至落地')
        draw.text((x+5,y-25),title,fill='white',font=font)
        draw.text((x+5,y+185),f'phase {frames[index]["phase"]:.2f}',fill='white')
sheet.save(folder/'native-review.jpg',quality=93)
bad=[]; tests=0
for frame in frames:
    for geometry in frame.get('geometry') or []:
        tests+=geometry.get('exactTests',0)
        parts=[p for p in geometry.get('exactParts',[]) if 'Hand' not in p]
        if parts:bad.append(dict(section=frame['section'],phase=frame['phase'],parts=parts))
summary=dict(passed=data['passed'] and not bad,checks=len(data['checks']),frames=len(frames),simulation_hz=data['simulationHz'],exact_edge_tests=tests,non_grip_surface_crossings=bad,
             recovery_frames=sum(bool(f.get('recovering')) for f in frames),
             rejected_preview='poses/ and pose-review.jpg use model-only editor sampling and are not final rendered-pose evidence')
(folder/'acceptance.json').write_text(json.dumps(summary,indent=2))
sys.path.insert(0,str(root/'.codex/encounter-redesign-20261017/python-deps'))
import imageio_ffmpeg
ranges=[]
for section in sections:
    indices=[i for i,f in enumerate(frames) if f['section']==section]
    ranges.append(f'between(n,{indices[0]},{indices[-1]})')
subprocess.run([imageio_ffmpeg.get_ffmpeg_exe(),'-hide_banner','-loglevel','error','-y','-framerate','30','-i',str(take/'frames/%05d.png'),
    '-vf',f"select={'+'.join(ranges).replace(',', r'\,')},setpts=N/30/TB",'-c:v','libx264','-threads','2','-crf','20','-pix_fmt','yuv420p','-movflags','+faststart',str(folder/'JumpChop-Transitions-v276.mp4')],check=True,creationflags=subprocess.CREATE_NO_WINDOW)
print(json.dumps(summary,indent=2))
