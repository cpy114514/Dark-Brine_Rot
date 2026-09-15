import bpy,json
from pathlib import Path
root=Path('D:/indie_game_dev/unity/Dark Brine_Rot/BlenderWork')
bpy.ops.wm.open_mainfile(filepath=str(root/'character_fixed.blend'))
body=bpy.data.objects['Character_Body'];rig=bpy.data.objects['Armature'];bat=bpy.data.objects['Hand_Bat']
bone_count=len(rig.data.bones);bat_world=bat.matrix_world.copy()
with bpy.data.libraries.load(str(root/'before_finger_correction.blend'),link=False) as (src,dst):
 dst.objects=['Character_Body']
source=dst.objects[0]
assert len(source.data.vertices)==205770
body.data=source.data.copy()
bpy.data.objects.remove(source,do_unlink=True)
assert len(rig.data.bones)==bone_count==73
assert bat.parent==rig and bat.parent_bone=='hand.R'
assert max(abs(bat.matrix_world[i][j]-bat_world[i][j]) for i in range(4) for j in range(4))<1e-6
(root/'finger_revert_report.json').write_text(json.dumps({'finger_mesh':'restored from before_finger_correction.blend','body_vertices':len(body.data.vertices),'bones':bone_count,'bat_parent':bat.parent_bone},indent=2))
bpy.ops.wm.save_as_mainfile(filepath=str(root/'character_fixed.blend'))
