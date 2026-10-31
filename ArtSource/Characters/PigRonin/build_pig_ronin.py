"""Procedural, editable pig ronin sculpt. Run with the bundled Blender in background."""
import bpy, bmesh, math, json
from pathlib import Path
from mathutils import Vector

ROOT = Path(__file__).resolve().parents[3]
OUT = ROOT / 'ArtSource/Characters/PigRonin'
ASSET = ROOT / 'Assets/Game/Prefabs/Characters/PigRonin'
OUT.mkdir(parents=True, exist_ok=True)
(ASSET / 'Models').mkdir(parents=True, exist_ok=True)
bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)
for d in bpy.data.materials:
    bpy.data.materials.remove(d)
CHAR = bpy.data.collections.new('CHARACTER | Jade Pig Ronin')
bpy.context.scene.collection.children.link(CHAR)
STUDIO = bpy.data.collections.new('STUDIO | render only')
bpy.context.scene.collection.children.link(STUDIO)

def mat(name, color, rough=.6, metal=0):
    m=bpy.data.materials.new(name); m.diffuse_color=(*color,1); m.use_nodes=True
    p=m.node_tree.nodes.get('Principled BSDF'); p.inputs['Base Color'].default_value=(*color,1)
    p.inputs['Roughness'].default_value=rough; p.inputs['Metallic'].default_value=metal
    return m

skin=mat('Skin_WarmIvory',(.83,.61,.48),.56)
snoutmat=mat('Snout_DustyRose',(.65,.29,.25),.58)
earmat=mat('Ears_Rose',(.57,.235,.22),.72)
dark=mat('Ink_Charcoal',(.025,.019,.026),.52)
nostrilmat=mat('Nostrils_DeepRose',(.15,.043,.043),.92)
white=mat('Eyes_Cream',(.96,.91,.76),.38)
teal=mat('Haori_Jade',(.028,.22,.205),.77)
tealdark=mat('Haori_DeepJade',(.016,.10,.105),.79)
trim=mat('Piping_Celadon',(.34,.56,.43),.71)
plum=mat('Hakama_InkPlum',(.125,.071,.17),.83)
plumlight=mat('Hakama_Fold',(.185,.105,.225),.82)
orange=mat('Obi_BurntCopper',(.61,.205,.072),.73)
gold=mat('Hardware_BrushedBrass',(.65,.40,.12),.4,.6)
leather=mat('Scabbard_Oxblood',(.12,.035,.04),.5)
wood=mat('Sandals_DarkWalnut',(.13,.061,.032),.74)
sole=mat('Sandals_WovenStraw',(.55,.36,.17),.87)

def move_col(o, col=CHAR):
    for c in list(o.users_collection): c.objects.unlink(o)
    col.objects.link(o)
    return o

def finish(o,name,material,smooth=True):
    o.name=name; move_col(o)
    if material:o.data.materials.append(material)
    if o.type=='MESH' and smooth:
        for p in o.data.polygons:p.use_smooth=True
    return o

def uv(name,loc,scale,material,seg=40,rings=24):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=seg,ring_count=rings,location=loc)
    o=bpy.context.object; o.scale=scale
    bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    return finish(o,name,material)

def mesh(name,verts,faces,material,sub=0):
    me=bpy.data.meshes.new(name);me.from_pydata(verts,[],faces);me.update()
    o=bpy.data.objects.new(name,me); CHAR.objects.link(o)
    if material:me.materials.append(material)
    for p in me.polygons:p.use_smooth=True
    if sub:
        mod=o.modifiers.new('Silhouette subdivision','SUBSURF');mod.levels=sub
        bpy.context.view_layer.objects.active=o; o.select_set(True)
        bpy.ops.object.modifier_apply(modifier=mod.name);o.select_set(False)
    return o

def tube(name,pts,radius,material,radii=None):
    cu=bpy.data.curves.new(name,'CURVE');cu.dimensions='3D';cu.resolution_u=12
    cu.bevel_depth=radius;cu.bevel_resolution=3;cu.resolution_u=16;cu.use_fill_caps=True
    sp=cu.splines.new('BEZIER');sp.bezier_points.add(len(pts)-1)
    for i,(p,co) in enumerate(zip(sp.bezier_points,pts)):
        p.co=co;p.handle_left_type='AUTO';p.handle_right_type='AUTO'
        if radii:p.radius=radii[i]
    o=bpy.data.objects.new(name,cu);CHAR.objects.link(o);cu.materials.append(material)
    return o

def bevelbox(name,loc,scale,material,bevel=.06):
    bpy.ops.mesh.primitive_cube_add(size=1,location=loc)
    o=bpy.context.object;o.scale=scale;bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    finish(o,name,material)
    b=o.modifiers.new('Soft tailored edges','BEVEL');b.width=bevel;b.segments=3
    bpy.context.view_layer.objects.active=o;bpy.ops.object.modifier_apply(modifier=b.name)
    return o

def link(name,a,b,width,material,depth=None):
    a,b=Vector(a),Vector(b);o=uv(name,(a+b)*.5,(width,depth or width,(b-a).length*.5+width*.55),material)
    o.rotation_euler=(b-a).to_track_quat('Z','Y').to_euler();return o

def cylinder(name,a,b,r,material,vertices=32):
    a,b=Vector(a),Vector(b);bpy.ops.mesh.primitive_cylinder_add(vertices=vertices,radius=r,depth=(b-a).length,location=(a+b)*.5)
    o=bpy.context.object;o.rotation_euler=(b-a).to_track_quat('Z','Y').to_euler()
    finish(o,name,material);mod=o.modifiers.new('Rounded rim','BEVEL');mod.width=min(r*.22,.025);mod.segments=3
    bpy.context.view_layer.objects.active=o;bpy.ops.object.modifier_apply(modifier=mod.name);return o

def join_remesh(objects,name,voxel=.035):
    bpy.ops.object.select_all(action='DESELECT')
    for o in objects:o.select_set(True)
    bpy.context.view_layer.objects.active=objects[0];bpy.ops.object.join();o=bpy.context.object;o.name=name
    mod=o.modifiers.new('Continuous sculpt','REMESH');mod.mode='VOXEL';mod.voxel_size=voxel;mod.use_smooth_shade=True
    bpy.ops.object.modifier_apply(modifier=mod.name)
    sm=o.modifiers.new('Relax sculpt','SMOOTH');sm.factor=.8;sm.iterations=4;bpy.ops.object.modifier_apply(modifier=sm.name)
    return o

# Wide, soft head and pear body: the recognizable silhouette stays deliberately simple.
body=join_remesh([uv('Torso',(0,0,1.38),(.64,.425,.69),skin),uv('Neck',(0,0,1.95),(.30,.29,.28),skin)],'Body_Sculpt',.027)
head=uv('Head_WideCheeks',(0,-.025,2.42),(.84,.55,.635),skin,64,40)
# A turned-up, thick ear with a concave inset rather than a flat triangle.
for s in (-1,1):
    outline=[(.49,-.015,2.79),(.56,.025,3.08),(.89,.055,3.255),(.99,.015,2.99),(.82,-.025,2.76)]
    front=[(s*x,y-.105,z) for x,y,z in outline]
    back=[(s*x,y+.10,z) for x,y,z in outline]
    center=(s*.755,-.09,2.975)
    verts=front+back+[center,(s*.755,.13,2.975)]
    faces=[]
    for i in range(5):
        j=(i+1)%5;faces.extend([(10,j,i),(11,5+i,5+j),(i,j,5+j,5+i)])
    ear=mesh(('L' if s<0 else 'R')+'_Ear_Sculpt',verts,faces,skin,2)
    inset=uv(('L' if s<0 else 'R')+'_Ear_Inner',(s*.765,-.094,3.004),(.111,.038,.151),earmat)
    inset.rotation_euler[1]=s*.42

# Snout is a separate sculpt with two real recessed nostrils.
sn=uv('Snout_WithRecessedNostrils',(0,-.54,2.32),(.355,.205,.263),snoutmat,56,36)
for s in (-1,1):
    cutter=uv('temporary_nostril',(s*.132,-.738,2.34),(.065,.100,.103),None,32,20)
    bpy.context.view_layer.objects.active=sn
    mod=sn.modifiers.new('Carved nostril','BOOLEAN');mod.operation='DIFFERENCE';mod.object=cutter
    bpy.ops.object.modifier_apply(modifier=mod.name);bpy.data.objects.remove(cutter,do_unlink=True)
    uv(('L' if s<0 else 'R')+'_Nostril_Depth',(s*.132,-.673,2.342),(.046,.036,.077),nostrilmat,24,16)

# Brows follow the ellipsoid surface, eyes sit below them in a proud, faintly suspicious stare.
for s in (-1,1):
    eye=uv(('L' if s<0 else 'R')+'_Eye_Cream',(s*.35,-.505,2.565),(.178,.067,.091),white)
    eye.rotation_euler[1]=-s*.16
    uv(('L' if s<0 else 'R')+'_Pupil',(s*.315,-.565,2.562),(.043,.027,.057),dark,28,18)
    uv(('L' if s<0 else 'R')+'_Eye_Glint',(s*.315-.012,-.586,2.582),(.011,.009,.015),white,16,12)
    pts=[(s*.155,-.535,2.645),(s*.29,-.529,2.713),(s*.445,-.47,2.749),(s*.566,-.393,2.738)]
    tube(('L' if s<0 else 'R')+'_Expressive_Brow',pts,.067,dark,[.77,1.12,1,.43])
    tube(('L' if s<0 else 'R')+'_LowerEyelid',[(s*.20,-.548,2.532),(s*.35,-.556,2.493),(s*.487,-.478,2.526)],.011,snoutmat)
# Off-center smirk, two short cheeks and a tiny chin crease.
tube('Smirk',[( -.23,-.509,2.101),(-.08,-.546,2.072),(.105,-.543,2.078),(.264,-.48,2.143)],.014,dark,[.3,.95,1,.35])
tube('Smirk_Corner',[(.257,-.477,2.119),(.272,-.477,2.159),(.304,-.457,2.164)],.010,nostrilmat)
tube('Chin_Crease',[(-.074,-.451,1.998),(0,-.466,1.982),(.074,-.451,1.998)],.008,snoutmat)

# Trousers: two rounded legs with purposeful cloth folds and an asymmetric short coat.
trousers=[uv('Hakama_Hips',(0,.025,.91),(.60,.395,.36),plum)]
for s in (-1,1):
    trousers.append(uv(('L' if s<0 else 'R')+'_Hakama_Leg',(s*.305,.025,.665),(.285,.34,.395),plum))
join_remesh(trousers,'Hakama_ContinuousSculpt',.022)
def pants_front(x,z):
    result=[]
    for cx,cz,rx,ry,rz in [(0,.91,.60,.395,.36),(-.305,.665,.285,.34,.395),(.305,.665,.285,.34,.395)]:
        k=1-((x-cx)/rx)**2-((z-cz)/rz)**2
        if k>0:result.append(.025-ry*math.sqrt(k))
    return min(result)-.004 if result else -.20
for s in (-1,1):
    for offset in (-.105,.03,.14):
        x=s*.305+offset
        pts=[(x+offset*.18*t,pants_front(x+offset*.18*t,.98-.51*t),.98-.51*t) for t in (0,.25,.5,.75,1)]
        tube('Hakama_RaisedPleat',pts,.012,plumlight,[.1,.8,1,.8,.1])
    uv('Ankle',(s*.305,-.005,.289),(.125,.14,.155),skin)
    uv('Cloven_Foot',(s*.305,-.16,.18),(.217,.31,.15),skin)
    for z,thick,material in [(.068,.085,wood),(.115,.052,sole)]:
        bevelbox('Geta_Sandal',(s*.305,-.14,z),(.465,.68,thick),material,.045)
    tube('Hoof_Division',[(s*.305,-.455,.13),(s*.305,-.465,.19),(s*.305,-.423,.255)],.013,snoutmat)
    tube('Sandal_Thong',[(s*.305-.18,-.08,.267),(s*.305-.095,-.22,.319),(s*.305,-.315,.321),(s*.305+.095,-.22,.319),(s*.305+.18,-.08,.267)],.025,tealdark)

# Wrap coat made as a continuous shell over the torso's back and sides, open at the belly.
verts=[];faces=[];n=64
levels=[]
for z in (1.01,1.11,1.27,1.43,1.59,1.75,1.87,1.97):
    f=math.sqrt(1-((z-1.38)/.69)**2)
    levels.append((z,.64*f+.046,.425*f+.046))
for z,rx,ry in levels:
    for i in range(n+1):
        # 0 faces front; a 92 degree opening exposes the belly.
        a=math.radians(38)+(math.tau-math.radians(76))*i/n
        verts.append((rx*math.sin(a),-ry*math.cos(a),z))
for j in range(len(levels)-1):
    for i in range(n):
        k=j*(n+1)+i;faces.append((k,k+1,k+n+2,k+n+1))
coat=mesh('Haori_OpenShell',verts,faces,teal,2)
sol=coat.modifiers.new('Cloth thickness','SOLIDIFY');sol.thickness=.025
bpy.context.view_layer.objects.active=coat;bpy.ops.object.modifier_apply(modifier=sol.name)
for s in (-1,1):
    pts=[(s*rx*math.sin(math.radians(38)),-ry*math.cos(math.radians(38))-.009,z) for z,rx,ry in levels]
    tube('Haori_BoundLapel',pts,.030,trim)
    # Hands on hips; each arm is fused into a single soft shape.
    arm=join_remesh([link('UpperArm',(s*.51,.0,1.77),(s*.90,-.005,1.48),.18,skin),link('Forearm',(s*.90,-.005,1.48),(s*.66,-.32,1.27),.155,skin),uv('Hand',(s*.65,-.339,1.269),(.18,.133,.163),skin)],('L' if s<0 else 'R')+'_Arm_HandOnHip',.023)
    uv('Thumb',(s*.556,-.38,1.321),(.087,.08,.105),skin)
    tube('Hand_ClovenCrease',[(s*.65,-.464,1.202),(s*.658,-.474,1.26),(s*.675,-.453,1.30)],.010,snoutmat)

# Wide rust obi, fabric layers, small brass seal and hanging sash ends.
belt=uv('Obi_WideWrap',(0,.005,1.065),(.665,.458,.143),orange,56,20)
for z in (1.005,1.126):
    tube('Obi_Stitch',[(.65*math.sin(i*math.tau/64),-.452*math.cos(i*math.tau/64),z) for i in range(65)],.007,gold)
uv('Obi_Knot',(.185,-.46,1.066),(.107,.066,.112),orange)
for x,angle in ((.16,-.12),(.28,.19)):
    o=bevelbox('Obi_HangingEnd',(x,-.42,.865),(.115,.045,.34),orange,.025);o.rotation_euler[1]=angle
    tube('Sash_End_Seam',[(x-.035,-.452,.724),(x+.035,-.452,.724)],.006,gold)
seal=cylinder('Obi_BrassSeal',(-.18,-.452,1.062),(-.18,-.49,1.062),.083,gold)
uv('Seal_JadeInset',(-.18,-.496,1.062),(.054,.014,.054),teal)

# Minimal crest on the back: a brass ring and three seed-like marks.
crestpts=[(.115*math.cos(a),.470,1.526+.115*math.sin(a)) for a in [i*math.tau/48 for i in range(49)]]
tube('Back_Crest_Ring',crestpts,.009,gold)
for a in (-.6,.6,math.pi):
    x=.043*math.sin(a);z=1.526+.044*math.cos(a)
    o=uv('Back_Crest_Seed',(x,.48,z),(.021,.012,.043),gold,20,12);o.rotation_euler[1]=a

# Curly pig tail remains visible below the coat on the back.
tailpts=[]
for i in range(38):
    t=i/37;a=t*math.pi*3.3;r=.105*(1-.45*t)
    tailpts.append((r*math.sin(a),.35+.15*t+r*(1-math.cos(a)),.91+.15*t))
tube('Curly_Tail',tailpts,.037,skin,[1-i/45 for i in range(38)])

# A proper sheathed short sword at the left hip, with woven grip and separate fittings.
pom=Vector((-1.06,.19,1.54));tip=Vector((-.74,.50,.42));axis=(pom-tip).normalized()
guard=pom-axis*.30;scabend=tip
cylinder('Saya_Scabbard',scabend,guard-axis*.045,.067,tealdark)
cylinder('Saya_BrassChape',tip-axis*.015,tip+axis*.08,.071,gold)
cylinder('Saya_Mouth',guard-axis*.10,guard-axis*.025,.073,gold)
cylinder('Tsuka_Grip',guard+axis*.035,pom,.057,leather)
g=uv('Tsuba_Guard',guard,(.123,.123,.024),gold);g.rotation_euler=axis.to_track_quat('Z','Y').to_euler()
cylinder('Kashira_Pommel',pom-axis*.025,pom+axis*.012,.063,gold)
v=axis.cross(Vector((0,1,0))).normalized();w=axis.cross(v).normalized()
for i in range(7):
    c=guard+axis*(.063+i*.031)
    # Two diagonal bands cross the cylinder, giving actual raised woven geometry.
    for direction in (-1,1):
        pts=[]
        for j in range(13):
            a=j/12*math.tau; p=c+v*math.cos(a)*.059+w*math.sin(a)*.059+axis*(direction*(j/12-.5)*.028)
            pts.append(tuple(p))
        tube('Tsuka_WovenWrap',pts,.009,trim)
tube('Sword_SuspensionCord',[(-.59,-.05,1.12),(-.71,-.04,1.18),tuple(guard-axis*.10),(-.58,.10,1.08)],.018,orange)

# Convert curves, apply transforms, merge matching materials by logical edit group.
bpy.ops.object.select_all(action='DESELECT')
for o in list(CHAR.objects):
    o.select_set(True)
    bpy.context.view_layer.objects.active=o
    if o.type=='CURVE': bpy.ops.object.convert(target='MESH')
    bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    o.select_set(False)
    # Preserve editable source topology while exporting a substantially lighter surface.
    if o.type=='MESH':
        bm=bmesh.new();bm.from_mesh(o.data)
        bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bm.to_mesh(o.data);bm.free();o.data.update()
        count=len(o.data.polygons)
        if count>600:
            d=o.modifiers.new('Game surface reduction (non destructive)','DECIMATE')
            d.ratio=.22 if count>4000 else (.35 if count>1000 else .55)
            d.use_collapse_triangulate=True
root=bpy.data.objects.new('PigRonin',None);CHAR.objects.link(root)
for o in list(CHAR.objects):
    if o!=root:o.parent=root

# Export a true 3D asset; the studio is deliberately excluded.
bpy.ops.object.select_all(action='DESELECT')
for o in CHAR.objects:o.select_set(True)
bpy.context.view_layer.objects.active=root
bpy.ops.export_scene.fbx(filepath=str(ASSET/'Models/PigRonin.fbx'),use_selection=True,object_types={'EMPTY','MESH'},apply_unit_scale=True,axis_forward='-Z',axis_up='Y',bake_anim=False,add_leaf_bones=False,use_mesh_modifiers=True)
bpy.ops.export_scene.gltf(filepath=str(OUT/'PigRonin.glb'),export_format='GLB',use_selection=True,export_yup=True)

# Studio lighting, a broad plinth, orthographic product camera.
def studio(o):move_col(o,STUDIO);return o
floor=mat('Studio_Charcoal',(.028,.044,.048),.86)
base=mat('Studio_Platform',(.063,.09,.09),.77)
bpy.ops.mesh.primitive_plane_add(size=200,location=(0,0,-.085));o=studio(bpy.context.object);o.data.materials.append(floor)
bpy.ops.mesh.primitive_cylinder_add(vertices=96,radius=1.30,depth=.14,location=(0,0,-.075));o=studio(bpy.context.object);o.name='Display_Platform';o.data.materials.append(base)
be=o.modifiers.new('Soft rim','BEVEL');be.width=.055;be.segments=4
for p in o.data.polygons:p.use_smooth=True

def area(name,loc,power,color,size):
    data=bpy.data.lights.new(name,'AREA');data.energy=power;data.color=color;data.shape='DISK';data.size=size
    o=bpy.data.objects.new(name,data);STUDIO.objects.link(o);o.location=loc;o.rotation_euler=(Vector((0,0,1.6))-o.location).to_track_quat('-Z','Y').to_euler()
area('Key_Softbox',(-3.5,-4.5,6),650,(1,.84,.68),4)
area('Fill_Softbox',(4,-2,3.8),420,(.68,.84,1),3)
area('Rim_Light',(1.5,3.5,5.3),950,(.69,1,.87),3)
sc=bpy.context.scene
sc.world.color=(.11,.11,.11)
sc.render.engine='CYCLES';sc.cycles.samples=32;sc.cycles.use_denoising=True
sc.render.resolution_x=1100;sc.render.resolution_y=1200;sc.render.resolution_percentage=100
sc.view_settings.view_transform='AgX'
bpy.ops.object.camera_add();cam=studio(bpy.context.object);cam.name='Product_Camera';sc.camera=cam;cam.data.type='ORTHO';cam.data.ortho_scale=4.05
def aim(pos):
    cam.location=pos;cam.rotation_euler=(Vector((0,0,1.60))-cam.location).to_track_quat('-Z','Y').to_euler()
aim((-4,-8,4.0))
sc.render.image_settings.file_format='PNG';sc.render.filepath=str(OUT/'PigRonin_Hero.png')
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'PigRonin.blend'))
bpy.ops.render.render(write_still=True)
for label,pos in [('Front',(0,-9,2.7)),('Back',(3,8,3.4)),('Side',(-8,-2,3.0))]:
    aim(pos);sc.render.resolution_x=800;sc.render.resolution_y=1000;sc.render.filepath=str(OUT/f'PigRonin_{label}.png');bpy.ops.render.render(write_still=True)

objects=[o for o in CHAR.objects if o.type=='MESH']
deps=bpy.context.evaluated_depsgraph_get();tris=0
for o in objects:
    me=o.evaluated_get(deps).to_mesh();me.calc_loop_triangles();tris+=len(me.loop_triangles);o.evaluated_get(deps).to_mesh_clear()
report={'name':'Jade Pig Ronin','mesh_objects':len(objects),'triangles':tris,'materials':len({m.name for o in objects for m in o.data.materials if m}),'rigged':False,'format':['blend','fbx','glb'],'front':'Blender -Y, Unity +Z after FBX import','notes':'Static multi-part character, real recessed nostrils; no texture dependencies.'}
(OUT/'model_report.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
print('PIG_RONIN_COMPLETE '+json.dumps(report))

# Finish the current deliverable with its skinned skeleton after rebuilding the base surface.
import runpy
runpy.run_path(str(OUT/'rig_pig_ronin.py'),run_name='__main__')
