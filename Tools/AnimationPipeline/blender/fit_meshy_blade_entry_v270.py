"""Lift the off-hand dagger before the jumping knee reaches its neutral pose."""
import bpy,sys,json
from pathlib import Path
from mathutils import Vector
sys.path.insert(0,str(Path(__file__).parent))
from common import slot_for,curve_records
from validate_animation import validate
from export_unity_fbx import export
ROOT=Path(__file__).resolve().parents[3];OUT=ROOT/'ArtSource/Combat/v270';OUT.mkdir(exist_ok=True)
E={'__file__':str(Path(__file__).with_name('author_pose_revision_v215.py'))};exec(Path(E['__file__']).read_text().split('def reviewed_previews')[0],E)
mode=sys.argv[-1]
if mode=='build':
 dest=OUT/'sahur-meshy-jump-entry-v270.blend'
 if dest.exists():raise FileExistsError(dest)
 bpy.ops.wm.open_mainfile(filepath=str(ROOT/'ArtSource/Combat/v269/sahur-meshy-jump-weapon-fit-v269.blend'))
 rig=bpy.data.objects['Armature'];source=bpy.data.actions['PLAYER_BladeJumpChop_v269'];samples=[E['sample'](rig,source,i+1) for i in range(52)];a=source.copy();a.name='PLAYER_BladeJumpChop_v270';a.use_fake_user=True
 for _,fc in curve_records(a):fc.keyframe_points.clear()
 rig.animation_data.action=a;rig.animation_data.action_slot=slot_for(a,rig)
 for i,pose in enumerate(samples):
  bpy.context.scene.frame_set(i+1);E['restore'](rig,pose);t=i/60;w=1-E['ease'](t/.14)
  p=rig.pose.bones['LeftHand'];p0=p.matrix.translation.copy();q0=p.matrix.to_quaternion();chest=rig.pose.bones['Spine2'].matrix.translation.copy()
  axis=Vector((.85,-.5,.10)).normalized();q=E['basis_rotation'](Vector((-.18,.41,-.89)),axis,(0,1,0),(-1,0,0))
  grip=chest+Vector((38,-24,0));E['limb'](rig,'Left','arm',p0.lerp(grip-q@Vector((4,12.5,-.5)),w),chest+Vector((66,-5,8)),q0.slerp(q,w))
  for p in rig.pose.bones:
   for channel in ('location','rotation_quaternion','scale'):p.keyframe_insert(data_path=channel,frame=i+1,group=p.name)
 for _,fc in curve_records(a):
  for key in fc.keyframe_points:key.interpolation='LINEAR'
 bpy.context.preferences.filepaths.save_version=0;bpy.ops.wm.save_as_mainfile(filepath=str(dest))
elif mode=='preview':
 from render_animation_preview import render
 bpy.ops.wm.open_mainfile(filepath=str(OUT/'sahur-meshy-jump-entry-v270.blend'));rig=bpy.data.objects['Armature'];a=bpy.data.actions['PLAYER_BladeJumpChop_v270']
 render(rig,a,OUT/'preview',[1,3,6,9,12,31],slot_for(a,rig),resolution=384)
elif mode=='export':
 bpy.ops.wm.open_mainfile(filepath=str(OUT/'sahur-meshy-jump-entry-v270.blend'));rig=bpy.data.objects['Armature'];a=bpy.data.actions['PLAYER_BladeJumpChop_v270']
 m=json.loads((ROOT/'ArtSource/Combat/v269/PLAYER_BladeJumpChop_v269.animation.json').read_text());m.update(action=a.name,unity_clip=a.name,revision='combat-v270',review_reference='ArtSource/Combat/v270/review.md',stage='FIRST PASS');m.pop('fbx',None);m.pop('blend',None)
 report=validate(rig,a,m,slot_for(a,rig));(OUT/'validation.json').write_text(json.dumps(report,indent=2));export(rig,a,m,OUT/(a.name+'.fbx'),slot_for(a,rig))
