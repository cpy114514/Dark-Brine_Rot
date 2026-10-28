"""Encode actual sequential native frames, silent, without pose interpolation."""
from pathlib import Path
import sys,json,subprocess,imageio_ffmpeg
root=Path(__file__).resolve().parents[2]
take=sys.argv[1]
p=root/'.codex/jump-meshy-v268'/take
d=json.loads((p/'review.json').read_text())
indices=[i+1 for i,f in enumerate(d['frames']) if f['section'] in ['Stick early chop','CapriTwinBlades early chop']]
sequence=p/'preview-frames.txt'
lines=[]
for i in indices:
 lines.extend(["file '"+(p/'frames'/f'{i:05}.png').as_posix()+"'",'duration 0.033333333'])
sequence.write_text('\n'.join(lines)+'\n')
out=p/'Sahur-AI-JumpChops-v268.mp4'
subprocess.run([imageio_ffmpeg.get_ffmpeg_exe(),'-hide_banner','-loglevel','error','-y','-f','concat','-safe','0','-i',str(sequence),'-c:v','libx264','-crf','20','-pix_fmt','yuv420p','-movflags','+faststart',str(out)],check=True)
print(out)
