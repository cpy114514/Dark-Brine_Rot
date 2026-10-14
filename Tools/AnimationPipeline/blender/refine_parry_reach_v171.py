"""Original Sahur rig parry: loaded planted legs and a single right-arm deflection."""
import bpy,sys,json,math
from pathlib import Path
from mathutils import Vector,Quaternion,Matrix
ROOT=Path(r'D:/indiegameDev/unity/Dark Brine_Rot')
exec((ROOT/'Tools/AnimationPipeline/blender/actual_rig_blocking_v047.py').read_text().split("if mode=='build':")[0].replace("range(6)","range(12)"),globals())
OUT=ROOT/'ArtSource/Encounter/v171';OUT.mkdir(parents=True,exist_ok=True)
mode=sys.argv[-1]
if mode=='build':
    bpy.ops.wm.open_mainfile(filepath=str(ROOT/'ArtSource/Encounter/v047/sahur-actual-rig-blocking-v047.blend'))
    rig=bpy.data.objects['Armature'];source=bpy.data.actions['CH1_SahurGuard_v047']
    rest={b.name:tuple(b.matrix_local) for b in rig.data.bones}
    parents={b.name:b.parent.name if b.parent else None for b in rig.data.bones}
    pose=sample(rig,source,1);world={p.name:p.matrix.copy() for p in rig.pose.bones}
    action=bpy.data.actions.new('CH1_SahurParry_v171');action.use_fake_user=True
    rig.animation_data.action=action;support_error=0;arm_error=0;max_reach_clamp=0;previous_quaternions={}
    for i in range(67):
        t=i/60;bpy.context.scene.frame_set(i+1)
        for p in rig.pose.bones:
            p.rotation_mode='QUATERNION';p.location,p.rotation_quaternion,p.scale=pose[p.name]
        bpy.context.view_layer.update()
        load=curve(t,[(0,0),(.25,1),(.484,.8),(.62,.5),(.85,.15),(1.1,0)])
        recoil=curve(t,[(0,0),(.38,0),(.54,1),(.66,.65),(.9,.1),(1.1,0)])
        root=rig.pose.bones['root'];m=root.matrix.copy();m.translation+=Vector((0,0,-3*load-2*recoil));root.matrix=m
        bpy.context.view_layer.update()
        hips=rig.pose.bones['Hips'];m=hips.matrix.copy();m.translation+=Vector((-4*load,1.5*recoil,0));hips.matrix=m
        bpy.context.view_layer.update()
        rotate_world(hips,Quaternion((0,0,1),math.radians(-3*load)))
        rotate_world(rig.pose.bones['Spine'],Quaternion((1,0,0),math.radians(3*load-6*recoil)))
        rotate_world(rig.pose.bones['Spine2'],Quaternion((0,0,1),math.radians(-5*load+4*recoil)))
        rotate_world(rig.pose.bones['Head'],Quaternion((0,0,1),math.radians(3*load-2*recoil)))
        chest_shift=rig.pose.bones['Spine2'].matrix.translation-world['Spine2'].translation
        reach=curve(t,[(0,0),(.20,.2),(.40,.75),(.484,1),(.58,.85),(.76,.3),(1.1,0)])
        sweep=curve(t,[(0,0),(.28,-.2),(.484,.6),(.62,1),(.82,.25),(1.1,0)])
        for side,sign in [('Right',1),('Left',-1)]:
            target=world[side+'Hand'].translation+chest_shift
            target+=Vector((-10*sweep,-24*reach,32*reach-3*recoil)) if side=='Right' else Vector((4*load,4*recoil,-3*load))
            shoulder=rig.pose.bones[side+'Arm'].matrix.translation.copy()
            elbow=rig.pose.bones[side+'ForeArm'].matrix.translation.copy()
            hand_at=rig.pose.bones[side+'Hand'].matrix.translation.copy()
            maximum=(elbow-shoulder).length+(hand_at-elbow).length-.5
            requested=(target-shoulder).length
            if requested>maximum:
                max_reach_clamp=max(max_reach_clamp,requested-maximum)
                target=shoulder+(target-shoulder).normalized()*maximum
            arm_error=max(arm_error,solve([side+n for n in ('Arm','ForeArm','Hand')],target,shoulder+Vector((-sign*20,0,-22))))
            hand=rig.pose.bones[side+'Hand'];hm=hand.matrix.copy()
            rotation=world[side+'Hand'].to_quaternion()
            if side=='Right':rotation=Quaternion((0,0,1),math.radians(-12*sweep))@rotation
            hand.matrix=Matrix.LocRotScale(hm.translation,rotation,hm.to_scale());bpy.context.view_layer.update()
        for side,sign in [('Left',1),('Right',-1)]:
            target=world[side+'Foot'].translation.copy();hips_at=hips.matrix.translation.copy()
            support_error=max(support_error,solve([side+n for n in ('UpLeg','Leg','Foot')],target,hips_at+Vector((sign*23,-45,0))))
            foot=rig.pose.bones[side+'Foot'];fm=foot.matrix.copy()
            foot.matrix=Matrix.LocRotScale(fm.translation,world[side+'Foot'].to_quaternion(),fm.to_scale());bpy.context.view_layer.update()
        for p in rig.pose.bones:
            q=p.rotation_quaternion.copy();q.normalize()
            if p.name in previous_quaternions and q.dot(previous_quaternions[p.name])<0:q.negate()
            p.rotation_quaternion=q;previous_quaternions[p.name]=q.copy()
            for channel in ('location','rotation_quaternion','scale'):p.keyframe_insert(data_path=channel,frame=i+1,group=p.name)
    for _,fc in curve_records(action):
        for key in fc.keyframe_points:key.interpolation='LINEAR'
    assert len(rest)==28 and rest=={b.name:tuple(b.matrix_local) for b in rig.data.bones}
    assert parents=={b.name:b.parent.name if b.parent else None for b in rig.data.bones}
    assert support_error<.02 and arm_error<.05,(support_error,arm_error)
    save(rig,'sahur-open-water-parry-v171',[(action,1.1,False,.484)],'root','Humanoid','Assets/Game/Prefabs/Characters/Sahur/Models/PbrSahur.fbx')
    index_path=OUT/'sahur-open-water-parry-v171-index.json';index=json.loads(index_path.read_text())
    for m in index:m.update(revision='v171',stage='FIRST PASS',design_reference='Documentation/AnimationWorkflow/Actions/CH1_SahurSharkEncounter_v001/design-v039.md',approval_reference='2026-10-13 user 继续制作',review_status='pending')
    index_path.write_text(json.dumps(index,indent=2))
    audit=dict(original_rest_preserved=True,original_mesh_preserved=True,bones=28,parents=parents,source=source.name,action=action.name,
        duration=1.1,fps=60,contact=.484,supported_ankle_error_cm=support_error,hand_error_cm=arm_error,requested_hand_reach_clamp_cm=max_reach_clamp,runtime_activated=False)
    (OUT/'source-audit.json').write_text(json.dumps(audit,indent=2));print(json.dumps(audit))
elif mode in ('preview','export'):
    m=json.loads((OUT/'sahur-open-water-parry-v171-index.json').read_text())[0]
    bpy.ops.wm.open_mainfile(filepath=m['blend']);rig=bpy.data.objects[m['rig']];action=bpy.data.actions[m['action']]
    if mode=='preview':report=render(rig,action,ROOT/'.codex/opening-optimization-20261012/parry-source-v171',[1,14,25,30.04,36,49,67],slot_for(action,rig),resolution=480)
    else:
        review=OUT/'source-review.json'
        if action.name not in json.loads(review.read_text())['accepted_for_staging']:raise ValueError('Source not reviewed')
        m.update(review_status='accepted_for_stage',review_reference=str(review))
        report=export(rig,action,m,OUT/(action.name+'.fbx'),slot_for(action,rig))
    (OUT/(mode+'-report.json')).write_text(json.dumps(report,indent=2))
else:raise ValueError(mode)
