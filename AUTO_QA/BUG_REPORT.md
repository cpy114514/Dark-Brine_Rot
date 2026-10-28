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
