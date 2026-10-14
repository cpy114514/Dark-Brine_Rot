"""Smooth diagnosed local quaternion spikes and replant retained source feet."""
import bpy,sys,json,math
from pathlib import Path
from mathutils import Vector,Matrix
ROOT=Path(r'D:/indiegameDev/unity/Dark Brine_Rot')
exec((ROOT/'Tools/AnimationPipeline/blender/build_encounter_v006.py').read_text().split("if MODE=='hero':")[0],globals())
OUT=ROOT/'ArtSource/Encounter/v040';OUT.mkdir(parents=True,exist_ok=True)
mode=sys.argv[-1]
if mode=='build':
    bpy.ops.wm.open_mainfile(filepath=str(ROOT/'ArtSource/Encounter/v039/sahur-combat-v039.blend'))
    rig=bpy.data.objects['Armature'];rest={b.name:tuple(b.matrix_local) for b in rig.data.bones};specs=[];report=[]
    def rotate(bone,delta):
        p=rig.pose.bones[bone];m=p.matrix.copy();p.matrix=Matrix.LocRotScale(m.translation,delta@m.to_quaternion(),m.to_scale());bpy.context.view_layer.update()
    def plant(side,target,rotation):
        names=['mixamorig:'+side+n for n in ('UpLeg','Leg','Foot')];h,k,f=[rig.pose.bones[n] for n in names]
        for _ in range(4):
            hip=h.matrix.translation.copy();knee=k.matrix.translation.copy();ankle=f.matrix.translation.copy();upper=(knee-hip).length;lower=(ankle-knee).length
            axis=(target-hip).normalized();distance=max(.001,min((target-hip).length,upper+lower-.001));bend=knee-hip-axis*(knee-hip).dot(axis)
            if bend.length<.001:bend=Vector((0,0,1))
            bend.normalize();along=(upper*upper+distance*distance-lower*lower)/(2*distance)
            wanted=hip+axis*along+bend*math.sqrt(max(0,upper*upper-along*along));rotate(names[0],(knee-hip).rotation_difference(wanted-hip))
            knee=k.matrix.translation.copy();ankle=f.matrix.translation.copy();rotate(names[1],(ankle-knee).rotation_difference(target-knee))
            m=f.matrix.copy();f.matrix=Matrix.LocRotScale(m.translation,rotation,m.to_scale());bpy.context.view_layer.update()
        return (f.matrix.translation-target).length
    filtered=['mixamorig:'+n for n in ('Hips','Spine','Spine1','Spine2','RightShoulder','RightArm','RightForeArm','RightHand','LeftShoulder','LeftArm','LeftForeArm','LeftHand')]
    for suffix,seconds,hit in [('Guard',1.6,0),('Parry',1.1,.484),('Slash',1.4,.63),('Brace',1.3,.572),('Miss',1.4,0)]:
        source=bpy.data.actions['CH1_Sahur'+suffix+'_v039'];poses=[];feet=[];count=round(seconds*60)+1
        for i in range(count):
            poses.append(sample(rig,source,i+1));feet.append([(side,rig.pose.bones['mixamorig:'+side+'Foot'].matrix.translation.copy(),rig.pose.bones['mixamorig:'+side+'Foot'].matrix.to_quaternion().copy()) for side in ('Left','Right')])
        rotations={n:[p[n][1].copy() for p in poses] for n in filtered}
        # Symmetric seven-sample binomial filtering spreads imported wrist
        # discontinuities without delaying the authored contact clock.
        weights=[1,6,15,20,15,6,1]
        for _ in range(3):
            for n in filtered:
                old=rotations[n];new=[]
                for i in range(count):
                    q=None;total=0
                    for offset,w in zip(range(-3,4),weights):
                        value=old[max(0,min(count-1,i+offset))]
                        if q is None:q=value.copy();total=w
                        else:q=q.slerp(value,w/(total+w));total+=w
                    new.append(q.normalized())
                rotations[n]=new
        action=bpy.data.actions.new('CH1_Sahur'+suffix+'_v040');action.use_fake_user=True;rig.animation_data.action=action;worst=0
        for i,p in enumerate(poses):
            bpy.context.scene.frame_set(i+1)
            for bone in rig.pose.bones:
                loc,q,s=p[bone.name];bone.rotation_mode='QUATERNION';bone.location=loc;bone.rotation_quaternion=rotations[bone.name][i] if bone.name in rotations else q;bone.scale=s
            bpy.context.view_layer.update()
            for side,target,q in feet[i]:worst=max(worst,plant(side,target,q))
            for bone in rig.pose.bones:
                for channel in ('location','rotation_quaternion','scale'):bone.keyframe_insert(data_path=channel,frame=i+1,group=bone.name)
        for _,fc in curve_records(action):
            for k in fc.keyframe_points:k.interpolation='LINEAR'
        peak={n.split(':')[-1]:max(math.degrees(a.rotation_difference(b).angle) for a,b in zip(q,q[1:])) for n,q in rotations.items()}
        report.append(dict(action=action.name,peak_degrees_per_frame=peak,foot_replant_error_cm=worst,contact_seconds=hit,source=source.name))
        specs.append((action,seconds,suffix=='Guard',hit))
    assert rest=={b.name:tuple(b.matrix_local) for b in rig.data.bones}
    assert max(r['foot_replant_error_cm'] for r in report)<.05
    save(rig,'sahur-smooth-combat-v040',specs,'mixamorig:Hips','Humanoid','Assets/Game/Prefabs/Characters/Sahur/Animations/Source/StandingMeleeAttackDownward.fbx')
    path=OUT/'sahur-smooth-combat-v040-index.json';index=json.loads(path.read_text())
    for m in index:m.update(revision='v040',design_reference='Documentation/AnimationWorkflow/Actions/CH1_SahurSharkEncounter_v001/design-v039.md',approval_reference='2026-10-12 user 开始实施计划',stage='BLOCKING')
    path.write_text(json.dumps(index,indent=2));(OUT/'source-arc-audit.json').write_text(json.dumps(dict(original_rig_retained=True,actions=report),indent=2))
elif mode=='export':
    index=json.loads((OUT/'sahur-smooth-combat-v040-index.json').read_text());bpy.ops.wm.open_mainfile(filepath=index[0]['blend']);rig=bpy.data.objects[index[0]['rig']];report=[]
    review=OUT/'source-review.json';accepted=json.loads(review.read_text())['accepted_for_staging']
    for m in index:
        if m['action'] not in accepted:continue
        m.update(review_status='accepted_for_stage',review_reference=str(review))
        report.append(export(rig,bpy.data.actions[m['action']],m,OUT/(m['action']+'.fbx'),slot_for(bpy.data.actions[m['action']],rig)))
    (OUT/'export-report.json').write_text(json.dumps(report,indent=2))
else:raise ValueError(mode)
