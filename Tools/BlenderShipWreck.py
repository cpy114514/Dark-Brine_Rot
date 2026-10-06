"""Split the project's original ship, preserving its UVs and material assignments."""
import bpy
import json
import os
import sys
import bmesh
import math
from mathutils import Vector
from mathutils import Matrix

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
SOURCE = os.path.join(ROOT, 'Assets/Game/Prefabs/Environment/Props/Ship/ship_g.fbx')
REPORT = os.path.join(ROOT, '.codex/ship-wreck-blender')
os.makedirs(REPORT, exist_ok=True)
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=SOURCE)
meshes = [o for o in bpy.context.scene.objects if o.type == 'MESH']
audit = []
for obj in meshes:
    points = [obj.matrix_world @ v.co for v in obj.data.vertices]
    low = [min(p[i] for p in points) for i in range(3)]
    high = [max(p[i] for p in points) for i in range(3)]
    audit.append(dict(name=obj.name, vertices=len(points), faces=len(obj.data.polygons),
                      minimum=low, maximum=high, materials=[m.name if m else '' for m in obj.data.materials]))
with open(os.path.join(REPORT, 'audit.json'), 'w') as file:
    json.dump(audit, file, indent=2)
print('SHIP_AUDIT', json.dumps(audit), flush=True)

if '--build' not in sys.argv:
    sys.exit(0)

source_collection = bpy.data.collections.new('Original ship - reference')
bpy.context.scene.collection.children.link(source_collection)
pieces_collection = bpy.data.collections.new('Actual ship fragments')
bpy.context.scene.collection.children.link(pieces_collection)
for obj in meshes:
    for collection in list(obj.users_collection):
        collection.objects.unlink(obj)
    source_collection.objects.link(obj)
    obj.hide_render = True
    obj.hide_set(True)

texture_dir = os.path.dirname(SOURCE) + '/Textures'
for material in bpy.data.materials:
    if not material.use_nodes:
        continue
    texture = next((os.path.join(texture_dir, f) for f in os.listdir(texture_dir)
                    if f.startswith(material.name + '_BaseColor.')), None)
    if texture:
        image = bpy.data.images.load(texture, check_existing=True)
        node = material.node_tree.nodes.new('ShaderNodeTexImage')
        node.image = image
        shader = next((n for n in material.node_tree.nodes if n.type == 'BSDF_PRINCIPLED'), None)
        if shader:
            material.node_tree.links.new(node.outputs['Color'], shader.inputs['Base Color'])

def source_mesh(obj, simplify=False):
    data = obj.data.copy()
    data.transform(obj.matrix_world)
    if simplify and len(data.polygons) > 3000:
        copy = bpy.data.objects.new('Simplification temporary', data)
        bpy.context.scene.collection.objects.link(copy)
        modifier = copy.modifiers.new('Remove tiny hardware detail', 'DECIMATE')
        modifier.ratio = max(.08, min(1., 2500 / len(data.polygons)))
        graph = bpy.context.evaluated_depsgraph_get()
        simplified = bpy.data.meshes.new_from_object(copy.evaluated_get(graph))
        bpy.data.objects.remove(copy, do_unlink=True)
        data = simplified
    bm = bmesh.new()
    bm.from_mesh(data)
    bmesh.ops.remove_doubles(bm, verts=list(bm.verts), dist=.00001)
    return bm

def cut(bm, axis, location, keep_lower):
    normal = Vector((0, 0, 0)); normal[axis] = 1 if keep_lower else -1
    point = Vector((0, 0, 0)); point[axis] = location
    result = bmesh.ops.bisect_plane(bm, geom=list(bm.verts)+list(bm.edges)+list(bm.faces),
        dist=.00001, plane_co=point, plane_no=normal, clear_outer=True)
    edges = [e for e in result['geom_cut'] if isinstance(e, bmesh.types.BMEdge) and e.is_valid and e.is_boundary]
    if edges:
        filled = bmesh.ops.holes_fill(bm, edges=edges, sides=0)
        uv = bm.loops.layers.uv.active
        for face in filled['faces']:
            face.material_index = 0
            if uv:
                for loop in face.loops:
                    loop[uv].uv = (loop.vert.co.x * .12, loop.vert.co.z * .12)

records = []
def record(bm, original, kind, orientation, name):
    if len(bm.faces) < 2:
        bm.free(); return
    bmesh.ops.recalc_face_normals(bm, faces=list(bm.faces))
    if kind == 'deck':
        # The FBX's deck is a surface. Give the cut wood an underside and closed edges.
        data = bpy.data.meshes.new('Deck thickness temporary'); bm.to_mesh(data)
        copy = bpy.data.objects.new('Deck thickness temporary', data)
        bpy.context.scene.collection.objects.link(copy)
        modifier = copy.modifiers.new('Wood underside', 'SOLIDIFY')
        modifier.thickness = .12; modifier.offset = -1; modifier.use_rim = True
        thick = bpy.data.meshes.new_from_object(copy.evaluated_get(bpy.context.evaluated_depsgraph_get()))
        bpy.data.objects.remove(copy, do_unlink=True)
        bm.free(); bm = bmesh.new(); bm.from_mesh(thick)
    points = [v.co.copy() for v in bm.verts]
    lo = Vector(tuple(min(v[i] for v in points) for i in range(3)))
    hi = Vector(tuple(max(v[i] for v in points) for i in range(3)))
    center = (lo + hi) * .5
    rotate = Matrix.Rotation(math.radians(orientation), 3, 'Z')
    # Local X is ship starboard, local Z follows the bow. Float a curved side flat.
    floated = [rotate @ Vector((-v.y, v.z, -v.x)) for v in points]
    low = Vector(tuple(min(v[i] for v in floated) for i in range(3)))
    high = Vector(tuple(max(v[i] for v in floated) for i in range(3)))
    pivot = (low + high) * .5
    size = high - low
    if min(size.x, size.z) < .04:
        bm.free(); return
    size.y = max(.03, size.y)
    uv = bm.loops.layers.uv.active
    bmesh.ops.triangulate(bm, faces=list(bm.faces))
    vertices, texcoords, normals, triangles = [], [], [], []
    for face in bm.faces:
        tri = []
        for loop in face.loops:
            at = rotate @ Vector((-loop.vert.co.y, loop.vert.co.z, -loop.vert.co.x))
            at = (at - pivot)
            at = Vector((at.x / size.x, at.y / size.y, at.z / size.z))
            normal = rotate @ Vector((-loop.vert.normal.y, loop.vert.normal.z, -loop.vert.normal.x))
            tri.append(len(vertices)//3)
            vertices.extend(at)
            texcoords.extend(loop[uv].uv if uv else (0., 0.))
            normals.extend(normal)
        triangles.extend(tri)
    rec = dict(name=name, sourceObject=original.name, kind=kind,
               material=original.data.materials[0].name,
               center=[-center.x, center.z, center.y], sourceSize=list(size), orientation=orientation,
               vertices=vertices, uv=texcoords, normals=normals, triangles=triangles)
    records.append(rec)
    # Keep every authored piece at its true position in the original boat in the .blend.
    data = bpy.data.meshes.new(name); bm.to_mesh(data)
    data.materials.append(original.data.materials[0])
    obj = bpy.data.objects.new(name, data); pieces_collection.objects.link(obj)
    obj['source_object'] = original.name; obj['kind'] = kind
    bm.free()

def grid(original, counts, kind, float_side=False):
    base = source_mesh(original, simplify=kind == 'detail')
    lo = [min(v.co[i] for v in base.verts) for i in range(3)]
    hi = [max(v.co[i] for v in base.verts) for i in range(3)]
    for ix in range(counts[0]):
        for iy in range(counts[1]):
            for iz in range(counts[2]):
                bm = base.copy()
                for axis, cell in enumerate((ix, iy, iz)):
                    span = (hi[axis] - lo[axis])/counts[axis]
                    if cell > 0: cut(bm, axis, lo[axis] + span*cell, False)
                    if cell < counts[axis]-1: cut(bm, axis, lo[axis] + span*(cell+1), True)
                angle = (-90 if iy == 0 else 90) if float_side else (90 if kind == 'detail' else 0)
                record(bm, original, kind, angle, '%s_%s_%02d_%02d_%02d' % (kind,original.name,ix,iy,iz))
    base.free()

by_name = {o.name:o for o in meshes}
grid(by_name['Object_30'], (5, 4, 1), 'deck')
grid(by_name['Object_22'], (6, 2, 2), 'hull', float_side=True)
for original in meshes:
    if original.name in ('Object_30', 'Object_22'):
        continue
    bounds = next(a for a in audit if a['name'] == original.name)
    counts = [max(1, min(5, math.ceil((bounds['maximum'][i]-bounds['minimum'][i])/14))) for i in range(3)]
    # Split masts, rails, sails, cabin and fittings rather than making them vanish intact.
    grid(original, counts, 'detail')

gameplay = [r for r in records if r['kind'] in ('deck','hull')]
print('WRECK_COUNTS', json.dumps({k:len([r for r in records if r['kind']==k]) for k in ('deck','hull','detail')}), flush=True)
if len([r for r in records if r['kind']=='deck']) != 20:
    raise RuntimeError('Expected twenty nonempty deck pieces')
if len([r for r in records if r['kind']=='hull']) != 24:
    raise RuntimeError('Expected twenty-four hull pieces')
with open(os.path.join(REPORT, 'fragments.json'), 'w') as file:
    json.dump(dict(source='ship_g.fbx', pieces=records), file, separators=(',', ':'))
art = os.path.join(ROOT, 'ArtSource/ShipWreck'); os.makedirs(art, exist_ok=True)
for image in bpy.data.images:
    if image.source == 'FILE' and image.has_data:
        image.pack()
bpy.context.preferences.filepaths.save_version = 0
bpy.ops.wm.save_as_mainfile(filepath=os.path.join(art, 'ship_g_fractured.blend'))
print('SHIP_WRECK_COMPLETE', len(records), flush=True)
