"""Make an editable, one-handed Sahur charge take without changing the combo FBX.

Run from Blender 5.x in background mode. The imported Mixamo right arm is
guided around the character's right side and behind the shoulder with a
temporary IK target, then baked back to ordinary armature keyframes.
"""

import bpy
import json
from pathlib import Path
from mathutils import Vector

PROJECT = Path(__file__).resolve().parent.parent
SOURCE = PROJECT / "Assets/Game/Prefabs/Characters/Sahur/Animations/Source/SwordAndShieldSlash_ThreeHit.fbx"
OUTPUT = PROJECT / "Assets/Game/Prefabs/Characters/Sahur/Animations/Gameplay/SahurRightHandCharge.fbx"
BLEND = PROJECT / "Tools/blender/SahurRightHandCharge.blend"
REPORT = PROJECT / "Temp/sahur-right-hand-charge-blender.json"

bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=str(SOURCE))
armature = next(obj for obj in bpy.data.objects if obj.type == "ARMATURE")
scene = bpy.context.scene
scene.render.fps = 30
scene.frame_start = 1
scene.frame_end = 41
scene.frame_set(1)

# Freeze the source's opening pose as the retargetable base. Only the right arm
# will animate in this dedicated take; Unity's AvatarMask supplies locomotion.
opening_pose = {
    bone.name: (
        bone.location.copy(),
        bone.rotation_mode,
        bone.rotation_quaternion.copy(),
        bone.scale.copy(),
    )
    for bone in armature.pose.bones
}
armature.animation_data_clear()
action = bpy.data.actions.new("SahurRightHandCharge")
armature.animation_data_create().action = action
for bone in armature.pose.bones:
    location, rotation_mode, rotation, scale = opening_pose[bone.name]
    bone.rotation_mode = rotation_mode
    for frame in (1, 41):
        bone.location = location
        bone.rotation_quaternion = rotation
        bone.scale = scale
        bone.keyframe_insert(data_path="location", frame=frame, group=bone.name)
        bone.keyframe_insert(data_path="rotation_quaternion", frame=frame, group=bone.name)
        bone.keyframe_insert(data_path="scale", frame=frame, group=bone.name)

# In the imported Mixamo axes +Y is Sahur's right and +X is back. Move the grip
# outward first, then draw it rearward; the shaft never needs to cross the head.
target = bpy.data.objects.new("RightHandChargePath", None)
scene.collection.objects.link(target)
target.empty_display_type = "SPHERE"
target.empty_display_size = 0.06
hand = armature.pose.bones["mixamorig:RightHand"]
start = armature.matrix_world @ hand.head
path = (
    (1, start),
    (8, Vector((-0.27, 0.35, 1.03))),
    (17, Vector((-0.19, 0.42, 1.12))),
    (29, Vector((-0.04, 0.41, 1.17))),
    (41, Vector((0.09, 0.37, 1.18))),
)
for frame, position in path:
    target.location = position
    target.keyframe_insert(data_path="location", frame=frame)

forearm = armature.pose.bones["mixamorig:RightForeArm"]
constraint = forearm.constraints.new("IK")
constraint.name = "Bake Right-Side Windup"
constraint.target = target
constraint.chain_count = 2
constraint.use_stretch = False

bpy.ops.object.select_all(action="DESELECT")
armature.select_set(True)
bpy.context.view_layer.objects.active = armature
bpy.ops.object.mode_set(mode="POSE")
bpy.ops.nla.bake(
    frame_start=1,
    frame_end=41,
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
for frame in (1, 8, 17, 29, 41):
    scene.frame_set(frame)
    position = armature.matrix_world @ armature.pose.bones["mixamorig:RightHand"].head
    samples[str(frame)] = [round(value, 4) for value in position]

# Keep the .blend source editable, but export only the armature for Unity.
BLEND.parent.mkdir(parents=True, exist_ok=True)
bpy.ops.wm.save_as_mainfile(filepath=str(BLEND))
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
REPORT.write_text(json.dumps({
    "blender": bpy.app.version_string,
    "source": str(SOURCE),
    "output": str(OUTPUT),
    "blend": str(BLEND),
    "samples": samples,
    "keyframes": [frame for frame, _ in path],
}, indent=2), encoding="utf-8")
