import bpy,sys,bmesh
from pathlib import Path
root=Path(r'D:/indiegameDev/unity/Dark Brine_Rot');out=root/'Assets/AnimationStaging/NailongWearablesV265';out.mkdir(parents=True,exist_ok=True)
for name in ['20260930130250_547090e7','20260930130258_d69f77fb','20260930131057_77c279ab']:
 bpy.ops.wm.read_factory_settings(use_empty=True)
 bpy.ops.import_scene.fbx(filepath=str(root/'Assets/Game/Equipment/Nailong'/f'{name}.fbx'))
 meshes=[o for o in bpy.context.scene.objects if o.type=='MESH'];print(name,'before',sum(len(o.data.polygons) for o in meshes))
 for o in meshes:
  bpy.context.view_layer.objects.active=o
  # Generated source meshes contain disconnected duplicate seam vertices.
  # Weld topology before simplification; decimating triangle islands alone
  # discarded surface coverage rather than simplifying the intact garment.
  bm=bmesh.new();bm.from_mesh(o.data)
  coords=[v.co for v in bm.verts];span=max((max(v[j] for v in coords)-min(v[j] for v in coords)) for j in range(3))
  bmesh.ops.remove_doubles(bm,verts=list(bm.verts),dist=span*.00001)
  bm.to_mesh(o.data);bm.free();o.data.update()
  if len(o.data.polygons)>12000:
   mod=o.modifiers.new('Wearable display LOD','DECIMATE');mod.ratio=min(1,7000/len(o.data.polygons));bpy.ops.object.modifier_apply(modifier=mod.name)
 bpy.ops.object.select_all(action='DESELECT')
 for o in meshes:o.select_set(True)
 bpy.ops.export_scene.fbx(filepath=str(out/f'{name}.fbx'),use_selection=True,object_types={'MESH'},add_leaf_bones=False,bake_anim=False,axis_forward='-Z',axis_up='Y')
 print(name,'after',sum(len(o.data.polygons) for o in meshes))
