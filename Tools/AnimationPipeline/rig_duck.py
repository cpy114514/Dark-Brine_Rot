"""Rig the static long-legged duck and author in-place clips for Unity.

The mesh is one rubber toy: round body, beak toward -Y, webbed feet on Z=0,
and two long legs. Bones follow that shape. Clips are original.
"""
import math
import sys
from collections import defaultdict
from pathlib import Path

import bpy
from mathutils import Vector

SRC = r"C:\Users\pinyu\Downloads\duck.fbx"
DEST = Path(r"D:\indiegameDev\unity\Dark Brine_Rot\Temp\duck\LongLeggedDuck.fbx")
PREVIEW = DEST.parent
PREVIEW.mkdir(parents=True, exist_ok=True)

bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=SRC, use_anim=False)
mesh = next(obj for obj in bpy.data.objects if obj.type == "MESH")
mesh.name = "Mesh_0"
print("IMPORT_XFORM", [round(v, 3) for v in mesh.rotation_euler], [round(v, 3) for v in mesh.scale])
bpy.context.view_layer.objects.active = mesh
mesh.select_set(True)
bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
low = Vector((min(v.co.x for v in mesh.data.vertices), min(v.co.y for v in mesh.data.vertices), min(v.co.z for v in mesh.data.vertices)))
high = Vector((max(v.co.x for v in mesh.data.vertices), max(v.co.y for v in mesh.data.vertices), max(v.co.z for v in mesh.data.vertices)))
print("APPLIED_BOUNDS", [round(v, 3) for v in low], [round(v, 3) for v in high])
arm_data = bpy.data.armatures.new("Armature")
arm = bpy.data.objects.new("Armature", arm_data)
bpy.context.scene.collection.objects.link(arm)
scene = bpy.context.scene
scene.render.fps = 30
scene.render.fps_base = 1.0


def activate(obj, mode):
    if bpy.context.object and bpy.context.object.mode != "OBJECT":
        bpy.ops.object.mode_set(mode="OBJECT")
    bpy.ops.object.select_all(action="DESELECT")
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj
    if mode != "OBJECT":
        bpy.ops.object.mode_set(mode=mode)


points = [vertex.co.copy() for vertex in mesh.data.vertices]


def cluster(predicate):
    chosen = [point for point in points[::3] if predicate(point)]
    if len(chosen) < 8:
        return None
    center = sum(chosen, Vector()) / len(chosen)
    return center


def add_bone(name, head, tail, parent, deform):
    bone = arm_data.edit_bones.new(name)
    bone.head = Vector(head)
    bone.tail = Vector(tail)
    if (bone.tail - bone.head).length < 0.02:
        bone.tail = bone.head + Vector((0, -0.02, 0.02))
    direction = bone.tail - bone.head
    roll_hint = Vector((0, 0, 1)) if abs(direction.normalized().z) < 0.85 else Vector((1, 0, 0))
    bone.align_roll(roll_hint)
    bone.use_deform = deform
    bone.use_connect = False
    if parent:
        bone.parent = arm_data.edit_bones[parent]
    return bone


shin_l = cluster(lambda p: p.x > 0.045 and 0.15 < p.z < 0.28)
shin_r = cluster(lambda p: p.x < -0.045 and 0.15 < p.z < 0.28)
beak = cluster(lambda p: p.y < -0.16 and p.z > 0.72 and abs(p.x) < 0.1)
wing_l = cluster(lambda p: p.x > 0.16 and 0.58 < p.z < 0.8)
wing_r = cluster(lambda p: p.x < -0.16 and 0.58 < p.z < 0.8)
print("LANDMARKS", "shinL", [round(v, 3) for v in shin_l], "shinR", [round(v, 3) for v in shin_r])
print("BEAK", None if beak is None else [round(v, 3) for v in beak])
print("WINGS", None if wing_l is None else [round(v, 3) for v in wing_l], None if wing_r is None else [round(v, 3) for v in wing_r])
if shin_l is None or shin_r is None:
    sys.exit("Could not find the two legs")

activate(arm, "EDIT")
add_bone("Root", (0, 0, 0), (0, 0, 0.08), None, False)
add_bone("Body", (0, 0.02, 0.58), (0, -0.01, 0.98), "Root", True)
add_bone("Head", (0, -0.03, 0.9), beak if beak else (0, -0.22, 0.86), "Body", True)
add_bone("WingL", (0.1, 0.02, 0.7), wing_l if wing_l else (0.22, 0.02, 0.68), "Body", True)
add_bone("WingR", (-0.1, 0.02, 0.7), wing_r if wing_r else (-0.22, 0.02, 0.68), "Body", True)
for side, center in (("L", shin_l), ("R", shin_r)):
    x, y = center.x, center.y
    hip = (x, y, 0.56)
    knee = (x, y, 0.33)
    ankle = (x, y, 0.09)
    mid = (x, y - 0.07, 0.02)
    tip = (x, y - 0.16, 0.012)
    add_bone(f"Thigh{side}", hip, knee, "Body", True)
    add_bone(f"Shin{side}", knee, ankle, f"Thigh{side}", True)
    add_bone(f"Foot{side}", ankle, mid, f"Shin{side}", True)
    add_bone(f"Toe{side}", mid, tip, f"Foot{side}", True)
bpy.ops.object.mode_set(mode="OBJECT")

segments = {}
for bone in arm.data.bones:
    if bone.use_deform:
        segments[bone.name] = (bone.head_local.copy(), bone.tail_local.copy())


def segment_distance(point, head, tail):
    line = tail - head
    length2 = line.length_squared
    if length2 < 1e-8:
        return (point - head).length
    t = max(0.0, min(1.0, (point - head).dot(line) / length2))
    return (point - (head + line * t)).length


def nearest(point, names):
    ranked = sorted((segment_distance(point, *segments[name]), name) for name in names)
    first, second = ranked[0], ranked[1]
    if second[0] > first[0] + 0.04:
        return {first[1]: 1.0}
    w1 = 1.0 / (first[0] + 0.012) ** 2
    w2 = 1.0 / (second[0] + 0.012) ** 2
    total = w1 + w2
    return {first[1]: w1 / total, second[1]: w2 / total}


def weights_for(point):
    x, y, z = point.x, point.y, point.z
    side = "L" if x >= 0 else "R"
    # The web and heel sit behind and beside the shin bone. Sharing them with
    # the shin stretches a spike out of the heel when the foot lifts.
    if z < 0.13 and abs(x) > 0.015:
        return nearest(point, [f"Foot{side}", f"Toe{side}"])
    if z < 0.13:
        return nearest(point, ["FootL", "ToeL", "FootR", "ToeR"])
    if z < 0.58 and abs(x) > 0.02:
        return nearest(point, [f"Thigh{side}", f"Shin{side}", f"Foot{side}", "Body"])
    if y < -0.15 and z > 0.7 and abs(x) < 0.11:
        return {"Head": 1.0}
    if z > 0.9 or (y < -0.08 and z > 0.72):
        return nearest(point, ["Head", "Body"])
    if 0.56 < z < 0.84 and abs(x) > 0.165:
        return nearest(point, [f"Wing{side}", "Body"])
    return {"Body": 1.0}


levels = defaultdict(lambda: defaultdict(list))
region = defaultdict(int)
for index, point in enumerate(points):
    assigned = weights_for(point)
    region[max(assigned, key=assigned.get)] += 1
    for name, weight in assigned.items():
        bucket = round(weight * 20) / 20
        if bucket > 0:
            levels[name][bucket].append(index)
for name, buckets in levels.items():
    group = mesh.vertex_groups.new(name=name)
    for weight, indices in buckets.items():
        group.add(indices, weight, "REPLACE")
print("WEIGHTS", dict(sorted(region.items(), key=lambda item: -item[1])))

modifier = mesh.modifiers.new("Armature", "ARMATURE")
modifier.object = arm
modifier.use_vertex_groups = True
modifier.use_bone_envelopes = False
modifier.show_viewport = False
modifier.show_render = False
mesh.parent = arm
mesh.matrix_parent_inverse = arm.matrix_world.inverted()


def update():
    bpy.context.view_layer.update()


def clear_pose():
    activate(arm, "POSE")
    bpy.ops.pose.select_all(action="SELECT")
    bpy.ops.pose.transforms_clear()
    update()


def set_euler(name, axis, degrees):
    bone = arm.pose.bones[name]
    bone.rotation_mode = "XYZ"
    euler = [0.0, 0.0, 0.0]
    euler[axis] = math.radians(degrees)
    bone.rotation_euler = euler


def calibrate(bone_name, read, score):
    clear_pose()
    update()
    rest = read()
    best = None
    for axis in range(3):
        for sign in (1, -1):
            clear_pose()
            set_euler(bone_name, axis, sign * 25)
            update()
            value = score(read() - rest)
            if best is None or value > best[0]:
                best = (value, axis, sign)
    clear_pose()
    print(f"AXIS {bone_name} score={best[0]:.4f} axis={best[1]} sign={best[2]}")
    return best[1], best[2]


def tail_of(name):
    return arm.pose.bones[name].tail.copy()


leg_axis = {}
for side, shin in (("L", "ShinL"), ("R", "ShinR")):
    axis, sign = calibrate(f"Thigh{side}", lambda s=shin: tail_of(s), lambda delta: -delta.y)
    leg_axis[side] = (axis, sign)
    flex_axis, flex_sign = calibrate(f"Shin{side}", lambda s=shin: tail_of(s), lambda delta: delta.z - delta.y * 0.25)
    leg_axis[side + "flex"] = (flex_axis, flex_sign)
    foot_axis, foot_sign = calibrate(f"Foot{side}", lambda s=side: tail_of(f"Toe{s}"), lambda delta: -delta.z)
    leg_axis[side + "foot"] = (foot_axis, foot_sign)

body_axis = calibrate("Body", lambda: tail_of("Head"), lambda delta: -delta.y - delta.z)[0:2]
# Positive body pitch in the authored clips means "beak forward/down".
head_axis = calibrate("Head", lambda: tail_of("Head"), lambda delta: -delta.y - delta.z * 0.5)[0:2]
wing_axis = calibrate("WingL", lambda: tail_of("WingL"), lambda delta: delta.z)[0:2]
wing_axis_r = calibrate("WingR", lambda: tail_of("WingR"), lambda delta: delta.z)[0:2]
body_up = calibrate("Body", lambda: tail_of("Body"), lambda delta: delta.z)
# body_up is a rotation; bob is a location. Test location axes separately.
clear_pose()
rest_body = tail_of("Body")
bob_axis = 1
bob_sign = 1
best_bob = -1
for axis in range(3):
    for sign in (1, -1):
        clear_pose()
        bone = arm.pose.bones["Body"]
        bone.location = Vector((0, 0, 0))
        bone.location[axis] = sign * 0.04
        update()
        lift = tail_of("Body").z - rest_body.z
        if lift > best_bob:
            best_bob = lift
            bob_axis = axis
            bob_sign = sign
clear_pose()
print(f"BOB axis={bob_axis} sign={bob_sign} lift={best_bob:.4f}")


def apply_pose(pose):
    clear_pose()
    for name, (axis, degrees) in pose.get("rot", {}).items():
        set_euler(name, axis, degrees)
    for name, (axis, distance) in pose.get("loc", {}).items():
        bone = arm.pose.bones[name]
        bone.location = Vector((0, 0, 0))
        bone.location[axis] = distance
    update()


def plant_angle(side):
    axis, sign = leg_axis[side + "foot"]
    bone = arm.pose.bones[f"Foot{side}"]
    bone.rotation_mode = "XYZ"
    best = (10.0, 0.0)
    target = arm.pose.bones[f"Toe{side}"].tail.z
    # Use the bind-pose toe height captured before any clip. Stored below.
    target = BIND_TOE_Z[side]
    for angle in range(-40, 45, 10):
        euler = [0.0, 0.0, 0.0]
        euler[axis] = math.radians(angle)
        bone.rotation_euler = euler
        update()
        error = abs(arm.pose.bones[f"Toe{side}"].tail.z - target)
        if arm.pose.bones[f"Toe{side}"].tail.z < target - 0.015:
            error += 0.05
        if error < best[0]:
            best = (error, angle)
    euler = [0.0, 0.0, 0.0]
    euler[axis] = math.radians(best[1])
    bone.rotation_euler = euler
    update()
    return best[1]


clear_pose()
BIND_TOE_Z = {"L": tail_of("ToeL").z, "R": tail_of("ToeR").z}
print("BIND_TOE", {side: round(height, 4) for side, height in BIND_TOE_Z.items()})
print("BONE_LEN", [(bone.name, round((bone.tail_local - bone.head_local).length, 3)) for bone in arm.data.bones])
clear_pose()
rest_head = tail_of("Head")
set_euler("Head", head_axis[0], head_axis[1] * 40)
update()
print("HEAD_SWING", [round(v, 4) for v in (tail_of("Head") - rest_head)])


def swing(side, forward, flex):
    thigh_axis, thigh_sign = leg_axis[side]
    shin_axis, shin_sign = leg_axis[side + "flex"]
    return {
        f"Thigh{side}": (thigh_axis, thigh_sign * forward),
        f"Shin{side}": (shin_axis, shin_sign * flex),
    }


def with_plants(rotations, bob=0.0, body=0.0, head=0.0, wings=0.0):
    pose_rot = dict(rotations)
    if abs(body) > 0.01:
        pose_rot["Body"] = (body_axis[0], body_axis[1] * body)
    if abs(head) > 0.01:
        pose_rot["Head"] = (head_axis[0], head_axis[1] * head)
    if abs(wings) > 0.01:
        pose_rot["WingL"] = (wing_axis[0], wing_axis[1] * wings)
        pose_rot["WingR"] = (wing_axis_r[0], wing_axis_r[1] * -wings)
    apply_pose({"rot": pose_rot, "loc": {"Body": (bob_axis, bob_sign * bob)}})
    # The foot stays aligned with the shin. A separate foot twist sheared the web.
    for side in ("L", "R"):
        axis, _sign = leg_axis[side + "foot"]
        set_euler(f"Foot{side}", axis, 0)
        pose_rot[f"Foot{side}"] = (axis, 0.0)
    update()
    return pose_rot, bob


def key_current(frame, rotations, bob):
    for name, (axis, degrees) in rotations.items():
        bone = arm.pose.bones[name]
        bone.rotation_mode = "XYZ"
        bone.keyframe_insert("rotation_euler", frame=frame)
    body = arm.pose.bones["Body"]
    body.keyframe_insert("location", frame=frame)


arm.animation_data_create()


def begin_action(name):
    action = bpy.data.actions.new(name)
    action.use_fake_user = True
    arm.animation_data.action = action
    if hasattr(arm.animation_data, "action_slot") and getattr(action, "slots", None):
        if len(action.slots) == 0 and hasattr(action.slots, "new"):
            try:
                action.slots.new(arm.id_type, arm.name)
            except TypeError:
                pass
        if len(action.slots):
            arm.animation_data.action_slot = action.slots[0]
    return action


def author(name, keys):
    action = begin_action(name)
    if arm.animation_data.action_slot is None and len(action.slots) == 0:
        action.slots.new("OBJECT", arm.name)
        arm.animation_data.action_slot = action.slots[0]
    elif arm.animation_data.action_slot is None and len(action.slots):
        arm.animation_data.action_slot = action.slots[0]
    for frame, spec in keys:
        rotations, bob = with_plants(
            {**swing("L", spec["Lf"], spec["Ls"]), **swing("R", spec["Rf"], spec["Rs"])},
            bob=spec.get("bob", 0.0),
            body=spec.get("body", 0.0),
            head=spec.get("head", 0.0),
            wings=spec.get("wings", 0.0),
        )
        key_current(frame, rotations, bob)
    keyed = 0
    for layer in action.layers:
        for strip in layer.strips:
            bags = getattr(strip, "channelbags", None)
            if not bags:
                continue
            keyed += sum(len(bag.fcurves) for bag in bags)
    print(f"ACTION {name} curves={keyed} slot={arm.animation_data.action_slot}")
    return action


# forward > 0 reaches toward the beak. flex > 0 lifts the foot.
author("Idle", [
    (1, dict(Lf=4, Ls=6, Rf=-3, Rs=4, bob=0.0, head=0, wings=2)),
    (16, dict(Lf=1, Ls=4, Rf=1, Rs=4, bob=0.018, head=7, wings=-3)),
    (32, dict(Lf=-3, Ls=4, Rf=4, Rs=6, bob=0.0, head=-4, wings=3)),
    (48, dict(Lf=4, Ls=6, Rf=-3, Rs=4, bob=0.0, head=0, wings=2)),
])
author("Walk", [
    (1, dict(Lf=24, Ls=10, Rf=-20, Rs=14, bob=0.0, wings=6)),
    (5, dict(Lf=0, Ls=34, Rf=0, Rs=18, bob=0.03, wings=0)),
    (9, dict(Lf=-20, Ls=14, Rf=24, Rs=10, bob=0.0, wings=-6)),
    (13, dict(Lf=0, Ls=18, Rf=0, Rs=34, bob=0.03, wings=0)),
    (17, dict(Lf=24, Ls=10, Rf=-20, Rs=14, bob=0.0, wings=6)),
])
author("Run", [
    (1, dict(Lf=38, Ls=16, Rf=-32, Rs=22, bob=0.0, body=-4, wings=8)),
    (3, dict(Lf=6, Ls=46, Rf=-4, Rs=20, bob=0.045, body=-4, wings=2)),
    (6, dict(Lf=-32, Ls=22, Rf=38, Rs=16, bob=0.0, body=-4, wings=-8)),
    (8, dict(Lf=-4, Ls=20, Rf=6, Rs=46, bob=0.045, body=-4, wings=-2)),
    (11, dict(Lf=38, Ls=16, Rf=-32, Rs=22, bob=0.0, body=-4, wings=8)),
])
author("Peck", [
    (1, dict(Lf=0, Ls=4, Rf=0, Rs=4, head=0, body=0)),
    (4, dict(Lf=-6, Ls=8, Rf=-6, Rs=8, head=-14, body=-6)),
    (7, dict(Lf=10, Ls=8, Rf=10, Rs=8, head=22, body=8)),
    (12, dict(Lf=2, Ls=4, Rf=2, Rs=4, head=6, body=2)),
    (16, dict(Lf=0, Ls=4, Rf=0, Rs=4, head=0, body=0)),
])
author("Lunge", [
    (1, dict(Lf=0, Ls=4, Rf=0, Rs=4, body=0, head=0)),
    (6, dict(Lf=-12, Ls=16, Rf=-8, Rs=12, body=-10, head=-8)),
    (12, dict(Lf=30, Ls=12, Rf=-24, Rs=20, body=8, head=16, bob=0.02, wings=4)),
    (18, dict(Lf=8, Ls=10, Rf=6, Rs=8, body=2, head=4)),
    (24, dict(Lf=0, Ls=4, Rf=0, Rs=4, body=0, head=0)),
])
author("Flinch", [
    (1, dict(Lf=0, Ls=4, Rf=0, Rs=4)),
    (3, dict(Lf=-8, Ls=10, Rf=-8, Rs=10, body=-16, head=-12)),
    (8, dict(Lf=0, Ls=4, Rf=0, Rs=4)),
])
author("Knockdown", [
    (1, dict(Lf=0, Ls=4, Rf=0, Rs=4, body=0)),
    (8, dict(Lf=20, Ls=36, Rf=-16, Rs=28, body=28, head=10)),
    (16, dict(Lf=10, Ls=48, Rf=8, Rs=42, body=62, head=8)),
    (24, dict(Lf=10, Ls=48, Rf=8, Rs=42, body=62, head=8)),
])
author("Death", [
    (1, dict(Lf=0, Ls=4, Rf=0, Rs=4, body=0)),
    (10, dict(Lf=18, Ls=30, Rf=-20, Rs=36, body=40, head=6)),
    (20, dict(Lf=6, Ls=20, Rf=14, Rs=18, body=78, head=4)),
    (32, dict(Lf=6, Ls=20, Rf=14, Rs=18, body=78, head=4)),
])


def sample_mesh():
    modifier.show_viewport = True
    update()
    deps = bpy.context.evaluated_depsgraph_get()
    evaluated = mesh.evaluated_get(deps)
    data = evaluated.to_mesh()
    coords = [vertex.co.copy() for vertex in data.vertices]
    evaluated.to_mesh_clear()
    return coords


clear_pose()
arm.animation_data.action = None
rest_coords = sample_mesh()
flex_axis, flex_sign = leg_axis["Lflex"]
set_euler("ShinL", flex_axis, flex_sign * 35)
update()
posed = sample_mesh()
foot = [index for index, point in enumerate(points) if point.z < 0.05 and point.x > 0.08]
body = [index for index, point in enumerate(points) if point.z > 0.85 and abs(point.x) < 0.08 and point.y > 0.02]
other = [index for index, point in enumerate(points) if point.z < 0.05 and point.x < -0.08]
beak_ids = [index for index, point in enumerate(points) if point.y < -0.18 and point.z > 0.75]


def mean_delta(indices):
    if not indices:
        return Vector()
    total = Vector()
    for index in indices[:: max(1, len(indices) // 400)]:
        total += posed[index] - rest_coords[index]
    return total / max(1, len(indices[:: max(1, len(indices) // 400)]))


foot_delta = mean_delta(foot)
body_delta = mean_delta(body)
other_delta = mean_delta(other)
print(f"DEFORM foot={tuple(round(v, 4) for v in foot_delta)} body={tuple(round(v, 4) for v in body_delta)} other={tuple(round(v, 4) for v in other_delta)}")
if foot_delta.length < 0.03 or body_delta.length > 0.03:
    sys.exit("Deformation check failed")

arm.animation_data.action = bpy.data.actions["Peck"]
scene.frame_set(7)
peck_pose = sample_mesh()
peck_delta = Vector()
peck_sample = beak_ids[:: max(1, len(beak_ids) // 200)] or beak_ids
for index in peck_sample:
    peck_delta += peck_pose[index] - rest_coords[index]
peck_delta /= max(1, len(peck_sample))
print(f"PECK beak={tuple(round(v, 4) for v in peck_delta)}")


def render_current(name):
    update()
    deps = bpy.context.evaluated_depsgraph_get()
    evaluated = mesh.evaluated_get(deps)
    baked_data = evaluated.to_mesh().copy()
    baked = bpy.data.objects.new(name, baked_data)
    scene.collection.objects.link(baked)
    baked.matrix_world = mesh.matrix_world.copy()
    if mesh.data.materials:
        baked.data.materials.clear()
        for material in mesh.data.materials:
            baked.data.materials.append(material)
    mesh.hide_render = True
    arm.hide_render = True
    corners = [baked.matrix_world @ Vector(corner) for corner in baked.bound_box]
    center = sum(corners, Vector()) / 8
    extent = max((corner - center).length for corner in corners)
    scene.render.engine = "BLENDER_WORKBENCH"
    scene.render.resolution_x = 640
    scene.render.resolution_y = 800
    scene.render.image_settings.file_format = "PNG"
    scene.display.shading.light = "STUDIO"
    scene.display.shading.color_type = "MATERIAL"
    for view_name, view_dir in (("front", Vector((0, -1, 0.12))), ("side", Vector((1, -0.05, 0.12)))):
        camera_data = bpy.data.cameras.new(name + view_name)
        camera_data.type = "ORTHO"
        camera_data.ortho_scale = extent * 2.5
        camera = bpy.data.objects.new(name + view_name, camera_data)
        scene.collection.objects.link(camera)
        camera.location = center + view_dir.normalized() * 4
        camera.rotation_euler = (center - camera.location).to_track_quat("-Z", "Y").to_euler()
        scene.camera = camera
        scene.render.filepath = str(PREVIEW / f"{name}-{view_name}.png")
        bpy.ops.render.render(write_still=True)
    mesh.hide_render = False
    arm.hide_render = False
    bpy.data.objects.remove(baked, do_unlink=True)
    evaluated.to_mesh_clear()


clear_pose()
arm.animation_data.action = None
def repair_heel():
    modifier.show_viewport = True
    clear_pose()
    arm.animation_data.action = None
    rest = sample_mesh()
    arm.animation_data.action = bpy.data.actions["Run"]
    scene.frame_set(1)
    posed = sample_mesh()
    moved = []
    for index, point in enumerate(points):
        if point.z < 0.08 and abs(point.x) > 0.04:
            moved.append((posed[index] - rest[index]).length)
    moved.sort()
    median = moved[len(moved) // 2] if moved else 0.1
    stuck = []
    for index, point in enumerate(points):
        if point.z > 0.15 or abs(point.x) < 0.02:
            continue
        if (posed[index] - rest[index]).length < median * 0.4:
            stuck.append(index)
    print(f"HEEL median={median:.3f} stuck={len(stuck)}")
    if not stuck or len(stuck) > 25000:
        return
    stuck_set = set(stuck)
    for group in mesh.vertex_groups:
        present = [index for index in stuck if _has_weight(group, index)]
        if present:
            group.remove(present)
    extra = defaultdict(lambda: defaultdict(list))
    for index in stuck:
        point = points[index]
        side = "L" if point.x >= 0 else "R"
        for name, weight in nearest(point, [f"Foot{side}", f"Toe{side}"]).items():
            extra[name][round(weight * 20) / 20].append(index)
    for name, buckets in extra.items():
        group = mesh.vertex_groups[name]
        for weight, indices in buckets.items():
            if indices and weight > 0:
                group.add(indices, weight, "REPLACE")
    print("HEEL repaired", len(stuck_set))


def _has_weight(group, index):
    try:
        group.weight(index)
        return True
    except RuntimeError:
        return False


repair_heel()
arm.animation_data.action = bpy.data.actions["Run"]
scene.frame_set(1)
render_current("v4-run")
scene.frame_set(3)
render_current("v4-pass")

material = mesh.data.materials[0] if mesh.data.materials else None
if material and material.use_nodes:
    for node in material.node_tree.nodes:
        if node.type == "BSDF_PRINCIPLED":
            print("BASE", [round(v, 3) for v in node.inputs["Base Color"].default_value])
        if node.type == "TEX_IMAGE" and node.image:
            print("IMAGE", node.image.name, tuple(node.image.size), node.image.filepath)

activate(arm, "OBJECT")
arm.select_set(True)
mesh.select_set(True)
bpy.context.view_layer.objects.active = arm
bpy.ops.export_scene.fbx(
    filepath=str(DEST),
    use_selection=True,
    object_types={"ARMATURE", "MESH"},
    apply_scale_options="FBX_SCALE_NONE",
    apply_unit_scale=True,
    bake_anim=True,
    bake_anim_use_all_actions=True,
    bake_anim_use_nla_strips=False,
    bake_anim_force_startend_keying=True,
    bake_anim_step=1,
    bake_anim_simplify_factor=0,
    add_leaf_bones=False,
    primary_bone_axis="Y",
    secondary_bone_axis="X",
    mesh_smooth_type="FACE",
    use_mesh_modifiers=False,
    path_mode="COPY",
    embed_textures=True,
)
print("EXPORT", DEST.stat().st_size)
