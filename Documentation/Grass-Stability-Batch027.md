# Grass LOD stability — batch027

The user's moving-camera grass report was reproduced in the paused live first island. Its added grass cover has 11,298 native LODGroups, each switching between 23,097 / 7,799 / 1,948 triangles and then culling. All used `fadeMode=None`, zero transition widths. The low mesh removes whole cards, so crossing a threshold abruptly changes density. Camera following/orbiting can cross the same thresholds as walking. These patches have two materials and are not the manager's manually instanced original-grass path.

## Correction

`WorldPerformanceManager` gives only the recognized `VerifiedGrass*` native patch family distance-based CrossFade with width0.2. Existing meshes, thresholds, coverage, wind, density, tree settings, quality and original scene serialization stay intact. Authored fades and unsupported meshes are left alone. Ownership restores the original mode/widths on disable, recache and actual assembly reload, including an Editor-only handoff of original IDs/widths. No new per-frame scan was added.

`MavisFoliageWind.shader` implements URP's native fade in ForwardLit, DepthOnly and ShadowCaster. Both shader stages compile the LOD variant, and the instance ID reaches the fragment shader. A fragment-only variant candidate failed the native image test and was rejected; crossfade settings alone do not fix a shader without the appropriate variant. The existing pipeline already enables LOD crossfade and uses a fixed blue-noise texture.

This removes the hard whole-patch switch; it does not make natural wind, perspective, silhouettes or distant optimization visually identical at every distance. Dither is visible during a blend, and two adjacent levels can draw temporarily. No FPS improvement is claimed. The separate35m batched legacy mesh-detail path was not the observed patch family and is unchanged.

## Native evidence

Evidence: `AUTO_QA/Evidence/batch027/proof.json`, PNGs, JSON records and native-frame video encodings. Private snapshots/raw frames/build are in `.codex/grass027`; do not overwrite them.

| Check | Accepted result |
|---|---|
| Same-camera/same-projection isolated patch |39 samples per case,13 per threshold; frozen wind and visible-empty positive control. Initial visible pixels24,546. |
| Paired maximum adjacent-image RGB-channel delta |1.8240625→0.17062886,90.65% lower. This metric belongs to this frozen probe, not the entire game or FPS. Near/mid/cull jumps are spread across a distance band. |
| Production-policy probe |Actual automatically configured patch: visible-empty23,193 pixels, maximum0.17960841. Different isolated lighting/template; not another paired percentage. |
| Frozen stability |0 changed pixels during a20-frame hold, after ±18° rotation and return, and after ±4m dolly and return. Same camera/projection/material wind freeze. |
| Live scene pilot |Before/candidate48 positions each,286 render callbacks in total, camera travel±18m and yaw±22°. Original user player never moved. |
| Production first island |71,315 foliage entries; all11,298 recognized groups automatically use the new fade. Environment+Lighting isolated from savable Gameplay. |
| Four styles |Realistic,Dark Fairytale,Origami,Monochrome:48 positions each,192 native captures/576 render callbacks; representative images inspected. Frozen scaled time; not a continuous player-input or FPS benchmark. |
| Pixel mode |Actual2560×1440 capture, runtime scale0.1250007/180-high mode, active grass CrossFade; pixel/render scale released/restored. No preference write requested. |
| Ownership regressions |60 final native assertions:25 LOD scope/reload/lifecycle +12 core actual reload +12 lifecycle +6 additive scene +5 partial cancellation. Preparation9/24 assertions overlap and are not added. Cancellation observed at351 entries. Unsupported meshes/authored fades preserved. |
| Compilation/build |Shader diagnostics0errors. Same Editor57472,7 enabled scenes, Windows64 Mono Development build Succeeded149.97s.493warnings/4 pre-existing scene/missing-prefab messages account for the known606 missing plants; no new C#/shader error. Player was not launched. |

Videos are12fps encodings of discrete native screenshots, not measured game frame rate. Whole-scene roundtrip images have small nonzero differences; those images do not isolate wind/clouds/shadows/post effects/bounded refresh, so they are not evidence of pixel-perfect whole-scene invariance. Only the controlled frozen patch qualifies that claim.

## Safety and restoration

The initial Editor was already running the user's paused game at timescale0. Fresh private saves/live camera/player/scene data were recorded before probes. Probes restore their cameras, runtime render asset/pixel and timescale in finally blocks. The user's session then stopped normally, preserving its newly saved progress. All subsequent tests retain that post-user-stop save and backup byte-for-byte; older backups are not restored over it.

Original five scenes are clean/stopped, all11,298 authored grass modes/widths are back to None/0, owned fixtures0. No scene/model/UV/rig/animation/collision or material asset change enters this checkpoint. Build flags/preloads/ProjectSettings bytes restored. The build filled a legacy `_MainTex` alias in the otherwise unchanged duck material; a guarded exact-field rollback restored its pre-build Git-clean state after a native setter was re-populated by validation. Unrelated Plastic checkouts stay out of scope. Style selection used `persist=false`; no full registry snapshot was made, so full preference byte equivalence is not claimed.

Tools: `Tools/Grass027.cs` for native probes and ownership fixtures; `Tools/BuildGrass027.cs` for a guarded unique build. One Play-transition connection interruption and one scene-restore timeout were checked against actual native state before continuation. No second project Editor or duplicate fixture was started.

Checkpoint title: **Smooth native grass LOD transitions during camera movement**. Baseline Git ee5d23d / Plastic cs211; resolve the actual new commit/checkin before further sync. Next: user-path grass appearance at additional terrain/forest locations if new evidence requires it, then actual enlarged mixed flock/UI/Boss/tutorial coverage. Do not rerun unchanged timing/build matrices or claim all rendering differences are eliminated.
