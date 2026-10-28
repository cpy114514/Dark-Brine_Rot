"""Remove generated ground-pad remnants while retaining the optimized source candidates."""
import bpy,bmesh,sys,json
from pathlib import Path
kind=sys.argv[-1];root=Path.cwd()/'ArtSource/Environment';source=root/'Hunyuan-20261027';out=root/'Hunyuan-20261027-r2';out.mkdir(parents=True,exist_ok=True)
bpy.ops.wm.open_mainfile(filepath=str(source/(kind+'-working.blend')));obj=next(o for o in bpy.context.scene.objects if o.type=='MESH');bpy.ops.object.select_all(action='DESELECT');obj.select_set(True);bpy.context.view_layer.objects.active=obj
bm=bmesh.new();bm.from_mesh(obj.data);bm.verts.ensure_lookup_table();lo=min(v.co.z for v in bm.verts);hi=max(v.co.z for v in bm.verts);removed=0
if kind=='rock':
 cut=lo+(hi-lo)*.10;faces=[f for f in bm.faces if all(v.co.z<cut for v in f.verts)];removed=len(faces);bmesh.ops.delete(bm,geom=faces,context='FACES')
else:
 unseen=set(bm.verts);discard=[]
 while unseen:
  stack=[unseen.pop()];component=[]
  while stack:
   v=stack.pop();component.append(v)
   for edge in v.link_edges:
    other=edge.other_vert(v)
    if other in unseen:unseen.remove(other);stack.append(other)
  if max(v.co.z for v in component)<lo+(hi-lo)*.17:discard.extend(component)
 removed=len(discard);bmesh.ops.delete(bm,geom=discard,context='VERTS')
bm.to_mesh(obj.data);bm.free();obj.data.update();bpy.context.preferences.filepaths.save_version=0;bpy.ops.wm.save_as_mainfile(filepath=str(out/(kind+'-working.blend')))
bpy.ops.export_scene.gltf(filepath=str(out/(kind+'-optimized.glb')),export_format='GLB',use_selection=True)
bpy.ops.export_scene.fbx(filepath=str(out/(kind+'-optimized.fbx')),use_selection=True,object_types={'MESH'},path_mode='COPY',embed_textures=True,add_leaf_bones=False)
(out/(kind+'-inspection.json')).write_text(json.dumps({'polygons':len(obj.data.polygons),'removed_bottom_elements':removed,'status':'candidate requiring Unity visual and performance validation'},indent=2));print(kind,len(obj.data.polygons),removed)
