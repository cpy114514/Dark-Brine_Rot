"""Targeted full-action replacement: retained rig, readable one-hand/twin aerial chops."""
import bpy,sys,json,math
from pathlib import Path
from mathutils import Vector,Quaternion
ROOT=Path(__file__).resolve().parents[3]
sys.path.insert(0,str(Path(__file__).parent))
from common import curve_records,slot_for
from validate_animation import validate
from render_animation_preview import render
from export_unity_fbx import export
E={'__file__':str(Path(__file__).with_name('author_pose_revision_v215.py'))}
exec(Path(E['__file__']).read_text().split('def reviewed_previews')[0],E)
OUT=ROOT/'ArtSource/Combat/v266';MODE=sys.argv[-1]
if MODE=='build':
 OUT.mkdir(parents=True,exist_ok=True);dest=OUT/'sahur-jump-chops-v266.blend'
 if dest.exists():raise FileExistsError(dest)
 bpy.ops.wm.open_mainfile(filepath=str(ROOT/'ArtSource/Combat/v265/sahur-jump-chops-v265.blend'))
 rig=bpy.data.objects['Armature'];source=bpy.data.actions['CH1_SahurGuard_v050'];pose=E['sample'](rig,source,1)
 rest={b.name:tuple(b.matrix_local) for b in rig.data.bones};specs=[];audit=[]
 near=Vector((-3.6591,12.8643,2.1512));shaftlocal=Vector((16.7918,58.718,-96.8423))-near
 for kind in ['Club','Blade']:
  a=bpy.data.actions[f'PLAYER_{kind}JumpChop_v265'].copy();a.name=f'PLAYER_{kind}JumpChop_v266';a.use_fake_user=True
  for _,fc in curve_records(a):fc.keyframe_points.clear()
  rig.animation_data.action=a;rig.animation_data.action_slot=slot_for(a,rig)
  for i in range(52):
   t=i/60;bpy.context.scene.frame_set(i+1);E['restore'](rig,pose)
   lift=E['curve'](t,[(0,0),(.15,.85),(.23,1),(.31,.85),(.45,0),(.65,0),(.85,0)])
   hit=E['curve'](t,[(0,0),(.30,0),(.44,1),(.57,.75),(.85,0)])
   weight=E['curve'](t,[(0,0),(.09,1),(.66,1),(.85,0)])
   for b,degrees in [('Spine',-5*lift+10*hit),('Spine2',-3*lift+7*hit),('Head',-2*lift-3*hit)]:
    E['rotate_world'](rig.pose.bones[b],Quaternion((1,0,0),math.radians(degrees)))
   # Both weapons start above Sahur's right shoulder and cut across
   # the front into a low, slightly left follow-through. Rig right is -X.
   E['rotate_world'](rig.pose.bones['Spine2'],Quaternion((0,0,1),math.radians(-7*lift+9*hit)))
   chest=rig.pose.bones['Spine2'].matrix.translation.copy()
   for side,sign in ([('Right',-1),('Left',1)] if kind=='Blade' else [('Right',-1)]):
    hand=rig.pose.bones[side+'Hand'];p0=hand.matrix.translation.copy();q0=hand.matrix.to_quaternion()
    lateral=E['curve'](t,[(0,-26),(.15,-30),(.30,-30),(.44,13),(.60,18),(.85,-26)])
    lane=sign*9 if kind=='Blade' else 0
    grip=chest+Vector((lateral+lane,-23+11*lift-18*hit,2+36*lift-23*hit))
    axis=Vector((-.25*lift+.50*hit,-.58+.48*lift-.15*hit,.25+.78*lift-.95*hit)).normalized()
    local=Vector((sign*.18,.41,-.89)) if kind=='Blade' else shaftlocal
    q=E['basis_rotation'](local,axis,(0,1,0),(1,0,0))
    target=grip-q@(Vector((-sign*4,12.5,-.5)) if kind=='Blade' else near)
    E['limb'](rig,side,'arm',p0.lerp(target,weight),chest+Vector((sign*62,-2,12*lift-13*hit)),q0.slerp(q,weight))
   if kind=='Club':
    p=rig.pose.bones['LeftHand'].matrix.translation.copy()
    E['limb'](rig,'Left','arm',p.lerp(chest+Vector((36,6,-15+8*lift)),weight),chest+Vector((57,4,-20)))
   # A compact asymmetric tuck, followed by re-extension. No root translation.
   for side,sign in [('Right',-1),('Left',1)]:
    foot=rig.pose.bones[side+'Foot'];p=foot.matrix.translation.copy();q=foot.matrix.to_quaternion()
    p+=Vector((sign*2*lift,5*lift,(8 if side=='Left' else 4)*lift))
    E['limb'](rig,side,'leg',p,p+Vector((0,-35,32)),q)
   for b in rig.pose.bones:
    for channel in ('location','rotation_quaternion','scale'):b.keyframe_insert(data_path=channel,frame=i+1,group=b.name)
  for _,fc in curve_records(a):
   for k in fc.keyframe_points:k.interpolation='LINEAR'
  m=dict(blend=str(dest),rig=rig.name,action=a.name,unity_clip=a.name,revision='combat-v266',frame_start=1,frame_end=52,fps=60,loop=False,root_motion='in_place',root_bone='root',rig_type='Humanoid',stage='SECOND PASS',design_status='approved',design_reference='Documentation/JumpDiagonal-v266.md',approval_reference='User: chop from upper right diagonally down and finish slightly left; existing gameplay contract retained.',review_status='pending',review_reference='',avatar='Assets/Game/Prefabs/Characters/Sahur/Models/PbrSahur.fbx',gameplay={'active':[18,35],'startup':[1,18],'recovery':[35,52]})
  audit.append(validate(rig,a,m,slot_for(a,rig)));specs.append(m)
 assert rest=={b.name:tuple(b.matrix_local) for b in rig.data.bones}
 bpy.context.scene.render.fps=60;bpy.context.scene.render.fps_base=1;bpy.context.preferences.filepaths.save_version=0
 bpy.ops.wm.save_as_mainfile(filepath=str(dest));(OUT/'index.json').write_text(json.dumps(specs,indent=2));(OUT/'audit.json').write_text(json.dumps(audit,indent=2))
else:
 specs=json.loads((OUT/'index.json').read_text());bpy.ops.wm.open_mainfile(filepath=specs[0]['blend']);rig=bpy.data.objects['Armature']
 if MODE=='preview':
  proxy,floor=E['preview_contact_props'](rig)
  # Actual blade grip convention, with separate hand-bound proxies.
  blades=[]
  for side,sign in [('Right',-1),('Left',1)]:
   axis=Vector((sign*.18,.41,-.89)).normalized();a=Vector((-sign*4,12.5,-.5));b=a+axis*70;across=axis.cross(Vector((0,1,0))).normalized()*3;bind=rig.data.bones[side+'Hand'].matrix_local
   mesh=bpy.data.meshes.new('BladeReference'+side);mesh.from_pydata([bind@v for v in [a-across,a+across,b+across*.3,b-across*.3]],[],[(0,1,2,3)]);mesh.update()
   ob=bpy.data.objects.new(mesh.name,mesh);bpy.context.scene.collection.objects.link(ob);ob.matrix_world=rig.matrix_world.copy();ob.vertex_groups.new(name=side+'Hand').add(list(range(4)),1,'REPLACE');ob.modifiers.new('Binding','ARMATURE').object=rig;blades.append(ob)
  for m in specs:
   blade='Blade' in m['action'];proxy.hide_render=blade
   for ob in blades:ob.hide_render=not blade
   render(rig,bpy.data.actions[m['action']],OUT/'preview'/m['action'],[1,11,19,25,31,37,52],slot_for(bpy.data.actions[m['action']],rig),resolution=384)
 elif MODE=='export':
  for m in specs:export(rig,bpy.data.actions[m['action']],m,OUT/(m['action']+'.fbx'),slot_for(bpy.data.actions[m['action']],rig))
 else:raise ValueError(MODE)
