# Movement and combat feel — verified revision

The existing CharacterController, response presets, animation clips, weapon sweeps, contact feedback and camera already provide responsive control. This revision addresses reproduced downhill support loss, moving twin-blade leg sliding, and repeated-contact freezes. It preserves existing speeds, combat costs, hit windows, combo commitment, heavy-rush ownership and the canceled execution feature's removal. See `GameplayFeel-Audit-20261109.md` for architecture and baseline.

## Changes and causes

- A short predictive nonalloc ground probe supplies missing descent to the existing single collision-resolved Move. At sprint speed the old fixed downward velocity could not follow a steep slope. Rising jumps, airborne movement, external owners, rolls, soft-island traversal and moving decks exclude this probe. Missing ground or a drop outside the configured range remains a fall.
- Ordinary twin-blade combos now accept WASD/sprint translation before or during a swing. Root-motion translation is disabled only for these combos; the controller owns travel. Existing slash arms/torso remain authored animation. A separate legs-only layer in all three existing controllers blends reused walk/jog/directional clips under moving ordinary slash stages0/1. The rotating finisher and other special attacks keep their original legs and priority.
- Repeated contacts share a bounded local hitstop budget followed by real resumed playback. A moving blade slash freezes its attack speed parameter while its leg layer continues; rotating attacks retain whole-body hold. Public cancellation/death/unequip Release preserves its immediate semantics. No global time freeze was added.
- Fresh inspection found actual sprint×1.85 versus the locomotion tree's16.8m/s calibration. A locomotion-only rate parameter follows excess actual speed (about1.057 at17.76m/s), keeping the authored scene speed. Attack/dodge playback parameters remain independent. Controller metadata is cached on controller change.

## Measured checks

All evidence is local in ignored `.codex/gameplay-feel-20261109`; it includes private save snapshots and must not be uploaded wholesale.

| Same-condition40° downhill sprint, current×1.85,60Hz | Probe disabled | Enabled |
|---|---:|---:|
| Supported samples over2s |24/120|120/120|
| Maximum capsule-bottom/ramp gap |.272m|.155m|
| Horizontal travel |33.545m|33.559m|

The remaining gap includes the round capsule's geometric separation on a slope; it is not visible sole clearance. At30/60/120Hz current-speed enabled probes retain60/60,120/120,240/240 support. Earlier controlled×1.75 results are preserved separately and are not passed off as the exact current scene.

Native input/rendered tests passed:

| Route | Simulation / timing | Assertions |
|---|---|---:|
| Flat movement, jump, dodge, club/blade combos, camera and lock |30/60/120Hz|20 each|
| Original weapon charges, spins, guard, rifle fire/zoom, walking aim and sprint fire |60Hz|18|
| Late dodge/jump handoffs and active-preset transition |30/60/120Hz|4 each|
| Combat actions using real elapsed frames |60Hz target, no captureDeltaTime|12|
| Actual Island1 terrain using copied Gameplay controller settings |60Hz|20|
| Actual Island1 impacts, script locks, pause, camera handoff, low gravity and ocean/shore |60Hz|14|
| Expanded native twin-blade movement/contact/spin regression |3 controllers,48 directional cases|929|
| Direct no-snap guards |Jump/ascent/air/lock/ledge/long drop|6|

Actual-island60Hz input observations: movement begins on the first evaluated frame, reaches90% walk speed in~117ms, stops in~100ms/.370m, reverses sprint direction in~150ms, and accepts buffered late club/blade dodge after~150/~83ms while preserving commitment. Mouse input's requested30° reaches the camera on that evaluated frame. These are software input-to-evaluation measurements, not hardware-to-display latency. Existing responsiveness was already good; these numbers establish retained behavior, not an invented improvement over baseline.

All three rig masks leave the sampled weapon upper-arm/hand rotations unchanged while moving the feet. During moving-blade contact the attack phase stays fixed and the leg phase advances. The finisher disables ordinary footwork. Real accepted damage and per-swing deduplication pass. Original camera collision and cinematic handoff pass (largest observed handoff displacement.078m/frame). No runtime errors/asserts occurred in accepted review takes.

Warmed main-thread microbench,1000 calls per method: added ground follow~2.78μs/call, response~.078μs, idle footwork update~.569μs; each measured0 managed bytes. This excludes cold component caches, input/review allocations, whole-frame GC, rendering and GPU time. It is not a60/120FPS shipping claim.

## Visual comparison

Local `blade-before-after.mp4` (left: existing slash legs; right: new moving leg layer) contains identical60Hz input scripts on actual Island1, sampled15fps, without audio. `blade-comparison-contact.png` provides matched frames. Only this revision's new footwork/ground-follow/cadence switches are disabled for the left take; previous control tuning and the approved moving-attack behavior remain enabled. Existing sky/water time continues, so it is not a frozen pixel comparison. Reviewed matched frames show a walking/running leg cycle while the authored slash torso/arms continue.

## Tuning and rollback

The existing `Assets/Resources/Combat/SahurCombatFeel.asset` remains the one configuration:

| Setting | Current recommendation | Purpose |
|---|---:|---|
| ControlResponse.groundFollowDistance |.6m|Maximum short downward support correction;0 disables|
| ControlResponse.mobileBladeFootwork |true|Independent reuse of legs under ordinary mobile slashes|
| ControlResponse.matchOverspeedStride |true|Cadence above the existing tree's calibrated top speed|
| maximumContinuousStop |.13s|Shared contact burst budget|
| hitStopResumeSeconds |.035s|Minimum resumed playback between bursts|
| Existing inputBuffer |.26s, retained|Keeps legal late handoffs across existing commitment|
| Existing jump buffer / coyote |.15/.10s, retained|No speculative retuning|

The locomotion rate is clamped1–1.8; source states and neutral rate remain1. Control tuning and individual additions can be disabled for comparisons. Native controller changes are reproducible with `Tools/InstallMobileBladeFootwork20261109.cs` while stopped; it saves only owned controllers/profile. Before files are in ignored `source-before` and `controllers-before`. Scoped Git phases: audit9044089a, ground/legs7e36fbf0, bounded stopb2f603d8; integration and final reporting follow separately.

Editor returned stopped to the original clean Second Island setup. Verification reports formal save bytes unchanged and scene setup restored; physical input/settings and fixture cameras/scenes are released. No new EXE, remote push or rendering-style changes are part of this gameplay revision.

## Practical limits and human review

This reuses existing Humanoid clips; it is not newly hand-authored full-body locomotion, motion matching, universal terrain foot IK or a promise of zero sliding on every surface. A human play pass should judge the new legs against the torso, readability during repeated hits, and preferred response preset. The focused tests exercise real systems and actual Island1 terrain, but do not prove a full campaign/all-Boss completion, every moving platform or hardware latency. Camera/shake and sounds were retained after integration checks rather than amplified without evidence. The separate rendering measurements are in the [Van Gogh 2.0 report](VanGogh2-Results-20261109.md).
