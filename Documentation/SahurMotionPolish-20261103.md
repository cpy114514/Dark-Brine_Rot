# Sahur movement, combat and story transition polish

Targeted polish of existing actions in Unity 6000.3.23f1. The user requested all three areas. Existing clips, animation controllers, movement speeds, jump physics, stamina rules and damage windows remain authoritative.

## Changes

- Locomotion Speed now follows the capsule’s actual horizontal displacement after collision resolution, with quicker, bounded parameter damping. It no longer continues a full walk cycle while blocked against a wall or tutorial boundary.
- Moving landings retain initial impact compression, then return the legs to locomotion. Recovery scales from 0.14 to 0.24 seconds with impact strength; stationary landings keep the existing 0.42-second recovery. Landing-impact history is reset on platform/saved-pose handoff.
- Club and twin-blade charge cancellation retain the evaluated arm pose while its layer fades. No damaging attack is emitted by cancellation. The internal release flag is named fadingChargePose to cover both weapons.
- FinishCinematicPose retains an already-active/incoming locomotion pose. Normal RestoreSavedPose retains its existing save/respawn behavior. Both island get-up components use the cinematic path; FirstIslandArrival also uses the existing smooth camera handoff after releasing its controls.

## Measured native verification

The existing Editor ran real input/collision/Animator takes using a GameSaveExcluded copy of the production Sahur prefab, at 60 simulation Hz. Original first-island actors were inactive only inside the disposable Play session. Original authored scenes were not saved.

- Baseline: moving recovery 0.4167 seconds; against a wall, animation Speed 9.5999 while real speed was 0.00366. A cinematic idle at phase 0.6 restarted at phase 0.
- Controlled after take: 11 checks, 431 recorded samples. Moving recovery 0.1833 seconds, blocked Speed 0, both cancellations active and fading with maximum per-frame weight decrease 0.1389, no damaging release, idle phase 0.6 → 0.68, zero recorded runtime errors.
- Actual first-island terrain after take: 11 checks, 431 samples, all pass. Moving recovery 0.1333 seconds, blocked animation Speed 0, real speed 0.000114. Native walk/landing/charge screenshots inspected.
- Actual FirstIslandArrival and SecondIslandAwakening components: 7 checks, 886 samples, all pass. First-island control release retains locomotion; maximum sampled camera displacement over the eight handoff frames is 0.1918 m. The second get-up and real SPACE-driven dialogue run through input release without an unintended jump. This component test uses the first island’s real shore and excludes saving; it does not certify all second-island layouts or the full aircraft/Boss campaign.
- The three shared AnimatorController assets are unchanged. Final C# recompile reports no errors.

The first baseline’s charge checks exercised an early heavy release rather than cancellation and are excluded from cancellation evidence. The corrected after takes explicitly enter a real charge, invoke cancellation and verify the non-damaging fade. The initial terrain take failed before input injection because Unity destroyed its temporary default InputSettings on replacement. The fixture now snapshots/restores settings by JSON, recreates destroyed transient defaults and restores physical keyboard/mouse devices. The extra island-final label was still a flat-floor retake; only island-confirmed is actual terrain evidence. These superseded/failed takes remain private and are not counted as passed terrain checks.

Evidence: `.codex/sahur-polish063/`, including before, after, island-confirmed, story, restored.json and verification-summary.json. Original source rollback copies are under rollback/ and include all pre-existing user changes. Reusable native review helpers are Tools/ReviewSahurMotion063.cs, Tools/ReviewSahurStory063.cs and Tools/WorkspaceSahurMotion063.cs; protect a fresh workspace before testing and never replay an old restore snapshot.

This is a first polish pass on transition defects. Numeric checks do not establish final artistic approval, all-mesh clipping freedom, or a complete campaign acceptance. Human save bytes, original scene layout and physical inputs were restored after leaving Play mode.
