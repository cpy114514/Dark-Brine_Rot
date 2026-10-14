"""Versioned original-rig tail overlap, with unchanged authored contact poses."""
import bpy,sys,json,math
from pathlib import Path
ROOT=Path(r'D:/indiegameDev/unity/Dark Brine_Rot')
exec((ROOT/'Tools/AnimationPipeline/blender/build_encounter_v006.py').read_text().split("if MODE=='hero':")[0],globals())
OUT=ROOT/'ArtSource/Encounter/v097';OUT.mkdir(parents=True,exist_ok=True)
mode=sys.argv[-1]
chain={'Body':0,'Spine_Mid':.012,'Spine_Rear':.025,'Tail_Base':.04,'Tail_Mid':.055,'Tail_Tip':.07,'Head':.018,'Fin_Left':.04,'Fin_Right':.05}
if mode=='build':
    bpy.ops.wm.open_mainfile(filepath=str(ROOT/'ArtSource/Encounter/v048/shark-arcs-v048.blend'))
    rig=bpy.data.objects['Tralalero_Rig'];rest={b.name:tuple(b.matrix_local) for b in rig.data.bones}
    specs=[];reports=[]
    for suffix in ('Tail','ShipSmash'):
        source=bpy.data.actions['CH1_Shark'+suffix+'_v048'];seconds=1.5
        warped=[]
        for frame in range(91):
            t=frame/60
            # Let the heavy trunk lead the tail during the readable coil.
            # The lag converges smoothly before the original .65s impact.
            anticipation=ease((t-.08)/.16)*(1-ease((t-.36)/.20))
            recovery=ease((t-.76)/.16)*(1-ease((t-1.20)/.30))
            row={}
            for bone,delay in chain.items():
                time=max(0,t-delay*(anticipation*.8+recovery))
                row[bone]=sample(rig,source,1+time*60)[bone]
            warped.append(row)
        def modify(t,pose,rotate):
            for bone,value in warped[round(t*60)].items():
                loc,q,scale=pose[bone]
                pose[bone]=(loc,value[1].copy(),scale)
        action=author(rig,source,'CH1_Shark'+suffix+'_v097',seconds,lambda t:1+t*60,modify)
        before=sample(rig,source,40);after=sample(rig,action,40)
        contact_error=max(math.degrees(before[n][1].rotation_difference(after[n][1]).angle) for n in before)
        assert contact_error<.001,contact_error
        values=[sample(rig,action,f+1) for f in range(91)]
        peaks={n:max(math.degrees(a[n][1].rotation_difference(b[n][1]).angle) for a,b in zip(values,values[1:])) for n in chain}
        original=[sample(rig,source,f+1) for f in range(91)]
        differences={n:max(math.degrees(a[n][1].rotation_difference(b[n][1]).angle) for a,b in zip(values,original)) for n in chain}
        assert max(peaks.values())<10,peaks
        reports.append(dict(action=action.name,contact_seconds=.65,contact_pose_error_degrees=contact_error,
                            peak_degrees_per_source_frame=peaks,maximum_revision_difference_degrees=differences))
        specs.append((action,seconds,False,.65))
    assert rest=={b.name:tuple(b.matrix_local) for b in rig.data.bones}
    save(rig,'shark-tail-inertia-v097',specs,'Body','Generic','')
    index_path=OUT/'shark-tail-inertia-v097-index.json';index=json.loads(index_path.read_text())
    for entry in index:
        entry.update(revision='v097',stage='FIRST PASS',design_reference='Documentation/AnimationWorkflow/Actions/CH1_SahurSharkEncounter_v001/design-v039.md',approval_reference='2026-10-12 user 开始实施计划')
    index_path.write_text(json.dumps(index,indent=2))
    (OUT/'source-audit.json').write_text(json.dumps(dict(original_rig=True,rest_preserved=True,bones=len(rig.data.bones),actions=reports),indent=2))
elif mode in ('preview','export'):
    index=json.loads((OUT/'shark-tail-inertia-v097-index.json').read_text());bpy.ops.wm.open_mainfile(filepath=index[0]['blend']);rig=bpy.data.objects[index[0]['rig']];reports=[]
    for entry in index:
        action=bpy.data.actions[entry['action']]
        if mode=='preview':
            reports.append(render(rig,action,ROOT/'.codex/opening-optimization-20261012/shark-source-v097'/action.name,[1,16,28,34,40,55,67,79,91],slot_for(action,rig),resolution=480))
        else:
            review=OUT/'source-review.json'
            if entry['action'] not in json.loads(review.read_text())['accepted_for_staging']:continue
            entry.update(review_status='accepted_for_stage',review_reference=str(review));reports.append(export(rig,action,entry,OUT/(entry['action']+'.fbx'),slot_for(action,rig)))
    (OUT/(mode+'-report.json')).write_text(json.dumps(reports,indent=2))
else:raise ValueError(mode)
