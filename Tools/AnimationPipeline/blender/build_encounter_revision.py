"""Versioned source animation. Never rebuild or overwrite the existing shark rig."""
import bpy, math, json, sys
from pathlib import Path
from mathutils import Quaternion, Vector
ROOT=Path(r'D:/indiegameDev/unity/Dark Brine_Rot')
sys.path.insert(0,str(ROOT/'Tools/AnimationPipeline/blender'))
from common import curve_records, slot_for
from export_unity_fbx import export
OUT=ROOT/'ArtSource/Encounter/v001'
OUT.mkdir(parents=True,exist_ok=True)
MODE=sys.argv[-1]
def smooth(x):
    x=max(0,min(1,x));return x*x*(3-2*x)
def track(t,keys):
    for (a,x),(b,y) in zip(keys,keys[1:]):
        if t<=b:return x+(y-x)*smooth((t-a)/(b-a))
    return keys[-1][1]
def manifest(action,root,kind,duration,contact):
    return dict(action=action.name,unity_clip=action.name,revision='v001',frame_start=1,frame_end=round(duration*60)+1,
        fps=60,loop=False,root_motion='in_place',root_bone=root,rig_type=kind,stage='BLOCKING',
        design_status='approved',design_reference='Documentation/AnimationWorkflow/Actions/CH1_SahurSharkEncounter_v001/design.md',
        approval_reference='User 2026-10-07: 好，开始制作吧; no further questions, autonomous revisions authorized',
        review_status='accepted_for_stage',review_reference='Documentation/AnimationWorkflow/Actions/CH1_SahurSharkEncounter_v001/source-review.md',
        gameplay={'startup':[1,round(contact*60)],'active':[round(contact*60),round(contact*60)+5],
                  'recovery':[round(contact*60)+5,round(duration*60)+1]})
def finish(rig,actions,path,root,kind):
    bpy.context.scene.render.fps=60;bpy.context.scene.render.fps_base=1
    bpy.context.preferences.filepaths.save_version=0
    bpy.ops.wm.save_as_mainfile(filepath=str(OUT/path))
    reports=[]
    for action,duration,contact in actions:
        reports.append(export(rig,action,manifest(action,root,kind,duration,contact),OUT/(action.name+'.fbx'),slot_for(action,rig)))
    (OUT/(MODE+'-exports.json')).write_text(json.dumps(reports,indent=2),encoding='utf-8')
if MODE in ('shark','ship'):
    bpy.ops.wm.open_mainfile(filepath=str(ROOT/'ArtSource/SharkAnimation/tralalero_animated.blend'))
    rig=bpy.data.objects['Tralalero_Rig'];actions=[]
    specs=[('CH1_SharkBite_v001',1.1,.45),('CH1_SharkTail_v001',1.15,.65),('CH1_SharkRecoil_v001',.8,.13)] if MODE=='shark' else [('CH1_ShipTailSmash_v001',1.15,.65)]
    for name,duration,contact in specs:
        action=bpy.data.actions.new(name);rig.animation_data.action=action;action.use_fake_user=True
        for frame in range(round(duration*60)+1):
            t=frame/60;bpy.context.scene.frame_set(frame+1)
            for p in rig.pose.bones:p.rotation_mode='XYZ';p.rotation_euler=(0,0,0);p.location=(0,0,0)
            def rot(bone,x=0,y=0,z=0):rig.pose.bones[bone].rotation_euler=tuple(math.radians(v) for v in (x,y,z))
            if 'Tail' in name:
                power=1.15 if MODE=='ship' else 1
                # Proximal joints lead; the distal tail catches up late and overshoots.
                for i,(bone,amp) in enumerate([('Body',5),('Spine_Mid',9),('Spine_Rear',14),('Tail_Base',21),('Tail_Mid',19),('Tail_Tip',13)]):
                    delay=i*.026
                    bend=track(t,[(0,0),(.23+delay,-amp*.72),(.43+delay,-amp*.68),(.54+delay,amp),(.75+delay,amp*.72),(duration,0)])
                    rot(bone,x=track(t,[(0,0),(.34,-2),(.59,3 if MODE=='ship' else 1.5),(.86,1),(duration,0)]),z=bend*power)
                if MODE=='ship':
                    # Keep the torso low; only the whipping distal chain breaches to hit the hull.
                    for bone,amount,delay in [('Tail_Base',5,.02),('Tail_Mid',7,.06),('Tail_Tip',5,.10)]:
                        rig.pose.bones[bone].rotation_euler.x+=math.radians(track(t,[(0,0),(.30+delay,-amount*.25),(.54+delay,amount),(.80+delay,amount*.45),(duration,0)]))
                rot('Head',z=track(t,[(0,0),(.35,4),(.59,-3),(.9,-1),(duration,0)]))
                rot('Jaw',x=track(t,[(0,0),(.38,6),(.65,10),(.95,2),(duration,0)]))
                for side,sign in [('Left',1),('Right',-1)]:rot('Fin_'+side,y=sign*track(t,[(0,0),(.3,11),(.64,18),(.92,6),(duration,0)]))
                rot('Dorsal',y=track(t,[(0,0),(.34,-3),(.7,7),(duration,0)]))
            elif 'Bite' in name:
                rot('Body',x=track(t,[(0,0),(.24,-3),(.45,4),(.62,1),(duration,0)]),z=track(t,[(0,0),(.24,3),(.5,-2),(duration,0)]))
                rot('Head',x=track(t,[(0,0),(.30,-5),(.45,7),(.61,2),(duration,0)]))
                rot('Jaw',x=track(t,[(0,0),(.22,16),(.35,28),(.45,2),(.56,4),(duration,0)]))
                for i,bone in enumerate(['Spine_Mid','Spine_Rear','Tail_Base','Tail_Mid','Tail_Tip']):
                    rot(bone,z=track(t,[(0,0),(.16+i*.025,-(3+i*2)),(.4+i*.027,5+i*2),(.68+i*.025,-(2+i)),(duration,0)]))
                rot('Fin_Left',y=track(t,[(0,0),(.3,12),(.5,18),(duration,0)]));rot('Fin_Right',y=track(t,[(0,0),(.3,-12),(.5,-18),(duration,0)]))
            else:
                # One damped reaction, rather than the old oscillating head shake.
                for i,(bone,amp) in enumerate([('Head',8),('Body',5),('Spine_Mid',6),('Spine_Rear',8),('Tail_Base',10),('Tail_Mid',9),('Tail_Tip',6)]):
                    delay=i*.014;v=track(t,[(0,0),(.08+delay,amp),(.26+delay,amp*.65),(.52+delay,-amp*.10),(duration,0)])
                    rot(bone,x=-v*.35,z=v)
                rot('Jaw',x=track(t,[(0,0),(.08,11),(.36,4),(duration,0)]))
            for p in rig.pose.bones:
                if p.bone.use_deform:p.keyframe_insert(data_path='rotation_euler',frame=frame+1,group=p.name)
        for _,fc in curve_records(action):
            for k in fc.keyframe_points:k.interpolation='LINEAR'
        actions.append((action,duration,contact))
    finish(rig,actions,MODE+'-encounter-v001.blend','Body','Generic')
elif MODE=='hero':
    bpy.ops.import_scene.fbx(filepath=str(ROOT/'Assets/Game/Prefabs/Characters/Sahur/Animations/Source/StandingMeleeAttackDownward.fbx'))
    rig=next(o for o in bpy.context.scene.objects if o.type=='ARMATURE');source=rig.animation_data.action
    # Resample the approved downward slash at 60 FPS. The left hand balances throughout.
    poses=[];first=None
    mapping=[(0,1),(.3,7),(.63,24),(.9,46),(1.2,60),(1.6,69)]
    for f in range(97):
        source_frame=track(f/60,mapping);whole=math.floor(source_frame)
        bpy.context.scene.frame_set(whole,subframe=source_frame-whole)
        pose={p.name:(p.location.copy(),p.rotation_quaternion.copy() if p.rotation_mode=='QUATERNION' else p.rotation_euler.to_quaternion(),p.scale.copy()) for p in rig.pose.bones}
        if first is None:first=pose
        poses.append(pose)
    action=bpy.data.actions.new('CH1_SahurDiagonalSlash_v001');action.use_fake_user=True;rig.animation_data.action=action
    for f,pose in enumerate(poses):
        bpy.context.scene.frame_set(f+1)
        for p in rig.pose.bones:
            loc,q,scale=pose[p.name];p.rotation_mode='QUATERNION'
            if p.name=='mixamorig:Hips':loc=first[p.name][0].copy()
            if p.name.startswith('mixamorig:Left') and any(s in p.name for s in ('Shoulder','Arm','ForeArm','Hand')):
                q=first[p.name][1].copy()
                if p.name=='mixamorig:LeftArm':q=q@Quaternion((0,1,0),math.radians(track(f/60,[(0,0),(.6,-12),(.9,15),(1.6,0)])))
            p.location=loc;p.rotation_quaternion=q;p.scale=scale
            for field in ('location','rotation_quaternion','scale'):p.keyframe_insert(data_path=field,frame=f+1,group=p.name)
    for _,fc in curve_records(action):
        for k in fc.keyframe_points:k.interpolation='LINEAR'
    finish(rig,[(action,1.6,.9)],'sahur-encounter-v001.blend','mixamorig:Hips','Humanoid')
else:raise ValueError(MODE)
