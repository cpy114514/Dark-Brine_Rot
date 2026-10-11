import json,subprocess,sys
from pathlib import Path
import imageio_ffmpeg
folder=Path(__file__).resolve().parents[2]/'.codex/encounter-v011-controls'/sys.argv[1]
times=json.loads((folder/'times.json').read_text());lines=[]
for i,t in enumerate(times):
    duration=times[i+1]-t if i+1<len(times) else .1
    lines.extend(["file '"+(folder/f'{i:04}.png').resolve().as_posix()+"'",f'duration {max(.001,duration):.6f}'])
lines.append(lines[-2]);listing=folder/'native-concat.txt';listing.write_text('\n'.join(lines))
output=folder/'native-controls-review.mp4'
subprocess.run([imageio_ffmpeg.get_ffmpeg_exe(),'-hide_banner','-loglevel','error','-y','-f','concat','-safe','0','-i',str(listing),'-vf','fps=30,pad=ceil(iw/2)*2:ceil(ih/2)*2,format=yuv420p','-c:v','libx264','-crf','21','-movflags','+faststart',str(output)],check=True)
print(output)
