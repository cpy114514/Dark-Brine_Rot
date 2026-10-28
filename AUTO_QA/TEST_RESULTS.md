# Test results

## Batch 006 — complete

| Check | Observed result |
|---|---|
| Before, 60 Hz simulation | Expected failure: 2 expired dodge consumptions among 6 taps, ages 267/366 ms against configured 220 ms plus ~35–36 ms measured frame tolerance; 996 observations. |
| Before, 30 Hz simulation | Expected failure: 1 stale blade tap at 267 ms; 505 observations. Hz denotes simulation delta, not actual display FPS. Input ages use unscaled time. |
| Repair compile | PASS: completed, failed=false, errors=[] after both source edits completed. |
| After, 60 Hz simulation | PASS: 14 checks / 1,467 observations; zero stale cases, original-deadline readiness recovery, spin/guard exits retained. |
| After, 30 Hz simulation | PASS: 14 checks / 749 observations; zero stale cases and same positive readiness/combination contracts. |
| Normal queued-input regression | PASS: 28 checks / 1,258 observations across both weapons; combos, dodge, moving charge/release, jump/landing, lock and switching retained. |
| Input cleanup | All five runs report inputRestored=true. Independent final run inspection confirms same settings object/flags/focus policy, original current/enabled devices and capture delta. |
| Source provenance | PASS: exact desired working bytes; user changes preserved; QA-only source hunks staged (9 additions / 4 deletions total). Original snapshots are ignored local backups. |
| Workspace/save | PASS: original five scenes clean/loaded, stopped Play, no compile failure, markers cleared; save/backup hashes unchanged. |

Accepted post-fix total: 56 assertions and 3,474 native observations. Raw traces use lossless byte-checked gzip archives. No visual smoothness, full boss encounter, hit-stop boundary coverage or frame-time improvement is claimed.

## Batch 005 — complete

| Check | Observed result |
|---|---|
| Initial take01 | REJECTED overall: all 28 gameplay assertions pass / 1,259 frames, but inputRestored=false. Temporary default InputSettings was destroyed during replacement by package ownership rules. |
| Recovery | PASS: all 23 prior managed settings fields copied exactly into a valid native runtime settings object; original native identity was destroyed and is not claimed restored. No settings asset overwritten. |
| Accepted take02 | PASS: 28 queued-input checks / 1,258 observed native frames, error=null, inputRestored=true. Both native weapons complete combos, recovery evasion, moving charge/release, simultaneous jump chop/landing, total control lock and digit1/2 switching. |
| Input scope | Synthetic device state events queued through actual PlayerInput and normal Animator/Update; no direct attack callbacks or phase injection. Not human hardware input or a rendered visual review. |
| Independent cleanup | PASS for take02: same native settings instance and HideAndDontSave flags, original focus policy, device IDs/current IDs 1/2 enabled, captureDelta=0, preview gone. Take01's first destroyed instance remains identified as rejected. |
| Final harness compile | PASS: dry-run compilation of final native-input and scoped recovery helpers, no execution/scene mutation. |
| Final workspace/save | PASS: original five scenes loaded/clean, Play stopped, no compile failure, markers cleared, input settings valid and devices enabled; save/backup SHA256 unchanged. |
| Evidence integrity | Both full native traces losslessly gzip-compressed and byte-checked; readable summaries keep all checks and section bounds. Raw safety copies in ignored .codex/autoqa-native-input/. |

No production game source fix, new visual animation observation, boundary-buffer acceptance, performance benchmark, full cinematic or boss encounter is claimed. Forced-interruption cancellation hooks exist but were not exercised by the accepted normal-completion take.

## Batch 004 — complete

| Check | Observed result |
|---|---|
| QA-008 accepted before matrix | Expected failure: 33 failed / 84 assertions across three current controllers and actual stick/twin-blade modes. Locked callbacks/buffers spend 3 stick or 2.25 blade stamina; confirmed overlapping trigger contact damages during total lock. |
| Repair recompile | PASS: completed, failed=false, errors=[] |
| Final after matrix | PASS: 96 checks, zero failures/error, inputRestored=true. New total-lock and disabled-owner rejection plus existing unlocked callbacks, buffer expiry/consumption, duplicate callback prevention, cinematic gate, movement-only mount attacks, real overlap, unlocked contact and one-hit-per-swing behavior. |
| Scope | Native actual prefab/Animator/controller/weapon and Physics.ComputePenetration; public/trigger callbacks directly delivered and phases set by harness. No claim of keyboard playthrough, full cinematic, visual smoothness, arbitrary collision coverage or boss fight. |
| Source ownership | PASS: original dirty working source backed up; exactly five QA hunks staged via git apply --cached (5 added / 4 removed lines); byte reconstruction preserves all pre-existing edits. |
| Input/camera cleanup | PASS: original device roster/enabled states and current devices, camera tags/cursor restored; physical devices not disabled. |
| Workspace/save | PASS: fixture gone, original five scenes loaded/clean, Play stopped, no compile failure, recovery marker cleared; save/backup SHA256 unchanged. |
| Console | Known missing prefab restore errors and CS0108 remain; no new runtime gameplay exception in captured batch interval. |

Harness-only enum mismatch and an initial weapon-reset ordering issue were corrected before the accepted 84-check baseline. Final harness includes 12 additional positive contact/deduplication checks. Tests are explicit native scripted assertions, not discovered NUnit tests.

## Batch 003 — complete

| Check | Observed result |
|---|---|
| Original actual domain reload | Expected failure reproduced: 28 native children survive; Stamp[] becomes null; Fade errors captured. Fixture disabled and cleaned after evidence. |
| One-line repair compile | PASS: completed, failed=false, errors=[] |
| Repaired actual domain reload | PASS: 13 checks in sand-reload-repaired-after.json; 28 cache entries/native identities, mesh/material/cursor/lifetime/active prints preserved. |
| Fade/expiry and enable cycles | PASS: original alpha curve numerically matches at 85% lifetime, expired stamp deactivates, five re-enables retain 28 original renderers/identities. |
| Normal runtime continuation | Native frame advances from check frame 612 to observed 2687; post-repair console query since seq 168950 contains only an already-in-Play warning, no footprint error. This is a small isolated pool test, not a beach traversal/visual/performance benchmark. |
| Patch provenance | PASS: original source backup retained; working repaired hash matches provenance JSON; git apply --reverse --check succeeds. Original full feature remains untracked. |
| Workspace/save integrity | PASS: original five scenes restored loaded/clean, Play stopped, no compile failure, setup marker cleared, fixture gone; human save and backup hashes unchanged. |

A premature repaired-fixture setup rejected its Play precondition without creating objects. Only the later state-gated initialized fixture and real reload count as accepted verification. Restore dispatcher timeout is documented separately; independent native inspection confirms completion. No current combat/animation/input acceptance claim.

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
