import bpy,bmesh,math,json
from pathlib import Path
from mathutils import Vector
root=Path('D:/indie_game_dev/unity/Dark Brine_Rot/BlenderWork')
body=bpy.data.objects['Character_Body'];rig=bpy.data.objects['Armature'];bat=bpy.data.objects['Hand_Bat']
bpy.ops.wm.save_as_mainfile(filepath=str(root/'before_finger_correction.blend'),copy=True)
bat_world=bat.matrix_world.copy()
with bpy.data.libraries.load(str(root/'character_before.blend'),link=False) as (src,dst):
 dst.armatures=list(src.armatures)
restored=dst.armatures[0]
assert len(restored.bones)==71
rig.data=restored
bpy.ops.object.select_all(action='DESELECT');rig.select_set(True);bpy.context.view_layer.objects.active=rig
bpy.ops.object.mode_set(mode='EDIT')
for suffix,forearm,tail in [('L','Bone.004',(.202,.033,.452)),('R','Bone.009',(-.103,.033,.453))]:
 hand=rig.data.edit_bones.new('hand.'+suffix);hand.head=rig.data.edit_bones[forearm].tail;hand.tail=rig.matrix_world.inverted()@Vector(tail);hand.parent=rig.data.edit_bones[forearm];hand.use_connect=True
bpy.ops.object.mode_set(mode='OBJECT');bpy.context.view_layer.update();bat.matrix_world=bat_world
original=json.loads((root/'scene.json').read_text())
for entry in next(o for o in original if o['type']=='ARMATURE')['bones']:
 b=rig.data.bones[entry['name']]
 assert (rig.matrix_world@b.head_local-Vector(entry['head'])).length<1e-6
 assert (rig.matrix_world@b.tail_local-Vector(entry['tail'])).length<1e-6
 assert (b.parent.name if b.parent else None)==entry['parent']
# Rounded-ended interdigital slots remove the fused bridges while retaining the palm.
bpy.ops.object.select_all(action='DESELECT');body.select_set(True);bpy.context.view_layer.objects.active=body
before=len(body.data.vertices)
for side,x0,x1,ys in [('L',.145,.24,[.012,.030,.054]),('R',-.145,-.045,[.012,.033,.055])]:
 for i,y in enumerate(ys):
  rad=.0025;top=[.449,.444,.443][i];bottom=.365
  profile=[(y-rad,bottom),(y+rad,bottom)]
  profile += [(y+rad*math.cos(a*math.pi/16),top-rad+rad*math.sin(a*math.pi/16)) for a in range(17)]
  n=len(profile);verts=[(x,yy,z) for x in [x0,x1] for yy,z in profile]
  faces=[tuple(range(n-1,-1,-1)),tuple(range(n,2*n))]+[(j,(j+1)%n,(j+1)%n+n,j+n) for j in range(n)]
  mesh=bpy.data.meshes.new('FingerGapCutter');mesh.from_pydata(verts,[],faces);mesh.update()
  bm=bmesh.new();bm.from_mesh(mesh);bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bm.to_mesh(mesh);bm.free()
  cutter=bpy.data.objects.new('FingerGapCutter',mesh);bpy.context.collection.objects.link(cutter)
  mod=body.modifiers.new('Separate fused fingers '+side+str(i),'BOOLEAN');mod.operation='DIFFERENCE';mod.solver='EXACT';mod.object=cutter
  bpy.ops.object.modifier_apply(modifier=mod.name)
  bpy.data.objects.remove(cutter,do_unlink=True);bpy.data.meshes.remove(mesh)
for p in body.data.polygons:p.use_smooth=True
rig.show_in_front=True
(root/'finger_correction_report.json').write_text(json.dumps({'original_bones_restored':71,'added_hand_bones':2,'vertices_before':before,'vertices_after':len(body.data.vertices),'finger_gap_width':.005,'bat_parent':bat.parent_bone},indent=2))
bpy.ops.wm.save_as_mainfile(filepath=str(root/'character_fixed.blend'))
print('BONES_RESTORED_AND_MESH_FINGERS_SEPARATED')
