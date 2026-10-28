"""Encode the native, normal-speed gameplay view with full weapon framing."""
from pathlib import Path
import json,subprocess,imageio_ffmpeg
p=Path('.codex/jump-meshy-v270/presentation-30').resolve();d=json.loads((p/'review.json').read_text());assert d['passed']
indices=[i+1 for i,f in enumerate(d['frames']) if f['section'].endswith('early chop')]
sequence=p/'preview-frames.txt';lines=[]
for i in indices:lines.extend(["file '"+(p/'frames'/f'{i:05}.png').as_posix()+"'",f"duration {1/d['simulationHz']:.9f}"])
sequence.write_text('\n'.join(lines)+'\n');out=p/'Sahur-AI-JumpChops-v270.mp4'
subprocess.run([imageio_ffmpeg.get_ffmpeg_exe(),'-hide_banner','-loglevel','error','-y','-f','concat','-safe','0','-i',str(sequence),'-c:v','libx264','-crf','20','-pix_fmt','yuv420p','-movflags','+faststart',str(out)],check=True)
print(out)
