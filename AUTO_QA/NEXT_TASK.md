# Next task / restart instructions

1. Read all five AUTO_QA reports, git status and latest QA checkpoint before editing.
2. Confirm `unity status` and Editor play/compile state; use the existing Editor only. Current initial scenes: First Island Main/Lighting/Gameplay/Environment/Enemies, all clean in memory at entry.
3. QA-001/002 already pass real reload and missing-AI tests; do not repeat without a new change. BaselineAudit.Run accepts an optional label so follow-up snapshots do not overwrite baseline evidence. `LifecycleRegression.BeforeReload` stores native IDs in SessionState; request actual EditorUtility.RequestScriptReload (recompile alone reports up_to_date if no source changed), wait for ready, then AfterReload verifies reuse.
4. Next: QA-005 SandFootprints reload failure. Source was already untracked at entry. Preserve original separately before editing; record a minimal patch without claiming the whole pre-existing file as new QA work. Reproduce current failure with a controlled fixture, then repair pool lifecycle and test fade/expiry/bounded object reuse.
5. QA-003 exact GUID source is blocked (no current asset/meta/history match found; historical V253 reports same counts). Do not keep searching without new evidence. QA-007 ProceduralIsland OnValidate child creation and input/combat native smoke are actionable next tasks.
6. Update reports and tests with observed evidence; stage only QA-owned files. Never `git add -A` in this dirty project. Make a checkpoint only after relevant tests pass.

Pending CLI beta.14 update is optional infrastructure work and not required for game repairs; beta.13 is connected. Never infer a successful test from a trigger response.

Editor infrastructure: ~434k handles and thread-creation failure were observed. Do not force kill or launch a replacement. Bring the same Editor forward when scene startup stalls, allow completion, use bounded calls and document new infrastructure failures separately. Native image exists at Evidence/first-island-after-reload.png; no animation-smoothness claim yet. Save/backup safety copies in ignored .codex/autoqa-save-backup; compare/restore after each test if changed. Hourly heartbeat dark-brine-rot-qa remains active at minute 10.
