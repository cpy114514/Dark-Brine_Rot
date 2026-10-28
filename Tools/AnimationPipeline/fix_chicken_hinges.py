"""Point the knee hinge backward and the ankle hinge upward, then re-render."""
import math
import sys
from pathlib import Path

import bpy
from mathutils import Vector

src, dest, preview_dir = sys.argv[sys.argv.index("--") + 1 :]
preview_dir = Path(preview_dir)

bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=src)
arm = next(o for o in bpy.data.objects if o.type == "ARMATURE")
mesh = next(o for o in bpy.data.objects if o.type == "MESH")


def align_axis(edit_bone, local_axis, world_direction):
    target = (arm.matrix_world.inverted().to_3x3() @ world_direction).normalized()
    best_roll, best_dot = edit_bone.roll, -2.0
    for step in range(72):
        edit_bone.roll = step * (math.tau / 72)
        axis = (edit_bone.matrix.to_3x3() @ local_axis).normalized()
        dot = axis.dot(target)
        if dot > best_dot:
            best_dot, best_roll = dot, edit_bone.roll
    edit_bone.roll = best_roll
    return best_dot


bpy.context.view_layer.objects.active = arm
bpy.ops.object.mode_set(mode="EDIT")
backward = Vector((0, 1, 0))
up = Vector((0, 0, 1))
for side in ("Left", "Right"):
    shin = align_axis(arm.data.edit_bones[f"{side}Leg"], Vector((0, 0, 1)), backward)
    thigh = align_axis(arm.data.edit_bones[f"{side}UpLeg"], Vector((0, 0, 1)), backward)
    foot = align_axis(arm.data.edit_bones[f"{side}Foot"], Vector((0, 0, 1)), up)
    toe = align_axis(arm.data.edit_bones[f"{side}ToeBase"], Vector((0, 0, 1)), up)
    print(side, "thigh", round(thigh, 3), "shin", round(shin, 3), "foot", round(foot, 3), "toe", round(toe, 3))

bpy.ops.object.mode_set(mode="OBJECT")


def render(name, bends):
    bpy.context.view_layer.objects.active = arm
    bpy.ops.object.mode_set(mode="POSE")
    bpy.ops.pose.select_all(action="SELECT")
    bpy.ops.pose.transforms_clear()
    for bone_name, axis, degrees in bends:
        bone = arm.pose.bones[bone_name]
        bone.rotation_mode = "XYZ"
        angles = [0.0, 0.0, 0.0]
        angles["XYZ".index(axis)] = math.radians(degrees)
        bone.rotation_euler = angles
    bpy.context.view_layer.update()
    bpy.ops.object.mode_set(mode="OBJECT")
    scene = bpy.context.scene
    scene.render.engine = "BLENDER_WORKBENCH"
    scene.render.resolution_x = 720
    scene.render.resolution_y = 900
    scene.render.image_settings.file_format = "PNG"
    scene.display.shading.light = "STUDIO"
    scene.display.shading.color_type = "TEXTURE"
    points = [mesh.matrix_world @ Vector(corner) for corner in mesh.bound_box]
    center = sum(points, Vector()) / len(points)
    extent = max((point - center).length for point in points)
    for view_name, view_dir in (("front", Vector((0, -1, 0.05))), ("side", Vector((1, -0.15, 0.05)))):
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


render("knee2", [("LeftLeg", "X", 45)])
render("elbow2", [("LeftForeArm", "X", -55), ("RightForeArm", "X", 55)])
bpy.context.view_layer.objects.active = arm
bpy.ops.object.mode_set(mode="POSE")
bpy.ops.pose.select_all(action="SELECT")
bpy.ops.pose.transforms_clear()
bpy.ops.object.mode_set(mode="OBJECT")

bpy.ops.object.select_all(action="DESELECT")
arm.select_set(True)
mesh.select_set(True)
bpy.context.view_layer.objects.active = arm
bpy.ops.export_scene.fbx(
    filepath=dest,
    use_selection=True,
    object_types={"ARMATURE", "MESH"},
    apply_scale_options="FBX_SCALE_NONE",
    apply_unit_scale=True,
    bake_anim=False,
    add_leaf_bones=False,
    primary_bone_axis="Y",
    secondary_bone_axis="X",
    mesh_smooth_type="FACE",
    use_mesh_modifiers=False,
    path_mode="COPY",
    embed_textures=True,
)
points = [mesh.matrix_world @ Vector(corner) for corner in mesh.bound_box]
low = Vector((min(p.x for p in points), min(p.y for p in points), min(p.z for p in points)))
high = Vector((max(p.x for p in points), max(p.y for p in points), max(p.z for p in points)))
print("BOUNDS", tuple(round(v, 3) for v in low), tuple(round(v, 3) for v in high))
print("EXPORT", Path(dest).stat().st_size)
