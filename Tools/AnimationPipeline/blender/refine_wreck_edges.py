"""Correct the wood grain and lighting on existing Blender-cut wreck edges."""

import json
from pathlib import Path

import bpy
import numpy as np


ROOT = Path.cwd()
SOURCE = ROOT / "ArtSource/ShipWreck/ship_g_fractured_v002.blend"
GEOMETRY = ROOT / ".codex/ship-wreck-blender-v002/fragments.json"
EVIDENCE = ROOT / ".codex/model-refinement/wreck"
WORK = ROOT / "ArtSource/ModelRefinement/ShipWreck"


def repair(piece):
    mesh = bpy.data.objects[piece["name"]].data
    indices = np.asarray(piece["triangles"], dtype=np.int32)
    uv = np.asarray(piece["uv"], dtype=np.float32).reshape((-1, 2))
    source_uv = np.empty_like(uv)
    layer = mesh.uv_layers.active.uv
    layer.foreach_get("vector", source_uv.ravel())
    if not np.array_equal(uv, source_uv) or not np.array_equal(indices, np.arange(len(uv))):
        raise ValueError("Blender loop order differs from the production export: " + piece["name"])
    points = np.empty((len(mesh.vertices), 3), dtype=np.float32)
    mesh.vertices.foreach_get("co", points.ravel())
    loop_vertices = np.empty(len(mesh.loops), dtype=np.int32)
    mesh.loops.foreach_get("vertex_index", loop_vertices)
    xyz = points[loop_vertices].reshape(-1, 3, 3)
    physical_normal = np.cross(xyz[:, 1] - xyz[:, 0], xyz[:, 2] - xyz[:, 0])
    area = np.linalg.norm(physical_normal, axis=1)
    uv_tri = uv.reshape(-1, 3, 2)
    a = uv_tri[:, 1] - uv_tri[:, 0]
    b = uv_tri[:, 2] - uv_tri[:, 0]
    uv_area = np.abs(a[:, 0] * b[:, 1] - a[:, 1] * b[:, 0])
    # The previous bisect tool projected every cap in X/Z. At a bow-facing
    # cut X is constant, so that projection loses all grain width. The deck
    # solidify operation also leaves narrow rim faces with collapsed UVs.
    prior_cap = np.max(np.abs(uv_tri - xyz[:, :, [0, 2]] * .12), axis=(1, 2)) < 2e-5
    side = np.abs(physical_normal[:, 2]) < area * .82
    selected = (area > 1e-7) & side & (prior_cap | (uv_area < 1e-10))
    changed = np.flatnonzero(selected)
    if not len(changed):
        return None
    updated_uv = uv_tri.copy()
    for face_index in changed:
        x, y, z = xyz[face_index].T
        nx, ny, _ = physical_normal[face_index]
        if abs(nx) >= abs(ny):
            # Grain on a transverse break runs across the plank thickness.
            updated_uv[face_index] = np.stack((y * .28 + z * .08, z * .31 + y * .025 + .39), axis=-1)
        else:
            updated_uv[face_index] = np.stack((x * .16, z * .46 + y * .023 + .22), axis=-1)
        mesh.polygons[face_index].use_smooth = False
    ua = updated_uv[changed, 1] - updated_uv[changed, 0]
    ub = updated_uv[changed, 2] - updated_uv[changed, 0]
    if np.any(np.abs(ua[:, 0] * ub[:, 1] - ua[:, 1] * ub[:, 0]) < 1e-10):
        raise ValueError("A corrected cut face has collapsed UVs: " + piece["name"])
    layer.foreach_set("vector", updated_uv.ravel())
    mesh.update()

    native = np.asarray(piece["vertices"], dtype=np.float32).reshape(-1, 3, 3)
    native_normal = np.cross(native[:, 1] - native[:, 0], native[:, 2] - native[:, 0])
    length = np.linalg.norm(native_normal, axis=1)
    old = np.asarray(piece["normals"], dtype=np.float32).reshape(-1, 3, 3)
    aligned = (native_normal * old.mean(axis=1)).sum(axis=1)
    orientation = 1 if np.count_nonzero(aligned > 0) > np.count_nonzero(aligned < 0) else -1
    normals = old.copy()
    flat = native_normal[changed] / length[changed, None] * orientation
    normals[changed] = flat[:, None, :]
    return {
        "name": piece["name"],
        "vertices": piece["vertices"],
        "triangles": piece["triangles"],
        "originalUv": piece["uv"],
        "originalNormals": piece["normals"],
        "uv": updated_uv.ravel().tolist(),
        "normals": normals.ravel().tolist(),
        "cutFaces": changed.tolist(),
        "collapsedUvFaces": int(np.count_nonzero(selected & (uv_area < 1e-10))),
    }


bpy.ops.wm.open_mainfile(filepath=str(SOURCE), load_ui=False)
bpy.context.preferences.filepaths.save_version = 0
pieces = json.loads(GEOMETRY.read_text())["pieces"]
updates = []
for parent in pieces:
    if parent["kind"] in {"deck", "hull"}:
        for piece in [parent, *parent["children"]]:
            item = repair(piece)
            if item:
                updates.append(item)
if not updates or not all(np.isfinite(p["normals"]).all() for p in updates):
    raise ValueError("Invalid wreck shading output")
EVIDENCE.mkdir(parents=True, exist_ok=True)
WORK.mkdir(parents=True, exist_ok=True)
(EVIDENCE / "edge-surfaces.json").write_text(json.dumps(updates, separators=(",", ":")))
summary = {
    "meshes": len(updates),
    "faces": sum(len(p["cutFaces"]) for p in updates),
    "collapsedUvFaces": sum(p["collapsedUvFaces"] for p in updates),
    "baseMaterials": ["acmat_7", "acmat_2"],
}
(EVIDENCE / "blender-edges.json").write_text(json.dumps(summary, indent=2))
print(summary, flush=True)
bpy.ops.wm.save_as_mainfile(filepath=str(WORK / "Shipwreck - cut wood edges.blend"), compress=True)
