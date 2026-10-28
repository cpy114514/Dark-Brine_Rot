# Test results

## Batch 002 — complete

| Check | Observed result |
|---|---|
| Resource/usage recheck | Ordinary usage allowed. Native Windows query: ~433,940 handles/170 threads; ~8.2 GB free physical RAM. Resource issue remains open. |
| QA-007 before | Expected reproduction passes 9 checks; native SerializedObject validation rebuilds immediately (33 to 49 vertices) and emits 6 SendMessage lifecycle warnings. |
| QA-007 recompile | PASS: completed, failed=false, errors=[] |
| QA-007 after | PASS: 11 checks; native validation keeps 33 vertices and creates no child, explicit LateUpdate then produces 49 vertices; collider shares render mesh; material-only edits preserve terrain/shore vertex positions; one foam child after repeated validation; 0 lifecycle warnings. |
| Scope | Same Editor, isolated small unsaved Play scene; no full island runtime, input, animation or performance acceptance claim. |
| Workspace restore | PASS by final live inspection: original five scenes loaded/clean, Play stopped, scriptCompilationFailed=false, saved setup marker cleared. |
| Save and backup | PASS: both SHA256 values match ignored safety copies. |
| Console after test | Dispatcher timeout and known 606 missing-prefab restore errors remain; no new OnValidate lifecycle warning in post-test interval. |

Reproduction/restore dispatcher calls timed out, but execution completed; result file and independent final scene-state query confirm outcomes. Test scripts live outside Assets; no source scene, art, animation, or prefab rewritten. No automatic claim that repeated validations improved FPS.

## Batch 001 — complete

| Check | Observed result |
|---|---|
| unity status | PASS: one ready project Editor, Unity 6000.3.23f1, PID 35488 |
| editor_status | stopped, compiling false, domainReloadInProgress false |
| EditorUtility.scriptCompilationFailed | false (current baseline; no fresh compile requested yet) |
| list_tests mode editor | 0 tests discovered; this is not a passing regression suite |
| Console tail | FAIL: repeated CombatImpactVisuals.Sample and NailongHealthBar.Refresh null references; scene missing-prefab errors; CS0108 warning |
| Visual animation QA | NOT RUN |
| Fresh runtime smoke / domain reload | NOT RUN |

Completed verification superseding the pending rows above:

| Check | Observed result |
|---|---|
| Native scene audit | 96,226 objects; zero missing script components; 606 missing prefab instances in Edit Mode; Play strips missing-prefab placeholders, so Play's zero does not mean repaired |
| Missing-AI health-bar fixture before | FAIL with NullReferenceException in Refresh:82; expected reproduction, saved in missing-ai-before.json |
| Native reload before repair | FAIL: managed pulse entries lost, repeated visual/HUD/SandFootprints exceptions; native pool identities survived |
| Fresh recompile after repair | PASS: completed, failed=false, errors=[] |
| Missing-AI health-bar fixture after | PASS: no exception, fraction=1, AI absent |
| Actual Play-mode reload after repair | PASS: 15 checks in reload-after.json; native pool IDs unchanged, 8+2 caches restored, both ribbons build meshes, landing effect emits, disable clears effects, five enable cycles preserve renderer identities/count; Nailong HP/HUD and singleton panel restored |
| Current V278 animation contracts | PASS: 30 checks using Tools/VerifyStableJumpV278.cs; copied result in animation-contracts.json |
| Native Game-view capture | PASS: first-island-after-reload.png renders Sahur/grass/sea; no moving-animation acceptance claim |
| Console after validation reload | No CombatImpactVisuals or NailongHealthBar exception in captured interval; thread/Search/Version Control/procedural warnings remain in reload-console-after.json |
| Git diff --check (QA-owned files) | PASS |
| Save SHA256 comparison | PASS: current save equals local safety copy; save/backup timestamps predate this batch |

Unity Test Runner remains zero tests; the passing checks above are explicit native scripted regressions, not NUnit. No build, full boss fight, input playthrough, performance benchmark or comprehensive visual animation review performed.

Store reproducible scripts and compact machine-readable results under Tools/ and Evidence/. Distinguish historical logs, injected fault tests, normal gameplay, static inspection and visual observations.
