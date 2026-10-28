"""Find which local axis lowers the arm without squashing it."""
import math
import bpy
from mathutils import Vector

src = sys.argv[sys.argv.index("--") + 1] if False else None
import sys
src = sys.argv[sys.argv.index("--") + 1]
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=src)
arm = next(o for o in bpy.data.objects if o.type == "ARMATURE")


def direction(name):
    bpy.context.view_layer.update()
    bone = arm.pose.bones[name]
    delta = (arm.matrix_world @ bone.tail) - (arm.matrix_world @ bone.head)
    return tuple(round(v, 3) for v in delta), tuple(round(v, 3) for v in bone.scale)


bpy.context.view_layer.objects.active = arm
bpy.ops.object.mode_set(mode="POSE")
print("REST", direction("LeftArm"), direction("RightArm"), direction("LeftLeg"))
for axis, degrees in (("X", 70), ("X", -70), ("Z", 70), ("Z", -70)):
    for name in ("LeftArm", "RightArm"):
        bpy.ops.pose.select_all(action="SELECT")
        bpy.ops.pose.transforms_clear()
        bone = arm.pose.bones[name]
        bone.rotation_mode = "XYZ"
        angles = [0.0, 0.0, 0.0]
        angles["XYZ".index(axis)] = math.radians(degrees)
        bone.rotation_euler = angles
        print(name, axis, degrees, direction(name))
for axis in "XYZ":
    bpy.ops.pose.select_all(action="SELECT")
    bpy.ops.pose.transforms_clear()
    bone = arm.pose.bones["LeftLeg"]
    bone.rotation_mode = "XYZ"
    angles = [0.0, 0.0, 0.0]
    angles["XYZ".index(axis)] = math.radians(40)
    bone.rotation_euler = angles
    print("Leg", axis, "+40", direction("LeftLeg"))
