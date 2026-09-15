import bpy,math,json,os
from mathutils import Vector
scene=bpy.context.scene;rig=bpy.data.objects['Sahur_Walk_Rig'];obj=bpy.data.objects['Sahur_Character']
def loc(pb,world):pb.location=pb.bone.matrix_local.to_3x3().inverted()@Vector(world)
for f in range(1,34):
    scene.frame_set(f);phase=2*math.pi*(f-1)/32
    pb=rig.pose.bones['Body'];loc(pb,(.009*math.sin(phase),0,-.022+.007*math.cos(2*phase)))
    pb.keyframe_insert('location',frame=f)
    for side in ['L','R']:
        pb=rig.pose.bones['Foot_IK.'+side]
        world=pb.bone.matrix_local.to_3x3()@pb.location
        world.y*=.85
        loc(pb,world);pb.keyframe_insert('location',frame=f)
action=rig.animation_data.action
for layer in action.layers:
    for strip in layer.strips:
        for bag in strip.channelbags:
            for fc in bag.fcurves:
                for k in fc.keyframe_points:k.interpolation='LINEAR'
scene.sync_mode='FRAME_DROP'
scene.frame_set(1)
bpy.ops.object.select_all(action='DESELECT')
bpy.ops.mesh.primitive_plane_add(size=4,location=(0,0,-.002))
floor=bpy.context.object;floor.name='Preview_Ground';floor.color=(.12,.14,.17,1)
bpy.ops.object.select_all(action='DESELECT')
rig.select_set(True);obj.select_set(True);bpy.context.view_layer.objects.active=rig
scene.frame_end=33
bpy.ops.export_scene.fbx(filepath='D:/indie_game_dev/3D Models/tongtongtongsahur_walk.fbx',use_selection=True,object_types={'ARMATURE','MESH'},add_leaf_bones=False,use_armature_deform_only=True,bake_anim=True,bake_anim_use_all_actions=False,bake_anim_use_nla_strips=False,bake_anim_force_startend_keying=True,bake_anim_simplify_factor=0.0,path_mode='COPY',embed_textures=True,axis_forward='-Z',axis_up='Y')
scene.frame_end=32
scene.frame_set(1)
rig['Animation']='32-frame in-place walk at 24 fps. Frame 33 repeats frame 1 in FBX.'
rig['Controls']='Foot_IK.L/R and Knee_Pole.L/R control the legs. Body controls sway.'
scene['Walk notes']='Original unified sculpt rigged with topology-based leg weights. Ground is preview only, not exported.'
bpy.ops.file.pack_all()
def finish_view():
    for a in bpy.context.screen.areas:
        if a.type=='CONSOLE':a.type='VIEW_3D'
        if a.type=='VIEW_3D':
            a.spaces.active.shading.color_type='TEXTURE'
            a.spaces.active.shading.show_shadows=True
            a.spaces.active.shading.show_cavity=True
            a.spaces.active.overlay.show_overlays=False
    bpy.ops.wm.save_as_mainfile(filepath='D:/indie_game_dev/3D Models/tongtongtongsahur_walk.blend')
    return None
bpy.app.timers.register(finish_view,first_interval=.5)
print('EXPORTED WALK FBX; SAVING BLEND')
