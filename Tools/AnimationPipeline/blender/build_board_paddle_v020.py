"""Single supporting leg and a real water stroke, on the retained UAL2 rig."""
import bpy,sys,json,math
from pathlib import Path
from mathutils import Vector,Matrix
ROOT=Path(r'D:/indiegameDev/unity/Dark Brine_Rot')
exec((ROOT/'Tools/AnimationPipeline/blender/build_encounter_v006.py').read_text().split("if MODE=='hero':")[0],globals())
OUT=ROOT/'ArtSource/Encounter/v020';OUT.mkdir(parents=True,exist_ok=True)
mode=sys.argv[-1]
SOURCE=ROOT/'ArtSource/Encounter/v006/sahur-board-v006.blend'
if mode=='build':
    bpy.ops.wm.open_mainfile(filepath=str(SOURCE))
    rig=bpy.data.objects['Armature'];source=bpy.data.actions['PLAYER_BoardBalance_v006'];specs=[]
    before={b.name:(list(b.head_local),list(b.tail_local),b.parent.name if b.parent else None) for b in rig.data.bones}
    rig.animation_data.action=source;bpy.context.scene.frame_set(1)
    support=rig.pose.bones['foot_l'].matrix.translation.copy()
    def rotate_world(bone,delta):
        p=rig.pose.bones[bone];m=p.matrix.copy()
        p.matrix=Matrix.LocRotScale(m.translation,delta@m.to_quaternion(),m.to_scale())
        bpy.context.view_layer.update()
    def plant():
        h=rig.pose.bones['thigh_l'];k=rig.pose.bones['calf_l'];f=rig.pose.bones['foot_l']
        ankleRotation=f.matrix.to_quaternion().copy()
        for _ in range(2):
            at=h.matrix.translation.copy();knee=k.matrix.translation.copy();ankle=f.matrix.translation.copy()
            upper=(knee-at).length;lower=(ankle-knee).length;direction=(support-at).normalized()
            distance=min((support-at).length,upper+lower-.0001)
            bend=knee-at-direction*(knee-at).dot(direction)
            if bend.length<.0001:bend=Vector((0,-1,0))
            bend.normalize();along=(upper*upper+distance*distance-lower*lower)/(2*max(distance,.001))
            wantedKnee=at+direction*along+bend*math.sqrt(max(0,upper*upper-along*along))
            rotate_world('thigh_l',(knee-at).rotation_difference(wantedKnee-at))
            knee=k.matrix.translation.copy();ankle=f.matrix.translation.copy()
            rotate_world('calf_l',(ankle-knee).rotation_difference(support-knee))
            m=f.matrix.copy();f.matrix=Matrix.LocRotScale(m.translation,ankleRotation,m.to_scale());bpy.context.view_layer.update()
    for suffix,direction in [('BoardPaddle',1),('BoardPaddleReverse',-1)]:
        def modify(t,pose,rot):
            u=t/1.2
            swing=curve(u,[(0,-13),(.16,-30),(.28,-26),(.62,32),(.73,34),(.87,6),(1,-13)])
            bend=curve(u,[(0,16),(.16,29),(.28,-10),(.62,-14),(.73,30),(.87,28),(1,16)])
            load=curve(u,[(0,0),(.22,0),(.34,1),(.61,.9),(.72,0),(1,0)])
            rot('thigh_r',x=swing*direction,z=26)
            rot('calf_r',x=bend)
            rot('foot_r',x=curve(u,[(0,2),(.28,-17),(.62,-9),(.78,12),(1,2)]))
            rot('pelvis',z=-3,x=-2-load*2)
            rot('spine_01',x=-3-load*4*direction,z=3)
            rot('spine_02',y=-load*3*direction)
            rot('neck_01',x=load*2,z=-2)
            rot('upperarm_l',z=-9-load*3);rot('lowerarm_l',z=8)
            rot('upperarm_r',z=5);rot('lowerarm_r',z=-4)
        action=author(rig,source,'PLAYER_'+suffix+'_v020',1.2,lambda t:1,modify)
        # Key a real planted supporting leg, keeping the water leg's arc free.
        rig.animation_data.action=action
        for frame in range(1,74):
            bpy.context.scene.frame_set(frame);plant()
            for name in ('thigh_l','calf_l','foot_l'):
                p=rig.pose.bones[name]
                for channel in ('location','rotation_quaternion','scale'):p.keyframe_insert(data_path=channel,frame=frame,group=name)
        specs.append((action,1.2,True,.408))
    if before!={b.name:(list(b.head_local),list(b.tail_local),b.parent.name if b.parent else None) for b in rig.data.bones}:raise RuntimeError('Rest rig changed')
    save(rig,'sahur-board-paddle-v020',specs,'root','Humanoid','Assets/Game/Prefabs/Characters/Sahur/Animations/Gameplay/QuaterniusUAL2/UAL2_Standard.fbx')
    (OUT/'source-audit.json').write_text(json.dumps({'source':str(SOURCE),'original_bones_preserved':len(before),'root_motion':'in_place','supporting_leg':'left','water_stroke_leg':'right','cycle_seconds':1.2},indent=2))
elif mode in ('preview','export'):
    index=json.loads((OUT/'sahur-board-paddle-v020-index.json').read_text());bpy.ops.wm.open_mainfile(filepath=index[0]['blend']);rig=bpy.data.objects[index[0]['rig']]
    reports=[]
    for m in index:
        action=bpy.data.actions[m['action']]
        if mode=='preview':reports.append(render(rig,action,ROOT/'.codex/encounter-v020/previews'/action.name,[1,13,24,39,53,64,73],slot_for(action,rig),resolution=384))
        else:
            review=OUT/'source-review.json';accepted=json.loads(review.read_text())
            if m['action'] not in accepted['accepted_for_staging']:raise ValueError('Source not reviewed')
            m.update(review_status='accepted_for_stage',review_reference=str(review),revision='v020',approval_reference='2026-10-08 user: only small surfboard-shaped planks; use legs to paddle; continue without questions')
            m['contacts']=[dict(bone='foot_l',start=1,end=73,max_drift_m=.005)]
            reports.append(export(rig,action,m,OUT/(action.name+'.fbx'),slot_for(action,rig)))
    (OUT/(mode+'-report.json')).write_text(json.dumps(reports,indent=2))
else:raise ValueError(mode)
