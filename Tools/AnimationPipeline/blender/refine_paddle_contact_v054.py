"""Keep the original UAL rig/support leg; give the power stroke real water depth."""
import bpy,sys,json,math
from pathlib import Path
from mathutils import Vector,Matrix
ROOT=Path(r'D:/indiegameDev/unity/Dark Brine_Rot')
exec((ROOT/'Tools/AnimationPipeline/blender/build_encounter_v006.py').read_text(encoding='utf-8').split("if MODE=='hero':")[0],globals())
OUT=ROOT/'ArtSource/Encounter/v054';OUT.mkdir(parents=True,exist_ok=True)
mode=sys.argv[-1]
if mode=='build':
    bpy.ops.wm.open_mainfile(filepath=str(ROOT/'ArtSource/Encounter/v022/sahur-board-paddle-v022.blend'))
    rig=bpy.data.objects['Armature'];rest={b.name:tuple(b.matrix_local) for b in rig.data.bones};specs=[];audit=[]
    def rotate_world(name,delta):
        p=rig.pose.bones[name];m=p.matrix.copy();p.matrix=Matrix.LocRotScale(m.translation,delta@m.to_quaternion(),m.to_scale());bpy.context.view_layer.update()
    def ankle_to(target):
        h=rig.pose.bones['thigh_r'];k=rig.pose.bones['calf_r'];f=rig.pose.bones['foot_r'];rotation=f.matrix.to_quaternion().copy()
        for _ in range(3):
            at=h.matrix.translation.copy();knee=k.matrix.translation.copy();foot=f.matrix.translation.copy()
            upper=(knee-at).length;lower=(foot-knee).length;axis=(target-at).normalized()
            length=min((target-at).length,upper+lower-.0001)
            bend=knee-at-axis*(knee-at).dot(axis)
            if bend.length<.0001:bend=Vector((0,-1,0))
            bend.normalize();along=(upper*upper+length*length-lower*lower)/(2*max(.001,length))
            wanted=at+axis*along+bend*math.sqrt(max(0,upper*upper-along*along))
            rotate_world('thigh_r',(knee-at).rotation_difference(wanted-at))
            rotate_world('calf_r',(f.matrix.translation-k.matrix.translation).rotation_difference(target-k.matrix.translation))
            m=f.matrix.copy();f.matrix=Matrix.LocRotScale(m.translation,rotation,m.to_scale());bpy.context.view_layer.update()
    for suffix in ('BoardPaddle','BoardPaddleReverse'):
        source=bpy.data.actions['PLAYER_'+suffix+'_v022'];action=author(rig,source,'PLAYER_'+suffix+'_v054',1.2,lambda t:1+t*60,lambda *args:None)
        rig.animation_data.action=action;rows=[]
        for frame in range(1,74):
            bpy.context.scene.frame_set(frame);u=(frame-1)/72
            left=rig.pose.bones['foot_l'].matrix.translation.copy();right=rig.pose.bones['foot_r'].matrix.translation.copy()
            weight=curve(u,[(0,0),(.16,0),(.28,1),(.62,1),(.76,0),(1,0)])
            target=right.copy();target.z+=(min(right.z,left.z-.14)-right.z)*weight
            ankle_to(target)
            rows.append(dict(frame=frame,phase=u,support=list(left),before=list(right),after=list(rig.pose.bones['foot_r'].matrix.translation),weight=weight))
            for name in ('thigh_r','calf_r','foot_r'):
                p=rig.pose.bones[name]
                for channel in ('location','rotation_quaternion','scale'):p.keyframe_insert(data_path=channel,frame=frame,group=name)
        specs.append((action,1.2,True,.408));audit.append(dict(action=action.name,rows=rows))
    assert rest=={b.name:tuple(b.matrix_local) for b in rig.data.bones}
    save(rig,'sahur-paddle-contact-v054',specs,'root','Humanoid','Assets/Game/Prefabs/Characters/Sahur/Animations/Gameplay/QuaterniusUAL2/UAL2_Standard.fbx')
    path=OUT/'sahur-paddle-contact-v054-index.json';index=json.loads(path.read_text(encoding='utf-8'))
    for m in index:m.update(revision='v054',stage='SECOND PASS',design_reference='Documentation/AnimationWorkflow/Actions/CH1_SahurSharkEncounter_v001/design-v039.md',approval_reference='2026-10-12 user continue approved existing controls optimization')
    path.write_text(json.dumps(index,indent=2),encoding='utf-8');(OUT/'source-audit.json').write_text(json.dumps(dict(rest_preserved=True,bones=len(rest),support_channels_unchanged=True,actions=audit),indent=2),encoding='utf-8')
elif mode in ('preview','export'):
    index=json.loads((OUT/'sahur-paddle-contact-v054-index.json').read_text(encoding='utf-8'));bpy.ops.wm.open_mainfile(filepath=index[0]['blend']);rig=bpy.data.objects[index[0]['rig']];reports=[]
    for m in index:
        action=bpy.data.actions[m['action']]
        if mode=='preview':reports.append(render(rig,action,ROOT/'.codex/opening-optimization-20261012/paddle-source-v054'/action.name,[1,13,24,39,53,64,73],slot_for(action,rig),resolution=480))
        else:
            review=OUT/'source-review.json'
            if m['action'] not in json.loads(review.read_text(encoding='utf-8'))['accepted_for_staging']:raise ValueError('Source review required')
            m.update(review_status='accepted_for_stage',review_reference=str(review));reports.append(export(rig,action,m,OUT/(action.name+'.fbx'),slot_for(action,rig)))
    (OUT/(mode+'-report.json')).write_text(json.dumps(reports,indent=2),encoding='utf-8')
else:raise ValueError(mode)
