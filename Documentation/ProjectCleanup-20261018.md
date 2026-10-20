# Project cleanup and focused runtime optimization — October 18, 2026

This round follows the user's request to clean the project and improve performance. Existing uncommitted v221/v222 work, gameplay, English prompts, original shark size, source animation and review evidence were preserved. Only the existing Unity6000.3.23f1 Editor was used. No check-in, push, new schedule or scene serialization.

## Cleanup

- Removed21 obsolete generated Windows test builds from `.codex/opening-optimization-20261012/standalone-vNNN`, releasing37,986,717,549 bytes (37.99GB decimal). Each directory was resolved and checked to remain inside that exact generated root, matched to a successful build report and executable, checked for links and unexpected source/evidence files, and inventoried before removal. Latest historical `standalone-v165`, all reports, videos, raw capture frames, source files, saves and compiler caches remain intact. Downloads is outside the cleanup scope. These removed compiled outputs can be recreated from source; they are not archived binary copies.
- Archived13 unreferenced recovery scenes and their metadata outside the project at `D:/indiegameDev/unity/ProjectCleanupArchive/Dark-Brine-Rot-20261018/Assets/_Recovery/`. The existing cleanup tool hash-verifies backups before calling AssetDatabase.DeleteAsset. They remain recoverable. Do not restore recovery scenes wholesale over the current authored scenes.
- Moved the remaining root-level `Story1Eyelids.cs` into `Assets/Scripts/Story/Chapter1/` using AssetDatabase.MoveAsset, retaining its GUID and content.
- Moved three superseded native v214 pose folders (GetUp, ProneDodge, Overhead) from `Assets/Resources/Encounter/v214/` to `Assets/ArtArchive/Encounter/v214/`. Runtime source and serialized dependencies were checked before moving; current v215 replacements remain at their original paths. All six recognized files and GUIDs are preserved. The FBX sources and ownership markers remain available for editing/history. These unused models are no longer implicitly included through Resources; no new Player package size was measured this round.
- Removed an unused `finalSplash` field/assignment that caused a compiler warning. No behavior depended on it.

Plans, executable hashes and completed removals are recorded in `.codex/project-cleanup-20261018/generated-build-cleanup-*.json`. Resource moves are recorded in `archived-resources.json`. External recovery archive contains its own `asset-cleanup-plan.json` and original file hashes.

## Runtime work

Changes are confined to the opening's existing geometry work; no visual-quality, wave-amplitude, animation-timing or collision-margin reductions:

1. Tutorial contacts reuse a native baked shark pose within the same frame. Every RedShark bone sample invalidates that cache. Root fitting still reads the current world matrix. Crush/cloth projection keeps its uncached behavior.
2. Vertex loops reuse transform matrices instead of crossing the native Transform boundary for every point.
3. Safety queries search only as far as their required positive margin. Overlaps and fully contained capsules still obtain the correct signed depth. The original uncapped query remains available for contact/reference checks.
4. A uniform, orthogonal renderer transform permits capsule/triangle queries in shark-local space. Nonuniform or sheared transforms retain the world-space fallback. This removes root travel/turns from geometry refitting.
5. Between exact tree refits, bounds expand by the maximum measured displacement of any current vertex from the retained reference pose. Every possible current triangle remains enclosed; leaf tests continue using current vertices. A3m world displacement threshold refreshes exact bounds. This avoids repeated full triangle-bound work for small deformation.

The initial optimization pass reduced typical cost but exposed remaining expensive refits; its full evidence is retained as `cleanup-v223-after-30` and `comparison-first-pass.json`. The final source uses local-space conservative refitting and has its own independent capture.

## Measurements and limits

Function timings during actual-camera tutorial recordings:

| Metric | Before | Final |
| --- | ---: | ---: |
| Clearance median | 3.95ms | 2.38ms |
| Clearance95th percentile | 4.34ms | 5.75ms |
| Clearance maximum | 24.89ms | 23.25ms |
| Route-planning maximum | 8.77ms | 6.46ms |

The median is about40% lower. Tail timings are not uniformly improved: the95th percentile is higher in the final capture, and occasional peaks remain above a60FPS budget. Free timber evolves differently between recordings, and the final observer performs additional native validation. These captures are not an overall FPS benchmark and do not establish a causal improvement to every frame or to the island scene.

Across16 identical posed-mesh snapshots, uncapped reference capsule queries took11.94ms total versus0.44ms for margin-limited queries, with zero query-result error after applying the limit. Fresh native bake versus cached pose maximum local error was0.00001193m; matrix versus native TransformPoint world error was0.00017273m. These are reference calculations on the same snapshots, not shipping FPS claims.

Final take `cleanup-v223-final-30`:1,573 actual-camera frames through Pursuit, real keyboard/mouse controls, HP88, both final visible tutorial head/shaft contacts0.000m. Wrong-key/map gates pass; moving real timber onto the selected roll destination chooses another route. Recorded dodge clearance minimum0.528m, other-timber body penetration cost0, actual swim input6s. Independent uncapped checks on four sampled dodge poses have minimum1.586m; these sparse samples do not replace the full frame sequence. Key game-camera poses and transitions were visually inspected. Console errors0.

`TestTutorialGeometryV223` passes4,000 queries across rotated/uniform/nonuniform scaled, translated and expanding closed-box poses, including interior cases and conservative refitting, against brute-force distances/containment; maximum error0. No geometry/collision test is claimed to prove every possible layout is clipping-free.

## Integrity and handoff

- Seven build scenes and all394 dependency GUIDs remain identical to the pre-cleanup audit.
- All721 retained runtime resource paths/GUIDs match; the six archived obsolete files account exactly for the Resources difference.
-2,515 metadata GUIDs checked; duplicate GUIDs0, orphan metadata0.
- User save restored after every confirmed stop. Current protected SHA256: `EE58C485F0BA23FB6DE948F847944569C21BCFB2A83581104E4B61F1D1C523D0`. Never restore older build/animation test saves over it.
- Existing Editor returns to stopped Story1. No scene files or build-scene configuration were changed.
- This task updates the Unity project and does not rebuild or replace anything in Downloads. The previously used executable path was absent at the final check; this cleanup performs no file operations in Downloads and does not restore potentially intentional external changes.

Final artifacts: `.codex/project-cleanup-20261018/` (inventory, cleanup manifests, dependency audit, geometry test, comparison and final source snapshot). Final movie: `.codex/encounter-redesign-20261017/cleanup-v223-final-30/tutorial-review.mp4`. All before/intermediate/final takes are retained.

Full hull-fragmentation physics spikes and island CPU/GPU frame-time acceptance remain outside this focused optimization's verified results. No project-wide performance approval is claimed.
