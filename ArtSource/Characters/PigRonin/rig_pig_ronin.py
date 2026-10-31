"""Bind the pig ronin, preserve its sculpted pose, and export tested animated skins."""
import bpy, math, json, shutil
import numpy as np
from pathlib import Path
from mathutils import Vector, Matrix, Quaternion

OUT=Path(__file__).resolve().parent
ROOT=OUT.parents[2]
ASSET=ROOT/'Assets/Game/Prefabs/Characters/PigRonin'
source=OUT/'PigRonin.blend'
archive=OUT/'PigRonin_Unrigged.blend'
bpy.context.preferences.filepaths.save_version=0
if not archive.exists():shutil.copy2(source,archive)
bpy.ops.wm.open_mainfile(filepath=str(source))
if any(o.type=='ARMATURE' for o in bpy.data.objects):
    bpy.ops.wm.open_mainfile(filepath=str(archive))
else:
    shutil.copy2(source,archive)
bpy.context.preferences.filepaths.save_version=0
sc=bpy.context.scene;sc.render.fps=30;sc.frame_start=1;sc.frame_end=31
char=bpy.data.collections['CHARACTER | Jade Pig Ronin']
meshes=[o for o in char.objects if o.type=='MESH']
root=bpy.data.objects['PigRonin']

# Freeze the existing surface reduction before weighting. Every coordinate is in armature space.
bpy.ops.object.select_all(action='DESELECT')
original={}
for o in meshes:
    bpy.context.view_layer.objects.active=o;o.select_set(True)
    for mod in list(o.modifiers):bpy.ops.object.modifier_apply(modifier=mod.name)
    world=o.matrix_world.copy();o.parent=None;o.data.transform(world);o.matrix_world=Matrix.Identity(4)
    original[o.name]=np.array([v.co[:] for v in o.data.vertices],dtype=np.float64)
    o.select_set(False)

arm=bpy.data.armatures.new('PigRonin_Skeleton')
rig=bpy.data.objects.new('Rig',arm);char.objects.link(rig);rig.parent=root
rig.show_in_front=True;arm.display_type='OCTAHEDRAL'
bpy.context.view_layer.objects.active=rig;rig.select_set(True)
bpy.ops.object.mode_set(mode='EDIT')
bones={}
def bone(name,head,tail,parent=None,connect=False):
    b=arm.edit_bones.new(name);b.head=head;b.tail=tail
    if parent:b.parent=bones[parent];b.use_connect=connect
    bones[name]=b
bone('Root',(0,0,.025),(0,0,.23))
bone('Hips',(0,0,.98),(0,0,1.18),'Root')
bone('Spine',(0,0,1.18),(0,0,1.50),'Hips',True)
bone('Chest',(0,0,1.50),(0,0,1.79),'Spine',True)
bone('Neck',(0,0,1.79),(0,0,2.04),'Chest',True)
bone('Head',(0,0,2.04),(0,0,2.80),'Neck',True)
for side,s in [('Left',1),('Right',-1)]:
    bone(side+'Shoulder',(s*.12,0,1.78),(s*.51,0,1.77),'Chest')
    bone(side+'UpperArm',(s*.51,0,1.77),(s*.90,-.005,1.48),side+'Shoulder',True)
    bone(side+'LowerArm',(s*.90,-.005,1.48),(s*.70,-.28,1.30),side+'UpperArm',True)
    bone(side+'Hand',(s*.70,-.28,1.30),(s*.60,-.38,1.24),side+'LowerArm',True)
    bone(side+'Thumb',(s*.61,-.35,1.31),(s*.53,-.395,1.355),side+'Hand')
    bone(side+'UpperLeg',(s*.305,0,.99),(s*.305,-.012,.62),'Hips')
    bone(side+'LowerLeg',(s*.305,-.012,.62),(s*.305,-.005,.285),side+'UpperLeg',True)
    bone(side+'Foot',(s*.305,-.005,.285),(s*.305,-.30,.16),side+'LowerLeg',True)
    bone(side+'Toes',(s*.305,-.30,.16),(s*.305,-.435,.16),side+'Foot',True)
    bone(side+'Ear',(s*.56,0,2.83),(s*.88,.015,3.145),'Head')
tailcurve=[]
for i in range(38):
    t=i/37;a=t*math.pi*3.3;r=.105*(1-.45*t)
    tailcurve.append(Vector((r*math.sin(a),.35+.15*t+r*(1-math.cos(a)),.91+.15*t)))
for i,(a,b) in enumerate([(0,12),(12,25),(25,37)]):
    bone('Tail'+str(i+1),tailcurve[a],tailcurve[b],'Hips' if i==0 else 'Tail'+str(i),i>0)
bone('Sword',(-.975,.272,1.243),(-.74,.50,.42),'Hips')
bpy.ops.object.mode_set(mode='OBJECT')
bone_names=[b.name for b in arm.bones]
old_rest={b.name:b.matrix_local.copy() for b in arm.bones}
for b in arm.bones:
    b.use_deform=True
    group='Arms' if any(s in b.name for s in ['Arm','Hand','Thumb','Shoulder']) else ('Legs' if any(s in b.name for s in ['Leg','Foot','Toes']) else ('Accessories' if 'Ear' in b.name or 'Tail' in b.name or b.name=='Sword' else 'Body'))
    coll=arm.collections.get(group) or arm.collections.new(group);coll.assign(b)
    try:b.color.palette={'Body':'THEME04','Arms':'THEME03','Legs':'THEME01','Accessories':'THEME05'}[group]
    except AttributeError:pass

def smooth(v):
    t=max(0.,min(1.,float(v)));return t*t*(3-2*t)
def mix(a,b,t):
    t=smooth(t);return {a:1-t,b:t}
def torso(p):
    z=p[2]
    if z<1.14:return {'Hips':1}
    if z<1.40:return mix('Hips','Spine',(z-1.14)/.26)
    if z<1.72:return mix('Spine','Chest',(z-1.40)/.32)
    if z<1.96:return mix('Chest','Neck',(z-1.80)/.16)
    return mix('Neck','Head',(z-1.96)/.20)
def project(p,a,b):
    a=np.asarray(a);d=np.asarray(b)-a;t=max(0,min(1,float(np.dot(p-a,d)/np.dot(d,d))))
    return float(np.linalg.norm(p-(a+t*d))),t
def arm_weights(p,side):
    names=[side+'UpperArm',side+'LowerArm',side+'Hand']
    upper,lower,hand=[arm.bones[n] for n in names]
    u=(upper.tail_local-upper.head_local).normalized()
    f=(lower.tail_local-lower.head_local).normalized()
    h=(hand.tail_local-hand.head_local).normalized()
    # A continuous joint bisector avoids nearest-segment discontinuities inside a bent elbow.
    elbow=smooth((Vector(p-upper.tail_local[:]).dot((u+f).normalized())+.135)/.27)
    wrist=smooth((Vector(p-lower.tail_local[:]).dot((f+h).normalized())+.075)/.15)
    return {names[0]:1-elbow,names[1]:elbow*(1-wrist),names[2]:elbow*wrist}
def pants(p):
    x,y,z=p;left=smooth((x+.10)/.20)
    leg=smooth((1.03-z)/.29);shin=smooth((.70-z)/.24)
    return {'Hips':1-leg,'LeftUpperLeg':leg*left*(1-shin),'LeftLowerLeg':leg*left*shin,
            'RightUpperLeg':leg*(1-left)*(1-shin),'RightLowerLeg':leg*(1-left)*shin}
def weight(o,p):
    n=o.name;side='Left' if p[0]>=0 else 'Right'
    if 'Ear_' in n:return {side+'Ear':1}
    if any(t in n for t in ['Head_','Eye_','Pupil','Glint','Snout','Nostril','Expressive_Brow','LowerEyelid','Smirk','Chin_Crease']):return {'Head':1}
    if 'Arm_HandOnHip' in n:return arm_weights(p,side)
    if n.startswith('Thumb'):return {side+'Thumb':1}
    if n.startswith('Hand_'):return {side+'Hand':1}
    if n.startswith(('Body_','Haori_','Back_Crest')):return torso(p)
    if n.startswith('Hakama'):return pants(p)
    if n.startswith('Ankle'):return mix(side+'Foot',side+'LowerLeg',(p[2]-.25)/.13)
    if n.startswith(('Cloven_Foot','Hoof_Division')):return mix(side+'Foot',side+'Toes',(-p[1]-.30)/.12)
    if n.startswith(('Geta_Sandal','Sandal_Thong')):return {side+'Foot':1}
    if n.startswith('Curly_Tail'):
        nearest=min(range(len(tailcurve)),key=lambda i:(Vector(p)-tailcurve[i]).length_squared)
        if nearest<17:return mix('Tail1','Tail2',(nearest-7)/10)
        return mix('Tail2','Tail3',(nearest-20)/10)
    if n.startswith(('Saya_','Tsuka_','Tsuba_','Kashira_')):return {'Sword':1}
    if n.startswith('Sword_SuspensionCord'):return mix('Hips','Sword',(-p[0]-.60)/.35)
    if n.startswith(('Obi_','Seal_','Sash_')):return {'Hips':1}
    raise RuntimeError('Unclassified skin object: '+n)

allweights={};maxinfluences=0
for o in meshes:
    weights=[]
    for p in original[o.name]:
        w={n:v for n,v in weight(o,p).items() if v>1e-7}
        # Keep at most four normalized influences for Unity/GLB skinning.
        w=dict(sorted(w.items(),key=lambda pair:-pair[1])[:4]);total=sum(w.values())
        assert total>0
        weights.append({n:v/total for n,v in w.items()});maxinfluences=max(maxinfluences,len(w))
    allweights[o.name]=weights

# Set a straight-arm neutral bind. Bone matrices are stored explicitly to preserve joint locations.
def direction(name,target):
    pb=rig.pose.bones[name];current=pb.tail-pb.head
    q=current.normalized().rotation_difference(Vector(target).normalized())@pb.matrix.to_quaternion()
    pb.matrix=Matrix.LocRotScale(pb.head.copy(),q,Vector((1,1,1)));bpy.context.view_layer.update()
for side,s in [('Left',1),('Right',-1)]:
    for n in ['UpperArm','LowerArm','Hand']:direction(side+n,(s,0,0))
neutral={b.name:b.matrix.copy() for b in rig.pose.bones}

# The original sculpt fused two bent ellipsoids. Replace that joint with evenly spaced
# neutral rings so the elbow has actual deformation loops instead of a sculpted crease.
rebuilt_arms=[]
for o in meshes:
    if 'Arm_HandOnHip' not in o.name:continue
    s=1 if original[o.name][:,0].mean()>0 else -1;side='Left' if s>0 else 'Right'
    shoulder=neutral[side+'UpperArm'].translation
    elbow=neutral[side+'LowerArm'].translation
    wrist=neutral[side+'Hand'].translation
    hand_end=Vector(neutral[side+'Hand']@Vector((0,arm.bones[side+'Hand'].length,0)))
    x0=abs(shoulder.x);xe=abs(elbow.x);xw=abs(wrist.x);xt=abs(hand_end.x)
    profile=[(x0-.11,.006),(x0-.065,.12),(x0+.02,.18),(x0+.19,.178),
             (xe-.10,.166),(xe,.16),(xe+.12,.153),(xw-.07,.14),(xw,.137),
             (xw+.07,.167),(xt+.015,.15),(xt+.085,.08),(xt+.11,.006)]
    verts=[];faces=[];rings=76;segments=32
    for j in range(rings):
        x=profile[0][0]+(profile[-1][0]-profile[0][0])*j/(rings-1)
        k=next((k for k in range(len(profile)-1) if profile[k][0]<=x<=profile[k+1][0]),len(profile)-2)
        t=smooth((x-profile[k][0])/(profile[k+1][0]-profile[k][0]))
        r=profile[k][1]*(1-t)+profile[k+1][1]*t
        for i in range(segments):
            a=math.tau*i/segments;verts.append((s*x,shoulder.y+math.cos(a)*r*.85,shoulder.z+math.sin(a)*r))
    for j in range(rings-1):
        for i in range(segments):
            a=j*segments+i;b=j*segments+(i+1)%segments
            faces.append((a,b,b+segments,a+segments))
    faces.append(tuple(reversed(range(segments))))
    faces.append(tuple((rings-1)*segments+i for i in range(segments)))
    me=bpy.data.meshes.new(o.name+'_DeformationLoops');me.from_pydata(verts,[],faces);me.update()
    me.materials.append(bpy.data.materials['Skin_WarmIvory'])
    import bmesh
    bm=bmesh.new();bm.from_mesh(me);bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bm.to_mesh(me);bm.free()
    for p in me.polygons:p.use_smooth=True
    o.data=me;weights=[]
    for x,y,z in verts:
        a=smooth((abs(x)-xe+.14)/.28);h=smooth((abs(x)-xw+.08)/.16)
        w={side+'UpperArm':1-a,side+'LowerArm':a*(1-h),side+'Hand':a*h}
        w={n:v for n,v in w.items() if v>1e-7};total=sum(w.values());weights.append({n:v/total for n,v in w.items()})
    allweights[o.name]=weights;rebuilt_arms.append(o.name)

# Inverse linear skinning makes the original hands-on-hips sculpt recover exactly.
# This avoids baking elbow compression into the new neutral mesh.
transforms={n:np.array(old_rest[n]@neutral[n].inverted(),dtype=np.float64) for n in bone_names}
for o in meshes:
    if o.name in rebuilt_arms:continue
    values=original[o.name];matrix=np.zeros((len(values),4,4),dtype=np.float64)
    for i,w in enumerate(allweights[o.name]):
        for n,v in w.items():matrix[i]+=transforms[n]*v
    xyz=np.linalg.solve(matrix[:,:3,:3],(values-matrix[:,:3,3])[:,:,None])[:,:,0]
    assert np.isfinite(xyz).all()
    for v,co in zip(o.data.vertices,xyz):v.co=co
    o.data.update()
bpy.context.view_layer.objects.active=rig
bpy.ops.object.mode_set(mode='POSE');bpy.ops.pose.armature_apply(selected=False);bpy.ops.object.mode_set(mode='OBJECT')
for o in meshes:
    for n in sorted({n for w in allweights[o.name] for n in w}):o.vertex_groups.new(name=n)
    for i,w in enumerate(allweights[o.name]):
        for n,v in w.items():o.vertex_groups[n].add([i],v,'REPLACE')
    o.parent=rig;o.matrix_parent_inverse=Matrix.Identity(4)
    mod=o.modifiers.new('PigRonin skin','ARMATURE');mod.object=rig;mod.use_deform_preserve_volume=False

def clear():
    for pb in rig.pose.bones:pb.matrix_basis=Matrix.Identity(4);pb.rotation_mode='QUATERNION'
    bpy.context.view_layer.update()
def sculpt_pose():
    clear()
    for pb in rig.pose.bones:
        pb.matrix=old_rest[pb.name];bpy.context.view_layer.update()
def key(frame):
    for pb in rig.pose.bones:
        pb.keyframe_insert(data_path='location',frame=frame,group=pb.name)
        pb.keyframe_insert(data_path='rotation_quaternion',frame=frame,group=pb.name)
        pb.keyframe_insert(data_path='scale',frame=frame,group=pb.name)
def action(name):
    rig.animation_data_create();a=bpy.data.actions.new(name);rig.animation_data.action=a;a.use_fake_user=True;return a
clear();bind=action('BindTPose');key(1);key(2)
hips=action('HandsOnHips');sculpt_pose();key(1);key(31)

# Validate every weighted vertex against the original sculpt before generating test movement.
bpy.context.view_layer.update();deps=bpy.context.evaluated_depsgraph_get();rest_error=0
for o in meshes:
    if o.name in rebuilt_arms:continue
    ev=o.evaluated_get(deps);me=ev.to_mesh();co=np.array([v.co[:] for v in me.vertices])
    error=float(np.linalg.norm(co-original[o.name],axis=1).max());rest_error=max(rest_error,error);ev.to_mesh_clear()
assert rest_error<.00015,('Original pose did not recover',rest_error)
checks={}
for bn,on in [('Head','Snout_WithRecessedNostrils'),('LeftLowerArm','R_Arm_HandOnHip'),('LeftLowerLeg','Hakama_ContinuousSculpt'),('LeftToes','Cloven_Foot.001'),('Sword','Saya_Scabbard'),('LeftEar','R_Ear_Sculpt'),('Tail2','Curly_Tail')]:
    o=bpy.data.objects[on];pb=rig.pose.bones[bn];saved=pb.matrix_basis.copy()
    before=o.evaluated_get(bpy.context.evaluated_depsgraph_get()).to_mesh()
    a=np.array([v.co[:] for v in before.vertices]);o.evaluated_get(bpy.context.evaluated_depsgraph_get()).to_mesh_clear()
    pb.rotation_quaternion=pb.rotation_quaternion@Quaternion((1,0,0),math.radians(20));bpy.context.view_layer.update()
    ev=o.evaluated_get(bpy.context.evaluated_depsgraph_get());me=ev.to_mesh();b=np.array([v.co[:] for v in me.vertices]);ev.to_mesh_clear()
    moved=np.linalg.norm(a-b,axis=1);checks[bn]={'max_motion':float(moved.max()),'moved_vertices':int((moved>1e-5).sum())}
    assert moved.max()>.004,(bn,'skin did not move')
    pb.matrix_basis=saved;bpy.context.view_layer.update()

test=action('RigCheck');sculpt_pose();key(1);key(31)
direction('LeftUpperArm',( .90,-.10,-.20));direction('LeftLowerArm',(.25,-.80,.45));direction('LeftHand',(.20,-.80,.35))
direction('RightUpperArm',(-.8,.02,-.35));direction('RightLowerArm',(-.6,-.25,-.4));direction('RightHand',(-.6,-.25,-.4))
direction('LeftUpperLeg',(0,-.28,-.96));direction('LeftLowerLeg',(0,.30,-.95));direction('LeftFoot',(0,-1,-.4))
for n,axis,angle in [('Head',(0,1,0),-12),('LeftEar',(1,0,0),8),('Tail2',(1,0,0),16),('Sword',(0,1,0),7)]:
    pb=rig.pose.bones[n];pb.rotation_quaternion=pb.rotation_quaternion@Quaternion(axis,math.radians(angle))
key(16)
sc.frame_set(16);bpy.context.view_layer.update()
sc.render.resolution_x=1000;sc.render.resolution_y=1100;sc.render.filepath=str(OUT/'PigRonin_RigCheck.png')
bpy.ops.render.render(write_still=True)
rig.animation_data.action=bind;sc.frame_set(1);clear()
sc.render.filepath=str(OUT/'PigRonin_BindTPose.png');bpy.ops.render.render(write_still=True)

# Save with the familiar sculpted pose active; neutral bind and test actions remain available.
rig.animation_data.action=hips;sc.frame_set(1);sculpt_pose()
sc.render.filepath=str(OUT/'PigRonin_Hero.png');bpy.ops.render.render(write_still=True)
rig['rig_notes']='30-bone FK deform rig; T-pose bind; HandsOnHips and RigCheck actions. Max 4 normalized weights.'
rig['front']='-Y in Blender / +Z in Unity'
bpy.ops.object.select_all(action='DESELECT')
for o in char.objects:o.select_set(True)
bpy.context.view_layer.objects.active=rig
bpy.ops.export_scene.fbx(filepath=str(ASSET/'Models/PigRonin.fbx'),use_selection=True,
    object_types={'EMPTY','MESH','ARMATURE'},axis_forward='-Z',axis_up='Y',apply_unit_scale=True,
    use_mesh_modifiers=True,use_armature_deform_only=True,add_leaf_bones=False,armature_nodetype='NULL',
    bake_anim=True,bake_anim_use_all_actions=True,bake_anim_use_nla_strips=False,
    bake_anim_simplify_factor=0,bake_anim_force_startend_keying=True)
bpy.ops.export_scene.gltf(filepath=str(OUT/'PigRonin.glb'),export_format='GLB',use_selection=True,
    export_yup=True,export_skins=True,export_animations=True,export_animation_mode='ACTIONS')
for o in char.objects:o.select_set(False)
rig.select_set(True);bpy.context.view_layer.objects.active=rig
bpy.ops.object.mode_set(mode='POSE')
bpy.ops.wm.save_as_mainfile(filepath=str(source))
report={'bones':len(arm.bones),'bone_names':bone_names,'skinned_objects':len(meshes),
    'vertices':sum(len(o.data.vertices) for o in meshes),'unweighted_vertices':0,
    'max_influences':maxinfluences,'max_original_pose_error':rest_error,'bind_pose':'T pose',
    'rebuilt_elbows':rebuilt_arms,'default_pose':'HandsOnHips','actions':['BindTPose','HandsOnHips','RigCheck'],'deformation_checks':checks}
(OUT/'rig_report.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
model=json.loads((OUT/'model_report.json').read_text());model['rigged']=True;model['bones']=len(arm.bones)
model['triangles']=sum(len(p.vertices)-2 for o in meshes for p in o.data.polygons)
model['notes']='30-bone skinned model, T-pose bind and pose/test actions; no texture dependencies.'
(OUT/'model_report.json').write_text(json.dumps(model,indent=2),encoding='utf-8')
print('PIG_RIG_COMPLETE '+json.dumps(report))
