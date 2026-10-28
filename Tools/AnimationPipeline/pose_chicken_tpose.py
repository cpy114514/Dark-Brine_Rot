"""Pose Chicken Bro's arms into a T-pose, leave the standing legs, and render both."""
import sys
from pathlib import Path

import bpy
from mathutils import Matrix, Vector

argv = sys.argv[sys.argv.index("--") + 1 :]
src, out_dir = argv[0], Path(argv[1])
out_dir.mkdir(parents=True, exist_ok=True)

bpy.ops.wm.read_factory_settings(use_empty=True)
try:
    bpy.ops.wm.fbx_import(filepath=src)
except Exception:
    bpy.ops.import_scene.fbx(filepath=src)

arm = next(o for o in bpy.data.objects if o.type == "ARMATURE")
meshes = [o for o in bpy.data.objects if o.type == "MESH"]
print("ARM", arm.name, "scale", tuple(arm.scale), "meshes", [(m.name, len(m.data.vertices), tuple(m.scale)) for m in meshes])


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
    rotation = quat.to_matrix().to_4x4()
    pivot = Matrix.Translation(head)
    world = pivot @ rotation @ pivot.inverted() @ (arm.matrix_world @ pose_bone.matrix)
    pose_bone.matrix = arm.matrix_world.inverted() @ world
    update()


def frame_camera(name, location, forward):
    cam_data = bpy.data.cameras.new(name)
    cam_data.type = "ORTHO"
    cam = bpy.data.objects.new(name, cam_data)
    bpy.context.scene.collection.objects.link(cam)
    points = []
    for mesh in meshes:
        for corner in mesh.bound_box:
            points.append(mesh.matrix_world @ Vector(corner))
    center = sum(points, Vector()) / len(points)
    extent = max((point - center).length for point in points)
    cam_data.ortho_scale = extent * 2.3
    cam.location = center + forward.normalized() * (extent * 4 + 2)
    cam.rotation_euler = (center - cam.location).to_track_quat("-Z", "Y").to_euler()
    return cam


def render(path, camera):
    scene = bpy.context.scene
    scene.camera = camera
    scene.render.engine = "BLENDER_WORKBENCH"
    scene.render.resolution_x = 720
    scene.render.resolution_y = 960
    scene.render.filepath = str(path)
    scene.render.image_settings.file_format = "PNG"
    scene.display.shading.light = "STUDIO"
    scene.display.shading.color_type = "TEXTURE"
    bpy.ops.render.render(write_still=True)
    print("RENDER", path)


bpy.context.view_layer.objects.active = arm
bpy.ops.object.mode_set(mode="POSE")
front = frame_camera("Front", None, Vector((0, -1, 0)))
side = frame_camera("Side", None, Vector((1, 0, 0)))
render(out_dir / "chicken-rest-front.png", front)
render(out_dir / "chicken-rest-side.png", side)

# Character faces -Y. A real T-pose puts both arms straight out.
for bone_name, direction in (
    ("LeftArm", Vector((1, 0, 0))),
    ("LeftForeArm", Vector((1, 0, 0))),
    ("LeftHand", Vector((1, 0, 0))),
    ("RightArm", Vector((-1, 0, 0))),
    ("RightForeArm", Vector((-1, 0, 0))),
    ("RightHand", Vector((-1, 0, 0))),
):
    aim(arm.pose.bones[bone_name], direction)

render(out_dir / "chicken-tpose-front.png", front)
render(out_dir / "chicken-tpose-side.png", side)
print("DONE")
