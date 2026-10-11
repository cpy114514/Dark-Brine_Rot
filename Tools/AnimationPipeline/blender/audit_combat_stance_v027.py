"""Read actual retained Mixamo combat poses before a versioned stance pass."""
import bpy,json
from pathlib import Path
ROOT=Path(r'D:/indiegameDev/unity/Dark Brine_Rot')
rows=[]
for version,file,action,time in [
    ('v006','sahur-combat-v006.blend','CH1_SahurGuard_v006',0),
    ('v006','sahur-combat-v006.blend','CH1_SahurSlash_v006',.63),
    ('v006','sahur-combat-v006.blend','CH1_SahurBrace_v006',.572),
    ('v008','sahur-parry-v008.blend','CH1_SahurParry_v008',.484)]:
    bpy.ops.wm.open_mainfile(filepath=str(ROOT/'ArtSource/Encounter'/version/file))
    rig=bpy.data.objects['Armature'];rig.animation_data.action=bpy.data.actions[action]
    bpy.context.scene.frame_set(int(time*60)+1,subframe=time*60%1)
    names=['Hips','LeftUpLeg','LeftLeg','LeftFoot','RightUpLeg','RightLeg','RightFoot']
    rows.append(dict(action=action,scale=list(rig.scale),height=rig.dimensions.z,
        positions={n:list(rig.pose.bones['mixamorig:'+n].matrix.translation) for n in names}))
folder=ROOT/'ArtSource/Encounter/v027';folder.mkdir(parents=True,exist_ok=True)
(folder/'stance-audit.json').write_text(json.dumps(rows,indent=2))
print(json.dumps(rows))
