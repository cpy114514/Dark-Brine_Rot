# Dark Brine_Rot automated QA

## Baseline — 2026-10-28

Unity 6000.3.23f1; one project Editor connected, stopped in the five First Island scenes. HEAD at entry: `dbf282d`. Numerous pre-existing uncommitted changes are listed in `Evidence/initial-worktree.txt`; preserve them and commit only QA-owned changes. No existing AUTO_QA records or applicable AGENTS.md found. Unity reports no current script compilation failure. Unity Test Runner discovers zero EditMode tests; existing regression scripts use explicit static entry points.

## Verified fixes

- **QA-001 / P1 — FIXED.** Real Play-mode domain reload reproduced lost managed pulse/ribbon entries while native children survived. `CombatImpactVisuals` now reconnects those same children, meshes and materials in OnEnable; disabled components ignore external sampling. Native reload regression passes, including both ribbon meshes, landing emission and five re-enable cycles without duplicate renderers. See `Evidence/reload-after.json`.
- **QA-002 / P1 — FIXED.** Nailong's nonserialized BossHudPanel wrapper was lost during reload. OnEnable rebuilds only the missing wrapper and retires its orphan Canvas. Fresh Play, real reload and repeated enable cycles pass. Independent missing-AI fixture failed before and passes after: phase title now tolerates the optional AI. See `Evidence/missing-ai-before.json`, `missing-ai-after.json`, `reload-after.json`.
- **QA-007 / P3 — FIXED, batch 002.** Native SerializedObject validation in isolated Play reproduced six child-creation lifecycle warnings and an immediate terrain rebuild. ProceduralIsland now queues Play-mode validation changes for LateUpdate and coalesces repeated requests. Eleven targeted checks pass with zero lifecycle warnings; terrain/shore vertex positions, collider/render mesh synchronization and single foam child are preserved. See `Evidence/island-validation-before.json` and `island-validation-after.json`.
- **QA-005 / P2 — FIXED in working source, batch 003.** Actual isolated Play-mode domain reload reproduced a null Stamp[] cache while all 28 native stamp objects survived. Adding only System.Serializable to the Stamp struct lets Unity preserve its private hot-reload cache. Thirteen checks pass after actual reload: all object/mesh/material identities, pool cursor, active prints and lifetime retained; original fade alpha/expiry and five enable cycles remain correct. Source was pre-existing/untracked, so Git checkpoints only `Patches/SandFootprints-serialize-stamp.patch` plus provenance/tests; the full original feature remains untracked and preserved. See Patches/README.md and sand-reload-*.json.

## Confirmed, open

- **QA-003 / P1 — Missing environment prefab dependencies.** Scene-open logs report 234 + 177 + 195 missing prefab instances (606 total), GUIDs `2bde5fdeb204bc6418608a5fe3993ad1`, `8e67ad946c4264b479267ee1643fd2ce`, `ad3d230ef428f6049ab82c5136f00a47`. Exact source asset recovery is required; do not replace or delete instances speculatively.
- **QA-004 / P3 — Compiler warning CS0108.** Story1WreckChaseEntry.camera hides Component.camera. No current compiler error reported.
- **QA-006 / P2 — Editor infrastructure resource failures.** Validation reload logged `ExecutionEngineException: Couldn't create thread. Error 0x5af`, Unity Search LMDB lock warnings and a Version Control package null reference. Editor PID 35488 had ~433,926 handles / 175 threads; ~3.9 GB physical memory available. Cause is not established. Commands briefly timed out; bringing the existing Editor forward restored command execution. No second project Editor was launched, no process killed.

## Suspected / unverified

- Other runtime-created UI wrappers and nonserialized combat caches may fail during in-Play script reload. Audit affected lifecycle paths after QA-001/002.
- One First Island performance snapshot reported CPU frame 106.93 ms, 11,965 draw calls and 230M triangles. This is a single Editor snapshot under heavy load, not a reliable gameplay profile or measured regression. Collect a bounded sample in a stable session before optimization.
- Combat transitions, buffering, hit timing and camera responsiveness still require a current native gameplay pass. Prior V278 reports are historical evidence, not this batch's results.
- SandFootprints.SetAlpha constructs a MaterialPropertyBlock for each fading stamp per frame. Source suggests avoidable allocations; no measured GC result yet, so this remains an unverified performance candidate rather than a demonstrated frame-time regression.

## Coverage pending

Enemy/boss encounters, cinematics, saving, full movement/collision coverage, performance profiling and complete prefab/serialization audit.

QA-003 blocker: AssetDatabase returns no path for all three GUIDs, filesystem meta search and Git-history search for the first GUID found no source. `Documentation/TutorialCheckpoint-v253.md` records the same exact 606 references predating this QA. Preserve instances; move to actionable code issues until exact source provenance is recovered.

Batch 002 resource recheck: native Windows process query still reports ~433,940 handles / 170 threads; free physical memory improved to ~8.2 GB. Unity Mono's System.Diagnostics returned zeros for handles/threads, so do not use those zeros as evidence of recovery. Heavy island runtime testing remains deferred; isolated small-scene testing completed. Dispatcher timeouts remain an infrastructure issue, and completed evidence files plus final workspace inspection establish actual outcomes.

Batch 003 native Windows recheck: ~443,310 handles / 174 threads, Editor responding. Resource issue remains open; isolated footprint tests succeeded. Scene restore again exceeded its dispatcher timeout but independently completed, with original scenes clean and recovery marker cleared.
