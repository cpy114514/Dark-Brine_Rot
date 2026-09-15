import bpy,bmesh,json
from pathlib import Path
from mathutils import Vector
root=Path('D:/indie_game_dev/unity/Dark Brine_Rot/BlenderWork');s=bpy.context.scene;cam=s.camera;bat=bpy.data.objects['Hand_Bat'];body=bpy.data.objects['Character_Body']
cam_matrix=cam.matrix_world.copy();cam_type=cam.data.type;bat.hide_render=True
for label,target,offset in [('fingers_right_fixed',(-.09,.03,.43),(-.6,-.12,.12)),('fingers_left_fixed',(.19,.03,.43),(.6,-.12,.12))]:
 cam.location=Vector(target)+Vector(offset);cam.rotation_euler=(Vector(target)-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.type='ORTHO';cam.data.ortho_scale=.165
 s.render.filepath=str(root/(label+'.png'));bpy.ops.render.render(write_still=True)
cam.matrix_world=cam_matrix;cam.data.type=cam_type;bat.hide_render=False
bm=bmesh.new();bm.from_mesh(body.data)
report=json.loads((root/'finger_correction_report.json').read_text());report['mesh_boundary_edges']=sum(e.is_boundary for e in bm.edges);report['nonmanifold_edges']=sum(not e.is_manifold for e in bm.edges);bm.free()
(root/'finger_correction_report.json').write_text(json.dumps(report,indent=2))
print('FINGER_QA_DONE')
