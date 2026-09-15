import bpy,bmesh
body=bpy.data.objects['Character_Body'];bm=bmesh.new();bm.from_mesh(body.data)
layer=bm.edges.layers.float.get('bevel_weight_edge') or bm.edges.layers.float.new('bevel_weight_edge')
for e in bm.edges:
 p=body.matrix_world@e.verts[0].co;q=body.matrix_world@e.verts[1].co
 hand=(p.x<-.04 or p.x>.14) and .365<p.z<.452 and (q.x<-.04 or q.x>.14) and .365<q.z<.452
 e[layer]=1.0 if hand and e.is_manifold and e.calc_face_angle()>.5 else 0.0
bm.to_mesh(body.data);bm.free()
bpy.context.view_layer.objects.active=body
mod=body.modifiers.new('Round repaired finger seams','BEVEL');mod.limit_method='WEIGHT';mod.width=.0013;mod.segments=3;mod.use_clamp_overlap=True
bpy.ops.object.modifier_apply(modifier=mod.name)
exec(compile(open('D:/indie_game_dev/unity/Dark Brine_Rot/BlenderWork/fingers_qa.py',encoding='utf-8').read(),'fingers_qa.py','exec'))
