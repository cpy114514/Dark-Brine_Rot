import bpy,bmesh,json
from pathlib import Path
src='C:/Users/pinyu/Downloads/7432c0320e21a8cab7fce92df0208e63.glb';out=Path.cwd()/'ArtSource/Environment/Hunyuan-20261027-clean';out.mkdir(exist_ok=True)
bpy.ops.wm.read_factory_settings(use_empty=True);bpy.ops.import_scene.gltf(filepath=src);obj=next(o for o in bpy.context.scene.objects if o.type=='MESH');bpy.context.view_layer.objects.active=obj;obj.select_set(True);bpy.ops.object.transform_apply(location=False,rotation=True,scale=True)
bm=bmesh.new();bm.from_mesh(obj.data);n=len(bm.verts);bmesh.ops.remove_doubles(bm,verts=list(bm.verts),dist=.000002);bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));merged=n-len(bm.verts);bm.to_mesh(obj.data);bm.free();obj.data.update();print('merged',merged,flush=True)
mod=obj.modifiers.new('Game-ready mesh','DECIMATE');mod.ratio=4500/len(obj.data.polygons);bpy.ops.object.modifier_apply(modifier=mod.name)
bm=bmesh.new();bm.from_mesh(obj.data);lo=min(v.co.z for v in bm.verts);hi=max(v.co.z for v in bm.verts);faces=[f for f in bm.faces if all(v.co.z<lo+(hi-lo)*.10 for v in f.verts)];bmesh.ops.delete(bm,geom=faces,context='FACES');bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bm.to_mesh(obj.data);bm.free();obj.data.update()
for img in bpy.data.images:
 if img.size[0]>2048:img.scale(2048,2048)
 if img.source=='FILE':img.pack()
bpy.context.preferences.filepaths.save_version=0;bpy.ops.wm.save_as_mainfile(filepath=str(out/'rock-clean.blend'));bpy.ops.export_scene.fbx(filepath=str(out/'rock-clean.fbx'),use_selection=True,object_types={'MESH'},path_mode='COPY',embed_textures=True,add_leaf_bones=False)
(out/'inspection.json').write_text(json.dumps({'triangles':len(obj.data.polygons),'merged_vertices':merged},indent=2));print('triangles',len(obj.data.polygons),flush=True)
