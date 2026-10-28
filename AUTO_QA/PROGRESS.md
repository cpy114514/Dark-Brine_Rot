# Progress

## 2026-10-28 — Batch 010 complete (expanded user-requested scope)

- User explicitly requested larger batches covering more systems. Investigated aircraft ownership/death, projectile time/physics, blast/fire contracts, camera offsets, footprint function/performance qualification and two-weapon input integration; do not revert to ending after one tiny check.
- Resumed from 62a136b, saved current per-batch save and four source snapshots. Three Bombardino source files were clean at entry; untracked SandFootprints source preserved exactly. No reset/purchase.
- Initial ownership matrix failed 22/44 checks: disabled Boss still emitted at 95 particles/s; inactive target or disabled PlayerHealth kept attacking and spawned delayed fire. Fixed target validation before delayed impacts/reacquisition and emitter owner gate. Final 44 checks pass.
- Timing probe failed 6/9: one-second step became .1 s, ground fire outlived a 5.1 s tick, bomb gravity depended on frame partition. Full elapsed time is consumed through bounded swept substeps and analytic gravity; final nine checks pass with .000229 m trajectory error at four partitions. Eleven collision/cover/dedup/fire-height/cadence checks pass. Fire does not catch up missed damage pulses.
- Footprint/performance + camera investigation: initial zero-allocation result rejected after 1 MB positive control also returned zero. No speculative footprint edit. Seven valid functional tests pass, including 100 camera offset/remove cycles and footprint fading/property isolation. Initial contact test assumptions about mitigation/recovery were corrected; rejected evidence retained.
- Native player integration passes 28 checks / 1,256 frames for both weapons. Original input settings object/flags/device roster/current devices/capture delta and preview independently restored; raw trace losslessly compressed with byte-verified round trip. Total accepted assertions: 99 across five suites. No visual polish claim or full boss encounter claim.
- Workspace changed outside the QA fixture to clean CombatTest. Guards prevented test execution there; archived both old/new setups and preserved the latest workspace as the restoration target. Final CombatTest loaded/clean, Play stopped, setup/fixture markers clear. Old five-scene setup is archived, not restored over the new workspace. Save/backup hashes unchanged from batch010 snapshots.
- Editor transition network/dispatcher errors persist; actual state inspected, existing Editor focused, no second Editor launched. Native PID 35488: 518,647 handles / 174 threads, responding.

Checkpoint: commit titled `Repair aircraft hazards and preserve projectile elapsed time` (resolve with git log). Next expanded batch: aircraft AI long-frame clock/flight and terrain safety, valid-target reacquisition, and one independent player/camera or measured-performance path.

## 2026-10-28 — Batch 009 complete (hourly continuation)

- Resumed from 53a73ca. Read current QA records and inspected Cappuccino AI, presentation, health and user-edited ultimate. Snapshot current source/save bytes in ignored batch009 paths; one existing Editor, no quota reset or purchase.
- Confirmed QA-012 on native attack frames: three attacks x component/object disable keep Attack state and deal 7/11/12 damage shortly after re-enable without another telegraph. Eighteen of 36 pre-fix assertions fail. Requested observation wait is .05 s, native-frame quantized, not a precise display-FPS measurement.
- Cancel unfinished combat on disable, preserve Dead, and defer interrupted-pose recovery to the next valid Tick. First repair was superseded after Console review caught ten inactive Animator warnings; final repair eliminates them. No animation asset or ultimate-design change.
- Final recompile succeeds. Accepted 37 cancellation checks + 15 regression checks = 52 passing assertions, zero inactive Animator warnings. All normal fresh attacks still hit; numeric ultimate charge/empower/heal/immobilize/spent/death contracts pass. Native action begins through reflected entry points, then advances on normal frames; not a full encounter.
- Normal source truncation write failed with unchanged readback; a guarded extending r+ write succeeded exactly. Existing CappuccinoAI and CappuccinoUltimate user changes preserved. Only QA diff against entry backup is staged; ultimate source is untouched.
- Network/main-thread calls failed during transitions; checked actual state/markers rather than replaying unknown mutations. Bringing the same Editor forward restored response; auto-tick confirmed enabled at 16 ms.
- Original five scenes restored loaded/clean, Play stopped, no compile failure, setup/fixture markers cleared. Save/backup hashes equal fresh batch009 snapshots. Final resource query: 510,092 handles / 178 threads, responding; infrastructure issue remains open.

Checkpoint: commit titled `Cancel unfinished Cappuccino attacks on disable` (resolve with git log). Next: Bombardino death/disable cleanup of delayed ordnance and flame attacks in an isolated fixture.

## 2026-10-28 — Batch 008 complete (hourly continuation)

- Resumed from e024611, read QA reports and Unity workflow instructions. Same project Editor PID 35488; fresh per-batch save/backup and AI source snapshots saved under ignored .codex paths. No reset/purchase.
- Built isolated native Nailong cancellation fixture. Initial graph-teardown suspicion rejected: existing AttackMotion.Stop already handles it, with 75 baseline checks passing. One helper-only compile needed the Playables extension namespace; corrected before execution.
- Verified QA-011: deactivated target remains selected through acquisition and Attack continues, releasing pending saliva. Inactive-target controls fail six of 20 checks while destroyed-target controls pass.
- Minimal production correction: clear inactive target/health before the throttled acquisition gate; use existing StopPresentation when leaving Attack for Idle. No new architecture, clip, controller or design change. Source was clean at entry and exact intended edits match its backup.
- Fresh recompile passes. Post-fix: 20 target-loss/reacquisition checks, 75 interruption/death/disable/return checks across five patterns, six launched-effect/death checks = 101 passing assertions. Launched glob/first wave preserved on target loss, queued glob/later emissions cancelled; death cleans effects. No visual-improvement claim.
- One network error occurred before the target-loss after run: verified no result, marker or boss fixture before retry; accepted result then completed. Test helpers and explicit observations are not NUnit or a full encounter.
- Restored all original five scenes loaded/clean, Play stopped, no compilation failure, setup/fixture markers cleared. Human save/backup hashes match current batch008 snapshots. Native resources: 497,916 handles / 175 threads, Editor responding; prior infrastructure issue open. Known missing-prefab errors remain.

Checkpoint: commit titled `Stop Nailong attacks when the player target becomes inactive` (resolve with git log). Next: Cappuccino AI/ultimate cancellation and delayed attack cleanup, preserving its pre-existing user edits.

## 2026-10-28 — Batch 007 complete (hourly continuation)

- Resumed from 2e2558d. Extended the existing native-input harness with bounded SRP readback and a fixture follow camera; no production source, prefab, scene or animation asset modified.
- Initial take01 failed a mistaken assertion: single Freeze(.35) is capped at .13 s and the early click validly reached the queue window within .22 s. Corrected the test rather than changing working gameplay. The rejected take and reason are preserved.
- Accepted take02 passes 15 assertions / 985 native observations and captures 87 rendered frames for both weapons. Repeated .06 s stops renewed for .35 s test actual expired early taps versus intentionally retained authored-window inputs. Both paths pass; independent trace analysis confirms expiry before queue admission and valid continuation after 823/900 ms.
- Reviewed nine selected accepted images of startup, swing, jump contact and grounded recovery. Sparse captures establish pose rendering only; no visual smoothness, full-rate playback, gameplay camera, target-contact or artistic approval claim.
- All original five scenes restored loaded/clean, Play stopped, input settings object/flags and physical current/enabled devices restored, fixture/setup markers cleared. Current save and backup hashes match this batch's fresh ignored snapshots. Existing user changes preserved.
- Existing Editor PID 35488 responding, 489,418 handles / 183 threads; infrastructure issue remains open. Console shows known missing-prefab restore errors. Raw result archives are byte-verified lossless gzip; images use existing Git LFS rules.

Checkpoint: commit titled `Capture native combat poses and verify combo hit-stop buffering` (resolve with git log). No production bug fix was justified this batch. Next: focused Nailong attack cancellation/death cleanup and hit-window ownership using the existing small fixture; inspect existing Tools test helpers before execution.

## 2026-10-28 — Batch 006 complete (hourly continuation)

- Resumed from 7b57684; read QA records, ordinary usage available with refreshed five-hour window, no reset/purchase. Same Editor; final native query ~481,138 handles/171 threads, responding.
- Targeted native queued dodge taps at attack phases .20/.45/.60. Before fix: 60 Hz simulation take records 2 stale consumptions (stick 267 ms, blades 366 ms versus configured 220 ms + measured two-frame tolerance); 30 Hz simulation take records 1. Traces show the combat deadline advancing after key release while the movement deadline stays fixed.
- Small repair: forward the fresh input edge, keep readiness separate and let combat own one lifetime. Public one-argument cancel API retained; buffered guard/spin exits clear the consumed buffer. Original cancel phases, stamina costs and animations preserved.
- After: 14 boundary checks / 1,467 frames at 60 Hz simulation, 14 / 749 at 30 Hz, plus 28 normal-flow checks / 1,258 frames. Total 56 checks / 3,474 native observations pass, zero stale cases. Fresh inputs still survive a short stamina-unavailable interval; guard/spin exits work. All five before/after runs restore input state.
- A two-file apply encountered a motor-file write failure after updating the first file. Readback confirmed the partial state; ordinary truncating writes failed, ReadWrite open succeeded. A backed-up, guarded extending r+ write completed exactly and full recompile passed. No permissions or user processes changed; no partial fix remains.
- Both gameplay files already held user changes. Preserved original bytes; exact reconstruction verifies only QA deltas. Staged only QA source hunks (attack +5/-3, motor +4/-1); reduced patch context excludes the user's neighboring translation changes.
- Original scenes restored loaded/clean, Play stopped, markers cleared, settings identity/flags/current device state independently restored; save/backup hashes unchanged. Accepted traces are losslessly compressed with byte-verified round trips; expected failing reproduction takes retained.

Checkpoint: commit titled `Expire dodge cancels from the original input press` (resolve with git log). Next: focused rendered startup/recovery/landing review and early combo/hit-stop boundaries, then enemy/boss coverage. No visual animation improvement claimed this batch.

## 2026-10-28 — Batch 005 complete (hourly continuation)

- Resumed from 3970951 and read all reports. Usage permits ordinary work; no reset/purchase. Same Editor ~466,030 handles/176 threads, responding; isolated scene used.
- Implemented a current queued-input runner using actual PlayerInput, dynamic queued keyboard/mouse events and normal 60 Hz captured simulation frames. No direct attack callbacks or Animator phase injection for the tested actions.
- Accepted take02 passes 28 checks over 1,258 observed native frames: both weapon three-hit combos, recovery dodge/air-flip, moving charge/release, simultaneous jump chop/landing, control lock and keyboard weapon switching. No new production gameplay bug exposed; no game source, art, prefab, controller or animation asset changed.
- Rejected take01 passed gameplay assertions but failed cleanup: package InputManager.settings destroys replaced temporary defaults with exactly HideAndDontSave. Recovered all 23 original managed setting fields exactly into a valid native runtime object. The destroyed old native identity is not claimed as recovered. No settings asset was rewritten; first take remains failed in evidence.
- Guarded lifetime handling protects the original default before replacement, then restores its same object, flags/focus policy and all physical device enabled/current states. Independent take02 inspection confirms identity/flags/policy, original device IDs 1/2 enabled, zero capture delta and cleared preview. Final harness compiles after adding cross-assembly cancellation lookup. Forced-interruption cancellation was not exercised in this batch.
- Original five scenes restored loaded/clean, Play stopped, fixture/setup markers cleared, native settings valid, save and backup hashes unchanged. Raw traces are preserved losslessly in byte-verified gzip archives; readable summaries and rejected/accepted provenance retained.

Checkpoint: commit titled `Add native combat input smoke with safe settings restoration` (resolve with git log). Next: targeted input-buffer/cancel boundary cases and native rendered startup/recovery evidence; do not repeat the whole passing normal-flow suite without a relevant change. Historical V276–V278 input helpers need the same temporary-settings lifetime guard before reuse.

## 2026-10-28 — Batch 004 complete (hourly continuation)

- Read the five reports and resumed from 6b764aa. Usage permits ordinary work; no reset/purchase. Same Editor, ~455,134 native handles/177 threads; used a small unsaved isolated scene.
- Inspected current movement, melee, combinations, loadout and existing native test harnesses. Reproduced QA-008: total ExternalControlLock/disabled movement did not gate light/aerial callbacks; queued attacks spent stamina under the lock, LateUpdate could reopen hitboxes, and direct delivered contacts with confirmed native weapon overlap still damaged a target.
- Accepted baseline matrix confirms both actual weapon modes across all three current controllers: 33 failures / 84 assertions. Initial harness enum typo and weapon-reset-order diagnostics were corrected before accepted evidence; no production compile failure is inferred from harness compilation.
- Minimal fix adds a shared total-control/disabled-owner predicate at four existing input/hit gates. Movement-only platform locking keeps its prior light-attack behavior; existing animation timing, source curves, combo costs and design preserved.
- Recompile completed failed=false/errors=[]. Final matrix passes all 96 checks, including 12 added positive unlocked-contact/deduplication assertions. Device roster/enabled states/current devices, camera tags and cursor restored; no physical device disabled. Tests invoke actual gameplay callbacks/Animator/physics synchronously and do not claim a keyboard/rendered playthrough.
- SahurAttack.cs already held unrelated V272–V278 edits. Original working bytes and original user diff are preserved in ignored .codex/autoqa-source-before/. Only QA's five source hunks were applied to the Git index (5 additions / 4 deletions). Byte-level reconstruction verifies no other working source changes.
- Temporary fixture removed; original five scenes restored loaded/clean, Play stopped, no compilation failure, setup marker cleared. Save and backup hashes unchanged. Known missing-prefab errors recur on restoring the environment; no new gameplay exception appeared in the captured interval.

Checkpoint: commit titled `Respect total player control ownership in melee input and hits` (resolve with git log). Next: natural frame-driven queued-input smoke for combo continuation/expiry, dodge buffering/cancel, held charge/release and jump/landing recovery. Use the existing copied-input-settings/focus-restoration pattern; do not call the direct-callback matrix a physical-input test.

## 2026-10-28 — Batch 003 complete (hourly continuation)

- Resumed from a586744 and all five reports. Ordinary usage allowed; no reset/purchase used. Same project Editor remains connected; native query still shows ~443,310 handles/174 threads, so runtime work used an isolated unsaved scene.
- QA-005 is reproduced in a current fixture with actual domain reload: native 28-object footprint pool survives, managed Stamp[] becomes null and Fade logs null references. Disabled and removed only the temporary fixture after capturing the failure.
- Smallest repair is one System.Serializable attribute on Stamp. Fresh script compile passes. A second actual domain reload passes 13 checks preserving cache entries, all native object/mesh/material identities, next slot, lifetime, live prints, original fade alpha/expiry and bounded pool across five enable cycles. By subsequent native frame 2687 versus assertion frame 612, no further footprint errors appeared in the captured post-repair console interval.
- Pre-existing SandFootprints.cs remains untracked. Its original bytes are preserved in ignored .codex/autoqa-source-before/SandFootprints-batch003.cs. Only the one-line repair patch, before/after hashes and repeatable QA evidence are checkpointed; do not mistake a clean Git checkout for the full pre-existing feature. Reverse patch check passes against working source.
- One premature Prepare call correctly rejected a not-yet-entered Play state before creating a fixture. The redundant reload request had no test fixture; it is not counted as validation. Subsequent native state gating confirmed Play and an initialized fixture before the accepted actual reload.
- Fixture cleaned; Play stopped; original five scenes restored clean without saving any scene, no compile failure, setup recovery marker cleared. Restore timed out at dispatcher but completed independently. Save and backup SHA256 still match local safety copies.

Checkpoint: `8a155b1` — Preserve footprint pool across Play-mode script reload. A patch-format follow-up removes an empty context line that Git's whitespace checker flagged; forward application checks against the preserved original and reverse checks against repaired source both pass, with no production code change. Next: current isolated character/combat input smoke (buffering, dodge/cancel, charge and landing), selecting a verified defect before changing combat. Footprint per-frame property-block allocations are only a source-level candidate until measured.

## 2026-10-28 — Batch 002 complete (hourly continuation)

- Read all five QA records and latest checkpoint fc2e6c4. Usage check permits ordinary work; no reset credit or purchase used.
- Native resource check: ~8.2 GB free physical memory, but Editor still ~433,940 handles / 170 threads. Used a small isolated scene rather than the full island runtime. SandFootprints source remains pre-existing and untracked; preserved without importing its unrelated feature into this checkpoint.
- Reproduced QA-007 with native SerializedObject.ApplyModifiedPropertiesWithoutUndo in Play: six lifecycle warnings while OnValidate created shore child/components, terrain immediately changed from 33 to 49 vertices.
- Minimal tracked source fix: queue validation rebuilds and consume them in LateUpdate; explicit/context-menu/OnEnable rebuild clears the request. Original geometry generation, materials, collisions and gameplay design remain unchanged.
- Recompile completed with failed=false/errors=[]. Eleven targeted native checks pass, warnings fall from six to zero. Test explicitly invokes LateUpdate after native validation for deterministic scheduling/geometry checks; it is not a performance or visual quality benchmark.
- Original five clean scenes were saved as an in-memory SessionState setup, temporarily unloaded for the isolated test, then restored without saving any scene or creating any scene asset. Final live inspection confirms all five loaded and clean, stopped Play, no compile failure, and recovery marker cleared.
- Initial reproduction and scene restoration exceeded dispatcher timeouts; the reproduction result file and final native scene state establish completion. No blind replay of timed-out mutations.
- Human save and backup hashes still match ignored safety copies. Unrelated worktree changes remain untouched.

Checkpoint: commit titled `Defer procedural island rebuilds out of validation callbacks` (resolve with git log). Next task is QA-005 isolated SandFootprints reload repair with narrow provenance, followed by native combat input tests when Editor resources permit. Known missing GUID blocker remains unchanged.

## 2026-10-28 — Batch 001 complete

- Read Unity CLI/Pipeline skills and animation router; confirmed one connected project Editor and no current compilation failure.
- Preserved initial dirty-worktree inventory; no original asset or scene changes made by QA.
- Discovered repeated combat visual/Nailong HUD exceptions and missing environment dependencies. See BUG_REPORT.md.
- Enabled existing Editor auto-tick at 16 ms; did not start another Editor.
- Created hourly heartbeat `dark-brine-rot-qa`, attached to this chat, at minute 10. Resume reads these files; quota recovery is checked at a later execution, never assumed. No reset credit or purchase authorized/used.
- User explicitly requests autonomous work without routine questions and continuation after five-hour quota recovery.

QA-001 and QA-002 fixed with narrowly scoped production edits. Fresh missing-AI regression passes; actual Play-mode domain reload passes 15 lifecycle checks; existing V278 animation contracts pass 30 checks. Initial baseline: 96,226 scene objects, zero missing scripts, 606 missing prefab instances. Original scripts, art and scene files retained. No scene save or core design change.

Editor returned stopped after testing. Save/backup timestamps predate the QA Play sessions; save SHA256 `09439469086C64A92F1518ABC8BA1D8B546A4E5EEB0B4C8FBC5F474DA8BEE231` matched the local safety copy after testing. Copies are in ignored `.codex/autoqa-save-backup/`, not committed.

Infrastructure warning: some calls timed out during scene startup and domain reload; test results come from completed result files and native checks, not timeout responses. An ad-hoc diagnostic eval failed due to reversed namespaces for AdditiveSceneBootstrap/FirstIslandArrival; this was harness-only compilation, not a project compile error. No dependent gameplay claim was made from it.

Checkpoint: `e2389b7` — Recover combat visuals and Nailong HUD after script reload. Base HEAD at entry: `dbf282d`. Preserve the initial dirty-worktree inventory. Next priorities: SandFootprints reload failure, ProceduralIsland validation warnings, native input/combat smoke and bounded performance evidence.

End-of-batch resource blocker: bundled PowerShell also failed on startup with System.OutOfMemoryException (the reported configuration/security message wraps that allocation failure). A separate Git command subsequently completed successfully; verify actual command output before retrying mutations. Native Node file access/execFile remains available as a lightweight fallback. Heavy Play testing is deferred until resource availability stabilizes; no claim that the resource failure is solved.

## 2026-10-28 — Batch 011 — expanded batch

User requested Unity Version Control integration, GitHub upload/merge, performance and UI bug fixes. Full legitimate pre-existing workspace checkpoint 7417515 was pushed with 388 LFS objects (~1.1 GB); caches/recovery files excluded. PR #1 is attached. Incoming Plastic cs190/191 imported; Environment three-way merge preserves 41 independently changed prefab documents. Three mesh-reference conflicts resolve to byte-identical generated mesh bodies after ignoring document ID; local equivalent references retained and all incoming objects preserved. Whole-scene SmartMerge was interrupted; recovered using bounded document merges, with backups and proof. Original Plastic client configuration restored.

Editor was externally restarted between sessions (35488 → 57472), with five clean first-island scenes and no old recovery marker. New per-session scene preservation used before isolated tests. Performance optimization passes valid native allocation profiling, eight functional checks and 13 actual reload checks. Two UI reload defects reproduced and repaired; final 12 checks pass. One UI test incorrectly required destroying a still-owned canvas; rejected result retained, assertion now accepts owned retention.

Final workspace acceptance: merged Environment loads in the same Editor; five original scenes are clean, Play stopped and recovery marker cleared. Console reports only the same three missing-prefab groups (234+177+195=606). Saves and backup exactly match batch011 copies. Original client configuration restored. UI/camera fixtures absent. Full diff formatting reports inherited Unity asset whitespace; QA source/docs formatting is checked independently.

Batch011 completion: validated source Git checkpoint 3a4ca40; PR #1 merged to GitHub main as c6d8146e0e5b03b4ddcfbbe1812a57aced73728c, verified by fetch/ancestry. No GitHub CI checks are configured; native tests provide acceptance. Unity Version Control cs192 is at /main head, with no unfinished merge and only the two pre-existing recovery additions pending. Plastic requires atomic incoming ancestry: 99 unchanged upstream cs191 cache CP records were preserved, with no new local generated cache added; Git excludes them. New Editor native handle count is 11,093 versus prior ~519k; this is an observed restart baseline, not an established leak fix.

## 2026-10-28 — Batch 012 — four verified UI repairs

Continued from Git 9cf0ee7 / Plastic cs193 with a clean Git workspace and the same Editor PID 57472. Captured fresh save/backup copies and preserved the five clean scenes before isolated Play. Native authored pause prefab reproduced disable freeze, unrelated-instance release and actual-reload cache/button failure. A separate queued Esc probe then reproduced same-frame reopening across two menus.

One production script repaired: explicit pause owner, disable cleanup, serializable settings/shadow ownership, managed cache/listener reconstruction and per-frame Esc consumption. Final acceptance: 6 lifecycle + 11 actual reload/settings retention + 16 native input checks = 33, including 147 native frames. Saved/default settings, original Soft light-shadow cache, title-screen cursor, Resume button, single option handler and runtime pipeline ownership pass. Real I/M/Esc/Enter input covers equipment/map exclusivity, simultaneous hotkeys, death and respawn. Input settings/device enabled/current state restored exactly; fixtures cleaned.

The initial final-input request failed at transport after recompile. Evidence/marker inspection showed it had not executed; foregrounding the same Editor restored service, then the accepted take completed. No duplicate Editor or input fixture. Earlier failed takes are preserved as confirmed reproductions, not passing tests.

Final workspace: same Editor, Play stopped, five original scenes loaded/clean; scene/pause/input markers cleared, preview absent, compilation clean. Native handles 19,274; leak cause remains open. Known missing prefab count remains 606. Git formatting checks pass for this batch.

## 2026-10-28 — Batch 013 — two-system batch

Continued from Git ba4f06d / Plastic cs194; same Editor PID 57472, five clean scenes. Fresh saves captured before isolated Play. Reproduced three death reload failures and preserved all original death/label evidence. Small PlayerDeathRespawn repair retains disabled ownership and rebinds death/Respawn handlers. Original localization passes; no speculative text-system edit.

Aircraft matrix confirms elapsed-time loss in patrol, authored low-pass, return, phase-2 salvo and death. Final Tick wrapper preserves time in <=1/60 s steps. The first elapsed-time candidate omitted native transform sync and regressed a large-step low-pass up to 423.48 m vertically; rejected. Sync between catch-up steps removes that regression. Final horizontal deviation <=.000245 m; vertical <=.083313 m over a raised ground discontinuity, within one bounded vertical step (.335334 m). Exact vertical .06 m tolerance was over-strict for differing fractional partitions and is superseded by separate horizontal/vertical bounds.

Initial clearance probes changed patrol cruise altitude to 4, which changed the clearance branch threshold. These are configuration stress evidence, not the authored low-pass baseline. A temporary box-sweep candidate gave no improvement and was reverted byte-for-byte before the timing patch. The shipped Boss diff changes only Tick stepping/synchronization and non-finite guards, preserving ground sampling.

Accepted 146 checks: death/label fresh 3 + actual reload/reenable 13; aircraft temporal/clearance matrix 80; existing hazard ownership regression 44; invalid/small-time inputs 6. Catch-up is qualified up to the tested 1.25 s input, not a whole-game FPS benchmark. Extra visual marker work in catch-up is a performance profiling candidate for the next batch.
