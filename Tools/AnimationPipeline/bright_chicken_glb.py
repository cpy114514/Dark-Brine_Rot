"""Bright, large T-pose chicken so Tripo's viewer cannot miss it."""
import sys
from pathlib import Path

import bpy
from mathutils import Vector

src, dest = sys.argv[sys.argv.index("--") + 1 :]
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.gltf(filepath=src)
mesh = next(o for o in bpy.data.objects if o.type == "MESH")
bpy.context.view_layer.objects.active = mesh
mesh.select_set(True)
points = [mesh.matrix_world @ Vector(corner) for corner in mesh.bound_box]
height = max(p.z for p in points) - min(p.z for p in points)
mesh.scale = (40 / height, 40 / height, 40 / height)
bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
mat = bpy.data.materials.new("Bright")
mat.use_nodes = True
bsdf = mat.node_tree.nodes.get("Principled BSDF")
bsdf.inputs["Base Color"].default_value = (0.1, 0.85, 0.2, 1)
bsdf.inputs["Roughness"].default_value = 0.6
mesh.data.materials.clear()
mesh.data.materials.append(mat)
bpy.ops.export_scene.gltf(filepath=dest, export_format="GLB", use_selection=True, export_apply=True, export_animations=False, export_skins=False)
print("BYTES", Path(dest).stat().st_size, "height", 40)
