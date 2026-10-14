"""Blocking directly on the user's retained PbrSahur rig and skin, no bind rebuild."""
import bpy,sys,json,math
from pathlib import Path
from mathutils import Vector,Matrix,Quaternion
ROOT=Path(r'D:/indiegameDev/unity/Dark Brine_Rot')
exec((ROOT/'Tools/AnimationPipeline/blender/build_encounter_v006.py').read_text().split("if MODE=='hero':")[0],globals())
OUT=ROOT/'ArtSource/Encounter/v047';OUT.mkdir(parents=True,exist_ok=True)
mode=sys.argv[-1]
def rotate_world(p,q):
    m=p.matrix.copy();p.matrix=Matrix.LocRotScale(m.translation,q@m.to_quaternion(),m.to_scale());bpy.context.view_layer.update()
def solve(names,target,pole):
    h,k,f=[rig.pose.bones[n] for n in names]
    for _ in range(6):
        a=h.matrix.translation.copy();b=k.matrix.translation.copy();c=f.matrix.translation.copy();upper=(b-a).length;lower=(c-b).length
        axis=(target-a).normalized();d=min((target-a).length,upper+lower-.01);bend=pole-a; bend-=axis*bend.dot(axis)
        if bend.length<.001:bend=Vector((0,-1,0))
        bend.normalize();along=(upper*upper+d*d-lower*lower)/(2*max(d,.001));wanted=a+axis*along+bend*math.sqrt(max(0,upper*upper-along*along))
        rotate_world(h,(b-a).rotation_difference(wanted-a));b=k.matrix.translation.copy();c=f.matrix.translation.copy();rotate_world(k,(c-b).rotation_difference(target-b))
    return (f.matrix.translation-target).length
if mode=='build':
    bpy.ops.import_scene.fbx(filepath=str(ROOT/'Assets/Game/Prefabs/Characters/Sahur/Models/PbrSahur.fbx'))
    rig=next(o for o in bpy.context.scene.objects if o.type=='ARMATURE');rest={b.name:tuple(b.matrix_local) for b in rig.data.bones}
    assert len(rest)==28
    original={p.name:(p.location.copy(),p.rotation_quaternion.copy() if p.rotation_mode=='QUATERNION' else p.rotation_euler.to_quaternion(),p.scale.copy()) for p in rig.pose.bones}
    root_base=rig.pose.bones['root'].matrix.translation.copy();hips_base=rig.pose.bones['Hips'].matrix.translation.copy();foot_base={side:rig.pose.bones[side+'Foot'].matrix.translation.copy() for side in ['Left','Right']}
    specs=[];audits=[]
    for suffix,seconds,hit in [('Guard',1.6,0),('Brace',1.3,.572),('BoardHop',1.85,0)]:
        action=bpy.data.actions.new('CH1_Sahur'+suffix+'_v047');action.use_fake_user=True;rig.animation_data_create();rig.animation_data.action=action;foot_error=0
        for frame in range(round(seconds*60)+1):
            t=frame/60;bpy.context.scene.frame_set(frame+1)
            for p in rig.pose.bones:p.rotation_mode='QUATERNION';p.location,p.rotation_quaternion,p.scale=original[p.name]
            bpy.context.view_layer.update()
            impact=curve(t,[(0,0),(.32,0),(.572,1),(.72,.72),(1.04,.12),(1.3,0)]) if suffix=='Brace' else 0
            compression=curve(t,[(0,8),(.24,15),(.5,5),(.85,0),(1.25,3),(1.43,17),(1.65,11),(1.85,8)]) if suffix=='BoardHop' else 9+impact*5
            tuck=curve(t,[(0,0),(.5,0),(.75,20),(1.05,24),(1.33,0),(1.85,0)]) if suffix=='BoardHop' else 0
            root=rig.pose.bones['root'];m=root.matrix.copy();m.translation=root_base+Vector((0,0,-compression));root.matrix=m;bpy.context.view_layer.update()
            lean=curve(t,[(0,6),(.28,13),(.5,5),(.9,1),(1.33,8),(1.45,16),(1.85,6)]) if suffix=='BoardHop' else 7-impact*12
            rotate_world(rig.pose.bones['Spine'],Quaternion((1,0,0),math.radians(lean)))
            rotate_world(rig.pose.bones['Spine2'],Quaternion((1,0,0),math.radians(-impact*6)))
            chest=rig.pose.bones['Spine2'].matrix.translation.copy();up=Vector((0,0,1));forward=Vector((0,-1,0));right=Vector((-1,0,0))
            free=curve(t,[(0,0),(.3,-5),(.65,8),(1.1,6),(1.43,-6),(1.85,0)]) if suffix=='BoardHop' else impact*4
            for side,sign in [('Right',1),('Left',-1)]:
                shoulder=rig.pose.bones[side+'Arm'].matrix.translation.copy()
                target=chest+right*(sign*(24 if side=='Right' else 38))+forward*(28-impact*7 if side=='Right' else 8+free)+up*(10+impact*2 if side=='Right' else -15)
                solve([side+n for n in ['Arm','ForeArm','Hand']],target,shoulder+right*(sign*20)+up*(-22))
            for side,sign in [('Left',1),('Right',-1)]:
                target=foot_base[side].copy();target.x=sign*20;target.y+=-10 if side=='Left' else 9;target.z+=tuck*(1 if side=='Left' else .7)
                pole=hips_base+Vector((sign*23,-45,0));error=solve([side+n for n in ['UpLeg','Leg','Foot']],target,pole)
                if tuck<.01:foot_error=max(foot_error,error)
                foot=rig.pose.bones[side+'Foot'];m=foot.matrix.copy();foot.matrix=Matrix.LocRotScale(m.translation,rig.data.bones[side+'Foot'].matrix_local.to_quaternion(),m.to_scale());bpy.context.view_layer.update()
            for p in rig.pose.bones:
                for channel in ['location','rotation_quaternion','scale']:p.keyframe_insert(data_path=channel,frame=frame+1,group=p.name)
        for _,fc in curve_records(action):
            for k in fc.keyframe_points:k.interpolation='LINEAR'
        audits.append(dict(action=action.name,source='PbrSahur original 28-bone rig and mesh',supported_ankle_error_cm=foot_error));specs.append((action,seconds,suffix=='Guard',hit))
    assert rest=={b.name:tuple(b.matrix_local) for b in rig.data.bones}
    save(rig,'sahur-actual-rig-blocking-v047',specs,'root','Humanoid','Assets/Game/Prefabs/Characters/Sahur/Models/PbrSahur.fbx')
    path=OUT/'sahur-actual-rig-blocking-v047-index.json';index=json.loads(path.read_text())
    for m in index:m.update(revision='v047',stage='BLOCKING',design_reference='Documentation/AnimationWorkflow/Actions/CH1_SahurSharkEncounter_v001/design-v039.md',approval_reference='2026-10-12 user 开始实施计划')
    path.write_text(json.dumps(index,indent=2));(OUT/'actual-rig-audit.json').write_text(json.dumps(dict(rest_retained=True,bones=28,actions=audits),indent=2))
elif mode=='preview':
    index=json.loads((OUT/'sahur-actual-rig-blocking-v047-index.json').read_text());bpy.ops.wm.open_mainfile(filepath=index[0]['blend']);rig=bpy.data.objects[index[0]['rig']];report=[]
    for m in index:
        frames=[1,16,31,52,76,m['frame_end']] if 'Hop' in m['action'] else [1,18,35,48,m['frame_end']]
        report.append(render(rig,bpy.data.actions[m['action']],ROOT/'.codex/opening-optimization-20261012/actual-rig-v047'/m['action'],frames,slot_for(bpy.data.actions[m['action']],rig),resolution=480))
    (OUT/'preview-report.json').write_text(json.dumps(report,indent=2))
elif mode=='export':
    index=json.loads((OUT/'sahur-actual-rig-blocking-v047-index.json').read_text());bpy.ops.wm.open_mainfile(filepath=index[0]['blend']);rig=bpy.data.objects[index[0]['rig']];report=[]
    review=OUT/'source-review.json';accepted=json.loads(review.read_text())['accepted_for_staging']
    for m in index:
        if m['action'] not in accepted:continue
        m.update(review_status='accepted_for_stage',review_reference=str(review));report.append(export(rig,bpy.data.actions[m['action']],m,OUT/(m['action']+'.fbx'),slot_for(bpy.data.actions[m['action']],rig)))
    (OUT/'export-report.json').write_text(json.dumps(report,indent=2))
else:raise ValueError(mode)
