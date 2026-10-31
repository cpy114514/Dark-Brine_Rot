# Animation review

## Batch032 — farther grass boundary

Native sameusercamera before/after and freshPlay images showgrass covering formerlybare farstrip. Extended representativefade31–40→60–80m;fixedprojectionwinddisabledpatchconfirmsgradualnewfade.24sampledadvancing/turning/returnpositions and pixelinspected;notcontinuousgameFPS/streamlatency or perfectpixelstability. Noanimation/character/rig/clip/collider/materialchanges. Morecompletegrassinrangeaddsgeometry;noFPSgainclaim. Allsixstylespreserved. Documentation/Grass-Distance-Batch032.md.


## Batch031 — native ink/oil visual review

Actual six-card gallery, matching Ink Wash/Van Gogh comparison and noon/night/MSAA4/pixel images inspected. Ink wash has warm paper, dark/light ink and dry-brush/mist; oil uses local curved dabs/impasto and blue-gold color. First dense interference-ring prototype rejected. Both painting modes have24 captured orbit positions; roundtrip2,693/1,547 pixels>2 of921,600, not pixel invariance/gameFPS. Sahur pose frozen; no animation-smoothness or full encounter acceptance. All original geometry/UV/rig/clip/collider/material/scene bytes preserved; full grass layout and existing comics retained. Next actual action/encounter readability in six styles. Documentation/Painting-Batch031.md.


## Batch030 — comic rendering replaces paper and film modes

Inspected actual matching Color Comic/B&W Manga noon views, midnight,4×MSAA, pixel, gallery and split comparison. Comic surfaces use contours/printed dots and Manga shadow hatching; full world/transparencies are processed, overlay red stays red. Frozen Sahur and24-position orbit validate appearance samples only; no smoother-animation/full-combat acceptance. Final roundtrip differs3,998/921,600 pixels>2, not perfect temporal invariance. Models/UVs/rigs/source clips/controllers/collision and complete grass layout unchanged. User's replacement direction supersedes prior requests to retain Origami. Next active encounter/tutorial readability in both comic modes. Documentation/Comic-Batch030.md.


## Batch029 — Origami appearance and world coverage

Actual native before/after views confirm ground/grass now participate in paper shading and the original Sahur surface has triangular folded lighting with reduced photographic/glossy treatment. Four matching previews, actual gallery/comparison, MSAA4 and pixel views inspected.24 captured camera positions plus return are a visual sample, not game FPS or full animation acceptance. Actor pose was frozen; no claim of smoother animation/combat. Meshes/rigs/clips/controllers/collision and grass028 layout unchanged. Original silhouettes remain; true folded silhouettes need separate art scope. Next: active Sahur/Boss/tutorial readability under paper lighting. Documentation/Origami-Batch029.md.


## Baseline — 2026-10-28

No new visual observations yet. Preserve current combat design and existing uncommitted V278 jump, V272/V273 blade, V274 momentum, V275 spin movement and V276 transitions. `Documentation/JumpStable-v278.md` records historical native verification and pending human artistic approval; do not relabel it as a current test or final approval.

Combat visual lifecycle exceptions are repaired and verified with native reload/pool sampling. `Evidence/baseline-editor.json` records installed controller states, timing and transitions. The existing `VerifyStableJumpV278.Run` was rerun this batch: all 30 installed-controller/Humanoid/timing/root contracts pass across grounded, gameplay and rifle controllers. No clip/controller was edited. Code and numeric contracts cannot establish perceived smoothness, arbitrary-pose clipping freedom, silhouette or weight.

`Evidence/first-island-after-reload.png` is an actual native Game-view still, inspected this batch: Sahur is standing in dense grass beside the sea in the existing pixel style. It establishes rendered scene availability after reload; it contains no attack sequence and does not verify smoother animation. A transient black Game view during earlier loading was observed through desktop UI and subsequently resolved.

Next review: moving attacks, combo buffering, charge release, jumping/landing recovery, dodge cancel, weapon switching and hit-window alignment using current native gameplay captures. Do not change clips or controllers without a reproduced issue.

Batch 002: environment validation scheduling only. No character, camera, combat, clip or controller changes. No new visual animation observations or smoother-animation claim. Prior 30 animation contracts were not redundantly rerun for this unrelated source change.

Batch 003: footprint serialization only. Native fixture verifies numerical alpha/expiry, not visible footprint style or character animation. No art, animation, camera, weapon contact timing or controller change. Next substantive gameplay batch should prioritize current input/combo/dodge/charge/landing smoke in a small isolated character fixture before animation tuning.

Batch 004: current native actor/controller/weapon matrix exposed and repaired total-control ownership bypass in melee. Tested real Animator states and collider penetration, with directly delivered gameplay callbacks. No clip, blend duration, root curve or controller asset changed; no visual smoothness, frame-by-frame animation or physical-keyboard acceptance claim. Unlocked starts, input-buffer expiry/consumption, duplicate callback prevention, contact damage/deduplication and movement-only mount behavior pass. Natural frame-driven combo, dodge, charge and landing transitions remain the next visual/input review.

Batch 005 accepted take02 uses queued synthetic device events through actual PlayerInput, normal player frames and Animator evaluation, without direct attack calls or phase injection. Twenty-eight checks / 1,258 native frame observations pass across stick and twin blades: three-stage combos, recovery evasion, moving charge/release, simultaneous jump slash and landing recovery, control-lock input, keyboard weapon switching. This establishes current normal-flow responsiveness contracts. No rendered frames were captured or visually reviewed, so smoothness, clipping, silhouette, camera quality and final artistic approval remain unobserved. No game animation/source/controller change was needed.

Batch 006 removes measured late evasion from expired input, without changing clips, blends, root motion or authored cancel phases. Queued native input reproduces 267/366 ms stale dodge consumption against the 220 ms configured lifetime, then verifies expiry, valid recovery evasion, temporary stamina readiness and native guard/spin exits after repair. Both 30/60 Hz simulation deltas are tested; these labels are not measured display frame rates. No new visual smoothness/clipping observation is claimed. Rendered startup/landing/recovery review remains pending.

## Batch 007 — native rendered pose review and queue semantics

Reference: existing Sahur V278 jump/V272-V276 combat implementation; no replacement reference or new artistic approval. Stage: focused QA of existing animation. Native SRP captures use the real prefab/Animator and queued PlayerInput, 640x480, every four frames at a 60 Hz simulation delta (display FPS unmeasured). Accepted take batch007-review02-before contains 87 captures covering idle, first combo startup/recovery and moving jump chop/landing for both weapons. The isolated fixture uses its own follow camera, distant noncontact target and flat floor. This does not assess the gameplay camera, boss contact or full scene lighting.

| Selected raw render | Native frame / phase | Observation |
|---|---|---|
| render-0013.png | Stick combo, frame 129 | Body and extended stick remain visible in the sampled swing pose. |
| render-0033.png | Stick jump, frame 244 / .471 | Bent knees and forward/downward two-hand stick pose visible in the contact interval. |
| render-0038.png | Stick recovery, frame 264 / .863 | Lowered legs and rising weapon; native trace reports grounded. |
| render-0044.png, render-0049.png | Blades idle/startup, frames 546/566 | Both held blades visible from the oblique view. |
| render-0055.png, render-0060.png | Blades combo, frames 590/610 | Forward swing then upright recovery poses visible. |
| render-0077.png | Blades jump, frame 714 / .471 | Bent knees, two separated blade directions visible. |
| render-0082.png | Blades recovery, frame 734 / .863 | Legs lower and blades return toward torso; native trace reports grounded. |

Files above are under Evidence/native-input/batch007-review02-before/renders/. No gross missing-body/weapon rendering observed in these selected poses. Sparse stills do not establish temporal smoothness, weight, clipping freedom, foot-contact precision or camera responsiveness; those scores remain N/A. No clip/controller/blend/root curve was changed. Original .85 s jump and authored hit interval remain unchanged.

Queue stress passes: actual traces show early intent cleared at phases .105/.100 before queueOpen .12, with no next combo stage. Inputs admitted at phases .313/.320 persist beyond their initial lifetime as designed. The initial single-stop failing assertion was a harness mistake, retained and explicitly rejected; it is not animation snapping evidence.

## Batch 008 — Boss attack ownership on target loss

QA-011 repairs state/presentation ownership when Nailong's player target becomes inactive. Before: imported animation graph continues in Attack and a queued glob releases. After: AI enters Idle and the graph and pending release stop; native frames confirm the state remains idle, and an active target can be reacquired.

Baseline cancellation was already correct for heavy-hit stagger, death, AI disable and return: five imported attack patterns have real action mixer weight above .5 before cancellation; 75 checks pass before and after. NailongAttackMotion.Stop already delegates to NailongPhaseAnimation.Stop, so no new direct graph teardown or animation replacement was introduced. Additional tests preserve launched projectile/wave behavior while cancelling future emissions; death cleans remaining effects.

No new gameplay images/video captured. This is native graph/state and effect-lifetime evidence, not an observation that animation looks smoother. Smoothness, contact, silhouette and blend appearance remain unscored. No art, clip, controller, cue timing, boss damage, root motion or combat design changed.

## Batch 009 — Cappuccino disable/re-enable action ownership

QA-012 stops an interrupted sword action from resuming into an immediate damage window after its AI or whole object is disabled. All three attacks now require a new action/telegraph; regular fresh attacks retain their 7/11/12 native fixture damage. Logical cancellation is immediate, while locomotion pose recovery waits for the next valid Tick so an inactive Animator is never crossfaded. The initial immediate-pose version generated warnings and was rejected; final native tests record zero inactive Animator warnings including teardown.

Existing user edits to ultimate pacing, fixed health, healing and immobilization are preserved byte-for-byte. Regression phase-clock checks confirm those numeric contracts and death ownership, without claiming an entire real-time ultimate encounter. No clip, controller, root curve, authored attack window or original art changed. No new rendered evidence was captured, so blend appearance and smoothness remain unscored.

## Batch 010 — expanded aircraft, camera and player QA

Native settings confirm flame emission stops when Bombardino is disabled or its encounter loses a valid target. This is emission/ownership evidence, not a visual improvement observation. No new image/video captured. Ordnance flight follows the existing 25 m/s² gravity using time-preserving swept steps; original bomb/missile damage, steering rules, fire height/cadence and boss art remain. No animation clip/controller changed.

Camera feedback tests cover 100 additive apply/remove cycles without base-pose drift, pause lifetime preservation and disable cleanup. They do not establish gameplay follow-camera responsiveness, collision behavior or perceived shake quality. Footprint alpha, renderer property independence, lifetime/expiry and 28-stamp pool pass. A failed allocation positive control prevents any performance benefit claim or source optimization.

Current native queued input integration passes 28 assertions / 1,256 observed frames for both weapons, including complete combos, recovery evasion, moving charge/release, jump attack/landing, total-control ownership and switching. No current visual smoothness/weight/clipping acceptance is implied by these numeric/input checks.

## 2026-10-28 — Batch 011 — UI and performance

No animation clip/controller/root-motion or combat timing changed. Footprint fading alpha, property isolation, pool/lifetime and actual reload pass. UI evidence verifies equipment/map ownership, reopen, pause and camera-mask restoration; no new gameplay render/video, visual smoothness or whole-game FPS claim. Native profiler marker timings are an isolated workload (2.351 to .184 ms per 1,000 calls), not frame-time acceptance.

Batch011 fixes accepted and synchronized: Git 3a4ca40, GitHub main via PR #1/c6d8146, Plastic cs192. No visual animation acceptance added.

## 2026-10-28 — Batch 012 — UI control ownership

No clip, blend tree, animator controller, root-motion or attack window changed. Queued keyboard input exercised the real Sahur preview with equipment/map/pause, death and respawn. This validates modal/input/time/camera ownership, not rendered UI layout or motion smoothness. No new visual acceptance or frame-rate claim.

## 2026-10-28 — Batch 013 — restored death recovery and bounded aircraft steps

Death-reload evidence verifies retained disabled-component identities, the original canvas/button, restored combat/movement enables and preservation of an originally disabled component. It does not establish locomotion/attack blend appearance after respawn. No clip/controller/root-motion, authored attack windows, art or animation speed changed.

Aircraft authored low-pass tests retain default cruise altitude (55), use SpeedRun with 2x visual scale, and query native body/floor/ridge/player capsule penetration at sampled tick boundaries. Positive overlap controls pass; accepted paths have zero overlaps. This is collision/time evidence, not a filmed full boss encounter or visual smoothness claim.

## 2026-10-28 — Batch 014 — warning pose preservation and death during actions

Coalesced warnings retain committed target, heading, strip start and sweep from the scheduled simulation step. Native snapshots for six attacks × telegraph/cross-to-attack/target-loss/death have exactly zero point deviation; visibility, state and emission counters match. No original art/controller/clip or attack timing changed.

55 native action/death/respawn checks pass over 617 observed frames: stick/blade charge, throw windup/flight, blade rush and combo. Controls, damage gate, charge layer, grounded movement, eventual locomotion graph and finite near-player camera are verified. The .42 s Land interval remains intentional and unchanged. No new images/video captured; smoothness, clipping and visual weapon recovery are not accepted from state flags alone.

## 2026-10-28 — Batch 015 — camera collision regression

No animation/art/controller/timing edits. Actual smoothed camera position now checks side obstructions missed by the desired-pose ray. Four-heading native collider probes verify separation; no gameplay images/video were captured, so perceived camera smoothness is unscored. Native action/death/respawn regression passes 55 checks / 620 frames after the camera edit, including eventual locomotion and finite near-player camera.

## 2026-10-28 — Batch 016 — rendering lifecycle and slope coverage

No production animation/controller/camera/art changes accepted. Native foliage submissions drop from 4 to 0 after actual script reload; transient initialization and ownership candidates failed and were fully reverted. Diagnostic PNGs lack a qualified rendering control and do not support visual acceptance. Native Sahur capsule lands and activates floor checkpoints on four slopes, with foot-to-ground distances .015/.031/.085/.153 m; this does not assess animation foot contact visually.

## 2026-10-28 — Batch 017 — restored rendering ownership

QA-030 is repaired with native object/state/submission evidence: 636 renderers retain original material and mesh IDs, including one intentionally hidden renderer with TwoSided shadows and occlusion disabled. Map-style camera masking stops submissions and restoring the mask resumes them. No art/animation/controller/camera/combat edit or new qualified image/video; this does not establish perceived visual smoothness or whole-game FPS. Diagnostic PNGs remain unqualified.

## 2026-10-29 — Visual Style System V2 compatibility

No models, UVs, rigs, clips, Animator controllers, colliders or combat timings changed. Native keyboard/mouse drives Sahur movement/melee in all four modes. Each mode changes its look during a real active action without changing same-frame actor position, Animator state or normalized time. Six existing Chicken Bro skeleton poses advance during live profiles. Actual pressure rings remain visible in Origami/Monochrome with reversible off/on pixel controls. This establishes rendering/state compatibility, not smoother animation or full boss anticipation acceptance. Matched first-island previews use identical frozen poses; runtime CombatTest captures and composited UI are retained under Evidence/visual-style-v2.

## 2026-10-28 (Vancouver) — Batch018 — foliage CPU optimization

No animation, camera behavior, clip, controller, gameplay or art change. The original and optimized foliage math produce identical classifications across16 actual camera configurations, including custom culling matrices, with unchanged property-array contents. Native first-island coast renders remain populated; water/weather times differ between captures, so images are not a pixel identity or animation-smoothness test. No new combat animation acceptance is claimed.

## 2026-10-28 (Vancouver) — Batch019 — standalone camera/AI evidence

No authored animation, clip, rig, collider, model or normal camera behavior change. Actual standalone coast/forest/boss-vicinity PNGs show Sahur, native environment and the boss; they are fixed-camera diagnostic views with a protected player and camera RT output. This does not qualify overlay UI, full encounter anticipation, moving-camera paper stability or smoother animation. Diagnostic target restoration no longer forces a disabled camera on; verified natively.


## 2026-10-29 — Batch020 — chicken contact and duck scale

Actual native roaming/Idle PNGs and motion0/80/160 inspected. Chicken's rapid back-and-forth patrol changes are removed (158→2 reversals across separate240-interval runs), and cooldown uses Idle while gravity settles. Visual Y lowers0.65m, without changing chicken clips/rig/controller/collider.168 phase comparisons across8 states preserve pose and shift sole height exactly0.65m. Qualified native Idle sole gaps:chicken0.06729m,duck0.03435m. Walk still ranges-0.11088…0.14358m; precise foot planting is not complete.

Duck grows40%; Walk/Run state speeds divide by1.4 to retain world stride rate at unchanged travel speed. Attack/reaction clip speeds and timing remain. No original art/animation asset overwritten. Rendered-proxy and1m translation controls qualify sole measurements; false-scale/CPU-unreadable results rejected. Original spherical hit volume retained after correcting a scaled-sole fixture error. Full island swarm, long combat and exact foot IK remain outside this batch. See Evidence/batch020 and Documentation/Enemy-Batch020.md.


## 2026-10-29 — Batch021 — moving mixed flock in real terrain

Native1280×720 Realistic/Dark Fairytale/Origami/Monochrome PNGs inspected after90 native frames per mode with an orbit camera.5 chicken+3 enlarged ducks animate/chase/attack on actual island terrain. Motor soles stay0.0508461–0.09234619m above sampled ground. No animation/controller/art or gameplay change. A protected capsule target substitutes for Sahur input; these captures do not qualify whole combat, precise mesh foot planting or flicker-free temporal behavior. Residual batch020 toe intersections remain. See Evidence/batch021 and Documentation/Flock-Batch021.md.


## 2026-10-29 — Batch022 — standalone moving-camera matrix

Four styles × coast/forest/boss vicinity,6s per phase, with8°/s orbit and real render fences. Four native1080 PNGs inspected; actual Sahur/world/boss visible. Proof captures pause orbit and compare visible/empty pixels outside measured windows. No authored animation/rig/model/controller/combat edit; camera changes only under opt-in diagnostics and teardown restores policy/style/pixel. This is not full combat, overlay UI, exact feet or flicker-free temporal acceptance. Sequential boss actions differ, so style rows are not shader-only comparisons. Batch020 residual foot contact remains open.


## 2026-10-29 — Batch023 — startup scheduling and wind preservation

No model/UV/rig/clip/controller/collider/camera gameplay edit. Only fresh wind/renderer scan scheduling changes;6,420 scalar wind profile values match exactly across641 authored/owned renderers under fixed/adaptive policies. Actual reload/lifecycle/cancellation preserve original material/mesh/visibility/shadow/occlusion state. No new visual-smoothness or temporal-flicker acceptance; existing small chicken toe intersections remain open. Four styles/independent pixel mode and batch020 duck size/collision/stride remain unchanged.


## 2026-10-29 — Batch024 — frozen forest render controls

Native final full-recovery/half-a PNGs inspected:actual Sahur,terrain,grass and trees at the same forest pose. Output1080p,temporary renderScale1/.5 controls;scaled simulation time/camera stay fixed. No animation/model/rig/clip/collider or normal camera edit. Numeric/native proof does not establish smoother animation,flicker-free movement,whole combat/UI or precise feet. Camera/time/quality ownership restores;all4 styles/pixel and chicken/duck fixes retained.


## 2026-10-29 — Batch025 — native CPU capture only

Only optional binary profiling/phase labels changed;no animation/model/rig/controller/collision or normal camera behavior edit. Native paired frozen forest renders pass7 existing pixel controls,without new temporal-smoothness/whole combat/UI/precision foot acceptance. Raw CPU markers qualify timing,not animation appearance. Styles/pixel and chicken/duck repairs remain;small toe intersections remain open.


## 2026-10-29 — Batch026 — targeted duck controller polish

RevisionLongLeggedDuck-controller-batch026;existing design,user-requestedPOLISH. Before native sampleframes0/4/10/20/29 stayedRun at slow/zero travel. Final measured planar speed selects actualWalk/Run/Idle,acceleration damping.12s,zeroSpeed/Motion0 from first stopping frame;actualIdle settles after blend. Controller locomotion blends.16s,Walk/Run rates.8428571/.8 compensate extra1.5× size. Attack/reaction rates/windows/rig/FBX unchanged;default chicken policy preserved.

Before/after size+UI and finalWalk/Idle PNGs inspected. Silhouette and caption readability observed;continuous smoothness/weight/anticipation/impact/exact-foot stability are unscored/not fully observed. No perfectfootIK or human FINAL/APPROVED claim.31 native checks pass for targeted mechanics/layout;next actual island terrain,mixed enlarged flock and precise foot contact. See Documentation/Duck-UI-Batch026.md and Evidence/batch026.


## 2026-10-29 — Batch027 — grass render continuity

No character/enemy animation,rig,collision or original grass geometry edit. Grass native LODs now use reversible distance fades;forward/depth/shadow share the same URP clip. Wind behavior is preserved. Frozen isolated grass20-frame hold,rotation-return and dolly-return each produce0changedpixels;this is a render-control result,not a general wind/character animation smoothness claim. Actual4-style moving-camera images inspected;pixel smoke accepted. Preserve all026 duck size/gait/UI changes and remaining fine-foot-contact work.


## 2026-10-30 — Batch028 — grass layout, not wind

Grass retains its original complete mesh at all visible distances; camera movement no longer selects thinner blade layouts. Wind, source geometry/materials and actor animations/rigs/collisions remain unchanged. Frozen isolated hold/turn-return/dolly-return each0 changed pixels; old near/mid model thresholds also0. Do not infer general wind or character-animation smoothness from this. Four-style moving camera images and pixel screenshot inspected; full-game pixel invariance is not claimed.
