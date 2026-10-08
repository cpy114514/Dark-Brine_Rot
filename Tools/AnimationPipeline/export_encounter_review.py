"""Encode existing Unity captures at their measured animation times."""
import argparse,json,subprocess
from pathlib import Path
import imageio_ffmpeg
parser=argparse.ArgumentParser();parser.add_argument('--revision',required=True)
args=parser.parse_args();root=Path(__file__).resolve().parents[2]/'.codex'/('encounter-'+args.revision)
def encode(files,times,destination,width,height):
    lines=[]
    for i,file in enumerate(files):
        duration=max(.025,times[i+1]-times[i] if i+1<len(times) else times[-1]-times[-2])
        lines.extend(["file '"+file.resolve().as_posix()+"'",f'duration {duration:.6f}'])
    lines.append(lines[-2]);listing=destination.with_suffix('.concat.txt');listing.write_text('\n'.join(lines),encoding='utf-8')
    subprocess.run([imageio_ffmpeg.get_ffmpeg_exe(),'-hide_banner','-loglevel','error','-y','-f','concat','-safe','0','-i',str(listing),
        '-vf',f'fps=30,pad={width}:{height}:0:0,format=yuv420p','-c:v','libx264','-preset','fast','-crf','20','-movflags','+faststart',str(destination)],check=True)
    print(destination)
preview=root/'preview';times=json.loads((preview/'frame-times.json').read_text())
encode([preview/'frames'/f'{i:04d}.png' for i in range(len(times))],times,root/'encounter-gameplay.mp4',720,406)
multi=root/'multiview';times=json.loads((multi/'times.json').read_text())
for view in ['front','side','three-quarter']:
    encode([multi/f'{view}-{i:04d}.png' for i in range(len(times))],times,root/f'{view}-review.mp4',960,540)
