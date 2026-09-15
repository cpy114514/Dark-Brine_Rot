import bpy

src = 'D:/indie_game_dev/unity/Dark Brine_Rot/sahur_v2_working.blend'
out_blend = 'D:/indie_game_dev/3D Models/tongtongtongsahur_lively_walk_v2.blend'
out_fbx = 'D:/indie_game_dev/3D Models/tongtongtongsahur_lively_walk_v2.fbx'
bpy.ops.wm.open_mainfile(filepath=src)
scene = bpy.context.scene
rig = bpy.data.objects['Sahur_Walk_Rig']
rig.data.pose_position = 'POSE'
scene.frame_start = 1
scene.frame_end = 32
scene.render.fps = 24
if rig.animation_data:
    act = bpy.data.actions.get('Sahur_Lively_Walk_Separated_Arms')
    if act:
        rig.animation_data.action = act

export_names = {
    'Sahur_Original_Face_Legs',
    'Sahur_Rebuilt_Trunk',
    'Sahur_Rebuilt_Arm_L', 'Sahur_Rebuilt_Hand_L',
    'Sahur_Rebuilt_Arm_R', 'Sahur_Rebuilt_Hand_R',
    'Sahur_Wooden_Club',
    'Sahur_Walk_Rig',
}
bpy.ops.object.select_all(action='DESELECT')
for obj in scene.objects:
    obj.select_set(obj.name in export_names)
bpy.context.view_layer.objects.active = rig

scene.frame_set(1)
bpy.ops.export_scene.fbx(
    filepath=out_fbx,
    use_selection=True,
    object_types={'ARMATURE', 'MESH'},
    add_leaf_bones=False,
    use_armature_deform_only=True,
    bake_anim=True,
    bake_anim_use_all_actions=False,
    bake_anim_use_nla_strips=False,
    bake_anim_force_startend_keying=True,
    bake_anim_simplify_factor=0.0,
    path_mode='COPY',
    embed_textures=True,
    axis_forward='-Z',
    axis_up='Y',
)
bpy.ops.wm.save_as_mainfile(filepath=out_blend)
print('EXPORTED', out_fbx)
print('SAVED', out_blend)
