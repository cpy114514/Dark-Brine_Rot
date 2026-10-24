# Sahur melee completion v239 — 2026-10-22

User clarification authorizes jump chops for both melee weapons and single-hand club use. Existing ground combos, charged strikes, 16m twin-blade rush and guard remain. The earlier two-hand club-spin request is superseded by this one-hand clarification.

## Source review

Retained Sahur humanoid rig and full v001 JumpChop/ClubSpin Actions are duplicated into v003; originals are preserved. ClubJumpChop retains the original single-right-hand overhead arc with a small authored leg tuck. BladeJumpChop raises both blades in separate lanes and chops down/forward into low follow-through and recovery. ClubSpinOneHand retains the pivot/root/torso/right-hand arc and places the left arm in free counterbalance.

Actions PLAYER_ClubSpinOneHand_v003:frames1–55,60FPS,0.9s,loop; PLAYER_ClubJumpChop_v003 and PLAYER_BladeJumpChop_v003:frames1–52,60FPS,0.85s,nonloop. In-place controller-owned travel, retained humanoid Avatar. Chop active interval approximately normalized0.34–0.67 (frames18–35). No animation event adds damage.

Blender MCP was unavailable (localhost9876 refused); the configured portable Blender5.2.2 ran the existing pipeline in background. Source validation passes for all three Actions; skeleton rest matrices preserved. Source front/side/three-quarter poses inspected at lift, contact and follow-through in preview02. Blade preview01 used the wrong club proxy and is retained as rejected evidence; preview02 uses separate hand-bound blade proxies. Accepted for SECOND PASS import/native review only; not human FINAL approval. Runtime integration and actual weapon contact remain to be verified.

Initial native take `native-30` confirmed club chop contact and recovery, but failed the new simultaneous-input check. Reflection found the Editor was still running the prior assembly (no jump buffer field), so it is retained as a stale-assembly take. Explicit recompile and loaded-field verification are required before the replacement replay.

## Integration and actual verification

Three existing controllers (SahurGameplay, SahurGrounded, shared M4A1/SahurRifle) now reference the separate ClubJumpChopV003, BladeJumpChopV003 and ClubSpinOneHandV003 assets. Preserve the v003 mapping in the existing combination installer so rerunning it cannot silently restore the two-hand spin/old blade slash. No runtime bone posing, new VFX or root-translation system is added.

Ground attacks now require nonpositive physical vertical speed; ascending motion counts as airborne even if CharacterController.isGrounded retains the previous contact. A150ms jump-attack buffer handles LMB arriving before the movement update on the same Space press. It is consumed once and cleared on expiry, pause, disable/swimming, cancellation and equipment handoff. Physical jump travel remains owned by the CharacterController; original damage/stamina, ground combo, heavy rush and guard remain. Both new chop clips use normalized0.34–0.67 damage windows and existing continuous real-weapon sweeps. No radial landing damage.

Loaded-assembly reflection was checked before the accepted replay. `native-current-30` records367 final SRP gameplay-camera frames and passes25 real-input checks: rifle→melee switch; each weapon's early jump, correct full clip, actual one-per-victim contact, local-stop cleanup and landing; simultaneous jump+attack; falling/apex input; real misses; single-hand spin with free left palm >0.2m from the shaft and damage cleanup. Original target's existing hit reaction is visible. `controller-contracts.json` passes69 controller/clip/state/mask/clock checks across all three graphs.94 total checks pass.

Native pose sheets inspected at lift/contact/follow-through and three spin headings. Silent30Hz simulation preview `.codex/melee-completion-20261022/native-current-30/Sahur-MeleeCompletion-v239.mp4`; source previews and rejected stale-assembly take retained. One initial controller assertion expected the copied Unity clip's internal name to remain its Blender Action name; Unity names the copied asset after its filename. The corrected check verifies the actual exact asset path and independently checks humanoid motion/duration/loop/clock. This was a QA expectation fix, with no animation change after the accepted replay.

This is a targeted SECOND PASS gameplay checkpoint, not human FINAL artistic approval, all-target clipping proof, an all-frame terrain test or measured low-end Player FPS. Only30Hz gameplay simulation was rerun in this pass. Existing original dirty Story1 remains unsaved, Editor stopped/unpaused; human save restored after confirmed Stop. No build, push, UVCS operation, new Editor/Player or scheduling in this pass. Existing shark-film wave occlusion remains as previously documented.
