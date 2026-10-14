import bpy,json,sys
from pathlib import Path
ROOT=Path(r'D:/indiegameDev/unity/Dark Brine_Rot')
exec((ROOT/'Tools/AnimationPipeline/blender/build_encounter_v006.py').read_text().split("if MODE=='hero':")[0],globals())
bpy.ops.wm.open_mainfile(filepath=str(ROOT/'ArtSource/Encounter/v040/sahur-smooth-combat-v040.blend'))
rig=bpy.data.objects['Armature'];report={}
for suffix,frame in [('Guard',1),('Brace',35),('Parry',30)]:
    action=bpy.data.actions['CH1_Sahur'+suffix+'_v040'];sample(rig,action,frame)
    report[suffix]={'rig_matrix':list(map(list,rig.matrix_world)), 'bones':{n:list(rig.pose.bones['mixamorig:'+n].matrix.translation) for n in ['Hips','Head','RightShoulder','RightArm','RightForeArm','RightHand','LeftShoulder','LeftArm','LeftForeArm','LeftHand','LeftFoot','RightFoot']}}
(ROOT/'.codex/opening-optimization-20261012/source-poses-v040.json').write_text(json.dumps(report,indent=2))
