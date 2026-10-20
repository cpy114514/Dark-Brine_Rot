# Whole-game performance v226 — October 19–20, 2026

This pass targets lower-spec Windows hardware while preserving visible coverage, materials, animation and effects. It modifies world rendering and shader work. Combat v003 and the opening's exact native contact work v225 are retained.

## Implemented

- Fixed grass registration during incremental cache construction. Previously, new members of an already-culled cell could retain independent rendering indefinitely: over 21,000 batchable renderers escaped the managed state. Registered batch members now immediately suppress their original draw.
- Preserved the distant hillside. Enforcing the former 110 m grass cutoff visibly removed grass, so that intermediate solution was rejected. Grass now retains its original full geometry, with normal per-camera frustum/clip rejection instead of a separate short draw distance. Tree LOD and shadow-distance policies are unchanged.
- Added conservative per-camera batch/instance visibility checks, including bounds for wind and body displacement. A distinct `GrassRenderSubmission` MonoScript has persisted execution order 31000 and runs after project camera movement, before native culling. Camera-specific draws support simultaneous views without submitting the field repeatedly to every camera.
- Rebuild grass state into a spare snapshot with bounded work, then atomically swap complete snapshots. Visible checks use a 1.5 ms slice; offscreen background checks use 0.35 ms. Readers retain the preceding complete field throughout the update. Cache construction blocks refreshes to prevent collection mutation across coroutine yields.
- Generated four render-only grass layouts. Float32 position, normal, UV0, submeshes, bounds, indices and winding are preserved exactly. Unused tangent/additional-UV streams are omitted only for the existing `Mavis/FoliageWind` shader. Major source strides are 80 → 32 bytes; the four selected vertex streams total 5,610,288 → 2,542,528 bytes. Original models/colliders remain intact. This is a vertex-fetch reduction, not a claim that total application RAM fell: originals remain referenced and complete grass snapshots use additional bounded cache memory.
- Added exact shader early-outs for zero-contribution cloud samples and out-of-footprint foliage interaction. Cloud octave counts, raymarch quality, wind shapes, body influence, colors and render scale remain unchanged.

## Verification

- `VerifyWorldShadersV226`: 18 cloud/weather/detail/view cases and 16 foliage/body-boundary cases; all 34 paired renders have zero pixel difference. The foliage fixture has subdivided blades so body interaction affects visible vertices.
- `BuildGrassRenderMeshesV226`: all four layouts match the source position/normal/UV arrays, triangle indices, material slots, topology and bounds exactly.
- `VerifyGrassRenderingV226`: 25,673 renderers, 12 paired normal-rendered views (shore, forest, panorama, eight turns and a second camera). All final views match the reference pixels exactly. Reference rendering uses the original full meshes individually. Unrelated tree LOD/cache changes are settled and held identical within each pair. This was rerun after extracting the submission driver into its own MonoScript and persisting its execution order.
- Lifecycle checks restore mesh, material, original render flags, shadows and occlusion for 25,795 entries. Reenable rebuilds the same count, with zero unsuppressed batch members.
- Final source compilation and runtime acceptance completed with zero console errors. Earlier diagnostic/tool failures are retained in `.codex/whole-game-performance-20261019/` and are not acceptance results.

## Measurement

Existing Unity 6000.3.23f1 Editor, i5-14400F / RTX 4060, 32 GB RAM, 2560×1440 Game View, PC quality, render scale 1. Frame cap/VSync are removed only during tests and restored afterward. Each steady window runs 4.5 seconds at a fixed camera. Screenshots use normal rendered frames, rather than manual `Camera.Render`, which can omit transient instanced submissions.

Live results, raw frames/counters, draw counts and limitations are in `.codex/whole-game-performance-20261019/comparison.json`. CPU allocation diagnostics and full Profiler captures are separated from the final normal-timing pass. A frozen-time A/B isolates changing cloud/wave phases; its timing is rendering evidence, not gameplay FPS.

| Live view | Before average ms (median of 3 runs) | After average ms | Before P95 ms | After P95 ms |
| --- | ---: | ---: | ---: | ---: |
| shore | 21.97 | 19.52 | 24.49 | 20.00 |
| forest | 11.90 | 11.87 | 12.50 | 12.58 |
| panorama | 8.55 | 8.49 | 9.61 | 9.55 |
| ocean | 4.73 | 4.77 | 5.63 | 5.51 |
| map | 3.73 | 3.82 | 4.43 | 4.49 |

| Frozen visual workload (simulation time 0) | Before GPU ms | After GPU ms |
| --- | ---: | ---: |
| shore | 20.07 | 18.61 |
| forest | 11.36 | 11.47 |
| panorama | 8.07 | 8.41 |
| ocean | 3.85 | 3.61 |

The gain is concentrated in shoreline submission overhead. Forest rendering remains dominated by tree geometry; this pass does not claim a large forest FPS increase. The forest/tree triangle budget is preserved. The open ocean and map are broadly unchanged in clean steady windows.

In the frozen A/B, shore draw submissions fall from approximately 10,943 to 863 and GPU time from 20.07 to 18.61 ms. Forest/panorama GPU timing does not improve (11.36 → 11.47 ms and 8.07 → 8.41 ms); do not advertise a uniform whole-game gain. Ocean GPU timing is 3.85 → 3.61 ms. Simulation time and time scale are both exactly zero in both frozen runs, keeping cloud/wave phases comparable. The subsequent driver-file extraction preserves that rendering workload; final normal-frame visual/lifecycle checks use the extracted driver.

Some Editor runs show large intermittent allocation/GC spikes in the ocean or after returning from the map. Simplified map-close reproduction and sampled allocation hierarchies did not establish a map/foliage runtime cause. This remains an unaccepted diagnostic finding, not a fixed-runtime claim; do not infer smooth release FPS from the selected steady windows. Raw rejected/diagnostic runs are retained.

## Preservation and limits

Only the existing Editor was used. No standalone Player/build, low-end physical-machine test, second Editor launch, commit, push or automation. User save was protected before Play and restored after each run; final hash is `3EA875D36D67463D847BDDB30494E75A689A349F1416A89AB60110931EE864A5`. Always back up newer human progress before future tests.

The dirty in-memory Story1 scene is preserved without save/reload/discard. No source animation/controller, shark scale, combat input/damage or contact geometry changes. Sources and Library backups, final source hashes, native reference images and diagnostics are under `.codex/whole-game-performance-20261019/`.

Opening-specific v225 spikes, cold shader/loading hitches, long-session behavior, arbitrary moving foliage/third-party camera integrations and actual lower-spec/release performance remain outside this acceptance. Profiling tools are under `Tools/` and are not included as runtime game scripts.
