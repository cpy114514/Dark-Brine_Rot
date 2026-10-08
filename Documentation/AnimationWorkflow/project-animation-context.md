# Dark Brine_Rot animation context

Verified 2026-10-06/07 against the live Unity 6000.3.23f1 Editor, source code, Animator assets and read-only Blender audits. This is shared context for all animation skills. Update facts with evidence; a historical asset is not necessarily an active animation.

## Artistic and gameplay intent confirmed by the user

- Relatively realistic motion, water and effects. UI is minimal black/white; illustrations remain coloured.
- Sahur's charged stick attack uses the right hand, raises the weapon and cuts diagonally downward. Charging and heavy release must permit locomotion, sprint and direction changes.
- The accepted source is Standing Melee Attack Downward; anticipation comes from the earlier part, release from the later part. Preserve the design when fixing local defects.
- Shark breaks the ship with its tail, remains reasonably submerged, and exchanges attacks with Sahur. Fragment boards settle by physics; Sahur can jump, swim, climb and steer a board. The encounter eventually ends in Sahur's defeat.
- Final encounter animation can begin on low player health, a randomized combat timer, or the previously specified shark-health condition. Check current encounter code and serialized tuning before authoring a replacement.
- Sound is temporarily disabled. Do not introduce new sound or unrelated effects to solve animation defects.

These facts establish existing intent, not approval for a newly designed move. For a materially new move, use action-designer and the user's design gate.

## Playable rig and active animation

SahurPlayer: `Assets/Game/Prefabs/Characters/Sahur/SahurPlayer.prefab`.
The actual player uses valid Humanoid `PbrSahurAvatar` from `Assets/Game/Prefabs/Characters/Sahur/Models/PbrSahur.fbx` and `SahurGrounded.controller`. The older `Models/sahur.fbx` is Generic with no Avatar; do not use it interchangeably with the active Humanoid model.

`SahurGrounded.controller` has Base Layer and masked `Charge Upper Body`, with `SahurChargeUpperBody.mask`. Charging/release uses `Gameplay/SahurStandingDownwardCharge.anim` (clip name SahurStandingMeleeDownward, 30 FPS, about 2.266667 s). The prefab's separate chargedStrikeClip is unassigned. Other .anim files include earlier experiments; their existence does not prove that they are active.

Prefab charge settings observed: fullChargeTime 1.8 s, chargeWindupTime 0.48 s, maxChargePosePhase 0.28, heavy damage phase 0.35–0.43. Scene overrides/runtime state may differ; inspect before making timing changes.

`SahurAttack.cs` owns normalized hit windows, charge pose time, release and combo logic. `CanMoveDuringCombat` permits charging and right-arm heavy release. `UsesAnimationRootMotion` differs by action. `ThirdPersonPlayerController.cs` and `SahurRootMotionRelay` can apply combat displacement despite Animator.applyRootMotion being false. Do not replace this with a blanket root-motion policy or duplicate hitbox AnimationEvents.

Camera: third-person gameplay camera is present in the controller/game; exact framing, distance and FOV for a new move are **undefined until inspected in the relevant scene**. A Blender preview camera is not a substitute for Unity gameplay capture.

## Shark and editable sources

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

## Pipeline conventions and undefined decisions

- Effective FPS = fps / fps_base. Preserve the source rate: active player assets include 30 and 60 FPS; shark is 60; old character master is 24. A universal preferred authoring FPS is **undefined**.
- Clip duration = (last source sample − first source sample) / effective FPS; source endpoint samples are inclusive. Gameplay windows are half-open intervals.
- New names: PLAYER_ATK_Heavy_01, PLAYER_Run_F, ENEMY_Name_ATK_01, BOSS_Name_ATK_TailStrike_01. Existing Action/state/bone names are not migrated automatically.
- FBX initial contract: -Z forward / Y up, file units respected, import globalScale 1, no new leaf bones, one selected rig/Action, no all-Actions/NLA bake, step 1 and simplify 0. Verify orientation/scale after actual import.
- Root policy is per action and runtime ownership; universal root-motion policy is **undefined**. Ground contacts, in-place compensation velocity, cancel windows and camera need action-specific evidence.
- Other playable characters/enemies, boss phase library, formal art bible, final approval authority beyond the requesting user, and required quality score thresholds are **undefined**.
- All important new moves require a presented design and actual user approval or explicit waiver. Review compares against DESIGN/BLOCKING/FIRST PASS/SECOND PASS/POLISH/FINAL/APPROVED and revises specific problem ranges.

Evidence snapshots: `verification/project-unity-audit.json`, `verification/shark-source-audit.json`, `verification/character-source-audit.json`. Source files remain the authority; repeat inspection when assets change.

Current integrated encounter revision (2026-10-07): `ArtSource/Encounter/v001/` and
`v002/` retain original rig names and add isolated actions. Unity loads the cloned
`Assets/Resources/Encounter/v001/EncounterShark.prefab/controller`; original assets
remain available. Actual contact snapshots are .65 s for new tail/ship strikes.
The v003 20.8 s paired finale owns cinematic pose/world placement with a shared manual
clock; live locomotion, attacks and wreck traversal retain existing ownership.
See `Actions/CH1_SahurSharkEncounter_v001/final-review.md` for the final evidence.

v003 adds three hops between real wreck boards and two shark breaches; the existing
Humanoid Jumping.fbx was copied to a versioned clip and retimed by pose phase. The
new 60 FPS / 3.2 s Generic Breach Action was authored on a copy of the original shark
rig in Blender. Active shark prefab/controller now come from Encounter/v003; v001
and original assets remain available. The final fallen pose stays held through
blackout while its world transform follows actual ocean height/slope. See
`Actions/CH1_SahurSharkEncounter_v001/traversal-v003-review.md` for current captures
and measured regression results. Tests must restore the current save after stopping
the Editor too, because shutdown can flush previously queued save data.

v004 contact integration retains v003 animation assets and the 20.8 s core clock.
Anatomical shark/wood solving runs after cinematic pose sampling; live attack damage
uses the solved pose. Ship fracture source is now
`ArtSource/ShipWreck/ship_g_fractured_v002.blend`, with irregular deck cuts and real
child fragments. Finale lead-in duration follows an animated walk to the board edge;
landings face outward. See `Actions/CH1_SahurSharkEncounter_v001/wreck-contact-v004-review.md`
and `Tools/SharkBoardContact-README.md` for measured tests and remaining wave occlusion.

v005 fragment collision integration gives every small wreck piece a dynamic body
and solid mesh contact. Twenty decks and 24 hulls use their authored child mesh
parts as compound colliders. Shark contacts and board surface queries inspect all
parts. Existing rig/actions and the core encounter clock are unchanged; see
`Tools/WreckMeshColliders-README.md` for physical contact/traversal verification.
