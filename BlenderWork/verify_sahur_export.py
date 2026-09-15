import bpy,json
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath='D:/indie_game_dev/3D Models/tongtongtongsahur_walk.fbx')
rig=next(o for o in bpy.context.scene.objects if o.type=='ARMATURE')
mesh=next(o for o in bpy.context.scene.objects if o.type=='MESH')
action=rig.animation_data.action
poses={}
first,last=map(int,action.frame_range)
for f in [first,first+8,first+16,first+24,last]:
    bpy.context.scene.frame_set(f)
    poses[f]={b.name:list(b.matrix.translation) for b in rig.pose.bones}
seam=max(abs(poses[first][n][i]-poses[last][n][i]) for n in poses[first] for i in range(3))
motion=max(abs(poses[first][n][i]-poses[first+16][n][i]) for n in poses[first] for i in range(3))
assert seam<.0001,(seam,'loop seam')
assert motion>.01,(motion,'no animation')
assert len(mesh.vertex_groups)>0
print('EXPORT_VALIDATION',json.dumps({'bones':len(rig.pose.bones),'vertices':len(mesh.data.vertices),'action':action.name,'frame_range':list(action.frame_range),'loop_seam':seam,'motion':motion,'images':[(im.name,list(im.size)) for im in bpy.data.images]}))
