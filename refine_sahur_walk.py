import bpy,math,json
from mathutils import Vector,Quaternion
obj=bpy.data.objects['Sahur_Character'];rig=bpy.data.objects['Sahur_Walk_Rig']
def smooth(a,b,t):
    t=max(0,min(1,(t-a)/(b-a)));return t*t*(3-2*t)
start=Vector((-.143,-.371,.095));end=Vector((-.101,-.025,.41));d=end-start
club_indices=[]
leg_parent=list(range(len(obj.data.vertices)))
leg_ids=set()
def root(i):
    while leg_parent[i]!=i:
        leg_parent[i]=leg_parent[leg_parent[i]];i=leg_parent[i]
    return i
for v in obj.data.vertices:
    t=max(0,min(1,(v.co-start).dot(d)/d.length_squared))
    if v.co.z<.355 and (v.co-(start+t*d)).length>=.065:leg_ids.add(v.index)
for e in obj.data.edges:
    a,b=e.vertices
    if a in leg_ids and b in leg_ids:
        ra,rb=root(a),root(b)
        if ra!=rb:leg_parent[rb]=ra
leg_comps={}
for i in leg_ids:leg_comps.setdefault(root(i),[]).append(i)
leg_side={}
for ids in leg_comps.values():
    side='L' if sum(obj.data.vertices[i].co.x for i in ids)/len(ids)<.01 else 'R'
    for i in ids:leg_side[i]=side
print('LEG COMPONENTS',[(len(ids),sum(obj.data.vertices[i].co.x for i in ids)/len(ids)) for ids in leg_comps.values() if len(ids)>50])
for v in obj.data.vertices:
    x,y,z=v.co
    t=max(0,min(1,(v.co-start).dot(d)/d.length_squared))
    club=(v.co-(start+t*d)).length<.065 and z<.445
    if club:club_indices.append(v.index)
    side=leg_side.get(v.index,'L' if x<.01 else 'R')
    if club or z>=.40:
        weights={'Body':1.0}
    else:
        upper=smooth(.20,.28,z);foot=1-smooth(.055,.105,z);torso=smooth(.355,.405,z)
        weights={'Body':torso,'Thigh.'+side:(1-torso)*(1-foot)*upper,'Shin.'+side:(1-torso)*(1-foot)*(1-upper),'Foot.'+side:(1-torso)*foot}
    for old in list(v.groups):obj.vertex_groups[old.group].remove([v.index])
    for n,w in weights.items():
        if w>1e-6:obj.vertex_groups[n].add([v.index],w,'REPLACE')
# Keep the upper body and held club coherent in the unified sculpt.
# The free arm gets a restrained, smoothly feathered shoulder swing.
for v in obj.data.vertices:
    x,y,z=v.co
    if .40<z<.72 and x>.117:
        aw=smooth(.117,.145,x)*(1-smooth(.64,.72,z))
        if aw:
            obj.vertex_groups['Body'].add([v.index],1-aw,'REPLACE')
            obj.vertex_groups['Arm.R'].add([v.index],aw,'REPLACE')
scene=bpy.context.scene
report=[]
rest=[v.co.copy() for v in obj.data.vertices]
footids={s:[v.index for v in obj.data.vertices if v.co.z<.018 and (v.co.x<.002)==(s=='L')] for s in ['L','R']}
for f in [1,9,17,25,33]:
    scene.frame_set(f)
    dg=bpy.context.evaluated_depsgraph_get();ev=obj.evaluated_get(dg);m=ev.to_mesh()
    bad=[]
    for e in obj.data.edges:
        a,b=e.vertices;l=(rest[a]-rest[b]).length
        if l>.001:
            diff=(m.vertices[a].co-m.vertices[b].co).length-l
            if diff>.012:bad.append((round(diff,5),a,b))
    report.append({'frame':f,'foot_min_z':{s:min(m.vertices[i].co.z for i in ids) for s,ids in footids.items()},'stretched_edges':len(bad),'worst':sorted(bad,reverse=True)[:4]})
    ev.to_mesh_clear()
with open('D:/indie_game_dev/unity/Dark Brine_Rot/walk_qa.json','w') as f:json.dump(report,f)
scene.frame_set(9)
def show_refined():
    for a in bpy.context.screen.areas:
        if a.type=='CONSOLE':a.type='VIEW_3D'
        if a.type=='VIEW_3D':
            a.spaces.active.region_3d.view_rotation=Quaternion((.82,.48,.16,.26)).normalized()
            a.spaces.active.overlay.show_overlays=False
    return None
bpy.app.timers.register(show_refined,first_interval=.5)
print('REFINED AND CHECKED')
