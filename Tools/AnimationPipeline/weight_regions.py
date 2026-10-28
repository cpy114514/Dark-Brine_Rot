"""Count which bone owns the head, the arms, and the legs."""
import sys
import bpy
from mathutils import Vector

src = sys.argv[sys.argv.index("--") + 1]
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=src)
mesh = next(o for o in bpy.data.objects if o.type == "MESH")
groups = {group.index: group.name for group in mesh.vertex_groups}

regions = {
    "face": lambda p: p.z > 0.95 and p.y < 0.02 and abs(p.x) < 0.28,
    "hair": lambda p: p.z > 1.05 and p.y > 0.05,
    "left-arm": lambda p: p.x > 0.38 and 0.55 < p.z < 1.05,
    "torso": lambda p: abs(p.x) < 0.2 and 0.4 < p.z < 0.85 and abs(p.y) < 0.25,
    "left-pants": lambda p: 0.08 < p.x < 0.28 and 0.16 < p.z < 0.4,
    "left-shoe": lambda p: p.x > 0.05 and p.z < 0.12,
}
counts = {name: {} for name in regions}
for vertex in mesh.data.vertices:
    best_name, best_weight = "none", 0.0
    for element in vertex.groups:
        if element.weight > best_weight:
            best_name = groups.get(element.group, "none")
            best_weight = element.weight
    point = mesh.matrix_world @ vertex.co
    for name, test in regions.items():
        if test(point):
            counts[name][best_name] = counts[name].get(best_name, 0) + 1

for name, bones in counts.items():
    ranked = sorted(bones.items(), key=lambda item: -item[1])[:6]
    print(name, ranked)
