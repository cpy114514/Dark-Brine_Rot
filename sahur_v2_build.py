import bpy,bmesh,math,os,json,heapq
from mathutils import Vector,Quaternion
BASE='D:/indie_game_dev/unity/Dark Brine_Rot'
bpy.ops.wm.open_mainfile(filepath='D:/indie_game_dev/3D Models/tongtongtongsahur_walk.blend')
scene=bpy.context.scene;rig=bpy.data.objects['Sahur_Walk_Rig'];src=bpy.data.objects['Sahur_Character']
rig.data.pose_position='REST';scene.frame_set(1)
def smooth(a,b,t):
    t=max(0,min(1,(t-a)/(b-a)));return t*t*(3-2*t)
def segdist(p,a,b):
    d=b-a;t=max(0,min(1,(p-a).dot(d)/d.length_squared));return (p-a-t*d).length
def limbscore(p,s):
    # References measured on the original sculpt in world/rest coordinates.
    x0=-.112 if s=='L' else .138
    points=[Vector((x0,.075,.682)),Vector((x0*1.055,.108,.555)),Vector((x0,.035,.453)),Vector((x0,.006,.422)),Vector((x0*.86,.012,.383))]
    ds=[segdist(p,points[i],points[i+1])/r for i,r in enumerate([.027,.024,.031,.027])]
    thumb_a=Vector((x0,.006,.443));thumb_b=Vector((x0*.69,-.013,.424))
    ds.append(segdist(p,thumb_a,thumb_b)/.020)
    return min(ds)
club_a=Vector((-.143,-.371,.095));club_b=Vector((-.100,.08,.461))
verts=src.data.vertices;n=len(verts)
parent=list(range(n));bins={v.index:int((v.co.z-.47)/.012) for v in verts if .47<v.co.z<.65}
def root(i):
    while parent[i]!=i:parent[i]=parent[parent[i]];i=parent[i]
    return i
adj=[[] for _ in verts]
for e in src.data.edges:
    a,b=e.vertices;length=(verts[a].co-verts[b].co).length
    adj[a].append((b,length));adj[b].append((a,length))
    if a in bins and b in bins and bins[a]==bins[b]:
        ra,rb=root(a),root(b)
        if ra!=rb:parent[rb]=ra
comps={}
for i in bins:comps.setdefault(root(i),[]).append(i)
labels=[None]*n;distance=[1e9]*n;queue=[]
def seed(i,lab):
    if distance[i]==0:return
    labels[i]=lab;distance[i]=0;heapq.heappush(queue,(0,i))
for ids in comps.values():
    xs=[verts[i].co.x for i in ids];mean=sum(xs)/len(xs)
    if len(ids)>15 and max(xs)-min(xs)<.075 and (mean<-.069 or mean>.09):
        for i in ids:seed(i,'L' if mean<0 else 'R')
    else:
        for i in ids:seed(i,'body')
for v in verts:
    x,y,z=v.co
    club=z<.37 and segdist(v.co,club_a,club_b)<.05 and y<-.055
    if club:seed(v.index,'L')
    elif .375<z<.47 and ((x>.096 and y<.08) or (x<-.078 and y<.085)):seed(v.index,'L' if x<0 else 'R')
    elif z>.715 or (z<.34 and y>-.02) or (.37<z<.735 and -.065<x<.085):seed(v.index,'body')
while queue:
    dist,i=heapq.heappop(queue)
    if dist>distance[i]+1e-9:continue
    for j,length in adj[i]:
        nd=dist+length
        if nd<distance[j]:distance[j]=nd;labels[j]=labels[i];heapq.heappush(queue,(nd,j))
face_labels=[]
for poly in src.data.polygons:
    counts={k:sum(labels[i]==k for i in poly.vertices) for k in ['body','L','R']}
    face_labels.append(max(counts,key=counts.get))

report={}
def extract(label,name):
    ob=src.copy();ob.data=src.data.copy();ob.name=name;scene.collection.objects.link(ob)
    bm=bmesh.new();bm.from_mesh(ob.data);bm.faces.ensure_lookup_table()
    unwanted=[f for f in bm.faces if face_labels[f.index]!=label]
    bmesh.ops.delete(bm,geom=unwanted,context='FACES')
    loose=[v for v in bm.verts if not v.link_faces]
    if loose:bmesh.ops.delete(bm,geom=loose,context='VERTS')
    boundary=[e for e in bm.edges if e.is_boundary]
    uv=bm.loops.layers.uv.active
    # Close every cut surface and give the patch a locally sampled UV.
    if boundary:
        result=bmesh.ops.holes_fill(bm,edges=boundary,sides=0)
        newfaces=set(result.get('faces',[]))
        for f in newfaces:
            f.smooth=True
            for loop in f.loops:
                if uv:
                    existing=[l[uv].uv.copy() for l in loop.vert.link_loops if l.face not in newfaces]
                    if existing:loop[uv].uv=sum(existing,Vector((0,0)))/len(existing)
    bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces))
    bm.to_mesh(ob.data);bm.free();ob.data.update()
    report[label]={'verts':len(ob.data.vertices),'polys':len(ob.data.polygons),'cut_edges':len(boundary)}
    return ob
body=extract('body','Sahur_Body_Legs')
limbs={s:extract(s,'Sahur_Arm_'+s+('_With_Club' if s=='L' else '')) for s in ['L','R']}
src.hide_render=True;src.hide_set(True);src.name='Original_Sculpt_Backup'
backup=bpy.data.collections.new('Original sculpt backup (hidden)');scene.collection.children.link(backup)
for c in list(src.users_collection):c.objects.unlink(src)
backup.objects.link(src);backup.hide_viewport=True;backup.hide_render=True
for s,ob in limbs.items():
    sign=-1 if s=='L' else 1
    for v in ob.data.vertices:
        # Shoulder remains seated; the hand gains about 5 cm of clearance.
        amt=.012+.046*(1-smooth(.44,.69,v.co.z))
        v.co.x+=sign*amt
    ob.vertex_groups.clear()
    for n in ['Arm.'+s,'Forearm.'+s,'Hand.'+s]:ob.vertex_groups.new(name=n)
    for v in ob.data.vertices:
        z=v.co.z
        hand=1-smooth(.44,.48,z)
        upper=smooth(.53,.58,z)
        weights={'Hand.'+s:hand,'Forearm.'+s:(1-hand)*(1-upper),'Arm.'+s:(1-hand)*upper}
        for n,w in weights.items():
            if w>1e-6:ob.vertex_groups[n].add([v.index],w,'REPLACE')
# Body caps and the entire torso should stay with the torso, not the arm.
bodygroup=body.vertex_groups['Body']
for v in body.data.vertices:
    if v.co.z>.375:
        for g in list(v.groups):body.vertex_groups[g.group].remove([v.index])
        bodygroup.add([v.index],1,'REPLACE')

bpy.ops.object.select_all(action='DESELECT');rig.hide_set(False);rig.select_set(True);bpy.context.view_layer.objects.active=rig
bpy.ops.object.mode_set(mode='EDIT')
for s in ['L','R']:
    sign=-1 if s=='L' else 1;x0=-.112 if s=='L' else .138
    shoulder=Vector((x0+sign*.012,.075,.682))
    elbow=Vector((x0*1.055+sign*.04,.108,.555))
    wrist=Vector((x0+sign*.058,.025,.447))
    arm=rig.data.edit_bones['Arm.'+s];arm.head=shoulder;arm.tail=elbow
    fore=rig.data.edit_bones.new('Forearm.'+s);fore.head=elbow;fore.tail=wrist;fore.parent=arm;fore.use_connect=True
    hand=rig.data.edit_bones['Hand.'+s];hand.head=wrist;hand.tail=wrist+Vector((0,-.025,-.055));hand.parent=fore;hand.use_connect=True
bpy.ops.object.mode_set(mode='OBJECT')
rig.animation_data_clear()
rig.data.pose_position='POSE'
def loc(pb,world):pb.location=pb.bone.matrix_local.to_3x3().inverted()@Vector(world)
def rotation(pb,angles):
    pb.rotation_mode='QUATERNION';basis=pb.bone.matrix_local.to_quaternion()
    q=Quaternion((0,0,1),angles[2])@Quaternion((0,1,0),angles[1])@Quaternion((1,0,0),angles[0])
    pb.rotation_quaternion=basis.inverted()@q@basis
N=32
for f in range(1,N+2):
    t=2*math.pi*(f-1)/N
    pb=rig.pose.bones['Body']
    loc(pb,(.013*math.sin(t),.005*math.sin(2*t),-.027+.010*math.cos(2*t+.3)))
    rotation(pb,(.025+.014*math.sin(2*t),.04*math.sin(t),.055*math.cos(t)))
    pb.keyframe_insert('location',frame=f);pb.keyframe_insert('rotation_quaternion',frame=f)
    for s,off in [('L',0),('R',math.pi)]:
        p=(t+off)%(2*math.pi);u=p/(2*math.pi)
        if u<.58:
            k=u/.58;fore=-.078+.156*k;lift=0;pitch=0
        else:
            k=(u-.58)/.42;fore=.078-.156*smooth(0,1,k)
            lift=.055*math.sin(math.pi*k)**1.25
            pitch=.20*math.sin(2*math.pi*k)
        pb=rig.pose.bones['Foot_IK.'+s]
        loc(pb,((-.003 if s=='L' else .003)*math.sin(math.pi*u),fore,lift))
        rotation(pb,(pitch,0,0));pb.keyframe_insert('location',frame=f);pb.keyframe_insert('rotation_quaternion',frame=f)
        sign=-1 if s=='L' else 1
        pb=rig.pose.bones['Arm.'+s]
        rotation(pb,((.25 if s=='L' else .40)*math.cos(p-.15),sign*.085,.025*math.sin(p)))
        pb.keyframe_insert('rotation_quaternion',frame=f)
        pb=rig.pose.bones['Forearm.'+s]
        rotation(pb,(-.06-.13*(.5+.5*math.sin(p-.45)),0,sign*.025*math.sin(p-.2)))
        pb.keyframe_insert('rotation_quaternion',frame=f)
        pb=rig.pose.bones['Hand.'+s]
        rotation(pb,(.065*math.sin(p-.65),0,.035*math.sin(p-.4)))
        pb.keyframe_insert('rotation_quaternion',frame=f)
action=rig.animation_data.action;action.name='Sahur_Lively_Walk_Separated_Arms';action.use_fake_user=True
for layer in action.layers:
    for strip in layer.strips:
        for bag in strip.channelbags:
            for fc in bag.fcurves:
                for k in fc.keyframe_points:k.interpolation='LINEAR'
scene.frame_start=1;scene.frame_end=32;scene.frame_set(1)
scene.render.engine='BLENDER_WORKBENCH';scene.render.threads_mode='FIXED';scene.render.threads=2
scene.render.resolution_x=800;scene.render.resolution_y=900;scene.render.resolution_percentage=100
scene.display.shading.light='STUDIO';scene.display.shading.color_type='TEXTURE'
scene.display.shading.show_shadows=True;scene.display.shading.show_cavity=True
camdata=bpy.data.cameras.new('Walk_Preview_Camera');cam=bpy.data.objects.new('Walk_Preview_Camera',camdata);scene.collection.objects.link(cam);scene.camera=cam
camdata.type='ORTHO';camdata.ortho_scale=1.38
target=Vector((0,-.02,.53));cam.location=(1.45,-3,1.42);cam.rotation_euler=(target-cam.location).to_track_quat('-Z','Y').to_euler()
for frame in [1,9,17,25]:
    scene.frame_set(frame);scene.render.filepath=BASE+'/sahur_v2_pose_'+str(frame)+'.png';bpy.ops.render.render(write_still=True)
scene.frame_set(1)
rig.data.pose_position='REST'
camdata.ortho_scale=.72;target=Vector((.005,.03,.55));cam.location=(0,-3,.55);cam.rotation_euler=(target-cam.location).to_track_quat('-Z','Y').to_euler()
scene.render.filepath=BASE+'/sahur_v2_arms_rest.png';bpy.ops.render.render(write_still=True)
rig.data.pose_position='POSE';camdata.ortho_scale=1.38;target=Vector((0,-.02,.53));cam.location=(1.45,-3,1.42);cam.rotation_euler=(target-cam.location).to_track_quat('-Z','Y').to_euler()
with open(BASE+'/sahur_v2_report.json','w') as f:json.dump(report,f)
bpy.ops.wm.save_as_mainfile(filepath=BASE+'/sahur_v2_working.blend')
