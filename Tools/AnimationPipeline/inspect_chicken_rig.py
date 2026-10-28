"""Read-only dump of an imported FBX armature and skin weights."""
import json
import sys
from pathlib import Path

import bpy
from mathutils import Vector

argv = sys.argv[sys.argv.index("--") + 1 :]
src = argv[0]
out = Path(argv[1])


def clear():
    bpy.ops.wm.read_factory_settings(use_empty=True)


def import_fbx(path):
    clear()
    try:
        bpy.ops.wm.fbx_import(filepath=path)
    except Exception:
        bpy.ops.import_scene.fbx(filepath=path)


def bone_row(arm, bone):
    head = arm.matrix_world @ bone.head_local
    tail = arm.matrix_world @ bone.tail_local
    direction = (tail - head)
    length = direction.length
    direction = direction.normalized() if length > 1e-8 else Vector()
    return {
        "name": bone.name,
        "parent": bone.parent.name if bone.parent else None,
        "deform": bone.use_deform,
        "length": round(length, 4),
        "head": [round(v, 4) for v in head],
        "tail": [round(v, 4) for v in tail],
        "dir": [round(v, 3) for v in direction],
        "x_axis": [round(v, 3) for v in (bone.matrix_local.to_3x3() @ Vector((1, 0, 0)))],
        "children": [c.name for c in bone.children],
    }


def weights(mesh, bone_names):
    groups = {g.index: g.name for g in mesh.vertex_groups}
    used = {}
    zero = 0
    multi = 0
    total = len(mesh.data.vertices)
    for vertex in mesh.data.vertices:
        named = []
        for element in vertex.groups:
            name = groups.get(element.group)
            if name and element.weight > 0.001:
                named.append((name, element.weight))
                used[name] = used.get(name, 0) + 1
        if not named:
            zero += 1
        if len(named) > 4:
            multi += 1
    missing = sorted(set(bone_names) - set(used))
    extra = sorted(set(used) - set(bone_names))
    return {
        "mesh": mesh.name,
        "verts": total,
        "zero_weight": zero,
        "over_four_influences": multi,
        "groups": len(mesh.vertex_groups),
        "unused_bones": missing[:40],
        "groups_without_bones": extra[:40],
    }


import_fbx(src)
arms = [o for o in bpy.data.objects if o.type == "ARMATURE"]
meshes = [o for o in bpy.data.objects if o.type == "MESH"]
report = {"file": src, "armatures": [], "meshes": []}
for arm in arms:
    bones = [bone_row(arm, bone) for bone in arm.data.bones]
    report["armatures"].append(
        {
            "name": arm.name,
            "scale": [round(v, 4) for v in arm.scale],
            "location": [round(v, 4) for v in arm.location],
            "bone_count": len(bones),
            "bones": bones,
        }
    )
    names = [b.name for b in arm.data.bones]
    for mesh in meshes:
        if any(m.type == "ARMATURE" and m.object == arm for m in mesh.modifiers):
            report["meshes"].append(weights(mesh, names))
if not report["meshes"]:
    for mesh in meshes:
        report["meshes"].append({"mesh": mesh.name, "verts": len(mesh.data.vertices), "groups": len(mesh.vertex_groups), "note": "no armature modifier"})

out.parent.mkdir(parents=True, exist_ok=True)
out.write_text(json.dumps(report, indent=2), encoding="utf-8")
print("WROTE", out, "bones", sum(a["bone_count"] for a in report["armatures"]))
