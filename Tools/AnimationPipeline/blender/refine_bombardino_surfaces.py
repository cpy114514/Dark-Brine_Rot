"""Refine the existing crocodile plane's painted metal, engines and skin."""

from pathlib import Path
from uuid import uuid4
import json

import bpy
import numpy as np


ROOT = Path.cwd()
FOLDER = ROOT / "Assets/Game/Prefabs/Bosses/BombardinoCrocodilo"
ART = ROOT / "Assets/Game/Art/ModelRefinement"
WORK = ROOT / "ArtSource/ModelRefinement/Bombardino"
EVIDENCE = ROOT / ".codex/model-refinement/bombardino"
SIZE = 512
SOURCES = {"Fusil": "Fusil.002", "Engine": "Engine.002", "crocodile_v01body_blinn": "crocodile_v01body_blinn.001"}


def pixels(image):
    copy = image.copy()
    copy.scale(SIZE, SIZE)
    data = np.empty(SIZE * SIZE * 4, np.float32)
    copy.pixels.foreach_get(data)
    bpy.data.images.remove(copy)
    return data.reshape(SIZE, SIZE, 4)


def ramp(lo, hi, x):
    t = np.clip((x - lo) / (hi - lo), 0, 1)
    return t * t * (3 - 2 * t)


bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.context.preferences.filepaths.save_version = 0
bpy.ops.import_scene.fbx(filepath=str(FOLDER / "source/Low.fbx"))
models = [obj for obj in bpy.context.scene.objects if obj.type == "MESH"]
for image in bpy.data.images:
    if image.source != "FILE":
        continue
    basename = Path(image.filepath).name
    candidate = FOLDER / "textures" / basename
    if not candidate.exists() and "normal" in basename.lower():
        candidate = FOLDER / "textures" / (basename.split(".")[0] + ".002_Normal_OpenGL.png")
    if not candidate.exists():
        raise FileNotFoundError("Missing production texture " + basename)
    image.filepath = str(candidate)
    image.reload()
summary = []
for slot, prefix in SOURCES.items():
    colour = bpy.data.images.load(str(FOLDER / ("textures/" + prefix + "_Base_color.png")), check_existing=True)
    normal = bpy.data.images.load(str(FOLDER / ("textures/" + prefix + "_Normal_OpenGL.png")), check_existing=True)
    normal.colorspace_settings.name = "Non-Color"
    rgb = pixels(colour)[:, :, :3]
    value = rgb.mean(axis=2)
    chroma = rgb.max(axis=2) - rgb.min(axis=2)
    slope = np.clip(1 - pixels(normal)[:, :, 2], 0, .5)
    if slot == "Fusil":
        bare = ramp(.30, .60, value) * (1 - ramp(.08, .23, chroma))
        metal = bare * .69
        smoothness = .145 + bare * .24 - slope * .08
    elif slot == "Engine":
        exposed = ramp(.21, .53, value)
        metal = .31 + exposed * .44
        smoothness = .22 + exposed * .17 - slope * .09
    else:
        ivory = ramp(.51, .73, rgb.min(axis=2)) * (1 - ramp(.05, .18, chroma))
        metal = np.zeros_like(value)
        smoothness = .17 + ramp(.13, .44, value) * .09 + ivory * .23 - slope * .16
    image = bpy.data.images.new("Bombardino " + slot + " surface", SIZE, SIZE, alpha=True)
    image.colorspace_settings.name = "Non-Color"
    values = np.zeros((SIZE, SIZE, 4), np.float32)
    values[:, :, 0] = np.clip(metal, 0, 1)
    values[:, :, 3] = np.clip(smoothness, .10, .54)
    image.pixels.foreach_set(values.ravel())
    image.filepath_raw = str(ART / ("Bombardino" + slot + "Surface.png"))
    image.file_format = "PNG"
    image.save()
    image.pack()
    refined = bpy.data.materials.new(slot + " refined")
    refined.use_nodes = True
    nodes, links = refined.node_tree.nodes, refined.node_tree.links
    shader = next(node for node in nodes if node.type == "BSDF_PRINCIPLED")
    for texture in [colour, normal, image]:
        node = nodes.new("ShaderNodeTexImage")
        node.image = texture
        if texture == colour:
            links.new(node.outputs["Color"], shader.inputs["Base Color"])
        elif texture == normal:
            bump = nodes.new("ShaderNodeNormalMap")
            bump.inputs["Strength"].default_value = .75
            links.new(node.outputs["Color"], bump.inputs["Color"])
            links.new(bump.outputs["Normal"], shader.inputs["Normal"])
        else:
            separate = nodes.new("ShaderNodeSeparateColor")
            links.new(node.outputs["Color"], separate.inputs["Color"])
            links.new(separate.outputs["Red"], shader.inputs["Metallic"])
            subtract = nodes.new("ShaderNodeMath")
            subtract.operation = "SUBTRACT"
            subtract.inputs[0].default_value = 1
            links.new(node.outputs["Alpha"], subtract.inputs[1])
            links.new(subtract.outputs[0], shader.inputs["Roughness"])
    assigned = 0
    for model in models:
        for index, material in enumerate(model.data.materials):
            if material and material.name == slot:
                model.data.materials[index] = refined
                assigned += 1
    if assigned == 0:
        raise ValueError("Source material slot missing: " + slot)
    summary.append({"slot": slot, "smoothnessP05P50P95": np.quantile(values[:, :, 3], [.05, .5, .95]).tolist()})
bpy.ops.file.pack_all()
WORK.mkdir(parents=True, exist_ok=True)
temporary = WORK / (uuid4().hex + ".blend")
bpy.ops.wm.save_as_mainfile(filepath=str(temporary), compress=True)
temporary.replace(WORK / "Bombardino - painted metal and scales.blend")
EVIDENCE.mkdir(parents=True, exist_ok=True)
(EVIDENCE / "blender-surfaces.json").write_text(json.dumps(summary, indent=2))
print(summary, flush=True)
