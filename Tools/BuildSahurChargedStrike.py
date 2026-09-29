"""Author a forward, one-handed charged strike from the final charge pose.

Run with Blender 5.x in background mode. The grip travels around Sahur's
right side and then down in front of him; the hips supply a short forward
lunge, so Unity can take both direction and timing from the exported clip.
"""

import bpy
import json
from pathlib import Path
from mathutils import Vector

PROJECT = Path(__file__).resolve().parent.parent
CHARGE_BLEND = PROJECT / "Tools/blender/SahurRightHandCharge.blend"
STRIKE_BLEND = PROJECT / "Tools/blender/SahurRightHandChargedStrike.blend"
OUTPUT = PROJECT / "Assets/Game/Prefabs/Characters/Sahur/Animations/Gameplay/SahurRightHandChargedStrike.fbx"
REPORT = PROJECT / "Temp/sahur-charged-strike-blender.json"

bpy.ops.wm.open_mainfile(filepath=str(CHARGE_BLEND))
scene = bpy.context.scene
scene.render.fps = 30
scene.frame_set(41)
armature = next(obj for obj in bpy.data.objects if obj.type == "ARMATURE")
charge_pose = {
    bone.name: (
        bone.location.copy(), bone.rotation_mode,
        bone.rotation_quaternion.copy(), bone.scale.copy(),
    )
    for bone in armature.pose.bones
}

armature.animation_data_clear()
action = bpy.data.actions.new("SahurRightHandChargedStrike")
armature.animation_data_create().action = action
scene.frame_start = 1
scene.frame_end = 37
for bone in armature.pose.bones:
    location, rotation_mode, rotation, scale = charge_pose[bone.name]
    bone.rotation_mode = rotation_mode
    for frame in (1, 37):
        bone.location = location
        bone.rotation_quaternion = rotation
        bone.scale = scale
        bone.keyframe_insert(data_path="location", frame=frame, group=bone.name)
        bone.keyframe_insert(data_path="rotation_quaternion", frame=frame, group=bone.name)
        bone.keyframe_insert(data_path="scale", frame=frame, group=bone.name)

# The imported Mixamo rig is scaled 0.01: -X is forward and bone-local Y is up.
# Travel remains in the FBX, rather than being invented by gameplay code.
hips = armature.pose.bones["mixamorig:Hips"]
hip_start = charge_pose[hips.name][0]
points = (
    # frame, metres forward, metres up, grip position relative to hips
    (1, 0.00, 0.00, Vector((0.09, 0.37, 1.18))),
    (5, 0.05, 0.02, Vector((-0.08, 0.46, 1.28))),
    (12, 0.29, 0.07, Vector((-0.42, 0.49, 1.36))),
    (19, 0.70, 0.03, Vector((-0.78, 0.39, 1.09))),
    (26, 0.90, 0.00, Vector((-0.88, 0.28, 0.78))),
    (37, 0.95, 0.00, Vector((-0.58, 0.37, 0.94))),
)

target = bpy.data.objects.get("RightHandChargePath")
if target is None:
    target = bpy.data.objects.new("RightHandStrikePath", None)
    scene.collection.objects.link(target)
target.name = "RightHandStrikePath"
target.animation_data_clear()
target.empty_display_type = "SPHERE"
target.empty_display_size = 0.06
for frame, forward, up, grip in points:
    hips.location = hip_start + Vector((-100.0 * forward, 100.0 * up, 0.0))
    hips.keyframe_insert(data_path="location", frame=frame, group=hips.name)
    target.location = grip + Vector((-forward, 0.0, up))
    target.keyframe_insert(data_path="location", frame=frame)

forearm = armature.pose.bones["mixamorig:RightForeArm"]
constraint = forearm.constraints.new("IK")
constraint.name = "Bake Forward Strike"
constraint.target = target
constraint.chain_count = 2
constraint.use_stretch = False

bpy.ops.object.select_all(action="DESELECT")
armature.select_set(True)
bpy.context.view_layer.objects.active = armature
bpy.ops.object.mode_set(mode="POSE")
bpy.ops.nla.bake(
    frame_start=1,
    frame_end=37,
    step=1,
    only_selected=False,
    visual_keying=True,
    clear_constraints=True,
    clear_parents=False,
    use_current_action=True,
    bake_types={"POSE"},
)
bpy.ops.object.mode_set(mode="OBJECT")

samples = {}
for frame, _, _, _ in points:
    scene.frame_set(frame)
    hand = armature.matrix_world @ armature.pose.bones["mixamorig:RightHand"].head
    hip = armature.matrix_world @ armature.pose.bones["mixamorig:Hips"].head
    samples[str(frame)] = {
        "hand": [round(value, 4) for value in hand],
        "hip": [round(value, 4) for value in hip],
    }

STRIKE_BLEND.parent.mkdir(parents=True, exist_ok=True)
bpy.ops.wm.save_as_mainfile(filepath=str(STRIKE_BLEND))
bpy.ops.object.select_all(action="DESELECT")
armature.select_set(True)
bpy.context.view_layer.objects.active = armature
OUTPUT.parent.mkdir(parents=True, exist_ok=True)
bpy.ops.export_scene.fbx(
    filepath=str(OUTPUT),
    use_selection=True,
    object_types={"ARMATURE"},
    add_leaf_bones=False,
    bake_anim=True,
    bake_anim_use_nla_strips=False,
    bake_anim_use_all_actions=False,
    bake_anim_simplify_factor=0.0,
)
REPORT.parent.mkdir(parents=True, exist_ok=True)
REPORT.write_text(json.dumps({"blender": bpy.app.version_string, "samples": samples}, indent=2), encoding="utf-8")
