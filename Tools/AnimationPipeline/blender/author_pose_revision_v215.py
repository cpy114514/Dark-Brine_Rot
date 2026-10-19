"""Targeted paired-film pose revision on preserved actual Sahur rig.

This does not edit v214. All support targets are in retained armature centimetres;
no rest bone, scale, mesh or hierarchy changes are used to gain reach.
"""
import bpy, sys, math, json
from pathlib import Path
from mathutils import Vector, Matrix, Quaternion
ROOT=Path(r'D:/indiegameDev/unity/Dark Brine_Rot')
exec((ROOT/'Tools/AnimationPipeline/blender/author_redesign_v214.py').read_text().split('def build_hero():')[0],globals())
OUT=ROOT/'ArtSource/Encounter/v215';OUT.mkdir(parents=True,exist_ok=True)

def set_matrix(p,translation=None,rotation=None):
    m=p.matrix.copy();p.matrix=Matrix.LocRotScale(translation if translation is not None else m.translation,rotation if rotation is not None else m.to_quaternion(),m.to_scale());bpy.context.view_layer.update()

def restore(rig,pose):
    for p in rig.pose.bones:p.rotation_mode='QUATERNION';p.location,p.rotation_quaternion,p.scale=pose[p.name]
    bpy.context.view_layer.update()

def limb(rig,side,kind,target,pole,rotation=None):
    names=[side+n for n in (('Arm','ForeArm','Hand') if kind=='arm' else ('UpLeg','Leg','Foot'))]
    solve(rig,names,Vector(target),Vector(pole))
    if rotation is not None:set_matrix(rig.pose.bones[names[-1]],rotation=rotation)

def basis_rotation(local_axis,world_axis,local_across,world_across):
    a=Vector(local_axis).normalized();c=Vector(local_across);c=(c-a*c.dot(a)).normalized()
    b=Vector(world_axis).normalized();d=Vector(world_across);d=(d-b*d.dot(b)).normalized()
    return (Matrix((d,b.cross(d),b)).transposed()@Matrix((c,a.cross(c),a))).to_quaternion()

def weapon_grip(rig,grip,shaft,amount,separation=14):
    # Native capsule axis expressed in RightHand local metres, converted to cm.
    # Both importers preserve bone local Y as the finger axis: point B is 12.9cm
    # along this axis, matching the retained hand's 13cm bone. Global Y/Z must
    # not be swapped a second time inside this local hand frame.
    near=Vector((-3.6591,12.8643,2.1512));far=Vector((16.7918,58.718,-96.8423))
    rightq=basis_rotation(far-near,shaft,(0,1,0),(1,0,0))
    leftq=basis_rotation((0,0,1),shaft,(0,1,0),(-1,0,0))
    targets={'Right':(Vector(grip)-rightq@near,rightq),'Left':(Vector(grip)+Vector(shaft).normalized()*separation-leftq@Vector((0,9,0)),leftq)}
    chest=rig.pose.bones['Spine2'].matrix.translation
    for side,sign in [('Right',-1),('Left',1)]:
        target,q=targets[side];old=rig.pose.bones[side+'Hand'].matrix.copy()
        limb(rig,side,'arm',old.translation.lerp(target,amount),chest+Vector((sign*60,8,25)),old.to_quaternion().slerp(q,amount))

def pose_support(rig,original,matrices,phase):
    """0 = prone contact; 1 = settled standing. Shared by dodge and rise."""
    # Weight stays low while palms support, then the knees gather under the hips.
    hip_z=curve(phase,[(0,28.5),(.22,31),(.42,35),(.67,49),(1,70.46147)])
    pitch=curve(phase,[(0,88),(.22,80),(.42,63),(.67,36),(1,0)])
    # Hips is connected to root, so its translation is ignored by Blender.
    # Lower the original root vertically instead; root X/Y remains in place.
    root=rig.pose.bones['root'];set_matrix(root,matrices['root'].translation+Vector((0,0,hip_z-matrices['Hips'].translation.z)))
    hips=rig.pose.bones['Hips'];set_matrix(hips,rotation=Quaternion((1,0,0),math.radians(pitch))@matrices['Hips'].to_quaternion())
    # Lift the eyes slightly off the timber; avoid bending the large head into it.
    rotate_world(rig.pose.bones['Head'],Quaternion((1,0,0),math.radians(-10*(1-phase))))
    rotate_world(rig.pose.bones['Neck'],Quaternion((1,0,0),math.radians(-4*(1-phase))))
    for side,sign in [('Right',-1),('Left',1)]:
        # Feet draw under the centre before the body rises, rather than dragging
        # behind a suddenly upright pelvis. Opposite legs move in succession.
        lag=.05 if side=='Right' else 0
        gather=curve(phase-lag,[(0,0),(.12,0),(.43,1),(1,1)])
        source=matrices[side+'Foot'].translation
        foot=Vector((sign*20,curve(gather,[(0,61),(1,source.y)]),14.5*(1-phase)+source.z*phase+12*math.sin(gather*math.pi)))
        footrot=Quaternion((1,0,0),math.radians(-18*(1-gather)))@matrices[side+'Foot'].to_quaternion()
        limb(rig,side,'leg',foot,(sign*24,-20,8+28*phase),footrot)
        # Palm tail touches z=0 while wrist stays at 11cm; no arm stretching.
        planted=Vector((sign*25,-33,5.8))
        hand=matrices[side+'Hand'].translation
        release=curve(phase,[(0,0),(.29,0),(.48,1),(1,1)])
        target=planted.lerp(hand,release)
        # After release the free hands rise with the torso to the standing guard.
        if release>0:
            chest=rig.pose.bones['Spine2'].matrix.translation
            target=planted.lerp(chest+Vector((sign*27,-18,-14)),release)
            target=target.lerp(hand,ease((phase-.7)/.3))
        # A palm-down contact orientation has its short finger axis forward.
        # Mesh audit: this hand's thickness runs along local X, not local Z.
        # Lay the fingers (local Y) flat along the timber; its minimum local X
        # is -5.75cm, so a 5.8cm wrist leaves the palm just above the surface.
        y=Vector((0,-1,0));x=Vector((0,0,1));z=x.cross(y)
        palm=Matrix((x,y,z)).transposed().to_quaternion()
        q=palm.slerp(matrices[side+'Hand'].to_quaternion(),release)
        if side=='Right':
            # Left palm supports the dive; right hand keeps the actual long
            # weapon clear of the board, extended alongside the prone body.
            shaft=Vector((-.12,-1,.02)).normalized()
            held=basis_rotation(Vector((20.4509,45.8537,-98.9935)),shaft,(0,1,0),(1,0,0))
            target=Vector((-28,-30,18)).lerp(target,release)
            q=held.slerp(matrices[side+'Hand'].to_quaternion(),release)
        limb(rig,side,'arm',target,(sign*55,-12,30),q)

def build():
    bpy.ops.wm.open_mainfile(filepath=str(ROOT/'ArtSource/Encounter/v214/sahur-redesign-v214.blend'))
    rig=bpy.data.objects['Armature'];source=bpy.data.actions['CH1_SahurGuard_v047']
    original=sample(rig,source,1);matrices={p.name:p.matrix.copy() for p in rig.pose.bones};rest={b.name:tuple(b.matrix_local) for b in rig.data.bones};specs=[]
    for suffix,seconds,hit in [('Overhead',2.8,1.3),('ProneDodge',2.4,0),('GetUp',2.5,0)]:
        action=source.copy();action.name='CH1_Sahur'+suffix+'_v215';action.use_fake_user=True
        for _,fc in curve_records(action):fc.keyframe_points.clear()
        rig.animation_data.action=action
        for i in range(round(seconds*60)+1):
            t=i/60;bpy.context.scene.frame_set(i+1);restore(rig,original)
            if suffix=='Overhead':
                wind=curve(t,[(0,0),(.25,.2),(.78,1),(1.04,1),(1.3,0),(1.6,-.2),(2.8,0)])
                hitphase=curve(t,[(0,0),(1.04,0),(1.3,1),(1.55,.92),(1.95,.55),(2.8,0)])
                # Shoulder elevation and a bowed head gain real overhead clearance
                # on this long-head/short-arm character without scaling any bone.
                rotate_world(rig.pose.bones['Spine'],Quaternion((1,0,0),math.radians(7*wind+16*hitphase)))
                rotate_world(rig.pose.bones['Spine2'],Quaternion((1,0,0),math.radians(5*wind+13*hitphase)))
                rotate_world(rig.pose.bones['Head'],Quaternion((1,0,0),math.radians(75*max(0,wind)-14*hitphase)))
                for side,sign in [('Right',-1),('Left',1)]:
                    rotate_world(rig.pose.bones[side+'Shoulder'],Quaternion((0,1,0),math.radians(sign*-16*max(0,wind))))
                chest=rig.pose.bones['Spine2'].matrix.translation.copy()
                grip=Vector((0,curve(t,[(0,-25),(.78,6),(1.04,6),(1.3,-27),(1.6,-27),(2.8,-25)]),curve(t,[(0,114),(.78,132),(1.04,132),(1.3,105),(1.6,98),(2.8,114)])))
                for side,sign in [('Right',-1),('Left',1)]:
                    target=grip+Vector((sign*6,sign*1.2,sign*2))
                    limb(rig,side,'arm',target,chest+Vector((sign*65,10,28)))
                shaft=Vector((0,curve(t,[(0,-.8),(.78,.3),(1.04,.3),(1.3,-.95),(1.6,-.9),(2.8,-.8)]),curve(t,[(0,.6),(.78,.95),(1.04,.95),(1.3,-.3),(1.6,-.42),(2.8,.6)]))).normalized()
                contact=curve(t,[(0,0),(.25,1),(2.15,1),(2.8,0)])
                weapon_grip(rig,grip,shaft,contact,14-9*hitphase)
            elif suffix=='ProneDodge':
                phase=curve(t,[(0,1),(.12,.82),(.30,.36),(.48,0),(2.4,0)])
                pose_support(rig,original,matrices,phase)
            else:
                phase=curve(t,[(0,0),(.22,.04),(.65,.22),(1.05,.43),(1.6,.72),(2.25,1),(2.5,1)])
                pose_support(rig,original,matrices,phase)
            if suffix=='Overhead':
                join=curve(t,[(0,0),(.25,1),(2.15,1),(2.8,0)])
                for side in ['Right','Left']:
                    for part in ['Shoulder','Arm','ForeArm','Hand']:
                        p=rig.pose.bones[side+part];loc,q,s=original[p.name]
                        p.rotation_quaternion=q.slerp(p.rotation_quaternion,join)
                bpy.context.view_layer.update()
            for p in rig.pose.bones:
                for channel in ('location','rotation_quaternion','scale'):p.keyframe_insert(data_path=channel,frame=i+1,group=p.name)
        for _,fc in curve_records(action):
            for k in fc.keyframe_points:k.interpolation='LINEAR'
        specs.append((action,seconds,False,hit))
    assert rest=={b.name:tuple(b.matrix_local) for b in rig.data.bones}
    save(rig,'sahur-pose-v215',specs,'root','Humanoid','Assets/Game/Prefabs/Characters/Sahur/Models/PbrSahur.fbx')
    path=OUT/'sahur-pose-v215-index.json';index=json.loads(path.read_text())
    for m in index:m.update(revision='v215',stage='BLOCKING',design_reference='Documentation/AnimationWorkflow/Actions/CH1_SahurSharkEncounter_v001/redesign-20261017.md',approval_reference='2026-10-17 user: 可以，那开始做吧')
    path.write_text(json.dumps(index,ensure_ascii=False,indent=2),encoding='utf-8')
    return index

def refine_existing():
    """Apply the final support arc and guard joins to the already versioned copy."""
    path=OUT/'sahur-pose-v215.blend';bpy.ops.wm.open_mainfile(filepath=str(path));rig=bpy.data.objects['Armature']
    source=bpy.data.actions['CH1_SahurGuard_v047'];original=sample(rig,source,1);matrices={p.name:p.matrix.copy() for p in rig.pose.bones}
    for suffix,seconds in [('Overhead',2.8),('ProneDodge',2.4),('GetUp',2.5)]:
        action=bpy.data.actions['CH1_Sahur'+suffix+'_v215'];rig.animation_data.action=action
        # Cache the original authored pose before changing curves at existing keys.
        poses=[sample(rig,action,i+1) for i in range(round(seconds*60)+1)]
        for i,pose in enumerate(poses):
            t=i/60;bpy.context.scene.frame_set(i+1);restore(rig,pose if suffix=='Overhead' else original)
            if suffix=='Overhead':
                join=curve(t,[(0,0),(.25,1),(2.15,1),(2.8,0)])
                for side in ['Right','Left']:
                    for part in ['Shoulder','Arm','ForeArm','Hand']:
                        p=rig.pose.bones[side+part];p.rotation_quaternion=original[p.name][1].slerp(p.rotation_quaternion,join)
                bpy.context.view_layer.update()
            else:
                phase=curve(t,[(0,1),(.12,.82),(.30,.36),(.48,0),(2.4,0)]) if suffix=='ProneDodge' else curve(t,[(0,0),(.22,.04),(.65,.22),(1.05,.43),(1.6,.72),(2.25,1),(2.5,1)])
                pose_support(rig,original,matrices,phase)
            for p in rig.pose.bones:
                for channel in ('location','rotation_quaternion','scale'):p.keyframe_insert(data_path=channel,frame=i+1,group=p.name)
    bpy.context.preferences.filepaths.save_version=0;bpy.ops.wm.save_as_mainfile(filepath=str(path))

def finalize_contacts():
    """Targeted pass after mesh/proxy review; keep all inspected lower-body arcs."""
    path=OUT/'sahur-pose-v215.blend';bpy.ops.wm.open_mainfile(filepath=str(path));rig=bpy.data.objects['Armature']
    for suffix,seconds in [('Overhead',2.8),('ProneDodge',2.4),('GetUp',2.5)]:
        action=bpy.data.actions['CH1_Sahur'+suffix+'_v215'];poses=[sample(rig,action,i+1) for i in range(round(seconds*60)+1)];rig.animation_data.action=action
        for i,pose in enumerate(poses):
            t=i/60;bpy.context.scene.frame_set(i+1);restore(rig,pose)
            if suffix=='Overhead':
                weight=curve(t,[(0,0),(.25,1),(2.15,1),(2.8,0)])
                if weight>0:
                    hitphase=curve(t,[(0,0),(1.04,0),(1.3,1),(1.55,.92),(1.95,.55),(2.8,0)])
                    grip=Vector((0,curve(t,[(0,-25),(.78,6),(1.04,6),(1.3,-27),(1.6,-27),(2.8,-25)]),curve(t,[(0,114),(.78,132),(1.04,132),(1.3,105),(1.6,98),(2.8,114)])))
                    shaft=Vector((0,curve(t,[(0,-.8),(.78,.3),(1.04,.3),(1.3,-.95),(1.6,-.9),(2.8,-.8)]),curve(t,[(0,.6),(.78,.95),(1.04,.95),(1.3,-.3),(1.6,-.42),(2.8,.6)]))).normalized()
                    weapon_grip(rig,grip,shaft,weight,14-9*hitphase)
                names=[s+p for s in ['Right','Left'] for p in ['Arm','ForeArm','Hand']]
            else:
                phase=curve(t,[(0,1),(.12,.82),(.30,.36),(.48,0),(2.4,0)]) if suffix=='ProneDodge' else curve(t,[(0,0),(.22,.04),(.65,.22),(1.05,.43),(1.6,.72),(2.25,1),(2.5,1)])
                rotate_world(rig.pose.bones['Neck'],Quaternion((1,0,0),math.radians(-4*(1-phase))));names=['Neck']
            for name in names:rig.pose.bones[name].keyframe_insert(data_path='rotation_quaternion',frame=i+1,group=name)
    bpy.context.preferences.filepaths.save_version=0;bpy.ops.wm.save_as_mainfile(filepath=str(path))

def previews(revision='preview01'):
    index=json.loads((OUT/'sahur-pose-v215-index.json').read_text());bpy.ops.wm.open_mainfile(filepath=index[0]['blend']);rig=bpy.data.objects[index[0]['rig']];paths=[]
    for m in index:
        frames=[1]+list(range(13,m['frame_end'],12))+[m['frame_end']]
        window=bpy.context.window_manager.windows[0];area=next(a for a in window.screen.areas if a.type=='VIEW_3D')
        with bpy.context.temp_override(window=window,area=area):paths.append(render(rig,bpy.data.actions[m['action']],OUT/revision/m['action'],frames,slot_for(bpy.data.actions[m['action']],rig),resolution=320))
    return paths

def preview_contact_props(rig):
    """Disposable native-capsule proxy and a z=0 support reference, not exports."""
    near=Vector((-3.6591,12.8643,2.1512));far=Vector((16.7918,58.718,-96.8423));axis=(far-near).normalized()
    x=axis.cross(Vector((1,0,0))).normalized();y=axis.cross(x);verts=[]
    rest=rig.data.bones['RightHand'].matrix_local
    for end in [near,far]:
        for n in range(16):verts.append(rest@(end+(x*math.cos(n*math.tau/16)+y*math.sin(n*math.tau/16))*4.5))
    faces=[tuple(range(15,-1,-1)),tuple(range(16,32))]+[(n,(n+1)%16,(n+1)%16+16,n+16) for n in range(16)]
    mesh=bpy.data.meshes.new('PreviewNativeCapsule');mesh.from_pydata(verts,[],faces);mesh.update();proxy=bpy.data.objects.new('PreviewNativeCapsule',mesh);bpy.context.scene.collection.objects.link(proxy);proxy.matrix_world=rig.matrix_world.copy()
    mat=bpy.data.materials.new('PreviewStickBrown');mat.diffuse_color=(.32,.11,.025,1);mesh.materials.append(mat)
    group=proxy.vertex_groups.new(name='RightHand');group.add(list(range(32)),1,'REPLACE');mod=proxy.modifiers.new('PreviewBinding','ARMATURE');mod.object=rig
    floorMesh=bpy.data.meshes.new('PreviewSupportPlane');floorMesh.from_pydata([(-3,-3,0),(3,-3,0),(3,3,0),(-3,3,0)],[],[(0,1,2,3)]);floor=bpy.data.objects.new('PreviewSupportPlane',floorMesh);bpy.context.scene.collection.objects.link(floor)
    return proxy,floor

def reviewed_previews(revision='preview03'):
    index=json.loads((OUT/'sahur-pose-v215-index.json').read_text());bpy.ops.wm.open_mainfile(filepath=index[0]['blend']);rig=bpy.data.objects[index[0]['rig']]
    proxy,floor=preview_contact_props(rig)
    # Reuse the preserved helper in memory; the temporary support plane does not
    # participate in camera framing and is never written into the working blend.
    source=(ROOT/'Tools/AnimationPipeline/blender/render_animation_preview.py').read_text()
    source=source.split("if __name__=='__main__':")[0].replace('for mesh in meshes:scene.collection.objects.link(mesh)','for mesh in meshes:scene.collection.objects.link(mesh)\n        scene.collection.objects.link(bpy.data.objects["PreviewSupportPlane"])')
    env={'__file__':str(ROOT/'Tools/AnimationPipeline/blender/render_animation_preview.py')};exec(source,env)
    paths=[];window=bpy.context.window_manager.windows[0];area=next(a for a in window.screen.areas if a.type=='VIEW_3D')
    try:
        with bpy.context.temp_override(window=window,area=area):
            for m in index:
                a=bpy.data.actions[m['action']];frames=[1]+list(range(7,m['frame_end'],6))+[m['frame_end']]
                paths.append(env['render'](rig,a,OUT/revision/m['action'],frames,slot_for(a,rig),resolution=320))
    finally:
        bpy.data.objects.remove(proxy,do_unlink=True);bpy.data.objects.remove(floor,do_unlink=True)
    (OUT/(revision+'-index.json')).write_text(json.dumps(paths,indent=2))
    return paths
