import json,sys,subprocess
from pathlib import Path
from PIL import Image,ImageDraw,ImageFont
root=Path.cwd();folder=root/'.codex/combat-combinations-20261019';j=json.loads((folder/'tests-60hz.json').read_text(encoding='utf-8-sig'))
sys.path.insert(0,str(root/'.codex/encounter-redesign-20261017/python-deps'));import imageio_ffmpeg
out=folder/'review-frames';out.mkdir(exist_ok=True);font=ImageFont.truetype('C:/Windows/Fonts/arial.ttf',18)
sections={'Club spin','Blade X charge and rush','Rush contact','Blade guard','Blade spin','Jump chop'}
selected=[(n+1,r) for n,r in enumerate(j['frames']) if (n+1)%4==0 and r['section'] in sections and (folder/'frames-reviewed60'/f'{n+1:05d}.png').exists()]
manifest=[]
for idx,(n,row) in enumerate(selected):
 im=Image.open(folder/'frames-reviewed60'/f'{n:05d}.png').convert('RGB');d=ImageDraw.Draw(im);d.rectangle((0,0,960,34),fill=(16,19,25));tag=' | LOCAL HIT STOP' if row['stop'] else ''
 d.text((12,7),'Native Unity preview | '+row['section']+tag,fill=(238,239,243),font=font)
 path=out/f'{idx:04d}.png';im.save(path);manifest.append("file '"+path.as_posix()+"'")
 duration=selected[idx+1][1]['time']-row['time'] if idx+1<len(selected) and selected[idx+1][1]['section']==row['section'] else .15
 manifest.append(f'duration {max(.016,min(duration,.2)):.6f}')
(folder/'review-concat.txt').write_text('\n'.join(manifest))
subprocess.run([imageio_ffmpeg.get_ffmpeg_exe(),'-hide_banner','-loglevel','error','-y','-f','concat','-safe','0','-i',str(folder/'review-concat.txt'),'-vf','fps=30','-c:v','libx264','-crf','20','-pix_fmt','yuv420p','-movflags','+faststart',str(folder/'combat-combinations-review.mp4')],check=True)
sheet=Image.new('RGB',(1440,900),(20,20,25));draw=ImageDraw.Draw(sheet)
for idx,section in enumerate(['Club spin','Blade X charge and rush','Rush contact','Blade guard','Blade spin','Jump chop']):
 choices=[(n,r) for n,r in selected if r['section']==section];event=next((e for e in j['events'] if e['section']==section),None)
 target=event['time'] if event else choices[len(choices)//2][1]['time'];n,row=min(choices,key=lambda x:abs(x[1]['time']-target))
 im=Image.open(folder/'frames-reviewed60'/f'{n:05d}.png').resize((480,270));x=idx%3*480;y=idx//3*450;sheet.paste(im,(x,y+32));draw.text((x+10,y+8),section,fill='white',font=font)
sheet.save(folder/'contact-sheet.jpg');print('Native preview frames:',len(selected))
