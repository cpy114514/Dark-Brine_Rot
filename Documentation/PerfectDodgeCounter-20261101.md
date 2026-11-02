# Sahur perfect step and returning sweep — 2026-11-01 / task 043

The user requested that a dodge just before an enemy hit become a short step, with one immediate normal-attack press turning Sahur toward that attacker for a horizontal sweep. The concrete design was presented; the user's reply requested a wider trigger window. The implementation uses **0.28 seconds** rather than the proposed 0.18 seconds. Design/provenance: `AnimationWorkflow/Actions/PLAYER_SahurPerfectDodgeCounter_v001/design-v001.md`.

## Result

- A legal grounded dodge queries actual hostile attack forecasts. Timing, attack geometry, height and cover must agree. Merely being near an enemy does not qualify. A bounded recent-strike ledger prevents repeated rewards from the same strike.
- Success replaces the existing roll with a **0.28 s / approximately 2 m** in-place humanoid step. CharacterController still owns displacement and collision. Step protection starts immediately; ordinary roll protection/timing and legal attack-recovery dodge cancels retain their existing routes and stamina cost.
- A fresh normal attack in the final **0.10 s** can buffer. Otherwise the opportunity lasts **0.45 s after the step actually finishes**, including at low rates. A normal-attack press consumes it once. Startup turns the gameplay heading toward the remembered living attacker in approximately 0.10 s, then commits to the sweep.
- Club and twin blades have separate **0.60 s** sweep clips/states. Damage remains normal weapon damage through the existing swept-weapon volume, cover checks and shared per-swing victim set. No synthetic radial damage, guaranteed stagger, teleport or root-motion slide is added. Jump input, charge/chord input, incompatible weapons and external owners retain priority. Rifle shooting keeps its existing route.
- Two pooled baked-body echoes use a low-opacity gold/green edge shader, fade in 0.14 s and cast no extra shadows. The template material lives in Resources so its shader is retained in a build. No particles or global slow motion are added.

Enemy forecasts are implemented for generic EnemyAttack, ChickenBro/derived marshmallow encounters, Nailong melee/charge/tantrum, Capri, Pig Ronin, Bombardino's low pass, actual spit/ordnance flight paths and emitted Nailong pressure waves. These adapters expose attack clocks/geometry; they do not change enemy damage or AI decisions. One actual Nailong attack is exercised end to end below. Coverage is not a claim of a complete playthrough of every boss pattern.

New partial files isolate the player's step and counter logic. Existing AI changes add a partial declaration and register the forecast interface. Existing rig, Avatar, original clips, scenes and unrelated pending combat/VFX changes remain. Three current Sahur controllers append `Perfect Step`, `Perfect Counter`, and `Capri Perfect Counter` without replacing normal states.

## Animation revisions

- `SahurStepV001` derives a compact nonlooping, upright push/step/recovery from the retained jog/idle humanoid curves, retaining original rig/bind/grip data. Movement is code-driven and independent of curve root translation.
- `ClubReturnSweepV001` (slash-derived) is rejected for this counter. `V002` (retimed one-hand sweep) is rejected because the forward pass fell outside the chosen hit window. `ClubReturnSweepV003` preserves the accepted one-hand circular sweep, shifts its source phase by 0.27 and aligns the forward passage with the counter's normalized **0.30–0.65** real-contact window. Original sources and rejected revisions are retained. `BladeReturnSweepV001` preserves the existing frontal blade revision.
- Actor root-motion extraction is excluded only for the new counter, preventing the source strike's translation from sliding Sahur toward/around the victim. A first echo-scale trial is rejected; the final bake preserves the actual rendered body scale.

## Verification and review

Accepted takes: `.codex/perfect-dodge043/final-60/` and `final-30/`.

Both native input takes pass **24 checks each, 48 total, with zero captured runtime errors**. Each produces seven perfect steps and four counters. Checks include unchanged early ordinary roll; facing/range/cover rejection; the widened 0.25 s incoming cue; startup protection against a real hostile health call; buffered fresh mouse press; approximately two metres of actual capsule movement; attacker-facing before contact; real club/twin-blade damage exactly once; gamepad dodge/normal attack; expiry; external-control cleanup; no repeated reward from one strike; actual loadout grip/hitboxes; and a real native Nailong windup, imminent hit forecast and player-input perfect step/counter. The controlled melee test attacker advances 0.25 m with its strike; weapon reach is not enlarged to manufacture a hit.

Continuous native-camera JPGs, frame/time/phase/weapon-volume metadata, silent `PerfectDodgeCounter.mp4`, and closely spaced section sheets are retained in each accepted take. Reviewed the incoming pose, upright step, heading pivot, waist-height sweep, actual victim flash and outgoing recovery in the sheets. No claim of watching the entire recording at normal speed, hardware gamepad feel, full island playthrough, all-layout collision freedom or hardware FPS benchmark. Fixed 30/60 Hz captures establish simulation/input behavior. This is a targeted **FIRST PASS**, not human FINAL art approval.

Earlier takes remain for provenance. Several early failures were fixture setup/input-settings lifetime issues; later misses exposed genuine contact spacing/phase/root-motion issues and are not accepted evidence. InputManager destroys replaced HideAndDontSave defaults; the final fixture preserves the exact original settings object/flags and restores it before destroying its disposable copy. Lifecycle guards restore physical input on completion, failure, Stop, reload and quit.

## Shared editor and restoration

The user explicitly authorized coordination with the concurrent VFX chat. Its task050 granted an exclusive interval using `editor-handoff050.txt`. The originally observed Story1 setup is retained in `initial-scene-setup.json`; the latest clean **Environment + Lighting** handoff state was separately protected and restored after the tests. The human save matched the initial snapshot throughout and was verified byte-identical at release; proof is in `restore-proof.json`. Physical keyboard/mouse are enabled, replay devices removed, original input settings/flags and healing preferences restored, and the editor returned stopped/unpaused before `editor-released043.txt` was written. No task050 backup or scene was overwritten.

Fresh source/controller backups: `.codex/perfect-dodge043/source-before/`. Frozen accepted bytes and hashes: `source-after/`, `source-sha256.json`; acceptance data: `acceptance.json`. No source scene/prefab rewrite, new Editor/Player, shipping build, commit or push.

Final generic-enemy regression: a previous strike's victim set must not reject the next attack's windup; it rejects consumed victims only while the current damage window is active. The isolated native Animator/EnemyAttack preview-scene assertion passed both branches (`generic-cycle-regression.json`) after the one-line guard correction. Final native compilation completed without errors. All preview objects were destroyed; Environment + Lighting remained stopped/unpaused. The additional coordinated interval was released through `editor-final-guard-released043.txt`. The two captured input takes precede this generic-only correction; the targeted assertion verifies the correction separately.
