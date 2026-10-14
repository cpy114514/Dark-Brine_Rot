"""Weight and overlap pass on the retained Sahur rig; never replace the bind."""
import bpy,sys,json,math
from pathlib import Path
from mathutils import Vector,Quaternion,Matrix
ROOT=Path(r'D:/indiegameDev/unity/Dark Brine_Rot')
exec((ROOT/'Tools/AnimationPipeline/blender/actual_rig_blocking_v047.py').read_text().split("if mode=='build':")[0].replace("range(6)","range(12)"),globals())
OUT=ROOT/'ArtSource/Encounter/v085';OUT.mkdir(parents=True,exist_ok=True)
mode=sys.argv[-1]
if mode=='build':
    bpy.ops.wm.open_mainfile(filepath=str(ROOT/'ArtSource/Encounter/v047/sahur-actual-rig-blocking-v047.blend'))
    rig=bpy.data.objects['Armature'];source=bpy.data.actions['CH1_SahurBoardHop_v047']
    rest={b.name:tuple(b.matrix_local) for b in rig.data.bones}
    parents={b.name:b.parent.name if b.parent else None for b in rig.data.bones}
    poses=[];world=[]
    for i in range(112):
        poses.append(sample(rig,source,i+1))
        world.append({p.name:p.matrix.copy() for p in rig.pose.bones})
    action=bpy.data.actions.new('CH1_SahurBoardHop_v085');action.use_fake_user=True
    rig.animation_data.action=action;support_error=0;max_tuck=0
    for i,pose in enumerate(poses):
        t=i/60;bpy.context.scene.frame_set(i+1)
        for p in rig.pose.bones:
            p.rotation_mode='QUATERNION';p.location,p.rotation_quaternion,p.scale=pose[p.name]
        bpy.context.view_layer.update()
        # Load the trailing/right leg, then extend through takeoff. The
        # original sole targets stay fixed during anticipation and landing.
        lateral=curve(t,[(0,0),(.30,-4),(.50,0),(1.33,0),(1.48,3),(1.85,0)])
        vertical=curve(t,[(0,0),(.30,-4),(.49,1),(.66,0),(1.33,0),(1.47,-3),(1.70,-1),(1.85,0)])
        root=rig.pose.bones['root'];m=root.matrix.copy()
        m.translation+=Vector((lateral,0,vertical));root.matrix=m;bpy.context.view_layer.update()
        lean=curve(t,[(0,0),(.32,3),(.50,-2),(.88,-1),(1.33,0),(1.47,4),(1.69,1),(1.85,0)])
        rotate_world(rig.pose.bones['Spine'],Quaternion((1,0,0),math.radians(lean)))
        overlap=curve(t,[(0,0),(.39,2),(.59,-2),(1.33,0),(1.54,2),(1.85,0)])
        rotate_world(rig.pose.bones['Spine2'],Quaternion((1,0,0),math.radians(overlap)))
        rotate_world(rig.pose.bones['Head'],Quaternion((1,0,0),math.radians(-overlap*.7)))
        chest_shift=rig.pose.bones['Spine2'].matrix.translation-world[i]['Spine2'].translation
        for side,sign in [('Right',1),('Left',-1)]:
            target=world[i][side+'Hand'].translation+chest_shift
            if side=='Left':
                balance=curve(t,[(0,0),(.38,5),(.65,-6),(1.15,-3),(1.33,0),(1.51,6),(1.85,0)])
                target+=Vector((balance*.5,balance,-balance*.3))
            shoulder=rig.pose.bones[side+'Arm'].matrix.translation.copy()
            solve([side+n for n in ('Arm','ForeArm','Hand')],target,shoulder+Vector((-sign*20,0,-22)))
            hand=rig.pose.bones[side+'Hand'];hm=hand.matrix.copy()
            hand.matrix=Matrix.LocRotScale(hm.translation,world[i][side+'Hand'].to_quaternion(),hm.to_scale())
            bpy.context.view_layer.update()
        for side,sign in [('Left',1),('Right',-1)]:
            tuck=curve(t,[(0,0),(.50,0),(.80,12 if side=='Left' else 8),(1.08,10 if side=='Left' else 6),(1.33,0),(1.85,0)])
            target=world[i][side+'Foot'].translation+Vector((0,0,tuck));max_tuck=max(max_tuck,tuck)
            hips=rig.pose.bones['Hips'].matrix.translation.copy()
            error=solve([side+n for n in ('UpLeg','Leg','Foot')],target,hips+Vector((sign*23,-45,0)))
            if t<=.50 or t>=1.33:support_error=max(support_error,error)
            foot=rig.pose.bones[side+'Foot'];fm=foot.matrix.copy()
            foot.matrix=Matrix.LocRotScale(fm.translation,world[i][side+'Foot'].to_quaternion(),fm.to_scale())
            bpy.context.view_layer.update()
        for p in rig.pose.bones:
            for channel in ('location','rotation_quaternion','scale'):p.keyframe_insert(data_path=channel,frame=i+1,group=p.name)
    for _,fc in curve_records(action):
        for key in fc.keyframe_points:key.interpolation='LINEAR'
    assert len(rest)==28 and rest=={b.name:tuple(b.matrix_local) for b in rig.data.bones}
    assert parents=={b.name:b.parent.name if b.parent else None for b in rig.data.bones}
    assert support_error<.02,(support_error,'source centimetres')
    save(rig,'sahur-board-hop-v085',[(action,1.85,False,0)],'root','Humanoid','Assets/Game/Prefabs/Characters/Sahur/Models/PbrSahur.fbx')
    index_path=OUT/'sahur-board-hop-v085-index.json';index=json.loads(index_path.read_text())
    for m in index:m.update(revision='v085',stage='FIRST PASS',design_reference='Documentation/AnimationWorkflow/Actions/CH1_SahurSharkEncounter_v001/design-v039.md',approval_reference='2026-10-13 user 继续',review_status='pending')
    index_path.write_text(json.dumps(index,indent=2))
    audit=dict(original_rest_preserved=True,original_mesh_preserved=True,bones=28,parents=parents,source=source.name,action=action.name,
        duration=1.85,fps=60,takeoff=.5,landing=1.33,supported_ankle_error_cm=support_error,added_left_tuck_cm=max_tuck,runtime_activated=False)
    (OUT/'source-audit.json').write_text(json.dumps(audit,indent=2));print(json.dumps(audit))
elif mode in ('preview','export'):
    m=json.loads((OUT/'sahur-board-hop-v085-index.json').read_text())[0]
    bpy.ops.wm.open_mainfile(filepath=m['blend']);rig=bpy.data.objects[m['rig']];action=bpy.data.actions[m['action']]
    if mode=='preview':report=render(rig,action,ROOT/'.codex/opening-optimization-20261012/hop-source-v085',[1,19,31,49,66,81,89,103,112],slot_for(action,rig),resolution=480)
    else:
        review=OUT/'source-review.json'
        if action.name not in json.loads(review.read_text())['accepted_for_staging']:raise ValueError('Source not reviewed')
        m.update(review_status='accepted_for_stage',review_reference=str(review))
        report=export(rig,action,m,OUT/(action.name+'.fbx'),slot_for(action,rig))
    (OUT/(mode+'-report.json')).write_text(json.dumps(report,indent=2))
else:raise ValueError(mode)
