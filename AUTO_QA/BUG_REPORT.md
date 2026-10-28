# Dark Brine_Rot automated QA

## Baseline — 2026-10-28

Unity 6000.3.23f1; one project Editor connected, stopped in the five First Island scenes. HEAD at entry: `dbf282d`. Numerous pre-existing uncommitted changes are listed in `Evidence/initial-worktree.txt`; preserve them and commit only QA-owned changes. No existing AUTO_QA records or applicable AGENTS.md found. Unity reports no current script compilation failure. Unity Test Runner discovers zero EditMode tests; existing regression scripts use explicit static entry points.

## Verified fixes

- **QA-001 / P1 — FIXED.** Real Play-mode domain reload reproduced lost managed pulse/ribbon entries while native children survived. `CombatImpactVisuals` now reconnects those same children, meshes and materials in OnEnable; disabled components ignore external sampling. Native reload regression passes, including both ribbon meshes, landing emission and five re-enable cycles without duplicate renderers. See `Evidence/reload-after.json`.
- **QA-002 / P1 — FIXED.** Nailong's nonserialized BossHudPanel wrapper was lost during reload. OnEnable rebuilds only the missing wrapper and retires its orphan Canvas. Fresh Play, real reload and repeated enable cycles pass. Independent missing-AI fixture failed before and passes after: phase title now tolerates the optional AI. See `Evidence/missing-ai-before.json`, `missing-ai-after.json`, `reload-after.json`.
- **QA-007 / P3 — FIXED, batch 002.** Native SerializedObject validation in isolated Play reproduced six child-creation lifecycle warnings and an immediate terrain rebuild. ProceduralIsland now queues Play-mode validation changes for LateUpdate and coalesces repeated requests. Eleven targeted checks pass with zero lifecycle warnings; terrain/shore vertex positions, collider/render mesh synchronization and single foam child are preserved. See `Evidence/island-validation-before.json` and `island-validation-after.json`.
- **QA-005 / P2 — FIXED in working source, batch 003.** Actual isolated Play-mode domain reload reproduced a null Stamp[] cache while all 28 native stamp objects survived. Adding only System.Serializable to the Stamp struct lets Unity preserve its private hot-reload cache. Thirteen checks pass after actual reload: all object/mesh/material identities, pool cursor, active prints and lifetime retained; original fade alpha/expiry and five enable cycles remain correct. Source was pre-existing/untracked, so Git checkpoints only `Patches/SandFootprints-serialize-stamp.patch` plus provenance/tests; the full original feature remains untracked and preserved. See Patches/README.md and sand-reload-*.json.
- **QA-008 / P2 — FIXED, batch 004: melee bypasses total control ownership.** In a native grounded fixture, ExternalControlLock still allowed light callbacks, queued attacks and stamina spending; locked aerial callbacks and contacts also passed. A disabled movement owner had the same callback hole. Accepted pre-fix matrix: 33 failed assertions out of 84 across three controllers and both actual weapon modes. One shared predicate now guards callbacks, buffer processing, LateUpdate sampling and final contact acceptance. The final 96-check native matrix passes, including unlocked contact/deduplication and preserved movement-only platform attack behavior. This is native callback/physics-filter verification, not a full cinematic or keyboard playthrough. See `Evidence/combat-control-before.json`, `combat-control-after.json` and provenance.
- **QA-009 / QA tooling — FIXED in batch 005 harness.** Input System 1.20 automatically destroys replaced settings carrying exactly HideAndDontSave. The initial queued-input harness copied the historical V278 pattern and lost its temporary default settings during replacement, so cleanup failed. That take remains rejected. Its 23 managed settings fields were recovered exactly into a valid native runtime settings object; no project settings asset was changed. The guarded harness preserves/restores the original settings object and flags. Accepted take02 passes 28 gameplay checks over 1,258 native frames and independent restoration checks. Historical review helpers need the same lifetime guard before reuse. See `Evidence/native-input/README.md`.
- **QA-010 / P2 — FIXED, batch 006: released dodge taps renew their own buffer.** Movement forwarded its buffered flag each frame, which combat interpreted as a fresh press and renewed the deadline. At 60 Hz simulation stepping, released inputs caused evasion after 267 ms (stick) and 366 ms (blades), beyond the configured 220 ms plus two observed native frames; the 30 Hz simulation probe also reproduced a stale blade tap. Movement now forwards the fresh input edge and separate readiness; combat keeps one deadline, including when stamina briefly prevents consumption. Legacy single-argument API, guard/spin exits and cancel thresholds remain. Two boundary matrices plus normal queued-input regression pass 56 checks / 3,474 native observations, with zero stale cases after repair. See `Evidence/dodge-buffer-proof.json` and batch006 native-input traces.

- **QA-011 / P2 — FIXED, batch 008: Nailong keeps attacking an inactive player.** Native windup fixture reproduced Attack state, active imported graph and one pending glob released after the player GameObject became inactive, both when reacquisition was due and when throttled. Six of 20 assertions failed; destroyed-target controls passed. Clear inactive target/health before the acquisition timer, and stop unfinished presentation when leaving Attack for Idle. The corrected matrix passes 20 checks; a 75-check five-pattern interruption/death/disable/return matrix and six in-flight regressions also pass. Already-launched attacks retain their existing interruption behavior; death still clears them. See Evidence/nailong-target-loss-proof.json.

- **QA-012 / P2 — FIXED, batch 009: Cappuccino resumes an old hit after disable.** Native attack frames reproduced retained Attack state through component disable and whole-object deactivation. Re-enable delivered 7/11/12 damage for TwinSlash/DashThrust/Whirlwind in the short observation requested at 50 ms, with no fresh telegraph; 18/36 assertions failed. Disable now cancels the logical action and queues interrupted-pose recovery for the first valid Tick. Dead state is preserved. Final 52 checks pass, including fresh attack damage, existing ultimate phase/heal contracts and zero inactive Animator warnings. See Evidence/cappuccino-disable-proof.json.

- **QA-013 / P2 — FIXED, batch 010: Bombardino attacks invalid targets.** Deactivating the target or disabling its PlayerHealth left FightActive/Attack set, retained hazards and created a delayed fire zone with acquisition both due/deferred. Validate ownership before processing delayed impacts and include active health in reacquisition. Invalid targets now cause Returning and hazard cleanup before new fire can spawn.
- **QA-014 / P2 — FIXED, batch 010: flame jet ignores disabled Boss owner.** Boss component disable clears hazards but its separate emitter continued at 95 particles/s. Emission now also requires owner.isActiveAndEnabled; native emission settings drop to zero. Combined ownership/death matrix passes 44 checks. No rendered quality claim.
- **QA-015 / P2 — FIXED, batch 010: ordnance discards elapsed time and changes bomb gravity by frame partition.** Tick(1) advanced only .1 s; a 5.1 s ground-fire tick did not expire. One-second bomb trajectory error ranged from 12.657 m (one step) to .208 m (60 steps). Full elapsed time now feeds bounded 1/60 s swept substeps and analytic constant-gravity displacement; four partitions match within .000229 m. Fire keeps real lifetime and at most one damage pulse per game frame, without catch-up bursts. Nine timing and 11 contact/cover/cadence checks pass.

## Confirmed, open

- **QA-003 / P1 — Missing environment prefab dependencies.** Scene-open logs report 234 + 177 + 195 missing prefab instances (606 total), GUIDs `2bde5fdeb204bc6418608a5fe3993ad1`, `8e67ad946c4264b479267ee1643fd2ce`, `ad3d230ef428f6049ab82c5136f00a47`. Exact source asset recovery is required; do not replace or delete instances speculatively.
- **QA-004 / P3 — Compiler warning CS0108.** Story1WreckChaseEntry.camera hides Component.camera. No current compiler error reported.
- **QA-006 / P2 — Editor infrastructure resource failures.** Validation reload logged `ExecutionEngineException: Couldn't create thread. Error 0x5af`, Unity Search LMDB lock warnings and a Version Control package null reference. Editor PID 35488 had ~433,926 handles / 175 threads; ~3.9 GB physical memory available. Cause is not established. Commands briefly timed out; bringing the existing Editor forward restored command execution. No second project Editor was launched, no process killed.

## Suspected / unverified

- Other runtime-created UI wrappers and nonserialized combat caches may fail during in-Play script reload. Audit affected lifecycle paths after QA-001/002.
- One First Island performance snapshot reported CPU frame 106.93 ms, 11,965 draw calls and 230M triangles. This is a single Editor snapshot under heavy load, not a reliable gameplay profile or measured regression. Collect a bounded sample in a stable session before optimization.
- Batch 005 now passes current queued-input normal-flow checks for both weapons: complete combos, recovery dodge/air-flip, moving charge/release, simultaneous jump chop/landing, total control lock and keyboard weapon switching. Boundary timing, camera responsiveness, full boss encounters and rendered animation quality remain unverified; historical V278 visual reports are not current acceptance.
- SandFootprints.SetAlpha constructs a MaterialPropertyBlock for each fading stamp per frame. Source suggests avoidable allocations; no measured GC result yet, so this remains an unverified performance candidate rather than a demonstrated frame-time regression.

## Coverage pending

Enemy/boss encounters, cinematics, saving, full movement/collision coverage, performance profiling and complete prefab/serialization audit.

QA-003 blocker: AssetDatabase returns no path for all three GUIDs, filesystem meta search and Git-history search for the first GUID found no source. `Documentation/TutorialCheckpoint-v253.md` records the same exact 606 references predating this QA. Preserve instances; move to actionable code issues until exact source provenance is recovered.

Batch 002 resource recheck: native Windows process query still reports ~433,940 handles / 170 threads; free physical memory improved to ~8.2 GB. Unity Mono's System.Diagnostics returned zeros for handles/threads, so do not use those zeros as evidence of recovery. Heavy island runtime testing remains deferred; isolated small-scene testing completed. Dispatcher timeouts remain an infrastructure issue, and completed evidence files plus final workspace inspection establish actual outcomes.

Batch 003 native Windows recheck: ~443,310 handles / 174 threads, Editor responding. Resource issue remains open; isolated footprint tests succeeded. Scene restore again exceeded its dispatcher timeout but independently completed, with original scenes clean and recovery marker cleared.

Batch 004 native Windows recheck: ~455,134 handles / 177 threads, responding. Isolated combat tests and scene restoration completed; heavy full-island performance issue is not closed.

Batch 005 native Windows recheck: ~466,030 handles / 176 threads, responding. Normal-flow native input tests completed; resource issue remains open. No new production gameplay defect was demonstrated by this batch.

Batch 006 final native Windows query: ~481,138 handles / 171 threads, responding. File truncation writes to the movement source failed despite a successful ReadWrite open; a guarded extending write succeeded and matched exact desired bytes. No permission/process changes were made. Resource/handle cause remains unproven.

## Batch 007 — combo hit-stop suspicion rejected

Native queued-input review passes 15 checks / 985 observations for stick and twin blades. Early taps expire before queue admission; accepted authored-window taps intentionally survive recovery and repeated local stops (consumed after 823/900 ms). No new production defect is demonstrated, so no combat source changed.

Take batch007-review01 is a rejected test: a requested .35 s single Freeze is clamped to .13 s by CombatLocalHitStop. Its input reached the authored queue window before the .22 s lifetime ended. Treating the resulting chain as stale was incorrect. Corrected take batch007-review02-before renews .06 s stops for .35 s through the real API; both expired-tap and valid-intent cases pass. Its historical before suffix does not imply a subsequent fix. Evidence/combo-hitstop-review.json records actual deadlines and expiry phases.

Native Editor recheck: 489,418 handles / 183 threads, responding. Resource cause remains open. Post-test console contains the known 606 missing-prefab scene restore errors; no new runtime exception appears in the captured interval.

## Batch 008 scope and remaining coverage

An initial phase-graph cancellation suspicion was disproved: NailongAttackMotion.Stop already releases the graph. The untouched baseline passes 75 explicit checks. No graph/clip/controller fix was made. Target loss was then separately reproduced and repaired as QA-011. Tests invoke actual AI state boundaries and public damage APIs, allowing native Animator frames between observations; they are not a full autonomous boss encounter or visual polish acceptance.

Editor PID 35488 final: 497,916 handles / 175 threads, responding. High-handle resource issue remains open. Console since previous cursor contains the known CS0108 warning and missing-prefab restore errors, no new runtime exception. Other Unity-named processes were the CLI/MCP and asset workers, not additional project Editors.

## Batch 009 — regression qualification

The first cancellation repair passed gameplay assertions but introduced ten inactive Animator warnings by crossfading during object disable/destruction. It is superseded: pose recovery now runs after re-enable, and both final suites explicitly assert zero such warnings. The intermediate results/console are retained as rejected acceptance. No animation smoothness claim is made.

Editor PID 35488 final: 510,092 handles / 178 threads, responding. Dispatcher/network failures still occur; foregrounding the existing Editor restored service. Known CS0108 and 606 missing-prefab restore issues remain; no new game exception in the final captured console.

## Batch 010 — wider coverage and measurement qualification

Accepted suites span aircraft hazard ownership (44), ballistics (9), physical hit/cover/fire contracts (11), camera/footprint function (7) and native two-weapon input (28): 99 assertions, including 1,256 native player frames. Performance measurement is unavailable: GC.GetAllocatedBytesForCurrentThread also reports zero for an explicit 1 MB allocation, invalidating the initial zero-allocation reading. SandFootprints remains unchanged; its suspected MPB allocation hotspot is still unmeasured. Qualify future zero-byte results with a positive control.

The first contact fixture incorrectly assumed raw player damage and reused the post-hit recovery clock. Its four failures are test mistakes, not game regressions. Corrected tests honor existing mitigation and explicitly clear the independent player-recovery clock between directly stepped fire cadence samples.

BombardinoBoss.Tick itself still caps delta at .1 s; whether its flight/cooldown/death clocks should preserve full long-frame time is a remaining suspected issue requiring focused movement/terrain evidence. Do not equate the repaired ordnance loop with a complete low-FPS boss encounter. Editor remains responding at 518,647 handles / 174 threads; existing infrastructure issue open.
