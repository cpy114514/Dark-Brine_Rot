# Frame-paced island initialization — Batch023

QA-035 is verified and repaired. Two fresh foliage scans used a fixed2ms target per native frame. With the observed slow VSync1 startup pacing (~8 frames/s), wind-profile preparation and the71,315-renderer ownership cache stretched island readiness to roughly80s. The scene/player itself was ready around4–6s. The small correction increases only fresh-acquisition slices to1/16 of the previous unscaled frame duration, clamped2–8ms. Interactive recaches, stopped-Editor work, visibility refresh and rendering policy retain their previous budgets.

## Attribution and paired result

Same existing Editor PID57472 produced two Windows64 Mono Development builds with the same7 enabled scenes. Baseline trace build succeeded135.06s/2,729,608,073bytes (493 warnings and4 known missing-plant messages); adaptive build succeeded66.62s/2,729,609,431bytes (2 warnings,0 errors). Incremental build duration is not an optimization result. Existing606 missing plants remain.

| Run | Control | Island readiness s | World-ready frame | Main-camera callbacks | Foliage roster |
|---|---|---:|---:|---:|---:|
| a-base | original2ms,VSync1 | 80.45 | 600 | 582 | 71315 |
| b-early | original2ms,VSync1,early RT | 81.56 | 611 | 594 | 71315 |
| c-nosync | original2ms,temporary VSync0 | 23.03 | 575 | 528 | 71315 |
| d-base | original2ms,VSync1 again | 79.09 | 599 | 583 | 71315 |
| e-adaptive | new binary,adaptive,VSync1 | 33.43 | 245 | 228 | 71315 |
| f-fixed | same new binary,old2ms,VSync1 | 80.02 | 605 | 588 | 71315 |
| g-adaptive2 | same new binary,adaptive,VSync1 | 32.60 | 242 | 224 | 71315 |

The same-binary adaptive/fixed/adaptive sequence reduces measured readiness58.23–59.26%. All three launch preference snapshots are byte-identical; target60/VSync1 and focused=true are recorded. Player-ready times5.05/4.37/4.58s remain similar; first observed wind-ready22.89/51.43/22.41s and final cache readiness33.95/80.54/33.12s identify where the wait changes (stage timestamps include the preceding~0.52s diagnostic initialization). There are0 recorded runtime errors; all7 Players exit0 and retain the71,315 roster.

Early render-target binding alone did not improve readiness. Main-camera callbacks continued during all loads, and native focus wastrue despite the launcher's Hidden request. Thus no hidden/occluded-window cause is established. Temporary VSync0 is a diagnostic control only, fully restored; the production fix does not change synchronization, frame cap, renderScale, style, pixel mode, vegetation density, view distance, shadows or gameplay. The D3D12 upload/ring-buffer warnings remain observations, not the identified root cause.

## Implementation and regression

WorldPerformanceManager.InitializationBudget applies the fresh-scene policy only when no previous renderer ownership exists. FoliageWindDriver uses it only before its first completed profile acquisition; subsequent profile rebuilds remain2ms. Yield checks still occur every32 candidates, so2–8ms is a work-slice target, not a hard whole-frame maximum. Existing cancellation, original-renderer restoration, LOD selection, bounds and wind math remain unchanged.

The opt-in sampler adds load-trace/load-only/early-render/fixed-budget controls. It records actual scene/player/wind/cache readiness, native frames/camera callbacks and saved frame policy. Load-only exits before benchmark quality overrides. Startup RT reuse/resize preserves the original external target. The fixed-budget flag provides the old policy in the same executable and restores its owned control on teardown; normal play does not enable diagnostics.

76 accepted native checks:13 trace/render-target contracts,20 budget-policy checks,9 authored-cache checks,11 actual domain-reload ownership checks,12 lifecycle checks,6 additive scene-load checks and5 true partial-cache cancellation checks. A separate stopped-Editor policy readback passes. An actual script reload unloaded the ephemeral preparation assembly; original hidden/material/mesh/shadow/occlusion state and camera-mask gating survived. Cancellation occurred after383 cached entries, then replacement636 retained originals. No general screenshot pixel-identity acceptance is claimed for the ownership fixture.

Native fixed/adaptive wind runs compare6,420 scalar property values exactly across641 authored/owned renderers, including bark/branch/leaves/grass/mixed material slots. All match. An initial diagnostic-helper cleanup failed to retire five temporary cases/restore its disabled manager; the helper was corrected, its owned fixture safely resumed and the authored cleanup reports0 fixtures. This was a fixture issue, not a production regression. No animation/visual smoothness claim is based on these numeric checks.

## Scope and safe continuation

This is measured diagnostic island readiness, excluding executable/menu startup. It is not a shipping/low-end guarantee, total executable-to-interactive measurement or steadyFPS/GPU/GC improvement. Tests ran on the existing i5-14400F/RTX4060 machine with other Editors open. The same-binary A/B and repeat constrain warm-cache/build differences; they do not explain the underlying slow VSync frame pacing. No unchanged full style/FPS matrix was repeated. Forest CPU/GPU attribution, actual Sahur combat/UI/tutorial and fine chicken foot contact remain next work.

All100 native preferences/types restore after every Player, including7 legacy8-byte DWORD values; both fresh saves remain byte-identical. Original5 scenes clean/stopped, build flags/backend/preloads/ProjectSettings bytes restored. Four unrelated Plastic settings/recovery files remain hash-identical and existing unrelated checkouts are excluded. No private registry snapshots/logs/binaries/cache enter version control. Native evidence is in AUTO_QA/Evidence/batch023 plus the batch023-cache*/batch023-ownership* regression receipts; private snapshots and immutable builds/runs remain .codex/performance023. Do not overwrite or rerun accepted cases unchanged.

Checkpoint title: Reduce frame-paced island initialization wait. Baseline Gitdb92b8e/Plasticcs206; inspect actual remotes before syncing.

The authored fixture camera PNGs were inspected and show identical blank backgrounds; they cannot qualify visible geometry or visual regression. Both are archived under .codex/performance023/unqualified-capture and excluded from committed evidence. Numeric/native renderer-state checks remain accepted independently; no screenshot identity/smoothness conclusion is drawn.
