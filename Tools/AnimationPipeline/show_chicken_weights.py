"""Color the chicken by its dominant bone and print where each bone's skin sits."""
import sys
from pathlib import Path

import bpy
from mathutils import Vector

src, preview_dir = sys.argv[sys.argv.index("--") + 1 :]
preview_dir = Path(preview_dir)
preview_dir.mkdir(parents=True, exist_ok=True)

bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=src)
arm = next(o for o in bpy.data.objects if o.type == "ARMATURE")
mesh = next(o for o in bpy.data.objects if o.type == "MESH")

palette = {
    "Hips": (0.45, 0.28, 0.16),
    "Spine": (0.55, 0.35, 0.2),
    "Spine1": (0.7, 0.45, 0.2),
    "Spine2": (0.85, 0.55, 0.15),
    "Neck": (0.95, 0.8, 0.2),
    "Head": (0.95, 0.85, 0.2),
    "LeftShoulder": (0.2, 0.6, 0.7),
    "LeftArm": (0.1, 0.75, 0.9),
    "LeftForeArm": (0.2, 0.35, 0.95),
    "LeftHand": (0.55, 0.2, 0.95),
    "RightShoulder": (0.2, 0.7, 0.45),
    "RightArm": (0.15, 0.85, 0.35),
    "RightForeArm": (0.45, 0.85, 0.2),
    "RightHand": (0.75, 0.9, 0.25),
    "LeftUpLeg": (0.85, 0.75, 0.15),
    "LeftLeg": (0.9, 0.15, 0.15),
    "LeftFoot": (0.1, 0.85, 0.25),
    "LeftToeBase": (0.15, 0.35, 0.95),
    "RightUpLeg": (0.85, 0.45, 0.15),
    "RightLeg": (0.75, 0.1, 0.35),
    "RightFoot": (0.1, 0.7, 0.55),
    "RightToeBase": (0.2, 0.8, 0.85),
}

groups = {group.index: group.name for group in mesh.vertex_groups}
stats = {}
colors = []
for vertex in mesh.data.vertices:
    best_name, best_weight = "none", 0.0
    for element in vertex.groups:
        if element.weight > best_weight:
            best_name = groups.get(element.group, "none")
            best_weight = element.weight
    point = mesh.matrix_world @ vertex.co
    bucket = stats.setdefault(best_name, {"count": 0, "min": Vector((9, 9, 9)), "max": Vector((-9, -9, -9))})
    bucket["count"] += 1
    bucket["min"] = Vector(tuple(min(bucket["min"][i], point[i]) for i in range(3)))
    bucket["max"] = Vector(tuple(max(bucket["max"][i], point[i]) for i in range(3)))
    color = palette.get(best_name, (0.5, 0.5, 0.5))
    colors.append((*color, 1.0))

print("WEIGHT REGIONS")
for name, bucket in sorted(stats.items(), key=lambda item: -item[1]["count"]):
    if bucket["count"] < 50:
        continue
    low, high = bucket["min"], bucket["max"]
    print(
        f"{name:16} {bucket['count']:6}  "
        f"x={low.x:6.2f}..{high.x:6.2f}  y={low.y:6.2f}..{high.y:6.2f}  z={low.z:6.2f}..{high.z:6.2f}"
    )

mesh.data.color_attributes.remove(mesh.data.color_attributes[0]) if mesh.data.color_attributes else None
attribute = mesh.data.color_attributes.new("Weights", "BYTE_COLOR", "POINT")
attribute.data.foreach_set("color", [channel for color in colors for channel in color])

bpy.context.view_layer.objects.active = arm
bpy.ops.object.mode_set(mode="POSE")
bpy.ops.pose.select_all(action="SELECT")
bpy.ops.pose.transforms_clear()
bpy.ops.object.mode_set(mode="OBJECT")

scene = bpy.context.scene
scene.render.engine = "BLENDER_WORKBENCH"
scene.render.resolution_x = 900
scene.render.resolution_y = 1100
scene.render.image_settings.file_format = "PNG"
scene.display.shading.light = "FLAT"
scene.display.shading.color_type = "VERTEX"
points = [mesh.matrix_world @ Vector(corner) for corner in mesh.bound_box]
center = sum(points, Vector()) / len(points)
extent = max((point - center).length for point in points)
for view_name, view_dir in (("front", Vector((0, -1, 0.08))), ("side", Vector((1, -0.2, 0.08)))):
    cam_data = bpy.data.cameras.new(view_name)
    cam_data.type = "ORTHO"
    cam_data.ortho_scale = extent * 2.3
    cam = bpy.data.objects.new(view_name, cam_data)
    scene.collection.objects.link(cam)
    cam.location = center + view_dir.normalized() * (extent * 4 + 2)
    cam.rotation_euler = (center - cam.location).to_track_quat("-Z", "Y").to_euler()
    scene.camera = cam
    scene.render.filepath = str(preview_dir / f"weights-{view_name}.png")
    bpy.ops.render.render(write_still=True)
print("RENDERED")
