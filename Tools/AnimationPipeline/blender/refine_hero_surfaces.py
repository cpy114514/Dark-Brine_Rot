"""Prepare URP surface maps on the existing Sahur, Cappuccino and Nailong models."""

from pathlib import Path
import bpy
import numpy as np


ROOT = Path.cwd()
ART = ROOT / "Assets/Game/Art/ModelRefinement"
WORK = ROOT / "ArtSource/ModelRefinement"
SIZE = 1024


def pixels(image):
    temp = image.copy()
    temp.scale(SIZE, SIZE)
    result = np.empty(SIZE * SIZE * 4, dtype=np.float32)
    temp.pixels.foreach_get(result)
    bpy.data.images.remove(temp)
    return result.reshape((SIZE, SIZE, 4))


def write_map(name, rgb, alpha=1.0, export=True):
    image = bpy.data.images.new(name, SIZE, SIZE, alpha=True)
    image.colorspace_settings.name = "Non-Color"
    values = np.ones((SIZE, SIZE, 4), dtype=np.float32)
    values[:, :, :3] = rgb
    values[:, :, 3] = alpha
    image.pixels.foreach_set(values.ravel())
    if export:
        image.filepath_raw = str(ART / (name + ".png"))
        image.file_format = "PNG"
        image.save()
    image.pack()
    return image


def smoothstep(lo, hi, value):
    t = np.clip((value - lo) / (hi - lo), 0, 1)
    return t * t * (3 - 2 * t)


def wire_image(material, image, destination, normal=False):
    shader = next(node for node in material.node_tree.nodes if node.type == "BSDF_PRINCIPLED")
    target = shader.inputs[destination]
    for link in list(target.links):
        material.node_tree.links.remove(link)
    source = material.node_tree.nodes.new("ShaderNodeTexImage")
    source.image = image
    if normal:
        bump = material.node_tree.nodes.new("ShaderNodeNormalMap")
        bump.inputs["Strength"].default_value = .52
        material.node_tree.links.new(source.outputs["Color"], bump.inputs["Color"])
        material.node_tree.links.new(bump.outputs["Normal"], target)
    else:
        material.node_tree.links.new(source.outputs["Color"], target)


def sahur():
    bpy.ops.wm.open_mainfile(filepath=str(ROOT / "ArtSource/Encounter/v247/sahur-stick-idle-v247.blend"), load_ui=False)
    bpy.context.preferences.filepaths.save_version = 0
    body = next(obj for obj in bpy.context.scene.objects if obj.type == "MESH" and len(obj.data.polygons) == 499949)
    source = bpy.data.images["texture_pbr_20250901_roughness.png"]
    rough = pixels(source)[:, :, 0]
    # Preserve the painted roughness contrasts, compress extreme specular spots,
    # and keep this dry wood entirely dielectric.
    colour = pixels(bpy.data.images["texture_pbr_20250901.png"])[:, :, :3]
    tint = colour.max(axis=2) - colour.min(axis=2)
    pale_glints = smoothstep(.45, .82, colour.min(axis=2)) * (1 - smoothstep(.05, .25, tint))
    smoothness = .14 + (1 - np.clip(rough, 0, 1)) * .25 + pale_glints * .23
    write_map("SahurWoodSmoothness", np.zeros((SIZE, SIZE, 3)), smoothness)
    preview = write_map("Calibrated wood roughness", (1 - smoothness)[:, :, None], export=False)
    wire_image(body.data.materials[0], preview, "Roughness")
    bsdf = next(node for node in body.data.materials[0].node_tree.nodes if node.type == "BSDF_PRINCIPLED")
    bsdf.inputs["Metallic"].default_value = 0
    print("Sahur source roughness range:", np.quantile(rough, [.05, .5, .95]).tolist())
    bpy.ops.wm.save_as_mainfile(filepath=str(WORK / "Sahur/Sahur - wood surface.blend"), compress=True)


def cappuccino():
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.context.preferences.filepaths.save_version = 0
    folder = ROOT / "Assets/Game/Prefabs/Bosses/CappuccinoAssassino"
    bpy.ops.import_scene.gltf(filepath=str(folder / "source/CappuccinoAssassino.glb"))
    body = next(obj for obj in bpy.context.scene.objects if obj.type == "MESH" and "Capuchino" in obj.name)
    material = body.data.materials[0]
    albedo = pixels(bpy.data.images.load(str(folder / "textures/Capuchino_baseColor.jpeg"), check_existing=True))
    r, g, b = np.moveaxis(albedo[:, :, :3], -1, 0)
    u, v = np.meshgrid(np.arange(SIZE) / SIZE, np.arange(SIZE) / SIZE)
    # The vertical cup strip is paper. The adjacent white rail is moulded
    # plastic, and the brown sleeve receives a pressed fibre weave.
    strip = 1 - smoothstep(.126, .136, u)
    sleeve = smoothstep(.22, .229, u) * (1 - smoothstep(.425, .434, u))
    coloured = smoothstep(.035, .14, r - b) * smoothstep(.08, .19, r)
    fabric = (1 - strip) * coloured
    fiber = np.sin(2 * np.pi * (u * 216 + .13 * np.sin(2 * np.pi * v * 31)))
    cross = np.sin(2 * np.pi * (v * 179 + .14 * np.sin(2 * np.pi * u * 15)))
    paper = np.sin(2 * np.pi * (v * 144 + u * 79)) * np.cos(2 * np.pi * (v * 78 - u * 199))
    height = .011 * strip * paper + (.011 * fabric + .019 * sleeve) * fiber * cross
    dx = (np.roll(height, -1, axis=1) - np.roll(height, 1, axis=1)) * 4.5
    dy = (np.roll(height, -1, axis=0) - np.roll(height, 1, axis=0)) * 4.5
    normal = np.stack([-dx, -dy, np.ones_like(dx)], axis=-1)
    normal /= np.linalg.norm(normal, axis=-1)[..., None]
    normal_map = write_map("CapriPaperAndSleeveNormal", normal * .5 + .5)
    wire_image(material, normal_map, "Normal", normal=True)
    bpy.ops.file.pack_all()
    target = WORK / "Cappuccino"
    target.mkdir(exist_ok=True)
    bpy.ops.wm.save_as_mainfile(filepath=str(target / "Capri - paper and sleeve.blend"), compress=True)
    print("Cappuccino body mesh", body.name, len(body.data.polygons), "faces")


def nailong():
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.context.preferences.filepaths.save_version = 0
    folder = ROOT / "Assets/Game/Prefabs/Bosses/Nailong"
    bpy.ops.import_scene.fbx(filepath=str(folder / "nailong.fbx"))
    body = max((obj for obj in bpy.context.scene.objects if obj.type == "MESH"), key=lambda obj: len(obj.data.polygons))
    maps = folder / "Textures"
    colour_image = bpy.data.images.load(str(maps / "texture_pbr_20250901.png"), check_existing=True)
    rough_image = bpy.data.images.load(str(maps / "texture_pbr_20250901_roughness.png"), check_existing=True)
    rough_image.colorspace_settings.name = "Non-Color"
    r, g, b = np.moveaxis(pixels(colour_image)[:, :, :3], -1, 0)
    roughness = pixels(rough_image)[:, :, 0]
    # Yellow body and teal irises receive a smoother response. The neutral
    # garment stays broad and matte, so the existing seams read in close-ups.
    yellow = smoothstep(.12, .32, r - b) * smoothstep(.10, .25, g - b)
    iris = smoothstep(.06, .26, np.minimum(g, b) - r)
    smoothness = .17 + (1 - roughness) * .12 + yellow * .21 + iris * .24
    write_map("NailongClothAndSkinSmoothness", np.zeros((SIZE, SIZE, 3)), smoothness)
    preview = write_map("NailongRoughnessPreview", (1 - smoothness)[:, :, None], export=False)
    surface = bpy.data.materials.new("Nailong - satin skin and matte garment")
    surface.use_nodes = True
    wire_image(surface, colour_image, "Base Color")
    wire_image(surface, preview, "Roughness")
    normal = bpy.data.images.load(str(maps / "texture_pbr_20250901_normal.png"), check_existing=True)
    normal.colorspace_settings.name = "Non-Color"
    wire_image(surface, normal, "Normal", normal=True)
    body.data.materials.clear()
    body.data.materials.append(surface)
    bpy.ops.file.pack_all()
    target = WORK / "Nailong"
    target.mkdir(exist_ok=True)
    bpy.ops.wm.save_as_mainfile(filepath=str(target / "Nailong - cloth and skin.blend"), compress=True)
    print("Nailong:", len(body.data.polygons), "faces with source skinning retained in the authoring copy")


sahur()
cappuccino()
nailong()
