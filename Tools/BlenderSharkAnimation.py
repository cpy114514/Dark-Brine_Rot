"""Rig the original Tralalero mesh; create editable, baked shark actions."""
import bpy, json, math, os, sys
from mathutils import Vector
ROOT=os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
OUT=os.path.join(ROOT,'.codex','shark-blender');os.makedirs(OUT,exist_ok=True)
SOURCE=os.path.join(ROOT,'Assets/Game/Prefabs/Characters/TralaleroTralala/Models/TralaleroTralala.fbx')
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=SOURCE)
objects=[o for o in bpy.context.scene.objects if o.type=='MESH']
obj=objects[0]
points=[obj.matrix_world@v.co for v in obj.data.vertices]
# Audit connected surfaces to isolate fins, mouth and shoes without cutting the mesh.
parents=list(range(len(points)))
def find(i):
    while parents[i]!=i:
        parents[i]=parents[parents[i]];i=parents[i]
    return i
for edge in obj.data.edges:
    a,b=edge.vertices;parents[find(b)]=find(a)
parts={}
for i in range(len(points)):parts.setdefault(find(i),[]).append(i)
audit=[]
for ids in parts.values():
    ps=[points[i] for i in ids]
    audit.append(dict(count=len(ids),minimum=[min(p[i] for p in ps) for i in range(3)],
                      maximum=[max(p[i] for p in ps) for i in range(3)],centre=list(sum(ps,Vector())/len(ps))))
report=dict(objects=[dict(name=o.name,matrix=[list(row) for row in o.matrix_world]) for o in objects],
            vertices=len(points),minimum=[min(p[i] for p in points) for i in range(3)],
            maximum=[max(p[i] for p in points) for i in range(3)],parts=sorted(audit,key=lambda p:-p['count'])[:45],
            materials=[m.name for m in obj.data.materials])
with open(os.path.join(OUT,'audit.json'),'w') as file:json.dump(report,file,indent=2)
print(json.dumps(report),flush=True)
if '--build' not in sys.argv:sys.exit(0)

# Restore the original mesh-local facing, with Blender Z up and -Y forward.
# Geometry, faces, UVs and the texture are retained; only vertex weights are added.
for vertex in obj.data.vertices:
    p=vertex.co.copy();vertex.co=(p.x,-p.z,p.y)
obj.matrix_world.identity();obj.name='Tralalero_Skin'
bpy.context.view_layer.objects.active=obj;obj.select_set(True)
texture=os.path.join(ROOT,'Assets/Game/Prefabs/Characters/TralaleroTralala/Textures/shaded.png')
for mat in obj.data.materials:
    mat.use_nodes=True
    shader=next(n for n in mat.node_tree.nodes if n.type=='BSDF_PRINCIPLED')
    node=mat.node_tree.nodes.new('ShaderNodeTexImage');node.image=bpy.data.images.load(texture,check_existing=True)
    mat.node_tree.links.new(node.outputs['Color'],shader.inputs['Base Color']);shader.inputs['Roughness'].default_value=.55
rig_data=bpy.data.armatures.new('Tralalero_Anatomy')
rig=bpy.data.objects.new('Tralalero_Rig',rig_data);bpy.context.collection.objects.link(rig)
bpy.ops.object.select_all(action='DESELECT');rig.select_set(True);bpy.context.view_layer.objects.active=rig
bpy.ops.object.mode_set(mode='EDIT')
bones={}
def bone(name,head,tail,parent=None,deform=True):
    b=rig_data.edit_bones.new(name);b.head=head;b.tail=tail;b.use_deform=deform
    if parent:b.parent=bones[parent]
    bones[name]=b;return b
bone('Body',(0,0,5.3),(0,1.5,5.3))
bone('Spine_Mid',(0,1.5,5.3),(0,3.5,5.3),'Body')
bone('Spine_Rear',(0,3.5,5.3),(0,5.5,5.2),'Spine_Mid')
bone('Tail_Base',(0,5.5,5.2),(0,8.2,5.4),'Spine_Rear')
bone('Tail_Mid',(0,8.2,5.4),(0,10.2,5.6),'Tail_Base')
bone('Tail_Tip',(0,10.2,5.6),(0,12.3,5.9),'Tail_Mid')
bone('Head',(0,-3.5,5.3),(0,-10.5,5.3),'Body')
bone('Jaw',(0,-6.8,5.2),(0,-11.5,4.8),'Head')
bone('Fin_Left',(-2.5,.5,4.5),(-5.5,1,4.2),'Body')
bone('Fin_Right',(2.5,.5,4.5),(5.5,1,4.2),'Body')
bone('Dorsal',(0,1.5,7),(0,1.5,11.9),'Spine_Mid')
bone('Foot_Left',(-2.1,0,3),(-3.5,-.2,.8),'Body')
bone('Foot_Right',(2.1,0,3),(3.5,-.2,.8),'Body')
bone('Foot_Rear',(0,4,3),(0,4,.8),'Spine_Mid')
pts=[v.co.copy() for v in obj.data.vertices]
lo=min(p.y for p in pts);hi=max(p.y for p in pts);band=(hi-lo)*.02
nose=sum((p for p in pts if p.y<lo+band),Vector())/sum(p.y<lo+band for p in pts);nose.y=lo
tail=sum((p for p in pts if p.y>hi-band),Vector())/sum(p.y>hi-band for p in pts);tail.y=hi
bone('Nose_Marker',nose,nose+Vector((0,-.3,0)),'Head',False)
bone('Tail_Marker',tail,tail+Vector((0,.3,0)),'Tail_Tip',False)
bpy.ops.object.mode_set(mode='OBJECT')
groups={b.name:obj.vertex_groups.new(name=b.name) for b in rig_data.bones if b.use_deform}
def clamp(v):return max(0,min(1,v))
spine=[('Head',-5),('Body',0),('Spine_Mid',2.5),('Spine_Rear',4.5),('Tail_Base',6.8),('Tail_Mid',9.2),('Tail_Tip',11.5)]
for vertex in obj.data.vertices:
    x,y,z=vertex.co
    if y<=spine[0][1]:weights={'Head':1}
    elif y>=spine[-1][1]:weights={'Tail_Tip':1}
    else:
        for (a,ya),(b,yb) in zip(spine,spine[1:]):
            if ya<=y<=yb:
                t=clamp((y-ya)/(yb-ya));t=t*t*(3-2*t);weights={a:1-t,b:t};break
    overlay=None;w=0
    if z<3.0 and y<6:
        seeds=[('Foot_Left',Vector((-3.5,-.2,1))),('Foot_Right',Vector((3.5,-.2,1))),('Foot_Rear',Vector((0,4,1)))]
        overlay=min(seeds,key=lambda pair:(vertex.co-pair[1]).length_squared)[0];w=clamp((3.2-z)/1.4)
    elif y<-6.2 and z<5.4:
        overlay='Jaw';w=clamp((-y-6.2)/2.8)*clamp((5.4-z)/.85)
    elif abs(x)>2.8 and -.5<y<3 and z<6.3:
        overlay='Fin_Left' if x<0 else 'Fin_Right';w=clamp((abs(x)-2.8)/2.2)
    elif z>7.5 and abs(x)<1.7:
        overlay='Dorsal';w=clamp((z-7.5)/1.5)
    if overlay:
        weights={k:v*(1-w) for k,v in weights.items()};weights[overlay]=weights.get(overlay,0)+w
    weights={k:v for k,v in weights.items() if v>.0001};total=sum(weights.values())
    for name,value in weights.items():groups[name].add([vertex.index],value/total,'REPLACE')
modifier=obj.modifiers.new('Anatomical deformation','ARMATURE');modifier.object=rig;modifier.use_deform_preserve_volume=True
obj.parent=rig
rig.show_in_front=True;rig_data.display_type='BBONE';rig.animation_data_create()
for p in rig.pose.bones:p.rotation_mode='XYZ'
FPS=60;bpy.context.scene.render.fps=FPS
actions=[]
def degrees(v):return math.radians(v)
def pulse(t,start,peak,end):
    if t<start or t>end:return 0
    u=(t-start)/(peak-start) if t<=peak else (end-t)/(end-peak)
    return .5-.5*math.cos(math.pi*clamp(u))
def strike(t,wind,hit,end):
    if t<=wind:return -.5*(1-math.cos(math.pi*clamp(t/wind)))
    if t<=hit:return -1+2.0*(.5-.5*math.cos(math.pi*(t-wind)/(hit-wind)))
    return 1-(.5-.5*math.cos(math.pi*clamp((t-hit)/(end-hit))))
def build_action(name,duration,kind):
    action=bpy.data.actions.new(name);action.use_fake_user=True;rig.animation_data.action=action
    frames=round(duration*FPS)
    for frame in range(frames+1):
        t=frame/FPS;bpy.context.scene.frame_set(frame+1)
        for p in rig.pose.bones:p.rotation_euler=(0,0,0);p.location=(0,0,0);p.scale=(1,1,1)
        def rotate(name,x=0,y=0,z=0):rig.pose.bones[name].rotation_euler=(degrees(x),degrees(y),degrees(z))
        if kind in ('swim','fast'):
            phase=2*math.pi*t/duration;effort=1.0 if kind=='swim' else 1.45
            for i,(name,amplitude) in enumerate([('Body',1.8),('Spine_Mid',4),('Spine_Rear',8),('Tail_Base',12),('Tail_Mid',15),('Tail_Tip',18)]):
                rotate(name,z=amplitude*effort*math.sin(phase-i*.46))
            rotate('Head',z=-1.1*effort*math.sin(phase));rotate('Dorsal',y=3*math.sin(phase-.3))
            rotate('Fin_Left',x=4*math.sin(phase+.4),y=5*math.sin(phase));rotate('Fin_Right',x=-4*math.sin(phase+.4),y=-5*math.sin(phase))
            rotate('Foot_Left',x=5*math.sin(phase+.6),z=3*math.sin(phase));rotate('Foot_Right',x=-5*math.sin(phase+.6),z=-3*math.sin(phase))
            rotate('Foot_Rear',x=3*math.sin(phase-1));rotate('Jaw',x=1.5+1.2*math.sin(phase))
        elif kind in ('tail','smash'):
            swing=strike(t,.25,.65,duration);strong=1.0 if kind=='tail' else 1.22
            rotate('Body',x=-3*pulse(t,0,.4,.9),y=4*swing,z=-6*swing)
            for name,amount in [('Spine_Mid',7),('Spine_Rear',13),('Tail_Base',27),('Tail_Mid',25),('Tail_Tip',15)]:rotate(name,z=amount*strong*swing)
            rotate('Head',z=4*swing);rotate('Dorsal',y=-10*swing)
            rotate('Fin_Left',y=14*swing);rotate('Fin_Right',y=-14*swing)
            rotate('Foot_Left',x=-12*pulse(t,.15,.55,1.0),z=8*swing);rotate('Foot_Right',x=12*pulse(t,.15,.55,1.0),z=-8*swing)
            rotate('Foot_Rear',x=-9*swing);rotate('Jaw',x=10*pulse(t,.1,.5,1.05))
        elif kind=='bite':
            lift=pulse(t,0,.32,.8);snap=pulse(t,.3,.45,.8)
            rotate('Body',x=-7*lift+4*snap,z=-4*lift);rotate('Head',x=-4*lift+7*snap)
            rotate('Jaw',x=30*pulse(t,0,.3,.5)+3*pulse(t,.5,.65,.9))
            for name,amp in [('Spine_Mid',4),('Spine_Rear',9),('Tail_Base',15),('Tail_Mid',18),('Tail_Tip',13)]:rotate(name,z=amp*math.sin(t/duration*math.pi*2)*pulse(t,0,.5,duration))
            rotate('Fin_Left',y=12*lift);rotate('Fin_Right',y=-12*lift)
            rotate('Foot_Left',x=-10*lift);rotate('Foot_Right',x=-10*lift);rotate('Foot_Rear',x=8*lift)
        elif kind=='recoil':
            hit=pulse(t,0,.13,duration);twist=hit*math.cos(t*11)
            rotate('Body',x=-6*hit,y=10*twist,z=-9*hit);rotate('Head',x=6*hit,z=5*hit)
            for name,amp in [('Spine_Mid',7),('Spine_Rear',10),('Tail_Base',14),('Tail_Mid',12),('Tail_Tip',8)]:rotate(name,z=amp*hit)
            rotate('Fin_Left',y=18*hit);rotate('Fin_Right',y=-18*hit);rotate('Dorsal',y=-12*twist)
            rotate('Jaw',x=10*hit);rotate('Foot_Left',x=-13*hit);rotate('Foot_Right',x=11*hit);rotate('Foot_Rear',x=-10*hit)
        elif kind=='threat':
            p=pulse(t,0,.65,duration);rotate('Body',x=-5*p,y=3*p);rotate('Jaw',x=18*p)
            for i,name in enumerate(['Spine_Mid','Spine_Rear','Tail_Base','Tail_Mid','Tail_Tip']):rotate(name,z=(3+i*2)*math.sin(t*5-i*.5)*p)
            rotate('Fin_Left',y=12*p);rotate('Fin_Right',y=-12*p)
        for p in rig.pose.bones:
            if p.bone.use_deform:
                p.keyframe_insert(data_path='rotation_euler',frame=frame+1,group=p.name)
    actions.append(dict(name=action.name,seconds=duration,frames=frames+1,loop=kind in ('swim','fast')))
    return action
for spec in [('Swim_Cruise',1.6,'swim'),('Swim_Fast',1.0,'fast'),('Tail_Strike',1.15,'tail'),('Ship_Smash',1.15,'smash'),('Bite_Lunge',1.1,'bite'),('Hit_Recoil',.8,'recoil'),('Threat',1.8,'threat')]:build_action(*spec)

rig.animation_data.action=bpy.data.actions['Swim_Cruise'];bpy.context.scene.frame_start=1;bpy.context.scene.frame_end=97;bpy.context.scene.frame_set(1)
for image in bpy.data.images:
    if image.source=='FILE':
        try:image.pack()
        except RuntimeError:pass
source_dir=os.path.join(ROOT,'ArtSource','SharkAnimation');os.makedirs(source_dir,exist_ok=True)
bpy.context.preferences.filepaths.save_version=0
native=os.path.join(source_dir,'tralalero_animated.blend');bpy.ops.wm.save_as_mainfile(filepath=native)
fbx_dir=os.path.join(ROOT,'Assets','Resources','SharkAnimation');os.makedirs(fbx_dir,exist_ok=True)
fbx=os.path.join(fbx_dir,'TralaleroAnimated.fbx')
bpy.ops.object.select_all(action='DESELECT');obj.select_set(True);rig.select_set(True);bpy.context.view_layer.objects.active=rig
bpy.ops.export_scene.fbx(filepath=fbx,use_selection=True,object_types={'ARMATURE','MESH'},
                        axis_forward='-Z',axis_up='Y',apply_unit_scale=True,apply_scale_options='FBX_SCALE_ALL',
                        add_leaf_bones=False,use_armature_deform_only=False,bake_anim=True,bake_anim_use_all_actions=True,
                        bake_anim_use_nla_strips=False,bake_anim_step=1,bake_anim_simplify_factor=0,
                        path_mode='AUTO',use_mesh_modifiers=True)
with open(os.path.join(OUT,'build-report.json'),'w') as file:json.dump(dict(source=SOURCE,blend=native,fbx=fbx,vertices=len(obj.data.vertices),bones=len(rig_data.bones),actions=actions),file,indent=2)
print('SHARK_RIG_COMPLETE',json.dumps(actions),flush=True)
