import bpy,sys,json
from pathlib import Path
p=Path.cwd()/'ArtSource/Environment';src=p/'Hunyuan-20261027-r2';out=p/'Hunyuan-20261027-lod';out.mkdir(exist_ok=True)
bpy.ops.wm.open_mainfile(filepath=str(src/'rock-working.blend'));obj=next(o for o in bpy.data.objects if o.type=='MESH');bpy.context.view_layer.objects.active=obj;obj.select_set(True)
mod=obj.modifiers.new('Rock gameplay budget','DECIMATE');mod.ratio=600/len(obj.data.polygons);bpy.ops.object.modifier_apply(modifier=mod.name)
bpy.context.preferences.filepaths.save_version=0;bpy.ops.wm.save_as_mainfile(filepath=str(out/'rock-working.blend'));bpy.ops.export_scene.fbx(filepath=str(out/'rock-lod.fbx'),use_selection=True,object_types={'MESH'},path_mode='COPY',embed_textures=True,add_leaf_bones=False)
(out/'rock-inspection.json').write_text(json.dumps({'triangles':len(obj.data.polygons),'source':'Hunyuan rock r2','target':600},indent=2));print('triangles',len(obj.data.polygons))
