"""Package untouched native Unity frames; captions identify the recorded attacks."""
import argparse
import json
import subprocess
from pathlib import Path
import imageio_ffmpeg
from PIL import Image, ImageDraw, ImageFont

parser = argparse.ArgumentParser()
parser.add_argument("take", type=Path)
args = parser.parse_args()
root = args.take.resolve()
rows = json.loads((root / "frames.json").read_text(encoding="utf-8"))
labels = {
    "01-club-light": "CLUB / LIGHT",
    "02-club-charge": "CLUB / CHARGE",
    "03-club-heavy": "CLUB / HEAVY RELEASE",
    "04-twin-blade-light": "TWIN BLADES / LIGHT",
    "05-twin-blade-charge": "TWIN BLADES / CHARGE",
    "06-twin-blade-heavy": "TWIN BLADES / HEAVY RELEASE",
    "07-twin-blade-spin": "TWIN BLADES / SPIN",
    "08-boss-explosion": "BOSS / THERMAL BLAST",
}
filters = []
for phase, label in labels.items():
    selected = [r["image"] for r in rows if r["phase"] == phase]
    if selected:
        filters.append(
            "drawtext=fontfile='C\\:/Windows/Fonts/segoeui.ttf':"
            f"text='{label}':fontsize=24:fontcolor=white:box=1:boxcolor=black@0.5:boxborderw=12:x=28:y=28:"
            f"enable='between(n,{min(selected)},{max(selected)})'"
        )
output = root / "Brine-Combat-Native.mp4"
subprocess.run([
    imageio_ffmpeg.get_ffmpeg_exe(), "-hide_banner", "-loglevel", "error", "-y",
    "-framerate", "30", "-i", str(root / "frames/%04d.jpg"), "-vf", ",".join(filters),
    "-c:v", "libx264", "-crf", "18", "-preset", "medium", "-pix_fmt", "yuv420p",
    "-movflags", "+faststart", str(output),
], check=True)
panels = [(phase, root / f"{phase}.png") for phase in labels if (root / f"{phase}.png").exists()]
grid = Image.new("RGB", (1280, ((len(panels)+1)//2)*390), (20, 26, 30))
draw = ImageDraw.Draw(grid)
font = ImageFont.truetype("C:/Windows/Fonts/segoeui.ttf", 17)
for i, (phase, source) in enumerate(panels):
    x, y = i % 2 * 640, i // 2 * 390
    grid.paste(Image.open(source).resize((640, 360)), (x, y))
    draw.text((x+16, y+365), labels[phase], font=font, fill=(225, 231, 230))
grid.save(root / "Native-Review.jpg", quality=94)
print(json.dumps({"video":str(output),"seconds":len(rows)/30,"frames":len(rows)}))
