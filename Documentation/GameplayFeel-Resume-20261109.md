# Latest authoritative status — completed gameplay revision

The focused movement/combat revision is implemented and verified. Read `Documentation/GameplayFeel-Results-20261109.md` first. Historical entries below describe intermediate states and do not mean those steps are still pending.

Branch `codex/gameplay-feel-20261109`: audit9044089a, ground/legs7e36fbf0, local stopb2f603d8, movement/cadence integrationb5fc5b78. Final evidence/report commit: bbef1e6b. Ordinary moving twin blades are included and must remain. No execution feature was added.

Accepted routes: flat30/60/120Hz20 assertions each; weapon rules60Hz18; handoffs30/60/120Hz4 each; real elapsed actions12; actual Island1 terrain20; actual Island1 ownership/impact/pause/low-gravity/ocean14. Expanded native blade regression929 checks48 cases passes again after final stride changes. Ground guard checks6 pass. Same-condition current×1.85 steep sprint60Hz improves24/120→120/120 support, other current-rate enabled probes all supported. Existing leg masks preserve weapon arms; moving blade hitstop holds attack but advances legs. Warmed changed-method measurements record0 managed allocations, not whole-game GC/FPS claims.

Visual comparison `.codex/gameplay-feel-20261109/blade-before-after.mp4`, left original slash legs/right mobile legs, reviewed via matched sampled frames. Editor is stopped; formal save bytes unchanged and original clean Second Island scene setup restored. Recompile reports up_to_date,failed=false,errors=[]. Existing unrelated second-island work is preserved and uncommitted. No new EXE or remote upload.

Human play/animation approval, fullcampaign/allBoss coverage and release-build hardware latency remain honest limitations, not deferred implementation phases. Do not repeat the completed revision merely because a scheduled heartbeat fires. Van Gogh Rendering2.0 is also now implemented and verified; read `Documentation/VanGogh2-Results-20261109.md` and its latest resume status. At scheduled wake-up, continue only genuinely unfinished new work; if neither revision has remaining implementation, do not invent extra work.

Two one-shot thread continuation automations exist:5h rendering recovery (`van-gogh`) and10h5m unfinished-work recovery (`automation`). They must not cause intentional deferral. At wake read current state, continue only unfinished work; if everything is complete, do not manufacture extra work. The user may run out of quota; durable checkpoints are recovery aids, not completion claims.

---

# Gameplay feel overhaul — continuation checkpoint

This is a recovery point, not authorization to defer work. User explicitly asked to keep working now and schedule a one-time continuation ten hours five minutes later in case their usage allowance runs out. At a scheduled wake-up, inspect actual current progress and continue ONLY unfinished work; do not redo completed changes or invent additional scope.

Current authorized work: implement the advanced movement/combat feel request in `C:/Users/pinyu/Downloads/message.txt` in Unity project `D:/indiegameDev/unity/Dark Brine_Rot`. It asks for audit, movement/locomotion/terrain, hit feedback, input/attack integration, camera, independent tunables, real native30/60/120Hz checks, visual comparisons/video if Unity can run, scoped rollback commits and a report of actual improvements/limits.

Separate pending rendering request: Van Gogh Rendering2.0, with its own five-hour continuation already scheduled. Read `Documentation/VanGogh2-Resume-20261109.md` before that work. The two requests are independent; preserve all completed work and avoid duplicating work if another continuation has already completed it.

## State at initial checkpoint

- Editor is Unity6000.3.23f1 / URP17.3.0, live CLI port7800. Stopped, clean Second Island scene. `unity status` detects it. Windows PowerShell, full file access, approval never; never pass sandbox_permissions.
- Numerous unrelated local second-island/UI/animation/scene changes remain dirty and MUST be preserved. Git branch was main, HEAD73d9ea2f. Do not blindly stage/commit the entire workspace.
- Earlier separate request completed ordinary twin-blade movement: two conditions in SahurAttack.cs permit WASD/sprint through ground combos and disable their physical root-motion translation. Existing animations/contact clocks are preserved. Native regression passed929 checks/48 directional cases across three controllers. See `Documentation/TwinBladeMovingAttacks-20261109.md`, `.codex/twin-blade-moving-attacks/native.json` and preservation.json. Formal saves and scene setup unchanged. Do not revert.
- Execution feature was explicitly canceled. Read the beginning of `Documentation/AnimationWorkflow/project-animation-context.md`. The attached task's mention of executions does NOT authorize restoring them.
- A previous, substantial control/feedback pass already exists. Read `Documentation/SahurControlFeel-20261104.md`, `ThirdPersonPlayerController.Feel.cs`, `CombatFeelProfile.cs`, `CombatLocalHitStop.cs`, `CombatHitFeedback.cs`, `CombatCameraShake.cs`. Reuse the architecture; do not replace it merely to satisfy the new document.
- No production changes made for this new comprehensive feel task yet. Inspection has begun; continue the audit and obtain reproducible before evidence before tuning.

## Architecture and candidate issues to verify (not confirmed findings)

The CharacterController owns normal physical motion in ThirdPersonPlayerController.Update. SahurRootMotionRelay forwards Animator delta ONLY when UsesAnimationRootMotion is true. SahurTwinBladeMotion.LateUpdate owns the heavy rush and respects movement/plot locks and local hitstop. Preserve that split, sea/soft-island movement and the player's animation/state authority.

Existing control tuning has Balanced/Responsive/Weighty in `Assets/Resources/Combat/SahurCombatFeel.asset`, no need for a duplicate config. Balanced already improves acceleration/braking/redirection, visual turn, camera follow, dodge/recovery and upper-body club attacks. Existing inputBuffer asset is about.26s, longer than the document's80–150ms exploratory range; do not shorten blindly and regress late commitment handoffs.

Candidate checks:
1. `CombatLocalHitStop.Hold` uses until=max(until,unscaledTime+seconds), with each call clamped. Repeated calls across frames can renew it indefinitely; reproduce before deciding on a bounded burst plus a short actual-resume interval. Existing whole-body and attack-layer freezes must restore only their owned speeds and retain movement/input collection.
2. IsGroundedOrOnSea uses only CharacterController.isGrounded or a floating deck. Normal slope adhesion relies on vertical groundStickSpeed5. At9.6/16.8m/s downhill this may lose ground; measure on actual15/30/40-degree slopes, steps and Island1 before adding a short speed-aware snap. Never pull rising jumps/chops down, snap off ledges, climb walls or alter soft-island bounces/sea/deck behavior.
3. Newly mobile ordinary blade slashes retain full-body poses. Existing `Lock-on Footwork` lower-body layer in the three controllers could provide walking legs for ordinary blade stages0/1 while retaining attack torso/arms, excluding the rotating finisher/continuous spin/rush/jump/plot. Inspect the real layer mask/blend tree and animated foot behavior before extending it. Current UpdateLockFootwork uses lock-on only, no combat, no sprint, updates LockMoveX/Y from planar velocity. No new assets/states implemented yet.
4. Combat response has no separate acceleration coefficient yet; add only if actual movement/camera/animation measurements justify one.
5. CombatCameraShake is bounded in amplitude but uses repeated-duration renewals. Camera collision and restoration need actual visual checks, not speculative rewriting.

Source inspection: character jumpBuffer.15/coyote.10 already exist; jump ballistic displacement is integrated before gravity. Aerial chop momentum must remain as patched v274; v275 blade spins and latest ordinary moving slashes must remain. Attack input buffers already preserve local-stop remainder and reject expired commands. Hit feedback is unified in CombatHitFeedback.Apply, confirms accepted HP change and uses per-swing victim sets in attack code. Boss CombatImpactResponse.unflinching/stopMultiplier0 prevent interruption. Style Changer and shaders are outside this gameplay task.

## Existing verification infrastructure

- `Tools/ReviewGameFeel072.cs` provides actual keyboard/mouse input playback, traces and screenshot scopes at fixed30/60/120Hz and real-time; read its Prepare/config/run contract carefully. Do not overwrite old evidence or blindly run old installers.
- `Tools/AuditFeel072.cs`, `Tools/ValidateFeelAssets072.cs`, `Tools/TestCombatFeelIntegrationV245.cs`, `Tools/TestCombatFeelContracts.cs`, `Tools/VerifyTwinSpinMovementV275.cs` are useful references.
- `Assets/Editor/VerifyTwinBladeMovingSession.cs` isolates save path on Play and reports restored scene/save bytes. Do not use test unlimited-stamina mode when checking real costs: its existing native moving-blade test temporarily disables that convenience during synchronous checks and restores it.
- Temporary actors must live in a separate runtime scene. SecondIslandAwakening.BlocksInputFor applies to all controllers in its scene; merely setting ExternalControlLock=false does not bypass that narrative lock.
- Formal save snapshots in ignored .codex are private; never upload.

## Sources consulted

Official CharacterController.Move and Blend Tree docs and paper https://arxiv.org/abs/2208.06155 were opened. The paper's evidence highlights coordinated hitstop, coherent sound and camera, not simply stronger VFX. Use APIs matching installed packages (InputSystem1.20, no Cinemachine dependency). Unity CLI/pipeline and shader skills were already read; for this task unity-csharp-scripting and performance-optimization skill were read and should be announced when applied. No subagents unless explicitly authorized by user/applicable skill.

Continue autonomously now. Record actual findings, back up/stage only scoped edits, implement meaningful fixes and test/iterate; do not call this checkpoint a completed overhaul. Update this file as progress advances.

## Progress update — Phase A and B

Branch is now codex/gameplay-feel-20261109. PhaseA committed as9044089a, only the audit doc and two new native tools. Actual baseline under .codex/gameplay-feel-20261109/before-{30,60,120}.json reproduces40° downhill sprint intermittent support and0 animation-resumed frames under repeated local holds for~.39s.

PhaseB now implements a short downward-only ground-follow probe in ThirdPersonPlayerController.Feel.cs, called inside the existing single Move displacement path. New groundFollowDistance=.6 in the existing ControlResponse, saved natively to SahurCombatFeel.asset.60Hz40° sprint improves29/120→120/120 supported samples without increasing horizontal travel. Three controllers now have a new weight-zero Mobile Blade Legs layer using the original legs-only mask and walk/jog/directional clips, activated only for moving grounded ordinary blade stages0/1. Mask checks show0 right-arm/hand pose change and moving feet; finisher priority retained. Builder Tools/InstallMobileBladeFootwork20261109.cs is idempotent and used native APIs. Backups in controllers-before and source-before. Probe phase-b-60.json includes these checks. Final rendered review and full regression still pending.

NEXT: finish scoped PhaseB commit if not yet present, then fix the confirmed repeated-hitstop renewal in CombatLocalHitStop with a bounded burst plus actual-resume grace, preserving public Release semantics. Let active mobile-blade lower-body locomotion continue using the existing LightPlaybackSpeed parameter to freeze only the attack state; CanMoveDuringCombat must NOT be broadened because that can replace the full-body attack with Locomotion. Controller.BladeFootworkActive provides the narrow condition. Native probe animation progress needs AlwaysAnimate and a no-hold control to avoid culling/time noise; the last phase-b hitstop sample was not accepted as a fresh baseline, so rely on the original before files. Continue integration/visual/frame-rate tests and later report, do not stop at this checkpoint.

## Progress update — Phase C

PhaseB committed7e36fbf0. PhaseC implemented: CombatLocalHitStop bounds a group of holds to first-start+.13s, then restores playback for.035s. Explicit Release retains immediate original handoff semantics. New fields saved natively to the existing CombatFeelProfile. Mobile ordinary blade slashes with active footwork freeze only LightPlaybackSpeed, leaving legs live; spins retain whole-body freeze. Probe now sets Animator AlwaysAnimate and includes an unheld clock control. phase-c-{30,60,120}.json show all40° sprint samples grounded and resumed frames under repeated holds, plus0 attack-phase/positive leg-phase movement during held blade impacts across all3 controllers. Expanded native twin-blade regression passes929/48 again, updated its assertion to distinguish ordinary attack-layer stop vs full-body spin stop. Editor stopped, formal saves and scene restoration checked by existing session helper.

NEXT: commit scoped PhaseC files if not yet committed, then do PhaseD/E integration and actual Island1 rendered input review. Existing Tools/ReviewGameFeel072.cs is a useful actual-input/camera harness, but read setup/cleanup before reuse and use a new evidence directory, isolated runtime scene/save path, preserve latest user scene setup. Need jump/ledge/step exclusions, club/charge/jump/dodge/counter/rifle and camera checks at30/60/120, real enemy/boss reactions, performance/alloc sampling, before-after images/video, final report and scoped commits. No need to repeat already passed targeted probes unless a new concern/change warrants it. Finish the task now; delayed automations are recovery only.
