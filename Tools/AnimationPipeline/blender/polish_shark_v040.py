"""Paired creature pass on the original 16-bone shark, preserving contact clocks."""
import bpy,sys,json,math
from pathlib import Path
ROOT=Path(r'D:/indiegameDev/unity/Dark Brine_Rot')
exec((ROOT/'Tools/AnimationPipeline/blender/build_encounter_v006.py').read_text().split("if MODE=='hero':")[0],globals())
OUT=ROOT/'ArtSource/Encounter/v040';OUT.mkdir(parents=True,exist_ok=True)
mode=sys.argv[-1]
if mode=='build':
    bpy.ops.wm.open_mainfile(filepath=str(ROOT/'ArtSource/Encounter/v037/shark-lift-v037.blend'))
    rig=bpy.data.objects['Tralalero_Rig'];rest={b.name:tuple(b.matrix_local) for b in rig.data.bones};specs=[];report=[]
    for path,names in [('v023/shark-bite-v023.blend',['CH1_SharkBite_v023']),('v003/shark-breach-v003.blend',['CH1_SharkBreach_v003'])]:
        with bpy.data.libraries.load(str(ROOT/'ArtSource/Encounter'/path),link=False) as (src,dst):dst.actions=names
    for suffix,sourceName,seconds,hit in [('Tail','CH1_SharkTail_v037',1.5,.65),('ShipSmash','CH1_SharkShipSmash_v037',1.5,.65),('Bite','CH1_SharkBite_v023',1.3,.52),('Breach','CH1_SharkBreach_v003',3.2,1.55)]:
        source=bpy.data.actions[sourceName];sourceFPS=60
        def modify(t,pose,rot):
            if suffix in ('Tail','ShipSmash'):
                power=1.12 if suffix=='ShipSmash' else 1
                # A submerged counter-bend loads the rear spine, then travels
                # to the tip. Fins and jaw lag instead of stopping in unison.
                for j,(bone,amplitude) in enumerate([('Spine_Mid',2.5),('Spine_Rear',4),('Tail_Base',6),('Tail_Mid',7),('Tail_Tip',9)]):
                    d=j*.018
                    rot(bone,z=power*curve(t,[(0,0),(.24+d,-amplitude),(.42+d,-amplitude*.6),(.61+d,amplitude),(.79+d,amplitude*.45),(1.05+d,-amplitude*.2),(1.5,0)]))
                rot('Fin_Left',y=curve(t,[(0,0),(.33,5),(.69,-4),(1.08,2),(1.5,0)]))
                rot('Fin_Right',y=curve(t,[(0,0),(.4,-4),(.76,6),(1.16,-2),(1.5,0)]))
                rot('Head',z=curve(t,[(0,0),(.34,3),(.65,-2),(1.1,1),(1.5,0)]))
            elif suffix=='Bite':
                rot('Body',z=curve(t,[(0,0),(.18,-2),(.42,2),(.7,1),(1.3,0)]))
                rot('Jaw',x=curve(t,[(0,0),(.26,4),(.43,6),(.52,0),(.66,2),(1.3,0)]))
                for j,bone in enumerate(['Spine_Mid','Spine_Rear','Tail_Base','Tail_Mid','Tail_Tip']):
                    d=j*.025;rot(bone,z=curve(t,[(0,0),(.22+d,-2-j),(.47+d,3+j),(.76+d,-1-j*.5),(1.3,0)]))
            else:
                rot('Body',x=curve(t,[(0,0),(.5,2),(.9,-4),(1.4,-2),(1.85,5),(2.35,2),(3.2,0)]),y=curve(t,[(0,0),(.9,-3),(1.5,4),(2.1,1),(3.2,0)]))
                for j,bone in enumerate(['Spine_Mid','Spine_Rear','Tail_Base','Tail_Mid','Tail_Tip']):
                    d=j*.045;rot(bone,x=curve(t,[(0,0),(.7+d,-2-j*.5),(1.3+d,3+j),(1.9+d,-2-j),(2.45+d,1+j*.3),(3.2,0)]))
                rot('Fin_Left',y=curve(t,[(0,0),(.9,8),(1.65,-5),(2.3,2),(3.2,0)]))
                rot('Fin_Right',y=curve(t,[(0,0),(1,-6),(1.8,7),(2.5,-2),(3.2,0)]))
        action=author(rig,source,'CH1_Shark'+suffix+'_v040',seconds,lambda t:1+t*sourceFPS,modify)
        samples=[sample(rig,action,i+1) for i in range(round(seconds*60)+1)]
        peak={n:max(math.degrees(a[n][1].rotation_difference(b[n][1]).angle) for a,b in zip(samples,samples[1:])) for n in ['Body','Spine_Mid','Spine_Rear','Tail_Base','Tail_Mid','Tail_Tip','Head']}
        report.append(dict(action=action.name,source=sourceName,peak_degrees_per_frame=peak,contact_seconds=hit));specs.append((action,seconds,False,hit))
    assert len(rest)==16 and rest=={b.name:tuple(b.matrix_local) for b in rig.data.bones}
    save(rig,'shark-paired-v040',specs,'Body','Generic','')
    path=OUT/'shark-paired-v040-index.json';index=json.loads(path.read_text())
    for m in index:m.update(revision='v040',stage='BLOCKING',design_reference='Documentation/AnimationWorkflow/Actions/CH1_SahurSharkEncounter_v001/design-v039.md',approval_reference='2026-10-12 user 开始实施计划')
    path.write_text(json.dumps(index,indent=2));(OUT/'shark-source-audit.json').write_text(json.dumps(dict(original_rest_retained=True,bones=list(rest),actions=report),indent=2))
elif mode in ('preview','preview-apex','export'):
    index=json.loads((OUT/'shark-paired-v040-index.json').read_text());bpy.ops.wm.open_mainfile(filepath=index[0]['blend']);rig=bpy.data.objects[index[0]['rig']];report=[]
    for m in index:
        action=bpy.data.actions[m['action']]
        if mode=='preview-apex':
            if 'Breach' in action.name:report.append(render(rig,action,ROOT/'.codex/opening-optimization-20261012/shark-apex-preview'/action.name,[1,31,65,94,118,145,193],slot_for(action,rig),resolution=384))
        elif mode=='preview':report.append(render(rig,action,ROOT/'.codex/opening-optimization-20261012/shark-source-preview'/action.name,[1,16,28,40,55,76,m['frame_end']],slot_for(action,rig),resolution=384))
        else:
            review=OUT/'shark-source-review.json';accepted=json.loads(review.read_text())['accepted_for_staging']
            if m['action'] not in accepted:continue
            m.update(review_status='accepted_for_stage',review_reference=str(review));report.append(export(rig,action,m,OUT/(action.name+'.fbx'),slot_for(action,rig)))
    (OUT/('shark-'+mode+'-report.json')).write_text(json.dumps(report,indent=2))
else:raise ValueError(mode)
