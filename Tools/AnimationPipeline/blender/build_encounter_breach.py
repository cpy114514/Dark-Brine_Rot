"""Breach on a copy of the production shark rig; no reconstruction or overwrites."""
import bpy,math,json,sys
from pathlib import Path
ROOT=Path(r'D:/indiegameDev/unity/Dark Brine_Rot')
sys.path.insert(0,str(ROOT/'Tools/AnimationPipeline/blender'))
from common import curve_records,slot_for
from render_animation_preview import render
from export_unity_fbx import export
OUT=ROOT/'ArtSource/Encounter/v003';OUT.mkdir(parents=True,exist_ok=True)
NAME='CH1_SharkBreach_v003'
def track(t,keys):
    for (a,x),(b,y) in zip(keys,keys[1:]):
        if t<=b:
            u=max(0,min(1,(t-a)/(b-a)));return x+(y-x)*u*u*(3-2*u)
    return keys[-1][1]
if sys.argv[-1]=='export':
    rig=bpy.data.objects['Tralalero_Rig'];action=bpy.data.actions[NAME]
    manifest=json.loads((OUT/'breach-manifest.json').read_text())
    report=export(rig,action,manifest,OUT/(NAME+'.fbx'),slot_for(action,rig))
    (OUT/'export-report.json').write_text(json.dumps(report,indent=2))
else:
    if (OUT/'shark-breach-v003.blend').exists():raise FileExistsError('Revision exists')
    bpy.ops.wm.open_mainfile(filepath=str(ROOT/'ArtSource/Encounter/v001/shark-encounter-v001.blend'))
    rig=bpy.data.objects['Tralalero_Rig']
    audit={'source':bpy.data.filepath,'bones':[p.name for p in rig.pose.bones],
           'actions':[(a.name,list(a.frame_range)) for a in bpy.data.actions]}
    (OUT/'source-audit.json').write_text(json.dumps(audit,indent=2))
    action=bpy.data.actions.new(NAME);action.use_fake_user=True;rig.animation_data.action=action
    for f in range(193):
        t=f/60;bpy.context.scene.frame_set(f+1)
        for p in rig.pose.bones:p.rotation_mode='XYZ';p.location=(0,0,0);p.rotation_euler=(0,0,0)
        def rot(b,x=0,y=0,z=0):rig.pose.bones[b].rotation_euler=tuple(math.radians(v) for v in (x,y,z))
        rot('Body',x=track(t,[(0,0),(.35,-4),(.8,6),(1.35,4),(2,-5),(2.65,-2),(3.2,0)]))
        for i,b in enumerate(['Spine_Mid','Spine_Rear','Tail_Base','Tail_Mid','Tail_Tip']):
            delay=i*.065;amplitude=4+i*2.7
            rot(b,x=track(t,[(0,0),(.38+delay,-amplitude*.6),(.78+delay,amplitude),(1.42+delay,amplitude*.25),
                (2.05+delay,-amplitude*.65),(2.65+delay,amplitude*.2),(3.2,0)]),
                z=track(t,[(0,0),(.35+delay,amplitude*.45),(.8+delay,-amplitude*.6),(1.65+delay,amplitude*.3),(2.5+delay,-amplitude*.15),(3.2,0)]))
        rot('Head',x=track(t,[(0,0),(.45,-5),(.95,7),(1.45,4),(2.1,-6),(2.65,-2),(3.2,0)]))
        rot('Jaw',x=track(t,[(0,0),(.5,10),(1.1,26),(1.6,8),(2.05,2),(3.2,0)]))
        for side,sign in [('Left',1),('Right',-1)]:
            rot('Fin_'+side,x=track(t,[(0,0),(.7,-5),(1.3,7),(2.2,-6),(3.2,0)]),
                y=sign*track(t,[(0,0),(.5,10),(1.15,23),(1.8,16),(2.45,3),(3.2,0)]))
        rot('Dorsal',y=track(t,[(0,0),(.75,-3),(1.55,4),(2.5,-2),(3.2,0)]))
        for p in rig.pose.bones:
            if p.bone.use_deform:p.keyframe_insert(data_path='rotation_euler',frame=f+1,group=p.name)
    for _,curve in curve_records(action):
        for key in curve.keyframe_points:key.interpolation='LINEAR'
    bpy.context.scene.render.fps=60;bpy.context.scene.frame_start=1;bpy.context.scene.frame_end=193
    bpy.context.preferences.filepaths.save_version=0
    bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'shark-breach-v003.blend'))
    preview=render(rig,action,ROOT/'.codex/encounter-v003/blender-breach',[1,31,61,91,127,163,193],slot_for(action,rig),resolution=640)
    (OUT/'preview-report.json').write_text(json.dumps(preview,indent=2))
    manifest=dict(action=NAME,unity_clip=NAME,revision='v003',frame_start=1,frame_end=193,fps=60,loop=False,
        root_motion='in_place',root_bone='Body',rig_type='Generic',stage='SECOND PASS',design_status='approval_waived',
        design_reference='Documentation/AnimationWorkflow/Actions/CH1_SahurSharkEncounter_v001/traversal-v003-design.md',
        approval_reference='User 2026-10-07 requested board hopping and shark breaches; autonomous production/review waiver persists',
        review_status='accepted_for_stage',review_reference='Documentation/AnimationWorkflow/Actions/CH1_SahurSharkEncounter_v001/traversal-v003-review.md',
        gameplay={'startup':[1,43],'active':[43,127],'recovery':[127,193]})
    (OUT/'breach-manifest.json').write_text(json.dumps(manifest,indent=2))
