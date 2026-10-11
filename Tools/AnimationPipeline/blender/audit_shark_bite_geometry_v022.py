import bpy,json,math
from pathlib import Path
from mathutils import Quaternion
root=Path(r'D:/indiegameDev/unity/Dark Brine_Rot')
bpy.ops.wm.open_mainfile(filepath=str(root/'ArtSource/Encounter/v016/shark-skin-v016.blend'))
rig=bpy.data.objects['Tralalero_Rig'];rig.animation_data.action=bpy.data.actions['CH1_SharkBite_v006']
rows=[]
for angle in (0,-25,25):
    bpy.context.scene.frame_set(32)
    p=rig.pose.bones['Head'];p.rotation_quaternion=p.rotation_quaternion@Quaternion((1,0,0),math.radians(angle));bpy.context.view_layer.update()
    rows.append(dict(extra_head_pitch=angle,points={n:list(rig.pose.bones[n].matrix.translation) for n in ('Body','Head','Nose_Marker','Tail_Base','Tail_Marker')}))
print(json.dumps(rows))
