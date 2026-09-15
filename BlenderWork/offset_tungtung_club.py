import bpy
from mathutils import Vector

source = 'D:/indie_game_dev/3D Models/tungtungtungsahur_separated_working.blend'
output = 'D:/indie_game_dev/unity/Dark Brine_Rot/tungtungtungsahur_club_detached_v2.blend'
preview = 'D:/indie_game_dev/unity/Dark Brine_Rot/tungtungtungsahur_club_detached_v2_preview.png'

bpy.ops.wm.open_mainfile(filepath=source)
scene = bpy.context.scene
club = bpy.data.objects['TungTung_Club']

# Create a deliberate visible gap from the left hand while keeping the club's angle.
club.location += Vector((-.145, -.018, 0.0))
club['separation_gap'] = 'Club moved 14.5 cm outward from hand for clear independent editing.'

cam_data = bpy.data.cameras.new('Detached_Club_Check_Camera')
cam = bpy.data.objects.new('Detached_Club_Check_Camera', cam_data)
scene.collection.objects.link(cam)
scene.camera = cam
target = Vector((0, .03, .55))
cam.location = (1.25, -2.75, 1.30)
cam.rotation_euler = (target - cam.location).to_track_quat('-Z', 'Y').to_euler()
cam_data.type = 'ORTHO'
cam_data.ortho_scale = 1.38
scene.render.engine = 'BLENDER_WORKBENCH'
scene.render.resolution_x = 800
scene.render.resolution_y = 900
scene.render.resolution_percentage = 100
scene.display.shading.light = 'STUDIO'
scene.display.shading.color_type = 'TEXTURE'
scene.display.shading.show_shadows = True
scene.render.filepath = preview
bpy.ops.render.render(write_still=True)
bpy.data.objects.remove(cam, do_unlink=True)
bpy.ops.wm.save_as_mainfile(filepath=output)
