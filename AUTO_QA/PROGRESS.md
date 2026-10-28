# Progress

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
