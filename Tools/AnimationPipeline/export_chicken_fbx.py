import sys
from pathlib import Path
import bpy

src, dest = sys.argv[sys.argv.index("--") + 1 :]
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.gltf(filepath=src)
mesh = next(o for o in bpy.data.objects if o.type == "MESH")
bpy.context.view_layer.objects.active = mesh
mesh.select_set(True)
bpy.ops.export_scene.fbx(filepath=dest, use_selection=True, apply_scale_options="FBX_SCALE_ALL", bake_anim=False)
print("BYTES", Path(dest).stat().st_size)
