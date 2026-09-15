import bpy,bmesh,math,json,numpy as np
from mathutils import Vector,Quaternion
from mathutils.bvhtree import BVHTree
BASE='D:/indie_game_dev/unity/Dark Brine_Rot'
bpy.ops.wm.open_mainfile(filepath='D:/indie_game_dev/3D Models/tongtongtongsahur_walk.blend')
scene=bpy.context.scene;rig=bpy.data.objects['Sahur_Walk_Rig'];src=bpy.data.objects['Sahur_Character'];rig.data.pose_position='REST'
def smooth(a,b,t):
    t=max(0,min(1,(t-a)/(b-a)));return t*t*(3-2*t)
# Preserve the detailed face and the original legs. Replace the fused trunk/arms.
body=src.copy();body.data=src.data.copy();body.name='Sahur_Original_Face_Legs';scene.collection.objects.link(body)
names={g.index:g.name for g in src.vertex_groups}
legverts=set()
for v in src.data.vertices:
    if v.co.z<.388 and sum(g.weight for g in v.groups if names[g.group].startswith(('Thigh.','Shin.','Foot.')))>.4:legverts.add(v.index)
bm=bmesh.new();bm.from_mesh(body.data);bm.faces.ensure_lookup_table()
keep=[all(v.co.z>.744 for v in f.verts) or all(v.index in legverts for v in f.verts) for f in bm.faces]
bmesh.ops.delete(bm,geom=[f for f in bm.faces if not keep[f.index]],context='FACES')
bmesh.ops.delete(bm,geom=[v for v in bm.verts if not v.link_faces],context='VERTS')
uv=bm.loops.layers.uv.active
newfaces=set(bmesh.ops.holes_fill(bm,edges=[e for e in bm.edges if e.is_boundary],sides=0).get('faces',[]))
for f in newfaces:
    for l in f.loops:
        old=[a[uv].uv.copy() for a in l.vert.link_loops if a.face not in newfaces]
        if old:l[uv].uv=sum(old,Vector((0,0)))/len(old)
bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bm.to_mesh(body.data);bm.free()
src.hide_set(True);src.hide_render=True;src.name='Original_Sculpt_Backup'
backup=bpy.data.collections.new('Original sculpt backup (hidden)');scene.collection.children.link(backup)
for c in list(src.users_collection):c.objects.unlink(src)
backup.objects.link(src);backup.hide_viewport=True;backup.hide_render=True

wood=bpy.data.materials.new('Warm_Wood_Articulated_Limbs');wood.diffuse_color=(.43,.20,.086,1);wood.use_nodes=True
bs=wood.node_tree.nodes.get('Principled BSDF');bs.inputs['Base Color'].default_value=wood.diffuse_color;bs.inputs['Roughness'].default_value=.43
def meshob(name,vs,fs,material=wood):
    mesh=bpy.data.meshes.new(name);mesh.from_pydata(vs,[],fs);mesh.update()
    ob=bpy.data.objects.new(name,mesh);scene.collection.objects.link(ob);ob.data.materials.append(material)
    for p in mesh.polygons:p.use_smooth=True
    ob.parent=rig;mod=ob.modifiers.new('Sahur Skin','ARMATURE');mod.object=rig
    return ob
def bind(ob,weights):
    for i,w in enumerate(weights):
        for name,weight in w.items():
            g=ob.vertex_groups.get(name) or ob.vertex_groups.new(name=name)
            if weight>1e-6:g.add([i],weight,'REPLACE')
def tube(vs,fs,centers,radii,sides=16):
    offset=len(vs)
    for i,p in enumerate(centers):
        tangent=(centers[min(i+1,len(centers)-1)]-centers[max(0,i-1)]).normalized()
        ref=Vector((1,0,0)) if abs(tangent.x)<.9 else Vector((0,1,0))
        u=tangent.cross(ref).normalized();v=tangent.cross(u).normalized()
        for j in range(sides):
            ang=2*math.pi*j/sides;vs.append(tuple(p+radii[i]*(u*math.cos(ang)+v*math.sin(ang))))
    for i in range(len(centers)-1):
        for j in range(sides):
            a=offset+i*sides+j;b=offset+i*sides+(j+1)%sides;fs.append((a,b,b+sides,a+sides))
    fs.append(tuple(offset+j for j in reversed(range(sides))))
    top=offset+(len(centers)-1)*sides;fs.append(tuple(top+j for j in range(sides)))
def ellipsoid(vs,fs,center,scale,rings=12,sides=24):
    offset=len(vs)
    for i in range(rings+1):
        phi=math.pi*(.001+(1-.002)*i/rings)
        for j in range(sides):
            t=2*math.pi*j/sides
            vs.append(tuple(center+Vector((scale[0]*math.sin(phi)*math.cos(t),scale[1]*math.sin(phi)*math.sin(t),scale[2]*math.cos(phi)))))
    for i in range(rings):
        for j in range(sides):
            a=offset+i*sides+j;b=offset+i*sides+(j+1)%sides;fs.append((a,a+sides,b+sides,b))
    fs.append(tuple(offset+j for j in range(sides)));top=offset+rings*sides;fs.append(tuple(top+j for j in reversed(range(sides))))
# New cylindrical wood surface under the preserved face, with original UV colors.
vs=[];fs=[];rings=38;sides=96
for i in range(rings+1):
    t=i/rings;z=.377+.383*t
    rx=.094+.015*t;cy=.078-.025*t;ry=.088+.035*t
    for j in range(sides):
        a=2*math.pi*j/sides;vs.append((.013+rx*math.cos(a),cy+ry*math.sin(a),z))
for i in range(rings):
    for j in range(sides):
        a=i*sides+j;b=i*sides+(j+1)%sides;fs.append((a,b,b+sides,a+sides))
fs.append(tuple(reversed(range(sides))));fs.append(tuple(rings*sides+j for j in range(sides)))
torso=meshob('Sahur_Rebuilt_Trunk',vs,fs,src.data.materials[0]);bind(torso,[{'Body':1} for v in vs])
src.data.calc_loop_triangles();tris=list(src.data.loop_triangles)
bvh=BVHTree.FromPolygons([v.co for v in src.data.vertices],[t.vertices for t in tris],all_triangles=True)
srcuv=src.data.uv_layers.active.data;newuv=torso.data.uv_layers.new(name='UVMap').data
for poly in torso.data.polygons:
    for li in poly.loop_indices:
        p=torso.data.vertices[torso.data.loops[li].vertex_index].co
        near,normal,ti,dist=bvh.find_nearest(p);tr=tris[ti];a,b,c=[src.data.vertices[k].co for k in tr.vertices]
        v0=b-a;v1=c-a;v2=near-a;d00=v0.dot(v0);d01=v0.dot(v1);d11=v1.dot(v1);den=d00*d11-d01*d01
        if abs(den)<1e-16:w1=w2=0
        else:w1=(d11*v2.dot(v0)-d01*v2.dot(v1))/den;w2=(d00*v2.dot(v1)-d01*v2.dot(v0))/den
        newuv[li].uv=srcuv[tr.loops[0]].uv*(1-w1-w2)+srcuv[tr.loops[1]].uv*w1+srcuv[tr.loops[2]].uv*w2
# Resample original wood into one continuous cylindrical UV island.
tex=bpy.data.images.get('texture_pbr_20250901.png');iw,ih=tex.size
pixels=np.empty(iw*ih*4,dtype=np.float32);tex.pixels.foreach_get(pixels);pixels=pixels.reshape(ih,iw,4)
W,H=512,768;paint=np.ones((H,W,4),dtype=np.float32)
for iy in range(H):
    t=(iy+.5)/H;z=.377+.383*t;rx=.094+.015*t;cy=.078-.025*t;ry=.088+.035*t
    for ix in range(W):
        angle=2*math.pi*(ix+.5)/W;p=Vector((.013+rx*math.cos(angle),cy+ry*math.sin(angle),z))
        near,normal,ti,dist=bvh.find_nearest(p);tr=tris[ti];a,b,c=[src.data.vertices[k].co for k in tr.vertices]
        v0=b-a;v1=c-a;v2=near-a;d00=v0.dot(v0);d01=v0.dot(v1);d11=v1.dot(v1);den=d00*d11-d01*d01
        if abs(den)<1e-16:w1=w2=0
        else:w1=(d11*v2.dot(v0)-d01*v2.dot(v1))/den;w2=(d00*v2.dot(v1)-d01*v2.dot(v0))/den
        uvp=srcuv[tr.loops[0]].uv*(1-w1-w2)+srcuv[tr.loops[1]].uv*w1+srcuv[tr.loops[2]].uv*w2
        px=int(max(0,min(iw-1,uvp.x*iw)));py=int(max(0,min(ih-1,uvp.y*ih)));paint[iy,ix]=pixels[py,px]
image=bpy.data.images.new('Rebuilt_Trunk_Wood',width=W,height=H);image.pixels.foreach_set(paint.ravel());image.pack()
trunkmat=bpy.data.materials.new('Rebuilt_Trunk_Wood');trunkmat.use_nodes=True
nd=trunkmat.node_tree.nodes.new('ShaderNodeTexImage');nd.image=image
trunkmat.node_tree.links.new(nd.outputs['Color'],trunkmat.node_tree.nodes.get('Principled BSDF').inputs['Base Color'])
torso.data.materials.clear();torso.data.materials.append(trunkmat)
for poly in torso.data.polygons:
    for li in poly.loop_indices:
        vi=torso.data.loops[li].vertex_index;row,col=divmod(vi,sides)
        # Unwrap the seam independently on the final quad in each ring.
        u=col/sides
        if poly.index<rings*sides and poly.index%sides==sides-1 and col==0:u=1
        newuv[li].uv=(u,row/rings)
limbs={}
for s in ['L','R']:
    sign=-1 if s=='L' else 1;x0=-.112 if s=='L' else .138
    shoulder=Vector((x0,.075,.682));elbow=Vector((x0*1.055+sign*.04,.108,.555));wrist=Vector((x0+sign*.058,.025,.447))
    vs=[];fs=[];centers=[];radii=[]
    for i in range(25):
        t=i/24
        if t<.5:k=t*2;p=shoulder.lerp(elbow,k);r=.022-.003*k
        else:k=(t-.5)*2;p=elbow.lerp(wrist,k);r=.019-.006*k
        centers.append(p);radii.append(r)
    tube(vs,fs,centers,radii,24)
    ellipsoid(vs,fs,shoulder,(.023,.023,.030))
    arm=meshob('Sahur_Rebuilt_Arm_'+s,vs,fs)
    weights=[]
    for v in arm.data.vertices:
        upper=smooth(.526,.580,v.co.z);hand=1-smooth(.442,.465,v.co.z)
        weights.append({'Arm.'+s:(1-hand)*upper,'Forearm.'+s:(1-hand)*(1-upper),'Hand.'+s:hand})
    bind(arm,weights);limbs[s]=arm
    vs=[];fs=[];palm=wrist+Vector((0,-.004,-.025))
    ellipsoid(vs,fs,palm,(.024,.016,.030))
    # Four individually rounded, slightly curled fingers and an opposing thumb.
    for finger in range(4):
        dx=(finger-1.5)*.012;length=[.035,.044,.042,.032][finger]
        start=palm+Vector((dx,-.001,-.020));cent=[];rad=[]
        for i in range(10):
            t=i/9
            curl=.024 if s=='L' else .013
            cent.append(start+Vector((0,-curl*math.sin(math.pi*t*.75),-length*t)))
            rad.append(.0068*(1-.32*t))
        tube(vs,fs,cent,rad,12);ellipsoid(vs,fs,cent[-1],(rad[-1],)*3,6,12)
    a=palm+Vector((-sign*.019,-.003,.007));b=palm+Vector((-sign*.036,-.014,-.012));c=palm+Vector((-sign*.028,-.026,-.026))
    cent=[];rad=[]
    for i in range(12):
        t=i/11;cent.append((1-t)**2*a+2*t*(1-t)*b+t*t*c);rad.append(.009*(1-.35*t))
    tube(vs,fs,cent,rad,14);ellipsoid(vs,fs,c,(.006,)*3,6,12)
    hand=meshob('Sahur_Rebuilt_Hand_'+s,vs,fs);bind(hand,[{'Hand.'+s:1} for v in vs])
    if s=='L':
        grip=palm+Vector((0,-.021,-.026));axis=Vector((-.025,-.36,-.31)).normalized()
        vs=[];fs=[];cent=[];rad=[]
        for i in range(30):
            t=i/29;cent.append(grip+axis*(-.04+.46*t));rad.append(.012+.021*smooth(.18,.85,t))
        tube(vs,fs,cent,rad,32);ellipsoid(vs,fs,cent[-1],(.033,)*3,10,24)
        club=meshob('Sahur_Wooden_Club',vs,fs);bind(club,[{'Hand.L':1} for v in vs])
report={'revision':'Rebuilt closed trunk, articulated arms and five-finger hands; preserved original face and legs.'}
# Reuse the reviewed IK walk authoring and preview setup, with the new mesh parts.
tail=open(BASE+'/sahur_v2_build.py').read().split("bpy.ops.object.select_all(action='DESELECT');rig.hide_set(False)",1)[1]
tail=tail.replace('x0+sign*.012,.075,.682','x0,.075,.682')
exec("bpy.ops.object.select_all(action='DESELECT');rig.hide_set(False)"+tail)
