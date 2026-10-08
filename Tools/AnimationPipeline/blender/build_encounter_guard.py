import bpy,math,json,sys
from pathlib import Path
from mathutils import Quaternion
ROOT=Path(r'D:/indiegameDev/unity/Dark Brine_Rot');sys.path.insert(0,str(ROOT/'Tools/AnimationPipeline/blender'))
from common import curve_records,slot_for
from export_unity_fbx import export
OUT=ROOT/'ArtSource/Encounter/v002';OUT.mkdir(parents=True,exist_ok=True)
bpy.ops.wm.open_mainfile(filepath=str(ROOT/'ArtSource/Encounter/v001/sahur-encounter-v001.blend'))
rig=next(o for o in bpy.context.scene.objects if o.type=='ARMATURE');source=bpy.data.actions['CH1_SahurDiagonalSlash_v001']
actions=[]
for name,seconds,loop in [('CH1_SahurGuard_v002',2,True),('CH1_SahurBrace_v002',1,False)]:
    samples=[];rig.animation_data.action=source
    for frame in range(round(seconds*60)+1):
        t=frame/60;phase=.34 if loop else .34+.24*(min(1,t/.5)**2*(3-2*min(1,t/.5)))
        source_frame=1+phase*60;whole=math.floor(source_frame);bpy.context.scene.frame_set(whole,subframe=source_frame-whole)
        sample={p.name:(p.location.copy(),p.rotation_quaternion.copy(),p.scale.copy()) for p in rig.pose.bones};samples.append(sample)
    action=bpy.data.actions.new(name);action.use_fake_user=True;rig.animation_data.action=action
    for frame,sample in enumerate(samples):
        t=frame/60;bpy.context.scene.frame_set(frame+1)
        for p in rig.pose.bones:
            loc,q,scale=sample[p.name]
            if loop:
                wave=math.sin(2*math.pi*t/seconds)
                if p.name=='mixamorig:Spine1':q=q@Quaternion((1,0,0),math.radians(wave*1.3))
                if p.name=='mixamorig:Spine2':q=q@Quaternion((0,0,1),math.radians(wave*.8))
                if p.name=='mixamorig:Head':q=q@Quaternion((1,0,0),math.radians(-wave*.65))
            p.rotation_mode='QUATERNION';p.location=loc;p.rotation_quaternion=q;p.scale=scale
            for field in ('location','rotation_quaternion','scale'):p.keyframe_insert(data_path=field,frame=frame+1,group=p.name)
    for _,fc in curve_records(action):
        for k in fc.keyframe_points:k.interpolation='LINEAR'
    actions.append((action,seconds,loop))
bpy.context.scene.render.fps=60;bpy.context.preferences.filepaths.save_version=0
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'sahur-guard-v002.blend'))
reports=[]
for action,seconds,loop in actions:
    m=dict(action=action.name,unity_clip=action.name,revision='v002',frame_start=1,frame_end=round(seconds*60)+1,fps=60,loop=loop,root_motion='in_place',
        root_bone='mixamorig:Hips',rig_type='Humanoid',stage='SECOND PASS',design_status='approved',
        design_reference='Documentation/AnimationWorkflow/Actions/CH1_SahurSharkEncounter_v001/design.md',
        approval_reference='2026-10-07 user: proceed and independently polish without further questions',review_status='accepted_for_stage',
        review_reference='Documentation/AnimationWorkflow/Actions/CH1_SahurSharkEncounter_v001/source-review.md')
    reports.append(export(rig,action,m,OUT/(action.name+'.fbx'),slot_for(action,rig)))
(OUT/'guard-exports.json').write_text(json.dumps(reports,indent=2),encoding='utf-8')
