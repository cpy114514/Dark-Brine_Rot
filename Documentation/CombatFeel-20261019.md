# Sahur melee combat feel — 2026-10-19

## Implemented and verified

This revision polishes the existing club combo and charged downward strike. It retains their source clips, humanoid Avatar, masks, damage values, charge time, walking/running, and root-motion ownership. It does not replace the character or combat system. The latest user message separately authorizes the additional moves listed below.

`Assets/Resources/Combat/SahurCombatFeel.asset` is the shared Inspector configuration. `Tools > Combat > Sahur Combat Feel` opens a scrollable tuning window with a temporary Play Mode arena and blocking/armored target switches. The arena changes no scene asset or save data. Stop Play Mode after testing to restore the original scene simulation.

The existing authored combo now uses a phase-dependent playback multiplier: anticipation, fast contact arc, follow-through, and slower settling. Standing attacks retain the full-body hip/step animation. Mobile attacks continue at the same pose phase on their torso layer. A real handoff race was fixed: the destination layer's stale playback time could finish the attack before `Animator.Play` was evaluated. Both progress and queued input now respect the handoff frame.

The charged strike retains its movable single-right-arm design. It accelerates from the held pose through contact, then settles with a longer recovery. No runtime bone posing was added to replace attack animation.

## Timing contract

| Move | Authored clip duration | Base speed | Damage window (normalized clip time) | Default visual hit stop |
|---|---:|---:|---|---:|
| Club Combo 1 | 1.066667 s | 1.25 | .57–.86 | 48 ms |
| Club Combo 2 | .700000 s | 1.25 | .12–.34 | 38 ms |
| Club Combo 3 | 1.766667 s | 1.55 | .32–.55 | 105 ms |
| Charged downward strike | 2.266667 s | phase curve | .35–.43; hold pose ends at .28 | 90 ms |

Playback multipliers do not change these normalized contact intervals. Actual elapsed durations vary with the configured curves and successful impacts. Armored light contacts scale attacker stop to 70% and victim stop to 50% of that; victim overrides can further tune it.

Input buffer defaults to 220 ms. Combo transitions use a 70 ms blend and a .90 recovery chain point, constrained to occur after contact. Charge can cancel into the existing dodge. Light recovery can cancel at .90 or after its damage window; heavy recovery at .62 or after its damage window. Buffered dodge survives automatic attack exit. Ground/air state still determines roll versus the existing air flip. A light input near heavy/finisher recovery can buffer the next basic attack.

## One confirmed impact path

`CombatHitFeedback.Apply` retains `IDamageable`, applies actual accepted damage, and emits one `CombatHitEvent` for the contacted victim. Existing per-swing health-component deduplication remains authoritative, including multi-collider enemies. Protected story targets, dead targets, and misses do not emit ordinary successful-hit feedback. Optional `CombatImpactResponse` and `ICombatHitResolver` support blocked and unflinching results without inventing new boss guard behavior.

The same event drives local hit stop, contact-point flash/debris, bounded directional camera impulse, layered audio, and existing enemy recoil. A blocked result causes no damage and uses reduced resistance feedback. Nailong light hits retain its current action; its existing heavy interruption remains. Cappuccino keeps its own poise/ultimate decisions and gains directional, strength-scaled presentation. Repeated rejected recoil requests cannot multiply an already strong Nailong recoil indefinitely.

`CombatLocalHitStop` never writes `Time.timeScale` or player movement locks. Full-body strikes hold Animator playback. Mobile strikes freeze the existing attack speed parameter, keeping locomotion running. The victim's evaluated pose can be held after manual/additive animation, including Cappuccino's manually sampled attacks. AI/physics clocks remain authoritative; this is visual stop, not a new stun system. Expiry uses unscaled time and safely restores playback on disable, pause, or ownership changes.

Weapon detection now samples the entire moving club, including the middle, rather than only handle/tip paths. Partial entry into a damage window and a complete window crossed between frames are both queried. Queries are bounded, nonallocating capsule scans. Feedback contact points come from the queried weapon segment. Walls still block strikes. The existing once-per-target set prevents repeat damage during stop or overlapping hurtboxes.

## Feedback assets and budgets

- Three persistent particle emitters per melee feedback owner: maximum 12 flash, 40 dust, 48 flecks. Effects remain local and short, without bloom or explosions.
- A fading, tapered weapon ribbon follows actual animated capsule endpoints: 16 samples, 32 vertices, 30 triangles, approximately 95 ms residual. Sampling occurs after the current frame's hit-window update and begins shortly before contact.
- Camera offset is directional, bounded, and removed before the normal follow update. Repeated impacts use capped amplitude; disable removes the offset. Camera motion and brightness are configurable.
- Existing whooshes remain synchronized to the strike window. Contact uses the retained StickHit recording; low-frequency/body and surface stems are derived offline into `ClubBody.wav` and `ClubSurface.wav`. Metal uses the retained Guard recording. Pitch variation is small; impacts never synthesize clips on demand. Three persistent impact AudioSources plus the existing swing source are reused.
- The user's explicit audio request supersedes the old temporary silent-gameplay policy. Master volume now follows the saved setting, including mute. No saved preference is overwritten.
- Profiler markers: `Combat.ConfirmedImpact` and `Sahur.WeaponSweep`.

## Actual verification

Artifacts: `.codex/combat-feel-20261018/`.

- `tests.json`: 19 native Editor checks, including actual mouse combo/charge input, miss, heavy contact, attack-layer stop, Ctrl charge/recovery cancels, blocked/armored/dead results, overlapping stops, ownership/disable restoration, unchanged global time, and exact camera return. Native footage samples were rendered from the gameplay camera. A miss by an individual combo stage is preserved when the weapon does not contact the target.
- `contracts.json`: 8 additional checks. A thin target crossing the middle of a fast sweep takes exactly one hit across repeated scans and multiple hurtboxes; a new swing can hit again. Manual Cappuccino posing remains visually held, repeated Nailong recoil stays bounded, and pause/mute/ownership cleanup pass.
- `moving-light-regression.json`: native mouse and directional keyboard inputs complete all three stages while sprinting/turning, preserve phase/root ownership, and restore layer weight. This is a controlled 60 Hz simulation test, not a frame-rate benchmark. The original variable-step harness was sensitive to Editor stalls and exposed the layer handoff race; historical failures are retained separately.
- Warmed same-frame Editor batch: 200 routed impacts took **3.5659 ms total**, with **0 managed bytes** in the measured loop. Unity Profiler recorded 200 impact marker calls totaling **2.4665 ms**. AudioSources remained 4 and particle systems 3. This excludes actor initialization, capture, and first-time cache creation; it does not establish Player FPS or GPU performance.
- `audio-check.json`: the configured heavy flesh mix's offline unattenuated peak is .770775 across eight endpoint pitch combinations. This excludes whoosh overlap, device processing, other material mixes, and subjective listening.
- Final compilation passed; fresh Unity console errors were zero. The existing Editor was stopped cleanly. No scene, prefab, package, or ProjectSettings file was changed by this revision. Save SHA256 remained `EE58C485F0BA23FB6DE948F847944569C21BCFB2A83581104E4B61F1D1C523D0`.

`combat-feel-review.mp4` is a silent native visual/timing preview, not a captured audio mix or performance benchmark. Source backups and historical failed harness attempts are retained in `before/` and `iterations/`.

## Remaining quality limits

The attack assets remain the existing authored clips with timing/blending polish. No new enemy vocal recording was available; the optional vocal slot remains unassigned. This pass does not claim final AAA animation/mix approval, a full island combat playthrough, or release-build frame-rate acceptance. First-hit cache creation and production hardware audio/GPU behavior have not been separately benchmarked. The existing shark opening and cinematic graphs were not re-authored in this pass.

## Latest user continuation — approved move intent

The user directly requested these additions after finishing the feel pass:

1. Jump + attack performs a jump chop; inspect and verify the existing once-per-jump slash before replacing anything.
2. Club: holding Left Mouse + Right Mouse continuously spins Sahur, with the club raised, held horizontally in both hands.
3. Cappuccino Assassino dual blades: charge with the blades crossed into an X, then dash forward on release while cutting outward to both sides.
4. E blocks; dual-blade Left Mouse + Right Mouse performs a spinning attack.

Preserve the core combat system, rigs, user saves, English-first UI, precise contact and nonlethal story tutorial. Inspect key conflicts and animation sources first. The user's specified poses, movement intent and requested implementation are the approval source; any material change to that intent needs clarification. Timing/cost tuning should reuse the current balance and remain configurable.

The one-time **2026-10-19 05:45 America/Vancouver** continuation has fired. Automation `automation` was deleted in the app and was not rescheduled. See the 05:45 report in `Documentation/CombatCombinations-20261019.md` for the latest transition/contact fixes and actual regression evidence.
