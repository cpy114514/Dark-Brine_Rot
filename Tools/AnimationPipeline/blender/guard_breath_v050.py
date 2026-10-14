"""Targeted living guard on retained v047 rig; preview before staged export."""
import bpy,sys,json,math
from pathlib import Path
from mathutils import Quaternion
ROOT=Path(r'D:/indiegameDev/unity/Dark Brine_Rot')
sys.path.insert(0,str(ROOT/'Tools/AnimationPipeline/blender'))
from common import curve_records,slot_for
from render_animation_preview import render
from export_unity_fbx import export
OUT=ROOT/'ArtSource/Encounter/v050';OUT.mkdir(parents=True,exist_ok=True)
mode=sys.argv[-1]
source=json.loads((ROOT/'ArtSource/Encounter/v047/sahur-actual-rig-blocking-v047-index.json').read_text(encoding='utf-8'))[0]
blend=OUT/'sahur-guard-breath-v050.blend'
name='CH1_SahurGuard_v050'
if mode=='build':
    if blend.exists():raise FileExistsError(blend)
    bpy.ops.wm.open_mainfile(filepath=source['blend']);rig=bpy.data.objects[source['rig']]
    rest={b.name:tuple(b.matrix_local) for b in rig.data.bones}
    original=bpy.data.actions[source['action']];rig.animation_data.action=original
    rig.animation_data.action_slot=slot_for(original,rig);bpy.context.scene.frame_set(1)
    base={p.name:(p.location.copy(),p.rotation_quaternion.copy(),p.scale.copy()) for p in rig.pose.bones}
    feet={side:rig.pose.bones[side+'Foot'].matrix.translation.copy() for side in ['Left','Right']}
    action=original.copy();action.name=name;action.use_fake_user=True
    rig.animation_data.action=action;rig.animation_data.action_slot=slot_for(action,rig)
    max_error=0;rotations={}
    for frame in range(97):
        bpy.context.scene.frame_set(frame+1);phase=frame/96*math.tau
        breath=(1-math.cos(phase))*.5;balance=math.sin(phase)
        changes={'Spine':(.65*breath,0,.35*balance),'Spine2':(.9*breath,0,-.15*balance),
                 'Head':(-.55*breath,0,-.2*balance),'RightForeArm':(.25*breath,0,0),
                 'RightHand':(0,.35*balance,0),'LeftArm':(.3*breath,0,.5*balance)}
        for bone,(x,y,z) in changes.items():
            p=rig.pose.bones[bone];p.location,p.rotation_quaternion,p.scale=base[bone]
            p.rotation_quaternion=base[bone][1]@Quaternion((1,0,0),math.radians(x))@Quaternion((0,1,0),math.radians(y))@Quaternion((0,0,1),math.radians(z))
            p.keyframe_insert(data_path='rotation_quaternion',frame=frame+1,group=bone)
        bpy.context.view_layer.update()
        max_error=max(max_error,*[(rig.pose.bones[s+'Foot'].matrix.translation-feet[s]).length for s in feet])
        rotations[frame]={n:tuple(rig.pose.bones[n].rotation_quaternion) for n in changes}
    for _,fc in curve_records(action):
        for k in fc.keyframe_points:k.interpolation='LINEAR'
    assert rest=={b.name:tuple(b.matrix_local) for b in rig.data.bones}
    seam=max(Quaternion(rotations[0][n]).rotation_difference(Quaternion(rotations[96][n])).angle for n in rotations[0])
    bpy.context.scene.render.fps=60;bpy.context.scene.frame_start=1;bpy.context.scene.frame_end=97
    bpy.context.preferences.filepaths.save_version=0;bpy.ops.wm.save_as_mainfile(filepath=str(blend))
    manifest=dict(source,blend=str(blend),action=name,unity_clip=name,revision='v050',stage='FIRST PASS',review_status='pending')
    (OUT/'sahur-guard-breath-v050-index.json').write_text(json.dumps([manifest],indent=2),encoding='utf-8')
    (OUT/'source-audit.json').write_text(json.dumps(dict(rest_retained=True,bones=len(rest),source_foot_error_cm=max_error,loop_seam_radians=seam,changed_bones=list(rotations[0]),root_and_legs_retained=True),indent=2),encoding='utf-8')
elif mode in ['preview','export']:
    m=json.loads((OUT/'sahur-guard-breath-v050-index.json').read_text(encoding='utf-8'))[0]
    bpy.ops.wm.open_mainfile(filepath=str(blend));rig=bpy.data.objects[m['rig']];action=bpy.data.actions[name]
    if mode=='preview':
        report=render(rig,action,ROOT/'.codex/opening-optimization-20261012/guard-v050',frames=[1,25,49,73,97],slot=slot_for(action,rig),resolution=480)
        (OUT/'preview-report.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
    else:
        review=json.loads((OUT/'source-review.json').read_text(encoding='utf-8'))
        if name not in review['accepted_for_staging']:raise RuntimeError('Review required')
        m.update(review_status='accepted_for_stage',review_reference=str(OUT/'source-review.json'))
        result=export(rig,action,m,OUT/(name+'.fbx'),slot_for(action,rig))
        (OUT/'export-report.json').write_text(json.dumps(result,indent=2),encoding='utf-8')
else:raise ValueError(mode)
