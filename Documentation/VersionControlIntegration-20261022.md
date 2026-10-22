# Unity Version Control local-priority integration — 2026-10-22

The user explicitly requested resolving UVCS conflicts with local work taking priority, then uploading the current project to GitHub. Workspace cs181 was behind `/main` cs182; Git initially had no unresolved entries and matched origin/main atff39f8a.

Before updating,352 local authored files (320,060,331bytes), Plastic metadata, the current human save and a copy of the original unsaved Story1 were preserved under ignored `.codex/uvcs-repair-20261022`. Ancestor/incoming contributors and native merge-tool decisions are retained there. An initial noninteractive update could not answer a directory conflict and made no completed update; the subsequent native update resolved it and completed with exit0.

## Resolution

- Resolve16 UVCS file conflicts through its configured merge tool. For text code, a normalized three-way merge keeps **local overlapping hunks**, while retaining compatible incoming additions. For serialized Unity scenes/binary data, keep the complete local contributor; the first-island Environment scene has no semantic Git diff.
- Keep the local deletion of ignored `Assets/_Recovery` against the two incoming scene/meta additions. Incoming copies remain in the preservation folder.
- Retain all v234 tutorial contact, v235 shared rifle/melee controller, v236 blade transitions and v237 shark continuity changes. The three frozen v237 source hashes still match. SahurAttack receives only the incoming charged-attack practice counter on top of the latest local blade code.
- Include compatible incoming island supply/recovery guidance, practice progression, UI/localization, existing ending dependency checks and recovery partial. Opening dependency regression reports0 missing clips. Do not replace the redesigned shark film with the historical ending.
- Native `cm status` now reports **cs182 - head**, no unresolved merge flags and no incoming progress file. Local modifications remain pending in UVCS; no UVCS check-in was requested. Client merge configuration is restored byte-for-byte after the operation.

## Validation and limits

The existing Unity6000.3.23f1 Editor compiles without compiler errors. All three actual Environment-scene regression groups pass: ExperienceRegression (6 categories), PacingExplorationRegression (5), PriorityFixRegression (8). The42 shared melee/controller contracts pass, including rifle layers, approved blade combo/charge/spin/guard states and masks. One cold additive-scene request exceeded Pipeline's60s transport timeout, but the actual test completed and wrote all three successful results; its timeout is retained as a tooling error, not described as a gameplay test failure or a clean Console. No additional Play replay or hardware performance claim is made by this integration pass.

Audit of2362 asset GUIDs (excluding local recovery/collaboration tooling folders) finds0 duplicates and0 orphan meta files. No conflict markers remain in project source/serialized assets. No backed-up local file is missing.15 backed-up files changed through compatible text integration; all other protected files remain byte-identical. The original dirty Story1 remains open unsaved. Human save SHA256 **80AE18C49EB1023655B5BA73412809312F95500C2FC3655062AE3EBF4285E976** is unchanged. Existing Editor only; no new Player/build. Raw recordings, client configuration, diagnostics, saves and machine caches stay ignored in Git.

GitHub upload includes the accumulated tutorial, blade and shark polish plus the above integration. The remaining v237 prone-shot wave/foreground occlusion is still documented; this version-control task does not claim to fix it.
