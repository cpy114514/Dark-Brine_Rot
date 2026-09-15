import bpy
from mathutils import Vector

source = 'D:/indie_game_dev/unity/Dark Brine_Rot/tungtungtungsahur_separated_working.blend'
output = 'D:/indie_game_dev/unity/Dark Brine_Rot/tungtungtungsahur_club_visible_gap.blend'
preview = 'D:/indie_game_dev/unity/Dark Brine_Rot/tungtungtungsahur_club_visible_gap_preview.png'
bpy.ops.wm.open_mainfile(filepath=source)
scene = bpy.context.scene
club = bpy.data.objects['TungTung_Club']
club.location += Vector((-.16, 0, -.015))
club['separation_gap'] = 'Visible gap version: club moved outward from the hand for independent editing.'

cam_data = bpy.data.cameras.new('Visible_Gap_Camera')
cam = bpy.data.objects.new('Visible_Gap_Camera', cam_data)
scene.collection.objects.link(cam)
scene.camera = cam
target = Vector((0, .03, .55))
cam.location = (0, -3, .72)
cam.rotation_euler = (target - cam.location).to_track_quat('-Z', 'Y').to_euler()
cam_data.type = 'ORTHO'
cam_data.ortho_scale = 1.32
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
