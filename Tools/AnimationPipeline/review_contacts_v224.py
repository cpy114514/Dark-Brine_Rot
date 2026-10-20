import json,sys,subprocess
from pathlib import Path
sys.path.insert(0,str(Path.cwd()/'.codex/encounter-redesign-20261017/python-deps'))
import imageio_ffmpeg
p=Path('.codex/encounter-redesign-20261017')/sys.argv[1]
r=json.loads((p/'result.json').read_text()); count=r['frames']
subprocess.run([imageio_ffmpeg.get_ffmpeg_exe(),'-hide_banner','-loglevel','error','-y','-framerate',str(r['fps']),'-i',str(p/'game-%05d.png'),'-frames:v',str(count),'-c:v','libx264','-threads','2','-preset','fast','-crf','20','-pix_fmt','yuv420p','-movflags','+faststart',str(p/'contact-review.mp4')],check=True)
print(p/'contact-review.mp4')
