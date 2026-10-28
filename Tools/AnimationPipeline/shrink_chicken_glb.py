"""Make a smaller, meter-scale T-pose GLB that a web viewer can load."""
import sys
from pathlib import Path

import bpy
from mathutils import Vector

src, dest, preview = sys.argv[sys.argv.index("--") + 1 :]
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.gltf(filepath=src)
mesh = next(o for o in bpy.data.objects if o.type == "MESH")
bpy.context.view_layer.objects.active = mesh
mesh.select_set(True)
points = [mesh.matrix_world @ Vector(corner) for corner in mesh.bound_box]
height = max(p.z for p in points) - min(p.z for p in points)
# glTF import uses Y-up converted to Blender Z-up, so height is Z.
if height < 1:
    height = max(p.y for p in points) - min(p.y for p in points)
scale = 1.8 / height
mesh.scale = (scale, scale, scale)
bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
print("HEIGHT", round(height, 3), "SCALE", round(scale, 6), "faces", len(mesh.data.polygons))

decimate = mesh.modifiers.new("Decimate", "DECIMATE")
decimate.ratio = 18000 / max(1, len(mesh.data.polygons))
bpy.ops.object.modifier_apply(modifier=decimate.name)
print("FACES", len(mesh.data.polygons), "VERTS", len(mesh.data.vertices))
for image in bpy.data.images:
    if max(image.size[0], image.size[1]) > 1024:
        image.scale(1024, 1024)

bpy.ops.export_scene.gltf(filepath=dest, export_format="GLB", use_selection=True, export_apply=True, export_animations=False, export_skins=False)
print("BYTES", Path(dest).stat().st_size)

scene = bpy.context.scene
scene.render.engine = "BLENDER_WORKBENCH"
scene.render.resolution_x = 640
scene.render.resolution_y = 800
scene.render.image_settings.file_format = "PNG"
scene.display.shading.light = "STUDIO"
scene.display.shading.color_type = "MATERIAL"
cam_data = bpy.data.cameras.new("Front")
cam_data.type = "ORTHO"
cam = bpy.data.objects.new("Front", cam_data)
scene.collection.objects.link(cam)
points = [mesh.matrix_world @ Vector(corner) for corner in mesh.bound_box]
center = sum(points, Vector()) / len(points)
extent = max((point - center).length for point in points)
cam_data.ortho_scale = extent * 2.4
cam.location = center + Vector((0, -1, 0.15)) * (extent * 4 + 1)
cam.rotation_euler = (center - cam.location).to_track_quat("-Z", "Y").to_euler()
scene.camera = cam
scene.render.filepath = preview
bpy.ops.render.render(write_still=True)
print("PREVIEW", preview, "extent", round(extent, 3))
