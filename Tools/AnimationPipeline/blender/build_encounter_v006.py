"""Versioned production pass on inspected rigs. Build first, review, export separately."""
import bpy, sys, json, math
from pathlib import Path
from mathutils import Quaternion, Vector
ROOT=Path(r'D:/indiegameDev/unity/Dark Brine_Rot')
sys.path.insert(0,str(ROOT/'Tools/AnimationPipeline/blender'))
from common import curve_records, slot_for
from render_animation_preview import render
from export_unity_fbx import export
OUT=ROOT/'ArtSource/Encounter/v006'; OUT.mkdir(parents=True,exist_ok=True)
MODE=sys.argv[-1]
def ease(x):
    x=max(0,min(1,x));return x*x*(3-2*x)
def curve(t,keys):
    if t<=keys[0][0]:return keys[0][1]
    for (a,x),(b,y) in zip(keys,keys[1:]):
        if t<=b:return x+(y-x)*ease((t-a)/(b-a))
    return keys[-1][1]
def sample(rig,source,f):
    rig.animation_data.action=source
    bpy.context.scene.frame_set(math.floor(f),subframe=f-math.floor(f))
    return {p.name:(p.location.copy(),p.rotation_quaternion.copy() if p.rotation_mode=='QUATERNION' else p.rotation_euler.to_quaternion(),p.scale.copy()) for p in rig.pose.bones}
def author(rig,source,name,seconds,phase,modify):
    poses=[sample(rig,source,phase(i/60)) for i in range(round(seconds*60)+1)]
    action=bpy.data.actions.new(name);action.use_fake_user=True;rig.animation_data.action=action
    for i,pose in enumerate(poses):
        t=i/60;bpy.context.scene.frame_set(i+1)
        def rotate(bone,x=0,y=0,z=0):
            if bone not in pose:return
            loc,q,s=pose[bone];pose[bone]=(loc,q@Quaternion((1,0,0),math.radians(x))@Quaternion((0,1,0),math.radians(y))@Quaternion((0,0,1),math.radians(z)),s)
        modify(t,pose,rotate)
        for p in rig.pose.bones:
            p.location,p.rotation_quaternion,p.scale=pose[p.name];p.rotation_mode='QUATERNION'
            for channel in ('location','rotation_quaternion','scale'):p.keyframe_insert(data_path=channel,frame=i+1,group=p.name)
    for _,fc in curve_records(action):
        for key in fc.keyframe_points:key.interpolation='LINEAR'
    return action
def save(rig,group,specs,root,kind,avatar):
    bpy.context.scene.render.fps=60;bpy.context.scene.render.fps_base=1
    bpy.context.preferences.filepaths.save_version=0
    path=OUT/(group+'.blend')
    if path.exists():raise FileExistsError(path)
    bpy.ops.wm.save_as_mainfile(filepath=str(path))
    index=[]
    for action,seconds,loop,contact in specs:
        index.append(dict(blend=str(path),rig=rig.name,action=action.name,unity_clip=action.name,revision='v006',frame_start=1,frame_end=round(seconds*60)+1,fps=60,loop=loop,root_motion='in_place',root_bone=root,rig_type=kind,stage='FIRST PASS',design_status='approved',design_reference='Documentation/AnimationWorkflow/Actions/CH1_SahurSharkEncounter_v001/design-v006.md',approval_reference='2026-10-07 user PLEASE IMPLEMENT THIS PLAN',review_status='pending',avatar=avatar,contact_seconds=contact))
    (OUT/(group+'-index.json')).write_text(json.dumps(index,indent=2),encoding='utf-8')
if MODE=='hero':
    bpy.ops.wm.open_mainfile(filepath=str(ROOT/'ArtSource/Encounter/v001/sahur-encounter-v001.blend'))
    rig=bpy.data.objects['Armature'];source=bpy.data.actions['CH1_SahurDiagonalSlash_v001'];specs=[]
    initial=sample(rig,source,1)
    for suffix,seconds,hit in [('Slash',1.4,.76),('Light1',.8,.46),('Light2',.85,.46),('Light3',1.05,.51)]:
        def modify(t,pose,rot):
            u=t/seconds
            # Feet/hips lead the chest; the elbow and wrist retain their imported arc.
            wind=curve(u,[(0,0),(.27,1),(.47,.8),(.59,-.75),(.75,-.32),(1,0)])
            rot('mixamorig:Hips',y=wind*7)
            rot('mixamorig:Spine',y=curve(u,[(0,0),(.32,12),(.6,-13),(.8,-4),(1,0)]))
            rot('mixamorig:Spine1',x=curve(u,[(0,0),(.38,-4),(.6,9),(.82,3),(1,0)]),y=wind*6)
            rot('mixamorig:Spine2',y=curve(u,[(0,0),(.4,7),(.64,-9),(1,0)]))
            rot('mixamorig:RightShoulder',z=curve(u,[(0,0),(.4,-5),(.62,7),(.85,2),(1,0)]))
            # Free hand counterbalances rather than touching or helping grip the weapon.
            for n in ['LeftShoulder','LeftArm','LeftForeArm','LeftHand']:
                bone='mixamorig:'+n;loc,q,s=pose[bone];pose[bone]=(loc,initial[bone][1].copy(),s)
            rot('mixamorig:LeftArm',y=-wind*12,z=curve(u,[(0,0),(.38,-8),(.65,13),(1,0)]))
            rot('mixamorig:LeftForeArm',z=-10)
            # Remove source planar travel. The controller or paired timeline owns displacement.
            loc,q,s=pose['mixamorig:Hips'];base=initial['mixamorig:Hips'][0]
            pose['mixamorig:Hips']=(Vector((base.x,loc.y,base.z)),q,s)
        phase=lambda t:curve(t,[(0,1),(seconds*.3,27),(hit,55),(seconds*.77,75),(seconds,97)])
        action=author(rig,source,'CH1_Sahur'+suffix+'_v006',seconds,phase,modify)
        specs.append((action,seconds,False,hit))
    for suffix,seconds in [('Guard',1.6),('Parry',1.1),('Brace',1.3)]:
        def modify(t,pose,rot):
            u=t/seconds;impact=curve(u,[(0,0),(.3,0),(.44,1),(.6,.65),(1,0)]) if suffix!='Guard' else 0
            rot('mixamorig:Spine',x=8-impact*11,y=-8+impact*12)
            rot('mixamorig:Spine1',x=6-impact*9)
            rot('mixamorig:RightArm',y=-12,z=impact*20)
            rot('mixamorig:RightForeArm',z=impact*9)
            rot('mixamorig:LeftUpLeg',x=-8);rot('mixamorig:LeftLeg',x=14)
            rot('mixamorig:RightUpLeg',x=-6);rot('mixamorig:RightLeg',x=12)
            if suffix=='Guard':rot('mixamorig:Spine2',x=math.sin(u*2*math.pi)*.65)
        action=author(rig,source,'CH1_Sahur'+suffix+'_v006',seconds,lambda t:24,modify)
        specs.append((action,seconds,suffix=='Guard',seconds*.44))
    save(rig,'sahur-combat-v006',specs,'mixamorig:Hips','Humanoid','Assets/Game/Prefabs/Characters/Sahur/Animations/Source/StandingMeleeAttackDownward.fbx')
elif MODE=='locomotion':
    bpy.ops.import_scene.fbx(filepath=str(ROOT/'Assets/ThirdParty/QuaterniusUniversalAnimations/Quaternius_Universal_Standard.fbx'))
    rig=bpy.data.objects['Rig'];specs=[]
    def find(name):return next(a for a in bpy.data.actions if a.name.endswith('|'+name))
    for suffix,sourceName,seconds,loop in [('Walk','Walk_Loop',1.2,True),('Run','Sprint_Loop',.72,True),('JumpStart','Jump_Start',.24,False),('JumpAir','Jump_Loop',1,True),('JumpLand','Jump_Land',.42,False),('Stagger','Hit_Chest',.75,False),('Fall','Death01',2.25,False)]:
        source=find(sourceName);lo,hi=source.frame_range;first=sample(rig,source,lo)
        def modify(t,pose,rot):
            u=t/seconds
            if suffix in ('Walk','Run'):
                v=math.sin(u*2*math.pi);rot('DEF-spine.001',y=v*(3 if suffix=='Run' else 2));rot('DEF-spine.003',y=-v*2)
                rot('DEF-neck',y=-v);rot('DEF-spine.002',x=-3 if suffix=='Run' else 0)
            elif suffix=='JumpStart':
                load=curve(u,[(0,0),(.42,1),(.72,1),(1,0)])
                for side in 'LR':rot('DEF-thigh.'+side,x=-load*12);rot('DEF-shin.'+side,x=load*18)
                rot('DEF-spine.001',x=-load*6)
            elif suffix=='JumpAir':
                rot('DEF-thigh.L',x=-10);rot('DEF-shin.L',x=15);rot('DEF-forearm.L',z=12)
            elif suffix=='JumpLand':
                load=curve(u,[(0,.2),(.2,1),(.55,.7),(1,0)])
                for side in 'LR':rot('DEF-thigh.'+side,x=-load*10);rot('DEF-shin.'+side,x=load*15)
                rot('DEF-spine.001',x=-load*7);rot('DEF-spine.003',x=curve(u,[(0,0),(.38,-5),(.72,3),(1,0)]))
            elif suffix=='Stagger':
                rot('DEF-spine.003',x=curve(u,[(0,0),(.16,-12),(.38,-7),(1,0)]))
                rot('DEF-hips',y=curve(u,[(0,0),(.3,12),(.65,6),(1,0)]))
            else:
                # Chest receives the blow first, pelvis follows, then free limbs settle.
                rot('DEF-spine.003',x=curve(u,[(0,0),(.14,-14),(.35,-6),(1,0)]))
                rot('DEF-hips',y=curve(u,[(0,0),(.28,12),(.65,4),(1,0)]))
                rot('DEF-forearm.R',z=curve(u,[(0,0),(.4,15),(.68,6),(1,2)]))
            # No planar source drift; keep authored vertical centre-of-mass change.
            for n in ('root','DEF-hips'):
                loc,q,s=pose[n];base=first[n][0];pose[n]=(Vector((base.x,base.y,loc.z)),q,s)
        action=author(rig,source,'PLAYER_'+suffix+'_v006',seconds,lambda t:lo+(hi-lo)*t/seconds,modify)
        specs.append((action,seconds,loop,0))
    save(rig,'sahur-locomotion-v006',specs,'root','Humanoid','Assets/ThirdParty/QuaterniusUniversalAnimations/Quaternius_Universal_Standard.fbx')
elif MODE=='board':
    bpy.ops.import_scene.fbx(filepath=str(ROOT/'Assets/Game/Prefabs/Characters/Sahur/Animations/Gameplay/QuaterniusUAL2/UAL2_Standard.fbx'))
    rig=bpy.data.objects['Armature'];specs=[]
    def find(name):return next(a for a in bpy.data.actions if a.name.endswith('|'+name))
    for suffix,seconds,loop in [('BoardBalance',1.6,True),('BoardDrive',1.6,True),('BoardBrake',1.6,True),('BoardLeft',1.6,True),('BoardRight',1.6,True),('Climb',1.15,False),('Throw',1.45,False)]:
        source=find('ClimbUp_1m' if suffix=='Climb' else 'OverhandThrow' if suffix=='Throw' else 'Idle_No_Loop')
        lo,hi=source.frame_range;first=sample(rig,source,lo)
        def modify(t,pose,rot):
            u=t/seconds
            if suffix.startswith('Board'):
                lean=-9 if suffix=='BoardDrive' else 7 if suffix=='BoardBrake' else -2
                side=-9 if suffix=='BoardLeft' else 9 if suffix=='BoardRight' else 0
                # Staggered knees absorb board pitch; torso counter-steers around the pelvis.
                rot('pelvis',z=side*.55);rot('spine_01',x=lean,z=-side*.45)
                rot('spine_02',y=side*.5,x=math.sin(u*math.pi*2)*.8)
                rot('thigh_l',x=-18,z=-5);rot('calf_l',x=28)
                rot('thigh_r',x=-12,z=6);rot('calf_r',x=22)
                rot('upperarm_l',z=-16,y=-8);rot('lowerarm_l',z=20)
                rot('upperarm_r',z=12);rot('lowerarm_r',z=-18)
                rot('neck_01',z=-side*.4)
            elif suffix=='Climb':
                effort=curve(u,[(0,0),(.23,1),(.48,.8),(.76,.3),(1,0)])
                rot('spine_01',x=-effort*8);rot('spine_03',x=effort*5)
                rot('clavicle_l',z=-effort*5);rot('clavicle_r',z=effort*5)
                rot('thigh_l',x=curve(u,[(0,0),(.5,-12),(.76,-5),(1,0)]))
            else:
                rot('pelvis',y=curve(u,[(0,0),(.25,-7),(.55,10),(1,0)]))
                rot('spine_01',y=curve(u,[(0,0),(.35,-8),(.6,9),(1,0)]))
            for n in ('root','pelvis'):
                loc,q,s=pose[n];base=first[n][0];pose[n]=(Vector((base.x,base.y,loc.z)),q,s)
        phase=lambda t:lo if suffix.startswith('Board') else curve(t/seconds,[(0,lo),(.22,lo+(hi-lo)*.16),(.55,lo+(hi-lo)*.58),(.83,lo+(hi-lo)*.87),(1,hi)])
        action=author(rig,source,'PLAYER_'+suffix+'_v006',seconds,phase,modify)
        specs.append((action,seconds,loop,.55*seconds))
    save(rig,'sahur-board-v006',specs,'root','Humanoid','Assets/Game/Prefabs/Characters/Sahur/Animations/Gameplay/QuaterniusUAL2/UAL2_Standard.fbx')
elif MODE=='shark':
    bpy.ops.wm.open_mainfile(filepath=str(ROOT/'ArtSource/Encounter/v003/shark-breach-v003.blend'))
    rig=bpy.data.objects['Tralalero_Rig'];specs=[];source=bpy.data.actions['Swim_Cruise']
    for suffix,seconds,hit in [('Bite',1.3,.52),('Tail',1.5,.65),('ShipSmash',1.5,.65),('Recoil',1.1,.15),('Breach',3.2,1.55)]:
        def modify(t,pose,rot):
            for name,(loc,q,s) in list(pose.items()):pose[name]=(Vector((0,0,0)),Quaternion(),s)
            if suffix in ('Tail','ShipSmash'):
                power=1.18 if suffix=='ShipSmash' else 1
                # Body coils first, tail tip receives the angular impulse last, then overshoots once.
                for i,(bone,amp) in enumerate([('Body',8),('Spine_Mid',13),('Spine_Rear',19),('Tail_Base',28),('Tail_Mid',24),('Tail_Tip',18)]):
                    delay=i*.024
                    bend=curve(t,[(0,0),(.22+delay,-amp*.7),(.39+delay,-amp),(.52+delay,amp*.95),(.7+delay,amp*.6),(1.05+delay,-amp*.12),(seconds,0)])
                    rot(bone,z=bend*power,x=curve(t,[(0,0),(.34+delay,-2),(.53+delay,4 if i>2 else 1),(.85+delay,2),(seconds,0)]))
                rot('Head',z=curve(t,[(0,0),(.33,7),(.59,-6),(1.15,-1),(seconds,0)]))
                rot('Fin_Left',y=18);rot('Fin_Right',y=-12)
            elif suffix=='Bite':
                rot('Body',x=curve(t,[(0,0),(.28,-5),(.5,5),(.8,2),(seconds,0)]))
                rot('Head',x=curve(t,[(0,0),(.3,-8),(.52,10),(.73,3),(seconds,0)]))
                rot('Jaw',x=curve(t,[(0,0),(.21,15),(.4,32),(.52,1),(.69,5),(seconds,0)]))
                for i,bone in enumerate(['Spine_Mid','Spine_Rear','Tail_Base','Tail_Mid','Tail_Tip']):
                    d=i*.032;rot(bone,z=curve(t,[(0,0),(.2+d,-6-i*2),(.45+d,9+i*3),(.75+d,-3-i),(seconds,0)]))
            elif suffix=='Recoil':
                for i,bone in enumerate(['Head','Body','Spine_Mid','Spine_Rear','Tail_Base','Tail_Mid','Tail_Tip']):
                    d=i*.026;v=curve(t,[(0,0),(.11+d,10+i),(.3+d,6),(.68+d,-1),(seconds,0)]);rot(bone,x=-v*.38,z=v)
                rot('Jaw',x=curve(t,[(0,0),(.15,16),(.4,7),(seconds,0)]))
            else:
                u=t/seconds;drive=curve(u,[(0,.3),(.2,1),(.48,.75),(.7,.4),(.88,.8),(1,.3)])
                for i,bone in enumerate(['Body','Spine_Mid','Spine_Rear','Tail_Base','Tail_Mid','Tail_Tip']):
                    rot(bone,z=math.sin(t*7-i*.55)*drive*(2+i*2),x=curve(u,[(0,0),(.25,-4),(.45,-2),(.7,7),(.85,3),(1,0)])*(.3+i*.1))
                rot('Head',x=curve(u,[(0,0),(.2,-6),(.5,0),(.72,10),(1,0)]))
                rot('Jaw',x=curve(u,[(0,0),(.34,14),(.5,5),(.74,2),(1,0)]))
                rot('Fin_Left',y=curve(u,[(0,5),(.32,22),(.63,8),(.83,26),(1,5)]));rot('Fin_Right',y=-curve(u,[(0,5),(.32,22),(.63,8),(.83,26),(1,5)]))
        action=author(rig,source,'CH1_Shark'+suffix+'_v006',seconds,lambda t:1,modify);specs.append((action,seconds,False,hit))
    save(rig,'shark-v006',specs,'Body','Generic','')
elif MODE=='seams':
    index=json.loads((OUT/'sahur-locomotion-v006-index.json').read_text())
    path=Path(index[0]['blend']);backup=path.with_name(path.stem+'-before-seam-fix.blend')
    if backup.exists():raise FileExistsError(backup)
    import shutil;shutil.copy2(path,backup)
    bpy.ops.wm.open_mainfile(filepath=str(path));rig=bpy.data.objects[index[0]['rig']]
    for m in index:
        if not m['loop']:continue
        action=bpy.data.actions[m['action']];first=sample(rig,action,1);end=m['frame_end'];samples=[sample(rig,action,f) for f in range(end-7,end+1)]
        rig.animation_data.action=action
        for i,data in enumerate(samples):
            weight=ease(i/7)
            for p in rig.pose.bones:
                loc,q,s=data[p.name];a,b,c=first[p.name]
                p.location=loc.lerp(a,weight);p.rotation_mode='QUATERNION';p.rotation_quaternion=q.slerp(b,weight);p.scale=s.lerp(c,weight)
                for channel in ('location','rotation_quaternion','scale'):p.keyframe_insert(data_path=channel,frame=end-7+i,group=p.name)
    bpy.ops.wm.save_as_mainfile(filepath=str(path))
elif MODE in ('preview','export'):
    reports=[]
    for indexfile in OUT.glob('*-index.json'):
        index=json.loads(indexfile.read_text());bpy.ops.wm.open_mainfile(filepath=index[0]['blend']);rig=bpy.data.objects[index[0]['rig']]
        for m in index:
            action=bpy.data.actions[m['action']]
            if MODE=='preview':
                if m['action'] not in ['PLAYER_JumpStart_v006','PLAYER_JumpLand_v006','PLAYER_BoardDrive_v006','PLAYER_Climb_v006','CH1_SharkTail_v006','CH1_SharkBreach_v006']:continue
                reports.append(render(rig,action,ROOT/'.codex/encounter-v006/previews'/action.name,[1,round(m['frame_end']*.25),round(m['frame_end']*.5),round(m['frame_end']*.75),m['frame_end']],slot_for(action,rig),resolution=384))
            else:
                acceptance=OUT/'source-review.json'
                accepted=json.loads(acceptance.read_text())
                if m['action'] not in accepted['accepted_for_staging']:raise ValueError('Action not reviewed for staging: '+m['action'])
                m['review_status']='accepted_for_stage';m['review_reference']=str(acceptance)
                path=OUT/(action.name+'.fbx')
                if not path.exists():reports.append(export(rig,action,m,path,slot_for(action,rig)))
    (OUT/(MODE+'-report.json')).write_text(json.dumps(reports,indent=2),encoding='utf-8')
else:raise ValueError(MODE)
