import bpy,json,math,numpy as np
from mathutils import Vector
from pathlib import Path
root=Path('D:/indie_game_dev/unity/Dark Brine_Rot/BlenderWork');body=bpy.data.objects['Character_Body'];rig=bpy.data.objects['Armature'];bat=bpy.data.objects['Hand_Bat']
report=json.loads((root/'skin_report.json').read_text());assert report['unweighted']==0
def coords():
 bpy.context.view_layer.update();ob=body.evaluated_get(bpy.context.evaluated_depsgraph_get());me=ob.to_mesh();a=np.array([ob.matrix_world@v.co for v in me.vertices]);ob.to_mesh_clear();return a
base=coords();saved={b.name:b.matrix_basis.copy() for b in rig.pose.bones};tests={}
for name in ['Bone.004','hand.R','Bone.014','Bone.065']:
 pb=rig.pose.bones[name];pb.rotation_mode='XYZ';pb.rotation_euler.x+=math.radians(20)
 a=coords();delta=np.linalg.norm(a-base,axis=1);tests[name]={'max_vertex_motion':float(delta.max()),'moved_vertices':int((delta>1e-5).sum())};assert delta.max()>.001
 pb.matrix_basis=saved[name]
rest=coords();assert np.max(np.abs(rest-base))<1e-5
s=bpy.context.scene;cam=s.camera;old_cam=cam.matrix_world.copy();old_type=cam.data.type
target=Vector((.035,-.015,.54));cam.location=target+Vector((1,-3,1));cam.rotation_euler=(target-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.type='ORTHO';cam.data.ortho_scale=1.3
for name,axis,angle in [('Bone.004',0,-30),('hand.R',0,15),('Bone.065',0,20),('Bone.014',0,15)]:
 pb=rig.pose.bones[name];pb.rotation_mode='XYZ';pb.rotation_euler[axis]+=math.radians(angle)
bpy.context.view_layer.update();s.render.filepath=str(root/'skin_pose_check.png');bpy.ops.render.render(write_still=True)
for b in rig.pose.bones:b.matrix_basis=saved[b.name]
bpy.context.view_layer.update();cam.matrix_world=old_cam;cam.data.type=old_type
report['pose_tests']=tests;report['rest_pose_restored']=True
(root/'skin_report.json').write_text(json.dumps(report,indent=2))
bpy.ops.wm.save_as_mainfile(filepath=str(root/'character_fixed.blend'))
print('SKIN_QA_COMPLETE')
