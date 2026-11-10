"""Build the Island2 cliff from the verified CC0 Mountainside download."""

import json
from pathlib import Path
import bpy
from mathutils import Vector


root = Path.cwd()
source = root / ".codex/model-refinement/downloads/mountainside/mountainside.gltf"
workspace = root / "ArtSource/ModelRefinement/Island2"
output = root / "Assets/ThirdParty/SecondIslandCloudSea"
workspace.mkdir(parents=True, exist_ok=True)
output.mkdir(parents=True, exist_ok=True)

bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.gltf(filepath=str(source))
cliff, = [obj for obj in bpy.context.scene.objects if obj.type == "MESH"]
cliff.data.calc_loop_triangles()
original = len(cliff.data.loop_triangles)
original_bounds = [tuple(point) for point in cliff.bound_box]
cliff.name = "Mountainside - Layered Chalk Bluff"
bpy.context.view_layer.objects.active = cliff

# The scan includes dense microrelief already carried in its tangent normal map.
# Keep the main ledges, UV boundaries and coarse fractured silhouette in the mesh.
reduce = cliff.modifiers.new("Remove scan microtriangles", "DECIMATE")
reduce.ratio = 0.105
reduce.use_collapse_triangulate = True
bpy.ops.object.modifier_apply(modifier=reduce.name)
cliff.data.calc_loop_triangles()
refined = len(cliff.data.loop_triangles)

material, = cliff.data.materials
material.name = "Chalk and lavender stratified rock"
nodes = material.node_tree.nodes
links = material.node_tree.links
shader = next(node for node in nodes if node.type == "BSDF_PRINCIPLED")
base = shader.inputs["Base Color"]
if base.is_linked:
    base_image = base.links[0].from_node.image
    # Grade the source albedo in linear space and retain photographed crevices.
    import numpy as np
    pixels = np.empty(len(base_image.pixels), dtype=np.float32)
    base_image.pixels.foreach_get(pixels)
    rgba = pixels.reshape((-1, 4))
    luminance = rgba[:, :3] @ np.array([.2126, .7152, .0722], dtype=np.float32)
    rgba[:, :3] = rgba[:, :3] * .47 + luminance[:, None] * np.array([.58, .49, .62]) + [.022, .018, .032]
    new_image = bpy.data.images.new("ChalkDiffuse", width=base_image.size[0], height=base_image.size[1], alpha=False)
    new_image.pixels.foreach_set(pixels)
    new_image.pack()
    base.links[0].from_node.image = new_image

normal_map = next((node for node in nodes if node.type == "NORMAL_MAP"), None)
if normal_map:
    normal_map.inputs["Strength"].default_value = .82
bpy.ops.file.pack_all()
bpy.ops.wm.save_as_mainfile(filepath=str(workspace / "chalk-bluff.blend"), compress=True)
bpy.ops.export_scene.gltf(filepath=str(output / "ChalkBluff.glb"), export_format="GLB", export_tangents=True)
record = dict(source="https://polyhaven.com/a/mountainside", license="CC0",
              trianglesBefore=original, trianglesAfter=refined, boundsBefore=original_bounds,
              boundsAfter=[tuple(point) for point in cliff.bound_box])
(workspace / "chalk-bluff.json").write_text(json.dumps(record, indent=2), encoding="utf-8")
print(json.dumps(record))
