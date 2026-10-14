"""Read actual retained shark anatomy before choosing a targeted tail revision."""
import bpy, json, math, sys
from pathlib import Path
from mathutils import Quaternion, Vector
ROOT=Path(r'D:/indiegameDev/unity/Dark Brine_Rot')
exec((ROOT/'Tools/AnimationPipeline/blender/build_encounter_v006.py').read_text().split("if MODE=='hero':")[0],globals())
bpy.ops.wm.open_mainfile(filepath=str(ROOT/'ArtSource/Encounter/v048/shark-arcs-v048.blend'))
rig=bpy.data.objects['Tralalero_Rig'];action=bpy.data.actions['CH1_SharkTail_v048']
report=[]
for seconds in (0,.25,.45,.65,.8,1,1.5):
    sample(rig,action,1+60*seconds)
    report.append(dict(seconds=seconds,bones={n:dict(position=list(p.matrix.translation),
        local_axis_x=list(p.matrix.to_quaternion()@Vector((1,0,0))),
        local_axis_z=list(p.matrix.to_quaternion()@Vector((0,0,1))),
        quaternion=list(p.rotation_quaternion)) for n,p in rig.pose.bones.items()}))
path=ROOT/'.codex/opening-optimization-20261012/tail-anatomy-v065.json'
path.write_text(json.dumps(dict(rig_matrix=list(map(list,rig.matrix_world)),samples=report),indent=2))
print(str(path))
