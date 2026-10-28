"""Encode and sheet the opening polish recordings (native 30 Hz frames)."""
import json, subprocess, sys
from pathlib import Path
import imageio_ffmpeg
from PIL import Image, ImageDraw

ROOT = Path(__file__).resolve().parents[2] / '.codex' / 'opening-polish-v262'


def frames(label):
    folder = ROOT / label / 'review.json-images'
    return sorted(folder.glob('*.jpg'))


def meta(label):
    p = ROOT / label / 'review.json-frames.json'
    return json.loads(p.read_text(encoding='utf-8')) if p.exists() else None


def shots(label):
    prev = None
    for i, r in enumerate(meta(label)):
        k = (r['stage'], r['shot'])
        if k != prev:
            print(i, r['stage'], round(r['t'], 2), r['shot'], r['camera'], r['beat'])
            prev = k


def encode(label, start=0, end=None, name=None):
    files = frames(label)[start:end]
    listing = ROOT / label / 'concat.txt'
    listing.write_text('\n'.join("file '" + f.resolve().as_posix() + "'\nduration 0.033333" for f in files), encoding='utf-8')
    out = ROOT / label / (name or (label + '.mp4'))
    subprocess.run([imageio_ffmpeg.get_ffmpeg_exe(), '-hide_banner', '-loglevel', 'error', '-y', '-f', 'concat', '-safe', '0',
                    '-i', str(listing), '-vf', 'fps=30,format=yuv420p', '-c:v', 'libx264', '-preset', 'fast', '-crf', '21',
                    '-movflags', '+faststart', str(out)], check=True)
    print(out)


def sheet(label, start, end, step, name, cols=6, width=320):
    files = frames(label)
    m = meta(label)
    picks = list(range(start, min(end, len(files)), step))
    h = width * 9 // 16
    rows = (len(picks) + cols - 1) // cols
    img = Image.new('RGB', (cols * width, rows * (h + 16)), 'black')
    d = ImageDraw.Draw(img)
    for n, i in enumerate(picks):
        x, y = (n % cols) * width, (n // cols) * (h + 16)
        img.paste(Image.open(files[i]).resize((width, h)), (x, y + 16))
        tag = str(i)
        if m and i < len(m):
            r = m[i]
            tag += f" {r['stage'][:5]} t{r['t']:.2f} s{r['shot']}"
        d.text((x + 3, y + 2), tag, fill='yellow')
    out = ROOT / label / 'sheets' / (name + '.jpg')
    out.parent.mkdir(exist_ok=True)
    img.save(out, quality=85)
    print(out)


if __name__ == '__main__':
    cmd = sys.argv[1]
    if cmd == 'shots':
        shots(sys.argv[2])
    elif cmd == 'encode':
        encode(sys.argv[2], *(int(a) for a in sys.argv[3:5]), *(sys.argv[5:6]))
    else:
        sheet(sys.argv[2], int(sys.argv[3]), int(sys.argv[4]), int(sys.argv[5]), sys.argv[6], *(int(a) for a in sys.argv[7:9]))
