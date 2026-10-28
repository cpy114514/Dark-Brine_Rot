"""Offline retarget of one selected AI action onto the retained Sahur rig.

No runtime bone replacement, source rest/bind edits or generated root travel.
The imported source is retained separately for review and provenance.
"""
import bpy,sys,json,math
from pathlib import Path
from mathutils import Matrix,Vector
sys.path.insert(0,str(Path(__file__).resolve().parent))
from common import curve_records,slot_for
from render_animation_preview import render
ROOT=Path(__file__).resolve().parents[3]
OUT=ROOT/'ArtSource/Combat/v268'
MODE=sys.argv[-1]
source_file=ROOT/'ArtSource/Combat/v268/Meshy_Sahur_Aerial_Diagonal_Chop_02.fbx'

def ease(x):
 x=max(0,min(1,x));return x*x*(3-2*x)
def source_frame(t):
 points=[(0,1),(.13,22),(.26,42),(.40,59),(.60,85),(.85,120)]
 for (a,x),(b,y) in zip(points,points[1:]):
  if t<=b:return x+(y-x)*(t-a)/(b-a)
 return points[-1][1]

if MODE=='build':
 OUT.mkdir(parents=True,exist_ok=True)
 dest=OUT/'sahur-meshy-jump-v268-draft2.blend'
 if dest.exists():raise FileExistsError(dest)
 bpy.ops.wm.open_mainfile(filepath=str(ROOT/'ArtSource/Combat/v266/sahur-jump-chops-v266.blend'))
 rig=bpy.data.objects['Armature'];guard=bpy.data.actions['CH1_SahurGuard_v050']
 rig.animation_data.action=guard;rig.animation_data.action_slot=slot_for(guard,rig);bpy.context.scene.frame_set(1)
 guardpose={p.name:(p.location.copy(),p.rotation_quaternion.copy(),p.scale.copy()) for p in rig.pose.bones}
 rest={b.name:[list(r) for r in b.matrix_local] for b in rig.data.bones}
 bpy.ops.import_scene.fbx(filepath=str(source_file),use_anim=True)
 source=next(o for o in bpy.context.scene.objects if o.type=='ARMATURE' and o!=rig)
 action=source.animation_data.action;source.animation_data.use_nla=False
 mapping={p.name:source.pose.bones.get('smartrig:'+p.name) for p in rig.pose.bones}
 mapping={n:s for n,s in mapping.items() if s}
 # Sample first, before changing action/frame state on the destination rig.
 samples=[]
 for i in range(52):
  f=source_frame(i/60);bpy.context.scene.frame_set(math.floor(f),subframe=f%1);bpy.context.view_layer.update()
  src_world=source.matrix_world.to_quaternion();dst_world=rig.matrix_world.to_quaternion()
  samples.append({n:(dst_world.inverted()@src_world@p.matrix.to_quaternion()@p.bone.matrix_local.to_quaternion().inverted()@src_world.inverted()@dst_world@rig.data.bones[n].matrix_local.to_quaternion()) for n,p in mapping.items()})
 for ob in [o for o in bpy.context.scene.objects if o==source or o.parent==source]:ob.hide_render=True;ob.hide_set(True)
 reports=[]
 for kind in ['Club','Blade']:
  a=guard.copy();a.name='PLAYER_'+kind+'JumpChop_v268';a.use_fake_user=True
  for _,fc in curve_records(a):fc.keyframe_points.clear()
  rig.animation_data.action=a;rig.animation_data.action_slot=slot_for(a,rig)
  for i,pose in enumerate(samples):
   t=i/60;bpy.context.scene.frame_set(i+1)
   for p in rig.pose.bones:p.rotation_mode='QUATERNION';p.location,p.rotation_quaternion,p.scale=guardpose[p.name]
   bpy.context.view_layer.update()
   # Child translations inherit the actual destination bone lengths.
   for p in rig.pose.bones:
    if p.name not in pose:continue
    m=p.matrix.copy();p.matrix=Matrix.LocRotScale(m.translation,pose[p.name],m.to_scale());bpy.context.view_layer.update()
   w=ease(t/.09)*(1-ease((t-.67)/.18))
   for p in rig.pose.bones:
    p.rotation_quaternion=guardpose[p.name][1].slerp(p.rotation_quaternion,w)
    for channel in ['location','rotation_quaternion','scale']:p.keyframe_insert(data_path=channel,frame=i+1,group=p.name)
  for _,fc in curve_records(a):
   for k in fc.keyframe_points:k.interpolation='LINEAR'
  reports.append({'action':a.name,'stage':'BLOCKING','source':str(source_file),'fps':60,'frames':[1,52],
   'duration':.85,'root_motion':'in_place','active_normalized':[.34,.67],
   'notes':'Raw whole-body retarget; hand/weapon fit and dual-blade adaptation require visual review before export.'})
 assert rest=={b.name:[list(r) for r in b.matrix_local] for b in rig.data.bones}
 bpy.context.scene.render.fps=60;bpy.context.scene.render.fps_base=1;bpy.context.preferences.filepaths.save_version=0
 bpy.ops.wm.save_as_mainfile(filepath=str(dest));(OUT/'index.json').write_text(json.dumps(reports,indent=2))
else:
 bpy.ops.wm.open_mainfile(filepath=str(OUT/'sahur-meshy-jump-v268-draft2.blend'))
 rig=bpy.data.objects['Armature']
 E={'__file__':str(Path(__file__).with_name('author_pose_revision_v215.py'))}
 exec(Path(E['__file__']).read_text().split('def reviewed_previews')[0],E)
 proxy,floor=E['preview_contact_props'](rig)
 for kind in ['Club']:
  a=bpy.data.actions['PLAYER_'+kind+'JumpChop_v268'];render(rig,a,OUT/'preview-draft2'/kind,[1,7,13,19,25,31,37,43,52],slot_for(a,rig),resolution=420)
