import bpy,json
from mathutils import Vector,Matrix
from pathlib import Path
root=Path('D:/indie_game_dev/unity/Dark Brine_Rot/BlenderWork');s=bpy.context.scene;cam=s.camera
for label,target,offset,scale in [('hand_after',(-.09,.03,.44),(-.6,-.25,.12),.20),('character_after',(.015,-.06,.54),(1,-3,1.0),1.3)]:
 cam.location=Vector(target)+Vector(offset);cam.rotation_euler=(Vector(target)-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.type='ORTHO';cam.data.ortho_scale=scale
 s.render.filepath=str(root/(label+'.png'));bpy.ops.render.render(write_still=True)
# Restore the original camera transform after inspection.
original=json.loads((root/'scene.json').read_text());cam.matrix_world=Matrix(next(o['matrix'] for o in original if o['name']=='Camera'));cam.data.type='PERSP'
bpy.ops.object.select_all(action='DESELECT');body=bpy.data.objects['Character_Body'];body.select_set(True);bpy.context.view_layer.objects.active=body
area=bpy.context.area
if area:
 area.type='VIEW_3D';area.spaces.active.region_3d.view_location=Vector((.02,-.04,.53));area.spaces.active.region_3d.view_distance=1.85
 area.spaces.active.region_3d.view_rotation=Vector((1,-3,1)).to_track_quat('Z','Y')
 area.spaces.active.overlay.show_overlays=False
bpy.ops.wm.save_as_mainfile(filepath=str(root/'character_fixed.blend'))
print('PREVIEWS_COMPLETE')
