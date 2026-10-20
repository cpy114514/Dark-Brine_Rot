"""Assemble captured native Editor frames using their recorded game times."""
import json, subprocess, sys
from pathlib import Path
from PIL import Image, ImageDraw
ROOT=Path(__file__).resolve().parents[1]
sys.path.insert(0,str(ROOT/'.codex/encounter-redesign-20261017/python-deps'))
import imageio_ffmpeg
folder=ROOT/'.codex/combat-feel-20261018'
report=json.loads((folder/'tests.json').read_text(encoding='utf-8-sig'))
rows=[r for r in report['observations'] if 'phase' in r]
events=[r for r in report['observations'] if 'kind' in r]
events=[r for r in events if rows[0]['t']<=r['eventTime']<=rows[-1]['t']]
selected=[(i,r) for i,r in enumerate(rows) if i%3==0 and (folder/f'frames/frame-{i+1:05d}.png').exists()]
output=folder/'review-frames';output.mkdir(exist_ok=True)
manifest=[]
for n,(i,row) in enumerate(selected):
    image=Image.open(folder/f'frames/frame-{i+1:05d}.png').convert('RGB')
    draw=ImageDraw.Draw(image);draw.rectangle((0,0,960,27),fill=(18,18,22))
    name=f'Combo {row["stage"]+1}' if row['stage']>=0 else 'Charge' if row['charge']>0 else 'Heavy / recovery'
    draw.text((12,7),f'Native Editor preview | {name} | phase {row["phase"]:.2f} | confirmed hits {row["hits"]}'+(' | LOCAL HIT STOP' if row['stop'] else ''),fill='white')
    path=output/f'{n:04d}.png';image.save(path)
    manifest.append(f"file '{path.as_posix()}'")
    dt=selected[n+1][1]['t']-row['t'] if n+1<len(selected) else .12
    manifest.append(f'duration {max(.008,min(dt,.35)):.6f}')
(folder/'review-concat.txt').write_text('\n'.join(manifest),encoding='utf-8')
subprocess.run([imageio_ffmpeg.get_ffmpeg_exe(),'-hide_banner','-loglevel','error','-y','-f','concat','-safe','0','-i',str(folder/'review-concat.txt'),'-vf','fps=30','-c:v','libx264','-crf','20','-pix_fmt','yuv420p','-movflags','+faststart',str(folder/'combat-feel-review.mp4')],check=True)
sheet=Image.new('RGB',(960,600),(20,20,24));d=ImageDraw.Draw(sheet)
for n,event in enumerate(events[:4]):
    nearest=min(selected,key=lambda item:abs(item[1]['t']-event['eventTime']))
    i,row=nearest;im=Image.open(folder/f'frames/frame-{i+1:05d}.png').resize((480,270))
    x=(n%2)*480;y=(n//2)*300;sheet.paste(im,(x,y));d.text((x+8,y+275),f'{event["kind"]} / {event["result"]}',fill='white')
sheet.save(folder/'contact-sheet.jpg')
print(json.dumps({'captured_frames':report['frames'],'video_images':len(selected),'native_checks':len([r for r in report['rows'] if 'passed' in r]),'confirmed_events':events,'video':str(folder/'combat-feel-review.mp4')},indent=2))
