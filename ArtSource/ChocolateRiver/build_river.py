import bpy, math, os, json
from mathutils import Vector
OUT=os.path.dirname(os.path.abspath(__file__))
bpy.ops.object.select_all(action='SELECT'); bpy.ops.object.delete(use_global=False)
def mat(name,color,rough):
 m=bpy.data.materials.new(name); m.diffuse_color=(*color,1); m.use_nodes=True
 p=m.node_tree.nodes.get('Principled BSDF'); p.inputs['Base Color'].default_value=(*color,1); p.inputs['Roughness'].default_value=rough; p.inputs['IOR'].default_value=1.46; p.inputs['Coat Weight'].default_value=.30; p.inputs['Coat Roughness'].default_value=.18
 return m
choc=mat('Pure milk chocolate - no textures',(.18,.055,.022),.25)
dark=mat('Chocolate river cut edges',(.095,.022,.009),.31)
cream=mat('Caramel chocolate ribbons',(.37,.15,.052),.28)
def center(t):return Vector((2.5*math.sin(t*math.tau-.7)+.8*math.sin(t*math.pi*3), (t-.5)*24,0))
def frame(t):
 d=(center(t+.0001)-center(t-.0001)).normalized();return Vector((d.y,-d.x,0))
def width(t):return 2.3+.4*math.sin(t*math.pi*3+.4)+.2*math.cos(t*17)
def height(t,u):return .38+.095*math.sin(t*43+u*4)+.035*math.sin(t*91-u*9)+.10*(1-u*u)+.10*math.exp(-((abs(u)-.84)/.11)**2)
N=420; W=80; vs=[]; fs=[]
for i in range(N+1):
 t=i/N; c=center(t); side=frame(t)
 for j in range(W+1):
  u=j/W*2-1; v=c+side*width(t)*u; v.z=height(t,u); vs.append(tuple(v))
for i in range(N):
 for j in range(W):
  a=i*(W+1)+j;fs.append((a,a+1,a+W+2,a+W+1))
# Closed solid underside, with rounded liquid edges.
boundary=list(range(W,-1,-1))+[i*(W+1) for i in range(1,N+1)]+[N*(W+1)+j for j in range(1,W+1)]+[i*(W+1)+W for i in range(N-1,0,-1)]
bottom=[]
for idx in boundary:
 x,y,z=vs[idx];bottom.append(len(vs));vs.append((x,y,-.30))
for k in range(len(boundary)):
 q=(k+1)%len(boundary);fs.append((boundary[k],bottom[k],bottom[q],boundary[q]))
fs.append(tuple(reversed(bottom)))
mesh=bpy.data.meshes.new('ChocolateRiver_SculptedSurface');mesh.from_pydata(vs,[],fs);mesh.update();river=bpy.data.objects.new('ChocolateRiver_Main',mesh);bpy.context.collection.objects.link(river);river.data.materials.append(choc)
for p in mesh.polygons:p.use_smooth=True
bev=river.modifiers.new('Rounded viscous edges','BEVEL');bev.width=.10;bev.segments=3;bev.limit_method='ANGLE'
objects=[river]
def tube(name,pts,radius,material):
 curve=bpy.data.curves.new(name,'CURVE');curve.dimensions='3D';curve.resolution_u=16;curve.bevel_depth=radius;curve.bevel_resolution=4
 s=curve.splines.new('POLY');s.points.add(len(pts)-1)
 for p,co in zip(s.points,pts):p.co=(*co,1)
 o=bpy.data.objects.new(name,curve);bpy.context.collection.objects.link(o);o.data.materials.append(material);objects.append(o);return o
# Raised flow folds are actual geometry, including tapered ends.
for k,(start,end,u0) in enumerate([(.06,.36,-.52),(.18,.53,.34),(.47,.82,-.36),(.67,.95,.55),(.32,.64,.77)]):
 pts=[]
 for i in range(150):
  t=start+(end-start)*i/149;u=u0+.08*math.sin(t*34+k);v=center(t)+frame(t)*width(t)*u;v.z=height(t,u)-.035;pts.append(tuple(v))
 o=tube('Sculpted flow fold %02d'%k,pts,.065,choc)
 for i,p in enumerate(o.data.splines[0].points):p.radius=max(.02,math.sin(math.pi*i/149)**.5)
# A few restrained lighter strands of chocolate, never a texture.
for k in range(2):
 pts=[]
 for i in range(230):
  t=.08+.83*i/229;u=-.18+k*.48+.055*math.sin(t*28+k);v=center(t)+frame(t)*width(t)*u;v.z=height(t,u)+.009;pts.append(tuple(v))
 o=tube('Caramel filament %d'%k,pts,.018,cream)
 for i,p in enumerate(o.data.splines[0].points):p.radius=math.sin(math.pi*i/229)**.5
# Presentation plinth is excluded from model exports.
floor=mat('Studio warm porcelain',(.26,.21,.18),.65)
bpy.ops.mesh.primitive_plane_add(size=200,location=(0,0,-.38));bpy.context.object.name='Studio ground';bpy.context.object.data.materials.append(floor)
def aim(o,pt):o.rotation_euler=(Vector(pt)-o.location).to_track_quat('-Z','Y').to_euler()
bpy.ops.object.camera_add(location=(21,-29,31));cam=bpy.context.object;aim(cam,(0,0,0));cam.data.type='ORTHO';cam.data.ortho_scale=30;bpy.context.scene.camera=cam
for loc,energy,size,color in [((0,3,17),3600,12,(1,.84,.67)),((-10,-2,9),2700,9,(1,.94,.88)),((7,9,11),4200,10,(.78,.87,1))]:
 bpy.ops.object.light_add(type='AREA',location=loc);o=bpy.context.object;o.data.energy=energy;o.data.shape='RECTANGLE';o.data.size=size;o.data.size_y=size*.45;o.data.color=color;aim(o,(0,0,0))
scene=bpy.context.scene;scene.render.engine='CYCLES';scene.cycles.samples=64;scene.cycles.use_denoising=True;scene.world.color=(.25,.25,.25);scene.render.resolution_x=1600;scene.render.resolution_y=1600;scene.render.resolution_percentage=100;scene.view_settings.view_transform='AgX'
bpy.ops.object.select_all(action='DESELECT')
for o in objects:o.select_set(True)
bpy.context.view_layer.objects.active=river
bpy.ops.export_scene.gltf(filepath=os.path.join(OUT,'ChocolateRiver.glb'),use_selection=True,export_format='GLB')
bpy.ops.wm.obj_export(filepath=os.path.join(OUT,'ChocolateRiver.obj'),export_selected_objects=True,export_curves_as_nurbs=False)
bpy.ops.wm.save_as_mainfile(filepath=os.path.join(OUT,'ChocolateRiver.blend'))
scene.render.filepath=os.path.join(OUT,'ChocolateRiver_Render.png');bpy.ops.render.render(write_still=True)
assert not any(n.type=='TEX_IMAGE' for m in bpy.data.materials if m.use_nodes for n in m.node_tree.nodes)
print('CHOCOLATE_RIVER_COMPLETE_NO_TEXTURES')
