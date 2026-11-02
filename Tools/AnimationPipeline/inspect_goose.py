import bpy
from mathutils import Vector

path = r"C:\Users\pinyu\Downloads\652a0ad76bdbd6c3b1438d5fa7a116fc.fbx"
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=path, use_anim=False)
print("OBJECTS", len(bpy.data.objects))
for obj in bpy.data.objects:
    print(f"OBJ {obj.type} {obj.name} parent={obj.parent.name if obj.parent else '-'} rot={[round(v, 3) for v in obj.rotation_euler]} dims={[round(v, 3) for v in obj.dimensions]}")
mesh_obj = next(obj for obj in bpy.data.objects if obj.type == "MESH")
print("ARMATURES", [obj.name for obj in bpy.data.objects if obj.type == "ARMATURE"])
bpy.context.view_layer.objects.active = mesh_obj
mesh_obj.select_set(True)
if any(abs(v) > 0.01 for v in mesh_obj.rotation_euler):
    bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
mesh = mesh_obj.data
print(f"MESH verts={len(mesh.vertices)} faces={len(mesh.polygons)} mats={len(mesh.materials)} images={len(bpy.data.images)}")
for image in bpy.data.images:
    if image.size[0] > 8:
        print("IMAGE", image.name, tuple(image.size))
world = [vertex.co.copy() for vertex in mesh.vertices]
low = Vector((min(p.x for p in world), min(p.y for p in world), min(p.z for p in world)))
high = Vector((max(p.x for p in world), max(p.y for p in world), max(p.z for p in world)))
print("BOUNDS", [round(v, 3) for v in low], [round(v, 3) for v in high])
step = max(1, len(world) // 100000)
points = world[::step]
span = (high.z - low.z) or 1
bands = 28
buckets = [[] for _ in range(bands)]
for point in points:
    buckets[min(bands - 1, int((point.z - low.z) / span * bands))].append(point)
for index, bucket in enumerate(buckets):
    if not bucket:
        continue
    z0 = low.z + span * index / bands
    xs = [p.x for p in bucket]
    ys = [p.y for p in bucket]
    print(f"Z {z0:.3f} n={len(bucket):5d} x={min(xs):+.3f}..{max(xs):+.3f} y={min(ys):+.3f}..{max(ys):+.3f} cx={sum(xs)/len(xs):+.3f} cy={sum(ys)/len(ys):+.3f}")
