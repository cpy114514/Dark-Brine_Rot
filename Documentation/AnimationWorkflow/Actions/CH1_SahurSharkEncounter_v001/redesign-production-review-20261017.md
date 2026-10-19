# Opening redesign production and review

Stage: BLOCKING. User production authorization: “可以，那开始做吧”; subsequent “继续” authorizes continuing the same design. This is not final visual approval.

## Sources and ownership

- Approved story/camera direction: `redesign-20261017.md`.
- Versioned source: `ArtSource/Encounter/v214/`, original rigs/Avatar retained.
- Runtime: `Story1WreckBattle.Redesign.cs`, `Story1WreckBattle.Contact.cs`.
- Original human work on the shorter voyage remains in `Story1SharkCollisionSequence.cs` and its v213 documentation.
- One existing Unity Editor only. No scene serialization, commit or push.

## Retained takes

Actual game-camera consecutive frames and JSON are under `.codex/encounter-redesign-20261017/`. `review_opening_v214.py` encodes retained PNGs without synthesizing frames. Simulation-clock recording is functional evidence, not a performance benchmark.

| Take | Result / review |
| --- | --- |
| first-15, second-15 | Failed graph initialization from a missing old guard clip reference. Retained failure frames. Reference corrected to existing v050. |
| third-15 | 1293 frames; full route completed, but landing selected tall hull geometry, climb crossed plank, prone floated, tail closeup cropped head. Rejected visually. |
| fourth-15 | 1261 frames; full route completed. Native thin-deck landing improved. Shot2 still hid shark behind hull; pursuit chose a high triangle splinter (~5.3m above water); climb and support rejected. |
| fifth-real-30 | Real input exposed missing early platform carry/admission during the tutorial. Actor walked into swimming before completing movement. Stopped; raw frames/progress retained. The observer at this revision did not retain its full frame map on manual stop; later observer revisions fix that limitation. |
| sixth-real-30 | 4148 frames, Finished, HP70, actual key/mouse input with wrong-key/map tests. Observer needed press-edge resets (now fixed); wood occlusion and climb framing rejected. |
| seventh-real-15 | 1356 frames, Finished, HP70, no diagnostic acceptance or observer interventions. Fragment scatter/camera obstruction improved; climb lens too close and leap insufficiently visible. |
| eighth-deck-15 | 1276 frames, Finished, HP70, real controls and wrong-key/map tests passed. New original ship sole alignment verified over 187 frames: left clearance .0296–.0431m; right .0193–.0395m. Original shark scale (3,3,3) retained. Leap crest still hidden by ship; rejected and trajectory revised for the next take. |
| ninth-deck-30 | 1982 frames, Finished, HP70. Recorder attached after tutorial gate, so no opening visual evidence. Exposed missed thrown-stick contact as the original cached world target separated from the wave-driven shark. Now tracks the posed head surface until impact and retains the endpoint crossing frame before return. |
| tenth-full-30 | Aborted with zero images: experimental recorder scene reload never reached capture initialization. Exact reload cause not established. Removed that experimental reload path; use normal Play plus prompt recorder attachment. |
| eleventh-full-30 | 2661 consecutive actual-camera frames, starts during voyage, Finished, real key/mouse controls, wrong-key/map checks pass, three cinematic contacts registered, HP70. Original deck support has 508 samples: left .02949–.04309m; right .01717–.03952m; original shark scale (3,3,3), also checked live during final tail launch. Console errors zero. Source compiles. Human save restored after confirmed stop; SHA256 matches protected backup. |

Review method so far: dense consecutive PNG/contact-sheet inspection at actual recorded clocks. A generated/opened MP4 is not claimed as normal-speed viewing.

## Verified functional fixes

- Real controller movement/swimming retain platform carry, collision registration and dry-footing admission.
- Cursor lock is restored when entering player control. Opposed keys, wave drift and simply waiting do not complete movement.
- Tutorial gate input respects pause/map/equipment ownership. Charge interrupted by a menu requires a new hold/release.
- Isolated Edit Mode health tests passed for entry HP100,5,0.01,100 repeated oversized hits, healing without budget reset, and external protection. Pause release is idempotent.
- Contact feedback now queries the posed shark head skin against the actual held/thrown stick capsule, and the posed visible tail surface against Sahur's torso. Timed feedback is being replaced with these measured contacts.
- Pursuit selection excludes splinters and validates original deck edge/stance against real mesh, water height and motion. Grip search finds the real mesh boundary rather than using an already inset boarding point.

## Still under review

Crush trajectory/body contact, visible underwater warning, chase continuity/near misses, actual climb support, revised double-handed/prone/getup poses, throw catch continuity, final tail reveal/launch, normal/low render-rate and pause recordings. Do not mark FINAL or APPROVED from compilation or full-route completion.

Protected human save SHA256: `5A296481F7414AF9DCE308C6F62B990A89CE67F92A1E9268D46136D87C4BF803`. Retain test saves separately, stop the test, restore this backup and verify its hash after each run.

## October 18 continuation: ship penetration and original shark size

User specifically reiterated original shark size, Sahur initially stuck in the ship, and clipping review. Current scene native shark local/lossy scale is (3,3,3). Redesign never applies the legacy tutorial shrink in PrepareShark. Do not solve staging by shrinking the shark.

The passenger previously replayed a serialized root position without checking posed soles; the guard takeover also retained that height. Read-only scene audit measured the left sole .4643m below the native Object_30 deck. Story1SahurPassenger now runs after animation, queries native nearby upward deck faces, aligns actual sole offsets, and applies bounded foot support. The cinematic notice/retreat shares the same support calculation. A Play Mode audit returned .0350m clearance; take eight provides consecutive measured and visual evidence. This establishes foot support for the captured opening, not a blanket claim that every animation is free from clipping.

Take eight inspection: shot1 feet above deck; shot25 revised 9m board-height lens avoids the earlier groin closeup; shots31/33/38/40/43 inspected as contact sheets. Remaining work includes v215 overhead/prone/getup integration (source and previews exist but NO FBX staging export yet), head/board/weapon clearance through full attack arcs, and dropped stick flotation after splash. The current runtime still uses v214 for these three poses and still includes its old prone lowering; remove that extra lowering when integrating the authored v215 vertical root motion.

The leap's additive parabola failed when the full original-size animal was sunk deeper: its belly never reached the intended visible crest. Revised trajectory explicitly rises to deck+65m, holds through the short slow beat, then accelerates downward. Shot2 backs out to frame the original-size animal and vessel together. This revision requires the next actual-camera take.

Eleventh take review supersedes that pending crest check: shot2 shows the complete original-size shark above the ship, and shot1 retains dry supported feet throughout the warning/retreat. Shot3 descent still hides much of the contact behind the sail and needs a clearer angle. Reviewed sheets1/2/3/36/37/41 plus prior eighth25/31/33/38/40/43; generated MP4 is retained, not claimed as normal-speed watched. Thrown head-side contact now registers in the 30 FPS take (previous ninth failure retained). Whole-body/weapon clearance across v215 and the other outstanding choreography items remain BLOCKING. Do not call this final or clipping-free.

One-shot thread automation updated via the app at the user's request: id `automation`, October 18 2026 **06:30 America/Vancouver**, COUNT=1 (replaces 06:00). Continue from the latest evidence at that time; do not create further schedules. Existing editor only, retain source history and protected save.

## October 18 second continuation: v215 and clearance

Imported three versioned v215 FBXs through UnityAnimationImport into isolated staging folders, verified all three Humanoid Avatars and exact 60 FPS durations, then moved them into Resources/Encounter/v215. Native skeleton and original hero/shark scales retained. Runtime/preloader now reference v215 Overhead/ProneDodge/GetUp; removed old extra prone lowering. See ArtSource/Encounter/v215/blocking-review-v215.md and export-index-v215.json for source review and contract. Isolated same-Avatar PlayableGraph pose audit retained as `.codex/encounter-redesign-20261017/pose-import-v215.json`.

Independent consecutive-frame reviews are retained as redesign-clipping-readonly-v001/v002-20261018.md. They identified a real 1.15s climb clip sampled as 2.4s, reversed native facing, a single-frame double-stick catch, and floating body/board overlap. Climb now samples its actual duration and compensates that imported clip's facing; catch eases into the hand's rotation, disables the projectile renderer immediately before deferred destruction. Impact points now retain signed shaft/torso clearance rather than interpreting a clamped zero as proof of no penetration.

Water.cs samples the native final Fall mesh to extend the full bone-capsule body envelope. It selects an airborne exit toward real open water, forecasts native compound-cut colliders and keeps later drift continuous with limited acceleration. Released stick continues the original flight into its actual sampled-water crossing, then damped flotation. These are geometry-envelope checks, not proof of every vertex's collision freedom.

Take twelfth-v215-30: 2642 actual-camera frames, Finished, real controls, HP70, three cinematic contacts, wrong-input/map checks pass, console errors zero. Original pose/camera/water changes compile. Climb and single-stick catch visibly improved; whole-body water costs zero in recorded Drift frames, and most of the body remains in water instead of resting through the wood. Late toes approach a floating board edge. Closest prone pass still obscured the body; lying/get-up support did not match board tilt. The splash-to-Fall cut moved the head ~2.44m in one frame. Saved failed/accepted evidence and exact source snapshot alongside the take; restored protected save after stop.

Next revision (thirteenth-v215-15 recording) addresses those findings: prone/early get-up root and baseline follow the real board normal; turn is preserved across prone/get-up; original-size shark banks past the board after its jaws skim over Sahur instead of sending its broad body through the same footprint; splash retains the actual AirHit pose, root rotation and initial head depth, then mixes continuously into unconscious face-up floating. Thrown-stick and tail contact thresholds tightened to .035m; avoidance looks farther ahead with more margin. Apex camera moves to the bow side of the ship; the descent camera stays high. Evaluate the new recording before accepting these corrections.

Completed review of that revision:

- thirteenth-v215-15: 1357 frames, Finished, three contacts, HP70, wrong-key/map tests pass. First actual Drift camera sample moves head .025m from the splash sample; maximum later drift head step .187m at 15 FPS. Consecutive images confirm gradual sinking/turning to face-up rather than the old instant pose cut. Prone/get-up mesh follows the tilted board better; catch has one stick; body drifts in open water. Same human save restored and verified.
- fourteenth-v215-30: 2641 frames, Finished, three contacts, HP70, wrong-key/map tests pass. First actual Drift camera sample moves head .0707m; maximum Drift step likewise .0707m. (The preceding row changes stage to Drift while still rendering the last normal-speed falling sample; that falling displacement is not a pose switch.) All recorded Drift body-envelope costs zero and released-stick flotation active. 504 original-ship foot samples retain positive soles (.0295m left minimum, .0190m right minimum) and shark scale (3,3,3). Console errors zero. Full actual-camera MP4 retained.
- fifteenth-nearpass-15: focused native geometry review, deliberately stopped on the first get-up shot after capturing 13 extra top views through the prone pass. Recorder therefore marks this focused take aborted; it is not a full-route success/failure. Primary game camera remains unchanged. Auxiliary top-000/003/006/009/012 inspected: banked shark passes beside the visible Sahur/board footprint; later fin/foot projections approach/cover the edge, so these top views alone do not certify complete vertical back/belly/appendage clearance. The earlier attempt to attach this observer during take14 missed shot34 and yielded no auxiliary samples; take15 is the actual diagnostic evidence.

Current source compiles; isolated low-HP/nonlethal cumulative budget and pause ownership checks pass after v215 integration. Each run stopped before restoring the protected save; final hash matches the protected backup. No commit, push or scene serialization. Work remains BLOCKING: main-camera closest prone pass still needs clearer contact silhouette, mast/sail response during body crush needs further visual work, warning shadow readability and full weapon/body clearances require polish. Continue those concrete items at the scheduled 06:30; preserve this evidence instead of claiming final or universally clipping-free animation.

## One-shot 06:30 continuation, October 18

The requested one-shot heartbeat ran in this chat. No further schedule has been created. Same existing Unity 6000.3.23f1 Editor on port7800 only; protected save verified before Play.

Independent camera and geometry audits are retained in `redesign-camera-readonly-v004-20261018.md` and `redesign-geometry-readonly-v005-20261018.md`.

- Added immediate camera near-plane sphere separation from the real compound timber geometry, keeping the existing damped distant-occlusion selection. Set the current shot's FOV before calculating its near-plane footprint.
- Native isolated probe (`Tools/AuditCameraLensV217.cs`, `.codex/encounter-redesign-20261017/camera-lens-v217.json`) proved disabled queries do not work in this Editor: disabled sphere false/0, enabled sphere true/.549999952. Camera and water query colliders now enable only inside try/finally, disable before returning, and remain physically placed far outside the scene.
- **This invalidates earlier zero water-cost values as geometry evidence.** The retained pictures and continuity measurements remain evidence, but the old disabled-query cost values did not test separation. Water forecasting now also rotates each part's broad-phase bounds about the COM, includes the COM/bounds offset, compares equal margins along three future samples including authored drift, and compares stopping against the current velocity before braking. These changes require new actual recordings.
- Warning silhouette follows displaced water and has stronger opacity; revised opening shot shows the approaching black shape. Its polygon edge remains too hard for final polish.
- Added opt-in posed shark-triangle versus native hero region-envelope diagnostics, preserving signed values and containment uncertainty. A negative conservative capsule gap is not declared actual hero-mesh penetration. Added auxiliary top/side pictures and a separate native mesh bounds probe; main camera is not changed by these observers.

Take **sixteenth-rigging-15** retained 1335 actual-camera frames, real inputs, Finished, HP70 and three contacts. Wrong-input/map tests pass; 252 original ship support samples have left/right minima .02965/.01767m and shark scale(3,3,3). Every recorded camera-lens-inside flag is false. Nevertheless this take is **rejected**: 115 rigging exceptions kept the game camera at shot1 throughout the leap/descent, so shot2/3 are absent. Native wood meshes are unreadable in Player; the Editor-only access used during initial source inspection concealed that issue. First cloth transfer also exposed a source Z-reflection mismatch (up to10.162m), not an acceptable deformation error. Errors, full frame map, MP4, source snapshot, and test save are preserved. Play stopped before restoring the protected save and rechecking its hash.

For the next recording: independently readable native contact mesh snapshots were created through Editor APIs in `Resources/Sailing/v217/RiggingContacts` without changing the original FBX/importer/scene. The native source SHA is verified against the existing cut asset. Rigging is being corrected to transfer native wind triangles at their current barycentric deformation, release only truly contacted cut cells, and preserve already released pieces through the t6 hull fracture. Prone staging now moves the entire shark outboard earlier before yawing, and climb framing has a slightly farther/higher frontal board-height position. Re-record and evaluate before accepting any of these revisions.

## Later October 18 continuation: v218

See [revision-v218-20261018.md](revision-v218-20261018.md) for the superseding geometry diagnosis, English prompts, stance transitions, performance changes, source snapshot and validation limits. Independent native skinning confirmed repeated scale in the old contact/envelope bakes; those old geometric measurements cannot certify contact or clearance. Visible shark scale remains unchanged. Full C# compilation passed independently, but the existing Editor main thread became unresponsive and no v218 actual-camera recording was possible. Keep the current Editor and unsaved work; resume runtime validation when it responds. Protected save hash remains unchanged. No follow-up automation created.

## October 18 evening — v219 crush revision

The existing Editor recovered after the user's intervention. See [crush-v219-20261018.md](crush-v219-20261018.md) for all accepted/rejected takes, exact source snapshots and actual performance measurements. The shark has a continuous breach/descent/recovery trajectory, contact-aligned body compression, original Swim tail/fin contribution, and unchanged (3,3,3) scale. Native mesh collision queries use cached/lazy spatial bounds and incremental preparation. Ship warm-up no longer drains its shard queue on cinematic entry. Source-parity verification is also sliced across frames. Initial skin-region indexing caches the bones array instead of repeatedly allocating it per vertex weight.

The broad landing decks now use water-entry drag; landing moves to 10.2s with a 2.6s get-up. Take26 records the motion through the English Move tutorial (757 frames). The deep deck dive is fixed in this capture; two early get-up head samples still cross the wave surface slightly (minimum -0.0762m). Real wave wash and launch-view debris occlusion remain polish issues. Its movie predates only the subsequent scheduling/array-cache optimizations, which have separate no-capture runtime profiles.

Compilation and 1,603 query-equivalence cases passed. Performance remains under review: realtime profiling still exposes long frames near hull fragmentation. Do not describe the whole opening as finished, performance-approved, or universally clipping-free. Preserve all takes and test saves. No scene serialization, commit, push, new Editor/Player, or follow-up automation.
