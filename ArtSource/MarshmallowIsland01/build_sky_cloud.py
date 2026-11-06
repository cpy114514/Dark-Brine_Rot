import bpy,os
base=os.path.dirname(os.path.abspath(__file__))
bpy.ops.wm.open_mainfile(filepath=os.path.join(base,'CottonCandy_Island01.blend'))
o=bpy.data.objects['Island_01_CottonCandy_HighRes']
bpy.ops.object.select_all(action='DESELECT');o.select_set(True);bpy.context.view_layer.objects.active=o
d=o.modifiers.new('Small distant cloud mesh','DECIMATE');d.ratio=.018;bpy.ops.object.modifier_apply(modifier=d.name)
o.name='CottonCandy_SkyCloud';o.data.materials.clear()
bpy.ops.wm.obj_export(filepath=os.path.join(base,'CottonCandy_SkyCloud.obj'),export_selected_objects=True,export_materials=False)
print('CLOUD_TRIANGLES',sum(len(p.vertices)-2 for p in o.data.polygons))
