"""Preserve actual simulation timing; encode native game and multiview evidence."""
import json,subprocess,sys
from pathlib import Path
import imageio_ffmpeg
ROOT=Path(__file__).resolve().parents[2]
folder=ROOT/'.codex'/('encounter-'+sys.argv[1])
tasks=[('game',folder/'preview/frames',folder/'preview/frame-times.json'),('ship-impact',folder/'preview/impact-frames',folder/'preview/impact-times.json')]
for view in ('front','side','three-quarter'):tasks.append((view,folder/'multiview',folder/'multiview/times.json'))
for name,images,timing in tasks:
    if not timing.exists():continue
    times=json.loads(timing.read_text());lines=[]
    for i,t in enumerate(times):
        file=images/(f'{i:04d}.png' if name in ('game','ship-impact') else f'{name}-{i:04d}.png')
        if not file.exists():raise FileNotFoundError(file)
        duration=times[i+1]-t if i+1<len(times) else (times[-1]-times[-2] if len(times)>1 else .033)
        lines.extend(["file '"+file.resolve().as_posix()+"'",f'duration {max(.001,duration):.6f}'])
    if not lines:continue
    lines.append(lines[-2]);listing=folder/(name+'-concat.txt');listing.write_text('\n'.join(lines),encoding='utf-8')
    output=folder/(name+'-review.mp4')
    subprocess.run([imageio_ffmpeg.get_ffmpeg_exe(),'-hide_banner','-loglevel','error','-y','-f','concat','-safe','0','-i',str(listing),'-vf','fps=30,pad=ceil(iw/2)*2:ceil(ih/2)*2,format=yuv420p','-c:v','libx264','-preset','fast','-crf','21','-movflags','+faststart',str(output)],check=True)
    print(str(output))
