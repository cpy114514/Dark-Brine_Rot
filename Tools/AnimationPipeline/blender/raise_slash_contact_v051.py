"""Retain diagonal sweep but meet the head before its shaft drops beside timber."""
import bpy,sys,json,math
from pathlib import Path
ROOT=Path(r'D:/indiegameDev/unity/Dark Brine_Rot')
exec((ROOT/'Tools/AnimationPipeline/blender/build_encounter_v006.py').read_text(encoding='utf-8').split("if MODE=='hero':")[0],globals())
OUT=ROOT/'ArtSource/Encounter/v051';OUT.mkdir(parents=True,exist_ok=True)
mode=sys.argv[-1];name='CH1_SahurSlash_v051'
if mode=='build':
    bpy.ops.wm.open_mainfile(filepath=str(ROOT/'ArtSource/Encounter/v040/sahur-smooth-combat-v040.blend'))
    rig=bpy.data.objects['Armature'];rest={b.name:tuple(b.matrix_local) for b in rig.data.bones}
    source=bpy.data.actions['CH1_SahurSlash_v040']
    def phase(t):return 1+60*max(0,t-.03*math.exp(-.5*((t-.63)/.12)**2))
    action=author(rig,source,name,1.4,phase,lambda t,pose,rotate:None)
    assert rest=={b.name:tuple(b.matrix_local) for b in rig.data.bones}
    save(rig,'sahur-raised-contact-v051',[(action,1.4,False,.63)],'mixamorig:Hips','Humanoid','Assets/Game/Prefabs/Characters/Sahur/Animations/Source/StandingMeleeAttackDownward.fbx')
    index=OUT/'sahur-raised-contact-v051-index.json';data=json.loads(index.read_text(encoding='utf-8'))
    data[0].update(revision='v051',stage='FIRST PASS',design_reference='Documentation/AnimationWorkflow/Actions/CH1_SahurSharkEncounter_v001/design-v039.md',approval_reference='2026-10-12 user 开始实施计划')
    index.write_text(json.dumps(data,indent=2),encoding='utf-8')
    (OUT/'source-audit.json').write_text(json.dumps(dict(original_rig_retained=True,bones=len(rest),source=source.name,contact_seconds=.63,source_phase_at_contact_seconds=(phase(.63)-1)/60,minimum_time_derivative=min((phase((i+1)/600)-phase(i/600))*10 for i in range(840)),intent='Meet the head during the accelerated descending arc, retaining impact time, recovery, original rig and one-hand sweep'),indent=2),encoding='utf-8')
elif mode in ['preview','export']:
    m=json.loads((OUT/'sahur-raised-contact-v051-index.json').read_text(encoding='utf-8'))[0]
    bpy.ops.wm.open_mainfile(filepath=m['blend']);rig=bpy.data.objects[m['rig']];action=bpy.data.actions[name]
    if mode=='preview':
        result=render(rig,action,ROOT/'.codex/opening-optimization-20261012/slash-v051',frames=[1,25,34,39,47,65,85],slot=slot_for(action,rig),resolution=480)
        (OUT/'preview-report.json').write_text(json.dumps(result,indent=2),encoding='utf-8')
    else:
        review=json.loads((OUT/'source-review.json').read_text(encoding='utf-8'))
        if name not in review['accepted_for_staging']:raise RuntimeError('Review required')
        m.update(review_status='accepted_for_stage',review_reference=str(OUT/'source-review.json'))
        result=export(rig,action,m,OUT/(name+'.fbx'),slot_for(action,rig))
        (OUT/'export-report.json').write_text(json.dumps(result,indent=2),encoding='utf-8')
else:raise ValueError(mode)
