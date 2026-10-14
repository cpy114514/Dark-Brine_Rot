import bpy,json
from pathlib import Path
ROOT=Path(r'D:/indiegameDev/unity/Dark Brine_Rot')
bpy.ops.import_scene.fbx(filepath=str(ROOT/'Assets/Game/Prefabs/Environment/Props/Ship/ship_g.fbx'))
items=[]
for o in bpy.context.selected_objects:
    if o.type!='MESH':continue
    items.append(dict(name=o.name,vertices=len(o.data.vertices),faces=len(o.data.polygons),dimensions=list(o.dimensions),location=list(o.location),materials=[m.name for m in o.data.materials if m],bounds=[list(o.matrix_world@__import__('mathutils').Vector(v)) for v in o.bound_box]))
out=ROOT/'.codex/opening-optimization-20261012/ship-source-audit.json'
out.write_text(json.dumps(items,indent=2))
