import bpy, json
from pathlib import Path
root=Path('D:/indie_game_dev/unity/Dark Brine_Rot/BlenderWork')
bpy.ops.wm.save_as_mainfile(filepath=str(root/'character_before.blend'),copy=True)
out=[]
for o in bpy.context.scene.objects:
 d={'name':o.name,'type':o.type,'matrix':[list(r) for r in o.matrix_world],'parent':o.parent.name if o.parent else None}
 if o.type=='ARMATURE':
  d['bones']=[{'name':b.name,'head':list(o.matrix_world@b.head_local),'tail':list(o.matrix_world@b.tail_local),'parent':b.parent.name if b.parent else None} for b in o.data.bones]
 if o.type=='MESH':
  d['modifiers']=[(m.name,m.type) for m in o.modifiers]
  d['groups']=[g.name for g in o.vertex_groups]
  n=len(o.data.vertices); parents=list(range(n))
  def find(i):
   while parents[i]!=i:
    parents[i]=parents[parents[i]]; i=parents[i]
   return i
  for e in o.data.edges:
   a,b=map(find,e.vertices); parents[a]=b
  comps={}
  for v in o.data.vertices: comps.setdefault(find(v.index),[]).append(v.index)
  d['components']=[]
  for inds in comps.values():
   vs=[o.matrix_world@o.data.vertices[i].co for i in inds]
   d['components'].append({'count':len(inds),'first':inds[0],'min':[min(v[k] for v in vs) for k in range(3)],'max':[max(v[k] for v in vs) for k in range(3)]})
 out.append(d)
(root/'scene.json').write_text(json.dumps(out,indent=2))
print('SCENE_INSPECTION_DONE')
