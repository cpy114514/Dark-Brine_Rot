# Progress

## 2026-10-28 — Batch 003 complete (hourly continuation)

- Resumed from a586744 and all five reports. Ordinary usage allowed; no reset/purchase used. Same project Editor remains connected; native query still shows ~443,310 handles/174 threads, so runtime work used an isolated unsaved scene.
- QA-005 is reproduced in a current fixture with actual domain reload: native 28-object footprint pool survives, managed Stamp[] becomes null and Fade logs null references. Disabled and removed only the temporary fixture after capturing the failure.
- Smallest repair is one System.Serializable attribute on Stamp. Fresh script compile passes. A second actual domain reload passes 13 checks preserving cache entries, all native object/mesh/material identities, next slot, lifetime, live prints, original fade alpha/expiry and bounded pool across five enable cycles. By subsequent native frame 2687 versus assertion frame 612, no further footprint errors appeared in the captured post-repair console interval.
- Pre-existing SandFootprints.cs remains untracked. Its original bytes are preserved in ignored .codex/autoqa-source-before/SandFootprints-batch003.cs. Only the one-line repair patch, before/after hashes and repeatable QA evidence are checkpointed; do not mistake a clean Git checkout for the full pre-existing feature. Reverse patch check passes against working source.
- One premature Prepare call correctly rejected a not-yet-entered Play state before creating a fixture. The redundant reload request had no test fixture; it is not counted as validation. Subsequent native state gating confirmed Play and an initialized fixture before the accepted actual reload.
- Fixture cleaned; Play stopped; original five scenes restored clean without saving any scene, no compile failure, setup recovery marker cleared. Restore timed out at dispatcher but completed independently. Save and backup SHA256 still match local safety copies.

Checkpoint: commit titled `Preserve footprint pool across Play-mode script reload` (resolve with git log). Next: current isolated character/combat input smoke (buffering, dodge/cancel, charge and landing), selecting a verified defect before changing combat. Footprint per-frame property-block allocations are only a source-level candidate until measured.

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
