"""Head-led bite reach on the preserved anatomy; no root/bind reconstruction."""
import bpy,sys,json,math
from pathlib import Path
ROOT=Path(r'D:/indiegameDev/unity/Dark Brine_Rot')
exec((ROOT/'Tools/AnimationPipeline/blender/build_encounter_v006.py').read_text().split("if MODE=='hero':")[0],globals())
OUT=ROOT/'ArtSource/Encounter/v069';OUT.mkdir(parents=True,exist_ok=True)
mode=sys.argv[-1]
if mode=='build':
    bpy.ops.wm.open_mainfile(filepath=str(ROOT/'ArtSource/Encounter/v057/shark-bite-v057.blend'))
    rig=bpy.data.objects['Tralalero_Rig'];source=bpy.data.actions['CH1_SharkBite_v057']
    rest={b.name:tuple(b.matrix_local) for b in rig.data.bones}
    original=[sample(rig,source,1+i) for i in range(79)]
    sample(rig,source,32.2);old_nose=rig.pose.bones['Nose_Marker'].matrix.translation.copy();old_body=rig.pose.bones['Body'].matrix.translation.copy()
    def modify(t,pose,rot):
        # Let the head reach the held shaft while the large trunk stays lower.
        # Preserve jaw closure, .52s contact and the damped propulsion chain.
        rot('Head',x=curve(t,[(0,0),(.2,-1),(.35,3),(.52,6),(.66,4.5),(.88,1.5),(1.12,.5),(1.3,0)]))
    action=author(rig,source,'CH1_SharkBite_v069',1.3,lambda t:1+t*60,modify)
    values=[sample(rig,action,1+i) for i in range(79)]
    preserved=[n for n in rest if n!='Head']
    retained=max(math.degrees(a[n][1].rotation_difference(b[n][1]).angle) for a,b in zip(original,values) for n in preserved)
    sample(rig,action,32.2);nose=rig.pose.bones['Nose_Marker'].matrix.translation.copy();body=rig.pose.bones['Body'].matrix.translation.copy()
    assert rest=={b.name:tuple(b.matrix_local) for b in rig.data.bones} and len(rest)==16 and retained<.001
    save(rig,'shark-bite-v069',[(action,1.3,False,.52)],'Body','Generic','')
    path=OUT/'shark-bite-v069-index.json';index=json.loads(path.read_text())
    for m in index:m.update(revision='v069',stage='FIRST PASS',design_reference='Documentation/AnimationWorkflow/Actions/CH1_SahurSharkEncounter_v001/design-v039.md',approval_reference='2026-10-12 user 把动画继续做完')
    path.write_text(json.dumps(index,indent=2))
    audit=dict(rest_preserved=True,bones=16,contact_seconds=.52,unchanged_other_channels_degrees=retained,source_nose_minus_body=list(old_nose-old_body),revised_nose_minus_body=list(nose-body),nose_rise_source_units=float(nose.z-old_nose.z),head_peak_degrees_per_frame=max(math.degrees(a['Head'][1].rotation_difference(b['Head'][1]).angle) for a,b in zip(values,values[1:])))
    (OUT/'source-audit.json').write_text(json.dumps(audit,indent=2));print(json.dumps(audit))
elif mode in ('preview','export'):
    m=json.loads((OUT/'shark-bite-v069-index.json').read_text())[0];bpy.ops.wm.open_mainfile(filepath=m['blend']);rig=bpy.data.objects[m['rig']];action=bpy.data.actions[m['action']]
    if mode=='preview':report=render(rig,action,ROOT/'.codex/opening-optimization-20261012/shark-source-v069',[1,13,22,32.2,40,55,79],slot_for(action,rig),resolution=480)
    else:
        review=OUT/'source-review.json'
        if action.name not in json.loads(review.read_text())['accepted_for_staging']:raise ValueError('Source not reviewed')
        m.update(review_status='accepted_for_stage',review_reference=str(review));report=export(rig,action,m,OUT/(action.name+'.fbx'),slot_for(action,rig))
    (OUT/(mode+'-report.json')).write_text(json.dumps(report,indent=2))
else:raise ValueError(mode)
