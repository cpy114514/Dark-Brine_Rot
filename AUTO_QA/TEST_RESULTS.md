# Test results

## Batch 008 — Nailong target-loss repair

| Check | Observed result |
|---|---|
| Initial cancellation suspicion | Rejected. Original 75 checks pass across ScoldingSpit/GroundSlam/Roar/Rampage/ShamelessCharge and heavy hit/death/AI disable/return. Actual mixer action weights exceed .5 before exit; existing indirect Stop releases graph. |
| QA-011 before | Expected FAIL: 6/20 assertions. Inactive target, acquisition due or deferred: Attack + graph playing + one queued glob released. Destroyed target controls pass. |
| Recompile | PASS: completed, failed=false, errors=[]. |
| QA-011 after | PASS: 20/20. Inactive/destroyed target with due/deferred acquisition enters Idle, releases graph, cancels pending release and reacquires an active target. Native follow-up frames remain Idle. |
| Cancellation regression after | PASS: 75/75, five imported attack patterns and four exits; pending glob cancelled, launched glob intentionally survives heavy-hit stagger. |
| In-flight regression | PASS: 6/6. Target loss preserves launched glob and first wave, stops pending glob/future waves, permits reacquisition. Death recycles glob and destroys wave on native frames. |
| Post-fix total | PASS: 101 explicit native assertions. State entry/cue boundaries use reflected AI methods and public damage; this is not a full autonomous encounter or NUnit run. |
| Cleanup | PASS: fixtures removed, five original scenes loaded/clean, Play stopped, compiling=false, scriptCompilationFailed=false, setup/fixture markers cleared. Save/backup SHA256 unchanged from batch008 baseline. |
| Infrastructure | Initial helper compile missing Playables namespace was corrected before execution. One after-test network failure had no result/marker/object and was safely retried. Console has only known CS0108/missing-prefab restore issues since prior cursor. |
| Visual scope | No captures or smoothness/pose-quality claims this batch. |

Evidence: nailong-cancellation-before.json, nailong-cancellation-after.json, nailong-target-loss-before.json, nailong-target-loss-after.json, nailong-inflight-after.json, nailong-target-loss-proof.json, nailong-cancellation-cleanup.json. Reproduction failure is expected; post-fix checks pass. QA source ownership is exact against entry backup, with no pre-existing NailongAI diff.

## Batch 007 — native render / combo hit-stop review

| Check | Observed result |
|---|---|
| Initial take01 | REJECTED harness assertion: single .35 s requested stop is clamped to .13 s, so the click can validly enter the authored queue interval before expiry. Four prior checks and 414 frame observations retained; not a gameplay regression. |
| Corrected take02 | PASS: 15 checks, 985 native frame observations, both weapons, 87 native SRP PNGs, no capture error. |
| Expired early combo input | PASS: both weapons clear intent before queueOpen .12, observed phases .105/.100; no next stage. Actual unscaled deadlines in combo-hitstop-review.json. |
| Accepted-window combo input | PASS: phases .313/.320, survives repeated local stop, consumed after 823/900 ms as designed. |
| Rendered scope | Nine selected stills inspected for idle/startup/swing/jump/grounded recovery pose rendering. No temporal-smoothness, gameplay-camera or contact-quality acceptance. |
| Input cleanup | PASS: original InputSettings object and HideAndDontSave flags, current Keyboard1/Mouse2 enabled, device roster matches, captureDelta=0, fixture preview removed. |
| Workspace cleanup | PASS: original five scenes loaded/clean, Play stopped, compiling=false, scriptCompilationFailed=false, setup/fixture markers cleared. |
| Saves | PASS: save and backup SHA256 equal fresh batch007 snapshots; no restoration needed. |
| Production source | Unchanged versus entry backup. No project recompile/build requested for external test-only changes; run_script compiled corrected helper successfully. |
| Console/resources | Known 606 missing-prefab errors on scene restore. No new runtime exception in captured interval. Editor responding, 489,418 handles / 183 threads; unresolved. |

Evidence: combo-hitstop-review.json; native-input/batch007-review01 (rejected), native-input/batch007-review02-before (accepted; historical name, no subsequent production fix), result summaries, raw-result.json.gz, renders and cleanup.json. Gzip round trips verified byte-for-byte. These are native scripted assertions, not NUnit or a full boss encounter. No repeat of unrelated passing matrices.

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
