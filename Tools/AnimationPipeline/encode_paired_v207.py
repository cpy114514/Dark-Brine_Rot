"""Encode actual native continuous review frames; no performance claims."""
import json,subprocess
from pathlib import Path
import imageio_ffmpeg
from PIL import Image,ImageDraw
root=Path(r'D:/indiegameDev/unity/Dark Brine_Rot/.codex/animation-continuation-20261014/native-paired-v207')
report=json.loads((root/'review.json').read_text(encoding='utf-8'))
views=('game','front','side','three-quarter')
for view in views:
    subprocess.run([imageio_ffmpeg.get_ffmpeg_exe(),'-hide_banner','-loglevel','error','-y','-framerate','30','-i',str(root/(view+'-%04d.png')),'-vf','format=yuv420p','-c:v','libx264','-preset','fast','-crf','20','-movflags','+faststart',str(root/(view+'-review.mp4'))],check=True)
times=(.1,1.184,2.43,4.55,7.03,8.94,12.8,14.572,18.05,20.7)
grid=Image.new('RGB',(4*480,len(times)*295),(25,25,25));draw=ImageDraw.Draw(grid)
for r,t in enumerate(times):
    row=min(report['rows'],key=lambda x:abs(x['time']-t))
    for c,view in enumerate(views):
        path=root/(view+'-'+str(row['frame']).zfill(4)+'.png')
        im=Image.open(path);im.thumbnail((480,270));grid.paste(im,(c*480,r*295+25))
        draw.text((c*480+4,r*295+5),f'{view} {row["time"]:.3f}s',fill='white')
grid.save(root/'contact-sheet.jpg',quality=92)
print(json.dumps({k:report[k] for k in ('sourceFPS','recordedFPS','frames','ExitReason','HeroCounterCount','SharkCounterCount','BoardJumpCount','ContactAttemptGaps','LargestFootHeightError','LargestPlantSlide')},indent=2))

