"""Replace weak defense silhouettes on the retained rig; original Actions stay intact."""
import bpy,sys,json,math
from pathlib import Path
from mathutils import Vector,Matrix
ROOT=Path(r'D:/indiegameDev/unity/Dark Brine_Rot')
exec((ROOT/'Tools/AnimationPipeline/blender/build_encounter_v006.py').read_text().split("if MODE=='hero':")[0],globals())
OUT=ROOT/'ArtSource/Encounter/v042';OUT.mkdir(parents=True,exist_ok=True)
mode=sys.argv[-1]
def rotate_world(p,delta):
    m=p.matrix.copy();p.matrix=Matrix.LocRotScale(m.translation,delta@m.to_quaternion(),m.to_scale());bpy.context.view_layer.update()
def solve(names,target,rotation,pole=None):
    h,k,f=[rig.pose.bones[n] for n in names]
    for _ in range(3):
        hip=h.matrix.translation.copy();knee=k.matrix.translation.copy();end=f.matrix.translation.copy()
        upper=(knee-hip).length;lower=(end-knee).length;axis=(target-hip).normalized();d=min((target-hip).length,upper+lower-.01)
        bend=(pole-hip if pole is not None else knee-hip);bend-=axis*bend.dot(axis)
        if bend.length<.001:bend=Vector((0,0,1))
        bend.normalize();along=(upper*upper+d*d-lower*lower)/(2*max(.001,d));wanted=hip+axis*along+bend*math.sqrt(max(0,upper*upper-along*along))
        rotate_world(h,(knee-hip).rotation_difference(wanted-hip));knee=k.matrix.translation.copy();end=f.matrix.translation.copy()
        rotate_world(k,(end-knee).rotation_difference(target-knee));m=f.matrix.copy();f.matrix=Matrix.LocRotScale(m.translation,rotation,m.to_scale());bpy.context.view_layer.update()
    return (f.matrix.translation-target).length
if mode=='build':
    bpy.ops.wm.open_mainfile(filepath=str(ROOT/'ArtSource/Encounter/v040/sahur-smooth-combat-v040.blend'))
    rig=bpy.data.objects['Armature'];rest={b.name:tuple(b.matrix_local) for b in rig.data.bones};specs=[];report=[]
    sample(rig,bpy.data.actions['CH1_SahurParry_v040'],30)
    guard_wrist=rig.pose.bones['mixamorig:RightHand'].matrix.to_quaternion().copy()
    for suffix,seconds,hit in [('Guard',1.6,0),('Brace',1.3,.572)]:
        source=bpy.data.actions['CH1_Sahur'+suffix+'_v040'];poses=[];feet=[]
        for i in range(round(seconds*60)+1):
            poses.append(sample(rig,source,i+1));feet.append([(s,rig.pose.bones['mixamorig:'+s+'Foot'].matrix.translation.copy(),rig.pose.bones['mixamorig:'+s+'Foot'].matrix.to_quaternion().copy()) for s in ['Left','Right']])
        action=bpy.data.actions.new('CH1_Sahur'+suffix+'_v042');action.use_fake_user=True;rig.animation_data.action=action;worst=0;armerror=0
        for i,p in enumerate(poses):
            t=i/60;bpy.context.scene.frame_set(i+1)
            for bone in rig.pose.bones:
                bone.rotation_mode='QUATERNION';bone.location,bone.rotation_quaternion,bone.scale=p[bone.name]
            bpy.context.view_layer.update()
            # Source Mixamo is centimetres, Y-up, facing -X. Derive the body
            # basis from retained shoulders rather than changing the bind axes.
            left=rig.pose.bones['mixamorig:LeftArm'].matrix.translation;right=rig.pose.bones['mixamorig:RightArm'].matrix.translation
            lateral=(right-left).normalized();up=Vector((0,1,0));forward=lateral.cross(up).normalized()
            impact=curve(t,[(0,0),(.38,0),(.572,1),(.71,.7),(1.04,.12),(1.3,0)]) if suffix=='Brace' else .12*math.sin(t/seconds*math.tau)
            hips=rig.pose.bones['mixamorig:Hips'];m=hips.matrix.copy();m.translation-=up*(3+impact*2.5);hips.matrix=m;bpy.context.view_layer.update()
            chest=(rig.pose.bones['mixamorig:Spine2'].matrix.translation+rig.pose.bones['mixamorig:Neck'].matrix.translation)*.5
            right_target=chest+forward*(34-impact*9)+lateral*25+up*(2+impact*3)
            shoulder=rig.pose.bones['mixamorig:RightArm'].matrix.translation.copy();pole=shoulder+lateral*14+forward*6-up*22
            armerror=max(armerror,solve(['mixamorig:Right'+n for n in ['Arm','ForeArm','Hand']],right_target,guard_wrist,pole))
            left_target=chest+forward*(13-impact*5)-lateral*31-up*(25-impact*3)
            wrist=rig.pose.bones['mixamorig:LeftHand'].matrix.to_quaternion().copy();shoulder=rig.pose.bones['mixamorig:LeftArm'].matrix.translation.copy()
            solve(['mixamorig:Left'+n for n in ['Arm','ForeArm','Hand']],left_target,wrist,shoulder-lateral*18-up*25)
            for side,target,q in feet[i]:worst=max(worst,solve(['mixamorig:'+side+n for n in ['UpLeg','Leg','Foot']],target,q))
            for bone in rig.pose.bones:
                for c in ['location','rotation_quaternion','scale']:bone.keyframe_insert(data_path=c,frame=i+1,group=bone.name)
        for _,fc in curve_records(action):
            for k in fc.keyframe_points:k.interpolation='LINEAR'
        specs.append((action,seconds,suffix=='Guard',hit));report.append(dict(action=action.name,source=source.name,foot_replant_error_cm=worst,arm_target_error_cm=armerror))
    assert rest=={b.name:tuple(b.matrix_local) for b in rig.data.bones}
    save(rig,'sahur-defense-v042',specs,'mixamorig:Hips','Humanoid','Assets/Game/Prefabs/Characters/Sahur/Animations/Source/StandingMeleeAttackDownward.fbx')
    path=OUT/'sahur-defense-v042-index.json';index=json.loads(path.read_text())
    for m in index:m.update(revision='v042',stage='BLOCKING',design_reference='Documentation/AnimationWorkflow/Actions/CH1_SahurSharkEncounter_v001/design-v039.md',approval_reference='2026-10-12 user 开始实施计划')
    path.write_text(json.dumps(index,indent=2));(OUT/'defense-audit.json').write_text(json.dumps(dict(rest_retained=True,actions=report),indent=2))
elif mode in ('export','preview'):
    index=json.loads((OUT/'sahur-defense-v042-index.json').read_text());bpy.ops.wm.open_mainfile(filepath=index[0]['blend']);rig=bpy.data.objects[index[0]['rig']];report=[]
    for m in index:
        if mode=='preview':
            report.append(render(rig,bpy.data.actions[m['action']],ROOT/'.codex/opening-optimization-20261012/defense-source-v042'/m['action'],[1,18,35,48,m['frame_end']],slot_for(bpy.data.actions[m['action']],rig),resolution=384));continue
        review=OUT/'source-review.json'
        if m['action'] not in json.loads(review.read_text())['accepted_for_staging']:continue
        m.update(review_status='accepted_for_stage',review_reference=str(review),stage='BLOCKING');report.append(export(rig,bpy.data.actions[m['action']],m,OUT/(m['action']+'.fbx'),slot_for(bpy.data.actions[m['action']],rig)))
    (OUT/(mode+'-report.json')).write_text(json.dumps(report,indent=2))
else:raise ValueError(mode)
