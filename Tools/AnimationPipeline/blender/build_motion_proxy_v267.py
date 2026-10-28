"""New disposable mannequin; contains no project model, textures or skeleton data."""
import bpy
from mathutils import Vector
from pathlib import Path

out = Path(__file__).resolve().parents[3] / '.codex' / 'ai-motion-v267'
out.mkdir(parents=True, exist_ok=True)
# Runs in a fresh background Blender process, never the user's open document.
bpy.ops.wm.read_factory_settings(use_empty=True)
parts = []
def ellipsoid(name, center, size):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=20, ring_count=12, location=center)
    ob = bpy.context.object
    ob.name = name
    ob.scale = size
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    parts.append(ob)

ellipsoid('Head', (0, 0, 1.65), (.105, .10, .14))
ellipsoid('Neck', (0, 0, 1.48), (.055, .06, .09))
ellipsoid('Chest', (0, 0, 1.30), (.20, .105, .23))
ellipsoid('Waist', (0, 0, 1.09), (.12, .09, .15))
ellipsoid('Pelvis', (0, 0, .97), (.16, .11, .13))
for side in [-1, 1]:
    ellipsoid('UpperArm', (side*.31, 0, 1.41), (.16, .055, .065))
    ellipsoid('Forearm', (side*.57, 0, 1.41), (.15, .043, .048))
    ellipsoid('Hand', (side*.76, -.008, 1.41), (.075, .044, .025))
    ellipsoid('Thumb', (side*.73, -.052, 1.39), (.043, .025, .023))
    ellipsoid('Thigh', (side*.105, 0, .72), (.075, .085, .27))
    ellipsoid('Shin', (side*.105, 0, .29), (.052, .06, .23))
    ellipsoid('Foot', (side*.105, -.065, .065), (.065, .14, .065))
bpy.ops.object.select_all(action='DESELECT')
for ob in parts:
    ob.select_set(True)
bpy.context.view_layer.objects.active = parts[0]
bpy.ops.object.join()
ob = bpy.context.object
ob.name = 'TemporaryMotionProxy'
# Weld the overlapping simple volumes into one clean connected mesh.
remesh = ob.modifiers.new('UnifiedSurface', 'REMESH')
remesh.mode = 'VOXEL'
remesh.voxel_size = .012
bpy.ops.object.modifier_apply(modifier=remesh.name)
dec = ob.modifiers.new('Budget', 'DECIMATE')
dec.ratio = .35
bpy.ops.object.modifier_apply(modifier=dec.name)
for p in ob.data.polygons:
    p.use_smooth = True
bpy.ops.wm.obj_export(filepath=str(out/'TemporaryMotionProxy.obj'), export_selected_objects=True,
                      forward_axis='NEGATIVE_Z', up_axis='Y', export_materials=False)
bpy.ops.wm.save_as_mainfile(filepath=str(out/'TemporaryMotionProxy.blend'))
print('PROXY', len(ob.data.vertices), len(ob.data.polygons), str(out))
