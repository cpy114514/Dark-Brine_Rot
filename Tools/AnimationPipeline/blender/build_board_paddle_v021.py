"""Reachable single-leg water sweep with a loaded, genuinely planted support leg."""
import bpy,sys,json,math
from pathlib import Path
from mathutils import Vector,Matrix
ROOT=Path(r'D:/indiegameDev/unity/Dark Brine_Rot')
exec((ROOT/'Tools/AnimationPipeline/blender/build_encounter_v006.py').read_text().split("if MODE=='hero':")[0],globals())
OUT=ROOT/'ArtSource/Encounter/v021';OUT.mkdir(parents=True,exist_ok=True)
mode=sys.argv[-1]
if mode=='build':
    bpy.ops.wm.open_mainfile(filepath=str(ROOT/'ArtSource/Encounter/v020/sahur-board-paddle-v020.blend'))
    rig=bpy.data.objects['Armature'];specs=[];audit=[]
    rest={b.name:tuple(b.matrix_local) for b in rig.data.bones}
    def rotate_world(name,delta):
        p=rig.pose.bones[name];m=p.matrix.copy()
        p.matrix=Matrix.LocRotScale(m.translation,delta@m.to_quaternion(),m.to_scale())
        bpy.context.view_layer.update()
    def leg(side,target):
        h=rig.pose.bones['thigh_'+side];k=rig.pose.bones['calf_'+side];f=rig.pose.bones['foot_'+side]
        rotation=f.matrix.to_quaternion().copy()
        for _ in range(2):
            at=h.matrix.translation.copy();knee=k.matrix.translation.copy();ankle=f.matrix.translation.copy()
            upper=(knee-at).length;lower=(ankle-knee).length;direction=(target-at).normalized()
            distance=min((target-at).length,upper+lower-.001)
            bend=knee-at-direction*(knee-at).dot(direction)
            if bend.length<.0001:bend=Vector((0,-1,0))
            bend.normalize();along=(upper*upper+distance*distance-lower*lower)/(2*max(distance,.001))
            wantedKnee=at+direction*along+bend*math.sqrt(max(0,upper*upper-along*along))
            rotate_world('thigh_'+side,(knee-at).rotation_difference(wantedKnee-at))
            knee=k.matrix.translation.copy();ankle=f.matrix.translation.copy()
            rotate_world('calf_'+side,(ankle-knee).rotation_difference(target-knee))
            m=f.matrix.copy();f.matrix=Matrix.LocRotScale(m.translation,rotation,m.to_scale());bpy.context.view_layer.update()
    for suffix,direction in [('BoardPaddle',1),('BoardPaddleReverse',-1)]:
        source=bpy.data.actions['PLAYER_'+suffix+'_v020']
        rig.animation_data.action=source;bpy.context.scene.frame_set(1)
        support=rig.pose.bones['foot_l'].matrix.translation.copy()
        action=author(rig,source,'PLAYER_'+suffix+'_v021',1.2,lambda t:1+t*60,lambda t,p,r:None)
        rig.animation_data.action=action;max_support=max_reach=0
        for frame in range(1,74):
            bpy.context.scene.frame_set(frame);u=(frame-1)/72
            load=curve(u,[(0,.75),(.22,.85),(.34,1),(.61,1),(.76,.85),(1,.75)])
            pelvis=rig.pose.bones['pelvis'];m=pelvis.matrix.copy()
            # Lower the actual hips rather than lengthening a leg to reach water.
            m.translation+=Vector((-.06,0,-.18-.025*load));pelvis.matrix=m;bpy.context.view_layer.update()
            target=Vector((-.54,curve(u,[(0,-.02),(.22,-.23),(.34,-.22),(.61,.23),(.72,.24),(.87,.12),(1,-.02)])*direction,
                support.z+curve(u,[(0,.09),(.16,.11),(.30,-.08),(.62,-.08),(.76,.14),(.90,.10),(1,.09)])))
            leg('l',support);leg('r',target)
            max_support=max(max_support,(rig.pose.bones['foot_l'].matrix.translation-support).length)
            max_reach=max(max_reach,(rig.pose.bones['foot_r'].matrix.translation-target).length)
            for name in ('pelvis','thigh_l','calf_l','foot_l','thigh_r','calf_r','foot_r'):
                p=rig.pose.bones[name]
                for channel in ('location','rotation_quaternion','scale'):p.keyframe_insert(data_path=channel,frame=frame,group=name)
        if max_support>.00001 or max_reach>.002:raise RuntimeError('Unreachable authored support/water target')
        specs.append((action,1.2,True,.408));audit.append(dict(action=action.name,max_support_ankle_drift=max_support,max_water_target_error=max_reach))
    if rest!={b.name:tuple(b.matrix_local) for b in rig.data.bones}:raise RuntimeError('Rest rig changed')
    save(rig,'sahur-board-paddle-v021',specs,'root','Humanoid','Assets/Game/Prefabs/Characters/Sahur/Animations/Gameplay/QuaterniusUAL2/UAL2_Standard.fbx')
    (OUT/'source-audit.json').write_text(json.dumps(audit,indent=2))
elif mode in ('preview','export'):
    index=json.loads((OUT/'sahur-board-paddle-v021-index.json').read_text());bpy.ops.wm.open_mainfile(filepath=index[0]['blend']);rig=bpy.data.objects[index[0]['rig']];reports=[]
    for m in index:
        action=bpy.data.actions[m['action']]
        if mode=='preview':reports.append(render(rig,action,ROOT/'.codex/encounter-v021/previews'/action.name,[1,13,24,39,53,64,73],slot_for(action,rig),resolution=384))
        else:
            review=OUT/'source-review.json';accepted=json.loads(review.read_text())
            if m['action'] not in accepted['accepted_for_staging']:raise ValueError('Source not reviewed')
            m.update(review_status='accepted_for_stage',review_reference=str(review),revision='v021',approval_reference='User approved independent production; small real boards and leg paddling; original walking/running/rolling retained')
            m['contacts']=[dict(bone='foot_l',start=1,end=73,max_drift_m=.005)]
            reports.append(export(rig,action,m,OUT/(action.name+'.fbx'),slot_for(action,rig)))
    (OUT/(mode+'-report.json')).write_text(json.dumps(reports,indent=2))
else:raise ValueError(mode)
