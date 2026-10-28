"""Retain Meshy whole-body action; fit only actual weapon grips/forearms offline."""
import bpy,sys,math,json
from pathlib import Path
from mathutils import Vector
sys.path.insert(0,str(Path(__file__).parent))
from common import curve_records,slot_for
from validate_animation import validate
ROOT=Path(__file__).resolve().parents[3];OUT=ROOT/'ArtSource/Combat/v269';OUT.mkdir(exist_ok=True)
E={'__file__':str(Path(__file__).with_name('author_pose_revision_v215.py'))}
exec(Path(E['__file__']).read_text().split('def reviewed_previews')[0],E)
mode=sys.argv[-1]
if mode=='build':
 dest=OUT/'sahur-meshy-jump-weapon-fit-v269.blend'
 if dest.exists():raise FileExistsError(dest)
 bpy.ops.wm.open_mainfile(filepath=str(ROOT/'ArtSource/Combat/v268/sahur-meshy-jump-v268-draft2.blend'))
 rig=bpy.data.objects['Armature'];rest={b.name:[list(r) for r in b.matrix_local] for b in rig.data.bones};reports=[]
 for kind in ['Club','Blade']:
  source=bpy.data.actions['PLAYER_'+kind+'JumpChop_v268'];samples=[E['sample'](rig,source,i+1) for i in range(52)]
  action=source.copy();action.name='PLAYER_'+kind+'JumpChop_v269';action.use_fake_user=True
  for _,fc in curve_records(action):fc.keyframe_points.clear()
  rig.animation_data.action=action;rig.animation_data.action_slot=slot_for(action,rig)
  for i,pose in enumerate(samples):
   t=i/60;bpy.context.scene.frame_set(i+1);E['restore'](rig,pose)
   w=E['ease'](t/.09)*(1-E['ease']((t-.67)/.18));chest=rig.pose.bones['Spine2'].matrix.translation.copy()
   # Weapon rises beside the broad head, not along the face. Whole-body timing is unchanged.
   axis=Vector(tuple(E['curve'](t,points) for points in [
    [(0,-.25),(.26,-.25),(.32,-.15),(.40,.25),(.49,.5),(.65,.55),(.85,.55)],
    [(0,-.12),(.26,-.12),(.32,-.35),(.40,-.8),(.49,-.75),(.65,-.6),(.85,-.6)],
    [(0,.96),(.26,.96),(.32,.92),(.40,.4),(.49,-.4),(.65,-.6),(.85,-.6)]])).normalized()
   hand=rig.pose.bones['RightHand'];q0=hand.matrix.to_quaternion();p0=hand.matrix.translation.copy()
   local=Vector((.18,.41,-.89)) if kind=='Blade' else Vector((16.7918,58.718,-96.8423))-Vector((-3.6591,12.8643,2.1512))
   near=Vector(( -4,12.5,-.5)) if kind=='Blade' else Vector((-3.6591,12.8643,2.1512))
   q=E['basis_rotation'](local,axis,(0,1,0),(1,0,0))
   up=E['curve'](t,[(0,0),(.12,1),(.27,1),(.40,0),(.85,0)])
   grip=p0+Vector((-8*up,-12,4*up))
   target=grip-q@near
   E['limb'](rig,'Right','arm',p0.lerp(target,w),chest+Vector((-65,-8,18)),q0.slerp(q,w))
   if kind=='Blade':
    hand=rig.pose.bones['LeftHand'];p0=hand.matrix.translation.copy();q0=hand.matrix.to_quaternion()
    # Counterbalancing dagger stays outside the torso during follow-through.
    axis=Vector((.75,-.55,-.35)).normalized();q=E['basis_rotation'](Vector((-.18,.41,-.89)),axis,(0,1,0),(-1,0,0))
    grip=p0.copy();grip.x=max(grip.x,chest.x+27);grip.y=min(grip.y,chest.y-16)
    E['limb'](rig,'Left','arm',p0.lerp(grip-q@Vector((4,12.5,-.5)),w),chest+Vector((65,-4,-8)),q0.slerp(q,w))
   for p in rig.pose.bones:
    for channel in ('location','rotation_quaternion','scale'):p.keyframe_insert(data_path=channel,frame=i+1,group=p.name)
  for _,fc in curve_records(action):
   for key in fc.keyframe_points:key.interpolation='LINEAR'
  reports.append({'action':action.name,'source_action':source.name,'fps':60,'frame_start':1,'frame_end':52,'duration':.85,'stage':'FIRST PASS'})
 assert rest=={b.name:[list(r) for r in b.matrix_local] for b in rig.data.bones}
 bpy.context.preferences.filepaths.save_version=0;bpy.ops.wm.save_as_mainfile(filepath=str(dest));(OUT/'index.json').write_text(json.dumps(reports,indent=2))
elif mode=='preview':
 from render_animation_preview import render
 bpy.ops.wm.open_mainfile(filepath=str(OUT/'sahur-meshy-jump-weapon-fit-v269.blend'))
 rig=bpy.data.objects['Armature'];E['preview_contact_props'](rig)
 action=bpy.data.actions['PLAYER_ClubJumpChop_v269']
 render(rig,action,OUT/'preview-club',[7,16,23,30,40,52],slot_for(action,rig),resolution=384)
else:raise ValueError(mode)
