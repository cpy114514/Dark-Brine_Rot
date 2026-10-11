"""Encode the native, fixed-camera 60 FPS action samples without retiming."""
from pathlib import Path
import subprocess
import imageio_ffmpeg

root = Path(__file__).resolve().parents[2]
folder = root / '.codex/encounter-v033/native-continuous'
for view in ('front', 'side', 'three-quarter'):
    pattern = folder / f'CH1_SahurSlash_v033-{view}-%02d.png'
    for index in range(85):
        path = folder / f'CH1_SahurSlash_v033-{view}-{index:02d}.png'
        if not path.exists():
            raise FileNotFoundError(path)
    output = folder / f'{view}-60fps-review.mp4'
    subprocess.run([
        imageio_ffmpeg.get_ffmpeg_exe(), '-hide_banner', '-loglevel', 'error',
        '-y', '-framerate', '60', '-i', str(pattern), '-frames:v', '84',
        '-c:v', 'libx264', '-crf', '18', '-pix_fmt', 'yuv420p',
        '-movflags', '+faststart', str(output)
    ], check=True)
    print(output)
