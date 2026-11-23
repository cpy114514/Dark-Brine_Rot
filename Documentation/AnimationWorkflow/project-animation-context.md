# Animation rigs, motion ownership and review

## Sahur in gameplay

The character controller owns ordinary movement. Attack state selects when animation root motion can move the character. Charging the stick and releasing its diagonal strike must work while walking, sprinting or turning. Ordinary twin-blade slashes allow movement. The heavy rush and the rotating finisher have their own movement rules. Check damage against the actual weapon path and keep the original Avatar and rig bindings. [Movement and combat measurements](../GameplayFeel-Results-20261109.md) · [Twin-blade movement](../TwinBladeMovingAttacks-20261109.md)

The execution feature was removed in November. Its earlier design and test records are historical. [Execution removal](../BossExecutionRemoved-20261108.md)

## Opening encounter

Sahur walks and jumps on floating wreckage, swims and climbs moving edges. The shark breaks the ship with its tail. The paired cinematic owns its actors during the fight and hands off after Sahur falls. For a change to contacts, inspect the sampled pose, the physical support and the water height in the same frame. The camera has a documented fixed-distance shot that permits foreground occlusion. [Encounter design and reviews](Actions/CH1_SahurSharkEncounter_v001/) · [Fixed-distance camera](cinematic-fixed-distance-20261015.md) · [Capture and review procedure](record-review-loop.md)

## Animation and environment sources

The jump-chop revisions include a Meshy AI animation retargeted to Sahur. The opening encounter also uses actions on the original Mixamo rest hierarchy. The second island uses Quaternius UAL2, KayKit and native boar actions, Poly Haven props and Kenney effects. The first island uses Hunyuan-derived grass and rocks. Source-specific credits, licenses and rejected animation candidates are recorded in the respective production notes. [AI animation sources](../AIAnimationSources-v267.md) · [Meshy jump chop](../MeshyJumpChop-v270.md) · [Second island sources](../SecondIslandPresentation-20261108.md) · [Hunyuan environment](../HunyuanEnvironment-20261027.md)

## October 2026 rig audit

The following observations describe the inspected revision. Check the current prefab, Animator assignments and scene overrides before changing a clip.

### Playable rig and active animation

SahurPlayer: `Assets/Game/Prefabs/Characters/Sahur/SahurPlayer.prefab`.

The actual player uses valid Humanoid `PbrSahurAvatar` from `Assets/Game/Prefabs/Characters/Sahur/Models/PbrSahur.fbx` and `SahurGrounded.controller`. The older `Models/sahur.fbx` is Generic with no Avatar; do not use it interchangeably with the active Humanoid model.

`SahurGrounded.controller` has Base Layer and masked `Charge Upper Body`, with `SahurChargeUpperBody.mask`. Charging/release uses `Gameplay/SahurStandingDownwardCharge.anim` (clip name SahurStandingMeleeDownward, 30 FPS, about 2.266667 s). The prefab's separate chargedStrikeClip is unassigned. Other .anim files include earlier experiments; their existence does not prove that they are active.

Prefab charge settings observed: fullChargeTime 1.8 s, chargeWindupTime 0.48 s, maxChargePosePhase 0.28, heavy damage phase 0.35–0.43. Scene overrides/runtime state may differ; inspect before making timing changes.

`SahurAttack.cs` owns normalized hit windows, charge pose time, release and combo logic. `CanMoveDuringCombat` permits charging and right-arm heavy release. `UsesAnimationRootMotion` differs by action. `ThirdPersonPlayerController.cs` and `SahurRootMotionRelay` can apply combat displacement despite Animator.applyRootMotion being false. Do not replace this with a blanket root-motion policy or duplicate hitbox AnimationEvents.

Camera: third-person gameplay camera is present in the controller/game; exact framing, distance and FOV for a new move are **undefined until inspected in the relevant scene**. A Blender preview camera is not a substitute for Unity gameplay capture.

### Shark and editable sources

`ArtSource/SharkAnimation/tralalero_animated.blend` is an editable existing master. Actual scene: 60 FPS, metric scale_length 1, Tralalero_Rig, 16 bones, active Swim_Cruise. Blender forward -Y / up Z is documented in `Tools/BlenderSharkAnimation.py`.

Important bones: Body, Spine_Mid, Spine_Rear, Tail_Base, Tail_Mid, Tail_Tip, Head, Jaw, Fin_Left, Fin_Right, Dorsal, Foot_Left/Right/Rear, Nose_Marker and Tail_Marker. Preserve marker names used by encounter code. Body is the top bone; it is not an independent zero-motion root control. Choose root sampling and water displacement ownership per action.

| Action | Source frames | FPS | Duration | Unity looping |
|---|---:|---:|---:|---|
| Swim_Cruise | 1–97 | 60 | 1.6 s | yes |
| Swim_Fast | 1–61 | 60 | 1.0 s | yes |
| Tail_Strike | 1–70 | 60 | 1.15 s | no |
| Ship_Smash | 1–70 | 60 | 1.15 s | no |
| Bite_Lunge | 1–67 | 60 | 1.1 s | no |
| Hit_Recoil | 1–49 | 60 | 0.8 s | no |
| Threat | 1–109 | 60 | 1.8 s | no |

Unity: `Assets/Resources/SharkAnimation/TralaleroAnimated.fbx`, Generic Avatar, `TralaleroAnatomy.controller` and `TralaleroRig.prefab`. Existing importer bakes root rotation/Y/XZ into pose and runtime owns world placement. The original shark builder exports all seven Actions; the new pipeline defaults to one Action per versioned FBX. Do not run the old builder merely to inspect or export one revision: it reconstructs the rig.

`ArtSource/ShipWreck/ship_g_fractured.blend` is an existing ship fragment source. It is unrelated to a character action unless the approved action needs ship interaction.

`BlenderWork/character_fixed.blend` was inspected read-only: Armature, 73 bones using Bone/Bone.001-style identifiers, no Actions, 24 FPS. It has not been verified as the active Sahur Humanoid animation skeleton. Do not rename those bones to fit naming examples or assume retargeting compatibility. The adjacent character_before_skin/before_finger_correction files are retained historical masters.

### Pipeline conventions and undefined decisions

- Effective FPS = fps / fps_base. Preserve the source rate: active player assets include 30 and 60 FPS; shark is 60; old character master is 24. A universal preferred authoring FPS is **undefined**.

- Clip duration = (last source sample − first source sample) / effective FPS; source endpoint samples are inclusive. Gameplay windows are half-open intervals.

- New names: PLAYER_ATK_Heavy_01, PLAYER_Run_F, ENEMY_Name_ATK_01, BOSS_Name_ATK_TailStrike_01. Existing Action/state/bone names are not migrated automatically.

- FBX initial contract: -Z forward / Y up, file units respected, import globalScale 1, no new leaf bones, one selected rig/Action, no all-Actions/NLA bake, step 1 and simplify 0. Verify orientation/scale after actual import.

- Root policy is per action and runtime ownership; universal root-motion policy is **undefined**. Ground contacts, in-place compensation velocity, cancel windows and camera need action-specific evidence.

- Other playable characters/enemies, boss phase library, formal art bible, final approval authority beyond the requesting user, and required quality score thresholds are **undefined**.

- All important new moves require a presented design and actual user approval or explicit waiver. Review compares against DESIGN/BLOCKING/FIRST PASS/SECOND PASS/POLISH/FINAL/APPROVED and revises specific problem ranges.

Evidence snapshots: `verification/project-unity-audit.json`, `verification/shark-source-audit.json`, `verification/character-source-audit.json`. Source files remain the authority; repeat inspection when assets change.

## Revision history

Later production reports document changes to the October rig and presentation. [Opening shark polish](../OpeningSharkPolish-20261031.md) · [Directional jump chop](../JumpStable-v278.md) · [Aircraft capture claws](../MechanicalCaptureClaws-20261104.md) · [Van Gogh rendering comparison](../VanGogh2-Results-20261109.md)

The older session chronology records individual takes, rejected trials and unfinished checks. [Original animation chronology](https://github.com/cpy114514/Dark-Brine_Rot/blob/adbe8cc794659768260f8ff7b007003fb6de6264/Documentation/AnimationWorkflow/project-animation-context.md) · [Opening session checkpoints](https://github.com/cpy114514/Dark-Brine_Rot/blob/adbe8cc794659768260f8ff7b007003fb6de6264/Documentation/AnimationWorkflow/Actions/CH1_SahurSharkEncounter_v001/resume-at-0250.md)
