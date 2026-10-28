import bpy
from mathutils import Vector

path = r"C:\Users\pinyu\Downloads\duck.fbx"
bpy.ops.wm.read_factory_settings(use_empty=True)
try:
    bpy.ops.import_scene.fbx(filepath=path, use_anim=False)
except Exception as error:
    print("IMPORT_FBX_FAILED", error)
    bpy.ops.wm.fbx_import(filepath=path)

print("OBJECTS", len(bpy.data.objects))
for obj in bpy.data.objects:
    print(f"OBJ {obj.type} {obj.name} parent={obj.parent.name if obj.parent else '-'} loc={tuple(round(v, 4) for v in obj.location)} scale={tuple(round(v, 4) for v in obj.scale)} dims={tuple(round(v, 4) for v in obj.dimensions)}")

for obj in bpy.data.objects:
    if obj.type != "MESH":
        continue
    mesh = obj.data
    print(f"MESH {obj.name} verts={len(mesh.vertices)} faces={len(mesh.polygons)} mats={len(mesh.materials)} vcols={len(mesh.color_attributes)} uvs={len(mesh.uv_layers)}")
    for slot, material in enumerate(mesh.materials):
        print(f"  MAT {slot} {material.name if material else 'None'}")
    world = [obj.matrix_world @ vertex.co for vertex in mesh.vertices]
    low = Vector((min(p.x for p in world), min(p.y for p in world), min(p.z for p in world)))
    high = Vector((max(p.x for p in world), max(p.y for p in world), max(p.z for p in world)))
    print(f"  BOUNDS {tuple(round(v, 4) for v in low)} -> {tuple(round(v, 4) for v in high)} size={tuple(round(v, 4) for v in (high - low))}")
    # Density along Z (Blender up after typical FBX) and Y.
    for axis, label in ((2, "Z"), (1, "Y"), (0, "X")):
        bins = [0] * 16
        span = high[axis] - low[axis] or 1
        for point in world[:: max(1, len(world) // 80000)]:
            bins[min(15, int((point[axis] - low[axis]) / span * 16))] += 1
        print(f"  DENSITY_{label} {bins}")
    # Extreme samples.
    samples = {
        "minz": min(world, key=lambda p: p.z),
        "maxz": max(world, key=lambda p: p.z),
        "miny": min(world, key=lambda p: p.y),
        "maxy": max(world, key=lambda p: p.y),
        "minx": min(world, key=lambda p: p.x),
        "maxx": max(world, key=lambda p: p.x),
    }
    for name, point in samples.items():
        print(f"  EXTREME {name} {tuple(round(v, 4) for v in point)}")
    if mesh.color_attributes:
        attr = mesh.color_attributes[0]
        print(f"  COLOR {attr.name} domain={attr.domain} type={attr.data_type}")
