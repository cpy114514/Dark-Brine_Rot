"""Native original-skin 60FPS previews. Sources are 67 boundary samples."""
from pathlib import Path
import subprocess,imageio_ffmpeg
root=Path(r'D:/indiegameDev/unity/Dark Brine_Rot/.codex/animation-continuation-20261014/native-framed-parry-v194')
for version in ('v171','v194'):
    for view in ('front','side','three-quarter'):
        name=f'CH1_SahurParry_{version}-{view}'
        subprocess.run([imageio_ffmpeg.get_ffmpeg_exe(),'-hide_banner','-loglevel','error','-y',
            '-framerate','60','-i',str(root/(name+'-%03d.png')),'-frames:v','66',
            '-vf','format=yuv420p','-c:v','libx264','-crf','18','-movflags','+faststart',
            str(root/(name+'-review.mp4'))],check=True)
print('Six 1.1 second, 60FPS native comparison previews encoded.')
