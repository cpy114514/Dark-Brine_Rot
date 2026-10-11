"""Move the existing descending strike outward with torso loading and real arm IK.

Only a new Action on a retained rig copy is authored. Source rest bones, bind pose,
lower-body animation, original Actions and gameplay attacks remain unchanged.
"""
import bpy,sys,json,math
from pathlib import Path
from mathutils import Vector,Quaternion,Matrix
ROOT=Path(r'D:/indiegameDev/unity/Dark Brine_Rot')
exec((ROOT/'Tools/AnimationPipeline/blender/build_encounter_v006.py').read_text().split("if MODE=='hero':")[0],globals())
OUT=ROOT/'ArtSource/Encounter/v032';OUT.mkdir(parents=True,exist_ok=True)
mode=sys.argv[-1]
if mode=='build':
    bpy.ops.wm.open_mainfile(filepath=str(ROOT/'ArtSource/Encounter/v027/sahur-supported-combat-v027.blend'))
    rig=bpy.data.objects['Armature'];before={b.name:(list(b.head_local),list(b.tail_local),b.parent.name if b.parent else None) for b in rig.data.bones}
    action=bpy.data.actions['CH1_SahurSlash_v027'].copy();action.name='CH1_SahurSlash_v032';action.use_fake_user=True
    rig.animation_data.action=action;rig.animation_data.action_slot=slot_for(action,rig)
    def rotate(name,delta):
        p=rig.pose.bones[name];m=p.matrix.copy();p.matrix=Matrix.LocRotScale(m.translation,delta@m.to_quaternion(),m.to_scale());bpy.context.view_layer.update()
    rows=[];worst=0;lower_error=0
    for frame in range(1,86):
        bpy.context.scene.frame_set(frame);bpy.context.view_layer.update();t=(frame-1)/60
        load=curve(t,[(0,0),(.18,-.12),(.36,.25),(.55,.9),(.63,1),(.79,.8),(1.02,.25),(1.22,0),(1.4,0)])
        lower={n:(rig.pose.bones['mixamorig:'+n].location.copy(),rig.pose.bones['mixamorig:'+n].rotation_quaternion.copy()) for n in ['Hips','LeftUpLeg','LeftLeg','LeftFoot','RightUpLeg','RightLeg','RightFoot']}
        hand=rig.pose.bones['mixamorig:RightHand'];original=hand.matrix.translation.copy();wrist=hand.matrix.to_quaternion().copy()
        # Source armature space is centimetres/Y-up. The chest initiates the
        # outward strike; its three spinal joints distribute the seven-degree
        # loading instead of translating a rigid whole actor across the board.
        for name,degrees in [('Spine',2),('Spine1',2),('Spine2',3)]:
            rotate('mixamorig:'+name,Quaternion((1,0,0),math.radians(degrees*load)))
        target=original+Vector((0,8,18))*load
        upper,fore,hand=[rig.pose.bones['mixamorig:'+n] for n in ['RightArm','RightForeArm','RightHand']]
        origin=upper.matrix.translation.copy();maximum=(fore.matrix.translation-origin).length+(hand.matrix.translation-fore.matrix.translation).length-.2
        requested=target.copy()
        if (target-origin).length>maximum:target=origin+(target-origin).normalized()*maximum
        for _ in range(4):
            shoulder=upper.matrix.translation.copy();elbow=fore.matrix.translation.copy();wrist_at=hand.matrix.translation.copy()
            a=(elbow-shoulder).length;b=(wrist_at-elbow).length;axis=(target-shoulder).normalized();distance=min((target-shoulder).length,a+b-.001)
            bend=elbow-shoulder-axis*(elbow-shoulder).dot(axis)
            if bend.length<.001:bend=Vector((0,0,-1))
            bend.normalize();along=(a*a+distance*distance-b*b)/(2*distance)
            wanted=shoulder+axis*along+bend*math.sqrt(max(0,a*a-along*along))
            rotate(upper.name,(elbow-shoulder).rotation_difference(wanted-shoulder))
            elbow=fore.matrix.translation.copy();wrist_at=hand.matrix.translation.copy()
            rotate(fore.name,(wrist_at-elbow).rotation_difference(target-elbow))
            m=hand.matrix.copy();hand.matrix=Matrix.LocRotScale(m.translation,wrist,m.to_scale());bpy.context.view_layer.update()
        error=(hand.matrix.translation-target).length;worst=max(worst,error)
        for name in ['Spine','Spine1','Spine2','RightArm','RightForeArm','RightHand']:
            p=rig.pose.bones['mixamorig:'+name];p.keyframe_insert(data_path='rotation_quaternion',frame=frame,group=p.name)
        for name,(location,q) in lower.items():
            p=rig.pose.bones['mixamorig:'+name];lower_error=max(lower_error,(p.location-location).length,q.rotation_difference(p.rotation_quaternion).angle)
        rows.append(dict(seconds=t,load=load,original_hand_source_cm=list(original),requested_hand_source_cm=list(requested),actual_hand_source_cm=list(hand.matrix.translation),reach_limited_cm=(target-requested).length,target_error_cm=error))
    assert before=={b.name:(list(b.head_local),list(b.tail_local),b.parent.name if b.parent else None) for b in rig.data.bones}
    assert worst<.1 and lower_error<.00001,(worst,lower_error)
    for _,fc in curve_records(action):
        for key in fc.keyframe_points:key.interpolation='LINEAR'
    save(rig,'sahur-reach-slash-v032',[(action,1.4,False,.63)],'mixamorig:Hips','Humanoid','Assets/Game/Prefabs/Characters/Sahur/Animations/Source/StandingMeleeAttackDownward.fbx')
    path=OUT/'sahur-reach-slash-v032-index.json';index=json.loads(path.read_text())
    for entry in index:entry.update(revision='v032',stage='BLOCKING',review_status='pending_native_review')
    path.write_text(json.dumps(index,indent=2))
    (OUT/'source-pose-audit.json').write_text(json.dumps(dict(original_rest_rig_retained=True,lower_body_channels_unchanged=True,maximum_lower_body_error=lower_error,maximum_hand_target_error_cm=worst,
        one_right_hand=True,seconds=1.4,contact_seconds=.63,source_units='centimetres Y-up',stage='BLOCKING; native retarget and reach review required',frames=rows),indent=2))
elif mode=='export':
    bpy.ops.wm.open_mainfile(filepath=str(OUT/'sahur-reach-slash-v032.blend'));rig=bpy.data.objects['Armature'];reports=[]
    for metadata in json.loads((OUT/'sahur-reach-slash-v032-index.json').read_text()):
        metadata.update(stage='BLOCKING',review_status='accepted_for_stage',review_reference=str(OUT/'source-blocking-review.json'))
        if not Path(metadata['review_reference']).exists():raise ValueError('Source rig/curve audit review required')
        action=bpy.data.actions[metadata['action']];reports.append(export(rig,action,metadata,OUT/(action.name+'.fbx'),slot_for(action,rig)))
    (OUT/'export-report.json').write_text(json.dumps(reports,indent=2))
else:raise ValueError(mode)
