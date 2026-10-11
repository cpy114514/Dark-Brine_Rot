"""Raise the authored head into a bite while the bulk and tail retain swimming attitude."""
import bpy,sys,json,math
from pathlib import Path
ROOT=Path(r'D:/indiegameDev/unity/Dark Brine_Rot')
exec((ROOT/'Tools/AnimationPipeline/blender/build_encounter_v006.py').read_text().split("if MODE=='hero':")[0],globals())
OUT=ROOT/'ArtSource/Encounter/v023';OUT.mkdir(parents=True,exist_ok=True)
mode=sys.argv[-1]
if mode=='build':
    bpy.ops.wm.open_mainfile(filepath=str(ROOT/'ArtSource/Encounter/v016/shark-skin-v016.blend'))
    rig=bpy.data.objects['Tralalero_Rig'];source=bpy.data.actions['CH1_SharkBite_v006']
    rest={b.name:tuple(b.matrix_local) for b in rig.data.bones}
    def modify(t,pose,rot):
        # Existing jaw closure, propulsion chain and original .52s contact stay
        # intact. The head leads the rise; its recoil settles after the body.
        rot('Head',x=curve(t,[(0,0),(.20,-2),(.32,3),(.52,20),(.68,13),(.90,2),(1.3,0)]))
        load=curve(t,[(0,0),(.28,.3),(.52,1),(.78,.45),(1.3,0)])
        rot('Fin_Left',y=load*6);rot('Fin_Right',y=-load*6)
    action=author(rig,source,'CH1_SharkBite_v023',1.3,lambda t:1+t*60,modify)
    if rest!={b.name:tuple(b.matrix_local) for b in rig.data.bones}:raise RuntimeError('Rest rig changed')
    save(rig,'shark-bite-v023',[(action,1.3,False,.52)],'Body','Generic','')
    rig.animation_data.action=action;bpy.context.scene.frame_set(32)
    (OUT/'source-audit.json').write_text(json.dumps(dict(original_bones_retained=len(rest),source='ArtSource/Encounter/v016/shark-skin-v016.blend',contact_seconds=.52,
        points={n:list(rig.pose.bones[n].matrix.translation) for n in ('Body','Head','Nose_Marker','Tail_Base','Tail_Marker')}),indent=2))
elif mode in ('preview','export'):
    m=json.loads((OUT/'shark-bite-v023-index.json').read_text())[0];bpy.ops.wm.open_mainfile(filepath=m['blend']);rig=bpy.data.objects[m['rig']];action=bpy.data.actions[m['action']]
    if mode=='preview':report=render(rig,action,ROOT/'.codex/encounter-v023/previews'/action.name,[1,13,22,32.2,42,55,79],slot_for(action,rig),resolution=480)
    else:
        review=OUT/'source-review.json';accepted=json.loads(review.read_text())
        if action.name not in accepted['accepted_for_staging']:raise ValueError('Source not reviewed')
        m.update(review_status='accepted_for_stage',review_reference=str(review),revision='v023')
        report=export(rig,action,m,OUT/(action.name+'.fbx'),slot_for(action,rig))
    (OUT/(mode+'-report.json')).write_text(json.dumps(report,indent=2))
else:raise ValueError(mode)
