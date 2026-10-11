"""Lift the rear spine/tail for contact instead of pitching the entire shark upright."""
import bpy,sys,json,math
from pathlib import Path
from mathutils import Vector
ROOT=Path(r'D:/indiegameDev/unity/Dark Brine_Rot')
exec((ROOT/'Tools/AnimationPipeline/blender/build_encounter_v006.py').read_text().split("if MODE=='hero':")[0],globals())
OUT=ROOT/'ArtSource/Encounter/v019';OUT.mkdir(parents=True,exist_ok=True)
mode=sys.argv[-1]
if mode=='build':
    bpy.ops.wm.open_mainfile(filepath=str(ROOT/'ArtSource/Encounter/v016/shark-skin-v016.blend'))
    rig=bpy.data.objects['Tralalero_Rig'];specs=[];report=[]
    original_bones=[b.name for b in rig.data.bones]
    for suffix,power in [('Tail',1),('ShipSmash',1.12)]:
        source=bpy.data.actions['CH1_Shark'+suffix+'_v006']
        def modify(t,pose,rot):
            # A small submerged countermove precedes the rising tail; the
            # body/head stay level while progressively later rear joints lift.
            for bone,amplitude,delay in [('Spine_Mid',3,0),('Spine_Rear',7,.018),('Tail_Base',22,.035),('Tail_Mid',16,.055),('Tail_Tip',-7,.075)]:
                lift=curve(t,[(0,0),(.23+delay,-amplitude*.28),(.40+delay,-amplitude*.15),(.59+delay,amplitude),(.78+delay,amplitude*.63),(1.07+delay,-amplitude*.12),(1.5,0)])
                rot(bone,x=lift*power)
            rot('Head',x=curve(t,[(0,0),(.3,-1.2),(.65,1.5),(.98,-.5),(1.5,0)]))
        action=author(rig,source,'CH1_Shark'+suffix+'_v019',1.5,lambda t:t*60+1,modify)
        specs.append((action,1.5,False,.65))
        rig.animation_data.action=action;bpy.context.scene.frame_set(40)
        report.append({'action':action.name,'body':list(rig.pose.bones['Body'].matrix.translation),'tail_tip':list(rig.pose.bones['Tail_Tip'].matrix.translation),'root_bone_unchanged':True})
    save(rig,'shark-lift-v019',specs,'Body','Generic','')
    (OUT/'source-audit.json').write_text(json.dumps({'bones_retained':original_bones,'skin_source':'ArtSource/Encounter/v016/shark-skin-v016.blend','contact_poses':report},indent=2))
elif mode in ('preview','export'):
    index=json.loads((OUT/'shark-lift-v019-index.json').read_text());bpy.ops.wm.open_mainfile(filepath=index[0]['blend']);rig=bpy.data.objects[index[0]['rig']];reports=[]
    for m in index:
        action=bpy.data.actions[m['action']]
        if mode=='preview':reports.append(render(rig,action,ROOT/'.codex/encounter-v019/previews'/action.name,[1,16,28,40,53,68,91],slot_for(action,rig),resolution=480))
        else:
            review=OUT/'source-review.json';accepted=json.loads(review.read_text())
            if m['action'] not in accepted['accepted_for_staging']:raise ValueError('Source not reviewed')
            m.update(review_status='accepted_for_stage',review_reference=str(review),revision='v019')
            reports.append(export(rig,action,m,OUT/(action.name+'.fbx'),slot_for(action,rig)))
    (OUT/(mode+'-report.json')).write_text(json.dumps(reports,indent=2))
else:raise ValueError(mode)
