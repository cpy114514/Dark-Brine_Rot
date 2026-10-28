# Test results

## Batch 010 — expanded multi-system batch

Validation uses current working tree with existing user prefab/player/scene edits. Three production files were clean at entry; only their QA changes are checkpointed.

| Check | Result |
|---|---|
| Aircraft ownership before | Expected FAIL: 22/44. Owner disable emits 95 particles/s; inactive/disabled-health target retains attack and creates delayed fire. Death controls pass. |
| Aircraft ownership final | PASS: 44. Due/deferred acquisition, target/component loss, hazard retirement and death; emission zero and no late fire. |
| Ballistics before | Expected FAIL: 6/9. One step consumes .1 of 1 s; .208–12.657 m bomb errors across frame partitions; fire does not expire on 5.1 s tick. |
| Ballistics after | PASS: 9. Four partitions consume 1 s with .000229 m trajectory error; 5.1 s fire expires. Input partitions are simulation samples, not measured display FPS. |
| Contact contracts | PASS: 11. Bomb/missile sweep hits thin wall; zero delta inert; repeated detonation and multiple hurtboxes do not multiply damage; cover blocks blast; fire height/cooldown/expiry preserved. Raw 8-damage pulse mitigates to 6 by current player profile. |
| Rejected contact take | Four failures were fixture assumptions: raw damage ignored player mitigation and prior hit recovery was retained. Final direct-step cadence sample explicitly clears the independent player recovery clock; not a gameplay regression. |
| Footprint/camera function | PASS: 7. Alpha/fade/expiry/pool/property isolation; 100 shake apply/remove cycles, pause lifetime and disable release. |
| Allocation measurement | UNAVAILABLE. Bound 10,000-call result is zero, but explicit 1 MB positive-control result is also zero. Initial zero-allocation acceptance rejected. Strict control take deliberately fails; final functional-only take makes no allocation assertion. SandFootprints source unchanged. |
| Native player integration | PASS: 28 assertions / 1,256 native observations, both weapons. Input settings identity/flags and device state independently restored. |
| Accepted total | 99 assertions across five suites. Existing health/movement and reflected state/cue setups used; no NUnit/build/full-encounter/visual-smoothness acceptance. |
| Recompile | Both production edit cycles complete with failed=false, errors=[]. |
| Cleanup | Latest externally changed CombatTest restored loaded/clean, Play stopped, setup/fixture markers clear. Save/backup hashes equal batch010 snapshots; no reset/purchase. |
| Console/resources | Existing CS0108 and dispatcher timeout errors; no new gameplay runtime exception in captured interval. Same Editor responding, 518,647 handles / 174 threads; issue open. |

Evidence: batch010-proof.json and batch010-cleanup.json; batch010-workspace-change.json; extended-bombardino-before/after/final.json; extended-ballistics-before/after.json; extended-contacts-after/final.json; extended-hotpaths-before/control/functional.json; native-input/batch010-player-integration with independent restore and byte-verified compressed raw trace. Rejected/unavailable evidence is explicitly separated from accepted assertions.

## Batch 009 — Cappuccino interrupted attack repair

Validation uses the current working tree, including pre-existing user source/prefab edits; it is not a clean-HEAD checkout. Only QA source hunks (+19/-3) are staged, while the original AI (+15/-13) and ultimate (+16/-52) worktree differences remain.

| Check | Observed result |
|---|---|
| Before | Expected FAIL: 18/36 checks. TwinSlash/DashThrust/Whirlwind x AI component/whole-object disable retain Attack and deliver 7/11/12 damage after re-enable in requested .05 s observation. Native frame quantization applies. |
| First repair | Superseded: 36+14 functional checks passed, but Console recorded ten inactive Animator warnings from immediate pose recovery during object disable/destruction. Do not treat it as final acceptance. |
| Final recompile | PASS: completed, failed=false, errors=[]. |
| Final cancellation | PASS: 37/37. Disable sets Idle and closes damage; re-enable stays Idle or starts new Windup, old hit causes zero damage. Zero inactive Animator warnings, including fixture destruction. |
| Fresh attacks / ultimate | PASS: 15/15. Three native uninterrupted attacks still deal 7/11/12. Explicit phase-clock checks preserve charge lock, empowerment without health multiplication, heal/immobilization, one spent phase and death state. Zero inactive Animator warnings. |
| Accepted total | 52 explicit passing checks. Reflected action entry plus native AI/physics frames; ultimate clock is deliberately sampled directly. No NUnit/full boss encounter/visual-smoothness acceptance. |
| Preservation | Actual AI bytes equal exact desired patch against entry backup; ultimate bytes equal entry snapshot. Stage only QA source hunks because both files originally had user changes. |
| Workspace/saves | PASS: five original scenes loaded/clean, Play stopped, markers cleared, scriptCompilationFailed=false. Save and backup hashes match fresh batch009 snapshots. |
| Infrastructure | Initial truncation edit failed unchanged, guarded extending write readback matches. Transition network/dispatcher errors resolved by focusing same Editor. Final console retains known CS0108/missing-prefab errors, no inactive Animator warning. Resource issue open. |

Evidence: cappuccino-disable-before.json; superseded cappuccino-disable-after.json, cappuccino-regression-after.json, cappuccino-disable-cleanup.json; accepted cappuccino-disable-final.json, cappuccino-regression-final.json, cappuccino-disable-final-cleanup.json; cappuccino-disable-proof.json. Tests explicitly subscribe to native log callbacks for the inactive Animator regression.

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

## 2026-10-28 — Batch 011 — measured footprint and UI regressions

| Check | Result |
|---|---|
| Native allocation before/after | Valid positive controls: 16 × 1,048,608 bytes in each run. Footprint: 24,000 → 0 bytes per 1,000 calls. |
| Marker CPU time | 2.351 → .184 ms per 1,000 calls, isolated microbenchmark; no game FPS claim. |
| Footprint function | PASS 8: alpha, property isolation, lifetime/pool, lazy block recovery and camera feedback contracts. Final repeat after integration passes 8. |
| Footprint actual reload | PASS 13; preparation assembly unloaded. AppDomain.Id did not change and is not reload proof. |
| UI baseline | Expected FAIL, 7/12. Equipment reopen/pause/reenable fail; equipment and map canvases orphan. |
| UI final | PASS 12 after actual reload: reopen, pause/time restore, camera mask, canvas ownership, disable/reenable. |
| Rejected UI assertion | Initial fixed take 11/12 wrongly required destruction of a retained owned map canvas. Inspection confirmed one map root with unchanged ID; corrected ownership assertion passes. |
| Recompile | PASS failed=false, errors=[]. |
| VCS scene merge | 264,648 unique selected documents; 41 conflict-free document merges; 3 references to identical generated meshes; proof in vcs-scene-merge-proof.json. Native load acceptance pending below. |

Final workspace acceptance: merged Environment loads in the same Editor; five original scenes are clean, Play stopped and recovery marker cleared. Console reports only the same three missing-prefab groups (234+177+195=606). Saves and backup exactly match batch011 copies. Original client configuration restored. UI/camera fixtures absent. Full diff formatting reports inherited Unity asset whitespace; QA source/docs formatting is checked independently.

Batch011 completion: validated source Git checkpoint 3a4ca40; PR #1 merged to GitHub main as c6d8146e0e5b03b4ddcfbbe1812a57aced73728c, verified by fetch/ancestry. No GitHub CI checks are configured; native tests provide acceptance. Unity Version Control cs192 is at /main head, with no unfinished merge and only the two pre-existing recovery additions pending. Plastic requires atomic incoming ancestry: 99 unchanged upstream cs191 cache CP records were preserved, with no new local generated cache added; Git excludes them. New Editor native handle count is 11,093 versus prior ~519k; this is an observed restart baseline, not an established leak fix.

## Batch 012 — pause/settings and native UI integration

| Check | Result |
|---|---|
| Baseline lifecycle | Expected FAIL 3/6: active disable freezes; unrelated destroy/Resume releases owner. |
| Baseline actual reload | Expected FAIL 2/5; reopening throws ArgumentOutOfRangeException, Resume binding absent, prior pause/overlay not released. Pipeline ownership control already passes. |
| Final lifecycle | PASS 6/6 with authored prefab; includes modal mutual exclusion and native Resume button. |
| Final actual reload | PASS 11/11: old preparation assembly unloaded; pause released; settings/defaults and Soft light cache retained; reopen/Resume binding, title-screen settings/cursor, disabled Open, no duplicate handlers and pipeline ownership. |
| Multi-menu Esc before | Expected FAIL 15/16, only same-frame reopen assertion fails. |
| Native UI final | PASS 16/16 / 147 native frames; queued Esc/I/M/Enter on real Sahur preview, equipment/map/death interlocks, respawn, two-menu Esc. Zero captured runtime errors. |
| Input cleanup | PASS: original settings object/flags, device enabled states and current keyboard/mouse restored. Preview/fixture markers cleared. |
| Recompile | PASS failed=false, errors=[]. |
| Saves | PASS current save and backup byte-identical to fresh batch012 safety copies. |

Accept 33 assertions across the final lifecycle/reload/native suites. Setup/repeated takes are not counted again. Scripted tests do not establish visual layout, animation smoothness or overall FPS.

Final workspace: same Editor, Play stopped, five original scenes loaded/clean; scene/pause/input markers cleared, preview absent, compilation clean. Native handles 19,274; leak cause remains open. Known missing prefab count remains 606. Git formatting checks pass for this batch.
