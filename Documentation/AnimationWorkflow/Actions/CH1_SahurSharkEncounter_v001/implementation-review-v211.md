# Current v211 checkpoint — Oct15

See ../../swimming-floating-review-20261015.md (from AnimationWorkflow root) for the current source, final recordings, measured acceptance and limits. No active run remains. The detailed Oct14 development notes below are historical, including superseded ACTIVE recording labels.

---

# v211 implementation review — RECORDING IN PROGRESS

Date: Oct14 Vancouver. Targeted swimming/buoyancy integration; broader animation work remains incomplete. Latest direction: design-v211.md and Documentation/AnimationWorkflow/record-review-loop.md.

The sole original Editor16920 is recording `.codex/chase-entry-20261014/final211-break-playable-pause-30/` through `Tools/ReviewFloatSwimWithTraceV211.cs`. Do not start a second recording/Editor. Its observer self-removes after Defeat; the Editor still needs a verified Stop and restore of the current human save. Fresh protected save: `.codex/float-swim-20261014/save-before.json`, SHA2565A296481F7414AF9DCE308C6F62B990A89CE67F92A1E9268D46136D87C4BF803. Preserve newer real human progress if any.

## Current implementation

- FloatBody: world-volume timber mass450kg/m3 (heavier non-floater675), cap50t; buoyant displacement from five real-mesh probes, net COM heave and damped water-plane moments. Raw sampled height with relative wave velocity avoids positional filter lag. Native gravity/interpolation/inertia/collisions remain; no Y teleport/kinematic standing platform.
- SharkWreckContact: once-onset momentum versus time-integrated held pressure. Repeating an onset impulse each step could overpower buoyancy and drive a passenger platform5m underwater, followed by a12m/s rebound. Native anatomy/mesh collision checks and hit tolerances are unchanged.
- ScatterPlayback: same original six-second214-fragment physical bake/endpoints; release with recorded velocity and free dynamic horizontal separation rather than permanently constrained overlaps.
- Battle: bounded forward swimming pursuit/recovery and real compact/thicker refuge selection (currently actual `deck_Object_30_00_01_00 splinter 4`), without moving/adding timber.
- ChaseEntry/Finale: torso-centred forward arcs, continuous stroke phase, same steering during docking; rebase intermediate points after actual pursuit, stage relative to support yaw rather than pitch/roll, bounded four-second real approach wait. Tighter waterline condition before the moving-edge climb. Entry camera remains on open-water side during climb.
- ThirdPersonPlayerController: ordinary swimmer checks the actual breathing point after current-frame animation, gated out for other motion owners/pause. Original walk/run/roll/ordinary attack/mobile charge and existing sailing/smoothed-break cameras remain.

No new Blender/FBX/rig/Avatar edits in this targeted runtime repair; retained210 pursuit Actions remain. Source changes include the seven runtime files above, the new211 physical bake resource and the native-authored Story1 ocean timing field. No commit/push.

## Review loop and rejected takes

All under `.codex/chase-entry-20261014/`. `hydro-before-30` retained baseline movie (hydro JSON failed due an initial diagnostic Vector3 serializer issue; do not invent baseline numeric clearance). `hydro-first-30` exposed head/chest submersion and53.7deg tilt; `hydro-second-10`, `hydro-third-10`, `hydro-final-playable-pause-30`, `hydro-density-multi-ground-10`, `hydro-balanced-ground-10` and `hydro-balanced-playable-pause-30` drove subsequent revisions. `swim-dock-ground-10` is an explicitly ABORTED recorder-harness take (tracer destroyed shared recorder object; fixed to Destroy(this)); partial MP4 retained, no final acceptance. `swim-rebase-ground-10` demonstrated no backward chase/dock travel but still shallow support/contact shortcomings. `compact-playable-pause-30` reached all6 genuine contacts but exposed severe sinking/rebound during docking; rejected for buoyancy, motivating held-pressure fix. No earlier lucky take approves later source.

## Latest completed take on current source

`contact-pressure-multi-ground-10`: true grounded fixture/StartsOnDeck,332 consecutive frames plus front/side/three-quarter MP4s;209 main-film frames0.066–20.866s, ExitReason=null, all6 contacts (max2.240mm), foot1.343mm and slide1.228mm, real loss-of-stick/floating. Standing0–18s head/chest never below water; feet can be washed by a passing crest (min left-.715m/right-.559m), board tilt max7.872deg. Real movie/key-sheet visually inspected; dense pursuit/fight sequences and final30Hz take still need review. These functional clocked captures are not a real-FPS benchmark or FINAL artistic acceptance.

Remaining: finish current30Hz run, encode/review its consecutive footage (not only key poses), inspect swim-heading trace, assess head/sole-to-water and true contacts; revise material issues. Verify current save/scene after Stop, then update checkpoint. Full jumping/alternative-board choreography, anatomy polish, camera occlusion and performance acceptance remain broader unfinished work.


## Later revisions — current WIP source supersedes earlier trial takes

The same impulse/mass repair alone did not pass every wave phase: `float-clearance-playable-pause-30` retained torso immersion during one fast crest despite six contacts. This is a FAILED buoyancy take, not current acceptance. Story1 waveMotionSpeed was1.8 while individual speeds already approximate gravity-wave dispersion. Native-authored Story1 override is now1.0, retaining all wave amplitudes/wavelengths/material/colour parameters. Native save was scoped to EXACTLY that one field; camera140 and all other original scene records retained. New Story1 SHA2560D7A15BBC4A446F0BDEC25A0001E21436036BD8576926336D0991B7DF85B9269 intentionally supersedes1F40.... See `.codex/float-swim-20261014/wave-scene-scope.json` and original/native scene backups.

After changing physical timing, generated a NEW native50Hz six-second214-piece actual gravity/collision/buoyancy bake: `Assets/Resources/Story1Wreck/OpeningScatter_v211.bytes`,1814649bytes, original210 untouched. Active Battle loader is211. Metadata `.codex/float-swim-20261014/scatter-bake-v211.json`; this was a real dynamic simulation with replay bypassed and battle frozen after release. No wood was positioned to suit the film. Dynamic native replay then releases recorded velocity with natural floating separation. Compact refuge now requires minimum3m width before final mesh/pose admission, to avoid wasting pursuit on a narrow cut that cannot fit the full stance.

Added unconscious body-to-actual-convex-mesh clearance at a bounded2m/s using the original posed body hurtbox segments and an isolated registered capsule query. No altered rig/hurtbox geometry. Actual native query witness: `.codex/float-swim-20261014/native-query-verification.json` hit=true, depth.34794m on144-vertex real wood, unit probe scale. `wave-rate-multi-ground-10` exercised3.381m accumulated bounded clearance. Raised final reveal follows the fallen body's captured heading; it no longer inherits a departing plank's yaw or changes shot side each frame.

`wave-rate-multi-ground-10` (old210 bake/new water) reduced board heave to[-3.072,4.037]m/s and kept torso clear; reversal missed1.487m, not accepted as full choreography. `final211-multi-ground-10` (new211 bake, before final width-preference refinement) played full209frames/all6real contacts(max11.019mm), foot1.260mm/slide1.454mm, standing head/chest never immersed, soles min left-.065m/right-.090m, board heave[-2.620,3.536]m/s. It searched the narrow default deck for14.47s before using real compact `deck_Object_30_00_01_00 splinter 1`; final width preference now approaches that wider candidate first. It did not fabricate a jump (BoardJumpCount0) and made one real breach.

Current30Hz take `final211-break-playable-pause-30` records actual shipbreak, playable combat, pursuit/climb,20.8s film and float; needs Stop/save restore/encoding/visual review. Last code compile errors[]. Older torso-immersion takes remain failures. Do not imply whole-animation FINAL/APPROVED.
