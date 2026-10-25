# Project cleanup and exact-result performance pass — v243

The user requested project cleanup and better performance with the same playing experience. Preserve the existing graphics, animation/timing, input, damage, physics and save data. This pass uses only the existing Unity6000.3.23f1 Editor; no standalone build, source-control operation or scheduling.

## Measured cause and implementation

Initial passive observation of the user's running Story1 Move tutorial gate found CPU LateUpdate approximately10.96ms, including native solids10.81ms (shark/wood8.62ms and shark/hero2.15ms). GPU median was approximately6.9ms. The tutorial legitimately paused simulation while awaiting input, but its final contact pass repeated identical exact queries. The first passive window includes tool compilation warmup and is diagnostic only; do not use its average/max frame time as a benchmark.

- `RedSolidTouch` can retain a previous-frame result only while the tutorial waits. The exact native pose stamps, tree identity, complete relative renderer/wood matrix and wood offset must all still match. Normal moving play retains the original per-frame policy. The geometry, containment tests and clearance margins are unchanged.
- Retain the tutorial's exact posed vertex snapshot and exact bounds across identical native bake stamps. Local snapshots remain valid when a uniformly scaled rigid root moves; the nonuniform/world fallback additionally matches the entire world matrix. New native poses invalidate the snapshot. No simplified collider replaces the mesh.
- Ocean and visible-sun updates use `TryGetComponent` for the optional DayNightCycle. They still reacquire the component every update, including runtime add/remove, and use the same fallback direction. This removes the sampled Editor missing-component allocation (approximately614 bytes per caller per frame) without retaining a stale negative cache. No sun, water, shader, render-quality or distance setting changes.

## Paired native measurement

`Tools/BenchmarkOpeningCacheV243.cs` alternates reference/cached/reference/cached on one settled, actually paused Move gate. The reference invalidates the result and vertex caches before each director tick but retains the original within-frame reuse. Both paths use the same exact geometry, original simulation/input and final solid solver. VSync/frame cap are removed only during measurement and restored in finally. Native screenshots are taken after the measured windows.

| Window | Reference1 | Cached1 | Reference2 | Cached2 |
|---|---:|---:|---:|---:|
| Sampled frames |408|469|419|470|
| Native solid mean ms |3.6152|0.8431|3.5667|0.8524|
| Shark/hero mean ms |2.2162|0.02238|2.2006|0.02189|
| Actual frame median ms |8.5432|7.4045|8.2873|7.3695|

Mean native-solid cost falls3.5909→0.8477ms (76.4%). Median frame time averaged across the two windows falls8.4152→7.3870ms (12.2%); GPU/Editor work remains. These numbers apply to this paused tutorial workload, not all-game FPS or a low-end hardware guarantee. The initial live scene has a different timber arrangement and must not be presented as a paired before/after timing.

All recorded final hero/shark/bone/timber transform components remain identical within and between the four windows. All three screenshots compared to reference1 have zero differing pixels. Evidence: `.codex/project-performance-20261023/paired-cache-benchmark.json`, `paired-visual-check.json`, reference/cached PNGs. Passive CPU hierarchy and allocation evidence are in `live-markers-before/result.json`.

## Cleanup

Removed only `.codex/opening-optimization-20261012/standalone` and `standalone-snapshot`: two obsolete generated Windows players,740 files,3,445,461,461 bytes (3.45GB decimal). Both match their successful build reports. Resolved absolute paths are direct children of the explicitly named generated-build directory, links/source/save/review files are rejected, running outputs are rejected, and every file is SHA256 inventoried twice before deletion. `Tools/CleanupGeneratedBuildsV243.ps1` implements the checks. Keep the newer historical `standalone-v165`, all reports, sources, recordings, saves, compiler/shader caches and Downloads. These generated binaries are reproducible outputs, not archived binary copies.

The before/after asset audits match all7 enabled build scenes,406 scene-dependency paths and745 runtime Resources paths/GUIDs, with no referenced backup assets.2611 metadata files have zero duplicate GUIDs and zero orphan metadata. All preserved serialized game assets, animation clips, controllers, masks and shaders remain byte-identical to the snapshot. No game asset is deleted, moved or reserialized by cleanup.

## Dynamic equivalence and opening regression

Final `performance-v243-verified` records2705 actual existing-Editor simulation rows from crush to unconscious drift/shore handoff. All17 charge/pose-ownership checks and23 contact/geometry/continuity checks pass: both real tutorial strikes, all three cinematic contacts, original shark scale3 and nonlethal70HP finish.79 independent later-film posed-body/shark/native-wood probes have minimum body gap0.028357m and zero sampled shark/wood triangle intersections. This data-only regression deliberately does not create another large frame/video cache; prior v242 artistic footage is retained.

`Tools/VerifyCacheEquivalenceV243.cs` also compares the actual cached/native algorithm against forced fresh calculations on identical live inputs, restoring the actor root and diagnostic gap after each probe.97 sampled hero separations and2908 real timber queries have exactly0 position/gap/contact difference, including each of the three cinematic contact frames. Query offsets and moving roots/poses exercise key invalidation. This supplements the static paired pixel/state check; it does not prove clipping freedom for every possible wreck layout.

Intermediate evidence is retained: `performance-v243` missed the first independent contact because its auditor attached after fast data-only playback reached that hit; it is not accepted. `performance-v243-final` passes charge and both tutorial contacts but one tail-frame conservative body envelope is-0.026991m; it is not a passing geometry take. The final verified run starts from a fresh paused Play-mode Story1 before attaching all observers and then begins the full crush, rather than attaching midway through the tutorial. It keeps the original thresholds and contact behavior. The native differential audit matches old fresh computation to optimized computation, without introducing an unrelated animation/contact redesign. A missing namespace and an invalid dictionary cast in diagnostic-only code were fixed; the initial error file is retained separately and no failing tool result is counted as acceptance.

The post-change passive capture `live-after` has no `GetComponentNullErrorMessage` allocation paths for either ocean or sun; both paths were present in the initial CPU trace. Do not claim the entire Editor heap is allocation-free or a standalone-player byte saving, since Editor-only lookup diagnostics contributed to the original allocation.

## Island/map regression and final state

`v243-world-trace` completes shore, forest, panorama, ocean, map and return-to-shore windows, then restores/rebuilds all25514 foliage entries with0 restoration failures and0 unsuppressed batch members. Native shore/forest images were inspected; grass, trees, lighting and water remain present. This is a regression/diagnostic run, not a paired island performance improvement claim. The old diagnostic renderer dump used C# null propagation on a missing Unity MeshFilter (Destination) and aborted the initial `v243-trace`; the tool now checks Unity object presence explicitly. Its rejected error/capture is retained. The normal-timing harness explicitly uses timeScale1 for the island, avoiding accidental inheritance of a paused tutorial; old scale/caps are restored afterward.

The existing island settings menu selects its saved runtime pipeline clone at renderScale0.6. This pass does not change those preferences or the pipeline assets. Therefore these island results must not be compared as an optimization gain against older renderScale1 captures. Current median frame times are shore18.813ms,forest11.606ms,panorama8.195ms,ocean4.689ms,map3.832ms and shore-return18.871ms. GPU averages are18.808,11.499,8.066,3.753,0.665 and18.858ms respectively. Graphics/vegetation remain the major island limitation; no uniform whole-game FPS uplift is claimed.

Final `Tools/VerifyProjectPerformanceV243.py` passes16 aggregate checks covering paired performance/pixels/state, dynamic fresh-query equivalence, opening regression, scene/resource integrity, cleanup bounds, island/map completion, foliage lifecycle and preservation. Runtime changes are confined to four existing C# files. Original art, animation, shader and serialized scene/controller/settings data are retained. Final source hashes and all raw/failed/accepted diagnostics are under `.codex/project-performance-20261023/` and the linked native/world recording folders.

After confirmed Stop, the fresh human save is restored byte-for-byte (SHA2568C65D7BCABE0C563AAD31ED60E0BEE2AC363D93EDB721826986062B035357000); saved Story1 remainsF13D1A3D625C04ACF199C0713DDE07F17FB25DDFD2329024BC514422C94F0C4D. Editor returns stopped/unpaused to clean Story1 and the authored PC pipeline at scale1,60fps cap,VSync1; runtime-only island settings clone is destroyed normally. Console errors0. Source quality files and saved preferences are not edited. No new Editor/Player, Windows rebuild, Git/UVCS mutation, upload or automation. Measurements are from the existing Editor/i5-14400F/RTX4060; shipping-player/low-end hardware and long-session behavior remain unverified.
