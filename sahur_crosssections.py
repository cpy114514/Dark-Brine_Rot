import bpy,json
bpy.ops.wm.open_mainfile(filepath='D:/indie_game_dev/3D Models/tongtongtongsahur_walk.blend')
obj=bpy.data.objects['Sahur_Character'];out={}
for low,high in [(.49,.63),(.51,.53),(.43,.45),(.66,.675)]:
    ids={v.index for v in obj.data.vertices if low<v.co.z<high};p={i:i for i in ids}
    def root(i):
        while p[i]!=i:p[i]=p[p[i]];i=p[i]
        return i
    for e in obj.data.edges:
        a,b=e.vertices
        if a in ids and b in ids:
            ra,rb=root(a),root(b)
            if ra!=rb:p[rb]=ra
    comps={}
    for i in ids:comps.setdefault(root(i),[]).append(i)
    r=[]
    for ids in sorted(comps.values(),key=len,reverse=True):
        if len(ids)<10:continue
        vs=[obj.data.vertices[i].co for i in ids]
        r.append({'n':len(ids),'lo':[min(v[j] for v in vs) for j in range(3)],'hi':[max(v[j] for v in vs) for j in range(3)],'mean':[sum(v[j] for v in vs)/len(vs) for j in range(3)]})
    out[str((low,high))]=r
with open('D:/indie_game_dev/unity/Dark Brine_Rot/sahur_crosssections.json','w') as f:json.dump(out,f)
