# Animation review

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
