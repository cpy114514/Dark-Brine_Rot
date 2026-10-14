import bpy,json
from pathlib import Path
from mathutils import Vector
ROOT=Path(r'D:/indiegameDev/unity/Dark Brine_Rot')
# Inspect the user's active imported model without clearing or modifying it.
bpy.ops.import_scene.fbx(filepath=str(ROOT/'Assets/Game/Prefabs/Characters/Sahur/Models/PbrSahur.fbx'))
rigs=[o for o in bpy.context.scene.objects if o.type=='ARMATURE'];report=[]
for rig in rigs:
    report.append(dict(name=rig.name,matrix=[list(r) for r in rig.matrix_world],bones={b.name:dict(head=list(rig.matrix_world@b.head_local),matrix=[list(r) for r in b.matrix_local],parent=b.parent.name if b.parent else None) for b in rig.data.bones},meshes=[o.name for o in bpy.context.scene.objects if o.type=='MESH' and any(m.type=='ARMATURE' and m.object==rig for m in o.modifiers)]))
(ROOT/'.codex/opening-optimization-20261012/actual-sahur-source-rig.json').write_text(json.dumps(report,indent=2))
