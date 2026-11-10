"""Prepare a compact version of the downloaded flowering shrub."""

import json
from pathlib import Path
import bpy


root = Path.cwd()
source = root / ".codex/model-refinement/downloads/shrub_01/shrub_01.gltf"
workspace = root / "ArtSource/ModelRefinement/Island2"
output = root / "Assets/ThirdParty/SecondIslandCloudSea"
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.gltf(filepath=str(source))
plant, = [obj for obj in bpy.context.scene.objects if obj.type == "MESH"]
bpy.context.view_layer.objects.active = plant
plant.data.calc_loop_triangles()
before = len(plant.data.loop_triangles)
plant.name = "White-flower highland shrub"

# Compare the reduced model with the source at the intended viewing size.
# Keep the original scan separate so close-up foliage can use a denser variant.
reduce = plant.modifiers.new("Plant silhouette budget", "DECIMATE")
reduce.ratio = .14
reduce.use_collapse_triangulate = True
bpy.ops.object.modifier_apply(modifier=reduce.name)
plant.data.calc_loop_triangles()
after = len(plant.data.loop_triangles)
bpy.ops.file.pack_all()
bpy.ops.wm.save_as_mainfile(filepath=str(workspace / "highland-shrub.blend"), compress=True)
bpy.ops.export_scene.gltf(filepath=str(output / "HighlandShrub.glb"), export_format="GLB", export_tangents=True)
(workspace / "highland-shrub.json").write_text(json.dumps({
    "source": "https://polyhaven.com/a/shrub_01",
    "license": "CC0",
    "trianglesBefore": before,
    "trianglesAfter": after
}, indent=2), encoding="utf-8")
print(f"Shrub: {before} -> {after} triangles")
