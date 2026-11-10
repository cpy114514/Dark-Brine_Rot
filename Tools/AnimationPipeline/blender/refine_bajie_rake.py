"""Refine the production rake in three parts while keeping its rigid skin."""

import json
from pathlib import Path
import struct
from uuid import uuid4

import bpy
from mathutils import Vector
from mathutils.bvhtree import BVHTree
import numpy as np


ROOT = Path.cwd()
EVIDENCE = ROOT / ".codex/model-refinement/bajie-rake"
ART = ROOT / "Assets/Game/Art/ModelRefinement"
TEXTURES = ROOT / "Assets/Game/Prefabs/Characters/ZhuBaJiePigForm/Human/Textures"
TARGET = ROOT / "ArtSource/ModelRefinement/ZhuBaJie"
# The long shaft has little curvature. The close-up rake teeth have a higher
# allowance than the narrow counterweight at the other end.
RATIOS = {"DingPa02": .42, "DingPa01": .34, "DingPa03": .18}
LABELS = {"DingPa02": "Bound shaft", "DingPa01": "Nine teeth and yoke", "DingPa03": "Counterweight"}


def read_source():
    with (EVIDENCE / "production.bin").open("rb") as file:
        size, bone_count, slots = struct.unpack("<iii", file.read(12))
        bones = [file.read(file.read(1)[0]).decode() for _ in range(bone_count)]
        materials = [file.read(file.read(1)[0]).decode() for _ in range(slots)]
        positions = np.fromfile(file, "<f4", size * 3).reshape(-1, 3)
        normals = np.fromfile(file, "<f4", size * 3).reshape(-1, 3)
        uv = np.fromfile(file, "<f4", size * 2).reshape(-1, 2)
        parts = [np.fromfile(file, "<i4", struct.unpack("<i", file.read(4))[0]).reshape(-1, 3) for _ in materials]
        weights = np.fromfile(file, dtype=[("count", "u1"), ("bone", "u1"), ("value", "<f4")])
    if len(weights) != size or not np.all(weights["count"] == 1) or not np.allclose(weights["value"], 1):
        raise ValueError("Production rake no longer uses rigid weights")
    index = int(weights["bone"][0])
    if not np.all(weights["bone"] == index) or bones[index] != "weapon":
        raise ValueError("Rake is no longer rigid to its weapon bone")
    return positions, normals, uv, parts, bones, materials, index


def load_map(slot, channel):
    return bpy.data.images.load(str(next(path for path in TEXTURES.rglob("*.png")
                                         if path.stem.lower().endswith(slot.lower() + "_" + channel))))


def sample(image, size):
    duplicate = image.copy()
    duplicate.scale(size, size)
    pixels = np.empty(size * size * 4, np.float32)
    duplicate.pixels.foreach_get(pixels)
    bpy.data.images.remove(duplicate)
    return pixels.reshape(size, size, 4)


def packed_map(slot, colour):
    size = 512
    source = load_map(slot, "m")
    source.colorspace_settings.name = "Non-Color"
    metal = sample(source, size)[:, :, 0]
    tint = sample(colour, size)[:, :, :3]
    value = tint.mean(axis=2)
    if slot == "DingPa01":
        # The metal source identifies exposed edges through the patina.
        polish = np.clip((metal - .12) / .63, 0, 1)
        gloss = .19 + polish * .31 + np.clip(value - .40, 0, .26) * .22
        metal = .08 + .69 * polish
    else:
        # Keep the woven wrap dry. Only the fittings receive a metal sheen.
        polish = np.clip((metal - .07) / .55, 0, 1)
        gloss = .16 + polish * .24
        metal = .52 * polish
    image = bpy.data.images.new("Bajie " + slot + " surface", size, size, alpha=True)
    image.colorspace_settings.name = "Non-Color"
    output = np.zeros((size, size, 4), np.float32)
    output[:, :, 0], output[:, :, 3] = metal, gloss
    image.pixels.foreach_set(output.ravel())
    image.filepath_raw = str(ART / ("Bajie" + slot + "Surface.png"))
    image.file_format = "PNG"
    image.save()
    image.pack()
    return image


def make_material(slot):
    material = bpy.data.materials.new(slot + " - " + LABELS[slot])
    material.use_nodes = True
    nodes, links = material.node_tree.nodes, material.node_tree.links
    shader = next(node for node in nodes if node.type == "BSDF_PRINCIPLED")
    base = nodes.new("ShaderNodeTexImage")
    base.image = load_map(slot, "d")
    links.new(base.outputs["Color"], shader.inputs["Base Color"])
    relief = nodes.new("ShaderNodeTexImage")
    relief.image = load_map(slot, "n")
    relief.image.colorspace_settings.name = "Non-Color"
    normal = nodes.new("ShaderNodeNormalMap")
    links.new(relief.outputs["Color"], normal.inputs["Color"])
    links.new(normal.outputs["Normal"], shader.inputs["Normal"])
    if slot in {"DingPa01", "DingPa02"}:
        surface = nodes.new("ShaderNodeTexImage")
        surface.image = packed_map(slot, base.image)
        separate = nodes.new("ShaderNodeSeparateColor")
        links.new(surface.outputs["Color"], separate.inputs["Color"])
        links.new(separate.outputs["Red"], shader.inputs["Metallic"])
        roughness = nodes.new("ShaderNodeMath")
        roughness.operation = "SUBTRACT"
        roughness.inputs[0].default_value = 1
        links.new(surface.outputs["Alpha"], roughness.inputs[1])
        links.new(roughness.outputs[0], shader.inputs["Roughness"])
    else:
        shader.inputs["Roughness"].default_value = .72
        shader.inputs["Metallic"].default_value = .22
    return material


def body(slot, triangles, positions, normals, uv, bone):
    face_indices = triangles[:, ::-1]
    indexed = face_indices.ravel()
    points, inverse = np.unique(positions[indexed], axis=0, return_inverse=True)
    mesh = bpy.data.meshes.new(slot + " original")
    mesh.from_pydata(points.tolist(), [], inverse.reshape(-1, 3).tolist(), shade_flat=False)
    mesh.uv_layers.new(name="Original atlas").uv.foreach_set("vector", uv[indexed].ravel())
    mesh.normals_split_custom_set(normals[indexed].tolist())
    mesh.materials.append(make_material(slot))
    original = bpy.data.objects.new(LABELS[slot] + " - production reference", mesh)
    bpy.context.collection.objects.link(original)
    original.vertex_groups.new(name=bone).add(list(range(len(mesh.vertices))), 1, "REPLACE")
    refined = original.copy()
    refined.data = mesh.copy()
    refined.name = LABELS[slot]
    bpy.context.collection.objects.link(refined)
    bpy.context.view_layer.objects.active = refined
    refined.select_set(True)
    modifier = refined.modifiers.new("Preserve ornament silhouettes", "DECIMATE")
    modifier.ratio = RATIOS[slot]
    modifier.use_collapse_triangulate = True
    bpy.ops.object.modifier_apply(modifier=modifier.name)
    refined.select_set(False)
    original.hide_set(True)
    original.hide_render = True
    return original, refined


def surface_distance(source, target):
    mesh = target.data
    tree = BVHTree.FromPolygons([vert.co for vert in mesh.vertices], [poly.vertices[:] for poly in mesh.polygons],
                                all_triangles=True)
    distances = [tree.find_nearest(vert.co)[3] for vert in source.data.vertices]
    return float(np.quantile(distances, .95)), max(distances)


def export_meshes(meshes, bone_names, material_names, bone):
    points, normals, uv, triangles = [], [], [], []
    for model in meshes:
        mesh = model.data
        mesh.calc_loop_triangles()
        indices = []
        lookup = {}
        for face in mesh.loop_triangles:
            for loop in reversed(face.loops):
                vertex = mesh.loops[loop].vertex_index
                tex = mesh.uv_layers.active.uv[loop].vector
                n = mesh.corner_normals[loop].vector
                key = (vertex, *tex, *n)
                if key not in lookup:
                    p = mesh.vertices[vertex].co
                    lookup[key] = len(points)
                    points.append((p.x, p.z, p.y))
                    normals.append((n.x, n.z, n.y))
                    uv.append(tuple(tex))
                indices.append(lookup[key])
        triangles.append(indices)
    with (EVIDENCE / "refined.bin").open("wb") as file:
        file.write(struct.pack("<iiii", len(points), len(bone_names), len(material_names), bone))
        for name in [*bone_names, *material_names]:
            raw = name.encode()
            file.write(bytes([len(raw)]))
            file.write(raw)
        for records in (points, normals, uv):
            np.asarray(records, dtype="<f4").tofile(file)
        for part in triangles:
            file.write(struct.pack("<i", len(part)))
            np.asarray(part, dtype="<i4").tofile(file)
    return len(points)


bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.context.preferences.filepaths.save_version = 0
xyz, normal, uv, parts, bones, materials, bone = read_source()
models, summary = [], []
for slot, triangles in zip(materials, parts):
    original, refined = body(slot, triangles, xyz[:, [0, 2, 1]], normal[:, [0, 2, 1]], uv, bones[bone])
    percentile, maximum = surface_distance(original, refined)
    reverse_percentile, reverse_maximum = surface_distance(refined, original)
    if max(maximum, reverse_maximum) > .006:
        raise ValueError("A protected rake feature drifted more than 6 mm: " + slot)
    summary.append({"slot": slot, "sourceTriangles": len(triangles), "refinedTriangles": len(refined.data.polygons),
                    "distanceP95Metres": max(percentile, reverse_percentile),
                    "maximumMetres": max(maximum, reverse_maximum)})
    models.append(refined)
vertices = export_meshes(models, bones, materials, bone)
(EVIDENCE / "blender-rake.json").write_text(json.dumps({"parts": summary, "vertices": vertices}, indent=2))
print(summary, flush=True)
bpy.ops.file.pack_all()
TARGET.mkdir(parents=True, exist_ok=True)
temporary = TARGET / (uuid4().hex + ".blend")
bpy.ops.wm.save_as_mainfile(filepath=str(temporary), compress=True)
temporary.replace(TARGET / "Nine-tooth rake - topology and patina.blend")
