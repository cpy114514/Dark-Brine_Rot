"""Swing the rebound chicken around world axes and see if the body stays intact."""
import math
import sys
from pathlib import Path

import bpy
from mathutils import Matrix, Vector

src, preview_dir = sys.argv[sys.argv.index("--") + 1 :]
preview_dir = Path(preview_dir)

bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=src)
arm = next(o for o in bpy.data.objects if o.type == "ARMATURE")
mesh = next(o for o in bpy.data.objects if o.type == "MESH")


def update():
    bpy.context.view_layer.update()


def rotate_world(pose_bone, axis, degrees):
    update()
    world = arm.matrix_world @ pose_bone.matrix
    head = world.to_translation()
    rot = Matrix.Rotation(math.radians(degrees), 4, axis)
    pivot = Matrix.Translation(head)
    local = arm.matrix_world.inverted() @ (pivot @ rot @ pivot.inverted() @ world)
    location, rotation, _scale = local.decompose()
    pose_bone.matrix = Matrix.LocRotScale(location, rotation, Vector((1, 1, 1)))
    update()


def render(name):
    update()
    scene = bpy.context.scene
    scene.render.engine = "BLENDER_WORKBENCH"
    scene.render.resolution_x = 720
    scene.render.resolution_y = 900
    scene.render.image_settings.file_format = "PNG"
    scene.display.shading.light = "STUDIO"
    scene.display.shading.color_type = "TEXTURE"
    depsgraph = bpy.context.evaluated_depsgraph_get()
    evaluated = mesh.evaluated_get(depsgraph)
    baked_data = evaluated.to_mesh().copy()
    baked = bpy.data.objects.new(name + "snap", baked_data)
    baked.matrix_world = mesh.matrix_world.copy()
    scene.collection.objects.link(baked)
    mesh.hide_render = True
    points = [baked.matrix_world @ Vector(corner) for corner in baked.bound_box]
    center = sum(points, Vector()) / len(points)
    extent = max((point - center).length for point in points)
    for view_name, view_dir in (("front", Vector((0, -1, 0.08))), ("side", Vector((1, -0.15, 0.08)))):
        cam_data = bpy.data.cameras.new(name + view_name)
        cam_data.type = "ORTHO"
        cam_data.ortho_scale = extent * 2.35
        cam = bpy.data.objects.new(name + view_name, cam_data)
        scene.collection.objects.link(cam)
        cam.location = center + view_dir.normalized() * (extent * 4 + 2)
        cam.rotation_euler = (center - cam.location).to_track_quat("-Z", "Y").to_euler()
        scene.camera = cam
        scene.render.filepath = str(preview_dir / f"{name}-{view_name}.png")
        bpy.ops.render.render(write_still=True)
    mesh.hide_render = False
    bpy.data.objects.remove(baked, do_unlink=True)
    evaluated.to_mesh_clear()


def reset():
    bpy.context.view_layer.objects.active = arm
    bpy.ops.object.mode_set(mode="POSE")
    bpy.ops.pose.select_all(action="SELECT")
    bpy.ops.pose.transforms_clear()
    update()


reset()
for name in ("LeftArm", "RightArm"):
    bone = arm.pose.bones[name]
    bone.rotation_mode = "XYZ"
    bone.rotation_euler.x = math.radians(75)
update()
render("hang4")

reset()
for name in ("LeftLeg", "RightLeg"):
    rotate_world(arm.pose.bones[name], Vector((1, 0, 0)), 40)
render("kick")

reset()
rotate_world(arm.pose.bones["Spine1"], Vector((1, 0, 0)), 28)
render("bow")
print("DONE")
