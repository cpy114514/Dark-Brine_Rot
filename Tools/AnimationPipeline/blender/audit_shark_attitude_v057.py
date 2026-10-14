"""Read-only original-rig bite attitude audit; never reconstruct or save the rig."""
import bpy,json,math
from pathlib import Path
from mathutils import Vector
root=Path(r'D:/indiegameDev/unity/Dark Brine_Rot')
bpy.ops.wm.open_mainfile(filepath=str(root/'ArtSource/Encounter/v048/shark-arcs-v048.blend'))
rig=bpy.data.objects['Tralalero_Rig'];rig.animation_data.action=bpy.data.actions['CH1_SharkBite_v048'];rows=[]
for seconds in [0,.2,.35,.52,.65,.9,1.3]:
    f=1+seconds*60;bpy.context.scene.frame_set(math.floor(f),subframe=f-math.floor(f));bpy.context.view_layer.update()
    points={n:rig.pose.bones[n].matrix.translation.copy() for n in ['Body','Head','Nose_Marker','Tail_Base','Tail_Marker']}
    def pitch(a,b):
        d=points[b]-points[a];return math.degrees(math.atan2(d.z,math.hypot(d.x,d.y)))
    rows.append(dict(seconds=seconds,head_pitch=pitch('Head','Nose_Marker'),trunk_pitch=pitch('Tail_Base','Head'),body_quaternion=list(rig.pose.bones['Body'].rotation_quaternion),head_quaternion=list(rig.pose.bones['Head'].rotation_quaternion),points={n:list(p) for n,p in points.items()}))
output=root/'.codex/opening-optimization-20261012/shark-attitude-audit-v057.json';output.write_text(json.dumps(rows,indent=2));print(json.dumps(rows,indent=2))
