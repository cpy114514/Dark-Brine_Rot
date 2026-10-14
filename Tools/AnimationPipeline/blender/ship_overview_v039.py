import bpy,math
from pathlib import Path
from mathutils import Vector
ROOT=Path(r'D:/indiegameDev/unity/Dark Brine_Rot')
bpy.ops.import_scene.fbx(filepath=str(ROOT/'Assets/Game/Prefabs/Environment/Props/Ship/ship_g.fbx'))
meshes=[o for o in bpy.context.selected_objects if o.type=='MESH']
scene=bpy.context.scene;scene.render.engine='BLENDER_WORKBENCH';scene.render.resolution_x=1200;scene.render.resolution_y=800;scene.render.resolution_percentage=100
scene.display.shading.color_type='TEXTURE';scene.display.shading.light='STUDIO';scene.display.shading.show_shadows=True;scene.display.shading.background_type='WORLD';scene.world.color=(.1,.12,.15)
camera_data=bpy.data.cameras.new('AuditCamera');camera=bpy.data.objects.new('AuditCamera',camera_data);scene.collection.objects.link(camera);scene.camera=camera
camera.location=(70,-120,68);camera.rotation_euler=(Vector((0,0,23))-camera.location).to_track_quat('-Z','Y').to_euler();camera.data.type='ORTHO';camera.data.ortho_scale=115;camera.data.clip_end=1000
for obj in list(bpy.data.objects):
    if obj not in meshes and obj!=camera:obj.hide_render=True
scene.render.filepath=str(ROOT/'.codex/opening-optimization-20261012/ship-overview.png');bpy.ops.render.render(write_still=True)
