import bpy, json
from pathlib import Path
root=Path(r'D:/indiegameDev/unity/Dark Brine_Rot')
bpy.ops.wm.open_mainfile(filepath=str(root/'ArtSource/Encounter/v020/sahur-board-paddle-v020.blend'))
rig=bpy.data.objects['Armature']; rows=[]
for name in ('PLAYER_BoardPaddle_v020','PLAYER_BoardPaddleReverse_v020'):
    rig.animation_data.action=bpy.data.actions[name]
    for frame in (1,13,24,39,53,64,73):
        bpy.context.scene.frame_set(frame)
        rows.append(dict(action=name,frame=frame,points={n:list(rig.pose.bones[n].matrix.translation) for n in ('pelvis','thigh_l','calf_l','foot_l','thigh_r','calf_r','foot_r')}))
print(json.dumps(rows))
