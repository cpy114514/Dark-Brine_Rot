"""Targeted original-rig bite revision: remove cumulative rear-chain snap."""
import bpy,sys,json,math
from pathlib import Path
from mathutils import Quaternion
ROOT=Path(r'D:/indiegameDev/unity/Dark Brine_Rot')
exec((ROOT/'Tools/AnimationPipeline/blender/build_encounter_v006.py').read_text().split("if MODE=='hero':")[0],globals())
OUT=ROOT/'ArtSource/Encounter/v057';OUT.mkdir(parents=True,exist_ok=True)
mode=sys.argv[-1]
if mode=='build':
    bpy.ops.wm.open_mainfile(filepath=str(ROOT/'ArtSource/Encounter/v048/shark-arcs-v048.blend'))
    rig=bpy.data.objects['Tralalero_Rig'];rest={b.name:tuple(b.matrix_local) for b in rig.data.bones}
    source=bpy.data.actions['CH1_SharkBite_v048']
    original=[sample(rig,source,1+i) for i in range(79)]
    factors={'Spine_Rear':.7,'Tail_Base':.6,'Tail_Mid':.5,'Tail_Tip':.5}
    def modify(t,pose,rot):
        # Head/jaw and trunk contact stay exact. Delayed rear-spine motion
        # still propels the lunge, with less cumulative corkscrew and recoil.
        for name,factor in factors.items():
            loc,q,scale=pose[name];pose[name]=(loc,Quaternion().slerp(q,factor),scale)
    action=author(rig,source,'CH1_SharkBite_v057',1.3,lambda t:1+t*60,modify)
    samples=[sample(rig,action,1+i) for i in range(79)]
    peaks={n:max(math.degrees(a[n][1].rotation_difference(b[n][1]).angle) for a,b in zip(samples,samples[1:])) for n in factors}
    preserved=['Body','Head','Jaw','Nose_Marker','Fin_Left','Fin_Right']
    retained=max(math.degrees(a[n][1].rotation_difference(b[n][1]).angle) for a,b in zip(original,samples) for n in preserved)
    assert rest=={b.name:tuple(b.matrix_local) for b in rig.data.bones} and len(rest)==16 and retained<.001
    save(rig,'shark-bite-v057',[(action,1.3,False,.52)],'Body','Generic','')
    path=OUT/'shark-bite-v057-index.json';index=json.loads(path.read_text())
    for m in index:m.update(revision='v057',stage='FIRST PASS',design_reference='Documentation/AnimationWorkflow/Actions/CH1_SahurSharkEncounter_v001/design-v039.md',approval_reference='2026-10-12 user 开始实施计划; 其他的继续做')
    path.write_text(json.dumps(index,indent=2))
    (OUT/'source-audit.json').write_text(json.dumps(dict(rest_preserved=True,bones=16,contact_seconds=.52,head_jaw_trunk_max_difference_degrees=retained,peak_degrees_per_frame=peaks),indent=2))
elif mode in ('preview','export'):
    m=json.loads((OUT/'shark-bite-v057-index.json').read_text())[0]
    bpy.ops.wm.open_mainfile(filepath=m['blend']);rig=bpy.data.objects[m['rig']];action=bpy.data.actions[m['action']]
    if mode=='preview':report=render(rig,action,ROOT/'.codex/opening-optimization-20261012/shark-source-v057',[1,13,25,32.2,40,55,79],slot_for(action,rig),resolution=480)
    else:
        review=OUT/'source-review.json'
        if m['action'] not in json.loads(review.read_text())['accepted_for_staging']:raise ValueError('Source not reviewed')
        m.update(review_status='accepted_for_stage',review_reference=str(review));report=export(rig,action,m,OUT/(action.name+'.fbx'),slot_for(action,rig))
    (OUT/(mode+'-report.json')).write_text(json.dumps(report,indent=2))
else:raise ValueError(mode)
