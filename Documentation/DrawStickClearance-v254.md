# Post-climb stick retrieval clearance — v254

## Scope and cause

Target: the existing shark film's swim → climb onto the board → catch a breath → retrieve the stick transition. Preserve the original v214 action, character/weapon mesh, grip, storyboard, camera and gameplay timing. No FBX, rig, prefab, scene, damage or combat windows are changed.

The previous stow blend interpolated the imported weapon object's origin and its rotation independently. That origin is far from the gripping end; the resulting long shaft swept through the animated torso while returning from the back to the right hand. Moving the handoff earlier with the old interpolation did not solve it.

`SahurSwimmingWeapon` now caches the fitted shaft's gripping end at initialization. It transports this end rather than the remote object origin, carries the shaft through a shoulder-side and forward clearance arc, then restores the exact original local hand transform. A small configurable back clearance keeps the stowed shaft outside the skin. The physical shaft direction is interpolated explicitly so quaternion roll cannot bow the long end inward. Side/forward clearance remains until that end passes the body.

The existing complete arm/body animation still runs; there is no replacement made by rotating character bones. Collider disabling/restoration retains its original ownership. New production work is vector/quaternion arithmetic during the existing transition; no per-frame mesh baking, searches, physics queries, new objects or allocations are added. The expensive mesh measurements below exist only in external Editor review tools.

## Review evidence

Evidence is retained in `.codex/draw-retrieve-v254/`. `ReviewRetrieveV254.cs` samples the original Humanoid action at 60 Hz on a temporary prefab instance inside the existing Editor. It uses the fitted weapon capsule against the actual posed skin triangles assigned primarily to torso/head/upper legs; normal grasping-arm contact is excluded. Native mesh baking compensates renderer scale before applying its world matrix. Earlier diagnostic takes which applied that scale twice, or hid the weapon renderer, are rejected and do not establish acceptance.

- `baseline-correct-scale`: 23 of 73 samples intersect, deepest surface gap approximately −0.135 m.
- `grip-pivot-correct-scale`: pivot correction alone still intersects in 20 samples.
- `early-correct-scale`, `shoulder-arc`, `axis-arc`, `outboard-handoff`, `forward-arc`: rejected alternatives with remaining intersections.
- `staged-arc`: 73 samples, zero intersections (−0.005 m tolerance), minimum measured core-body clearance 0.1228 m. Three native views are retained at each sixth frame.

This is targeted clearance evidence, not a certification of every animation, outfit or object contact in the game. The original v253 tutorial, healing and checkpoint changes are retained byte-for-byte; their initial hashes are recorded in `previous-work-hashes.json`. No check-in, Git commit, push or build is performed for this revision.

## Actual story replay and preservation

`native-take-05/result.json` passes in the existing Editor, using actual keyboard/mouse input through both tutorial strikes, the walking/swimming gates, pursuit and climb, retrieval, and the subsequent charged counter. The story camera records 215 frames at a fixed 30 Hz (7.17 seconds); the native review sheet and `climb-and-draw-v254-native-take-05.mp4` retain the climb/breath/draw sequence.

The 36 measured native retrieval samples compare the old and corrected weapon transforms against the same final posed body. The old interpolation intersects in 11 samples (minimum −0.135 m); the corrected path intersects in zero (minimum +0.04824 m). At Duel time 2.8667 s the stow component has released ownership, with original local grip rotation and a 0.00000721 m local-position difference. Both real tutorial strikes and the charged shark counter confirm their hits; the player remains alive at 88 health. Native camera frames were reviewed for the outboard shaft sweep and transition into the counter.

Review tooling failures are retained, not counted as passes. Takes 01–03 could not measure the optimized runtime finger mesh because its CPU read access is disabled. `ReadDrawMeshV254.Prepare` now reads metadata from a temporary readable clone in Edit Mode, destroys that clone, and leaves the original mesh untouched. Runtime measurements still bake the actual rendered finger skin. Take 04 clears the body and confirms all hits but fails an overly strict initial-grip equality check at the later counter endpoint. Take 05 checks the actual retrieval handoff instead; its later endpoint difference is only 0.00001766 m. Some Pipeline requests timed out during reload/cold compilation; retained result files establish what actually executed.

`acceptance.json` confirms all 35 previously pending files are unchanged. The human save is restored byte-for-byte after Play stops, the original campaign preferences are restored, and all five original island scenes are reopened clean. `final-editor.json` records that state. The shipping change is confined to the existing stow/return implementation; review tools are outside Assets and do not enter the Player.
