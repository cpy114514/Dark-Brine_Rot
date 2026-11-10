"""Refine the material separation of the existing chicken and goose skins."""

from pathlib import Path
import json
import uuid

import bpy
import numpy as np


ROOT = Path.cwd()
ENEMIES = ROOT / "Assets/Game/Prefabs/Enemies"
ART = ROOT / "Assets/Game/Art/ModelRefinement"
WORK = ROOT / "ArtSource/ModelRefinement"
EVIDENCE = ROOT / ".codex/model-refinement/flock"
SIZE = 1024


def read_pixels(image, size=SIZE):
    source = image
    if tuple(image.size) != (size, size):
        source = image.copy()
        source.scale(size, size)
    data = np.empty(size * size * 4, np.float32)
    source.pixels.foreach_get(data)
    if source != image:
        bpy.data.images.remove(source)
    return data.reshape(size, size, 4)


def image_from_values(name, red, green, blue, alpha=1, path=None, size=SIZE, colour=False):
    image = bpy.data.images.new(name, size, size, alpha=not colour)
    image.colorspace_settings.name = "sRGB" if colour else "Non-Color"
    data = np.empty((size, size, 4), np.float32)
    data[:, :, 0] = red
    data[:, :, 1] = green
    data[:, :, 2] = blue
    data[:, :, 3] = alpha
    image.pixels.foreach_set(data.ravel())
    if path:
        image.filepath_raw = str(path)
        image.file_format = "PNG"
        image.save()
    image.pack()
    return image


def ramp(lo, hi, value):
    x = np.clip((value - lo) / (hi - lo), 0, 1)
    return x * x * (3 - 2 * x)


def position_bake(body):
    material = bpy.data.materials.new("Position mask bake")
    material.use_nodes = True
    nodes = material.node_tree.nodes
    output = next(node for node in nodes if node.type == "OUTPUT_MATERIAL")
    emission = nodes.new("ShaderNodeEmission")
    position = nodes.new("ShaderNodeTexCoord")
    material.node_tree.links.new(position.outputs["Generated"], emission.inputs["Color"])
    material.node_tree.links.new(emission.outputs["Emission"], output.inputs["Surface"])
    image = bpy.data.images.new("Rest position on the source atlas", SIZE, SIZE, alpha=True)
    image.colorspace_settings.name = "Non-Color"
    target = nodes.new("ShaderNodeTexImage")
    target.image = image
    nodes.active = target
    body.data.materials.clear()
    body.data.materials.append(material)
    bpy.ops.object.select_all(action="DESELECT")
    body.select_set(True)
    bpy.context.view_layer.objects.active = body
    scene = bpy.context.scene
    scene.render.engine = "CYCLES"
    scene.cycles.samples = 1
    bpy.ops.object.bake(type="EMIT", margin=12)
    return image


def chicken_response(colour, rough, position):
    r, g, b = np.moveaxis(colour[:, :, :3], -1, 0)
    height = position[:, :, 2]
    chroma = np.maximum(r, g) - np.minimum(g, b)
    pale = ramp(.28, .64, np.minimum(np.minimum(r, g), b)) * (1 - ramp(.04, .18, chroma))
    hair = pale * ramp(.62, .84, height)
    black = 1 - ramp(.035, .15, np.maximum(np.maximum(r, g), b))
    boot = black * (1 - ramp(.14, .24, height))
    face = ramp(.14, .46, chroma) * ramp(.50, .65, height)
    eye = pale * ramp(.59, .65, height) * (1 - ramp(.74, .82, height)) * (1 - hair)
    # The white trousers and black top read as fabric, with independent
    # leather boots, silver hair and the saturated cheek and beak surfaces.
    u, v = np.meshgrid(np.arange(SIZE) / SIZE, np.arange(SIZE) / SIZE)
    weave = np.sin(u * np.pi * 366) * np.sin(v * np.pi * 341)
    cloth = .125 + .09 * (1 - rough) + weave * .025 * (1 - np.maximum(hair, face))
    smoothness = cloth + hair * .27 + boot * .19 + face * .18 + eye * .23
    metallic = hair * .035
    tint = (hair * .80 + pale * .16)[:, :, None] * np.array([-.105, -.087, -.056], np.float32)
    return metallic, np.clip(smoothness, .10, .60), tint


def goose_response(colour, rough, position):
    r, g, b = np.moveaxis(colour[:, :, :3], -1, 0)
    height = position[:, :, 2]
    amber = ramp(.08, .33, r - b) * ramp(.10, .34, r - g) * ramp(.22, .48, r)
    beak = amber * ramp(.63, .82, height)
    feet = amber * (1 - ramp(.12, .23, height))
    dark = 1 - ramp(.08, .19, np.maximum(np.maximum(r, g), b))
    iris = dark * ramp(.86, .94, height)
    fine_down = ramp(.48, .69, height) * (1 - ramp(.79, .9, height))
    # Lofty chest and wing feather shafts keep the broader sheen. The
    # tighter neck down is drier, and the feet read rougher than the bill.
    smoothness = .16 + .16 * (1 - rough) - fine_down * .064 + beak * .24 + feet * .084 + iris * .25
    white = (1 - ramp(.08, .22, r - b)) * ramp(.22, .68, np.minimum(np.minimum(r, g), b))
    cream = white * (1 - .36 * fine_down) * (.78 + rough * .22)
    tint = cream[:, :, None] * np.array([-.036, -.044, -.057], np.float32)
    return np.zeros_like(smoothness), np.clip(smoothness, .11, .64), tint


def author(name, albedo, response):
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.context.preferences.filepaths.save_version = 0
    source = ENEMIES / name
    bpy.ops.import_scene.fbx(filepath=str(source / (name + ".fbx")))
    body = max((ob for ob in bpy.context.scene.objects if ob.type == "MESH"), key=lambda ob: len(ob.data.polygons))
    position = position_bake(body)
    textures = source / "Textures"
    color_image = bpy.data.images.load(str(textures / albedo), check_existing=True)
    rough_image = bpy.data.images.load(str(textures / "texture_pbr_20250901_roughness.png"), check_existing=True)
    rough_image.colorspace_settings.name = "Non-Color"
    normal_image = bpy.data.images.load(str(textures / "texture_pbr_20250901_normal.png"), check_existing=True)
    normal_image.colorspace_settings.name = "Non-Color"
    position_values = read_pixels(position)
    if position_values[:, :, 2].max() < .95:
        raise RuntimeError("Missing vertical range in the baked position atlas")
    metallic, smoothness, tint = response(read_pixels(color_image), read_pixels(rough_image)[:, :, 0], position_values)
    packed = image_from_values(name + "Surface", metallic, 0, 0, smoothness, ART / (name + "Surface.png"))
    preview = image_from_values("Adjusted roughness", 1 - smoothness, 1 - smoothness, 1 - smoothness)
    tint_image = image_from_values("Tint weight", *(np.moveaxis(-tint, -1, 0)))
    native_colour = read_pixels(color_image, 2048)[:, :, :3]
    native_colour *= 1 - read_pixels(tint_image, 2048)[:, :, :3]
    refined_colour = image_from_values(name + "Colour", *np.moveaxis(native_colour, -1, 0),
                                       path=ART / (name + "Colour.png"), size=2048, colour=True)
    bpy.data.images.remove(tint_image)
    material = bpy.data.materials.new(name + " refined surfaces")
    material.use_nodes = True
    nodes = material.node_tree.nodes
    links = material.node_tree.links
    shader = next(node for node in nodes if node.type == "BSDF_PRINCIPLED")
    for image, channel in ((refined_colour, "Base Color"), (preview, "Roughness")):
        node = nodes.new("ShaderNodeTexImage")
        node.image = image
        links.new(node.outputs["Color"], shader.inputs[channel])
    node = nodes.new("ShaderNodeTexImage")
    node.image = normal_image
    normal = nodes.new("ShaderNodeNormalMap")
    links.new(node.outputs["Color"], normal.inputs["Color"])
    links.new(normal.outputs["Normal"], shader.inputs["Normal"])
    packed_node = nodes.new("ShaderNodeTexImage")
    packed_node.image = packed
    packed_node.label = "Unity URP metallic and smoothness"
    body.data.materials.clear()
    body.data.materials.append(material)
    bpy.data.images.remove(position)
    bpy.ops.file.pack_all()
    bpy.context.scene.render.engine = "BLENDER_EEVEE"
    work = WORK / name
    work.mkdir(parents=True, exist_ok=True)
    temporary = work / (uuid.uuid4().hex + ".blend")
    bpy.ops.wm.save_as_mainfile(filepath=str(temporary), compress=True)
    temporary.replace(work / (name + " - surface study.blend"))
    result = {"model": name, "faces": len(body.data.polygons), "sourceWeights": len(body.vertex_groups),
              "mapResolution": SIZE, "smoothnessRange": np.quantile(smoothness, [.05, .5, .95]).tolist()}
    print(result, flush=True)
    return result


EVIDENCE.mkdir(parents=True, exist_ok=True)
stats = [
    author("ChickenBro", "texture_pbr_20250901_albedo.png", chicken_response),
    author("BullyGoose", "texture_pbr_20250901.png", goose_response),
]
(EVIDENCE / "blender-surfaces.json").write_text(json.dumps(stats, indent=2))
