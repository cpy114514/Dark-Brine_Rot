"""Put Chicken Bro's limbs on a humanoid T-pose and consistent bone axes.

The rest-pose mesh stays put: skinning is identity at rest, so joint edits do
not move vertices until a bone actually rotates.
"""
import math
import sys
from pathlib import Path

import bpy
from mathutils import Matrix, Vector

argv = sys.argv[sys.argv.index("--") + 1 :]
src, dest, preview_dir = argv
preview_dir = Path(preview_dir)
preview_dir.mkdir(parents=True, exist_ok=True)

bpy.ops.wm.read_factory_settings(use_empty=True)
try:
    bpy.ops.wm.fbx_import(filepath=src)
except Exception:
    bpy.ops.import_scene.fbx(filepath=src)

arm = next(o for o in bpy.data.objects if o.type == "ARMATURE")
mesh = next(o for o in bpy.data.objects if o.type == "MESH")
print("PARENT", mesh.parent.name if mesh.parent else None, "MODS", [(m.name, m.type) for m in mesh.modifiers])


def update():
    bpy.context.view_layer.update()


def transfer_weights(source_name, dest_name):
    source = mesh.vertex_groups.get(source_name)
    dest = mesh.vertex_groups.get(dest_name)
    if source is None or dest is None:
        print("SKIP WEIGHT", source_name, dest_name)
        return 0
    source_index, dest_index = source.index, dest.index
    moved = 0
    for vertex in mesh.data.vertices:
        weight = 0.0
        for element in vertex.groups:
            if element.group == source_index and element.weight > 0:
                weight = element.weight
                break
        if weight <= 0:
            continue
        dest.add([vertex.index], weight, "ADD")
        source.remove([vertex.index])
        moved += 1
    print("MOVED", source_name, "->", dest_name, moved)
    return moved


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


def world_axes(bone):
    matrix = arm.matrix_world.to_3x3() @ bone.matrix.to_3x3()
    return matrix.col[0].normalized(), matrix.col[1].normalized(), matrix.col[2].normalized()


def align_z(edit_bone, world_forward):
    target = (arm.matrix_world.inverted().to_3x3() @ world_forward).normalized()
    best_roll, best_dot = edit_bone.roll, -2.0
    for step in range(72):
        edit_bone.roll = step * (math.tau / 72)
        z_axis = (edit_bone.matrix.to_3x3() @ Vector((0, 0, 1))).normalized()
        dot = z_axis.dot(target)
        if dot > best_dot:
            best_dot = dot
            best_roll = edit_bone.roll
    edit_bone.roll = best_roll
    return best_dot


# The long cannon bone is currently the "foot". Give that skin to the shin,
# and give the toe skin to the foot, before the joints move.
for side in ("Left", "Right"):
    transfer_weights(f"{side}Foot", f"{side}Leg")
    transfer_weights(f"{side}ToeBase", f"{side}Foot")

bpy.context.view_layer.objects.active = arm
bpy.ops.object.mode_set(mode="EDIT")
forward = Vector((0, -1, 0))
for side in ("Left", "Right"):
    shin = arm.data.edit_bones[f"{side}Leg"]
    foot = arm.data.edit_bones[f"{side}Foot"]
    toe = arm.data.edit_bones[f"{side}ToeBase"]
    ankle = foot.tail.copy()
    toe_end = toe.tail.copy()
    shin.tail = ankle
    foot.head = ankle
    toe_dir = toe_end - ankle
    if toe_dir.length < 1e-6:
        toe_dir = Vector((0, -1, 0))
    foot.tail = ankle + toe_dir.normalized() * toe_dir.length * 0.45
    toe.head = foot.tail
    toe.tail = toe_end
    print(side, "shin", round(shin.length, 4), "foot", round(foot.length, 4))

for name in (
    "Hips", "Spine", "Spine1", "Spine2", "Neck", "Head",
    "LeftShoulder", "LeftArm", "LeftForeArm", "LeftHand",
    "RightShoulder", "RightArm", "RightForeArm", "RightHand",
    "LeftUpLeg", "LeftLeg", "LeftFoot", "LeftToeBase",
    "RightUpLeg", "RightLeg", "RightFoot", "RightToeBase",
):
    dot = align_z(arm.data.edit_bones[name], forward)
    print("ROLL", name, round(dot, 3))

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

bpy.ops.pose.armature_apply()
bpy.ops.object.mode_set(mode="OBJECT")


def render(path, pose_action=None):
    if pose_action:
        bpy.context.view_layer.objects.active = arm
        bpy.ops.object.mode_set(mode="POSE")
        bpy.ops.pose.select_all(action="SELECT")
        bpy.ops.pose.transforms_clear()
        pose_action()
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
        cam_data = bpy.data.cameras.new(view_name + path.stem)
        cam_data.type = "ORTHO"
        cam_data.ortho_scale = extent * 2.35
        cam = bpy.data.objects.new(view_name + path.stem, cam_data)
        scene.collection.objects.link(cam)
        cam.location = center + view_dir.normalized() * (extent * 4 + 2)
        cam.rotation_euler = (center - cam.location).to_track_quat("-Z", "Y").to_euler()
        scene.camera = cam
        scene.render.filepath = str(preview_dir / f"{path.stem}-{view_name}.png")
        bpy.ops.render.render(write_still=True)
        print("RENDER", scene.render.filepath)


def bend(bone_name, axis, degrees):
    bone = arm.pose.bones[bone_name]
    bone.rotation_mode = "XYZ"
    angles = [0, 0, 0]
    angles["XYZ".index(axis)] = math.radians(degrees)
    bone.rotation_euler = angles
    update()


render(preview_dir / "rest")
render(preview_dir / "elbow", lambda: bend("LeftForeArm", "X", -55))
render(preview_dir / "knee", lambda: bend("LeftLeg", "X", 40))

bpy.ops.object.select_all(action="DESELECT")
arm.select_set(True)
mesh.select_set(True)
bpy.context.view_layer.objects.active = arm
Path(dest).parent.mkdir(parents=True, exist_ok=True)
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
print("EXPORT", dest, Path(dest).stat().st_size)
