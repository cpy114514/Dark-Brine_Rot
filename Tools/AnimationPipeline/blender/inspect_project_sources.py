"""Read an opened master .blend; never save, rename or assign Actions."""
import sys
from pathlib import Path
sys.path.insert(0,str(Path(__file__).resolve().parent))
from common import *
from inspect_rig import inspect

if __name__=='__main__':
    a=arguments(parser('Read-only rig/Action audit of an existing Blender master'))
    report({'file':bpy.data.filepath,'blender':bpy.app.version_string,'fps':effective_fps(),
            'units':{'system':bpy.context.scene.unit_settings.system,'scale_length':bpy.context.scene.unit_settings.scale_length},
            'scene_range':[bpy.context.scene.frame_start,bpy.context.scene.frame_end],
            'rigs':[inspect(o) for o in bpy.context.scene.objects if o.type=='ARMATURE'],
            'actions':[action_summary(x) for x in bpy.data.actions]},a.output)
