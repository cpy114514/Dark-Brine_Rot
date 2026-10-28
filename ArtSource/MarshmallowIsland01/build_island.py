import bpy, math, random, os, json
from mathutils import Vector
random.seed(19)
OUT=os.path.dirname(os.path.abspath(__file__))
bpy.ops.object.select_all(action='SELECT'); bpy.ops.object.delete(use_global=False)

def material(name, color):
    m=bpy.data.materials.new(name); m.diffuse_color=(*color,1); m.use_nodes=True
    n=m.node_tree.nodes; l=m.node_tree.links; p=n.get('Principled BSDF')
    p.inputs['Base Color'].default_value=(*color,1)
    p.inputs['Roughness'].default_value=.83
    p.inputs['Subsurface Weight'].default_value=.12
    p.inputs['Subsurface Radius'].default_value=(1,.45,.3)
    tex=n.new('ShaderNodeTexNoise'); tex.inputs['Scale'].default_value=115; tex.inputs['Detail'].default_value=3
    bump=n.new('ShaderNodeBump'); bump.inputs['Strength'].default_value=.10; bump.inputs['Distance'].default_value=.022
    l.new(tex.outputs['Fac'],bump.inputs['Height']); l.new(bump.outputs['Normal'],p.inputs['Normal'])
    return m

pink=material('Spun sugar - rose and vanilla',(.96,.59,.73))
n=pink.node_tree.nodes; l=pink.node_tree.links; p=n.get('Principled BSDF')
tex=n.new('ShaderNodeTexNoise'); tex.inputs['Scale'].default_value=.48; tex.inputs['Detail'].default_value=2; tex.inputs['Roughness'].default_value=.7
r=n.new('ShaderNodeValToRGB'); r.color_ramp.elements[0].position=.22; r.color_ramp.elements[0].color=(.62,.26,.46,1); r.color_ramp.elements[1].position=.77; r.color_ramp.elements[1].color=(1,.88,.83,1)
r.color_ramp.elements.new(.48).color=(1,.57,.72,1)
l.new(tex.outputs['Fac'],r.inputs[0]); l.new(r.outputs[0],p.inputs['Base Color'])
parts=[]
def puff(loc,scale):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=48,ring_count=32,location=loc)
    o=bpy.context.object; o.scale=scale; bpy.ops.object.transform_apply(location=False,rotation=False,scale=True); parts.append(o)

# Asymmetric overlapping billows, with smaller curls rather than a scalloped disc.
for loc,scale in [((-.9,.4,-.1),(4.4,3.8,2.1)),((-2.6,.6,.65),(3.25,3.05,2.25)),((1.4,1,.7),(3.4,3.0,2.0)),((.7,-1.8,.2),(3.6,2.65,1.8)),((-3.4,-1.4,-.55),(2.5,2.3,1.7)),((3.8,.2,-.4),(2.4,2.5,1.75)),((-.6,.5,-1.6),(3.7,3.15,1.9)),((.6,.8,-2.8),(2.45,2.1,1.65)),((1.6,.6,-3.65),(1.25,1.3,1.3)),((2.05,.5,-4.35),(.6,.75,.85))]:
    puff(loc,scale)
for i in range(23):
    a=random.uniform(0,math.tau); rr=random.uniform(.77,1.05); size=random.uniform(.7,1.6)
    puff((4.7*math.cos(a)*rr,3.6*math.sin(a)*rr,random.uniform(-1.25,.9)),(size*1.2,size,random.uniform(.75,1.4)))
for loc,scale in [((-2.4,.3,1.85),(2.2,2.0,1.2)),((.6,1.5,1.8),(2.1,1.7,1.1)),((2.5,-.5,1.2),(1.9,1.7,1.1)),((-.7,-2.2,1.2),(2.25,1.8,1.0))]:puff(loc,scale)
# Uneven curls soften the large billows and break the outline at several scales.
for i in range(47):
    a=random.uniform(0,math.tau); size=random.uniform(.32,.83)
    x=math.cos(a)*random.uniform(4.7,5.55); y=math.sin(a)*random.uniform(3.5,4.25)
    puff((x,y,random.uniform(-1.15,1.3)),(size*random.uniform(1,1.65),size, size*random.uniform(.8,1.35)))
for i in range(19):
    a=random.uniform(0,math.tau); rr=random.uniform(.3,1)
    puff((math.cos(a)*3.6*rr,math.sin(a)*2.8*rr,random.uniform(1.7,2.35)),(random.uniform(.7,1.25),random.uniform(.65,1.2),random.uniform(.5,.85)))
bpy.ops.object.select_all(action='DESELECT')
for o in parts:o.select_set(True)
bpy.context.view_layer.objects.active=parts[0]; bpy.ops.object.join(); island=bpy.context.object; island.name='Island_01_CottonCandy_HighRes'
rem=island.modifiers.new('Seamless sugar cloud union','REMESH'); rem.mode='VOXEL'; rem.voxel_size=.075; rem.use_smooth_shade=True
bpy.ops.object.modifier_apply(modifier=rem.name)
sm=island.modifiers.new('Soft blended lobes','SMOOTH'); sm.factor=1.6; sm.iterations=7; bpy.ops.object.modifier_apply(modifier=sm.name)
sub=island.modifiers.new('High resolution surface','SUBSURF'); sub.levels=1; bpy.ops.object.modifier_apply(modifier=sub.name)
for label,scale,strength,depth in [('Soft irregular loft',1.15,.16,1),('Cotton curls',.32,.038,1),('Fine spun fibres',.065,.009,1)]:
    t=bpy.data.textures.new(label,type='CLOUDS'); t.noise_scale=scale; t.noise_depth=depth
    d=island.modifiers.new(label,'DISPLACE'); d.texture=t; d.strength=strength; d.mid_level=.5; bpy.ops.object.modifier_apply(modifier=d.name)
island.data.materials.append(pink)
# Spherical UVs and a portable pastel texture shared by Blender, glTF and Unity.
from mathutils import noise
uv=island.data.uv_layers.new(name='SugarUV')
for poly in island.data.polygons:
    coords=[]
    for li in poly.loop_indices:
        v=island.data.vertices[island.data.loops[li].vertex_index].co
        q=Vector((v.x/6.5,v.y/5.0,(v.z+.8)/4.5)).normalized()
        coords.append([li,.5+math.atan2(q.y,q.x)/(2*math.pi),.5+math.asin(max(-1,min(1,q.z)))/math.pi])
    if max(c[1] for c in coords)-min(c[1] for c in coords)>.5:
        for c in coords:
            if c[1]<.5:c[1]+=1
    for li,u,v in coords:uv.data[li].uv=(u,v)
N=1024; pixels=[]
for y in range(N):
    lat=(y/(N-1)-.5)*math.pi
    for x in range(N):
        lon=(x/(N-1)-.5)*math.tau
        q=Vector((math.cos(lat)*math.cos(lon),math.cos(lat)*math.sin(lon),math.sin(lat)))
        f=noise.noise_vector(q*2.8)[0]*.5+.5
        f=max(0,min(1,(f-.22)*1.8))
        fine=noise.noise(q*190)*.012
        pixels.extend((.99+fine,.74+.20*f+fine,.84+.12*f+fine,1))
img=bpy.data.images.new('CottonCandy_Pastel',width=N,height=N); img.pixels.foreach_set(pixels); img.filepath_raw=os.path.join(OUT,'CottonCandy_Pastel.png'); img.file_format='PNG'; img.save(); img.pack()
im=n.new('ShaderNodeTexImage'); im.image=img; l.new(im.outputs['Color'],p.inputs['Base Color'])
for f in island.data.polygons:f.use_smooth=True
island['purpose']='First floating island; cotton candy cloud with broad landing crown'
island['units']='metres'
# Keep the portable export self contained; procedural material remains editable in .blend.
bpy.ops.object.select_all(action='DESELECT'); island.select_set(True); bpy.context.view_layer.objects.active=island
bpy.ops.wm.obj_export(filepath=os.path.join(OUT,'CottonCandy_Island01_HighRes.obj'),export_selected_objects=True,export_materials=True)
bpy.ops.export_scene.gltf(filepath=os.path.join(OUT,'CottonCandy_Island01.glb'),use_selection=True,export_format='GLB')

def aim(o,pt):o.rotation_euler=(Vector(pt)-o.location).to_track_quat('-Z','Y').to_euler()
bpy.ops.object.camera_add(location=(17,-23,14)); camera=bpy.context.object; camera.name='Preview_Camera'; aim(camera,(0,0,-.9)); camera.data.type='ORTHO'; camera.data.ortho_scale=20.7; bpy.context.scene.camera=camera
for name,loc,power,size,col in [('Key', (2,-9,14),2100,9,(1,.87,.8)),('Fill',(-10,-3,5),1600,10,(.69,.8,1)),('Rim',(5,8,9),2600,8,(1,.66,.79))]:
    bpy.ops.object.light_add(type='AREA',location=loc); o=bpy.context.object; o.name=name; o.data.energy=power; o.data.shape='DISK'; o.data.size=size; o.data.color=col; aim(o,(0,0,-1))
scene=bpy.context.scene; scene.render.engine='CYCLES'; scene.cycles.samples=48; scene.cycles.use_denoising=True
scene.world.color=(.21,.21,.21); scene.world.use_nodes=True; scene.world.node_tree.nodes['Background'].inputs[0].default_value=(.12,.16,.25,1); scene.world.node_tree.nodes['Background'].inputs[1].default_value=.55
scene.render.resolution_x=1600; scene.render.resolution_y=1400; scene.render.resolution_percentage=100
scene.render.image_settings.file_format='PNG'; scene.render.filepath=os.path.join(OUT,'CottonCandy_Island01_Preview.png')
scene.view_settings.view_transform='AgX'
bpy.ops.object.select_all(action='DESELECT'); island.select_set(True); bpy.context.view_layer.objects.active=island
for screen in bpy.data.screens:
    for area in screen.areas:
        if area.type=='VIEW_3D':
            area.spaces.active.region_3d.view_distance=23; area.spaces.active.region_3d.view_location=(0,0,-1)
bpy.ops.wm.save_as_mainfile(filepath=os.path.join(OUT,'CottonCandy_Island01.blend'))
stats={'vertices':len(island.data.vertices),'triangles':sum(len(p.vertices)-2 for p in island.data.polygons),'dimensions_m':list(island.dimensions),'closed_surface':all(len(e.link_faces)==2 for e in [])}
import bmesh
bm=bmesh.new(); bm.from_mesh(island.data); stats['closed_surface']=all(e.is_manifold for e in bm.edges); bm.free()
with open(os.path.join(OUT,'model_info.json'),'w') as f:json.dump(stats,f,indent=2)
bpy.ops.render.render(write_still=True)
print('ISLAND_COMPLETE',json.dumps(stats))
