import bpy, bmesh, math
from mathutils import Vector

source = 'D:/indie_game_dev/3D Models/tungtungtungsahur.blend'
output = 'D:/indie_game_dev/unity/Dark Brine_Rot/tungtungtungsahur_separated_working.blend'
preview = 'D:/indie_game_dev/unity/Dark Brine_Rot/tungtungtungsahur_separated_preview.png'

bpy.ops.wm.open_mainfile(filepath=source)
scene = bpy.context.scene
original = bpy.data.objects['Mesh_0']
rig = bpy.data.objects['Armature']

# Restore visible rig controls in every saved 3D viewport.
rig.show_in_front = True
rig.display_type = 'WIRE'
rig.data.display_type = 'STICK'
for area in bpy.context.screen.areas:
    if area.type == 'VIEW_3D':
        area.spaces.active.overlay.show_overlays = True
        area.spaces.active.overlay.show_bones = True

# Preserve the original mesh untouched in a hidden backup collection.
backup = bpy.data.collections.new('Original mesh backup')
scene.collection.children.link(backup)
asset_collection = bpy.data.collections.new('Separated asset')
scene.collection.children.link(asset_collection)
for collection in list(original.users_collection):
    collection.objects.unlink(original)
backup.objects.link(original)
original.hide_set(True)
original.hide_render = True
original.name = 'Original_Mesh_Backup'

# The bat is the long, near-cylindrical region measured from the supplied model.
axis_start = Vector((-.143, -.371, .095))
axis_end = Vector((-.101, .080, .461))
axis = axis_end - axis_start
axis_sq = axis.length_squared

def bat_vertex(vertex):
    world = original.matrix_world @ vertex.co
    t = (world - axis_start).dot(axis) / axis_sq
    closest = axis_start + axis * max(0.0, min(1.0, t))
    radial = (world - closest).length
    return -.02 < t < .90 and radial < .066

def bat_face(face):
    # Any triangle touching the bat volume belongs to the separate club.
    # This prevents a thin remnant of the old bat staying fused to the hand.
    return any(bat_vertex(vertex) for vertex in face.verts)

def make_piece(name, want_bat):
    obj = original.copy()
    obj.data = original.data.copy()
    obj.name = name
    asset_collection.objects.link(obj)
    obj.hide_set(False)
    obj.hide_render = False
    bm = bmesh.new()
    bm.from_mesh(obj.data)
    bm.faces.ensure_lookup_table()
    remove = [face for face in bm.faces if bat_face(face) != want_bat]
    bmesh.ops.delete(bm, geom=remove, context='FACES')
    if want_bat:
        # Keep only the long, largest continuous shaft. Tiny parts near the
        # fingers are removed rather than becoming detached hand fragments.
        seen = set()
        components = []
        for start in bm.verts:
            if start in seen:
                continue
            stack = [start]
            seen.add(start)
            component = []
            while stack:
                vert = stack.pop()
                component.append(vert)
                for edge in vert.link_edges:
                    other = edge.other_vert(vert)
                    if other not in seen:
                        seen.add(other)
                        stack.append(other)
            components.append(component)
        if components:
            keep = max(components, key=len)
            remove_verts = [vert for comp in components if comp is not keep for vert in comp]
            if remove_verts:
                bmesh.ops.delete(bm, geom=remove_verts, context='VERTS')
    loose = [vert for vert in bm.verts if not vert.link_faces]
    if loose:
        bmesh.ops.delete(bm, geom=loose, context='VERTS')
    boundary = [edge for edge in bm.edges if edge.is_boundary]
    if boundary:
        bmesh.ops.holes_fill(bm, edges=boundary, sides=0)
    bmesh.ops.recalc_face_normals(bm, faces=list(bm.faces))
    bm.to_mesh(obj.data)
    bm.free()
    obj.data.update()
    return obj

character = make_piece('TungTung_Character', False)
club = make_piece('TungTung_Club', True)
club.location += Vector((-.090, -.012, 0.0))

# Keep the objects visually clear and ready for later hand-bone binding.
club['purpose'] = 'Separated club mesh; bind or parent to the chosen hand bone after final hand rigging.'
character['rig_note'] = 'Existing Armature is visible, but this source mesh has no Armature modifier or vertex groups.'

# Render a small rest-pose check, without saving a camera into the gameplay asset.
cam_data = bpy.data.cameras.new('Separation_Check_Camera')
cam = bpy.data.objects.new('Separation_Check_Camera', cam_data)
scene.collection.objects.link(cam)
scene.camera = cam
target = Vector((0, .03, .55))
cam.location = (1.25, -2.75, 1.30)
cam.rotation_euler = (target - cam.location).to_track_quat('-Z', 'Y').to_euler()
cam_data.type = 'ORTHO'
cam_data.ortho_scale = 1.38
scene.render.engine = 'BLENDER_WORKBENCH'
scene.render.resolution_x = 800
scene.render.resolution_y = 900
scene.render.resolution_percentage = 100
scene.display.shading.light = 'STUDIO'
scene.display.shading.color_type = 'TEXTURE'
scene.display.shading.show_shadows = True
scene.render.filepath = preview
bpy.ops.render.render(write_still=True)
bpy.data.objects.remove(cam, do_unlink=True)

bpy.ops.wm.save_as_mainfile(filepath=output)
print('SAVED', output)
