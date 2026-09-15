import bpy
from mathutils import Vector
from pathlib import Path
root=Path('D:/indie_game_dev/unity/Dark Brine_Rot/BlenderWork')
s=bpy.context.scene
s.render.engine='BLENDER_WORKBENCH'
s.render.resolution_x=1000;s.render.resolution_y=1000;s.render.resolution_percentage=100
s.display.shading.light='STUDIO';s.display.shading.color_type='SINGLE';s.display.shading.single_color=(0.65,0.65,0.65)
s.display.shading.show_shadows=True;s.display.shading.show_cavity=True
s.display.shading.cavity_type='BOTH';s.display.shading.background_type='WORLD';s.world.color=(0.12,0.12,0.12)
cam=s.camera
cam.data.type='ORTHO'
for label,target,offset,scale in [('hand_before',(-.09,.03,.44),(-.6,-.25,.12),.18),('other_hand_before',(.19,.03,.44),(.6,-.25,.12),.18)]:
 cam.location=Vector(target)+Vector(offset);cam.rotation_euler=(Vector(target)-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.ortho_scale=scale
 s.render.filepath=str(root/(label+'.png'));bpy.ops.render.render(write_still=True)
