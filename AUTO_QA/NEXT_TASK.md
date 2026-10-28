# Continuation — Batch 012

Latest validated work: four pause/settings bugs QA-019–022; one production script, 33 accepted checks including 147 native UI frames. Resolve checkpoint titled "Repair pause menu ownership, reload recovery and Escape consumption" with git log. GitHub main and Unity Version Control uploads are authorized; inspect actual heads before syncing, never repeat the completed batch011 merge. Integration was cs192, followed by report checkpoint cs193.

Current expected workspace: same Editor PID 57472, Play stopped, five first-island scenes loaded/clean; setup marker cleared after restoration. Check actual state before changing scenes and preserve new unsaved user work. No fixture/input owner should remain.

## Next wider batch

1. Inspect PlayerDeathRespawn and LocalizedGameText reload/event ownership in actual fixtures; suspected only, not verified bugs. Add scene/disable transitions and test keyboard/button continuity. Batch012 already covers normal map/equipment/pause/death/respawn input; do not rerun unchanged full suites.
2. Reproduce BombardinoBoss.Tick elapsed-time loss above .1 s with terrain/wing collision evidence before any timing repair. Projectile/ordnance stepping already repaired in batch010; keep existing combat design.
3. Profile native scene CPU/render work after the integrated grass changes. Footprint SetAlpha allocation is already measured/fixed (24 KB → 0 per 1,000 warmed calls with valid 1 MB controls); do not use GC.GetAllocatedBytesForCurrentThread, whose positive control fails in this Editor. No whole-game FPS acceptance yet.

## Safe execution and recovery

- Read all five reports first. Broader batches are requested; group distinct evidence-backed checks/fixes. Keep user-facing updates concise and notify only verified improvements, major failure or required action. No routine approval questions.
- Check Git status, preserve user changes, back up dirty source and commit only owned hunks. Never add secrets, .codex backups, Library/Temp or new generated caches. Full pre-existing sources/art were authorized and tracked in 7417515; SandFootprints patch is historical/already applied.
- Use one existing Editor. PrepareIsolatedEditor/RestoreWorkspace in Tools/IslandValidationRegression.cs use SessionState AUTO_QA.Batch002.SceneSetup. If interrupted, inspect the marker before another fixture; preserve any newer user workspace. Never save test scenes over assets.
- Snapshot current saves into a new ignored .codex/autoqa-save-backup/batchNNN directory; compare only that baseline. Never restore an old session over legitimate progress.
- Tools/PauseRegression.cs needs isolated Play, no active Pixel360/Volumes and already initialized migration preferences. NativeControls uses real authored menu and CombatFeelPreview with guarded temporary input settings; restore exact old object/flags/current devices/enabled roster and clear CombatCombinationTests. Cleanup restores quality/pipeline/audio/time/cursor. Snapshot key AUTO_QA.Pause012.Snapshot must be empty after completion. Test scripts stay outside Assets.
- NativeInputSmoke old historical V276–V278 helpers lack the input-settings guard; avoid those. InputManager destroys replaced settings having exactly HideAndDontSave.
- Some source truncating writes fail UNKNOWN. Preserve exact original, use guarded r+ extending writes and verify byte-for-byte; do not change permissions or close user processes. Main-thread calls may time out despite execution: inspect result files/markers before retries; foreground the same Editor when needed.
- Existing missing prefab GUIDs 2bde5fdeb204bc6418608a5fe3993ad1 / 8e67ad946c4264b479267ee1643fd2ce / ad3d230ef428f6049ab82c5136f00a47 account for 606 instances. Assets unavailable; preserve them, do not repeat unchanged searches or delete plants.
- Editor was restarted externally in batch011; handles dropped from ~519k to ~11k, now ~19k. Leak cause remains unproven; bound heavy tests. CLI beta13 connected; no upgrade/reset purchase authorized or needed. At quota exhaustion save exact state, resume on a later actual-allowed execution.
