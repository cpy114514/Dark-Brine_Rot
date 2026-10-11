"""Spread the real one-arm overhead preparation; preserve the .63s strike."""
import bpy, sys, json
from pathlib import Path
ROOT=Path(r'D:/indiegameDev/unity/Dark Brine_Rot')
exec((ROOT/'Tools/AnimationPipeline/blender/build_encounter_v006.py').read_text().split("if MODE=='hero':")[0],globals())
OUT=ROOT/'ArtSource/Encounter/v036';OUT.mkdir(parents=True,exist_ok=True)
mode=sys.argv[-1]
if mode=='build':
    bpy.ops.wm.open_mainfile(filepath=str(ROOT/'ArtSource/Encounter/v033/sahur-timed-slash-v033.blend'))
    rig=bpy.data.objects['Armature'];before={b.name:tuple(b.matrix_local) for b in rig.data.bones}
    source=bpy.data.actions['CH1_SahurSlash_v032']
    # v033 native dense review: the overhead preparation jumped 47.6 degrees
    # in one 60 FPS frame at .45s. Begin preparation earlier and distribute
    # its original shoulder/elbow poses over more frames. Descending contact,
    # follow-through, recovery, clip length and gameplay windows stay intact.
    keys=[(0,0),(.06,.12),(.14,.27),(.22,.35),(.32,.44),(.42,.52),
          (.54,.575),(.63,.600),(.75,.625),(.92,.70),(1.15,1),(1.4,1.4)]
    def old_time(t):
        for (a,x),(b,y) in zip(keys,keys[1:]):
            if t<=b:return x+(y-x)*(t-a)/(b-a)
        return keys[-1][1]
    action=author(rig,source,'CH1_SahurSlash_v036',1.4,lambda t:1+old_time(t)*60,lambda t,pose,rot:None)
    assert before=={b.name:tuple(b.matrix_local) for b in rig.data.bones}
    save(rig,'sahur-spaced-slash-v036',[(action,1.4,False,.63)],'mixamorig:Hips','Humanoid',
         'Assets/Game/Prefabs/Characters/Sahur/Animations/Source/StandingMeleeAttackDownward.fbx')
    path=OUT/'sahur-spaced-slash-v036-index.json';index=json.loads(path.read_text())
    for entry in index:entry.update(revision='v036',stage='BLOCKING',review_status='pending_native_review')
    path.write_text(json.dumps(index,indent=2))
    (OUT/'source-timing-audit.json').write_text(json.dumps(dict(original_rest_rig_retained=True,
        original_actions_retained=True,source_action=source.name,time_map_seconds=keys,
        source_contact_seconds=old_time(.63),native_contact_seconds=.63,seconds=1.4,fps=60,
        native_review_required=True),indent=2))
elif mode=='export':
    bpy.ops.wm.open_mainfile(filepath=str(OUT/'sahur-spaced-slash-v036.blend'));rig=bpy.data.objects['Armature'];reports=[]
    for metadata in json.loads((OUT/'sahur-spaced-slash-v036-index.json').read_text()):
        metadata.update(stage='BLOCKING',review_status='accepted_for_stage',review_reference=str(OUT/'source-blocking-review.json'))
        if not Path(metadata['review_reference']).exists():raise ValueError('Source rig/curve review required')
        reports.append(export(rig,bpy.data.actions[metadata['action']],metadata,OUT/(metadata['action']+'.fbx'),slot_for(bpy.data.actions[metadata['action']],rig)))
    (OUT/'export-report.json').write_text(json.dumps(reports,indent=2))
else:raise ValueError(mode)
