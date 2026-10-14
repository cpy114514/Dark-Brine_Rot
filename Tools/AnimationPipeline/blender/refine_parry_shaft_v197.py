"""Turn the held shaft out over water, retaining the existing loaded stance."""
import bpy,sys,json,math
from pathlib import Path
from mathutils import Vector,Quaternion,Matrix
ROOT=Path(r'D:/indiegameDev/unity/Dark Brine_Rot')
exec((ROOT/'Tools/AnimationPipeline/blender/actual_rig_blocking_v047.py').read_text().split("if mode=='build':")[0],globals())
OUT=ROOT/'ArtSource/Encounter/v197';OUT.mkdir(parents=True,exist_ok=True)
group='sahur-forward-parry-v197';mode=sys.argv[-1]
if mode=='build':
    bpy.ops.wm.open_mainfile(filepath=str(ROOT/'ArtSource/Encounter/v171/sahur-open-water-parry-v171.blend'))
    rig=bpy.data.objects['Armature'];source=bpy.data.actions['CH1_SahurParry_v171']
    rest={b.name:tuple(b.matrix_local) for b in rig.data.bones}
    original=[sample(rig,source,i+1) for i in range(67)]
    action=bpy.data.actions.new('CH1_SahurParry_v197');action.use_fake_user=True;rig.animation_data.action=action
    previous={};hand_error=0;foot_error=0
    for i,pose in enumerate(original):
        t=i/60;bpy.context.scene.frame_set(i+1)
        for bone in rig.pose.bones:
            bone.rotation_mode='QUATERNION';bone.location,bone.rotation_quaternion,bone.scale=pose[bone.name]
        bpy.context.view_layer.update()
        hands=rig.pose.bones['RightHand'];forearm=rig.pose.bones['RightForeArm']
        saved_hand=hands.matrix.copy();feet={n:rig.pose.bones[n].matrix.translation.copy() for n in ('LeftFoot','RightFoot')}
        weight=curve(t,[(0,0),(.14,.1),(.30,.65),(.484,1),(.57,.9),(.72,.35),(.95,0),(1.1,0)])
        # Forearm rotation about its own shaft keeps the elbow/wrist endpoints
        # while sharing grip orientation with the wrist. The original hips,
        # loaded knees, torso retreat and free left arm remain authored v171.
        axis=(saved_hand.translation-forearm.matrix.translation).normalized()
        rotate_world(forearm,Quaternion(axis,math.radians(12*weight)))
        hands.matrix=Matrix.LocRotScale(saved_hand.translation,
            Quaternion((0,0,1),math.radians(40*weight))@Quaternion((1,0,0),math.radians(20*weight))@saved_hand.to_quaternion(),saved_hand.to_scale())
        bpy.context.view_layer.update()
        hand_error=max(hand_error,(hands.matrix.translation-saved_hand.translation).length)
        foot_error=max(foot_error,max((rig.pose.bones[n].matrix.translation-p).length for n,p in feet.items()))
        for bone in rig.pose.bones:
            q=bone.rotation_quaternion.copy();q.normalize()
            if bone.name in previous and q.dot(previous[bone.name])<0:q.negate()
            bone.rotation_quaternion=q;previous[bone.name]=q.copy()
            for channel in ('location','rotation_quaternion','scale'):bone.keyframe_insert(data_path=channel,frame=i+1,group=bone.name)
    for _,fc in curve_records(action):
        for key in fc.keyframe_points:key.interpolation='LINEAR'
    revised=[sample(rig,action,i+1) for i in range(67)]
    changed={'RightForeArm','RightHand'}
    unchanged=max(math.degrees(a[n][1].rotation_difference(b[n][1]).angle)
        for a,b in zip(original,revised) for n in rest if n not in changed)
    peaks={n:max(math.degrees(a[n][1].rotation_difference(b[n][1]).angle)
        for a,b in zip(revised,revised[1:])) for n in changed}
    original_peaks={n:max(math.degrees(a[n][1].rotation_difference(b[n][1]).angle)
        for a,b in zip(original,original[1:])) for n in changed}
    assert len(rest)==28 and rest=={b.name:tuple(b.matrix_local) for b in rig.data.bones}
    assert hand_error<.002 and foot_error<.0001 and unchanged<.001,(hand_error,foot_error,unchanged)
    assert all(peaks[n]<=max(10,original_peaks[n]*1.05) for n in changed),(original_peaks,peaks)
    save(rig,group,[(action,1.1,False,.484)],'root','Humanoid','Assets/Game/Prefabs/Characters/Sahur/Models/PbrSahur.fbx')
    index_path=OUT/(group+'-index.json');index=json.loads(index_path.read_text())
    for entry in index:entry.update(revision='v197',stage='FIRST PASS',
        design_reference='Documentation/AnimationWorkflow/Actions/CH1_SahurSharkEncounter_v001/design-v039.md',
        approval_reference='2026-10-14 authorized animation continuation',review_status='pending')
    index_path.write_text(json.dumps(index,indent=2))
    audit=dict(original_rest_preserved=True,bones=28,original_skin_preserved=True,source=source.name,action=action.name,
        changed_channels=sorted(changed),unchanged_rotation_error_degrees=unchanged,
        hand_position_error_source_cm=hand_error,foot_position_error_source_cm=foot_error,
        peak_degrees_per_frame=peaks,original_peak_degrees_per_frame=original_peaks,
        contact_seconds=.484,duration=1.1,fps=60,runtime_activated=False)
    (OUT/'source-audit.json').write_text(json.dumps(audit,indent=2));print(json.dumps(audit))
elif mode in ('preview','export'):
    entry=json.loads((OUT/(group+'-index.json')).read_text())[0]
    bpy.ops.wm.open_mainfile(filepath=entry['blend']);rig=bpy.data.objects[entry['rig']];action=bpy.data.actions[entry['action']]
    if mode=='preview':
        report=render(rig,action,ROOT/'.codex/animation-continuation-20261014/parry-source-v197',
            [1,10,19,25,30.04,35,44,58,67],slot_for(action,rig),resolution=480)
    else:
        review=OUT/'source-review.json'
        if action.name not in json.loads(review.read_text())['accepted_for_staging']:raise ValueError('Source review required')
        entry.update(review_status='accepted_for_stage',review_reference=str(review))
        report=export(rig,action,entry,OUT/(action.name+'.fbx'),slot_for(action,rig))
    (OUT/(mode+'-report.json')).write_text(json.dumps(report,indent=2))
else:raise ValueError(mode)
