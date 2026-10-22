# Restore approved twin-blade moves after rifle use — 2026-10-21

The user requested restoring the last working twin-blade version rather than redesigning the moves. Live inspection found Sahur equipped with twin blades, combat enabled, no narrative/control locks and no Console errors. His visual Animator was still using `Equipment/M4A1/SahurRifle.controller`, as intended by the rifle's shared-graph implementation. That graph was an older copy: its blade finisher, heavy and charge all referenced `CapriSlash`, and it lacked Blade Spin, Blade Guard and the Twin Blade Charge layer. The two melee controllers already held the approved moves. Nothing in the preceding tutorial-contact fix had modified the blade scripts or animation assets.

## Repair

Restored the existing approved melee states, timing, transitions and charge mask from `Encounter/v006/SahurGameplay.controller` into the rifle's shared graph through Unity Editor APIs. This includes `BladeFinisherV002`, `BladeRushV002`, `BladeCharge`, Blade Spin, Blade Guard and the existing club combination states. Added the missing shared animation parameter and upper-body charge layer. Transitions reference states in the destination controller; no runtime graph swapping or new per-frame processing was introduced.

The original rifle layers and their eleven named serialized objects remain byte-identical. Existing blades, meshes, clips, damage, 1.2x dimensions, 1.2s minimum/2.6s full charge, 16m travel, rush-only immunity and frontal guard rules remain unchanged. Both combination and v002 polish installers now include the rifle graph so that future installation of these moves reaches all three gameplay controllers.

Reproduction/repair tools: `InspectTwinBladeRestoreV235.Run`, `RestoreTwinBladeControllerV235.Run`, `VerifySharedMeleeControllerV235.Run` through Pipeline `run_script`, using the corresponding `Tools/*.cs` files. The repair tool requires Edit Mode and saves only its target asset.

## Actual verification

Existing Unity Editor 7800 only, native prefab, actual Input System keyboard/mouse, physical controller and native-camera captures. Temporary arena; all original scene behaviour ownership restored afterward.

- `TestDualBladeRestoreV235.Start30`: **49 passing checks**, **935 consecutive frames**. Actual rifle equip/fire followed by club and blade moves, 1.2x hit volumes, three-hit combo, X charge, long rush, real blade contacts, wall stop, guard, spin, dodge, jump chop, story/disable cleanup, club throw and three repeated rifle/blade switches. Rifle layers release their pose ownership after each switch.
- Native final-pose measurements: finisher **359.113°**, blade vertical component at most **0.001063** during the revolution; rush follow-through blade components **-0.621 down / +0.351 backward**. Reviewed consecutive camera sheets for finisher, rush, guard and spin. Authored approved clips are reused unchanged.
- `TestRushGuardV235.Start30`: **48 passing checks**, **882 frames**, explicitly starting with a real rifle equip before switching to twin blades. Travel **16m**, physical wall limits travel to **1.58179m**, **25/25** observed rush Move callbacks protected. Real continuous blade contact, charge thresholds/cancellation, startup/recovery vulnerability, guard front/rear, actual projectile/sonar/blast/strafe paths, interruption cleanup and nonlethal narrative floor pass.
- `VerifySharedMeleeControllerV235.Run`: **42 controller contracts pass** for approved states, timings, destination references, upper-body mask/ownership, shared parameters and retained rifle layers. Runtime Console errors **0**; Editor reports compiling false.

Evidence is retained under `.codex/twin-blade-restore-20261021/`. `ReviewTwinBladeRestoreV235.py` reproduces pose metrics, consecutive review sheets and `combinations-final-30hz/TwinBladeRestoredV235.mp4` without retiming. Simulation rate is not a release FPS measurement. This pass verifies the regression in the shared graph; full-island AI combat, every terrain/camera case, hardware audio and release performance have not been retested. No new animation quality or universal clipping approval is claimed.

Before testing, the current human save was backed up independently of older tasks. After confirmed Play Stop, diagnostic saves were retained separately and the current human save restored, SHA256 **80AE18C49EB1023655B5BA73412809312F95500C2FC3655062AE3EBF4285E976**. Original dirty `Story1.unity` remains loaded without save/reload/discard; its unsaved contents were also saved as a copy outside Assets before testing. Editor ends stopped and unpaused. Prior tutorial-contact edits are retained. No commit, push, build, second Editor/Player or scheduling.
