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
