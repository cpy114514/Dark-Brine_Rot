"""Rig the static bully goose and author original in-place clips.

The mesh faces -Y after the FBX's 90-degree import rotation is applied.
The long neck is a thin column, the wings are the wide mid-body, and the
legs are short. No third-party animation is used.
"""
import math
import shutil
import sys
from pathlib import Path

import bpy
from mathutils import Vector

SRC = r"C:\Users\pinyu\Downloads\652a0ad76bdbd6c3b1438d5fa7a116fc.fbx"
DEST = Path(r"D:\indiegameDev\unity\Dark Brine_Rot\Temp\goose\BullyGoose.fbx")
ASSET = Path(r"D:\indiegameDev\unity\Dark Brine_Rot\Assets\Game\Prefabs\Enemies\BullyGoose\BullyGoose.fbx")
TEX = Path(r"D:\indiegameDev\unity\Dark Brine_Rot\Assets\Game\Prefabs\Enemies\BullyGoose\Textures")
PREVIEW = DEST.parent
PREVIEW.mkdir(parents=True, exist_ok=True)
TEX.mkdir(parents=True, exist_ok=True)
ASSET.parent.mkdir(parents=True, exist_ok=True)

bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=SRC, use_anim=False)
meshes = [obj for obj in bpy.data.objects if obj.type == "MESH"]
if not meshes:
    sys.exit("No mesh in the goose FBX")
mesh = max(meshes, key=lambda obj: len(obj.data.vertices))
mesh.name = "Mesh_0"
print("IMPORT", mesh.name, len(mesh.data.vertices), [round(v, 3) for v in mesh.rotation_euler])
for image in bpy.data.images:
    if image.size[0] < 32:
        continue
    safe = image.name if image.name.lower().endswith(".png") else image.name + ".png"
    image.filepath_raw = str(TEX / safe)
    image.file_format = "PNG"
    image.save()
    print("SAVED", safe, int(image.size[0]), int(image.size[1]))

bpy.context.view_layer.objects.active = mesh
mesh.select_set(True)
bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
points = [vertex.co.copy() for vertex in mesh.data.vertices]
low = Vector((min(p.x for p in points), min(p.y for p in points), min(p.z for p in points)))
high = Vector((max(p.x for p in points), max(p.y for p in points), max(p.z for p in points)))
print("BOUNDS", [round(v, 3) for v in low], [round(v, 3) for v in high])

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


def cluster(predicate):
    chosen = [point for point in points[::2] if predicate(point)]
    if len(chosen) < 12:
        return None
    return sum(chosen, Vector()) / len(chosen)


def band(z0, z1, extra):
    return cluster(lambda p, a=z0, b=z1: a <= p.z < b and extra(p))


neck_pts = []
for z0 in (0.58, 0.68, 0.78, 0.88):
    found = band(z0, z0 + 0.1, lambda p: abs(p.x) < 0.1 and -0.25 < p.y < 0.08)
    neck_pts.append(found if found is not None else Vector((0.0, -0.1, z0 + 0.05)))
    print("NECK", round(z0, 2), [round(v, 3) for v in neck_pts[-1]])
beak = cluster(lambda p: p.y < -0.18 and p.z > 0.84 and abs(p.x) < 0.12)
print("BEAK", None if beak is None else [round(v, 3) for v in beak])
shin_l = cluster(lambda p: 0.04 < p.x < 0.22 and 0.05 < p.z < 0.16 and p.y < 0.06)
shin_r = cluster(lambda p: -0.22 < p.x < -0.04 and 0.05 < p.z < 0.16 and p.y < 0.06)
print("SHIN", None if shin_l is None else [round(v, 3) for v in shin_l], None if shin_r is None else [round(v, 3) for v in shin_r])
if shin_l is None:
    shin_l = Vector((0.1, -0.02, 0.1))
if shin_r is None:
    shin_r = Vector((-0.1, -0.02, 0.1))
wing_mid_l = cluster(lambda p: 0.16 < p.x < 0.32 and 0.34 < p.z < 0.54) or Vector((0.24, 0.02, 0.44))
wing_tip_l = cluster(lambda p: p.x > 0.32 and 0.34 < p.z < 0.54) or Vector((0.44, 0.03, 0.46))
wing_mid_r = cluster(lambda p: -0.32 < p.x < -0.16 and 0.34 < p.z < 0.54) or Vector((-0.24, 0.02, 0.44))
wing_tip_r = cluster(lambda p: p.x < -0.32 and 0.34 < p.z < 0.54) or Vector((-0.44, 0.03, 0.46))
print("WINGL", [round(v, 3) for v in wing_mid_l], [round(v, 3) for v in wing_tip_l])


def add_bone(name, head, tail, parent, deform):
    bone = arm_data.edit_bones.new(name)
    bone.head = Vector(head)
    bone.tail = Vector(tail)
    if (bone.tail - bone.head).length < 0.02:
        bone.tail = bone.head + Vector((0.0, -0.02, 0.04))
    direction = (bone.tail - bone.head).normalized()
    bone.align_roll(Vector((0.0, 0.0, 1.0)) if abs(direction.z) < 0.85 else Vector((1.0, 0.0, 0.0)))
    bone.use_deform = deform
    bone.use_connect = False
    if parent:
        bone.parent = arm_data.edit_bones[parent]
    return bone


activate(arm, "EDIT")
add_bone("Root", (0, 0, 0), (0, 0, 0.08), None, False)
body_tail = (0.0, -0.03, 0.54)
add_bone("Body", (0.0, 0.04, 0.22), body_tail, "Root", True)
previous = "Body"
start = body_tail
names = ("Neck1", "Neck2", "Neck3", "Neck4")
for index, name in enumerate(names):
    end = neck_pts[index]
    add_bone(name, start, end, previous, True)
    start = end
    previous = name
add_bone("Head", start, beak if beak is not None else (0.0, -0.28, 0.98), "Neck4", True)
add_bone("WingL", (0.12, 0.0, 0.44), wing_mid_l, "Body", True)
add_bone("WingL2", wing_mid_l, wing_tip_l, "WingL", True)
add_bone("WingR", (-0.12, 0.0, 0.44), wing_mid_r, "Body", True)
add_bone("WingR2", wing_mid_r, wing_tip_r, "WingR", True)
for side, center in (("L", shin_l), ("R", shin_r)):
    x, y = center.x, center.y
    hip = (x, y, 0.22)
    knee = (x, y, 0.13)
    ankle = (x, y, 0.055)
    mid = (x, y - 0.06, 0.012)
    tip = (x, y - 0.14, 0.008)
    add_bone(f"Thigh{side}", hip, knee, "Body", True)
    add_bone(f"Shin{side}", knee, ankle, f"Thigh{side}", True)
    add_bone(f"Foot{side}", ankle, mid, f"Shin{side}", True)
    add_bone(f"Toe{side}", mid, tip, f"Foot{side}", True)
bpy.ops.object.mode_set(mode="OBJECT")
print("BONES", [(bone.name, round((bone.tail_local - bone.head_local).length, 3)) for bone in arm.data.bones])

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
    if second[0] > first[0] + 0.035:
        return {first[1]: 1.0}
    w1 = 1.0 / (first[0] + 0.01) ** 2
    w2 = 1.0 / (second[0] + 0.01) ** 2
    total = w1 + w2
    return {first[1]: w1 / total, second[1]: w2 / total}


def weights_for(point):
    x, y, z = point.x, point.y, point.z
    side = "L" if x >= 0 else "R"
    if z < 0.09 and abs(x) > 0.015:
        return nearest(point, [f"Foot{side}", f"Toe{side}"])
    if y > 0.1 and z < 0.34:
        return {"Body": 1.0}
    if z < 0.24 and 0.025 < abs(x) < 0.22 and y < 0.08:
        return nearest(point, [f"Thigh{side}", f"Shin{side}", f"Foot{side}", "Body"])
    if y < -0.18 and z > 0.82:
        return {"Head": 1.0}
    if z > 0.93:
        return nearest(point, ["Head", "Neck4"])
    if z > 0.52 and abs(x) < 0.1 and y > -0.25:
        return nearest(point, ["Neck1", "Neck2", "Neck3", "Neck4", "Head", "Body"])
    if 0.30 < z < 0.56 and abs(x) > 0.15:
        return nearest(point, [f"Wing{side}", f"Wing{side}2", "Body"])
    return {"Body": 1.0}


from collections import defaultdict

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
    if best[0] < 0.004:
        print(f"WARN weak axis {bone_name}")
    return best[1], best[2]


def tail_of(name):
    return arm.pose.bones[name].tail.copy()


leg_axis = {}
for side in ("L", "R"):
    leg_axis[side] = calibrate(f"Thigh{side}", lambda s=side: tail_of(f"Shin{s}"), lambda delta: -delta.y)
    leg_axis[side + "flex"] = calibrate(f"Shin{side}", lambda s=side: tail_of(f"Foot{s}"), lambda delta: delta.z)
    leg_axis[side + "foot"] = calibrate(f"Foot{side}", lambda s=side: tail_of(f"Toe{s}"), lambda delta: -delta.z)
body_axis = calibrate("Body", lambda: tail_of("Head"), lambda delta: -delta.y)
head_axis = calibrate("Head", lambda: tail_of("Head"), lambda delta: -delta.y)
neck_axis = {}
for name in ("Neck1", "Neck2", "Neck3", "Neck4"):
    neck_axis[name] = calibrate(name, lambda n=name: tail_of(n), lambda delta: -delta.y)
wing_lift = {}
wing_sweep = {}
for bone, tip in (("WingL", "WingL2"), ("WingL2", "WingL2"), ("WingR", "WingR2"), ("WingR2", "WingR2")):
    wing_lift[bone] = calibrate(bone, lambda t=tip: tail_of(t), lambda delta: delta.z)
    wing_sweep[bone] = calibrate(bone, lambda t=tip: tail_of(t), lambda delta: -delta.y)

clear_pose()
rest_body = tail_of("Body")
bob_axis, bob_sign, best_bob = 2, 1, -1.0
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


def add_rot(rot, name, axis, degrees):
    axes = rot.setdefault(name, {})
    axes[axis] = axes.get(axis, 0.0) + degrees


def apply_pose(rot, bob):
    clear_pose()
    for name, axes in rot.items():
        bone = arm.pose.bones[name]
        bone.rotation_mode = "XYZ"
        euler = [0.0, 0.0, 0.0]
        for axis, degrees in axes.items():
            euler[axis] = math.radians(degrees)
        bone.rotation_euler = euler
    body = arm.pose.bones["Body"]
    body.location = Vector((0, 0, 0))
    body.location[bob_axis] = bob_sign * bob
    update()


def pose_for(spec):
    rot = {}
    for side, forward, flex in (("L", spec["Lf"], spec["Ls"]), ("R", spec["Rf"], spec["Rs"])):
        thigh_axis, thigh_sign = leg_axis[side]
        shin_axis, shin_sign = leg_axis[side + "flex"]
        foot_axis, _foot_sign = leg_axis[side + "foot"]
        add_rot(rot, f"Thigh{side}", thigh_axis, thigh_sign * forward)
        add_rot(rot, f"Shin{side}", shin_axis, shin_sign * flex)
        add_rot(rot, f"Foot{side}", foot_axis, 0.0)
    body = spec.get("body", 0.0)
    if abs(body) > 0.01:
        add_rot(rot, "Body", body_axis[0], body_axis[1] * body)
    head = spec.get("head", 0.0)
    if abs(head) > 0.01:
        add_rot(rot, "Head", head_axis[0], head_axis[1] * head)
    for index, amount in enumerate(spec.get("neck", (0, 0, 0, 0))):
        name = f"Neck{index + 1}"
        axis, sign = neck_axis[name]
        add_rot(rot, name, axis, sign * amount)
    lift = spec.get("lift", 0.0)
    sweep = spec.get("sweep", 0.0)
    for bone in ("WingL", "WingL2", "WingR", "WingR2"):
        if abs(lift) > 0.01:
            axis, sign = wing_lift[bone]
            add_rot(rot, bone, axis, sign * lift)
        if abs(sweep) > 0.01:
            axis, sign = wing_sweep[bone]
            add_rot(rot, bone, axis, sign * sweep)
    apply_pose(rot, spec.get("bob", 0.0))


def sample_mesh():
    modifier.show_viewport = True
    update()
    deps = bpy.context.evaluated_depsgraph_get()
    evaluated = mesh.evaluated_get(deps)
    data = evaluated.to_mesh()
    coords = [vertex.co.copy() for vertex in data.vertices]
    evaluated.to_mesh_clear()
    modifier.show_viewport = False
    return coords


def mean_delta(rest, posed, indices):
    if not indices:
        return Vector()
    step = max(1, len(indices) // 400)
    chosen = indices[::step]
    total = Vector()
    for index in chosen:
        total += posed[index] - rest[index]
    return total / len(chosen)


clear_pose()
rest_coords = sample_mesh()
flex_axis, flex_sign = leg_axis["Lflex"]
set_euler("ShinL", flex_axis, flex_sign * 35)
update()
foot_pose = sample_mesh()
clear_pose()
for name, (axis, sign) in neck_axis.items():
    set_euler(name, axis, sign * 18)
set_euler("Head", head_axis[0], head_axis[1] * 18)
update()
neck_pose = sample_mesh()
clear_pose()
sweep_axis, sweep_sign = wing_sweep["WingL"]
set_euler("WingL", sweep_axis, sweep_sign * 40)
set_euler("WingL2", wing_sweep["WingL2"][0], wing_sweep["WingL2"][1] * 40)
update()
wing_pose = sample_mesh()
clear_pose()

foot_ids = [index for index, point in enumerate(points) if point.z < 0.06 and point.x > 0.04]
other_ids = [index for index, point in enumerate(points) if point.z < 0.06 and point.x < -0.04]
body_ids = [index for index, point in enumerate(points) if 0.28 < point.z < 0.42 and abs(point.x) < 0.08 and abs(point.y) < 0.08]
beak_ids = [index for index, point in enumerate(points) if point.y < -0.2 and point.z > 0.85]
wing_ids = [index for index, point in enumerate(points) if point.x > 0.32 and 0.34 < point.z < 0.54]
foot_delta = mean_delta(rest_coords, foot_pose, foot_ids)
other_delta = mean_delta(rest_coords, foot_pose, other_ids)
body_from_leg = mean_delta(rest_coords, foot_pose, body_ids)
beak_delta = mean_delta(rest_coords, neck_pose, beak_ids)
body_from_neck = mean_delta(rest_coords, neck_pose, body_ids)
wing_delta = mean_delta(rest_coords, wing_pose, wing_ids)
print(f"DEFORM foot={tuple(round(v, 4) for v in foot_delta)} other={tuple(round(v, 4) for v in other_delta)} body={tuple(round(v, 4) for v in body_from_leg)}")
print(f"NECK beak={tuple(round(v, 4) for v in beak_delta)} body={tuple(round(v, 4) for v in body_from_neck)}")
print(f"WING tip={tuple(round(v, 4) for v in wing_delta)}")
if foot_delta.length < 0.012 or other_delta.length > 0.02 or body_from_leg.length > 0.03:
    sys.exit("Leg deformation check failed")
if beak_delta.y > -0.03 or body_from_neck.length > 0.04:
    sys.exit("Neck deformation check failed")
if wing_delta.length < 0.02:
    sys.exit("Wing deformation check failed")

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


def key_current(frame):
    for bone in arm.pose.bones:
        if bone.name == "Root":
            continue
        bone.rotation_mode = "XYZ"
        bone.keyframe_insert("rotation_euler", frame=frame)
    arm.pose.bones["Body"].keyframe_insert("location", frame=frame)


def author(name, keys):
    action = begin_action(name)
    slot = getattr(arm.animation_data, "action_slot", None)
    if slot is None and len(action.slots) == 0:
        action.slots.new("OBJECT", arm.name)
        arm.animation_data.action_slot = action.slots[0]
    elif slot is None and len(action.slots):
        arm.animation_data.action_slot = action.slots[0]
    for frame, spec in keys:
        pose_for(spec)
        key_current(frame)
    keyed = 0
    for layer in action.layers:
        for strip in layer.strips:
            bags = getattr(strip, "channelbags", None)
            if not bags:
                continue
            keyed += sum(len(bag.fcurves) for bag in bags)
    print(f"ACTION {name} curves={keyed}")
    return action


rest = dict(Lf=2, Ls=4, Rf=2, Rs=4)
author("Idle", [
    (1, dict(rest, bob=0.0, head=2, neck=(6, -7, 6, -4), lift=2)),
    (16, dict(rest, bob=0.012, head=7, neck=(-4, 8, -5, 6), lift=-2)),
    (32, dict(rest, bob=0.0, head=-5, neck=(7, -5, 8, -6), lift=3)),
    (48, dict(rest, bob=0.0, head=2, neck=(6, -7, 6, -4), lift=2)),
])
author("Walk", [
    (1, dict(Lf=16, Ls=8, Rf=-14, Rs=12, bob=0.0, neck=(3, -2, 3, -1), lift=4)),
    (5, dict(Lf=0, Ls=22, Rf=2, Rs=8, bob=0.016, neck=(1, 3, -2, 2), lift=0)),
    (9, dict(Lf=-14, Ls=12, Rf=16, Rs=8, bob=0.0, neck=(-2, 2, 3, -2), lift=-4)),
    (13, dict(Lf=2, Ls=8, Rf=0, Rs=22, bob=0.016, neck=(2, -2, 1, 3), lift=0)),
    (17, dict(Lf=16, Ls=8, Rf=-14, Rs=12, bob=0.0, neck=(3, -2, 3, -1), lift=4)),
])
author("Run", [
    (1, dict(Lf=28, Ls=12, Rf=-22, Rs=16, body=14, neck=(6, 8, 6, 4), lift=8)),
    (4, dict(Lf=4, Ls=28, Rf=-2, Rs=12, body=14, bob=0.02, neck=(5, 7, 5, 3), lift=2)),
    (7, dict(Lf=-22, Ls=16, Rf=28, Rs=12, body=14, neck=(6, 8, 6, 4), lift=-8)),
    (10, dict(Lf=-2, Ls=12, Rf=4, Rs=28, body=14, bob=0.02, neck=(5, 7, 5, 3), lift=-2)),
    (13, dict(Lf=28, Ls=12, Rf=-22, Rs=16, body=14, neck=(6, 8, 6, 4), lift=8)),
])
author("Peck", [
    (1, dict(rest)),
    (5, dict(Lf=-4, Ls=8, Rf=-4, Rs=8, body=-6, head=-8, neck=(-8, -12, -14, -8))),
    (9, dict(Lf=8, Ls=6, Rf=8, Rs=6, body=10, head=16, neck=(16, 22, 28, 18))),
    (14, dict(Lf=2, Ls=4, Rf=2, Rs=4, body=3, head=4, neck=(4, 6, 5, 3))),
    (18, dict(rest)),
])
author("Lunge", [
    (1, dict(rest)),
    (6, dict(Lf=-6, Ls=10, Rf=-6, Rs=10, body=8, head=10, neck=(10, 14, 12, 8), lift=18)),
    (14, dict(Lf=22, Ls=10, Rf=-16, Rs=14, body=18, head=14, neck=(14, 20, 22, 14), sweep=12)),
    (22, dict(Lf=4, Ls=8, Rf=4, Rs=8, body=4, head=3, neck=(4, 6, 4, 2))),
    (30, dict(rest)),
])
author("Wing", [
    (1, dict(rest)),
    (5, dict(Lf=-4, Ls=8, Rf=-4, Rs=8, body=4, neck=(4, 6, 4, 2), lift=42)),
    (9, dict(Lf=6, Ls=6, Rf=6, Rs=6, body=8, head=6, neck=(6, 8, 6, 4), sweep=52, lift=8)),
    (16, dict(rest)),
])
author("Alert", [
    (1, dict(rest)),
    (8, dict(Lf=-4, Ls=6, Rf=-4, Rs=6, body=6, head=12, neck=(12, 16, 14, 8), lift=16)),
    (16, dict(Lf=-2, Ls=4, Rf=-2, Rs=4, body=6, head=12, neck=(14, 18, 16, 10), lift=18)),
])
author("Flinch", [
    (1, dict(rest)),
    (3, dict(Lf=-6, Ls=10, Rf=-6, Rs=10, body=-12, head=-14, neck=(-12, -16, -14, -8), lift=10)),
    (8, dict(rest)),
])
author("Knockdown", [
    (1, dict(rest)),
    (8, dict(Lf=12, Ls=24, Rf=-10, Rs=20, body=28, head=8, neck=(6, 10, 14, 10), lift=16)),
    (16, dict(Lf=8, Ls=30, Rf=6, Rs=26, body=58, head=6, neck=(10, 16, 22, 14), lift=8)),
    (24, dict(Lf=8, Ls=30, Rf=6, Rs=26, body=58, head=6, neck=(10, 16, 22, 14), lift=8)),
])
author("Death", [
    (1, dict(rest)),
    (10, dict(Lf=10, Ls=18, Rf=-12, Rs=22, body=36, head=6, neck=(6, 12, 16, 10), lift=12)),
    (20, dict(Lf=4, Ls=14, Rf=8, Rs=12, body=76, head=4, neck=(8, 14, 20, 12), lift=6)),
    (32, dict(Lf=4, Ls=14, Rf=8, Rs=12, body=76, head=4, neck=(8, 14, 20, 12), lift=6)),
])


def render_current(name):
    modifier.show_viewport = True
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
    for view_name, view_dir in (("front", Vector((0, -1, 0.15))), ("side", Vector((1, -0.08, 0.18)))):
        camera_data = bpy.data.cameras.new(name + view_name)
        camera_data.type = "ORTHO"
        camera_data.ortho_scale = extent * 2.6
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
    modifier.show_viewport = False


def has_weight(group, index):
    try:
        group.weight(index)
        return True
    except RuntimeError:
        return False


def repair_heel():
    clear_pose()
    arm.animation_data.action = None
    rest = sample_mesh()
    pose_for(dict(Lf=4, Ls=28, Rf=-2, Rs=12, body=14, neck=(6, 8, 6, 4)))
    posed = sample_mesh()
    moved = []
    for index, point in enumerate(points):
        if point.z < 0.14 and abs(point.x) > 0.03 and point.y < 0.08:
            moved.append((posed[index] - rest[index]).length)
    moved.sort()
    median = moved[len(moved) // 2] if moved else 0.05
    stuck = []
    for index, point in enumerate(points):
        if point.z > 0.16 or abs(point.x) < 0.02 or point.y > 0.1:
            continue
        if (posed[index] - rest[index]).length < median * 0.35:
            stuck.append(index)
    print(f"HEEL median={median:.3f} stuck={len(stuck)}")
    if not stuck or len(stuck) > 8000:
        return
    for group in mesh.vertex_groups:
        present = [index for index in stuck if has_weight(group, index)]
        if present:
            group.remove(present)
    extra = defaultdict(lambda: defaultdict(list))
    for index in stuck:
        point = points[index]
        side = "L" if point.x >= 0.0 else "R"
        for name, weight in nearest(point, [f"Foot{side}", f"Toe{side}"]).items():
            extra[name][round(weight * 20) / 20].append(index)
    for name, buckets in extra.items():
        group = mesh.vertex_groups[name]
        for weight, indices in buckets.items():
            if indices and weight > 0:
                group.add(indices, weight, "REPLACE")
    print("HEEL repaired", len(stuck))


repair_heel()
clear_pose()
arm.animation_data.action = bpy.data.actions["Peck"]
scene.frame_set(9)
render_current("v2-peck")
arm.animation_data.action = bpy.data.actions["Wing"]
scene.frame_set(9)
render_current("v2-wing")
arm.animation_data.action = bpy.data.actions["Run"]
scene.frame_set(1)
render_current("v2-run")
arm.animation_data.action = bpy.data.actions["Idle"]
scene.frame_set(16)
render_current("v2-idle")

clear_pose()
arm.animation_data.action = None
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
shutil.copy2(DEST, ASSET)
print("EXPORT", DEST.stat().st_size, ASSET)
