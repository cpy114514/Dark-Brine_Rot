import sys
from pathlib import Path
sys.path.insert(0,str(Path(__file__).resolve().parent))
from common import *
if __name__=='__main__':
    a=arguments(parser('Inspect all Actions and object bindings without editing'))
    report({'file':bpy.data.filepath,'blender':bpy.app.version_string,'fps':effective_fps(),
            'scene_range':[bpy.context.scene.frame_start,bpy.context.scene.frame_end],
            'actions':[action_summary(x) for x in bpy.data.actions],
            'bindings':[{'object':o.name,'action':o.animation_data.action.name,
                         'slot':getattr(o.animation_data.action_slot,'identifier',None) if hasattr(o.animation_data,'action_slot') else None}
                        for o in bpy.context.scene.objects if o.animation_data and o.animation_data.action]},a.output)
