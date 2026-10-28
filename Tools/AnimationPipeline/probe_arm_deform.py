"""See whether arm vertices actually follow the arm bone."""
import math
import sys
import bpy
from mathutils import Vector

src = sys.argv[sys.argv.index("--") + 1]
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=src)
arm = next(o for o in bpy.data.objects if o.type == "ARMATURE")
mesh = next(o for o in bpy.data.objects if o.type == "MESH")
print("MODS", [(m.type, m.object.name if m.type == "ARMATURE" else "", m.show_viewport) for m in mesh.modifiers])
print("GROUPS", [g.name for g in mesh.vertex_groups])
print("INHERIT")
for bone in arm.data.bones:
    if any(part in bone.name for part in ("Arm", "Hand", "Leg", "Foot", "Spine", "Head")):
        print(bone.name, "rot", getattr(bone, "use_inherit_rotation", None), getattr(bone, "inherit_rotation", None), "scale", bone.inherit_scale)
print("PARENTS")
for bone in arm.data.bones:
    print(f"  {bone.name} <- {bone.parent.name if bone.parent else '-'}")


def moved_count():
    bpy.context.view_layer.update()
    depsgraph = bpy.context.evaluated_depsgraph_get()
    evaluated = mesh.evaluated_get(depsgraph)
    count = 0
    far = 0
    for original, current in zip(mesh.data.vertices, evaluated.data.vertices):
        if (current.co - original.co).length > 0.02:
            count += 1
            point = mesh.matrix_world @ original.co
            if abs(point.x) > 0.35:
                far += 1
    return count, far


groups = {g.index: g.name for g in mesh.vertex_groups}
extreme = None
for vertex in mesh.data.vertices:
    point = mesh.matrix_world @ vertex.co
    if extreme is None or point.x > extreme[0]:
        weights = sorted(((groups.get(element.group, "?"), round(element.weight, 2)) for element in vertex.groups), key=lambda item: -item[1])[:3]
        extreme = (point.x, point.y, point.z, weights)
print("MAX X VERTEX", tuple(round(v, 3) if isinstance(v, float) else v for v in extreme))

bpy.context.view_layer.objects.active = arm
bpy.ops.object.mode_set(mode="POSE")
bpy.ops.pose.select_all(action="SELECT")
bpy.ops.pose.transforms_clear()
print("REST moved", moved_count())
for name in ("LeftArm", "RightArm"):
    bone = arm.pose.bones[name]
    bone.rotation_mode = "XYZ"
    bone.rotation_euler.x = math.radians(75)
def tail_of(name):
    bpy.context.view_layer.update()
    bone = arm.pose.bones[name]
    tail = arm.matrix_world @ bone.tail
    return tuple(round(v, 3) for v in tail)

print("TAILS", {name: tail_of(name) for name in ("LeftArm", "LeftForeArm", "LeftHand")})
index = max(range(len(mesh.data.vertices)), key=lambda i: (mesh.matrix_world @ mesh.data.vertices[i].co).x)
evaluated = mesh.evaluated_get(bpy.context.evaluated_depsgraph_get())
posed = mesh.matrix_world @ evaluated.data.vertices[index].co
print("HAND TIP POSED", tuple(round(v, 3) for v in posed))
stuck = {}
for i, vertex in enumerate(mesh.data.vertices):
    rest = mesh.matrix_world @ vertex.co
    if rest.x < 0.4:
        continue
    posed_point = mesh.matrix_world @ evaluated.data.vertices[i].co
    if posed_point.x > 0.4:
        weights = tuple(sorted(((groups.get(element.group, "?"), round(element.weight, 2)) for element in vertex.groups), key=lambda item: -item[1])[:2])
        stuck[weights] = stuck.get(weights, 0) + 1
print("STUCK", sorted(stuck.items(), key=lambda item: -item[1])[:8])
print("ARMS moved", moved_count())
depsgraph = bpy.context.evaluated_depsgraph_get()
evaluated = mesh.evaluated_get(depsgraph)
xs = [(mesh.matrix_world @ vertex.co).x for vertex in evaluated.data.vertices]
print("EVAL X", round(min(xs), 3), round(max(xs), 3))
baked = evaluated.to_mesh()
xs = [(mesh.matrix_world @ vertex.co).x for vertex in baked.vertices]
print("TOMESH X", round(min(xs), 3), round(max(xs), 3))
evaluated.to_mesh_clear()
# Which group owns a far vertex?
groups = {g.index: g.name for g in mesh.vertex_groups}
samples = []
for vertex in mesh.data.vertices:
    point = mesh.matrix_world @ vertex.co
    if point.x > 0.5 and 0.7 < point.z < 0.95 and len(samples) < 5:
        weights = sorted(((groups[e.group], round(e.weight, 2)) for e in vertex.groups), key=lambda item: -item[1])
        samples.append((tuple(round(v, 2) for v in point), weights))
print("SAMPLES", samples)
