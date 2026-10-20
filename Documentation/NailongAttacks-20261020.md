# Nailong attack polish — v228

This pass concentrates on the requested transparent sound waves, a single arcing spit, and the preparation/release/recovery of the spit and roar. It preserves the existing boss, equipment, AI patterns, movement system, attack damage and jumpable ground-wave rules. The other melee attacks have not been replaced.

## Runtime changes

- `NailongSonar.shader` replaces the cyan/white solid band with a narrow neutral pressure rim and a small screen-space refraction (3 pixels). Its body shows the background rather than an opaque coloured stripe. Depth softening reduces ground intersections and rejects foreground silhouettes when sampling the offset background. Existing mesh expansion, timing and terrain following remain in place.
- The active PC pipeline already supplies opaque colour/depth, so this does not install an additional full-screen renderer feature or enable new global quality settings. Cameras without colour/depth use a faint transparent rim instead. That fallback was rendered with both camera copies explicitly disabled.
- Roar receives a cosmetic pressure pulse using the same presentation. Its visual pulse has no damage queries; the original roar damage and knockback still occur once. Ground-slam rings retain **15 damage per crossing**, three configured pulses and the existing 0.6 m jump-clearance rule.
- The spit cue emits exactly one projectile, including the Rampage spit release, which previously created a three-projectile fan. Older serialized `spitCount` values cannot restore a volley. UI now calls the ranged attack **ARC SPIT**. Legacy serialized count/interval fields remain hidden for compatibility.
- A bounded target prediction determines the destination once at launch. Flight time provides a clear apex above the mouth; gravity is 18 m/s² and default minimum rise is 0.65 m times the existing range scale factor. It does not home after launch.
- Projectile motion integrates the parabola exactly. Continuous sphere sweeps use segments no longer than 1/30 s, consuming the full incoming step instead of throwing away time on long frames. Hits resolve once against the nearest solid surface or damageable hurtbox. Existing player damage/guard handling is retained.
- The new water material uses scene transmission/refraction, Fresnel reflection and a directional highlight. The glob stretches slightly along its path, with two small cosmetic trailing beads. On contact, six short-lived water beads replace the previous green dust burst/flattened solid ball. These beads have no colliders or damage.
- Two globs and their visuals are prewarmed and reused, with a four-glob safety cap. Normal spit attacks therefore reuse GameObjects, meshes, material and property blocks. Warm ballistic/visual updates allocate zero managed memory in the isolated test.
- Release is queued by the AI and emitted in LateUpdate after the Animator has placed the mouth. Stagger cancels a queued but unreleased spit; an already airborne glob continues as before. Cancel/death/owner teardown recycles or disposes the pool.

## Animation

New complete non-looping humanoid clips are stored under `Assets/Resources/BossCombat/Animations/v228/`: `NailongSpit.anim` (1.6 s) and `NailongRoar.anim` (1.7 s). Their grounded full-body base is the existing imported idle, with authored muscle curves for torso anticipation, head movement, arms, release and recovery. Original clips/controllers are retained.

`NailongPhaseAnimation` maps the authored release point to the configured gameplay windup, so tuning the windup does not move the mouth action away from the attack event. The imported action owns the rig while playing; the previous procedural pose does not also rotate the bones. Existing controller blending is retained and the whole-rig `Animator.Rebind()` at every recovery was removed to avoid reset snaps. `NailongAttackMotion` caches the imported-action component after initialization.

## Verification

Used only the existing Unity 6000.3.23f1 Editor, port7800. Evidence is in `.codex/nailong-attacks-20261020/`.

- **31 contract checks passed** in `Tools/TestNailongAttacksV228.cs`: safe windup, one release despite repeated ticks/stale six-shot data, Rampage single release, queued-shot interruption, exact ballistic movement at10/30/60Hz, native capsule hits once at each rate, visible apex, duplicate hurtbox safety, a thin wall blocking a full one-second simulation step, unchanged fixed ground-wave damage and jump dodge, no extra roar-visual damage, complete humanoid clips, exact configurable cue mapping and pool reuse.
- **1,000 warmed projectile and water-visual updates allocated0 managed bytes**. This is a component allocation check, not a whole-game FPS measurement.
- `rendered-final/` records451 actual gameplay frames over15 simulated seconds, plus224 normal-rendered960×540 images. It runs the real release/update methods using the native equipped Nailong prefab against a capsule target on a diagnostic checker floor. Spit produces one projectile and16 damage; roar produces one14-damage event; three ground rings produce45 total damage. Recorded projectile vertical velocity changes from+6.47 to−10.33m/s. The original equipment remains visible; this is not an art replacement for the boss.
- `vfx-final/` compares visible and hidden effects at the same frozen game time using normal enabled-camera rendering. The water changes31,108 pixels and the pressure ring4,958 pixels relative to their hidden references, confirming that the almost-transparent presentation still renders. Water closeups and pressure waves were inspected with normal refraction and with opaque/depth camera textures disabled. Both shaders report **zero compiler messages**, and final Console errors are **0**.
- Production source whitespace checks pass. Native scene, shader, material and clip changes were made through the appropriate Unity APIs; no scene/prefab YAML was edited.

An initial run found an invalid native property-block allocation in a MonoBehaviour field initializer; it was moved into visual preparation and the31 checks were rerun successfully. The initial water shader also rejected a half-literal spelling; this was corrected and final rendering compiled both shaders cleanly. `native-review/` is the superseded recording containing the rejected water shader. `vfx-comparison/` is a rejected pressure comparison: disabling its component also disabled its renderers; `vfx-final/` corrects that harness error. Do not present those earlier artifacts as accepted results.

Not repeated: the full island encounter/navigation, long sessions with multiple bosses, all material/lighting backgrounds, packaged builds and low-end hardware. The fallback was tested on this desktop with camera textures disabled, not on an actual mobile device. No claim is made that this matches a shipped AAA game's final animation/art quality or resolves unrelated whole-game performance issues.

## Preservation

The current human save was backed up before Play and restored after the last Stop. SHA256:

`A7A0DBD92F9F0E762BB6573D407A824CA2EB4253C4008ED62A2F6DAEE9984A1E`

Story1's pre-existing dirty scene remains unsaved and the existing Editor is stopped/unpaused. Ship motion v227, Sahur combat, original boss scale/equipment, other user changes and original clips/controllers remain in place. No second Editor/Player, build, commit, push or automation was created.
