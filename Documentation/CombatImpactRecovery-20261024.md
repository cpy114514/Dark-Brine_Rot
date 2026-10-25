# Combat impact and recovery — v245, 2026-10-24

Improves the existing melee/rifle and received-damage paths. Source animations, weapons, enemy health, attack contact windows and the shark storyline remain in use. Configuration is in `Assets/Resources/Combat/SahurCombatFeel.asset`, including Inspector controls for received impacts, hostile damage, recovery and dodge protection.

## Concrete problems corrected

Player hostile damage previously lost HP and played Hurt without a body reaction or the unified impact event. Large boss light contacts reduced 48ms stop by both 0.7 and 0.5, leaving about 17ms. Frontal camera impulses were projected almost entirely out of the screen plane. Rifle impacts applied health directly and skipped the unified victim presentation. Ctrl recovery cancellation required grounded readiness, so an authored attack step briefly lifting the feet could discard the press.

Accepted hostile player damage now produces one actual-damage event, local animation stop, authored torso recoil, brief material response, contact VFX/audio and bounded directional camera kick. Rejected damage during dodge/rush/protection creates no ordinary hit event. Block remains a distinct resistance result. Damage events report actual HP loss, including a capped or final lethal hit.

The authored received-hit layer is appended to SahurGameplay, SahurGrounded and SahurRifle controllers. It uses the original stagger as its basis, with explicit supported spine/chest animation curves in `SahurReceivedHitV245.anim`; Root and leg motion remain masked out. The old stagger carried most visible lean in RootQ and an unmapped UpperChest, so simply masking its upper body was weak. The new controller layer is driven through ReceivedHitPhase and fades out over 0.36s. It preserves the controller's parameters, locomotion, attack states and equipment transitions. Death releases local stop and disables the hurt layer/flash before native death presentation takes ownership.

Boss light contact now retains about 46.75ms visual stop with the installed normal profile. Armor still owns gameplay poise and keeps light-hit actions running. Nailong's existing directional recoil has a stronger default; Cappuccino retains its existing recoil/poise implementation with the stronger profile; aircraft hits add a short directional visual bank/pitch response. Material feedback reuses cached property blocks, restores original empty blocks and respects pre-existing tint/other property changes. It creates no material per impact.

Frontal impact includes a short angular/axial kick rather than only a screen-plane projection: the 0.065 normal setting measures approximately 0.519 degrees in the controlled contract. Repeated impulses remain capped and remove their exact offset. Existing reduced-motion/brightness controls and saved audio mute remain respected. Rifle impacts use a separate short preset and do not stop the player's locomotion. Engine/counter damage remains calculated once by the existing rifle trace, avoiding a second weak-point multiplier.

## Forgiveness

- Hostile attack damage multiplier: 0.7; individual accepted hostile hit ceiling: 18% of maximum HP. These apply to directed hostile attacks, including projectiles, rather than altering enemy health or scripted story damage.
- Accepted hostile damage grants 0.6s shared recovery protection, preventing overlapping volumes and rapid following attacks from repeatedly draining HP.
- Ground roll protection spans normalized phase 0.04–0.72, preserving startup/recovery vulnerability.
- Ctrl buffer uses the existing 0.22s combat input setting. Recovery can cancel into the original once-per-jump air flip when an attack step is briefly airborne; stamina, cooldown and the completed contact window remain authoritative.

At 100 maximum HP, a 30 raw hostile hit resolves to 18 HP; a 6 raw hit resolves to 4.2 HP. Five separate oversized accepted hits leave 10 HP; the sixth can kill and death fires once. Narrative raw 30 HP loss still resolves to 30, and the complete shark opening still finishes at 70 HP. Twin frontal E guard and charged-rush immunity remain active.

## Actual verification

Evidence: `.codex/combat-recovery-20261023/acceptance.json`.

115 Editor checks pass: 26 damage/recovery/feedback, 13 twin guard/rush/rifle/camera, 9 material/death cleanup, 8 swept-hit/freeze/ownership/performance contracts, 19 real mouse/Ctrl input checks, and 40 opening charge/contact/geometry checks. Native input completes all three club stages and a real charged contact; an actual Ctrl recovery press now produces the existing air flip. Rifle engine impact resolves exactly 30 expected damage and one rifle event. The hurt layer remains active after switching to the rifle controller.

The final authored reaction was verified against the evaluated controller and rendered pose, rather than only testing a nonzero weight: chest rotation changes about 32.59 degrees at a held 0.85 layer weight at the sampled peak, with the upper skin moving in the backward direction. This is a held calibration pose; the normal runtime envelope peaks lower and recovers. Bake-space centroid distances are diagnostic and are not claimed as collision-body displacement.

The final silent native recording is `.codex/combat-recovery-20261023/final-controller-layer/CombatFeel-v245.mp4`. It shows real heavy, club and twin combo contacts plus received impacts and recovery. Reviewed temporal received-hit frames in `final-player-hurt-review.jpg`. The attacks and VFX remain based on real contacts; whiffs do not invent hits. The native contact observer records 15 confirmed events including received damage.

Warmed 200-impact Editor batch: 3.4467ms total and 0 managed bytes; 4 AudioSources and 3 persistent particle systems remain bounded. This excludes initialization and does not establish Player FPS or audible mix approval. The complete final opening records 2,891 steps, original shark scale 3, two tutorial contacts, three film contacts, real wood fracture, 70HP finish and zero sampled shark/timber intersections across 79 independent later-film probes.

No new Windows build, low-end hardware session, complete island boss playthrough or subjective device-audio review was performed. The preview is silent. Game code compiles; the Editor package manager's unrelated cancelled-list error remains in historical Console output.

## Rejected observations and preservation

The first internal Animator graph insertion could show a nonzero layer weight while the Animator replaced its output, so that implementation and its early footage are rejected. The final system uses ordinary controller layers. An initial curve sign leaned forward; native controller/skin comparison selected the corrected backward curves and reduced their amplitudes. The original stagger and all base attack clips are preserved. The first variable-step Ctrl test exposed the real airborne-cancel issue; the final native test passes. One opening harness inherited global pause and stayed in Crush; it is retained as invalid setup, replaced by a fresh paused-load take with the normal simulation scale explicitly restored.

Human save, Story1/Enemies scenes, Sahur/Nailong/Cappuccino prefabs and QualitySettings match the pre-task hashes. The prior v243 performance and v244 ocean changes remain intact. Animator assets and new animation/mask assets were edited through Unity Editor APIs. All arenas, equipment grants, calibration overrides and review cameras were confined to Play Mode. No commit, push, upload, new Editor or automation was requested or performed.
