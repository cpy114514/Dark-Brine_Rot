# Next task / restart instructions

Latest completed batch: 009, commit titled `Cancel unfinished Cappuccino attacks on disable` (resolve with git log). Read the latest sections of all five AUTO_QA reports before work.

1. Check git status and the existing Editor. Preserve all pre-existing dirty/untracked source, animation, art and scenes. Use one Editor; do not launch a replacement or force kill it. Never git add -A. For already-dirty source, back up current bytes and stage only QA hunks.
2. Current editing workspace: First Island Main/Lighting/Gameplay/Environment/Enemies, all loaded and clean in memory, Play stopped. Check actual state again; never discard new unsaved user changes.
3. Snapshot the current human save and backup into a new per-batch ignored directory before runtime tests. Compare/restore only that batch's own baseline, never an older session's save over legitimate new progress. Earlier unchanged copies remain in .codex/autoqa-save-backup/.
4. Isolated tests use AutoQaIslandValidationRegression.PrepareIsolatedEditor/RestoreWorkspace. Saved scene setup uses SessionState key AUTO_QA.Batch002.SceneSetup. If interrupted with the marker present, stop Play and restore that setup before starting another fixture. Current marker is cleared. No source scene needs saving.
5. NativeInputSmoke.StartBoundary(take,hz) supports unique 30/60 Hz simulation takes. The guarded settings lifetime is required: InputManager destroys replaced objects with exactly HideAndDontSave. Existing historical V276–V278 helpers lack the guard. Restore settings object/flags, physical enabled/current devices, capture delta, preview and CombatCombinationTests marker after every run. Current settings/devices are valid and restored.
6. Complete one evidence-backed correction, run relevant tests, update reports and checkpoint QA-only changes. Preserve rejected takes and distinguish reproduction failures from accepted post-fix tests.

## Next high-value work

Inspect BombardinoBoss/BombardinoOrdnance/BombardinoFlameJet and existing Tools regressions. Probe death/disable cancellation of delayed impacts, live projectile ownership and lingering flame damage in a small isolated native fixture. Preserve attack timing and existing design; reproduce a concrete issue before changing code.

Batch 009 fixes Cappuccino stale attacks on re-enable. Final cancellation/native attack/ultimate-contract suites pass 52 checks, with zero inactive Animator warnings. Earlier functionally passing after takes are superseded because immediate CancelCombat pose recovery warned on inactive objects. Final code defers pose recovery to a valid Tick. CappuccinoCancellationRegression.Run(take,false/true) requires an isolated unsaved Play scene and uses AUTO_QA.CappuccinoCancellation (currently false). Do not repeat unchanged tests. Existing user AI/ultimate edits remain in working tree; checkpoint contains only QA hunks.

Batch 008 fixes Nailong inactive-target Attack continuation and queued spit release. All 101 post-fix checks pass; do not repeat unchanged matrices. NailongAttackMotion.Stop already stops imported graph playback; the earlier suspicion was rejected with 75 baseline checks. NailongCancellationRegression.Run, RunTargetLoss and RunInFlight take unique evidence names and use AUTO_QA.NailongCancellation as their temporary fixture marker (currently false). No visual quality acceptance from this batch.

Batch 007 completes the early-combo/local-stop probe with 15 passing checks and 87 rendered frames. Single Freeze requests clamp to .13 s; use repeated real Freeze calls for a longer stress stop, never assume requested duration. Early input expires before queue admission; accepted-window intent deliberately survives recovery. Initial take01 is a rejected test, not an open bug. Review StartReview(take) supports native captures; do not re-run unchanged coverage without new evidence. Sparse selected images are not full-rate smoothness acceptance.

Batch 006 fixes renewed expired dodge input. Its 30/60 Hz boundary matrices and normal queued-input regression pass 56 assertions / 3,474 observations. Do not repeat the full suite without a relevant change. Batch 004 ownership matrix passes 96; visual/Nailong/footprint reload fixes and batch 005 normal-flow evidence already exist. None establishes current visual animation smoothness or exhaustive combat coverage.

## Known constraints and provenance

- Environment has 606 missing prefab references across GUIDs 2bde5fdeb204bc6418608a5fe3993ad1, 8e67ad946c4264b479267ee1643fd2ce, ad3d230ef428f6049ab82c5136f00a47. Source lookup is blocked and historical V253 reports match. Do not repeat searches or replace/delete plants without new source evidence.
- Native Editor handles reached ~510k with 178 threads; prior thread/resource/Search failures remain unexplained. Use bounded existing-Editor operations and verify timed-out mutations before retrying. Unity Mono reports zero handle/thread counts, which are unavailable data. Heavy island performance profiling remains pending.
- Motor and CappuccinoAI source truncation writes failed; a backed-up, guarded extending r+ write succeeded. Preserve exact bytes and do not change permissions or close user processes to force an edit.
- SandFootprints.cs remains pre-existing/untracked with its Serializable repair applied. Only Patches/SandFootprints-serialize-stamp.patch is checkpointed; verify reverse --check, do not double apply. Its full original feature is not part of a clean Git checkout.
- Source backups for batches 004/006/007/008/009 are under ignored .codex/autoqa-source-before/. User V272–V278 animation/movement edits remain in the working tree.
- The footprint per-frame MaterialPropertyBlock allocation candidate is unmeasured. If runtime work is blocked, measure it using a bound delegate/native renderer before optimizing.
- CLI beta.13 remains connected; beta.14 update was not authorized and is not needed for current QA. Hourly heartbeat dark-brine-rot-qa remains active at minute 10.
