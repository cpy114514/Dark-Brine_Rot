import bpy,sys,json
from mathutils import Vector
p=sys.argv[-1];bpy.ops.wm.read_factory_settings(use_empty=True);bpy.ops.import_scene.gltf(filepath=p)
for o in bpy.data.objects:
 if o.type=='MESH':
  points=[o.matrix_world@Vector(v) for v in o.bound_box]
  print('MESH',o.name,'verts',len(o.data.vertices),'polys',len(o.data.polygons),'bounds',[(round(min(v[i] for v in points),3),round(max(v[i] for v in points),3)) for i in range(3)],'materials',[m.name for m in o.data.materials])
for im in bpy.data.images:print('IMAGE',im.name,im.size[:],bool(im.packed_file))
