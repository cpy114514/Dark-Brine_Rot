Latest performance/contact-preservation checkpoint: `Documentation/AnimationPerformance-20261019.md` (v225). It supersedes the v224 performance measurements below; retain this history and its native reference. Final global/release performance remains unaccepted; newer save authority is3EA875D36D67463D847BDDB30494E75A689A349F1416A89AB60110931EE864A5.

# Opening contact repair — v224, Oct 19

## Latest accepted contact checkpoint

The frozen source passes both complete runs in the existing Editor. This accepts the targeted contact repair in the recorded layouts, not final animation quality or release performance. Earlier takes below are retained failure history.

| Evidence | 30Hz simulation | 10Hz simulation |
| --- | --- | --- |
| Folder | `contact-v224-eleventh-30` | `contact-v224-twelfth-10` |
| Consecutive game-camera frames | 2,906 | 946 |
| Independent native geometry samples | 1,502 | 491 |
| Shark/wood overlaps | 0 | 0 |
| Shark/hero overlaps | 0 | 0 |
| Hero/free-timber penetration over 1mm | 0 | 0 |
| Maximum measured hero/free-timber depth | .489mm | .493mm |
| Native plank fractures | 12 | 10 |
| Cinematic contacts / final HP | 3 / 70 | 3 / 70 |
| Minimum whole-body shark clearance | .0049m | .0284m |

Both reach Finished; original shark scale remains (3,3,3). Jump departure and final tail contact fracture the actual occupied board. Real input, wrong-input pause, map gating and displaced-native-timber adaptive dodge checks pass. Ship sole samples remain positive (minimum about .033m left/.034m right). No unresolved solver contacts or current console errors. Full frame maps, independent native results, continuous silent MP4s and consecutive transition sheets are retained in each folder under `.codex/encounter-redesign-20261017/`.

Visual review used native-camera consecutive sheets: 30Hz climb entry/exit, actual overhead contact, prone, get-up and tail/fracture; 10Hz climb exit, prone/get-up and tail/fracture. Visible body/board separation and impact-frame fracture are retained. Foreground wreck occlusion remains per the explicit camera direction; occluded anatomy was checked by native geometry queries, not certified visually. This is not a claim that the MP4 was watched at normal speed or that all animation polish is complete.

Performance remains a material limitation: the runtime solid-pass stopwatch during capture reports 30Hz Pursuit median21.97ms/P95 146.96ms/max436.36ms, Duel median27.67ms/P95 121.21ms/max471.08ms. At10Hz Pursuit median46.48ms/max421.03ms. Capture, Editor and independent diagnostic work are present; these timings do not establish release FPS, but the solver still has real long-frame spikes. Keep `Opening.NativeSolids` profiling and prioritize measured cost reduction without weakening the native contact reference. Do not call performance accepted.

Final source hashes match `contact-v224/source-hashes-final-candidate.json` across both recordings. Protected human save restored after each Stop, final SHA256 EE58C485F0BA23FB6DE948F847944569C21BCFB2A83581104E4B61F1D1C523D0. Existing Editor7800 left stopped/compiling false; Story1 remains dirty and loaded, not saved/reloaded/discarded. No new Editor/Player, scene serialization, build, commit, push or scheduling.

User report: the shark, Sahur and floating planks intersect during the opening. Preserve original shark scale and shark-caused plank fracture. This is a targeted collision/choreography repair, not final animation approval.

## Reproduction

The current Redesign route disables `Story1SharkWreckContact` after the crush. Only the tutorial explicitly separated the measured hero envelope from the shark. Pursuit/Duel sampled transforms without the corresponding solid-wood checks. Jump destroyed its board by clock, without a shark/wood contact. `RedFeet` uses the controller's constant foot offset, which is not the bottom of a prone/get-up mesh.

Baseline: `.codex/encounter-redesign-20261017/contact-v224-baseline-10`, 1003 consecutive native-camera frames through Finished, real keyboard/mouse lesson input, three cinematic contacts, 70 HP. Retain frame map and baseline-sheet.jpg. A captured game-camera stream is not release FPS evidence or normal-speed artistic approval.

## Implementation under verification

- Shared late contact pass after the current timeline/lesson pose; independent native shark skin vs original visible cut-timber triangles, with broad-phase bounds and cached geometry.
- Actual shark/timber contact uses the existing fracture/shard path. The occupied platform is retained until its approved departure/defeat beat. The entry-deck protection override is scoped to this confirmed fracture call.
- Jump now animates the shark toward the departed board and relies on native contact for fracture.
- Supported prone/get-up poses measure the native body against the deck; other free timber receives separation instead of passing through the hero envelope.
- Native posed-skin cache is reused within each non-crush frame; scale, original animation assets and camera policy unchanged.

First 15 Hz take was rejected and stopped: repeated separation-direction searches on the post-crush debris cost about 2488 ms in one measured runtime frame. Footage/result kept in `contact-v224-first-15`. Replaced repeated directional searches with nearest-surface separation and one exact validation, with a conservative supporting-plane fallback. Further tests pending; do not describe this first version as accepted.

## Preservation

Only the existing Unity Editor/7800. The open Story1 scene is dirty in memory; do not save/reload/discard it. No commit/push/build or new scheduling. Protected save before testing: `.codex/encounter-redesign-20261017/contact-v224/save-before.json`, SHA256 EE58C485F0BA23FB6DE948F847944569C21BCFB2A83581104E4B61F1D1C523D0. Diagnostic saves are stored separately and the protected save must be restored after stopping each diagnostic run.

## Additional verification / targeted corrections

Third take (`contact-v224-third-15`) reached all tutorials and two cinematic counters with no sampled native shark/wood intersections, but was rejected: the protected platform displaced the shark before the marker-based tail contact, preventing defeat. The tail contact is now evaluated before releasing its platform protection in the same impact frame. Native fixture test: 24 original-timber placements/rotations, 14 actual intersections, all separated with no residual. Keep the fixture report; large artificial fully embedded cases are not artistic motion examples.

Fourth take (`contact-v224-fourth-30`): 2827 frames through Finished, HP70, actual tutorial Attack/Charge contacts, three cinematic contacts and 11 native plank fractures, including the departure board and final support at the tail impact. This take is also superseded: an independent fresh-skin query found one chest-envelope penetration at the tail impact (~.928m). Replaced the hip-centre marker test with actual tail-weighted triangles, whole-body outside clearance, and an unsigned surface query for the open tail subsection. A new capture is required.

The counter stance is inset1.25m from the actual supported edge (still validated for both feet), so the descending club can meet the snout over open water. Previously a3m inset pulled the head through the occupied deck; collision correction delayed the supposed hit into recovery. Counter fitting now uses the existing front-skin fitter, not an arbitrary head-side vertex.

Measured bottleneck with the live Editor: full native shark tree refit16.8074ms;119 active timber queries total1.6396ms. New per-subtree displacement bounds preserve exact current triangle tests while updating only motion bounds between full refits. Independent equivalent-query test:3840 distance/closest-point checks, maximum error4.77e-7; original4000 signed/capped capsule checks still pass with zero error. Runtime profiler marker `Opening.NativeSolids` and per-frame timing retained. This is algorithm evidence, not a claim of release FPS.

## Later robustness findings

Sixth low-rate take (`contact-v224-sixth-10`) reached Finished/982 frames/HP70/three contacts; overhead at5.4s and native tail at21.6s/.0102m gap. It is **not a clean acceptance**: independent every-frame whole-body/native-timber queries found pursuit wood overlaps (one summed cost4.61), and the displaced-native-obstacle roll fixture failed because no route was selected. All sampled shark/wood checks were clear. Full data/video retained.

Seventh take was stopped: the movement tutorial handed back a get-up pose without re-registering standing support after `Story1WreckRider.OnDisable` had released it. Native controller feet are now placed on the current board surface and standing support is explicitly restored for Move only. Ordinary swimming handoff is unchanged.

New fixes under verification: native timber is separated against both shark and hero in one constrained solve; slide along the shark's supporting plane when its outward correction would enter Sahur, with a finite combined-bounds escape only for embedded shapes. Actual convex-timber/capsule verification remains the check. Corrected free-timber impulse application point after positional separation and cap outgoing momentum, avoiding repeated render-pass impulses. Roll support grows from the legal starting stance during its entry quarter, so a valid edge stance is not rejected by the full later roll footprint. Actual roll skin is supported against the native board.

ILPP compilation service briefly disconnected (Jobs code generation gRPC Unavailable). Existing Editor recovered after `CompilationPipeline.RequestScriptCompilation`; Tundra build success. No restart/new Editor or scene discard. Subsequent source is still to be retested before final acceptance.

The optional joint-fixture experiment is retained but NOT accepted as simultaneous-constraint coverage: early placements produced no simultaneous shark+hero overlap. Do not count those attempts as passing joint tests. Independent full recorded native queries are the integration evidence.

Joint-fixture correction: selecting an actual broad native deck (rather than the first thin hull piece) provided20 valid overlap placements,19 simultaneous shark+hero cases; all separated with zero residual native queries. Report `contact-v224/joint-native-tests.json` records this run. Fixture shark translation was restored synchronously before rendering/physics; this deliberately embedded test is not motion-quality footage.

Eighth10Hz take: Finished/902 frames/HP70/three cinematic contacts/9 native plank fractures. Shark/wood and shark/hero had zero sampled overlaps; adaptive actual-obstacle reroute changed endpoint3.854m with5.007m travel; no runtime console errors. Superseded by the latest final-impact cleanup: one pursuit body/timber overlap and one newly-created-shard contact required an end-of-frame body pass. Three other positive planner costs were near-tangent contacts (the planner adds .01 even for a submillimetre touch); recording now reports raw native penetration depth separately, with a1mm review tolerance. The new final pass includes freshly-created shards and checks both actors, using live broad-phase bounds. A conservative enclosing-sphere query rejects distant timber before triangle-pair traversal.

Ninth30Hz take stopped and superseded during Tutorial. New enclosing-sphere broad phase used a zero distance cap, which bypasses the signed interior branch for a fully contained sphere. Changed its query cap to .001m; the native24-case fixture now compares every production result against an independent full triangle-tree query without that broad phase. The live joint fixture at a different pose also reported insufficient simultaneous cases (0); do not count that attempted rerun as acceptance. Diagnostic save restored and hash verified after Stop. New full-rate and low-rate recordings pending.

Tenth30Hz take stopped during incoming voyage for the final conservative tolerance correction: reject the enclosing sphere only at >=.001m, preserving native1mm triangle-touch tolerance as well as signed containment. Source now frozen for the next recordings.

Latest frozen source: contained-sphere regression18/18 PASS (fully contained/overlapping/outside analytic closed cube). The fixture initially expected exact interior depth even for a sphere crossing the surface; corrected test to the existing surface-overlap convention and retained exact-depth checks only for fully-contained cases. Native timber24/24 production-vs-independent-triangle-query comparisons PASS,12 actual contacts all separated. Joint native fixture20 overlaps/19 simultaneous hero+shark cases PASS, all final queries clear. These fixture reports now correspond to the eleventh30Hz source. Artificial fully embedded displacement limits are not normal motion examples.
