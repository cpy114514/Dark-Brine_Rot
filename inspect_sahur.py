import bpy, json
from mathutils import Vector
obj = bpy.data.objects['Mesh_0']
vs = [obj.matrix_world @ v.co for v in obj.data.vertices]
lo=[min(v[i] for v in vs) for i in range(3)]
hi=[max(v[i] for v in vs) for i in range(3)]
slices=[]
for j in range(20):
    z=lo[2]+(hi[2]-lo[2])*j/20
    pts=[v for v in vs if z <= v.z < z+(hi[2]-lo[2])/20]
    slices.append({'z':z,'n':len(pts),'bounds':[[min(v[i] for v in pts),max(v[i] for v in pts)] for i in range(2)] if pts else []})
with open('D:/indie_game_dev/unity/Dark Brine_Rot/sahur_inspect.json','w') as f:
    json.dump({'bounds':[lo,hi],'vertices':len(vs),'slices':slices},f)
