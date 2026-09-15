import bpy, bmesh, json, math
import numpy as np
from mathutils import Vector, Matrix
from pathlib import Path
root=Path('D:/indie_game_dev/unity/Dark Brine_Rot/BlenderWork')
body=bpy.data.objects['node_0']; rig=bpy.data.objects['Armature']
assert not body.vertex_groups and not body.modifiers
bpy.ops.object.select_all(action='DESELECT');body.select_set(True);bpy.context.view_layer.objects.active=body
# Select only the disconnected bat island; separation preserves UVs and materials.
adj=[[] for v in body.data.vertices]
for e in body.data.edges:
 a,b=e.vertices;adj[a].append(b);adj[b].append(a)
seen={29846};stack=[29846]
while stack:
 for j in adj[stack.pop()]:
  if j not in seen:seen.add(j);stack.append(j)
assert len(seen)==44216
for v in body.data.vertices:v.select=v.index in seen
bpy.ops.object.mode_set(mode='EDIT');bpy.ops.mesh.separate(type='SELECTED');bpy.ops.object.mode_set(mode='OBJECT')
bat=next(o for o in bpy.context.selected_objects if o!=body)
body.name='Character_Body';bat.name='Hand_Bat'
# Remove exact duplicate finger bones and zero-length accidental extrusions.
bpy.ops.object.select_all(action='DESELECT');rig.select_set(True);bpy.context.view_layer.objects.active=rig
bpy.ops.object.mode_set(mode='EDIT')
eb=rig.data.edit_bones;kept=[];remap={};removed=[]
for b in list(eb):
 if b.length<0.00001:continue
 match=next((k for k in kept if (k.head-b.head).length<1e-6 and (k.tail-b.tail).length<1e-6),None)
 if match:remap[b.name]=match.name
 else:kept.append(b)
for b in list(eb):
 if b.parent and b.parent.name in remap:b.parent=eb[remap[b.parent.name]]
for name in remap:removed.append(name);eb.remove(eb[name])
for b in list(eb):
 if b.length<0.00001:
  for ch in list(b.children):ch.parent=b.parent;ch.use_connect=False
  removed.append(b.name);eb.remove(b)
for suffix,forearm,roots,tail in [('L','Bone.004',['Bone.008','Bone.013','Bone.016','Bone.020','Bone.024'],(.202,.033,.452)),('R','Bone.009',['Bone.045','Bone.047','Bone.050','Bone.054','Bone.058'],(-.103,.033,.453))]:
 hand=eb.new('hand.'+suffix);hand.head=eb[forearm].tail;hand.tail=rig.matrix_world.inverted()@Vector(tail);hand.parent=eb[forearm];hand.use_connect=True
 for name in roots:
  eb[name].parent=hand;eb[name].use_connect=False
bpy.ops.object.mode_set(mode='OBJECT')
# Principal axis gives the bat's true length regardless of its original tilt.
pts=np.array([bat.matrix_world@v.co for v in bat.data.vertices]);center=pts.mean(axis=0)
vals,vecs=np.linalg.eigh(np.cov((pts-center).T));axis=vecs[:,-1]
if axis[2]<0:axis=-axis
t=(pts-center)@axis;lo,hi=float(t.min()),float(t.max());length=hi-lo
old_dir=Vector((-axis).tolist());new_dir=Vector((0,-.97,-.243)).normalized()
rot=old_dir.rotation_difference(new_dir).to_matrix()
knob=Vector((center+axis*hi).tolist());grip=Vector((-.080,.028,.434))
new_knob=grip-new_dir*.065
scale=.55/length
for v,p in zip(bat.data.vertices,pts):v.co=new_knob+rot@(Vector(p.tolist())-knob)*scale
bat.matrix_world=Matrix.Identity(4)
# Put the object origin at the grip and use rigid bone parenting.
for v in bat.data.vertices:v.co-=grip
bat.location=grip;bpy.context.view_layer.update()
world=bat.matrix_world.copy();bat.parent=rig;bat.parent_type='BONE';bat.parent_bone='hand.R'
bpy.context.view_layer.update();bat.matrix_world=world;bpy.context.view_layer.update()
original=bat.matrix_world.copy();pb=rig.pose.bones['hand.R'];old_basis=pb.matrix_basis.copy();pb.rotation_mode='XYZ';pb.rotation_euler.y=math.radians(20);bpy.context.view_layer.update()
changed=max(abs(bat.matrix_world[i][j]-original[i][j]) for i in range(4) for j in range(4))
pb.matrix_basis=old_basis;bpy.context.view_layer.update()
assert changed>0.001,'Bat did not follow hand bone'
assert max(abs(bat.matrix_world[i][j]-original[i][j]) for i in range(4) for j in range(4))<1e-5
assert len(body.data.vertices)+len(bat.data.vertices)==249986
rig.show_in_front=True
(root/'fix_report.json').write_text(json.dumps({'removed_duplicate_or_zero_length_bones':removed,'bones_after':len(rig.data.bones),'bat_length_before':length,'bat_length_after':.55,'bat_scale_factor':scale,'parent_bone':bat.parent_bone,'bone_motion_test_delta':changed,'body_vertices':len(body.data.vertices),'bat_vertices':len(bat.data.vertices),'body_has_skin_weights':False},indent=2))
bpy.ops.wm.save_as_mainfile(filepath=str(root/'character_fixed.blend'))
print('CHARACTER_FIXED_OK')
