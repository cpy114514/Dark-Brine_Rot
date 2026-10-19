"""Opening redesign. Versioned poses on retained rigs; preview/export separately."""
import bpy, sys, json, math
from pathlib import Path
from mathutils import Vector, Matrix, Quaternion
ROOT=Path(r'D:/indiegameDev/unity/Dark Brine_Rot')
exec((ROOT/'Tools/AnimationPipeline/blender/build_encounter_v006.py').read_text().split("if MODE=='hero':")[0],globals())
OUT=ROOT/'ArtSource/Encounter/v214'; OUT.mkdir(parents=True,exist_ok=True)

def rotate_world(p,q):
    m=p.matrix.copy();p.matrix=Matrix.LocRotScale(m.translation,q@m.to_quaternion(),m.to_scale());bpy.context.view_layer.update()

def solve(rig,names,target,pole):
    h,k,f=[rig.pose.bones[n] for n in names]
    for _ in range(4):
        a=h.matrix.translation.copy();b=k.matrix.translation.copy();c=f.matrix.translation.copy()
        upper=(b-a).length;lower=(c-b).length;axis=(target-a).normalized()
        d=min((target-a).length,upper+lower-.01);bend=pole-a;bend-=axis*bend.dot(axis)
        if bend.length<.001:bend=Vector((0,-1,0))
        bend.normalize();along=(upper*upper+d*d-lower*lower)/(2*max(d,.001))
        wanted=a+axis*along+bend*math.sqrt(max(0,upper*upper-along*along))
        rotate_world(h,(b-a).rotation_difference(wanted-a))
        b=k.matrix.translation.copy();c=f.matrix.translation.copy();rotate_world(k,(c-b).rotation_difference(target-b))

def build_hero():
    bpy.ops.wm.open_mainfile(filepath=str(ROOT/'ArtSource/Encounter/v047/sahur-actual-rig-blocking-v047.blend'))
    rig=bpy.data.objects['Armature'];source=bpy.data.actions['CH1_SahurGuard_v047']
    original=sample(rig,source,1);rest={b.name:tuple(b.matrix_local) for b in rig.data.bones};specs=[]
    for suffix,seconds,hit in [('Overhead',2.8,1.3),('ProneDodge',2.4,0),('GetUp',2.5,0),('Stow',1.2,0),('Retrieve',1.2,0),('AirHit',2.2,0)]:
        action=source.copy();action.name='CH1_Sahur'+suffix+'_v214';action.use_fake_user=True
        # Keep a duplicated Action identity/slot, replacing only this copy's keys.
        for _,fc in curve_records(action):fc.keyframe_points.clear()
        rig.animation_data.action=action
        for i in range(round(seconds*60)+1):
            t=i/60;u=t/seconds;bpy.context.scene.frame_set(i+1)
            for p in rig.pose.bones:p.rotation_mode='QUATERNION';p.location,p.rotation_quaternion,p.scale=original[p.name]
            bpy.context.view_layer.update()
            if suffix=='Overhead':
                lift=curve(t,[(0,0),(.75,1),(1.06,1),(1.3,0),(1.58,-.15),(2.8,0)])
                strike=curve(t,[(0,0),(1.06,0),(1.3,1),(1.6,.8),(2.8,0)])
                rotate_world(rig.pose.bones['Spine'],Quaternion((1,0,0),math.radians(-10*lift+18*strike)))
                rotate_world(rig.pose.bones['Spine2'],Quaternion((1,0,0),math.radians(-6*lift+10*strike)))
                chest=rig.pose.bones['Spine2'].matrix.translation.copy()
                grip=chest+Vector((0,-38+35*lift-32*strike,18+80*lift-30*strike))
                for side,sign in [('Right',-1),('Left',1)]:
                    target=grip+Vector((sign*5,0,sign*5))
                    solve(rig,[side+n for n in ['Arm','ForeArm','Hand']],target,chest+Vector((sign*65,-12,25)))
            elif suffix in ('ProneDodge','GetUp','AirHit'):
                if suffix=='ProneDodge':bend=curve(t,[(0,0),(.18,.3),(.42,1),(1.5,1),(2.4,1)])
                elif suffix=='GetUp':bend=curve(t,[(0,1),(.65,.9),(1.3,.5),(2.2,0),(2.5,0)])
                else:bend=curve(t,[(0,0),(.12,-.2),(.45,-.6),(1.4,-.9),(2.2,-1)])
                rotate_world(rig.pose.bones['Hips'],Quaternion((1,0,0),math.radians(78*bend)))
                rotate_world(rig.pose.bones['Spine2'],Quaternion((1,0,0),math.radians(8*bend)))
                for side,sign in [('Right',-1),('Left',1)]:
                    rotate_world(rig.pose.bones[side+'Arm'],Quaternion((0,1,0),math.radians(sign*18*abs(bend))))
                    rotate_world(rig.pose.bones[side+'Leg'],Quaternion((1,0,0),math.radians(18*abs(bend))))
            else:
                reach=curve(u,[(0,0),(.3,.4),(.65,1),(1,1)]) if suffix=='Stow' else curve(u,[(0,1),(.3,1),(.7,.4),(1,0)])
                chest=rig.pose.bones['Spine2'].matrix.translation.copy()
                hand=rig.pose.bones['RightHand'].matrix.translation.copy()
                target=hand.lerp(chest+Vector((-20,22,35)),reach)
                solve(rig,['RightArm','RightForeArm','RightHand'],target,chest+Vector((-65,0,25)))
                rotate_world(rig.pose.bones['Head'],Quaternion((0,0,1),math.radians(-12*reach)))
            for p in rig.pose.bones:
                for channel in ('location','rotation_quaternion','scale'):p.keyframe_insert(data_path=channel,frame=i+1,group=p.name)
        for _,fc in curve_records(action):
            for key in fc.keyframe_points:key.interpolation='LINEAR'
        specs.append((action,seconds,False,hit))
    assert rest=={b.name:tuple(b.matrix_local) for b in rig.data.bones}
    save(rig,'sahur-redesign-v214',specs,'root','Humanoid','Assets/Game/Prefabs/Characters/Sahur/Models/PbrSahur.fbx')
    fix_index('sahur-redesign-v214')

def fix_index(group):
    path=OUT/(group+'-index.json');data=json.loads(path.read_text())
    for m in data:m.update(revision='v214',stage='BLOCKING',design_reference='Documentation/AnimationWorkflow/Actions/CH1_SahurSharkEncounter_v001/redesign-20261017.md',approval_reference='2026-10-17 user: 可以，那开始做吧')
    path.write_text(json.dumps(data,ensure_ascii=False,indent=2),encoding='utf-8')

def build_shark():
    bpy.ops.wm.open_mainfile(filepath=str(ROOT/'ArtSource/Encounter/v097/shark-tail-inertia-v097.blend'))
    rig=bpy.data.objects['Tralalero_Rig'];source=bpy.data.actions['Swim_Cruise'];specs=[]
    for suffix,seconds in [('LeapCrush',3.6),('HeadShake',1.5),('FeintTail',2.8)]:
        def modify(t,pose,rot):
            u=t/seconds
            if suffix=='LeapCrush':
                arch=curve(u,[(0,0),(.2,-12),(.45,-5),(.65,12),(.8,18),(1,0)])
                rot('Spine_Mid',x=arch*.45);rot('Spine_Rear',x=arch*.65)
                rot('Head',x=-arch*.3);rot('Jaw',x=curve(u,[(0,0),(.4,12),(.7,8),(1,0)]))
                rot('Fin_Left',y=-arch*.6);rot('Fin_Right',y=arch*.6)
            elif suffix=='HeadShake':
                shake=math.sin(u*math.pi*5)*math.sin(u*math.pi)
                rot('Head',z=shake*23);rot('Spine_Mid',z=math.sin(u*math.pi*5-.4)*math.sin(u*math.pi)*7)
                rot('Jaw',x=math.sin(u*math.pi)*8)
            else:
                coil=curve(t,[(0,0),(.65,-1),(1.25,-1),(1.58,1),(1.9,.55),(2.8,0)])
                for bone,amplitude,delay in [('Spine_Mid',16,0),('Spine_Rear',24,.04),('Tail_Base',34,.08),('Tail_Mid',42,.12),('Tail_Tip',48,.16)]:
                    value=curve(t-delay,[(0,0),(.65,-1),(1.25,-1),(1.58,1),(1.9,.55),(2.8,0)])
                    rot(bone,z=value*amplitude)
                rot('Head',z=-coil*10);rot('Jaw',x=max(0,-coil)*16)
        action=author(rig,source,'CH1_Shark'+suffix+'_v214',seconds,lambda t:1,modify)
        specs.append((action,seconds,False,2.52 if suffix=='LeapCrush' else 1.58 if suffix=='FeintTail' else 0))
    save(rig,'shark-redesign-v214',specs,'Body','Generic','');fix_index('shark-redesign-v214')

def preview(group,revision='preview03'):
    index=json.loads((OUT/(group+'-index.json')).read_text(encoding='utf-8'))
    bpy.ops.wm.open_mainfile(filepath=index[0]['blend']);rig=bpy.data.objects[index[0]['rig']]
    paths=[]
    for m in index:
        action=bpy.data.actions[m['action']]
        frames=[1]+list(range(13,m['frame_end'],12))+[m['frame_end']]
        window=bpy.context.window_manager.windows[0]
        area=next(a for a in window.screen.areas if a.type=='VIEW_3D')
        with bpy.context.temp_override(window=window,area=area):
            paths.append(render(rig,action,OUT/revision/m['action'],frames,slot_for(action,rig),resolution=320))
    return paths

def export_group(group):
    index=json.loads((OUT/(group+'-index.json')).read_text(encoding='utf-8'))
    review=OUT/(group+'-review.json');accepted=json.loads(review.read_text())['accepted_for_staging']
    bpy.ops.wm.open_mainfile(filepath=index[0]['blend']);rig=bpy.data.objects[index[0]['rig']];results=[]
    for m in index:
        if m['action'] not in accepted:raise ValueError('Missing stage review: '+m['action'])
        m.update(review_status='accepted_for_stage',review_reference=str(review))
        action=bpy.data.actions[m['action']];results.append(export(rig,action,m,OUT/(m['action']+'.fbx'),slot_for(action,rig)))
    return results
