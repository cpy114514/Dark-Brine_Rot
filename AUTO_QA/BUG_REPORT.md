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

## 2026-10-28 — Batch 011 — performance and UI lifecycle

- QA-016 VERIFIED FIX: SandFootprints.SetAlpha allocated a new MaterialPropertyBlock per fading stamp. Native raw Profiler GC.Alloc metadata measured 24,000 bytes per 1,000 warmed calls before, zero after reusing an instance block. Both 16-frame runs detect the explicit 1 MB positive control. Existing GetPropertyBlock preserves independent renderer properties.
- QA-017 VERIFIED FIX: actual Play-mode script reload left SahurLoadoutUI without its managed UI caches/singleton. SetOpen no longer opened the equipment panel, and its old native canvas remained orphaned. Serialize canvas ownership and rebuild managed bindings on enable after reload.
- QA-018 VERIFIED FIX: IslandMapUI lost ownership of its native canvas across reload and built another. Serialize root/font ownership; rebuild lazily if graphic cache is missing, retaining valid owned canvases. Camera/time restoration and disable/reenable remain valid.
- Existing 606 missing plant prefab references remain open. No source/art replacement or animation change.

Batch011 fixes accepted and synchronized: Git 3a4ca40, GitHub main via PR #1/c6d8146, Plastic cs192. No visual animation acceptance added.

## 2026-10-28 — Batch 012 — pause/settings ownership

- QA-019 VERIFIED FIX: disabling the active PauseSettingsMenu left IsOpen, timeScale=0 and its raycast overlay active. OnDisable now resumes only its own pause. Disabled public Open is rejected.
- QA-020 VERIFIED FIX: destroying an unrelated menu or calling its Resume released another menu's global pause. Track the active instance and gate release/destruction by ownership.
- QA-021 VERIFIED FIX: actual Play-mode script reload left the settings menu paused with no active static state; reopening threw ArgumentOutOfRangeException from the lost resolution cache, and Resume's runtime button listener was gone. Retain serializable Settings, rebuild resolution/control/original-light caches and bind listeners once per managed lifetime.
- QA-022 VERIFIED FIX: with two enabled menus, one queued Esc closed the owner and another menu reopened pause in the same native frame. Only the owner handles open-menu input; consume Esc once per frame. Native before probe fails exactly this check; final probe passes.
- No gameplay tuning, art, scene or animation asset changed. Known 606 missing prefab references remain open.

## 2026-10-28 — Batch 013 — death reload and aircraft timing

- QA-023 VERIFIED FIX: while the player is dead, a real script reload resets static death ownership. The death panel remains visible, but global IsOpen/BlocksInput no longer represent it. OnEnable now restores the active dead owner and health-event binding.
- QA-024 VERIFIED FIX: readonly disabled-behaviour list disappears across reload. Respawn returned successfully while attack, boomerang, hit reaction and movement remained disabled. Serialize this exact ownership list; originally disabled flash stays disabled.
- QA-025 VERIFIED FIX: the death panel's runtime Respawn listener disappears across reload. Rebind its named handler on enable with remove/add, retaining the original canvas. The actual native button and a second death/respawn after re-enable pass.
- QA-026 VERIFIED FIX: BombardinoBoss.Tick caps every call at .1 s. A 1.25 s input consumes .1 s, misses three missile pairs and delays return/flight/death timing. Consume elapsed time in <=1/60 s steps and synchronize native collider transforms between catch-up steps. Original attack numbers, warnings and ground-clearance algorithm remain.
- LocalizedGameText reload suspicion rejected: original legacy/TMP alias updates and unknown dynamic numbers already pass after actual reload. Source unchanged.
- Non-authored configuration stress: patrol with cruiseAltitude=4 and 2x visual scale intersects an 8 m ledge in the initial probe. This is not the authored SpeedRun/default cruiseAltitude=55 acceptance. A footprint-sweep candidate did not repair that stress case and was fully reverted; no ground sampling change ships.

## 2026-10-28 — Batch 014 — warning work and checkpoint range

- QA-027 VERIFIED PERFORMANCE FIX: the existing 10 Hz warning throttle can still rebuild geometry four times inside one 333 ms catch-up call. Native Profiler markers confirm 60 builds over 15 old calls versus 15 coalesced builds. Capture the scheduled warning pose and build once after simulation; all 24 native warning geometry/state/cancellation snapshots are unchanged. This is a geometry-work reduction, not demonstrated whole-Tick/FPS improvement.
- QA-028 VERIFIED FIX: checkpoint range used Sahur's imported model origin, ~3.415 m above capsule feet. A radius-3 checkpoint did not activate even when the actor stood directly at it. Compare the capsule foot position instead; retain full 3D radius and dead-player guard. Native before 3/5, after 5/5.
- Active-action death review found no new production bug. Six native charge/throw/rush/combo cases cancel and resume correctly after respecting a valid checkpoint and the authored .42 s landing recovery. Initial failed takes are rejected fixture assumptions, not game defects.

## 2026-10-28 — Batch 015 — intermediate camera obstruction

- QA-029 VERIFIED FIX: desired follow-camera sphere cast can be clear while the smoothed intermediate position lies inside a side obstacle. Native original probe fails 1/5: camera surface distance is zero. Sweep the actual ordinary follow ray from focus using the existing actor-filtered collision helper; cinematic handoff retains its previous-position travel origin. Final four-heading matrix passes 20/20, with blocked camera centers .190–.197 m from the obstacle surface. Trigger/disabled-collision/free-release behavior stays unchanged.
- Vitals HUD review passes 19 checks without production UI edits. HP/SP full, half, 1% and empty fills, integer HP labels, raycast exclusion, death/disabled movement hiding and alive recovery work. These are native state/layout-parameter checks, not visual acceptance.
- No new performance optimization is claimed; ordinary camera correction adds a bounded nonallocating-buffer cast, with existing dense-query fallback. Whole-scene profiling remains next.

## 2026-10-28 — Batch 016 — confirmed foliage reload defect (OPEN)

- QA-030 VERIFIED OPEN (resolved in batch017): actual Play-mode script reload loses WorldPerformanceManager managed LOD/render maps and draw caches. Original-source authored-material fixture: 30 LOD entries, 4 compact layouts, 36 classified renderers and 4 submitted batches before; all zero after. Native preparation assembly is confirmed unloaded. This is Editor reload evidence, not a shipping Player FPS claim.
- Initializing caches on enable recovers map cardinality but fails original renderer/material ownership regressions. Before-reload cleanup hook, serialized-baseline and inline-iterator candidates also failed; every production edit was reverted byte-for-byte. Do not ship these backups. Partial-scan cancellation is a suspected cause, not an established fix.
- RenderTexture PNGs were not qualified by a reliable rendered-frame fence/positive control: original authored-material captures are black. No visual disappearance/smoothness claim. File names containing accepted/final/fixed refer to historical rejected takes, not acceptance.
- Broader terrain coverage passes 12 native checks: grounded capsule, checkpoint activation and separate-floor exclusion on 0/15/30/40-degree platforms. No new checkpoint defect or source change.

## 2026-10-28 — Batch 017 — QA-030 repaired

QA-030 VERIFIED FIX: WorldPerformanceManager now recreates managed maps/draw scratch/submission binding on enable, rebinds the scene event once, and carries original renderer identities through actual Editor reload using SessionState. A bounded recache restores original mesh/material/visibility/shadow/occlusion immediately before owning each renderer. Previous-cache originals also remain available through ordinary additive recaches. Original source lost 30/4 maps and all submissions; final native 636-renderer tests retain 30/4 maps, original identities and 6 submitted batches after repeated reload and scene loading.

A bulk restore passed 36 objects but failed expanded original-state checks. A reload-only per-object version then failed after an additive recache. Both are rejected; only the final per-renderer handoff for reload and ordinary recache ships. Partial-scan suspicion is resolved without changing iterator cancellation: corrected control observes 351/636 entries and passes all five interruption/restore checks. Batch016 partial=36 had been the old snapshot, not demonstrated scan progress. No authored asset, clip, shader, combat rule or visual/FPS acceptance added.

## 2026-10-29 — Visual Style System V2

Completed the requested two additional rendering modes in the existing system. Origami simulates matte faceted paper surfaces/folds and stylized water/clouds; Monochrome transforms the complete world after transparents while preserving overlay UI. No gameplay or authored geometry/animation edits. Four-card gallery, four saved detail controls per new style and all sixteen comparison pairs are integrated.

Integration corrections verified: runtime manager/UI bindings recover after actual script reload without duplicate roots; dedicated UI cameras bypass world transformation; material uniform updates are cached and refreshed by parameter/quality/pixel revision. Early camera-space UI captures were rejected; composited native/pixel overlays provide actual UI evidence. No claim that old V1 animation snapping or the separate missing-plant issue was repaired. Ocean/Sky retain three compiler warnings in unchanged helper functions; no shader errors.

Remaining qualification: standalone/low-end outdoor cost and long moving-camera temporal stability. Existing 606 unavailable plant prefab instances preserved. See Documentation/VisualStyleSystem-V2.md and Evidence/visual-style-v2.

## 2026-10-28 (Vancouver) — Batch018 — QA-031 foliage CPU overhead

QA-031 VERIFIED PERFORMANCE: native first-island traces identify World.GrassVisibility around1.40ms, including submission. Each bounds/plane query redundantly recomputed absolute normals and every wind-property upload repeated string lookup. Cache six absolute normals once per camera and four shader property IDs. Exact native-camera/custom-matrix classification and array-binding regression passes192168 final cases/assertions. No visibility threshold, density, distance, padding, batching ownership or source asset change. Recorded native visibility path1.400→1.100ms; pixel/gallery1.417→1.108ms. Separate short Editor windows are observations, not a standalone/FPS or isolated GPU claim. Paired kernels independently reduce the tested inner-loop medians44.4%/61.5%. See Documentation/Performance-Batch018.md.

## 2026-10-28 (Vancouver) — Batch019 — QA-032 benchmark validity

QA-032 VERIFIED DIAGNOSTIC FIX: opt-in IslandPerformanceBaseline used an ordinary search that omits the installed DontSave WorldPerformanceManager, skipping initialization readiness; it also synchronized GPU ReadPixels in every measured frame. Resolve loaded runtime objects explicitly, wait/fail closed, gate samples on completed measured-camera/target callbacks, and perform finite visible/empty image controls outside measurement. Restore prior camera targets/enabled/background state and label unavailable GC counters.22 native instrumentation checks pass. No normal gameplay/art or new FPS-gain claim.

A current seven-scene Mono Development Player completed1798 measured frames across coast/forest/boss vicinity,0 runtime errors and0 unrendered intervals, with real image controls. At saved PC settings (1920×1080 output,0.6 renderScale,60fps cap,vSync1), average59.80–60.01FPS. Full-resolution/uncapped/headroom and all-style/long-combat qualification remain. Existing606 missing plants retained; build succeeds with4 known scene/prefab messages and493 warnings. Launcher initially failed restoring7 legacy8-byte REG_DWORD floats; recovered exact100 native preference values/types from private snapshot through native API, saves unchanged, corrected helper retained. Details: Documentation/Performance-Batch019.md.


## 2026-10-29 — Batch020 — QA-033 chicken jitter and hovering

VERIFIED/FIXED:0.8m return-home threshold conflicted with4.5m patrol goals, causing158 direction reversals/240 intervals. Use6m outer/0.8m inner return hysteresis, separate patrol/repath timers and reset pooled state. Melee cooldown now integrates gravity and uses Idle instead of stationary running. Chicken visual offset lowered0.65m; final reversals2 and mean sole gap0.70665→0.07666m. Native captures inspected.

User-requested duck enlargement:40% uniform visual/capsule growth, size-consistent reach/separation and compensated locomotion playback; damage/travel speed/windows preserved.27 native regression checks pass. The candidate tall-melee hit-volume change was rejected after correcting the fixture's scaled-player-sole placement; original Strike is byte-identical. Remaining measured limitation: occasional walk sole intersection about0.11m; full island crowd/precision foot planting pending. Details: Documentation/Enemy-Batch020.md.


## 2026-10-29 — Batch021 — actual flock and diagnostic limitation

No new gameplay defect confirmed. Actual first-island5 chicken+3 enlarged duck group passes360 sampled native frames across4 styles: max2 attackers,0 slot violations,0 missing ground,0 motor-sole penetrations deeper than0.35m and0 fixture-run exceptions. Ground clearance0.0508461–0.09234619m.

Suspected per-access path-corner array allocation rejected: native getters return the same cached array and replanning invalidates it. No extra production cache added. Confirmed diagnostic limitation: GC.GetAllocatedBytesForCurrentThread returns0 even for native fresh arrays and256 managed byte-array allocations. All raw zero allocation readings are UNQUALIFIED; accepted proof uses null. No zero-allocation/FPS improvement claim. Batch020 precision visual foot contact remains open. Documentation/Flock-Batch021.md.


## 2026-10-29 — Batch022 — QA-034 diagnostic counter/configuration trust

VERIFIED DIAGNOSTIC FIX: sampler availability previously meant only ProfilerRecorder.Valid. Add a finite positive control; unqualified data becomes-1.18 new native contracts pass, including reversible native1080/frame/pixel/style overrides and orbit/proof isolation. One production script, opt-in only; no normal gameplay/art change.

Qualified standalone12-phase/6963-frame result: native1920×1080,renderScale1,target-1,vSync0;0 measured frames>50ms,0 unrendered intervals/runtime errors. Forest77.25–79.95FPS versus coast120.89–123.48FPS is an observation requiring CPU/GPU attribution. Standalone allocation control rejected noisy warmup; no GC/optimization-gain claim. Suspected:82.70s island load and native D3D12 upload/ring-buffer warnings need stage attribution; no verified root cause. Four known missing-plant build messages/606 instances retained. See Documentation/Performance-Batch022.md.


## 2026-10-29 — Batch023 — QA-035 frame-paced startup work

VERIFIED/FIXED: fresh wind/profile and world ownership scans used2ms per frame, stretching readiness to~80s at observed~8FPS startup pacing. Adapt only fresh acquisition to1/16 previous frame time,2–8ms target; interactive recache/Editor stays2ms. Same-binary adaptive/fixed/adaptive gives33.43/80.02/32.60s with identical saved preferences,target60/VSync1 and71,315-renderer roster:58.23–59.26% less diagnostic readiness wait. All7 controlled loads exit0/errors0; early RT alone does not help, so no hidden-window or D3D12 upload-cause claim.76 native checks plus6,420 exact wind property comparisons pass. No steadyFPS/GC/quality reduction claim. Documentation/Performance-Batch023.md.
