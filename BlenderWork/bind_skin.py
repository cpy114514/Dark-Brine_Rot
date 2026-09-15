import bpy,json
from pathlib import Path
root=Path('D:/indie_game_dev/unity/Dark Brine_Rot/BlenderWork')
body=bpy.data.objects['Character_Body'];rig=bpy.data.objects['Armature']
if bpy.context.object and bpy.context.object.mode!='OBJECT':bpy.ops.object.mode_set(mode='OBJECT')
bpy.ops.wm.save_as_mainfile(filepath=str(root/'character_before_skin.blend'),copy=True)
assert len(body.vertex_groups)==0,'Existing weights found; inspect before replacing'
kept=[];disabled=[]
for b in rig.data.bones:
 duplicate=any((k.head_local-b.head_local).length<1e-6 and (k.tail_local-b.tail_local).length<1e-6 for k in kept)
 b.use_deform=b.length>1e-5 and not duplicate
 if b.use_deform:kept.append(b)
 else:disabled.append(b.name)
bpy.ops.object.select_all(action='DESELECT');rig.select_set(True);bpy.context.view_layer.objects.active=rig
bpy.ops.object.mode_set(mode='EDIT')
eb=rig.data.edit_bones
for suffix,forearm in [('L','Bone.004'),('R','Bone.009')]:
 hand=eb['hand.'+suffix]
 for b in list(eb):
  if b!=hand and b.parent==eb[forearm] and b.length>1e-5:
   b.parent=hand;b.use_connect=False
bpy.ops.object.mode_set(mode='OBJECT')
body.select_set(True)
try:
 result=str(bpy.ops.object.parent_set(type='ARMATURE_AUTO'))
except Exception as ex:result=str(ex)
bpy.context.view_layer.update()
deform_names={b.name for b in rig.data.bones if b.use_deform}
indices={g.index for g in body.vertex_groups if g.name in deform_names}
missing=[v.index for v in body.data.vertices if sum(g.weight for g in v.groups if g.group in indices)<1e-6]
report={'auto_weight_result':result,'vertices':len(body.data.vertices),'unweighted':len(missing),'disabled_deform_bones_preserved':disabled,'groups':len(body.vertex_groups),'modifiers':[(m.name,m.type) for m in body.modifiers]}
(root/'skin_report.json').write_text(json.dumps(report,indent=2))
print(report)
