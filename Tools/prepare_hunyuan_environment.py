"""Offline Hunyuan environment candidates. Originals remain in Downloads."""
import bpy,sys,json,bmesh
from pathlib import Path
kind,source=sys.argv[-2:];root=Path.cwd();out=root/'ArtSource/Environment/Hunyuan-20261027';out.mkdir(parents=True,exist_ok=True)
bpy.ops.wm.read_factory_settings(use_empty=True);bpy.ops.import_scene.gltf(filepath=source)
obj=next(o for o in bpy.context.scene.objects if o.type=='MESH');bpy.context.view_layer.objects.active=obj;obj.select_set(True)
bpy.ops.object.transform_apply(location=False,rotation=True,scale=True)
before=len(obj.data.polygons);mod=obj.modifiers.new('Game budget decimation','DECIMATE');mod.ratio=(8000 if kind=='rock' else 12000)/before
bpy.ops.object.modifier_apply(modifier=mod.name)
# Remove the generated circular ground pad; retain the visible object above it.
bm=bmesh.new();bm.from_mesh(obj.data);bm.verts.ensure_lookup_table();lo=min(v.co.z for v in bm.verts);hi=max(v.co.z for v in bm.verts);threshold=lo+(hi-lo)*(.045 if kind=='rock' else .055)
remove=[f for f in bm.faces if all(v.co.z<threshold for v in f.verts)];bmesh.ops.delete(bm,geom=remove,context='FACES');bm.to_mesh(obj.data);bm.free();obj.data.update()
for image in bpy.data.images:
 if image.size[0]>2048:image.scale(2048,2048)
 if image.source=='FILE':image.pack()
obj.name='Hunyuan_DarkFairy_'+kind.title();bpy.ops.wm.save_as_mainfile(filepath=str(out/(kind+'-working.blend')))
bpy.ops.export_scene.gltf(filepath=str(out/(kind+'-optimized.glb')),export_format='GLB',use_selection=True)
bpy.ops.export_scene.fbx(filepath=str(out/(kind+'-optimized.fbx')),use_selection=True,object_types={'MESH'},path_mode='COPY',embed_textures=True,add_leaf_bones=False)
report={'source':source,'before_triangles':before,'after_polygons':len(obj.data.polygons),'removed_bottom_faces':len(remove),'bottom_threshold':threshold,'texture_resolution':2048,'status':'candidate only: requires visual, material, collision, gameplay and performance review'}
(out/(kind+'-inspection.json')).write_text(json.dumps(report,indent=2));print('REPORT',json.dumps(report))
