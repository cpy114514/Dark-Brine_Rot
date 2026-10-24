# Opening uses the normal gameplay club charge — v242

User requested that the opening's charged attack match ordinary Sahur gameplay. This supersedes the opening-only two-handed overhead pose for the tutorial/counter; the scene's narrative sequence and original source actions remain.

## Implementation

- Read the actual live club attack's existing `chargeClip`, held phase, full-charge duration, damage window and combat-feel speed curve. Current contract is **SahurStandingMeleeDownward**,2.266667s,held phase0.28,full charge1.8s,active window0.35–0.43.
- Layer that same complete humanoid clip over the existing supported guard with the exact gameplay right-arm AvatarMask. The runtime Resources mask is an API-created copy of the existing mask; original clip/mask/controllers remain untouched. Left arm/body/legs/root remain on the base pose. There is no generated replacement skeletal animation.
- Tutorial hold uses the normal smooth held-phase progression. Release resumes from the held phase, follows the normal phase-dependent speed curve, finishes the authored recovery and fades over the existing0.18s gameplay layer release time.
- The manual cinematic clock integrates a cached256-interval phase/time table once. Temporary contact previews and paused input gates sample poses without advancing or restarting a clock. Root movement, foot support and menu/input ownership remain with the existing director.
- Fit the tutorial shark against the actual shared attack's active-window midpoint, then confirm its real shaft/head contact after native solid solving. The midpoint currently0.39 is inside the normal damage window, rather than reusing the retired overhead's1.21s cue.
- Later charged counter and feint windup use the same single-hand clip/mask/timing. Counter keeps its established5.3s story contact cue by starting release at the matching earlier instant. Story bite/dodge/throw/tail/flight timings and the nonlethal health budget remain.

## Verification

`Tools/AuditOpeningChargeV242.cs` passes49 asset/controller/mask contracts: all three existing controllers share the normal charge/release clip; every humanoid mask flag matches the cinematic resource; the left arm and root are excluded.

Native input/camera/geometry verification is recorded under `.codex/opening-charge-20261023/`. Deterministic simulation rate is not hardware FPS. Previous v241 direction/ownership fixes and user scene/save changes are preserved.


First `native-30`: shared charge animation,1.8s hold,actual0.39-phase hit and all13 charge checks pass, but the independent film geometry check rejects one tail-contact frame (body envelope gap-0.5784m). Water-exit planning temporarily sampled the unconscious clip and restored only the base Guard, losing the live right-arm charge layer (weight1→0; right upper arm61.60deg/right hand71.07deg jump). Preserve the charge layer's exact weight/time while restoring the prior base sample, before rendering or further contact work. The rejected take and probes are retained. Add explicit contact-frame layer and right-arm continuity checks for the final replay.

An isolated native graph passthrough check covers the unaffected original opening idle, airborne hit and rise at five source phases each (15 samples). With charge weight0, the new wrapper produces exactly0deg maximum bone rotation difference and identical local positions to the old base graph. This supplements the recording, which started at the Move tutorial and therefore does not provide new crush-camera footage.

Second `verified-30` covers the original crush through shore handoff (2489 native rows). All15 charge checks pass: at the tail contact the right-arm layer remains1 and upper-arm/hand rotation changes are below0.04deg. However the separate geometry audit still rejects that contact frame (-0.5750m conservative gap). The same hypothetical fall preview also leaked enlarged forearm collision radii into the currently held charge pose, after its real tail fit. The planner now owns a separate radii array and restores the live array, composite pose and capsule snapshot in `finally`. The prepared larger envelope is adopted when the actual actor transitions into flight/drift; trajectory prediction still uses the full fall envelope. No clearance threshold is relaxed. Both rejected takes remain as diagnostic evidence.

Release layer fading uses a smooth cinematic curve over the existing gameplay0.18s fade duration; it is not a claim of identical per-frame gameplay fade weights. Charge clip, right-arm mask, hold progression, release phase/speed and damage interval are shared with gameplay.

## Accepted native replay

Frozen sources in `isolated-preview-source-hashes.json` match the final files. `final-30` records2605 native rows and2604 actual gameplay-camera frames, from the original crush through unconscious drift/shore handoff. All17 charge/preview-ownership checks and23 independent contact/geometry/continuity checks pass. Native hold reaches1.803063s against the1.8s gameplay requirement; heavy contact is at phase0.39,0.188280s after release. Both tutorial hits and all three film contacts register once. Original shark scale remains3;13 native wood fractures occur; final health remains70.

Independent fresh mesh probes at both tutorial contacts pass weapon/head contact, foot support, shark/body and shark/wood separation. The79 later-film posed-mesh/body-envelope probes have minimum body gap0.029313m and zero sampled shark/wood triangle intersections, including the previously failing tail contact. The real flight retains the prepared larger fall envelope. The33 board-relative walking samples remain forward (minimum visual alignment0.671323 during heading blend); this is a regression observation, not a new direction redesign.

Reviewed native single-hand tutorial hold/release, cinematic counter and tail-contact consecutive frames, including actor side views and final gameplay shots. Silent complete video: `final-30/SahurShark-final-30.mp4`. No claim of normal-speed audio review, human final artistic approval, every-frame/every-layout clipping freedom or low-end hardware FPS. Existing wave/timber occlusion remains a shot-readability limitation. The cached charge phase table and one-time separate preview envelope introduce no per-frame mesh baking or per-frame table allocations.

After confirmed Editor Stop, the fresh pre-test human save was restored byte-for-byte and the saved Story1 hash remains unchanged (`final-30/preservation.json`). Removed the temporary missing-script diagnostic object left by the early pre-Play request; no user object or scene was saved. Existing Editor is stopped/unpaused, scene clean, Console errors0. No second Editor/Player, build, commit, push, UVCS operation or automation.
