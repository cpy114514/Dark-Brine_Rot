import bpy, json
from pathlib import Path
root=Path(r'D:/indiegameDev/unity/Dark Brine_Rot')
bpy.ops.import_scene.fbx(filepath=str(root/'Assets/Game/Prefabs/Characters/Sahur/Animations/Source/StandingMeleeAttackDownward.fbx'))
rig=next(o for o in bpy.context.scene.objects if o.type=='ARMATURE')
print('ENCOUNTER_SOURCE '+json.dumps({'rig':rig.name,'scale':list(rig.scale),'bones':[p.name for p in rig.pose.bones],'actions':[{'name':a.name,'range':list(a.frame_range),'slots':[s.identifier for s in a.slots]} for a in bpy.data.actions],'fps':bpy.context.scene.render.fps}))
