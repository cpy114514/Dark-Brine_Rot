"""Build a distance mesh from the existing Unity duck skin without changing its rig."""

from collections import defaultdict
import json
from pathlib import Path
import struct

import bpy
import numpy as np


ROOT = Path.cwd()
EVIDENCE = ROOT / ".codex/model-refinement/duck"
SOURCE = ROOT / "Assets/Game/Prefabs/Enemies/LongLeggedDuck/Textures"


def read_source():
    with (EVIDENCE / "production.bin").open("rb") as stream:
        count, index_count, bone_count = struct.unpack("<iii", stream.read(12))
        bones = [stream.read(stream.read(1)[0]).decode() for _ in range(bone_count)]
        positions = np.fromfile(stream, "<f4", count * 3).reshape((-1, 3))
        normals = np.fromfile(stream, "<f4", count * 3).reshape((-1, 3))
        uv = np.fromfile(stream, "<f4", count * 2).reshape((-1, 2))
        triangles = np.fromfile(stream, "<i4", index_count).reshape((-1, 3))
        weights = np.zeros((count, bone_count), np.float32)
        for vertex in range(count):
            for _ in range(stream.read(1)[0]):
                bone, weight = struct.unpack("<Bf", stream.read(5))
                weights[vertex, bone] = weight
    return positions, normals, uv, triangles, bones, weights


def add_body(name, points, faces, loop_uv, loop_normals, bones, weights, material):
    data = bpy.data.meshes.new(name)
    data.from_pydata(points.tolist(), [], faces.tolist(), shade_flat=False)
    data.uv_layers.new(name="Production UV").uv.foreach_set("vector", loop_uv.ravel())
    data.normals_split_custom_set(loop_normals.tolist())
    data.materials.append(material)
    model = bpy.data.objects.new(name, data)
    bpy.context.collection.objects.link(model)
    for index, name in enumerate(bones):
        group = model.vertex_groups.new(name=name)
        values = defaultdict(list)
        for vertex in np.flatnonzero(weights[:, index]):
            values[float(weights[vertex, index])].append(int(vertex))
        for weight, indices in values.items():
            group.add(indices, weight, "REPLACE")
    return model


def export_lod(body, scale, bones, source_triangle_count, original_count):
    data = body.data
    data.calc_loop_triangles()
    result = {}
    vertices = []
    indices = []
    for triangle in data.loop_triangles:
        for loop in reversed(triangle.loops):
            vertex = data.vertices[data.loops[loop].vertex_index]
            pos = vertex.co
            norm = data.corner_normals[loop].vector
            uv = data.uv_layers.active.uv[loop].vector
            key = (vertex.index, *norm, *uv)
            if key not in result:
                influences = sorted(
                    [(group.group, group.weight) for group in vertex.groups if group.weight > 1e-6],
                    key=lambda pair: pair[1], reverse=True
                )[:4]
                if not influences:
                    raise ValueError("Decimation produced an unweighted vertex")
                total = sum(weight for _, weight in influences)
                result[key] = len(vertices)
                vertices.append((
                    (pos.x / scale, pos.z / scale, pos.y / scale, *uv, norm.x, norm.z, norm.y),
                    [(bone, weight / total) for bone, weight in influences]
                ))
            indices.append(result[key])
    with (EVIDENCE / "distance.bin").open("wb") as stream:
        stream.write(struct.pack("<iii", len(vertices), len(indices), len(bones)))
        for attribs, weights in vertices:
            stream.write(struct.pack("<8fB", *attribs, len(weights)))
            for bone, weight in weights:
                stream.write(struct.pack("<Bf", bone, weight))
        np.asarray(indices, dtype="<i4").tofile(stream)
    stats = {
        "sourceTriangles": source_triangle_count,
        "sourceVertices": original_count,
        "lodTriangles": len(indices) // 3,
        "lodVertices": len(vertices),
        "boneNames": bones,
    }
    (EVIDENCE / "blender.json").write_text(json.dumps(stats, indent=2))
    print(stats, flush=True)


bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.context.preferences.filepaths.save_version = 0
positions, normals, uv, triangles, bones, weights = read_source()
# Restore the project's FBX scale for comfortable Blender editing and convert handedness.
scale = 100.0
faces = triangles[:, ::-1]
points = positions[:, [0, 2, 1]] * scale
loop_uv = uv[faces.reshape(-1)]
loop_normals = normals[faces.reshape(-1)][:, [0, 2, 1]]
surface = bpy.data.materials.new("Long-legged duck original colour")
surface.use_nodes = True
shader = next(node for node in surface.node_tree.nodes if node.type == "BSDF_PRINCIPLED")
image = surface.node_tree.nodes.new("ShaderNodeTexImage")
image.image = bpy.data.images.load(str(SOURCE / "texture_pbr_20250901.png"))
image.image.pack()
surface.node_tree.links.new(image.outputs["Color"], shader.inputs["Base Color"])

# Join only precisely coincident vertices with identical skin weights. UVs and
# split normals are corner data, so texture seams survive the join.
_, unique, inverse = np.unique(
    np.concatenate((points, weights), axis=1), axis=0, return_index=True, return_inverse=True)
original = add_body("Production rest mesh", points[unique], inverse[faces], loop_uv, loop_normals,
                    bones, weights[unique], surface)
lod = original.copy()
lod.data = original.data.copy()
lod.name = "Long-legged duck - distance mesh"
bpy.context.collection.objects.link(lod)
bpy.context.view_layer.objects.active = lod
lod.select_set(True)
modifier = lod.modifiers.new("Distance topology", "DECIMATE")
modifier.ratio = .13
modifier.use_collapse_triangulate = True
bpy.ops.object.modifier_apply(modifier=modifier.name)
original.hide_set(True)
original.hide_render = True
export_lod(lod, scale, bones, len(triangles), len(positions))
target = ROOT / "ArtSource/ModelRefinement/LongLeggedDuck"
target.mkdir(parents=True, exist_ok=True)
bpy.ops.wm.save_as_mainfile(filepath=str(target / "Long-legged duck - distance mesh.blend"), compress=True)
