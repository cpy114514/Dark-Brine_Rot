"""Rebind Chicken Bro so the face, hair, arms, pants and shoes follow the right bones.

The T-pose skeleton stays. Vertex groups are rebuilt from where each vertex sits
relative to that skeleton, then the elbows get a small forward bend so a humanoid
avatar can tell which way the arm folds.
"""
import math
import sys
from collections import defaultdict
from pathlib import Path

import bpy
from mathutils import Matrix, Vector

src, dest, preview_dir = sys.argv[sys.argv.index("--") + 1 :]
preview_dir = Path(preview_dir)
preview_dir.mkdir(parents=True, exist_ok=True)

bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=src)
arm = next(o for o in bpy.data.objects if o.type == "ARMATURE")
mesh = next(o for o in bpy.data.objects if o.type == "MESH")


def smoothstep(edge0, edge1, value):
    if edge0 == edge1:
        return 1.0 if value >= edge1 else 0.0
    t = min(1.0, max(0.0, (value - edge0) / (edge1 - edge0)))
    return t * t * (3.0 - 2.0 * t)


def bone_segment(name):
    bone = arm.data.bones[name]
    head = arm.matrix_world @ bone.head_local
    tail = arm.matrix_world @ bone.tail_local
    return head, tail


def segment_distance(point, head, tail):
    line = tail - head
    length2 = line.length_squared
    if length2 < 1e-10:
        return (point - head).length
    t = max(0.0, min(1.0, (point - head).dot(line) / length2))
    return (point - (head + line * t)).length


def nearest_chain(point, names):
    ranked = sorted((segment_distance(point, *bone_segment(name)), name) for name in names)
    first, second = ranked[0], ranked[1]
    if second[0] > first[0] + 0.045:
        return [(first[1], 1.0)]
    w1 = 1.0 / (first[0] + 0.008) ** 2
    w2 = 1.0 / (second[0] + 0.008) ** 2
    total = w1 + w2
    return [(first[1], w1 / total), (second[1], w2 / total)]


def add_weight(bucket, name, amount):
    if amount > 0.001:
        bucket[name] = bucket.get(name, 0.0) + amount


def weights_for(point):
    side = "Left" if point.x >= 0 else "Right"
    other = "Right" if side == "Left" else "Left"
    result = {}
    ax = abs(point.x)

    # Hair, face and beak stay on the skull. The arms pass below this band.
    if point.z > 0.97 and ax < 0.55:
        return {"Head": 1.0}

    # The sleeves hang below the shoulder line. Anything outside the ribcage
    # and above the shorts belongs to the arm, not the spine.
    arm_zone = ax > 0.34 and 0.40 < point.z < 1.05
    leg_zone = point.z < 0.40 and ax > 0.035 and abs(point.y) < 0.4

    if arm_zone:
        chain = nearest_chain(point, [f"{side}Shoulder", f"{side}Arm", f"{side}ForeArm", f"{side}Hand"])
        # Keep the opposite shoulder from grabbing the chest.
        for name, weight in chain:
            add_weight(result, name, weight)
        return result

    if leg_zone:
        shoe = 1.0 - smoothstep(0.055, 0.13, point.z)
        shin = smoothstep(0.055, 0.13, point.z) * (1.0 - smoothstep(0.15, 0.24, point.z))
        thigh = smoothstep(0.16, 0.25, point.z)
        outer = smoothstep(0.045, 0.14, ax)
        add_weight(result, f"{side}Foot", shoe)
        add_weight(result, f"{side}Leg", shin)
        add_weight(result, f"{side}UpLeg", thigh * outer)
        add_weight(result, "Hips", thigh * (1.0 - outer))
        # A little of the other side would cross the crotch; keep it on the hips.
        if ax < 0.03:
            result = {"Hips": 1.0}
        return result

    torso = nearest_chain(point, ["Hips", "Spine", "Spine1", "Spine2", "Neck", "Head"])
    for name, weight in torso:
        add_weight(result, name, weight)
    # Shoulders may be the nearest bone beside the collar. Leave them only if
    # the vertex is actually out at the sleeve.
    if ax < 0.16:
        result = {name: weight for name, weight in result.items() if other not in name}
    return result or {"Hips": 1.0}


def normalize(bucket):
    total = sum(bucket.values())
    if total <= 0:
        return {"Hips": 1.0}
    ordered = sorted(bucket.items(), key=lambda item: -item[1])[:3]
    total = sum(weight for _, weight in ordered)
    return {name: weight / total for name, weight in ordered}


print("BINDING")
batches = defaultdict(lambda: defaultdict(list))
region = defaultdict(lambda: defaultdict(int))
for vertex in mesh.data.vertices:
    point = mesh.matrix_world @ vertex.co
    assigned = normalize(weights_for(point))
    dominant = max(assigned, key=assigned.get)
    if point.z > 0.97 and abs(point.x) < 0.4:
        region["face"][dominant] += 1
    elif abs(point.x) > 0.35 and 0.6 < point.z < 0.95:
        region["arm"][dominant] += 1
    elif 0.18 < point.z < 0.4 and abs(point.x) > 0.08:
        region["pants"][dominant] += 1
    elif point.z < 0.1 and abs(point.x) > 0.05:
        region["shoe"][dominant] += 1
    for name, weight in assigned.items():
        batches[name][round(weight * 16) / 16].append(vertex.index)

for group in list(mesh.vertex_groups):
    mesh.vertex_groups.remove(group)
for name, levels in batches.items():
    group = mesh.vertex_groups.new(name=name)
    for weight, indices in levels.items():
        if indices and weight > 0:
            group.add(indices, weight, "REPLACE")

for name, bones in region.items():
    print(name, sorted(bones.items(), key=lambda item: -item[1])[:5])


def update():
    bpy.context.view_layer.update()


def aim(pose_bone, direction):
    update()
    head = arm.matrix_world @ pose_bone.head
    current = (arm.matrix_world @ pose_bone.tail) - head
    if current.length < 1e-8:
        return
    quat = current.normalized().rotation_difference(direction.normalized())
    pivot = Matrix.Translation(head)
    world = pivot @ quat.to_matrix().to_4x4() @ pivot.inverted() @ (arm.matrix_world @ pose_bone.matrix)
    pose_bone.matrix = arm.matrix_world.inverted() @ world
    update()


def render(name):
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


def pose_reset():
    bpy.context.view_layer.objects.active = arm
    bpy.ops.object.mode_set(mode="POSE")
    bpy.ops.pose.select_all(action="SELECT")
    bpy.ops.pose.transforms_clear()
    update()


pose_reset()
render("rest")

for bone_name, direction in (
    ("LeftArm", Vector((0.35, -0.05, -1))),
    ("LeftForeArm", Vector((0.22, -0.05, -1))),
    ("LeftHand", Vector((0.12, -0.02, -1))),
    ("RightArm", Vector((-0.35, -0.05, -1))),
    ("RightForeArm", Vector((-0.22, -0.05, -1))),
    ("RightHand", Vector((-0.12, -0.02, -1))),
):
    aim(arm.pose.bones[bone_name], direction)
aim(arm.pose.bones["LeftLeg"], Vector((0.15, 0.35, -1)))
render("arms-down")

pose_reset()
aim(arm.pose.bones["LeftForeArm"], Vector((0.2, -1, 0.05)))
aim(arm.pose.bones["LeftHand"], Vector((0.15, -1, 0)))
aim(arm.pose.bones["RightForeArm"], Vector((-0.2, -1, 0.05)))
aim(arm.pose.bones["RightHand"], Vector((-0.15, -1, 0)))
render("elbows")

pose_reset()
# A few degrees forward at the elbow becomes the new bind pose.
for bone_name, degrees in (("LeftForeArm", -12.0), ("RightForeArm", 12.0)):
    bone = arm.pose.bones[bone_name]
    bone.rotation_mode = "XYZ"
    bone.rotation_euler.x = math.radians(degrees)
update()
bpy.ops.object.mode_set(mode="POSE")
bpy.ops.pose.select_all(action="SELECT")
bpy.ops.pose.armature_apply()
bpy.ops.object.mode_set(mode="OBJECT")
render("bind")

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
print("EXPORT", Path(dest).stat().st_size)
