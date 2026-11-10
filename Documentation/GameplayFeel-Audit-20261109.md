# Gameplay feel overhaul — Phase A audit

Branch: `codex/gameplay-feel-20261109`. Existing unrelated work is retained. Source/profile backups and private verification evidence live in ignored `.codex/gameplay-feel-20261109`; do not upload saved-game snapshots.

## Existing architecture and scope

Unity6000.3.23f1, URP17.3, Input System1.20, CharacterController. No Cinemachine dependency. The existing control profile and three AnimatorControllers already supply response presets, directional lower-body blending, input buffers, local hitstop, real contact feedback, camera collision and accessible shake controls. Preserve them rather than introduce a second movement/feedback owner. Normal movement runs in the controller; the root-motion relay forwards only authorized attacks; twin-blade heavy rush has its own bounded collision-driven interval. Execution was canceled by the user and must stay removed.

The user's prior ordinary twin-blade movement change is already present and separately validated. This overhaul will preserve it and improve synchronization where justified.

## Reproduced baseline

`Tools/AuditGameplayFeel20261109.cs` records actual profile/controller configuration. `Tools/ProbeGameplayFeel20261109.cs` exercises a native scaled Sahur on isolated collider ramps using actual input/controller methods at fixed30/60/120Hz simulation steps. It uses the established Island1 movement configuration (9.6m/s, sprint×1.75, acceleration68, braking84, turn780 before profile multipliers). It also repeatedly calls local hitstop on a real animated rig, with real unscaled time and evaluated frame progress.

| Before | 30Hz | 60Hz | 120Hz |
|---|---:|---:|---:|
| 40° downhill sprint grounded samples |23/60|29/120|39/240|
| Maximum capsule-bottom gap above same ramp |.222m|.230m|.229m|
| Animated frames recovered during repeated-hit probe (~.39s) |0|0|0|

The capsule-bottom gap includes the geometric separation of a round capsule on a slope. It is not a direct measurement of visible sole clearance. Grounded samples expose the intermittent support; the lower-angle/flat and walking cases are controls. These are fixed-step native integration probes, not measured hardware FPS or a full playthrough.

Root causes:

1. Grounded state relies on the last CharacterController.Move result. A fixed five-unit downward stick velocity cannot follow a steep descent at sprint speed, so the actor repeatedly falls and reconnects. A short predictive ground-follow probe can supply the missing downward displacement while leaving jump/gravity and collision authoritative.
2. Hitstop clamps each new duration but extends the end relative to every new contact. Repeated contacts can renew a supposedly bounded local freeze indefinitely. A bounded burst must include an actual animation-resume interval; clamping each hit alone is insufficient.

Animation inspection also confirms the existing Lock-on Footwork mask affects only Left/RightLeg and FootIK, leaving Root, torso, head and weapon arms intact. Mobile ordinary blade slashes currently do not use it, so a controlled extension can be evaluated without replacing slash clips or changing damage timing. Spin/finisher/rush/air/story states must keep priority.

## Planned scoped phases

- B: speed-aware, short, downward-only slope following; validate jumps, ledges, stairs, walls and soft-island exclusions. Evaluate existing lower-body blending for mobile blade slashes and its actual stride timing.
- C: bound repeated hitstop and retain speed/layer ownership, inputs, audiovisual confirmation and boss poise. Keep current good weapon behavior; do not strengthen everything indiscriminately.
- D: integrate with club/twin/rifle, dodge/counter/jump/charge, story and camera; fix only reproduced handoff/collision problems.
- E: fixed-step regression, actual Island1 input/rendered review and comparisons; publish measured results, parameters and remaining human feel decisions. Commit scoped stages separately.

Research informing priorities: coordinated hitstop, sound and camera are associated with impact feel in [Lin et al.2022](https://arxiv.org/abs/2208.06155). Native collision ownership follows [CharacterController.Move](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/CharacterController.Move.html); animation blends use the existing [Blend Tree](https://docs.unity.cn/6000.2/Documentation/Manual/class-BlendTree.html) architecture. These sources motivate implementation choices; the table above comes from this project's own tests.

## Phase B implementation and targeted validation

Added a short predictive downward-only sphere probe for previously supported movement. Its maximum follow distance is.6m in the existing ControlResponse profile. It only supplies missing descent to the controller's existing single collision-resolved Move. Upward jumps, authored root travel, airborne states, rolls, moving decks, soft-island motion, external locks and tutorial movement remain excluded. No new position/velocity owner or per-frame allocation was added.

At60Hz the same40° sprint probe now reports120/120 grounded samples, compared with29/120 before. Horizontal travel remains31.85m over the two-second probe; the maximum capsule-bottom separation falls to the ordinary round-capsule contact value of.155m. Flat and30° controls remain supported.

The three existing controllers now have an initially weight-zero Mobile Blade Legs layer using the existing legs-only mask, calibrated forward walk and jog clips and existing directional footwork. It activates only for moving, grounded ordinary blade slash stages0/1; spinning finisher/other special states retain priority. Actual capsule velocity drives its directions. Blade slash base states keep their original clips, mirror and speed; an existing LightPlaybackSpeed multiplier is enabled at its neutral value1 to permit later attack-only hitstop while legs continue.

Native rig checks on all three controllers confirm active walking legs at full blend, zero change to the right upper-arm and hand local rotations at the same frozen attack phase, and a changed foot pose. The leg layer disengages for the finisher. This proves mask ownership; final rendered stride/transition review and30/120Hz regressions are still pending.

## Phase C implementation and targeted validation

Repeated contacts share a maximum continuous hold budget of.13s measured from the first hold, followed by at least.035s of restored playback. Public Release still immediately releases ownership and clears its grace, allowing explicit cancels/death/unequip to keep their original handoff semantics. Only Animator playback or the existing attack speed parameter is held; global time and input/movement remain live. New settings are in the existing CombatFeelProfile.

For an active Mobile Blade Legs layer, local stop uses the existing LightPlaybackSpeed parameter: ordinary slash contact holds its attack phase while the legs continue. Rotating finisher/continuous attacks retain whole-body stop. Native checks on all three controllers show zero attack-phase change and positive leg-phase change during the same hold, with Animator.speed remaining1.

Final30/60/120Hz targeted slope samples are60/60,120/120,240/240 for40° sprint. Repeated-hit probes at each rate now observe restored animation frames; their no-hold controls also prove the rig animation clock is running. The expanded movement/contact/spin regression passes929 checks/48 directional cases, including real accepted damage and no duplicate same-swing feedback. These targeted native tests are not a substitute for the remaining rendered and actual-island review.
