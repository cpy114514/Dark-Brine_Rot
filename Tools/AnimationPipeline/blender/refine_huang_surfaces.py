"""Give Huang Mei's existing skin, robes and ornaments separate surface responses."""

import json
from pathlib import Path
from uuid import uuid4

import bpy
import numpy as np


ROOT = Path.cwd()
FOLDER = ROOT / "Assets/Game/Prefabs/Characters/HuangMei"
ART = ROOT / "Assets/Game/Art/ModelRefinement"
WORK = ROOT / "ArtSource/ModelRefinement/HuangMei"
EVIDENCE = ROOT / ".codex/model-refinement/huang"
SIZE = 512


def pixels(image):
    original = image
    if tuple(image.size) != (SIZE, SIZE):
        image = image.copy()
        image.scale(SIZE, SIZE)
    values = np.empty(SIZE * SIZE * 4, np.float32)
    image.pixels.foreach_get(values)
    if image != original:
        bpy.data.images.remove(image)
    return values.reshape(SIZE, SIZE, 4)


def ramp(lo, hi, value):
    x = np.clip((value - lo) / (hi - lo), 0, 1)
    return x * x * (3 - 2 * x)


def response(slot, colour, normal):
    r, g, b = np.moveaxis(colour[:, :, :3], -1, 0)
    value = (r + g + b) / 3
    relief = np.clip(1 - normal[:, :, 2], 0, .5)
    u, v = np.meshgrid(np.arange(SIZE) / SIZE, np.arange(SIZE) / SIZE)
    gold = ramp(.09, .28, r - b) * ramp(-.015, .09, g - b) * ramp(.22, .52, g)
    metal = np.zeros_like(value)
    if slot == "Head01":
        # Dry pores and cheeks with a restrained skin sheen across the broad
        # forehead and nose. The base colour still carries the existing age.
        forehead = np.exp(-((u - .51) / .18) ** 2 - ((v - .68) / .14) ** 2)
        nose = np.exp(-((u - .51) / .06) ** 2 - ((v - .46) / .13) ** 2)
        smooth = .235 + .10 * np.maximum(forehead, nose) - relief * .19
    elif slot == "Body01":
        smooth = .21 + ramp(.12, .43, value) * .08 - relief * .16
    elif slot == "Cloth01":
        fiber = np.sin(u * np.pi * 235) * np.sin(v * np.pi * 219)
        smooth = .115 + ramp(.24, .62, value) * .06 + fiber * .012 - relief * .07
    elif slot == "Cloth02":
        metal = gold * .38
        smooth = .13 + gold * .26 - relief * .12
    elif slot == "ZhuangBei01":
        metal = gold * .73
        red_lacquer = ramp(.14, .44, r - np.maximum(g, b)) * ramp(.28, .62, r)
        blue_cloth = ramp(.04, .18, b - r)
        smooth = .20 + gold * .29 + red_lacquer * .12 - blue_cloth * .06 - relief * .10
    else:
        metal = gold * .51
        lacquer = ramp(.07, .27, r - b)
        smooth = .24 + lacquer * .08 + gold * .16 - relief * .11
    return np.clip(metal, 0, 1), np.clip(smooth, .10, .56)


bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.context.preferences.filepaths.save_version = 0
bpy.ops.import_scene.fbx(filepath=str(FOLDER / "Models/Huang Mei Source.fbx"))
body = max((o for o in bpy.context.scene.objects if o.type == "MESH"), key=lambda o: len(o.data.polygons))
summary = []
slots = ["Head01", "Body01", "ZhuangBei01", "FoXiang02", "Cloth02", "FoXiang03", "Cloth01"]
refined = {}
for index, original in enumerate(list(body.data.materials)):
    slot = next((name for name in slots if ("_" + name + "_").lower() in original.name.lower()), None)
    if slot is None:
        continue
    if slot in refined:
        body.data.materials[index] = refined[slot]
        continue
    colour = bpy.data.images.load(str(FOLDER / ("Textures/T_LYS_HuangMei_" + slot + "_D.png")), check_existing=True)
    normal = bpy.data.images.load(str(FOLDER / ("Textures/T_LYS_HuangMei_" + slot + "_N.png")), check_existing=True)
    normal.colorspace_settings.name = "Non-Color"
    metallic, smoothness = response(slot, pixels(colour), pixels(normal))
    image = bpy.data.images.new("Huang " + slot + " surface", SIZE, SIZE, alpha=True)
    image.colorspace_settings.name = "Non-Color"
    values = np.zeros((SIZE, SIZE, 4), np.float32)
    values[:, :, 0], values[:, :, 3] = metallic, smoothness
    image.pixels.foreach_set(values.ravel())
    image.filepath_raw = str(ART / ("Huang" + slot + "Surface.png"))
    image.file_format = "PNG"
    image.save()
    image.pack()
    material = bpy.data.materials.new("Huang " + slot + " refined")
    material.use_nodes = True
    nodes, links = material.node_tree.nodes, material.node_tree.links
    shader = next(node for node in nodes if node.type == "BSDF_PRINCIPLED")
    for texture, destination in [(colour, "Base Color"), (normal, None), (image, None)]:
        node = nodes.new("ShaderNodeTexImage")
        node.image = texture
        if destination:
            links.new(node.outputs["Color"], shader.inputs[destination])
        elif texture == normal:
            normal_node = nodes.new("ShaderNodeNormalMap")
            normal_node.inputs["Strength"].default_value = .8
            links.new(node.outputs["Color"], normal_node.inputs["Color"])
            links.new(normal_node.outputs["Normal"], shader.inputs["Normal"])
        else:
            channels = nodes.new("ShaderNodeSeparateColor")
            links.new(node.outputs["Color"], channels.inputs["Color"])
            links.new(channels.outputs["Red"], shader.inputs["Metallic"])
            inverse = nodes.new("ShaderNodeMath")
            inverse.operation = "SUBTRACT"
            inverse.inputs[0].default_value = 1
            links.new(node.outputs["Alpha"], inverse.inputs[1])
            links.new(inverse.outputs[0], shader.inputs["Roughness"])
    body.data.materials[index] = material
    refined[slot] = material
    summary.append({"slot": slot, "smoothness": np.quantile(smoothness, [.05, .5, .95]).tolist(),
                    "metallic": np.quantile(metallic, [.05, .5, .95]).tolist()})
if len(refined) != len(slots):
    raise RuntimeError("A Huang material source is missing")
EVIDENCE.mkdir(parents=True, exist_ok=True)
(EVIDENCE / "blender-surfaces.json").write_text(json.dumps(summary, indent=2))
print({"faces": len(body.data.polygons), "groups": len(body.vertex_groups), "surfaces": len(summary)}, flush=True)
for image in list(bpy.data.images):
    if image.users == 0:
        bpy.data.images.remove(image)
bpy.ops.file.pack_all()
WORK.mkdir(parents=True, exist_ok=True)
temporary = WORK / (uuid4().hex + ".blend")
bpy.ops.wm.save_as_mainfile(filepath=str(temporary), compress=True)
temporary.replace(WORK / "Huang Mei - skin brocade and lacquer.blend")
