"""Refine Sahur's production club with a topology-safe static mesh exchange."""

import json
import math
import struct
from pathlib import Path
import bpy
import bmesh
import numpy as np
from mathutils import Vector


ROOT = Path.cwd()
EVIDENCE = ROOT / ".codex/model-refinement/stick"
WORK = ROOT / "ArtSource/ModelRefinement/Sahur"
ART = ROOT / "Assets/Game/Art/ModelRefinement"
WORK.mkdir(parents=True, exist_ok=True)
ART.mkdir(parents=True, exist_ok=True)


def mesh_object(name, vertices, faces):
    mesh = bpy.data.meshes.new(name)
    mesh.from_pydata(vertices.tolist(), [], faces.tolist())
    mesh.update()
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(obj)
    for polygon in mesh.polygons:
        polygon.use_smooth = True
    return obj


def material(name, colour, roughness):
    mat = bpy.data.materials.new(name)
    mat.diffuse_color = (*colour, 1)
    mat.use_nodes = True
    bsdf = mat.node_tree.nodes.get("Principled BSDF")
    bsdf.inputs["Base Color"].default_value = (*colour, 1)
    bsdf.inputs["Roughness"].default_value = roughness
    return mat


def textures(mat):
    # Longitudinal fibres, dark fissures and fine ripples use the same periodic
    # coordinates. The grain wraps continuously around the authored club.
    size = 512
    u, v = np.meshgrid(np.arange(size) / float(size), np.arange(size) / float(size))
    q = math.tau * u
    warp = .075 * np.sin(11 * v + 2 * np.cos(3 * q)) + .036 * np.sin(17 * v - 4 * q)
    coarse = np.sin(8 * q + 9 * warp)
    grain = np.sin(28 * q + 22 * warp + 2.2 * np.sin(v * 6 + q * 7))
    fiber = np.sin(103 * q + 31 * warp + 2.4 * np.cos(26 * v + 3 * q))
    scratch = np.power(np.maximum(0, np.sin(58 * q + 2 * np.sin(13 * v - 5 * q))), 13)
    pores = np.sin(64 * q - 261 * v) * np.sin(141 * q + 112 * v)
    weather = .5 + .20 * np.sin(3 * q + 7 * v) + .12 * np.sin(19 * v - 5 * q)
    brightness = np.clip(.93 + .15 * coarse + .13 * grain + .045 * fiber - .19 * scratch + .07 * weather, .55, 1.37)
    wood = np.array([.235, .103, .039], dtype=np.float32)
    grip_wear = .025 * np.exp(-np.square((v - .2) / .09)) + .013 * np.exp(-np.square((v - .91) / .04))
    rgb = brightness[..., None] * wood + (.012 * weather + grip_wear)[..., None] * [1.0, .66, .38]
    rgb = np.where(rgb < .0031308, rgb * 12.92, 1.055 * np.power(rgb, 1 / 2.4) - .055)
    height = .030 * coarse + .016 * grain + .004 * fiber - .023 * scratch + .0028 * pores
    dx = (np.roll(height, -1, axis=1) - np.roll(height, 1, axis=1)) * 3.0
    dy = (np.roll(height, -1, axis=0) - np.roll(height, 1, axis=0)) * 3.0
    normal = np.stack([-dx, -dy, np.ones_like(dx)], axis=-1)
    normal /= np.linalg.norm(normal, axis=-1)[..., None]
    for filename, pixels, colour in (("SahurStickColour", rgb, True), ("SahurStickNormal", normal * .5 + .5, False)):
        im = bpy.data.images.new(filename, size, size, alpha=False)
        if not colour:
            im.colorspace_settings.name = "Non-Color"
        rgba = np.concatenate([pixels, np.ones((*pixels.shape[:2], 1))], axis=-1).astype(np.float32)
        im.pixels.foreach_set(rgba.ravel())
        im.filepath_raw = str(ART / (filename + ".png"))
        im.file_format = "PNG"
        im.save()
        im.pack()
        node = mat.node_tree.nodes.new("ShaderNodeTexImage")
        node.image = im
        bsdf = mat.node_tree.nodes.get("Principled BSDF")
        if colour:
            mat.node_tree.links.new(node.outputs["Color"], bsdf.inputs["Base Color"])
            mat.node_tree.nodes.active = node
        else:
            bump = mat.node_tree.nodes.new("ShaderNodeNormalMap")
            bump.inputs["Strength"].default_value = .66
            mat.node_tree.links.new(node.outputs["Color"], bump.inputs["Color"])
            mat.node_tree.links.new(bump.outputs["Normal"], bsdf.inputs["Normal"])


bpy.ops.wm.read_factory_settings(use_empty=True)
payload = (EVIDENCE / "native-source.bin").read_bytes()
count, index_count = struct.unpack_from("<ii", payload)
vertices = np.frombuffer(payload, dtype="<f4", count=count * 3, offset=8).reshape((-1, 3))
indices = np.frombuffer(payload, dtype="<i4", count=index_count, offset=8 + count * 20).reshape((-1, 3))
# Unity to Blender. Author in convenient units; the production units are restored below.
points = vertices[:, [0, 2, 1]].copy() * 1000
points[:, 1] *= -1
before = mesh_object("Source club", points, indices[:, ::-1])
before.data.materials.append(material("Source plain wood", (.20, .075, .022), .76))
after = before.copy()
after.data = before.data.copy()
after.name = "Refined club"
bpy.context.collection.objects.link(after)
bpy.context.view_layer.objects.active = after
after.select_set(True)

# This static prop has many exact duplicated seam vertices. Rebuild its one
# continuous wood UV after the geometry cleanup instead of accumulating seams.
bm = bmesh.new()
bm.from_mesh(after.data)
bmesh.ops.remove_doubles(bm, verts=list(bm.verts), dist=.00004)
bmesh.ops.recalc_face_normals(bm, faces=list(bm.faces))
bm.to_mesh(after.data)
bm.free()
reduce = after.modifiers.new("Remove dense scan triangulation", "DECIMATE")
reduce.ratio = .15
reduce.use_collapse_triangulate = True
bpy.ops.object.modifier_apply(modifier=reduce.name)
after.data.update()

# A restrained pass on the long wood surface removes high-frequency lumps.
# Extrema and the ends stay fixed to retain the grip and tip profile.
coords = np.array([tuple(v.co) for v in after.data.vertices])
mins, maxs = points.min(axis=0), points.max(axis=0)
keep = np.any((coords - mins < .055) | (maxs - coords < .055), axis=1)
center = coords.mean(axis=0)
_, _, axes = np.linalg.svd(coords - center, full_matrices=False)
axis = axes[0]
axis *= np.sign(axis[2])
side = np.cross(axis, [0, 1, 0])
side /= np.linalg.norm(side)
across = np.cross(axis, side)
longitudinal = (coords - center) @ axis
lo, hi = longitudinal.min(), longitudinal.max()
t = (longitudinal - lo) / (hi - lo)
neighbours = [[] for _ in coords]
for edge in after.data.edges:
    a, b = edge.vertices
    neighbours[a].append(b)
    neighbours[b].append(a)
for i, v in enumerate(after.data.vertices):
    if keep[i] or t[i] < .12 or t[i] > .89 or not neighbours[i]:
        continue
    delta = coords[neighbours[i]].mean(axis=0) - coords[i]
    delta -= np.dot(delta, axis) * axis
    v.co += Vector(np.clip(delta * .28, -.022, .022))
after.data.update()

uv_layer = after.data.uv_layers.new(name="Continuous wood grain")
uv = []
for vertex in after.data.vertices:
    p = np.array(vertex.co) - center
    u = math.atan2(np.dot(p, across), np.dot(p, side)) / math.tau + .5
    v = float(np.clip((np.dot(p, axis) - lo) / (hi - lo), 0, 1))
    uv.append((u, v))
for face in after.data.polygons:
    xs = [uv[i][0] for i in face.vertices]
    seam = max(xs) - min(xs) > .5
    for loop in face.loop_indices:
        u, v = uv[after.data.loops[loop].vertex_index]
        uv_layer.uv[loop].vector = (u + (1 if seam and u < .5 else 0), v)

mat = material("Grain and worn ridges", (.24, .10, .04), .73)
textures(mat)
after.data.materials.clear()
after.data.materials.append(mat)
after.data.calc_loop_triangles()
native_points, native_uv, native_normals, native_indices, remap = [], [], [], [], {}
for tri in after.data.loop_triangles:
    for loop in reversed(tri.loops):
        vertex = after.data.vertices[after.data.loops[loop].vertex_index]
        texcoord = tuple(uv_layer.uv[loop].vector)
        key = vertex.index, texcoord
        if key not in remap:
            remap[key] = len(native_points)
            x, y, z = vertex.co
            nx, ny, nz = vertex.normal
            native_points.append((x / 1000, z / 1000, -y / 1000))
            native_uv.append(texcoord)
            native_normals.append((nx, nz, -ny))
        native_indices.append(remap[key])
with (EVIDENCE / "native-refined.bin").open("wb") as out:
    out.write(struct.pack("<ii", len(native_points), len(native_indices)))
    for values in [native_points, native_uv, native_normals]:
        out.write(np.asarray(values, dtype="<f4").tobytes())
    out.write(np.asarray(native_indices, dtype="<i4").tobytes())

bpy.context.preferences.filepaths.save_version = 0
before.hide_set(True)
bpy.ops.wm.save_as_mainfile(filepath=str(WORK / "Sahur Stick - worked wood.blend"), compress=True)
record = {"verticesBefore": count, "trianglesBefore": index_count // 3, "verticesAfter": len(native_points),
          "trianglesAfter": len(native_indices) // 3, "maximumLocalSmoothingUnits": .022,
          "mapResolution": 512, "skeleton": "Static weapon; attachment and BoxCollider live in the Unity prefab."}
(EVIDENCE / "blender.json").write_text(json.dumps(record, indent=2))
print(json.dumps(record))

# Identical studio turntable for the source and final mesh.
scene = bpy.context.scene
camera_data = bpy.data.cameras.new("Matched orthographic camera")
camera = bpy.data.objects.new(camera_data.name, camera_data)
scene.collection.objects.link(camera)
scene.camera = camera
camera_data.type = "ORTHO"
camera_data.ortho_scale = 12.7
scene.render.engine = "BLENDER_WORKBENCH"
scene.render.resolution_x = scene.render.resolution_y = 520
scene.render.resolution_percentage = 100
scene.display.shading.light = "STUDIO"
scene.display.shading.color_type = "TEXTURE"
scene.display.shading.show_shadows = True
scene.display.shading.show_cavity = True
scene.display.shading.cavity_type = "BOTH"
target = Vector((mins + maxs) / 2)
for angle, direction in (("front", (0, -1, .09)), ("side", (1, 0, .09)), ("back", (0, 1, .09))):
    camera.location = target + Vector(direction) * 25
    camera.rotation_euler = (target - camera.location).to_track_quat("-Z", "Y").to_euler()
    for label, visible in (("before", before), ("after", after)):
        before.hide_render = before != visible
        after.hide_render = after != visible
        scene.render.filepath = str(EVIDENCE / f"{label}-{angle}.png")
        bpy.ops.render.render(write_still=True)
