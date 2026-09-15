import bpy, os, json
from mathutils import Vector
bpy.ops.wm.open_mainfile(filepath='D:/indie_game_dev/3D Models/tongtongtongsahur_walk.blend')
scene=bpy.context.scene
rig=bpy.data.objects['Sahur_Walk_Rig'];rig.data.pose_position='REST'
obj=bpy.data.objects['Sahur_Character']
scene.render.engine='BLENDER_WORKBENCH'
scene.render.resolution_x=900;scene.render.resolution_y=900;scene.render.resolution_percentage=100
scene.display.shading.light='STUDIO';scene.display.shading.color_type='TEXTURE'
scene.display.shading.show_shadows=False;scene.display.shading.show_cavity=True
scene.display.shading.background_type='WORLD';scene.world.color=(.09,.09,.09)
cam_data=bpy.data.cameras.new('InspectCamera');cam=bpy.data.objects.new('InspectCamera',cam_data);scene.collection.objects.link(cam);scene.camera=cam
cam_data.type='ORTHO';cam_data.ortho_scale=.62
out='D:/indie_game_dev/unity/Dark Brine_Rot'
for name,pos in [('front',(0,-3,.56)),('side',(3,.04,.56)),('back',(0,3,.56))]:
    target=Vector((.005,.04,.56));cam.location=pos;cam.rotation_euler=(target-cam.location).to_track_quat('-Z','Y').to_euler()
    scene.render.filepath=out+'/sahur_'+name+'.png';bpy.ops.render.render(write_still=True)
with open(out+'/sahur_v2_meshinfo.json','w') as f:
    json.dump({'materials':[m.name for m in obj.data.materials],'bounds':list(obj.dimensions),'bones':{b.name:[list(b.head_local),list(b.tail_local)] for b in rig.data.bones}},f)
