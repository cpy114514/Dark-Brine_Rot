# Next task / restart instructions

Latest completed batch: 006, commit titled `Expire dodge cancels from the original input press` (resolve with git log). Read the latest sections of all five AUTO_QA reports before work.

1. Check git status and the existing Editor. Preserve all pre-existing dirty/untracked source, animation, art and scenes. Use one Editor; do not launch a replacement or force kill it. Never git add -A. For already-dirty source, back up current bytes and stage only QA hunks.
2. Current editing workspace: First Island Main/Lighting/Gameplay/Environment/Enemies, all loaded and clean in memory, Play stopped. Check actual state again; never discard new unsaved user changes.
3. Snapshot the current human save and backup into a new per-batch ignored directory before runtime tests. Compare/restore only that batch's own baseline, never an older session's save over legitimate new progress. Earlier unchanged copies remain in .codex/autoqa-save-backup/.
4. Isolated tests use AutoQaIslandValidationRegression.PrepareIsolatedEditor/RestoreWorkspace. Saved scene setup uses SessionState key AUTO_QA.Batch002.SceneSetup. If interrupted with the marker present, stop Play and restore that setup before starting another fixture. Current marker is cleared. No source scene needs saving.
5. NativeInputSmoke.StartBoundary(take,hz) supports unique 30/60 Hz simulation takes. The guarded settings lifetime is required: InputManager destroys replaced objects with exactly HideAndDontSave. Existing historical V276–V278 helpers lack the guard. Restore settings object/flags, physical enabled/current devices, capture delta, preview and CombatCombinationTests marker after every run. Current settings/devices are valid and restored.
6. Complete one evidence-backed correction, run relevant tests, update reports and checkpoint QA-only changes. Preserve rejected takes and distinguish reproduction failures from accepted post-fix tests.

## Next high-value work

Capture native rendered startup, combo recovery and jump/landing transitions for focused visual review. Also probe early combo taps across local hit-stop using documented queue semantics: inputs entered in the authored combo queue interval intentionally survive recovery/hit-stop. Do not change that design merely because their short initial TTL elapsed. Select a newly reproduced issue before modifying animation. Broaden enemy/boss encounter coverage after these focused checks.

Batch 006 fixes renewed expired dodge input. Its 30/60 Hz boundary matrices and normal queued-input regression pass 56 assertions / 3,474 observations. Do not repeat the full suite without a relevant change. Batch 004 ownership matrix passes 96; visual/Nailong/footprint reload fixes and batch 005 normal-flow evidence already exist. None establishes current visual animation smoothness or exhaustive combat coverage.

## Known constraints and provenance

- Environment has 606 missing prefab references across GUIDs 2bde5fdeb204bc6418608a5fe3993ad1, 8e67ad946c4264b479267ee1643fd2ce, ad3d230ef428f6049ab82c5136f00a47. Source lookup is blocked and historical V253 reports match. Do not repeat searches or replace/delete plants without new source evidence.
- Native Editor handles reached ~481k with 171 threads; prior thread/resource/Search failures remain unexplained. Use bounded existing-Editor operations and verify timed-out mutations before retrying. Unity Mono reports zero handle/thread counts, which are unavailable data. Heavy island performance profiling remains pending.
- Motor source truncation writes failed; a backed-up, guarded extending r+ write succeeded. Preserve exact bytes and do not change permissions or close user processes to force an edit.
- SandFootprints.cs remains pre-existing/untracked with its Serializable repair applied. Only Patches/SandFootprints-serialize-stamp.patch is checkpointed; verify reverse --check, do not double apply. Its full original feature is not part of a clean Git checkout.
- Source backups for batches 004/006 are under ignored .codex/autoqa-source-before/. User V272–V278 animation/movement edits remain in the working tree.
- The footprint per-frame MaterialPropertyBlock allocation candidate is unmeasured. If runtime work is blocked, measure it using a bound delegate/native renderer before optimizing.
- CLI beta.13 remains connected; beta.14 update was not authorized and is not needed for current QA. Hourly heartbeat dark-brine-rot-qa remains active at minute 10.
