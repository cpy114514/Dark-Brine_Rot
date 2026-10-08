"""Encode native Unity pose review frames using their actual simulation times."""
import json
import subprocess
from pathlib import Path
import imageio_ffmpeg

root=Path(__file__).resolve().parents[2]/'.codex/encounter-v001/multiview'
times=json.loads((root/'times.json').read_text())
for view in ('front','side','three-quarter'):
    lines=[]
    for i,t in enumerate(times):
        duration=times[i+1]-t if i+1<len(times) else times[-1]-times[-2]
        lines.extend([f"file '{(root/f'{view}-{i:04}.png').resolve().as_posix()}'",f'duration {duration:.6f}'])
    lines.append(lines[-2]);listing=root/f'{view}-concat.txt';listing.write_text('\n'.join(lines),encoding='utf-8')
    output=root/f'{view}-review.mp4'
    subprocess.run([imageio_ffmpeg.get_ffmpeg_exe(),'-hide_banner','-loglevel','error','-y','-f','concat','-safe','0','-i',str(listing),'-vf','fps=30,format=yuv420p','-c:v','libx264','-preset','fast','-crf','21','-movflags','+faststart',str(output)],check=True)
    print(output)
