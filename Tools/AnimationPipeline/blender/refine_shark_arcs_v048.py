"""Reduce accumulated tail-chain twist while retaining the original bind and cues."""
import bpy,sys,json,math
from pathlib import Path
from mathutils import Quaternion
ROOT=Path(r'D:/indiegameDev/unity/Dark Brine_Rot')
exec((ROOT/'Tools/AnimationPipeline/blender/build_encounter_v006.py').read_text().split("if MODE=='hero':")[0],globals())
OUT=ROOT/'ArtSource/Encounter/v048';OUT.mkdir(parents=True,exist_ok=True)
mode=sys.argv[-1]
if mode=='build':
    bpy.ops.wm.open_mainfile(filepath=str(ROOT/'ArtSource/Encounter/v045/shark-paired-v045.blend'))
    rig=bpy.data.objects['Tralalero_Rig'];rest={b.name:tuple(b.matrix_local) for b in rig.data.bones};specs=[];report=[]
    for suffix,seconds,hit in [('Tail',1.5,.65),('ShipSmash',1.5,.65),('Bite',1.3,.52),('Breach',3.2,1.55)]:
        source=bpy.data.actions['CH1_Shark'+suffix+'_v045']
        def modify(t,pose,rot):
            if suffix not in ('Tail','ShipSmash'):return
            # Individual local bends accumulate along this five-joint chain.
            # Keep a strong caudal whip without folding the skinned fin through
            # the rear trunk. The whole-body approach remains runtime-owned.
            for name,factor in [('Body',.85),('Spine_Mid',.7),('Spine_Rear',.65),('Tail_Base',.6),('Tail_Mid',.6),('Tail_Tip',.7),('Head',.85)]:
                loc,q,scale=pose[name];pose[name]=(loc,Quaternion().slerp(q,factor),scale)
        action=author(rig,source,'CH1_Shark'+suffix+'_v048',seconds,lambda t:1+t*60,modify)
        values=[sample(rig,action,i+1) for i in range(round(seconds*60)+1)]
        peaks={n:max(math.degrees(a[n][1].rotation_difference(b[n][1]).angle) for a,b in zip(values,values[1:])) for n in ['Body','Spine_Mid','Spine_Rear','Tail_Base','Tail_Mid','Tail_Tip']}
        report.append(dict(action=action.name,contact_seconds=hit,peak_degrees_per_frame=peaks));specs.append((action,seconds,False,hit))
    assert rest=={b.name:tuple(b.matrix_local) for b in rig.data.bones}
    save(rig,'shark-arcs-v048',specs,'Body','Generic','')
    path=OUT/'shark-arcs-v048-index.json';index=json.loads(path.read_text())
    for m in index:m.update(revision='v048',stage='FIRST PASS',design_reference='Documentation/AnimationWorkflow/Actions/CH1_SahurSharkEncounter_v001/design-v039.md',approval_reference='2026-10-12 user 开始实施计划')
    path.write_text(json.dumps(index,indent=2));(OUT/'source-audit.json').write_text(json.dumps(dict(rest_preserved=True,bones=16,actions=report),indent=2))
elif mode in ('preview','export'):
    index=json.loads((OUT/'shark-arcs-v048-index.json').read_text());bpy.ops.wm.open_mainfile(filepath=index[0]['blend']);rig=bpy.data.objects[index[0]['rig']];report=[]
    for m in index:
        action=bpy.data.actions[m['action']]
        if mode=='preview':
            if 'Tail' in action.name or 'Smash' in action.name:report.append(render(rig,action,ROOT/'.codex/opening-optimization-20261012/shark-source-v048'/action.name,[1,16,28,40,55,76,91],slot_for(action,rig),resolution=480))
        else:
            review=OUT/'source-review.json'
            if m['action'] not in json.loads(review.read_text())['accepted_for_staging']:continue
            m.update(review_status='accepted_for_stage',review_reference=str(review));report.append(export(rig,action,m,OUT/(action.name+'.fbx'),slot_for(action,rig)))
    (OUT/(mode+'-report.json')).write_text(json.dumps(report,indent=2))
else:raise ValueError(mode)
