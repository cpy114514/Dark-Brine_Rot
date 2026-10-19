# Camera clearance source review — v004

2026-10-18 Vancouver. Read-only review of `Story1WreckBattle.Camera.cs`, `RedShot`, the shared wreck query matrices, current Unity documentation, and the integration owner's retained native probe. No Unity/Blender calls or runtime changes by this reviewer. Stage remains BLOCKING.

## Enabled query correction: confirmed

The earlier lens sphere had `enabled=false` during `Physics.ComputePenetration`. The integration owner ran `Tools/AuditCameraLensV217.cs` in the original Unity6000.3.23f1 Editor, port7800. Retained `.codex/encounter-redesign-20261017/camera-lens-v217.json` records disabled query **false, penetration0**; enabled query **true, penetration.549999952m**. The disabled sphere could therefore silently fail to repel the camera in this actual installed version. Unity's current6000.3 example explicitly requires both colliders enabled: [official ComputePenetration reference](https://docs.unity.com/en-us/engine/6000.3/script-reference/unityengine/physics/computepenetration).

Current source enables the hidden sphere during the query and disables it in `finally`; its physical object remains far below the scene. This addresses the measured API precondition and preserves cleanup. The method accepts explicit poses without physically moving the referenced scene colliders, as documented in the same official reference.

## Visual/physics transforms: consistent

Let `V` be the fragment's rendered root matrix and `P` its authoritative rigidbody matrix. The implementation uses `M=P*inverse(V)`:

- Sphere centre becomes `M*at`.
- Collider child position becomes `M*part.transform.position`; child rotation becomes `M.rotation*part.transform.rotation`.
- The separating vector returns through `inverse(M).MultiplyVector(direction*depth)`.

These transformations keep both tested shapes in the same physics pose and rotate the resulting displacement back into the rendered pose. A sphere needs no orientation change. Root dimensions can be nonuniform: they cancel in `P*inverse(V)` because both roots retain the same scale and the debris parent has no additional shear. This matches `RedCameraInsideWood`'s rendered-point → authoritative `ClosestPoint` → rendered-point convention. No double transformation was found. Collider `ClosestPoint` intentionally reads the authoritative collider pose; its enabled/active checks remain necessary. [Official ClosestPoint reference](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/Collider.ClosestPoint.html).

## Near-plane bound and FOV order

For vertical half-FOV tangent`t`, aspect`a` and near distance`n`, the distance from the lens to a near-plane corner is `n*sqrt(1+t*t*(1+a*a))`. The new sphere radius covers that corner distance, plus.12m, with a minimum.45m. Thus it conservatively encloses the near rectangle for the ordinary perspective projection in this scene. It is intentionally larger than the old fixed.35m lens check. `RedShot` now assigns the current shot FOV **before** clearance calculation, correcting the previous cut-frame use of the old shot's field of view.

## Native visual verification still required

The five-pass displacement is bounded iteration, not a mathematical guarantee that every narrow compound corner is solved. The final `RedesignCameraLensInsideWood` flag should remain false in the actual take; fallback staging is required if residual overlaps occur. The older `RedesignCameraWoodObstruction=0` uses a smaller.35m endpoint check and alone does not certify the new near-plane bound. The integration owner confirmed that sixteenth records the new flag separately in `clearance.json`; read-only parsing verifies all1335 flags are false. This complements the source review, although it does not measure camera-to-subject visibility or skin clearance.

`sixteenth-rigging-15` reached Finished with three contacts/HP70, but115 rigging exceptions omitted shot2/3 updates. The integration owner rejects its opening as a failed take. Independent inspection of its shot0/25/34 remains useful only for those captured intervals. No clipping-free or FINAL conclusion follows from source review or the isolated sphere/box probe.

## Related water-clearance query: invalidated assumption

At this checkpoint `Story1WreckBattle.Water.cs` also creates `redWaterQuery` disabled and passes it to `Physics.ComputePenetration` without enabling it. The same documented precondition applies. Prior zero predicted/drift costs therefore cannot certify that the geometry query actually ran; this needs the equivalent capsule probe and enable/disable correction before recording. Visible improvements in previous PNGs remain evidence for those images, but the all-zero costs must not be used as independent collision-free validation. Parent has been notified immediately. Do not silently carry those earlier numeric passes into final acceptance.

Subsequent integration-owner update: the water query has now received the enable/disable `try/finally` correction and its object is positioned outside the play area. Historical all-zero costs remain invalidated; the corrected implementation needs its own actual take.

## Sixteenth partial visual review

- Shot0 f164 (clock1.067) shows the underwater warning silhouette to the ship's lower-right. Its polygon/alpha boundary remains hard and reads as a flattened mesh; this is a polish issue, not sufficient reason to change original shark size.
- Shot25 f806–811 (clock32.767–33.100) records wood-obstruction5 at the beginning of the grab. Nearby foreground timber/waves hide shoulders and hands around the corresponding f804/815 sheet samples; the subsequent f825/836 pull and final standing face are readable. The new immediate lens clearance therefore succeeds at keeping the near plane outside timber but does not by itself solve that line-of-sight interval.
- Shot34 f1039–1055 shows no recurrence of the previous huge timber panel over the lens, but the large animal still occludes prone/back spacing. A subsequently authored earlier outboard bank and delayed yaw are pending their next recording and cannot be judged from these old path frames.
- Shot2/3 are absent after rigging exceptions. The failed take cannot certify ship/leap contact, even though later stages completed.

Evidence: `.codex/encounter-redesign-20261017/sixteenth-rigging-15/shot-0.jpg`, `shot-25.jpg`, `shot-34.jpg`, `frames.json`, and separately parsed `clearance.json` (1335 rows, zero true lens-inside flags). Scope is these inspected views and metrics; it does not establish whole-body or weapon penetration freedom.
