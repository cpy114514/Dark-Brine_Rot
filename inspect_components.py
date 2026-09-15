import bpy,json
obj=bpy.data.objects['Sahur_Character']
n=len(obj.data.vertices)
parent=list(range(n))
def find(i):
    while parent[i]!=i:
        parent[i]=parent[parent[i]]; i=parent[i]
    return i
for e in obj.data.edges:
    a,b=map(find,e.vertices)
    if a!=b:parent[b]=a
comps={}
for i in range(n):comps.setdefault(find(i),[]).append(i)
report=[]
for indices in sorted(comps.values(),key=len,reverse=True):
    if len(indices)<20:continue
    vs=[obj.data.vertices[i].co for i in indices]
    report.append({'n':len(indices),'first':indices[0],'lo':[min(v[j] for v in vs) for j in range(3)],'hi':[max(v[j] for v in vs) for j in range(3)]})
with open('D:/indie_game_dev/unity/Dark Brine_Rot/sahur_components.json','w') as f:json.dump(report,f)
print('COMPONENTS',len(comps))
