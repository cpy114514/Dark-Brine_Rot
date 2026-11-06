# Sahur directional perfect step — 2026-11-03 / task064

User request: make the existing 闪身步 feel like Black Myth: Wukong. This is a targeted revision of the previously approved perfect-dodge design, not a new dodge mechanic or a claim of matching the original game's final animation quality. Design remains `AnimationWorkflow/Actions/PLAYER_SahurPerfectDodgeCounter_v001/design-v001.md`.

## Production change

- Four versioned, nonlooping Humanoid clips replace the single upright jog-derived step: `SahurStepForwardV002`, `BackwardV002`, `LeftV002`, `RightV002` in `Assets/Resources/Combat/PerfectDodge`. Native Animator APIs install the same directional BlendTree in Gameplay, Grounded and Rifle controllers. Retained V001/source clips, Avatar and rig are unchanged.
- The body compresses into a compact directional step, with bent knees, reduced arm swing and a controlled recovery. The collision root holds its starting facing rather than turning toward the lateral movement direction. A cumulative smooth displacement curve supplies acceleration and braking; CharacterController remains responsible for contact and the approximately 2 m travel.
- Clip orientation is baked consistently with the retained idle: `keepOriginalOrientation=false`, `loopBlendOrientation=true`. Retaining V001's different extraction settings while copying idle RootQ caused an approximately half-turn inside the visual skeleton despite a stable collision heading. That intermediate revision is rejected.
- `SahurPerfectStepSupport` corrects only submerged soles during the step/outgoing blend. It uses existing cached skinned-sole measurements and bounded two-bone support against nearby walkable ground, excludes actors/own colliders, and does not reposition the capsule. Authored jog foot IK is disabled for this state because its goals no longer match the edited muscle curves. Pause preserves the last corrected leg rotations; external cinematic owners retain priority.
- The first three-dimensional body impression is captured at the attack's original target position before displacement. A weaker second impression appears at 0.055 s. Both remain anchored in world space and dissolve using object-space noise, silver-gray body shading and a restrained warm rim. Lifetimes are 0.34/0.20 s, with two reusable meshes and one material. No particle spray, Bloom dependency or global slowdown is introduced.

The approved mechanics remain: actual imminent hostile forecast within 0.28 s, real facing/range/height/cover, one reward per strike, 0.28 s step, existing stamina cost/protection, last 0.10 s fresh-press buffer and 0.45 s follow-up window. Club/twin counter damage still uses genuine weapon sweeps and one shared victim set. Ordinary rolls, source counter clips and enemy AI rules are retained.

## Reference and provenance

Viewed a short actual gameplay sample from [this perfect-dodge tutorial](https://www.youtube.com/watch?v=oyXnhPViK-o); a later browser seek failed, so this is not a claim of reviewing the complete video. The design reference is the short evasive displacement plus an impression left at the original position. No proprietary Black Myth model, animation, texture, audio or game data was downloaded or imported. New curves derive from the project's already retained Quaternius humanoid sources and V001; the echo shader and support integration are project code. Unity 6000.3.23f1 / URP 17.3.0.

## Review and validation

Evidence directory: `.codex/wukong-step064/`. Native images and `frames.json` contain simulation time, state, root facing/travel, body yaw, soles, echo count and real weapon volume metadata. `Tools/VerifyWukongStep064.cs` uses real keyboard/gamepad input events and production player/weapon/Boss components. Enemy/Boss fixtures use a temporary runtime scene to avoid first-island progression prerequisites; the original loaded scene assets are never saved. The terrain take uses existing first-island colliders/grass, not a replacement floor.

Final results and review ranges are recorded below after the orientation correction. Earlier `first-60`, `aligned-60`, `arena-60`, `final-60`, `final-30` and `island-30` are retained for diagnosis, not artistic acceptance. Early Boss failures were progression-gated fixture setup; real visual failures were the submerged feet and inconsistent Humanoid orientation. Knee calibration experiments were not shipped.

Stage: targeted SECOND PASS review. Numeric checks establish behavior, not artistic approval or hardware FPS. Three-quarter entry/peak/recovery and four-direction gameplay images are reviewed; a complete all-Boss campaign, every slope/layout, normal-speed full-video review and human FINAL approval are not claimed. High grass partially obscures foot silhouettes on the existing island.

## Preservation and rollback

The original seven modified production files/controllers are stored with their original paths under `.codex/wukong-step064/rollback/`; hashes are in `source-before.json`. They include the already completed task063 movement/charge/cinematic changes. To revert this task, stop Unity, compare those snapshots with current files and restore only task064 changes; import the restored controllers with Unity. The four V002 clips and new support component can then be removed through AssetDatabase after verifying no remaining references. Do not restore an older Git checkout over unrelated pending work.

Fresh human saves, healing preferences, physical input settings and scene setup were protected before each Play session group. Stop precedes save restoration, because shutdown can flush queued writes. Exact InputSettings identity/flags and physical device state are restored by lifecycle cleanup. Proof lives in `restored.json` and `orientation-fix/restored.json`. Original five loaded scenes are left clean and the Editor stopped. No build, commit, push or external asset login/download was performed.

### Final accepted evidence for this revision

- `orientation-60`: 37 checks, 1,530 continuous samples, 176 perfect-step samples.
- `orientation-30`: 37 checks, 817 continuous samples, 88 perfect-step samples.
- `orientation-island-30`: 10 checks, 235 samples, four directions on actual island ground.
- All three reports pass with no captured runtime errors. Native asset validation passes for all three controllers, all four 0.28 s Humanoid clips and the URP shader. Production C# compilation reports no errors.
- Recorded flat-floor minimum sole clearance is +0.0246 m at both rates. Maximum signed body yaw during the captured steps is 4.45 degrees at 60 Hz and 0.066 degrees at 30 Hz, replacing the rejected visual half-turn. Two body echoes maximum. These are sampled fixture measurements, not universal guarantees on every surface.
- Inspected corrected 60 Hz frames 378/385/393 for entry, compression and recovery, four-direction peak frames 1167/1250/1333/1416, and corrected island frames 55/59/113/221. Body heading remains readable without the former half-turn. Existing high grass still hides parts of the feet on the island; flat-floor captures expose support/leg behavior.
- Preview: `.codex/wukong-step064/DirectionalPerfectStep.mp4` (directional controlled fixture, 60 Hz). It is generated from the corrected `orientation-60` take. This document reports still/frame review rather than a claim of watching every video frame at normal speed.
- Both session groups were stopped and restored. The final native audit shows the original five clean scenes, stopped/unpaused Editor, enabled physical keyboard/mouse, live original InputSettings and captureDeltaTime=0. Human save bytes match the fresh snapshots. Native controller serialization emits empty `m_DirectBlendParameter` fields with trailing spaces; source whitespace checks are clean and these assets were left as Unity serialized them.
