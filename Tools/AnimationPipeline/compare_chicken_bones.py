"""Print world-space bone heads for the original and the fixed chicken."""
import sys
import bpy
from mathutils import Vector

path = sys.argv[sys.argv.index("--") + 1]
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=path)
arm = next(o for o in bpy.data.objects if o.type == "ARMATURE")
mesh = next(o for o in bpy.data.objects if o.type == "MESH")
print("FILE", path)
print("OBJECTS", [(o.name, o.type, tuple(round(v, 4) for v in o.scale)) for o in bpy.data.objects])
names = [
    "Hips", "Spine", "Spine1", "Spine2", "Neck", "Head",
    "LeftShoulder", "LeftArm", "LeftForeArm", "LeftHand",
    "RightArm", "RightForeArm",
    "LeftUpLeg", "LeftLeg", "LeftFoot", "LeftToeBase",
    "RightUpLeg", "RightLeg", "RightFoot", "RightToeBase",
]
for name in names:
    bone = arm.data.bones.get(name)
    if bone is None:
        print("MISSING", name)
        continue
    head = arm.matrix_world @ bone.head_local
    tail = arm.matrix_world @ bone.tail_local
    direction = tail - head
    print(
        f"{name:16} head=({head.x:7.3f},{head.y:7.3f},{head.z:7.3f})"
        f" tail=({tail.x:7.3f},{tail.y:7.3f},{tail.z:7.3f})"
        f" len={direction.length:6.3f} dir=({direction.x:6.3f},{direction.y:6.3f},{direction.z:6.3f})"
    )
points = [mesh.matrix_world @ Vector(corner) for corner in mesh.bound_box]
print(
    "MESH",
    tuple(round(min(p[i] for p in points), 3) for i in range(3)),
    tuple(round(max(p[i] for p in points), 3) for i in range(3)),
    "verts", len(mesh.data.vertices),
)
