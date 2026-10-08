"""Render Action samples in a temporary scene without changing production cameras."""
import math
import sys
from pathlib import Path
sys.path.insert(0,str(Path(__file__).resolve().parent))
from common import *
from mathutils import Vector

def render(rig,action,output,frames,slot=None,camera_name=None,resolution=640):
    output=Path(output).resolve()
    if output.exists():raise FileExistsError('Preview directory exists; choose a new revision directory')
    output.mkdir(parents=True)
    scene=bpy.data.scenes.new('AnimationPipelinePreview')
    owned=[];camera_data=None;world=None;files=[]
    try:
        scene.render.engine='BLENDER_WORKBENCH';scene.render.resolution_x=resolution;scene.render.resolution_y=resolution
        scene.render.resolution_percentage=100;scene.render.image_settings.file_format='PNG';scene.render.film_transparent=False
        scene.render.fps=bpy.context.scene.render.fps;scene.render.fps_base=bpy.context.scene.render.fps_base
        scene.display.shading.light='STUDIO';scene.display.shading.color_type='MATERIAL'
        scene.display.shading.show_shadows=True;scene.display.shading.show_cavity=True
        scene.display.shading.background_type='WORLD';world=bpy.data.worlds.new('AnimationPipelinePreviewWorld');world.color=(.14,.16,.18);scene.world=world
        scene.collection.objects.link(rig)
        meshes=[o for o in bpy.context.scene.objects if o.type=='MESH' and any(m.type=='ARMATURE' and m.object==rig for m in o.modifiers)]
        if not meshes:raise ValueError('No skinned mesh to preview; supply a visual rig rather than rendering an empty image')
        for mesh in meshes:scene.collection.objects.link(mesh)
        camera_data=bpy.data.cameras.new('AnimationPipelinePreviewCamera');camera=bpy.data.objects.new(camera_data.name,camera_data)
        owned.append(camera);scene.collection.objects.link(camera);scene.camera=camera
        gameplay=bpy.context.scene.objects.get(camera_name) if camera_name else None
        if camera_name and (gameplay is None or gameplay.type!='CAMERA'):raise ValueError('Gameplay camera not found')
        views={'front':Vector((0,-1,.15)),'side':Vector((1,0,.1)),'three-quarter':Vector((.75,-1,.35))}
        if gameplay:views['gameplay']=None
        with scoped_action(rig,action,slot):
            # Frame all sampled motion, including deformed bounds and root travel.
            points=[]
            for frame in frames:
                whole=math.floor(frame);bpy.context.scene.frame_set(whole,subframe=frame-whole)
                depsgraph=bpy.context.evaluated_depsgraph_get()
                for mesh in meshes:
                    evaluated=mesh.evaluated_get(depsgraph)
                    points.extend(evaluated.matrix_world@Vector(corner) for corner in evaluated.bound_box)
            low=Vector(tuple(min(p[i] for p in points) for i in range(3)));high=Vector(tuple(max(p[i] for p in points) for i in range(3)))
            centre=(low+high)*.5;distance=max((high-low).length*1.5,1)
            for name,direction in views.items():
                if direction is None:
                    camera.matrix_world=gameplay.matrix_world.copy();camera_data.type=gameplay.data.type
                    camera_data.lens=gameplay.data.lens;camera_data.ortho_scale=gameplay.data.ortho_scale
                    camera_data.clip_start=gameplay.data.clip_start;camera_data.clip_end=gameplay.data.clip_end
                else:
                    camera_data.type='PERSP';camera_data.lens=50;camera_data.clip_end=max(distance*20,100)
                    camera.location=centre+direction.normalized()*distance
                    camera.rotation_euler=(centre-camera.location).to_track_quat('-Z','Y').to_euler()
                for frame in frames:
                    whole=math.floor(frame)
                    # Shared objects can otherwise retain the source scene's evaluated pose.
                    bpy.context.scene.frame_set(whole,subframe=frame-whole)
                    scene.frame_set(whole,subframe=frame-whole)
                    path=output/f'{name}-{frame:07.2f}.png';scene.render.filepath=str(path)
                    bpy.ops.render.render(write_still=True,scene=scene.name);files.append(str(path))
        return {'action':action.name,'fps':effective_fps(),'frames':frames,'previews':files,
                'gameplay_camera':'rendered' if gameplay else 'undefined: no camera supplied; add a Unity gameplay capture before final review',
                'view_note':'default front assumes Blender -Y; inspect actual model forward direction'}
    finally:
        bpy.data.scenes.remove(scene)
        for obj in owned:bpy.data.objects.remove(obj,do_unlink=True)
        if camera_data:bpy.data.cameras.remove(camera_data)
        if world:bpy.data.worlds.remove(world)

if __name__=='__main__':
    p=parser('Render front/side/three-quarter and optional gameplay-camera previews into a new folder')
    p.add_argument('--directory',required=True);p.add_argument('--frames',help='Comma-separated source frames')
    p.add_argument('--every',type=int,help='Render an entire sequence at this frame interval')
    p.add_argument('--camera',help='Existing gameplay camera to clone');p.add_argument('--resolution',type=int,default=640)
    a=arguments(p);rig=rig_object(a.rig);action=action_object(a.action,rig);start,end=action.frame_range
    if a.frames:frames=[float(x) for x in a.frames.split(',')]
    elif a.every:
        if a.every<1:raise ValueError('--every must be positive')
        frames=list(range(math.ceil(start),math.floor(end)+1,a.every))
    else:frames=[start,(start+end)*.5,end]
    report(render(rig,action,a.directory,frames,slot_for(action,rig,a.slot),a.camera,a.resolution),a.output)
