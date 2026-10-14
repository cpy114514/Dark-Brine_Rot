"""Original-rig body-led bite revision, staged separately from runtime assets."""
import bpy,sys,json,math
from pathlib import Path
ROOT=Path(r'D:/indiegameDev/unity/Dark Brine_Rot')
exec((ROOT/'Tools/AnimationPipeline/blender/build_encounter_v006.py').read_text().split("if MODE=='hero':")[0],globals())
OUT=ROOT/'ArtSource/Encounter/v078';OUT.mkdir(parents=True,exist_ok=True)
mode=sys.argv[-1]
if mode=='build':
    bpy.ops.wm.open_mainfile(filepath=str(ROOT/'ArtSource/Encounter/v057/shark-bite-v057.blend'))
    rig=bpy.data.objects['Tralalero_Rig'];source=bpy.data.actions['CH1_SharkBite_v057']
    required={'Body','Head','Spine_Mid','Jaw','Nose_Marker'}
    assert required.issubset(set(rig.pose.bones.keys()))
    rest={b.name:tuple(b.matrix_local) for b in rig.data.bones}
    hierarchy={b.name:b.parent.name if b.parent else None for b in rig.data.bones}
    original=[sample(rig,source,1+i) for i in range(79)]
    sample(rig,source,32.2);old=rig.pose.bones['Nose_Marker'].matrix.translation-rig.pose.bones['Body'].matrix.translation
    def body_lift(t):return curve(t,[(0,0),(.14,-.7),(.32,1.5),(.52,4),(.65,3.5),(.95,-1),(1.3,0)])
    def modify(t,pose,rot):
        # Distribute the reaching attitude through the existing body/head
        # articulation. Do not create a front spine or change the bind rig.
        # The rear spine follows later and counterbends rather than raising
        # the entire tail as a stiff lever; jaw closure remains exact.
        rot('Body',x=-body_lift(t))
        rot('Head',x=curve(t,[(0,0),(.2,-1),(.35,2),(.52,4),(.66,3.5),(.88,1),(1.12,.3),(1.3,0)]))
        rot('Spine_Mid',x=.6*body_lift(max(0,t-.08))*ease((1.3-t)/.1))
    action=author(rig,source,'CH1_SharkBite_v078',1.3,lambda t:1+t*60,modify)
    values=[sample(rig,action,1+i) for i in range(79)]
    changed={'Body','Head','Spine_Mid'}
    retained=max(math.degrees(a[n][1].rotation_difference(b[n][1]).angle) for a,b in zip(original,values) for n in rest if n not in changed)
    sample(rig,action,32.2);new=rig.pose.bones['Nose_Marker'].matrix.translation-rig.pose.bones['Body'].matrix.translation
    assert rest=={b.name:tuple(b.matrix_local) for b in rig.data.bones} and len(rest)==16 and retained<.001
    save(rig,'shark-bite-v078',[(action,1.3,False,.52)],'Body','Generic','')
    path=OUT/'shark-bite-v078-index.json';index=json.loads(path.read_text())
    for m in index:m.update(revision='v078',stage='FIRST PASS',design_reference='Documentation/AnimationWorkflow/Actions/CH1_SahurSharkEncounter_v001/design-v039.md',approval_reference='2026-10-12 user 把动画继续做完',review_status='pending')
    path.write_text(json.dumps(index,indent=2))
    audit=dict(rest_preserved=True,bones=16,hierarchy=hierarchy,contact_seconds=.52,unchanged_other_channels_degrees=retained,source_nose_minus_body=list(old),revised_nose_minus_body=list(new),changed_channels=sorted(changed),runtime_activated=False,
        peak_degrees_per_frame={n:max(math.degrees(a[n][1].rotation_difference(b[n][1]).angle) for a,b in zip(values,values[1:])) for n in changed})
    (OUT/'source-audit.json').write_text(json.dumps(audit,indent=2));print(json.dumps(audit))
elif mode in ('preview','export'):
    m=json.loads((OUT/'shark-bite-v078-index.json').read_text())[0];bpy.ops.wm.open_mainfile(filepath=m['blend']);rig=bpy.data.objects[m['rig']];action=bpy.data.actions[m['action']]
    if mode=='preview':report=render(rig,action,ROOT/'.codex/opening-optimization-20261012/shark-source-v078',[1,13,22,32.2,40,55,79],slot_for(action,rig),resolution=480)
    else:
        review=OUT/'source-review.json'
        if action.name not in json.loads(review.read_text())['accepted_for_staging']:raise ValueError('Source not reviewed')
        m.update(review_status='accepted_for_stage',review_reference=str(review));report=export(rig,action,m,OUT/(action.name+'.fbx'),slot_for(action,rig))
    (OUT/(mode+'-report.json')).write_text(json.dumps(report,indent=2))
else:raise ValueError(mode)
