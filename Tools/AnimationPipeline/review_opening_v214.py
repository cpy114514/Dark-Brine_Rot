import sys,json,subprocess
from pathlib import Path
from PIL import Image,ImageDraw
ROOT=Path(__file__).resolve().parents[2]
sys.path.insert(0,str(ROOT/'.codex/encounter-redesign-20261017/python-deps'))
import imageio_ffmpeg
folder=ROOT/'.codex/encounter-redesign-20261017'/sys.argv[1]
result=json.loads((folder/'result.json').read_text(encoding='utf-8')) if (folder/'result.json').exists() else None
if result is None and (folder/'frames.json').exists():
    rows=json.loads((folder/'frames.json').read_text(encoding='utf-8'))
    result={'fps':int(sys.argv[sys.argv.index('--fps')+1]) if '--fps' in sys.argv else 15,'frames':len(rows),'stage':'Partial recording','rows':rows,'RedesignEvents':[]}
fps=result['fps'] if result else 15
if '--sheets-only' not in sys.argv:
    subprocess.run([imageio_ffmpeg.get_ffmpeg_exe(),'-hide_banner','-loglevel','error','-y','-framerate',str(fps),'-i',str(folder/'game-%05d.png'),'-c:v','libx264','-preset','fast','-crf','20','-pix_fmt','yuv420p','-movflags','+faststart',str(folder/'review.mp4')],check=True)
if result:
    groups={}
    for row in result['rows']:groups.setdefault(row['RedesignShot'],[]).append(row)
    for shot,rows in groups.items():
        picks=[rows[round(i*(len(rows)-1)/7)] for i in range(8)]
        sheet=Image.new('RGB',(1280,410),'#222');d=ImageDraw.Draw(sheet)
        for i,row in enumerate(picks):
            x=i%4*320;y=i//4*205;im=Image.open(folder/f"game-{row['frame']:05d}.png");im.thumbnail((320,180));sheet.paste(im,(x,y+25));d.text((x+4,y+4),f"f{row['frame']} clock {row['clock']:.2f} rate {row['rate']:.2f}",fill='white')
        sheet.save(folder/f'shot-{shot}.jpg',quality=94)
    print(json.dumps(dict(frames=result['frames'],stage=result['stage'],events=result.get('RedesignEvents',[])),ensure_ascii=False))
