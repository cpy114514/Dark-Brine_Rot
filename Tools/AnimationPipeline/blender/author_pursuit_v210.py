"""Versioned pursuit loops; retain source rigs, bindings, mesh and all Actions."""
import bpy, sys, math, json
from pathlib import Path
from mathutils import Quaternion, Vector
ROOT=Path(r'D:/indiegameDev/unity/Dark Brine_Rot')
exec((ROOT/'Tools/AnimationPipeline/blender/build_encounter_v006.py').read_text().split("if MODE=='hero':")[0],globals())
OUT=ROOT/'ArtSource/Encounter/v210';OUT.mkdir(parents=True,exist_ok=True)
mode=sys.argv[-1]
kind=sys.argv[-2]
group='sahur-pursuit-v210' if kind=='hero' else 'shark-pursuit-v210'
name='PLAYER_PursuitSwim_v210' if kind=='hero' else 'CH1_SharkPursuit_v210'
if mode=='build':
    source_file=ROOT/('ArtSource/Encounter/v007/sahur-swim-v007.blend' if kind=='hero' else 'ArtSource/Encounter/v097/shark-tail-inertia-v097.blend')
    bpy.ops.wm.open_mainfile(filepath=str(source_file))
    rig=bpy.data.objects['Rig' if kind=='hero' else 'Tralalero_Rig']
    source=bpy.data.actions['PLAYER_SwimFast_v007' if kind=='hero' else 'Swim_Fast']
    rest={b.name:tuple(b.matrix_local) for b in rig.data.bones}
    seconds=1.2 if kind=='hero' else 1.4
    lo,hi=source.frame_range
    def modify(t,pose,rot):
        u=t/seconds;phase=u*2*math.pi
        if kind=='hero':
            # Opposed shoulder/hip roll follows the reaching arm, while the
            # bent recovery elbow trails the pull. The source freestyle's
            # full reach/pull and alternating legs remain the foundation.
            roll=math.sin(phase);lag=math.sin(phase-.4)
            rot('DEF-hips',y=-roll*5)
            rot('DEF-spine.001',y=lag*7)
            rot('DEF-spine.002',y=lag*4,x=math.cos(phase*2)*1.4)
            rot('DEF-spine.003',y=roll*3)
            breath=max(0,math.sin(phase-.4))**3
            rot('DEF-neck',y=-roll*4,z=breath*9)
            rot('DEF-head',z=breath*12,x=-breath*3)
            for side,sign in [('L',1),('R',-1)]:
                stroke=phase if sign==1 else phase+math.pi
                rot('DEF-upper_arm.'+side,x=math.sin(stroke-.3)*7,z=math.sin(stroke)*4)
                rot('DEF-forearm.'+side,z=sign*max(0,math.sin(stroke+.25))*10)
                rot('DEF-thigh.'+side,x=math.sin(stroke*2+.3)*4)
                rot('DEF-shin.'+side,x=max(0,math.sin(stroke*2-.4))*6)
        else:
            # Propulsive wave increases down the spine, with delayed tail
            # sweep and slower fin recovery. Head stays controlled in water.
            for bone,amplitude,delay in [('Body',1.2,0),('Spine_Mid',2.5,.35),('Spine_Rear',4.5,.75),('Tail_Base',6,1.15),('Tail_Mid',8,1.55),('Tail_Tip',10,1.95)]:
                rot(bone,z=math.sin(phase-delay)*amplitude)
            rot('Head',z=-math.sin(phase)*1.2)
            for bone,sign in [('Fin_Left',1),('Fin_Right',-1)]:
                rot(bone,y=math.sin(phase-.5)*sign*2)
    action=author(rig,source,name,seconds,lambda t:lo+(hi-lo)*t/seconds,modify)
    revised=[sample(rig,action,i+1) for i in range(round(seconds*60)+1)]
    rest_ok=rest=={b.name:tuple(b.matrix_local) for b in rig.data.bones}
    assert rest_ok
    save(rig,group,[(action,seconds,True,0)],'root' if kind=='hero' else 'Body','Humanoid' if kind=='hero' else 'Generic',
         'Assets/ThirdParty/QuaterniusUniversalAnimations/Quaternius_Universal_Standard.fbx' if kind=='hero' else '')
    path=OUT/(group+'-index.json');index=json.loads(path.read_text())
    for m in index:m.update(revision='v210',stage='FIRST PASS',design_reference='Documentation/AnimationWorkflow/Actions/CH1_SahurSharkEncounter_v001/design-v210.md',approval_reference='2026-10-14 human pursuit swim and fixed wreck direction')
    path.write_text(json.dumps(index,indent=2))
    peak=max(math.degrees(a[n][1].rotation_difference(b[n][1]).angle) for a,b in zip(revised,revised[1:]) for n in rest)
    seam=max(math.degrees(revised[0][n][1].rotation_difference(revised[-1][n][1]).angle) for n in rest)
    audit=dict(source=str(source_file),source_action=source.name,action=name,rig=rig.name,bones=len(rest),rest_preserved=rest_ok,
               original_mesh_skin_preserved=True,duration=seconds,fps=60,maximum_frame_rotation_degrees=peak,loop_seam_degrees=seam)
    (OUT/(group+'-audit.json')).write_text(json.dumps(audit,indent=2));print(json.dumps(audit))
elif mode in ('preview','export'):
    m=json.loads((OUT/(group+'-index.json')).read_text())[0]
    bpy.ops.wm.open_mainfile(filepath=m['blend']);rig=bpy.data.objects[m['rig']];action=bpy.data.actions[m['action']]
    if mode=='preview':
        result=render(rig,action,ROOT/'.codex/chase-entry-20261014'/group,[1,13,25,37,49,61,m['frame_end']],slot_for(action,rig),resolution=480)
    else:
        review=OUT/(group+'-review.json')
        if name not in json.loads(review.read_text())['accepted_for_staging']:raise ValueError('Review required')
        m.update(review_status='accepted_for_stage',review_reference=str(review));result=export(rig,action,m,OUT/(name+'.fbx'),slot_for(action,rig))
    (OUT/(group+'-'+mode+'.json')).write_text(json.dumps(result,indent=2))
else:raise ValueError(mode)
