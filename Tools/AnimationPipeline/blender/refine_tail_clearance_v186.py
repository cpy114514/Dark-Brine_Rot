"""Original-rig rear arch revision; unchanged contact time, trunk and skin."""
import bpy,sys,json,math
from pathlib import Path
ROOT=Path(r'D:/indiegameDev/unity/Dark Brine_Rot')
exec((ROOT/'Tools/AnimationPipeline/blender/build_encounter_v006.py').read_text().split("if MODE=='hero':")[0],globals())
OUT=ROOT/'ArtSource/Encounter/v186';OUT.mkdir(parents=True,exist_ok=True)
mode=sys.argv[-1]
group='shark-tail-clearance-v186'
if mode=='build':
    bpy.ops.wm.open_mainfile(filepath=str(ROOT/'ArtSource/Encounter/v097/shark-tail-inertia-v097.blend'))
    rig=bpy.data.objects['Tralalero_Rig'];source=bpy.data.actions['CH1_SharkTail_v097']
    rest={b.name:tuple(b.matrix_local) for b in rig.data.bones}
    parents={b.name:b.parent.name if b.parent else None for b in rig.data.bones}
    original=[sample(rig,source,i+1) for i in range(91)]
    sample(rig,source,40)
    old=(rig.pose.bones['Tail_Tip'].matrix.translation-rig.pose.bones['Body'].matrix.translation).copy()
    changed={'Spine_Rear','Tail_Base','Tail_Mid'}
    def modify(t,pose,rotate):
        # Lift through the rear chain, rather than pitching the animal's
        # whole mass above the water to reach a guard on rising timber.
        # The rear leads; the middle tail arrives later and recoils last.
        for bone,amplitude,delay in [('Spine_Rear',3,0),('Tail_Base',4,.018),('Tail_Mid',2,.035)]:
            lift=curve(t,[(0,0),(.22+delay,-amplitude*.12),(.39+delay,amplitude*.12),
                          (.65,amplitude),(.78+delay,amplitude*.7),
                          (1.03+delay,-amplitude*.2),(1.5,0)])
            rotate(bone,x=lift)
    action=author(rig,source,'CH1_SharkTail_v186',1.5,lambda t:1+t*60,modify)
    revised=[sample(rig,action,i+1) for i in range(91)]
    retained=max(math.degrees(a[n][1].rotation_difference(b[n][1]).angle)
                 for a,b in zip(original,revised) for n in rest if n not in changed)
    peaks={n:max(math.degrees(a[n][1].rotation_difference(b[n][1]).angle)
                 for a,b in zip(revised,revised[1:])) for n in changed}
    sample(rig,action,40)
    new=(rig.pose.bones['Tail_Tip'].matrix.translation-rig.pose.bones['Body'].matrix.translation).copy()
    assert rest=={b.name:tuple(b.matrix_local) for b in rig.data.bones} and len(rest)==16
    assert retained<.001 and max(peaks.values())<10,(retained,peaks)
    save(rig,group,[(action,1.5,False,.65)],'Body','Generic','')
    index_path=OUT/(group+'-index.json');index=json.loads(index_path.read_text())
    for m in index:m.update(revision='v186',stage='FIRST PASS',
        design_reference='Documentation/AnimationWorkflow/Actions/CH1_SahurSharkEncounter_v001/design-v039.md',
        approval_reference='2026-10-14 authorized animation continuation',review_status='pending')
    index_path.write_text(json.dumps(index,indent=2))
    audit=dict(original_rest_preserved=True,bones=16,parents=parents,original_mesh_preserved=True,
        source=source.name,action=action.name,changed_channels=sorted(changed),duration=1.5,fps=60,
        contact_seconds=.65,unchanged_other_rotation_error_degrees=retained,
        source_tip_minus_body=list(old),revised_tip_minus_body=list(new),
        contact_tip_delta=list(new-old),peak_degrees_per_frame=peaks,runtime_activated=False)
    (OUT/'source-audit.json').write_text(json.dumps(audit,indent=2));print(json.dumps(audit))
elif mode in ('preview','export'):
    m=json.loads((OUT/(group+'-index.json')).read_text())[0]
    bpy.ops.wm.open_mainfile(filepath=m['blend']);rig=bpy.data.objects[m['rig']];action=bpy.data.actions[m['action']]
    if mode=='preview':
        report=render(rig,action,ROOT/'.codex/animation-continuation-20261014/tail-source-v186',
                      [1,16,25,34,40,49,64,79,91],slot_for(action,rig),resolution=480)
    else:
        review=OUT/'source-review.json'
        if action.name not in json.loads(review.read_text())['accepted_for_staging']:raise ValueError('Source not reviewed')
        m.update(review_status='accepted_for_stage',review_reference=str(review))
        report=export(rig,action,m,OUT/(action.name+'.fbx'),slot_for(action,rig))
    (OUT/(mode+'-report.json')).write_text(json.dumps(report,indent=2))
else:raise ValueError(mode)
