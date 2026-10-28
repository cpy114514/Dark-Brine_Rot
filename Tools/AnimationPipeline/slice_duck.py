import bpy
from mathutils import Vector

path = r"C:\Users\pinyu\Downloads\duck.fbx"
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=path, use_anim=False)
obj = next(item for item in bpy.data.objects if item.type == "MESH")
world = [obj.matrix_world @ vertex.co for vertex in obj.data.vertices]
step = max(1, len(world) // 120000)
points = world[::step]
lowz, highz = min(p.z for p in points), max(p.z for p in points)
print(f"SAMPLES {len(points)} Z {lowz:.3f} {highz:.3f}")
bands = 24
span = (highz - lowz) or 1
buckets = [[] for _ in range(bands)]
for point in points:
    buckets[min(bands - 1, int((point.z - lowz) / span * bands))].append(point)
for index, bucket in enumerate(buckets):
    if not bucket:
        continue
    z0 = lowz + span * index / bands
    xs = [p.x for p in bucket]
    ys = [p.y for p in bucket]
    print(
        f"Z {z0:.3f} n={len(bucket):5d} x={min(xs):+.3f}..{max(xs):+.3f} y={min(ys):+.3f}..{max(ys):+.3f} "
        f"cx={sum(xs)/len(xs):+.3f} cy={sum(ys)/len(ys):+.3f}"
    )

# Split lower body into left/right columns to find each leg.
print("--- LEG COLUMNS z<0.55 ---")
legs = [p for p in points if p.z < 0.55]
for side, chosen in (("L", [p for p in legs if p.x < -0.02]), ("R", [p for p in legs if p.x > 0.02]), ("C", [p for p in legs if abs(p.x) <= 0.02])):
    if not chosen:
        print(side, "empty")
        continue
    print(f"{side} n={len(chosen)} x={min(p.x for p in chosen):+.3f}..{max(p.x for p in chosen):+.3f} y={min(p.y for p in chosen):+.3f}..{max(p.y for p in chosen):+.3f} z={min(p.z for p in chosen):.3f}..{max(p.z for p in chosen):.3f}")

print("--- UPPER SLICES y bands at z>0.7 ---")
upper = [p for p in points if p.z > 0.7]
lowy, highy = min(p.y for p in upper), max(p.y for p in upper)
ybands = 12
yspan = (highy - lowy) or 1
ybuckets = [[] for _ in range(ybands)]
for point in upper:
    ybuckets[min(ybands - 1, int((point.y - lowy) / yspan * ybands))].append(point)
for index, bucket in enumerate(ybuckets):
    if not bucket:
        continue
    y0 = lowy + yspan * index / ybands
    print(f"Y {y0:+.3f} n={len(bucket):5d} z={min(p.z for p in bucket):.3f}..{max(p.z for p in bucket):.3f} x={min(p.x for p in bucket):+.3f}..{max(p.x for p in bucket):+.3f}")
