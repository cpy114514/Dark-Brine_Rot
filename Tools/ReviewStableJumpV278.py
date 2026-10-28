"""Review native jump poses and compare the complete humanoid, not only the hand."""
import json,math,sys,subprocess
from pathlib import Path
from PIL import Image,ImageDraw,ImageFont

root=Path.cwd();folder=root/'.codex/jump-stable-v278'
take=folder/(sys.argv[1] if len(sys.argv)>1 else 'after-synced-headings-60')
data=json.loads((take/'review.json').read_text());frames=data['frames']
bad=[];tests=0;directions=[];body=[]
for f in frames:
    for g in f.get('geometry') or []:
        tests+=g.get('exactTests',0)
        if g.get('exactParts'):bad.append(dict(section=f['section'],phase=f['phase'],parts=g['exactParts']))
    if not f['section'].endswith('early chop') or not .40<=f['phase']<=.68:continue
    hand=f['positions'][7];tip=max([f['weapon']['a'],f['weapon']['b']],key=lambda x:sum((s-t)**2 for s,t in zip(x,hand)))
    directions.append(dict(section=f['section'],phase=f['phase'],tip=tip,angle=abs(math.degrees(math.atan2(tip[0],tip[2])))))
    v=f['bodyForward'];body.append(abs(math.degrees(math.atan2(v[0],v[2]))))
def peaks(file):
    return {c['mode']:{p['bone']:p['peak'] for p in c['peaks']} for c in json.loads((folder/file).read_text())}
before=peaks('baseline.json');after=peaks('candidate-audit.json')
limbs=['LeftUpperLeg','RightUpperLeg','LeftLowerLeg','RightLowerLeg','LeftFoot','RightFoot','LeftUpperArm','RightUpperArm','LeftLowerArm','RightLowerArm','LeftHand','RightHand']
comparison={mode:{bone:dict(before=before[mode][bone],after=after[mode][bone]) for bone in limbs+['Hips','Spine','Chest','Neck','Head']} for mode in before}
forward=bool(directions) and all(d['tip'][2]>.25 and d['angle']<25 for d in directions)
summary=dict(passed=data['passed'] and forward and not bad,checks=len(data['checks']),frames=len(frames),simulation_hz=data['simulationHz'],contact_samples=len(directions),max_contact_side_angle=max((d['angle'] for d in directions),default=180),max_body_facing_angle=max(body,default=180),non_grip_crossings=bad,exact_edge_tests=tests,source_120hz_bone_peaks=comparison,directions=directions)
(take/'acceptance.json').write_text(json.dumps(summary,indent=2))
font=ImageFont.truetype('C:/Windows/Fonts/msyh.ttc',17);sheet=Image.new('RGB',(1600,520),'#161e27');draw=ImageDraw.Draw(sheet)
sections=['Stick early chop','CapriTwinBlades early chop']
for row,section in enumerate(sections):
    indices=[i for i,f in enumerate(frames) if f['section']==section and f['phase']>0]
    if not indices:continue
    for col,phase in enumerate([.20,.34,.44,.54,.66]):
        i=min(indices,key=lambda i:abs(frames[i]['phase']-phase));x,y=col*320,row*260+30
        sheet.paste(Image.open(take/'frames'/f'{i+1:05d}.png').resize((320,180)),(x,y))
        draw.text((x+5,y-26),('木棍' if row==0 else '双刀')+' · 朝 Sahur 自身正前方劈落',fill='white',font=font)
        draw.text((x+5,y+185),f'phase {frames[i]["phase"]:.2f}',fill='white')
sheet.save(take/'native-review.jpg',quality=94)
sys.path.insert(0,str(root/'.codex/encounter-redesign-20261017/python-deps'));import imageio_ffmpeg
selected=[i for i,f in enumerate(frames) if f['section'] in sections]
if selected:
    pieces=[]
    for n,section in enumerate(sections):
        ids=[i for i,f in enumerate(frames) if f['section']==section]
        if not ids:continue
        piece=take/f'piece{n}.mp4';pieces.append(piece)
        subprocess.run([imageio_ffmpeg.get_ffmpeg_exe(),'-hide_banner','-loglevel','error','-y','-framerate',str(data['simulationHz']),'-start_number',str(min(ids)+1),'-i',str(take/'frames/%05d.png'),'-frames:v',str(max(ids)-min(ids)+1),'-c:v','libx264','-threads','2','-crf','19','-pix_fmt','yuv420p',str(piece)],check=True,creationflags=subprocess.CREATE_NO_WINDOW)
    concat=take/'concat.txt';concat.write_text(''.join("file '"+p.name+"'\n" for p in pieces))
    subprocess.run([imageio_ffmpeg.get_ffmpeg_exe(),'-hide_banner','-loglevel','error','-y','-f','concat','-safe','0','-i',str(concat),'-c','copy','-movflags','+faststart',str(take/'Sahur-ForwardJump-v278.mp4')],check=True,creationflags=subprocess.CREATE_NO_WINDOW)
print(json.dumps({k:v for k,v in summary.items() if k not in ['source_120hz_bone_peaks','directions']},indent=2))
