# Second Island — marshmallow experience

The `MarshmallowTraversal` component on `Sahur Gameplay/Sahur Player` supplies the Second Island movement profile. It is active only in `Assets/Scenes/Second Island.unity`. The existing motor still owns movement, collision, animation transitions, stamina, dodges and combat.

- Effective gravity: 9.6 (previously 64); deliberate jump height: 4.8 world metres.
- Holding a movement direction produces free walking bounds (1.15 m) or sprint bounds (1.85 m), after 0.16 s of ground contact. Releasing movement ends automatic bounds.
- Air steering uses 45% of normal acceleration/braking. Landing compression is weighted by impact strength (up to about 10%), then recovers; the capsule remains rigid.
- Uphill bounds inherit the upward slope travel at takeoff, capped at the sprint launch speed, so rising ground does not immediately catch the character.
- Jump input, combat, rolls, swimming, cutscenes and other movement owners take priority over automatic bounds. The ordinary jump and flip keep their stamina costs.
- Camera distance is 10.4 to leave more space around the feet and ground.

## Astronaut stride

The scene player also has `MarshmallowSpacewalk`, with baked `MoonStrideLeft` and `MoonStrideRight` clips under `Assets/Game/Prefabs/Characters/Sahur/Animations/Marshmallow/`. Each automatic bound alternates the leading leg. The current clips start with KayKit's `Jump_Full_Long` and shape its symmetrical jump into an alternating single-foot bound using constrained humanoid muscle curves. The leading knee rises and reaches forward, the trailing leg pushes behind, and the expanded stride is held during low-gravity flight. Sahur's existing walking clip supplies only the relaxed arm swing. The source package and license are in `Assets/ThirdParty/KayKitLunarBound/`; the author distributes it at https://kaylousberg.itch.io/kaykit-character-animations under CC0.

The former world-space limb/foot aiming was removed: a forward toe vector alone did not constrain ankle roll, and toes were still receiving the underlying jump animation. The clips include toe joints, shoulders, upper chest, neck and head. Authoring limits ankle flex to +/-0.32 humanoid muscle units, foot twist and toe flex to +/-0.12, and lower-leg twist to +/-0.10. Knees retain the natural bend direction. The jump source is evaluated with `AlwaysAnimate`; a bake with less than 10 degrees of thigh motion is rejected so a culled/frozen source pose cannot silently become a stride clip.

The motor signals each automatic takeoff and landing. Air pose phase follows actual vertical velocity, so a long downhill flight holds the extended stride rather than cycling through repeated running steps. The leading knee folds during the swing and extends for landing; the supporting knee extends during push-off and folds after leaving the ground. Landing continues from the actual airborne phase, including an early uphill landing, and finishes the stride over the ground compression interval. Releasing movement then blends to the normal idle. Manual jump, air flip, roll, combat, scripted ownership and swimming retain their original animations. Rifle traversal preserves its authored upper-body pose. Pose restoration/respawning cancels the stride.

Only the visual skeleton is sampled; the controller, collision capsule and trajectory remain owned by the existing motor. The source AnimatorController and shared walk/run clips are unchanged. `AuthorMarshmallowSpacewalk.Bake()` rebuilds the two generic bone clips from an isolated rig at the origin, with no runtime IK. `VerifyMarshmallowSpacewalk.Start()` runs native input traversal, checks actual left/right foot placement, ankle flex/twist, toe flex, torso lean, manual-action handoffs and the existing surface feedback. Side/front frames and measurements for the repair are under `Temp/MarshmallowSpacewalk/FootRepair/Review/`. Earlier stride records remain under `Temp/MarshmallowSpacewalk/Review/` for comparison.

The knee-timing pass covered 8 alternating strides, all 239 measured forward reaches, manual jump/flip handoffs and the surface-effect regression. In native Play Mode samples, the advancing knee averaged 58 degrees during swing and 27 degrees before landing; the trailing knee averaged 17 degrees during push-off and 45 degrees later in flight. Eight landing transitions showed no phase jump, and ankle flex/twist/toe flex remained within the established limits. Saved 16 front/side review frames covering both legs and takeoff/flight/landing. The final pass recorded no Unity runtime errors.

## Linked surface feedback

`MarshmallowIslandExperience` is attached to the same player and references the island's high-resolution renderer. The motor ticks it before applying the visual and camera, so one landing drives all feedback together:

1. Standing pressure gradually depresses the surface by 0.24 m. The visual soles follow the rendered depression, capped at 0.432 m when fresh landing impressions overlap.
2. Landings stamp a broad depression and two sole impressions, with a strawberry blush and an outward cream rebound wave. Each impression recovers over 4.5 seconds. The shader bounds deformation to 0.48 m and keeps the collider stable.
3. Landing intensity controls the character squash, fluffy sugar puffs, short pulled candy fibres, warm damped audio and a small camera dip. Takeoff adds a softer lift cue and a few fibres. Ground rolls also leave compression feedback.
4. Sprinting eases the camera back by up to 0.75 m. Stopping returns it to the usual distance.
5. Sparse ambient floss drifts around the player. The 22 authored clouds float and breathe slowly through `MarshmallowCloudMotion`.

The scene uses `CandyReactiveSurface`, `CandyCloudSoft` and `CandyFluff` materials. Their shaders provide stable cream/rose/lavender marbling, fine sugar texture, soft wrapped lighting, shadow/depth passes, and soft particles. Contact state uses a renderer property block and a fixed pool of 12 impressions. Particle and audio resources are prewarmed once and reused.

The `marshmallowPalette` flag on this scene's `DayNightCycle` supplies a matching lavender/peach sky, cream clouds, softer sunlight, ambient light and fog while retaining the day/night clock. Other scenes keep the default flag and materials.

Pausing freezes the scaled-time feedback. Disabling either the traversal profile or experience clears its shader state, particles, visual sink, camera offset and voices. Large pose restores clear stale impressions. Master volume remains owned by the existing audio policy.

## Verification

Ran two Play Mode traversals using native Input System devices, real frame updates, and the authored island colliders. Reviewed captured game frames and adjusted uphill lift after the first pass.

Second pass: 1,292 recorded samples; walking bounds lasted 1.72–1.94 s downhill; uphill sprint bounds lasted 1.11–1.37 s (before adjustment: about 0.6 s); standing jump lasted 1.99 s and rose 4.65 m above its first sampled frame. The air flip completed and landed. Both release tests ended grounded with zero horizontal speed. Visual scale ranged from 0.915 to 1.025. No Unity errors were recorded.

The full experience then received three Play Mode passes with captured apex/landing frames and refinements to palette, uphill continuity, particle size and visual sole alignment. Final pass: 1,642 recorded samples, 11 landings, 10 takeoffs, 228 emitted puffs and 22 audio cues. Median recorded frame rate was 59.94 FPS on the current editor/machine; visible particle count peaked at 51. A standing jump lasted 1.99 s, uphill sprint bounds 1.23–1.32 s. Tested ground roll, steering, stopping, jump, air flip, impression expiry, bounded standing pressure, pause/resume and disabling/re-enabling the profile. Both candy shaders compiled without errors; generated cue audio peak was 0.104 before source volume, with no clipping. No Unity runtime errors were recorded.

Playtest implementation: `Assets/Editor/PlaySecondIslandMarshmallow.cs`. In Play Mode, run `return PlaySecondIslandMarshmallow.Start();` through the Unity evaluation tool. It restores input devices on completion. Initial movement-only records remain under `Temp/MarshmallowTraversal/`. Full experience samples, apex/landing screenshots and verified/performance summaries are under `Temp/MarshmallowExperience/Playthrough/`; pre-change backups are under `Temp/MarshmallowExperience/`.
