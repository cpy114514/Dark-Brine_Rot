import sys
from pathlib import Path
sys.path.insert(0,str(Path(__file__).resolve().parent))
from common import *

def inspect(rig):
    return {'rig':rig.name,'scale':list(rig.scale),'dimensions':list(rig.dimensions),
            'animation':rig.animation_data.action.name if rig.animation_data and rig.animation_data.action else None,
            'bones':[{'name':b.name,'parent':b.parent.name if b.parent else None,'deform':b.use_deform,
                      'head':list(b.head_local),'tail':list(b.tail_local),
                      'rotation_mode':rig.pose.bones[b.name].rotation_mode,
                      'constraints':[{'name':c.name,'type':c.type,'muted':c.mute} for c in rig.pose.bones[b.name].constraints]}
                     for b in rig.data.bones],
            'bound_meshes':[o.name for o in bpy.context.scene.objects if o.type=='MESH' and any(m.type=='ARMATURE' and m.object==rig for m in o.modifiers)],
            'fps':effective_fps(),'frame_range':[bpy.context.scene.frame_start,bpy.context.scene.frame_end]}
if __name__=='__main__':
    a=arguments(parser('Inspect armature hierarchy, constraints and bound meshes without editing'));report(inspect(rig_object(a.rig)),a.output)
