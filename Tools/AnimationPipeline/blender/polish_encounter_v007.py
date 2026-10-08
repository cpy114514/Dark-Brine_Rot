"""Forward guard, recoil and alternating swim strokes on retained source rigs."""
import bpy,sys,json,math
from pathlib import Path
from mathutils import Vector
ROOT=Path(r'D:/indiegameDev/unity/Dark Brine_Rot')
helper=(ROOT/'Tools/AnimationPipeline/blender/build_encounter_v006.py').read_text().split("if MODE=='hero':")[0]
exec(helper,globals())
OUT=ROOT/'ArtSource/Encounter/v007';OUT.mkdir(parents=True,exist_ok=True)
mode=sys.argv[-1]
if mode=='guard':
    bpy.ops.wm.open_mainfile(filepath=str(ROOT/'ArtSource/Encounter/v001/sahur-encounter-v001.blend'))
    rig=bpy.data.objects['Armature'];source=bpy.data.actions['CH1_SahurDiagonalSlash_v001'];specs=[]
    for suffix,seconds in [('Guard',1.6),('Parry',1.1),('Brace',1.3)]:
        def modify(t,pose,rot):
            u=t/seconds;load=0 if suffix=='Guard' else curve(u,[(0,0),(.30,0),(.44,1),(.61,.6),(1,0)])
            rot('mixamorig:Hips',y=-load*5)
            rot('mixamorig:Spine',x=4-load*9,y=load*8)
            rot('mixamorig:Spine1',x=3-load*6)
            rot('mixamorig:RightArm',z=load*8)
            rot('mixamorig:RightForeArm',z=load*9)
            rot('mixamorig:LeftArm',z=-load*7,y=load*9)
            for side in ('Left','Right'):
                rot('mixamorig:'+side+'UpLeg',x=-5-load*3)
                rot('mixamorig:'+side+'Leg',x=9+load*5)
            if suffix=='Guard':rot('mixamorig:Spine2',x=math.sin(u*2*math.pi)*.65)
        action=author(rig,source,'CH1_Sahur'+suffix+'_v007',seconds,lambda t:1,modify)
        specs.append((action,seconds,suffix=='Guard',seconds*.44))
    save(rig,'sahur-guard-v007',specs,'mixamorig:Hips','Humanoid','Assets/Game/Prefabs/Characters/Sahur/Animations/Source/StandingMeleeAttackDownward.fbx')
elif mode=='swim':
    bpy.ops.import_scene.fbx(filepath=str(ROOT/'Assets/ThirdParty/QuaterniusUniversalAnimations/Quaternius_Universal_Standard.fbx'))
    rig=bpy.data.objects['Rig'];specs=[]
    def find(name):return next(a for a in bpy.data.actions if a.name.endswith('|'+name))
    for suffix,sourceName,seconds in [('SwimIdle','Swim_Idle_Loop',1.5),('Swim','Swim_Fwd_Loop',1.2),('SwimFast','Swim_Fwd_Loop',.8)]:
        source=find(sourceName);lo,hi=source.frame_range;first=sample(rig,source,lo)
        opposite=[sample(rig,source,lo+(hi-lo)*((i/60/seconds+.5)%1)) for i in range(round(seconds*60)+1)]
        def modify(t,pose,rot):
            u=t/seconds;roll=math.sin(u*2*math.pi)
            if suffix=='SwimFast':
                other=opposite[min(round(t*60),len(opposite)-1)]
                for n in ('DEF-shoulder.R','DEF-upper_arm.R','DEF-forearm.R','DEF-hand.R'):
                    if n in pose:pose[n]=other[n]
                rot('DEF-spine.001',y=roll*5);rot('DEF-spine.003',y=roll*3)
                rot('DEF-neck',y=-roll*5,x=-3)
                rot('DEF-thigh.L',x=math.sin(u*4*math.pi)*5)
                rot('DEF-thigh.R',x=-math.sin(u*4*math.pi)*5)
            else:
                rot('DEF-spine.001',y=roll*2)
                rot('DEF-neck',y=-roll*2)
            for n in ('root','DEF-hips'):
                loc,q,s=pose[n];base=first[n][0];pose[n]=(Vector((base.x,base.y,loc.z)),q,s)
        action=author(rig,source,'PLAYER_'+suffix+'_v007',seconds,lambda t:lo+(hi-lo)*t/seconds,modify)
        # Exact seam with a short eased settlement preserves all intermediate strokes.
        begin=sample(rig,action,1);end=round(seconds*60)+1;ending=[sample(rig,action,f) for f in range(end-6,end+1)]
        rig.animation_data.action=action
        for i,data in enumerate(ending):
            weight=ease(i/6)
            for p in rig.pose.bones:
                loc,q,s=data[p.name];a,b,c=begin[p.name]
                p.location=loc.lerp(a,weight);p.rotation_mode='QUATERNION';p.rotation_quaternion=q.slerp(b,weight);p.scale=s.lerp(c,weight)
                for channel in ('location','rotation_quaternion','scale'):p.keyframe_insert(data_path=channel,frame=end-6+i,group=p.name)
        specs.append((action,seconds,True,0))
    save(rig,'sahur-swim-v007',specs,'root','Humanoid','Assets/ThirdParty/QuaterniusUniversalAnimations/Quaternius_Universal_Standard.fbx')
elif mode=='preview':
    index=json.loads((OUT/'sahur-swim-v007-index.json').read_text());bpy.ops.wm.open_mainfile(filepath=index[0]['blend']);rig=bpy.data.objects[index[0]['rig']]
    reports=[]
    for m in index:
        action=bpy.data.actions[m['action']]
        reports.append(render(rig,action,ROOT/'.codex/encounter-v007/previews'/action.name,[1,round(m['frame_end']*.25),round(m['frame_end']*.5),round(m['frame_end']*.75),m['frame_end']],slot_for(action,rig),resolution=384))
    (OUT/'preview-report.json').write_text(json.dumps(reports,indent=2),encoding='utf-8')
elif mode=='export':
    reports=[]
    for indexfile in OUT.glob('*-index.json'):
        index=json.loads(indexfile.read_text());bpy.ops.wm.open_mainfile(filepath=index[0]['blend']);rig=bpy.data.objects[index[0]['rig']]
        for m in index:
            m['revision']='v007';m['review_status']='accepted_for_stage';m['review_reference']=str(OUT/'source-review.json')
            action=bpy.data.actions[m['action']];path=OUT/(action.name+'.fbx')
            if not path.exists():reports.append(export(rig,action,m,path,slot_for(action,rig)))
    (OUT/'export-report.json').write_text(json.dumps(reports,indent=2),encoding='utf-8')
else:raise ValueError(mode)
