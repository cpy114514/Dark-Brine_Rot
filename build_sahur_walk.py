import bpy, math
from mathutils import Vector, Quaternion

obj=bpy.data.objects['Mesh_0']
bpy.ops.object.select_all(action='DESELECT')
obj.select_set(True)
bpy.context.view_layer.objects.active=obj
bpy.ops.object.transform_apply(location=False, rotation=True, scale=True)
obj.name='Sahur_Character'
arm=bpy.data.armatures.new('Sahur_Skeleton')
rig=bpy.data.objects.new('Sahur_Walk_Rig',arm)
bpy.context.collection.objects.link(rig)
bpy.context.view_layer.objects.active=rig
obj.select_set(False)
rig.select_set(True)
bpy.ops.object.mode_set(mode='EDIT')
def bone(name,head,tail,parent=None,deform=True):
    b=arm.edit_bones.new(name); b.head=head; b.tail=tail; b.use_deform=deform
    if parent: b.parent=arm.edit_bones[parent]
    return b
bone('Root',(0,0,0),(0,0,.15),deform=False)
bone('Body',(0,.055,.39),(0,.055,.95),'Root')
for side,x in [('L',-.073),('R',.075)]:
    bone('Thigh.'+side,(x,.052,.402),(x,.035,.23),'Body')
    bone('Shin.'+side,(x,.035,.23),(x,.045,.065),'Thigh.'+side)
    bone('Foot.'+side,(x,.045,.065),(x,-.105,.035),'Shin.'+side)
    bone('Foot_IK.'+side,(x,.045,.065),(x,-.105,.035),deform=False)
    bone('Knee_Pole.'+side,(x,-.4,.24),(x,-.4,.31),deform=False)
    sx=-.105 if side=='L' else .13
    bone('Arm.'+side,(sx,.055,.70),(sx*1.1,.09,.54),'Body')
    bone('Hand.'+side,(sx*1.1,.09,.54),(sx,.025,.415),'Arm.'+side)
bpy.ops.object.mode_set(mode='OBJECT')
rig.show_in_front=True
arm.display_type='STICK'
for side in ['L','R']:
    c=rig.pose.bones['Shin.'+side].constraints.new('IK')
    c.target=rig; c.subtarget='Foot_IK.'+side; c.chain_count=2
    c.pole_target=rig; c.pole_subtarget='Knee_Pole.'+side
    c.pole_angle=-math.pi/2
    c=rig.pose.bones['Foot.'+side].constraints.new('COPY_ROTATION')
    c.target=rig; c.subtarget='Foot_IK.'+side
    c.target_space='WORLD'; c.owner_space='WORLD'

groups={b.name:obj.vertex_groups.new(name=b.name) for b in arm.bones if b.use_deform}
def smooth(a,b,t):
    t=max(0,min(1,(t-a)/(b-a))); return t*t*(3-2*t)
for v in obj.data.vertices:
    x,y,z=v.co
    side='L' if x<.002 else 'R'
    # The wooden club projects forward of the legs; keep it with its hand.
    club=z<.40 and y<(-.08 if z>.30 else -.185 if z<.13 else -.12)
    if club:
        weights={'Hand.L':1.0}
    elif z<.40:
        upper=smooth(.20,.28,z)
        foot=1-smooth(.055,.105,z)
        torso=smooth(.355,.41,z)
        weights={'Body':torso,'Thigh.'+side:(1-torso)*(1-foot)*upper,'Shin.'+side:(1-torso)*(1-foot)*(1-upper),'Foot.'+side:(1-torso)*foot}
    else:
        # Blend the narrow side limbs into the cylindrical torso at the shoulder.
        edge=(-x-.09) if side=='L' else (x-.118)
        aw=smooth(0,.026,edge)*(1-smooth(.65,.72,z))*(1-smooth(.13,.18,y))
        hand=1-smooth(.49,.57,z)
        weights={'Body':1-aw,'Arm.'+side:aw*(1-hand),'Hand.'+side:aw*hand}
    for n,w in weights.items():
        if w>1e-6: groups[n].add([v.index],w,'REPLACE')
mod=obj.modifiers.new('Sahur Skin','ARMATURE'); mod.object=rig
obj.parent=rig
scene=bpy.context.scene
scene.render.fps=24; scene.frame_start=1; scene.frame_end=32
def loc(pb,world):
    pb.location=pb.bone.matrix_local.to_3x3().inverted()@Vector(world)
def rot(pb,axis,angle):
    pb.rotation_mode='QUATERNION'
    basis=pb.bone.matrix_local.to_quaternion()
    pb.rotation_quaternion=basis.inverted()@Quaternion(axis,angle)@basis
for f in range(1,34):
    phase=2*math.pi*(f-1)/32
    body=rig.pose.bones['Body']
    loc(body,(.009*math.sin(phase),0,-.014+.008*math.cos(2*phase)))
    rot(body,(0,1,0),.025*math.sin(phase))
    body.keyframe_insert('location',frame=f); body.keyframe_insert('rotation_quaternion',frame=f)
    for side,offset in [('L',0),('R',math.pi)]:
        p=(phase+offset)%(2*math.pi)
        u=p/(2*math.pi)
        # Flat planted stance, followed by a raised returning foot.
        if u<.60:
            t=u/.60; fore=-.085+.17*t; lift=0; pitch=0
        else:
            t=(u-.60)/.40; s=t*t*(3-2*t)
            fore=.085-.17*s; lift=.042*math.sin(math.pi*t)**1.3
            pitch=.12*math.sin(2*math.pi*t)
        foot=rig.pose.bones['Foot_IK.'+side]
        loc(foot,(0,fore,lift)); rot(foot,(1,0,0),pitch)
        foot.keyframe_insert('location',frame=f); foot.keyframe_insert('rotation_quaternion',frame=f)
        a=rig.pose.bones['Arm.'+side]
        rot(a,(1,0,0),(.07 if side=='L' else .16)*math.cos(p))
        a.keyframe_insert('rotation_quaternion',frame=f)
if rig.animation_data and rig.animation_data.action:
    rig.animation_data.action.name='Sahur_Walk_InPlace_24fps'
    rig.animation_data.action.use_fake_user=True
scene.frame_set(1)
for name,frame in [('CONTACT L',1),('PASS',9),('CONTACT R',17),('PASS',25)]:
    scene.timeline_markers.new(name,frame=frame)
for o in bpy.context.scene.objects:
    if o.name in {'Cube','Camera','Light'}: o.hide_set(True); o.hide_render=True
obj.select_set(True)
bpy.context.view_layer.objects.active=rig
def show():
    for screen in bpy.data.screens:
        for a in screen.areas:
            if a.type=='CONSOLE': a.type='VIEW_3D'
            if a.type=='VIEW_3D':
                a.spaces.active.region_3d.view_location=Vector((0,.02,.55))
                a.spaces.active.region_3d.view_distance=1.8
                a.spaces.active.region_3d.view_rotation=Quaternion((.88,.46,.05,.09)).normalized()
                a.spaces.active.shading.color_type='MATERIAL'
    return None
bpy.app.timers.register(show,first_interval=.5)
print('RIG AND 32-FRAME WALK CREATED')
