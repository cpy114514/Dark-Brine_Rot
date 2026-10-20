# Opening animation performance — v225, 2026-10-19

The user reports severe animation stutter. This pass measures and reduces CPU work in the existing Sahur/shark opening. It preserves authored clips, poses, camera policy, original shark scale (3,3,3), real native contacts/fracture, controls, nonlethal tutorial and current combat work. It changes no scene, prefab, animation, controller, rendering quality, Packages or PlayerSettings asset.

## Measured result

Existing Unity6000.3.23f1 Editor/7800 only. Baseline and final runs use30Hz deterministic simulation, VSync/frame cap temporarily disabled, no image capture and no independent geometry observation in measured phases. ProfilerRecorders and the existing solid-pass stopwatch identify a CPU script bottleneck; GPU/release FPS are not accepted by these traces.

| Measured scope | Before maximum | Final maximum |
| --- | ---: | ---: |
| Tail-hit fall/exit preparation | 404.07ms | 10.80ms |
| Tail skin contact | 189.32ms in the intermediate cold take | 3.17ms |
| Native prone/climb support | 132.64ms | 33.41ms |
| Pursuit native solid pass P95 | 122.91ms | 47.60ms |
| Duel native solid pass P95 | 44.08ms | 31.15ms |
| Duel Main Thread P95 | 106.87ms | 48.22ms |

The400ms exit preparation was confirmed by its dedicated marker, then moved into bounded preparation and reused. The tail index's cold construction was distributed before impact. Baseline pursuit averaged25.67ms in NativeSolids versus41.45ms Main Thread and1.22ms Physics.Simulate. Raw comparisons are `profile-comparison.json` plus `before-30/`, `instrumented-before-30/`, the retained intermediate takes and `final-after-30/` under `.codex/animation-performance-20261019/`.

Full-frame performance is **not universally accepted**: final no-image Editor Main Thread maxima still reach278.59ms in Crush and209.71ms in Pursuit; final native-solids maximum105.75ms in Pursuit. PhysX layouts, time origin, startup/render/Editor work and cache warming vary between takes. Some intermediate medians were worse and are retained. Do not interpret the30Hz stepping, encoded video smoothness, isolated marker reductions or capture timings as a delivered30/60FPS guarantee. Windows-player/GPU/GC budgets still need a separate permitted measurement. No second Editor or Player was launched.

## Implementation

- Reuse one post-crush local-space shark index for hero and wood queries, keyed to the actual baked frame and bone-pose version. Reuse the prewarmed topology. Bone changes invalidate it; root fitting retains valid local geometry.
- Exact full refits run bottom-up. Per-pose node/triangle query caches tighten visited small regions against the actual current vertices. Point/sphere queries avoid the general segment-distance work. Signed containment and existing tolerances remain.
- Repeat wood queries are memoized only within the same frame, exact relative matrix/offset, posed-index version and index instance. Same-frame translation/rotation still invalidates them.
- Native support rays consume one synchronous render/physics geometry snapshot. A conservative upper projection of the real compound rejects vertices that cannot increase lift. Native ray intersections and interpolated render-space hit mapping remain authoritative.
- Water prediction reuses native geometry snapshots and forecast transforms for each repeated future time. Epoch changes invalidate all forecasts. Nonnegative score bounds prune losing candidates and a zero global score stops the complete search, preserving the original candidate/tie order.
- The approved fall pose's279,411 native vertices are measured in1.5ms slices before the tail hit, using the existing rig/graph with weights and visible pose restored before returning to the frame. The actual impact still samples its real fall pose; only the expensive radii measurement is reused. If preparation is unavailable, the original exact measurement remains the fallback. Tail geometry builds in1.2ms slices before use, retaining an exact fallback.
- Profiling scopes remain available as `Opening.*`; warmup and forecast buffers are reused and disposed with the encounter. No substitute skeleton, reduced collision mesh, reduced animation sampling or graphic-quality cut was used.

## Functional and visual verification

Final frozen source records both complete routes with real keyboard/mouse lesson input in the existing Editor:

| Evidence | 30Hz simulation | 10Hz simulation |
| --- | ---: | ---: |
| Consecutive game-camera frames | 2,962 | 1,003 |
| Independent native contact samples | 1,502 | 507 |
| Cinematic contacts / final HP | 3 / 70 | 3 / 70 |
| Actual native plank fractures | 12 | 10 |
| Shark/wood, shark/hero, hero/timber overlap failures | 0 / 0 / 0 | 0 / 0 / 0 |
| Minimum shark/body envelope gap | .00500m | .02852m |
| Cached fall radii vs fresh original native measurement | .228mm max error | .253mm max error |

Wrong input retains the paused lesson; map input cannot consume its gate. Moving real timber into the selected dodge endpoint produces another usable route. The jump-departure board and tail-hit board fracture on actual shark contact. Original shark scale is retained in every sample.

Additional comparisons:3,840 articulated-index cases and1,800 native-shark distance/closest cases pass (maximum error<=4.77e-7m);18 analytic contained-sphere cases pass.48 cached forecasts exactly match the original cost/avoidance loop (0 error), and180 batched native support rays match the original render-space hit result (0 error).24 original-timber fixtures,12 true contacts, all separate and match the independent full-tree query;20 simultaneous shark/hero/timber fixtures separate with no residual. These exercise same-frame moving transforms and cache invalidation. A first attempt to run the native-shark fixture in Edit Mode found no runtime-generated shark; it was corrected to Play Mode and rerun, not counted as a production failure/pass.

`contact-final-30/` and `contact-final-10/` contain full maps, native results, continuous silent MP4s and dense transition sheets. Visual review inspected consecutive climb exit, overhead contact, prone and tail/fracture sheets at30Hz, and get-up/tail sheets at10Hz. Foreground wave/timber occlusion remains under the user's fixed-camera policy; hidden contacts were measured independently. This is contact/regression acceptance in these layouts, not final artistic approval, a claim of watching the entire MP4 at normal speed, or certification that every large deck remains dry under every storm crest. Buoyancy/authoring was not retuned in this performance pass.

Reproduction tools: `Tools/ProfileOpeningPerfV225.cs`, `Tools/AnalyzeOpeningPerfV225.py`, `Tools/ReviewContactsPerfV225.cs`, the `Test*PerfV225`/`TestNativeBvhV225`/`TestOpeningSnapshotsV225` tools and the three `Tools/AnimationPipeline/*perf_v225.py` helpers. For reproducible starts, set EditorApplication.isPaused=true before entering Play, add the observer while paused, then unpause. This starts at frame1/time0 without losing the introduction during domain reload. Earlier takes started at different phases and are retained with their actual clocks. Capture includes PNG/native-test overhead and is not a performance benchmark.

## Preservation and state

Final source matches `final-source-hashes.json` across both complete recordings. Latest human save restored after Stop and SHA256 verified **3EA875D36D67463D847BDDB30494E75A689A349F1416A89AB60110931EE864A5**, backed up in this task's `before/user-save.json`; diagnostic saves remain separate. Do not restore older F338/EE58 snapshots over newer user progress. Existing Editor stopped, compiling false, runtime Console errors0. Loaded dirty Story1 is preserved without saving/reloading/discarding. No build, commit, push, automation, second Editor or Player.
