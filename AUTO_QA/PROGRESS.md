# Progress

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
