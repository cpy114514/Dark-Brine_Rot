"""Verify forward/down primary-weapon geometry in final native camera frames."""
from pathlib import Path
import json,math,sys,subprocess
from PIL import Image,ImageDraw,ImageFont

root=Path.cwd();folder=root/'.codex/jump-forward-v277';take=folder/'final-bound-forward-30'
data=json.loads((take/'review.json').read_text());full=json.loads((folder/'after-clearance-30/review.json').read_text())
frames=data['frames'];bad=[];edge_tests=0
for frame in full['frames']:
    for g in frame.get('geometry') or []:
        edge_tests+=g.get('exactTests',0)
        parts=[x for x in g.get('exactParts',[]) if 'Hand' not in x]
        if parts:bad.append(dict(section=frame['section'],phase=frame['phase'],parts=parts))
directions=[]
for frame in frames:
    if not frame['section'].endswith('early chop') or not .40<=frame['phase']<=.68:continue
    a,b=frame['weapon']['a'],frame['weapon']['b'];hand=frame['positions'][7]
    distance=lambda p:sum((x-y)**2 for x,y in zip(p,hand))
    tip=a if distance(a)>distance(b) else b
    shaft=[x-y for x,y in zip(tip,hand)];length=math.sqrt(sum(x*x for x in shaft));shaft=[x/length for x in shaft]
    directions.append(dict(section=frame['section'],phase=frame['phase'],tip=tip,shaft=shaft,angle=abs(math.degrees(math.atan2(tip[0],tip[2])))))
forward=all(d['tip'][2]>.25 and d['angle']<25 and d['shaft'][1]<.2 for d in directions)
summary=dict(passed=data['passed'] and full['passed'] and not bad and forward,bound_checks=len(data['checks']),expanded_checks=len(full['checks']),bound_frames=len(frames),expanded_frames=len(full['frames']),
    forward_down_samples=len(directions),max_contact_side_angle=max(d['angle'] for d in directions),minimum_contact_forward=min(d['tip'][2] for d in directions),non_grip_surface_crossings=bad,exact_edge_tests=edge_tests,directions=directions)
(folder/'acceptance.json').write_text(json.dumps(summary,indent=2))
font=ImageFont.truetype('C:/Windows/Fonts/msyh.ttc',18);sheet=Image.new('RGB',(1600,600),'#131b25');draw=ImageDraw.Draw(sheet)
sections=['Stick early chop','CapriTwinBlades early chop']
for row,section in enumerate(sections):
    active=[i for i,f in enumerate(frames) if f['section']==section and f['phase']>0]
    for col,phase in enumerate([.24,.40,.48,.58,.67]):
        index=min(active,key=lambda i:abs(frames[i]['phase']-phase))
        image=Image.open(take/'frames'/f'{index+1:05d}.png').resize((320,180));x,y=col*320,row*300+30;sheet.paste(image,(x,y))
        draw.text((x+5,y-25),('木棍' if row==0 else '双刀')+' · 主武器向前劈下',fill='white',font=font)
        draw.text((x+5,y+185),f'phase {frames[index]["phase"]:.2f}',fill='white')
sheet.save(folder/'forward-review.jpg',quality=93)
sys.path.insert(0,str(root/'.codex/encounter-redesign-20261017/python-deps'));import imageio_ffmpeg
indices=[i for i,f in enumerate(frames) if f['section'] in sections];start,end=min(indices),max(indices)
subprocess.run([imageio_ffmpeg.get_ffmpeg_exe(),'-hide_banner','-loglevel','error','-y','-framerate','30','-i',str(take/'frames/%05d.png'),'-vf',f'trim=start_frame={start}:end_frame={end+1},setpts=PTS-STARTPTS',
    '-c:v','libx264','-threads','2','-crf','20','-pix_fmt','yuv420p','-movflags','+faststart',str(folder/'JumpChop-Forward-v277.mp4')],check=True,creationflags=subprocess.CREATE_NO_WINDOW)
print(json.dumps({k:v for k,v in summary.items() if k!='directions'},indent=2))
