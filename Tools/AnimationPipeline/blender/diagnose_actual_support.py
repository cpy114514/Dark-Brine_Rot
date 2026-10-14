import bpy,json
from pathlib import Path
from mathutils import Vector
ROOT=Path(r'D:/indiegameDev/unity/Dark Brine_Rot')
bpy.ops.wm.open_mainfile(filepath=str(ROOT/'ArtSource/Encounter/v046/sahur-actual-rig-blocking-v046.blend'))
rig=bpy.data.objects['Armature'];rig.animation_data.action=bpy.data.actions['CH1_SahurGuard_v046'];bpy.context.scene.frame_set(1)
report={'hips':dict(location=list(rig.pose.bones['Hips'].location),matrix_position=list(rig.pose.bones['Hips'].matrix.translation),rest_position=list(rig.data.bones['Hips'].head_local)), 'root':dict(location=list(rig.pose.bones['root'].location),rotation=list(rig.pose.bones['root'].rotation_quaternion))}
for side,sign in [('Left',1),('Right',-1)]:
    h,k,f=[rig.pose.bones[side+n].matrix.translation.copy() for n in ['UpLeg','Leg','Foot']]
    target=rig.data.bones[side+'Foot'].head_local.copy();target.x=sign*20;target.y+=-10 if side=='Left' else 9
    report[side]=dict(hip=list(h),knee=list(k),ankle=list(f),target=list(target),upper=(k-h).length,lower=(f-k).length,reach=(target-h).length,error=(f-target).length)
(ROOT/'.codex/opening-optimization-20261012/actual-support-diagnosis.json').write_text(json.dumps(report,indent=2))
