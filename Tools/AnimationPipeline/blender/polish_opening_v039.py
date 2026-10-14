"""Actual bone revisions on retained originals; export/review is a separate stage."""
import bpy,sys,json,math
from pathlib import Path
from mathutils import Vector,Matrix
ROOT=Path(r'D:/indiegameDev/unity/Dark Brine_Rot')
exec((ROOT/'Tools/AnimationPipeline/blender/build_encounter_v006.py').read_text().split("if MODE=='hero':")[0],globals())
OUT=ROOT/'ArtSource/Encounter/v039';OUT.mkdir(parents=True,exist_ok=True)
mode=sys.argv[-1]

def stamp(group):
    path=OUT/(group+'-index.json');items=json.loads(path.read_text())
    for m in items:m.update(revision='v039',stage='FIRST PASS',design_reference='Documentation/AnimationWorkflow/Actions/CH1_SahurSharkEncounter_v001/design-v039.md',approval_reference='2026-10-12 user 开始实施计划')
    path.write_text(json.dumps(items,indent=2),encoding='utf-8')

if mode=='hero':
    bpy.ops.wm.open_mainfile(filepath=str(ROOT/'ArtSource/Encounter/v027/sahur-supported-combat-v027.blend'))
    rig=bpy.data.objects['Armature'];rest={b.name:tuple(b.matrix_local) for b in rig.data.bones}
    with bpy.data.libraries.load(str(ROOT/'ArtSource/Encounter/v036/sahur-spaced-slash-v036.blend'),link=False) as (src,dst):dst.actions=['CH1_SahurSlash_v036']
    specs=[];audits=[]
    for suffix,seconds,contact in [('Guard',1.6,0),('Parry',1.1,.484),('Slash',1.4,.63),('Brace',1.3,.572),('Miss',1.4,0)]:
        source=bpy.data.actions['CH1_Sahur'+('Slash' if suffix=='Miss' else suffix)+('_v036' if suffix in ('Slash','Miss') else '_v027')]
        def modify(t,pose,rot):
            if suffix=='Guard':
                tension=math.sin(t/seconds*2*math.pi)
                rot('mixamorig:Spine',x=2,y=tension*1.5)
                rot('mixamorig:Spine2',y=-tension)
                rot('mixamorig:LeftArm',z=-4,y=-3)
                rot('mixamorig:LeftForeArm',z=5)
                rot('mixamorig:Head',y=-tension*.8)
            elif suffix in ('Slash','Miss'):
                # Hips initiate; loaded torso and shoulder unwind later. The
                # supplied overhead right-hand arc remains recognisable.
                rot('mixamorig:Hips',y=curve(t,[(0,0),(.14,-4),(.39,-6),(.56,6),(.75,2),(1.4,0)]))
                rot('mixamorig:Spine',y=curve(t,[(0,0),(.22,-6),(.43,-9),(.62,9),(.88,3),(1.4,0)]))
                rot('mixamorig:Spine1',x=curve(t,[(0,0),(.36,-3),(.55,-2),(.72,6),(.94,2),(1.4,0)]),y=curve(t,[(0,0),(.31,-3),(.5,-6),(.67,5),(1.4,0)]))
                rot('mixamorig:RightShoulder',z=curve(t,[(0,0),(.35,-3),(.52,-4),(.7,5),(.95,1),(1.4,0)]))
                rot('mixamorig:RightForeArm',y=curve(t,[(0,0),(.48,-3),(.65,2),(.84,4),(1.4,0)]))
                rot('mixamorig:LeftArm',y=curve(t,[(0,0),(.35,7),(.68,-9),(.9,-3),(1.4,0)]))
                if suffix=='Miss':
                    rot('mixamorig:Spine2',y=curve(t,[(0,0),(.55,0),(.76,9),(.94,7),(1.4,0)]),x=curve(t,[(0,0),(.6,0),(.82,7),(1.4,0)]))
                    rot('mixamorig:RightArm',z=curve(t,[(0,0),(.63,0),(.8,-6),(1.4,0)]))
            else:
                hit=contact
                load=curve(t,[(0,0),(hit-.22,-1),(hit,1),(hit+.13,.75),(min(seconds,hit+.42),0),(seconds,0)])
                rot('mixamorig:Hips',y=load*3)
                rot('mixamorig:Spine',x=-load*5,y=load*5)
                rot('mixamorig:Spine1',x=curve(t,[(0,0),(hit-.03,0),(hit+.055,-6),(hit+.18,-3),(seconds,0)]))
                rot('mixamorig:Spine2',y=-load*3)
                rot('mixamorig:RightShoulder',z=curve(t,[(0,0),(hit-.10,-2),(hit+.045,5),(hit+.19,2),(seconds,0)]))
                rot('mixamorig:RightForeArm',z=curve(t,[(0,0),(hit,0),(hit+.08,4),(hit+.23,1),(seconds,0)]))
                rot('mixamorig:LeftArm',y=-load*6,z=load*3)
        action=author(rig,source,'CH1_Sahur'+suffix+'_v039',seconds,lambda t:1+t*60,modify)
        specs.append((action,seconds,suffix=='Guard',contact))
        peak={}
        samples=[sample(rig,action,1+i) for i in range(round(seconds*60)+1)]
        for name in ('Hips','Spine','Spine1','Spine2','RightShoulder','RightArm','RightForeArm','RightHand'):
            bone='mixamorig:'+name
            peak[name]=max(math.degrees(a[bone][1].rotation_difference(b[bone][1]).angle) for a,b in zip(samples,samples[1:]))
        audits.append(dict(action=action.name,peak_degrees_per_60fps_frame=peak,source=source.name,contact_seconds=contact))
    assert rest=={b.name:tuple(b.matrix_local) for b in rig.data.bones}
    save(rig,'sahur-combat-v039',specs,'mixamorig:Hips','Humanoid','Assets/Game/Prefabs/Characters/Sahur/Animations/Source/StandingMeleeAttackDownward.fbx');stamp('sahur-combat-v039')
    (OUT/'hero-source-audit.json').write_text(json.dumps(dict(original_rest_retained=True,bones=len(rest),actions=audits),indent=2))
elif mode=='export-hero':
    index=json.loads((OUT/'sahur-combat-v039-index.json').read_text());bpy.ops.wm.open_mainfile(filepath=index[0]['blend']);rig=bpy.data.objects[index[0]['rig']];reports=[]
    review=OUT/'hero-source-review.json'
    if not review.exists():raise ValueError('Review the actual source audit before staging')
    accepted=json.loads(review.read_text())['accepted_for_staging']
    for m in index:
        if m['action'] not in accepted:continue
        m.update(stage='BLOCKING',review_status='accepted_for_stage',review_reference=str(review))
        reports.append(export(rig,bpy.data.actions[m['action']],m,OUT/(m['action']+'.fbx'),slot_for(bpy.data.actions[m['action']],rig)))
    (OUT/'hero-export-report.json').write_text(json.dumps(reports,indent=2))
else:raise ValueError(mode)
