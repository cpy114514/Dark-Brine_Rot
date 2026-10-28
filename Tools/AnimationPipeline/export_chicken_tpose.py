"""Bake a T-pose mesh without the old skeleton and export a lighter GLB for Tripo."""
import sys
from pathlib import Path

import bpy
from mathutils import Matrix, Vector

argv = sys.argv[sys.argv.index("--") + 1 :]
src, dest, preview = argv[0], argv[1], argv[2]

bpy.ops.wm.read_factory_settings(use_empty=True)
try:
    bpy.ops.wm.fbx_import(filepath=src)
except Exception:
    bpy.ops.import_scene.fbx(filepath=src)

arm = next(o for o in bpy.data.objects if o.type == "ARMATURE")
mesh = next(o for o in bpy.data.objects if o.type == "MESH")


def update():
    bpy.context.view_layer.update()


def aim(pose_bone, direction):
    update()
    head = arm.matrix_world @ pose_bone.head
    tail = arm.matrix_world @ pose_bone.tail
    current = tail - head
    if current.length < 1e-8:
        return
    quat = current.normalized().rotation_difference(direction.normalized())
    pivot = Matrix.Translation(head)
    world = pivot @ quat.to_matrix().to_4x4() @ pivot.inverted() @ (arm.matrix_world @ pose_bone.matrix)
    pose_bone.matrix = arm.matrix_world.inverted() @ world
    update()


bpy.context.view_layer.objects.active = arm
bpy.ops.object.mode_set(mode="POSE")
for bone_name, direction in (
    ("LeftArm", Vector((1, 0, 0))),
    ("LeftForeArm", Vector((1, 0, 0))),
    ("LeftHand", Vector((1, 0, 0))),
    ("RightArm", Vector((-1, 0, 0))),
    ("RightForeArm", Vector((-1, 0, 0))),
    ("RightHand", Vector((-1, 0, 0))),
):
    aim(arm.pose.bones[bone_name], direction)

bpy.ops.object.mode_set(mode="OBJECT")
bpy.context.view_layer.objects.active = mesh
mesh.select_set(True)
armature_mods = [m.name for m in mesh.modifiers if m.type == "ARMATURE"]
for name in armature_mods:
    bpy.ops.object.modifier_apply(modifier=name)

before = len(mesh.data.polygons)
decimate = mesh.modifiers.new("Decimate", "DECIMATE")
decimate.decimate_type = "COLLAPSE"
decimate.ratio = 0.28
bpy.ops.object.modifier_apply(modifier=decimate.name)
after = len(mesh.data.polygons)
print("FACES", before, "->", after, "verts", len(mesh.data.vertices))

for image in bpy.data.images:
    if image.size[0] > 2048 or image.size[1] > 2048:
        image.scale(2048, 2048)
        print("SCALED", image.name, image.size[0])

bpy.data.objects.remove(arm, do_unlink=True)
for obj in list(bpy.data.objects):
    if obj.type != "MESH":
        bpy.data.objects.remove(obj, do_unlink=True)

mesh.select_set(True)
bpy.context.view_layer.objects.active = mesh
Path(dest).parent.mkdir(parents=True, exist_ok=True)
bpy.ops.export_scene.gltf(
    filepath=dest,
    export_format="GLB",
    use_selection=True,
    export_apply=True,
    export_animations=False,
    export_skins=False,
    export_morph=False,
)
print("EXPORT", dest, Path(dest).stat().st_size)

scene = bpy.context.scene
scene.render.engine = "BLENDER_WORKBENCH"
scene.render.resolution_x = 720
scene.render.resolution_y = 960
scene.render.image_settings.file_format = "PNG"
scene.display.shading.light = "STUDIO"
scene.display.shading.color_type = "TEXTURE"
cam_data = bpy.data.cameras.new("Front")
cam_data.type = "ORTHO"
cam = bpy.data.objects.new("Front", cam_data)
scene.collection.objects.link(cam)
points = [mesh.matrix_world @ Vector(corner) for corner in mesh.bound_box]
center = sum(points, Vector()) / len(points)
extent = max((point - center).length for point in points)
cam_data.ortho_scale = extent * 2.3
cam.location = center + Vector((0, -1, 0)) * (extent * 4 + 2)
cam.rotation_euler = (center - cam.location).to_track_quat("-Z", "Y").to_euler()
scene.camera = cam
scene.render.filepath = preview
bpy.ops.render.render(write_still=True)
print("PREVIEW", preview)
