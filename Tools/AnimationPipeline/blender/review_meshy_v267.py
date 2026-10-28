"""Render the selected Meshy export, separate from production assets."""
import bpy,sys,json
from pathlib import Path
sys.path.insert(0,str(Path(__file__).resolve().parent))
from common import slot_for
from render_animation_preview import render
root=Path(__file__).resolve().parents[3]
bpy.ops.wm.read_factory_settings(use_empty=True)
args=sys.argv[sys.argv.index('--')+1:] if '--' in sys.argv else []
bpy.ops.import_scene.fbx(filepath=args[0] if args else 'C:/Users/pinyu/Downloads/Meshy_AI_TemporaryMotionProxy_01a17c1c_d55d_7358_a7.fbx')
rig=next(o for o in bpy.context.scene.objects if o.type=='ARMATURE')
a=bpy.data.actions[0]
out=Path(args[1]).resolve() if len(args)>1 else root/'.codex/ai-motion-v267/current-preview'
render(rig,a,out,[1,16,31,46,61,76,91,106,121,136,151,166,179],slot_for(a,rig),resolution=320)
