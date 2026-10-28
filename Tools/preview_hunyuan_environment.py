import bpy,sys,math
from pathlib import Path
from mathutils import Vector
kind=sys.argv[-1];out=Path.cwd()/'ArtSource/Environment/Hunyuan-20261027';bpy.ops.wm.open_mainfile(filepath=str(out/(kind+'-working.blend')))
obj=next(o for o in bpy.data.objects if o.type=='MESH');pts=[obj.matrix_world@Vector(v) for v in obj.bound_box];lo=Vector([min(v[i] for v in pts) for i in range(3)]);hi=Vector([max(v[i] for v in pts) for i in range(3)]);center=(lo+hi)/2
bpy.ops.object.camera_add(location=center+Vector((2.5,-3.5,1.9)));cam=bpy.context.object;cam.rotation_euler=(center-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.type='ORTHO';cam.data.ortho_scale=max(hi-lo)*1.5;bpy.context.scene.camera=cam
scene=bpy.context.scene;scene.render.engine='BLENDER_WORKBENCH';scene.display.shading.color_type='TEXTURE';scene.display.shading.light='STUDIO';scene.render.film_transparent=True;scene.render.resolution_x=640;scene.render.resolution_y=640;scene.render.resolution_percentage=100;scene.render.image_settings.file_format='PNG';scene.render.filepath=str(out/(kind+'-preview.png'));bpy.ops.render.render(write_still=True)
