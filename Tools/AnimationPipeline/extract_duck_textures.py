import bpy
from pathlib import Path

src = r"C:\Users\pinyu\Downloads\duck.fbx"
dest = Path(r"D:\indiegameDev\unity\Dark Brine_Rot\Assets\Game\Prefabs\Enemies\LongLeggedDuck\Textures")
dest.mkdir(parents=True, exist_ok=True)
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=src, use_anim=False)
for image in bpy.data.images:
    if image.size[0] < 8:
        continue
    name = "".join(ch if ch.isalnum() or ch in "._-" else "_" for ch in image.name)
    if not name.lower().endswith(".png"):
        name += ".png"
    path = dest / name
    image.filepath_raw = str(path)
    image.file_format = "PNG"
    image.save()
    print("SAVED", path.name, tuple(image.size), path.stat().st_size)
