# Paired forest timing and counter trust — Batch024

QA-036 repairs diagnostic interpretation. Finite native GPU timings failed a real geometry-to-empty control; the initial collector also discarded independent CPU frames whenever the GPU value waszero. The optional forest experiment now qualifies CPU and GPU separately, retains raw GPU data explicitly and publishes accepted GPU values as-1 when qualification fails. Normal gameplay/render quality remains unchanged.

## Controlled native run

New opt-in -baseline-forest-attribution requires the existing native1080 mode without story/style matrix/orbit. The existing scaled-capsule forest position is retained. Scaled simulation is frozen at one native time; camera position/rotation are fixed. Each control change waits2s plus10 frames before measurement to drain delayed frame timings. Samples require the previous native measured-camera render callback; timestamp identities reject repeated/stale timing records. Finite pixel readback/PNG work occurs outside timing windows.

The sequence is scale1/.5/1/.5/1, an8ms main-thread work control, an empty-camera control and full recovery. Output remains1920×1080; half scale is a diagnostic resolution control only. Saved VSync/frame/pixel policy is temporarily owned by native mode and restored, as is simulation time/camera mask. No quality/density/view-distance/shadow/style preference is changed permanently.

Final Windows64 Mono Development build succeeds in the same Editor57472:41.12s/2,729,620,771bytes,2 warnings/0 errors,7 existing enabled scenes,frame timing enabled only for this diagnostic build. Discovery build succeeded90.87s with493 warnings/4 precisely known scene/missing-prefab messages for the existing606 plants. These incremental build durations are not speedup results.

Final Player exits0/errors0,2,172 measured native intervals,0 missing camera intervals/timing records/duplicates/rejected CPU timings and7 visible/empty image controls. All7 controls change more than2 million output pixels. The inspected full-recovery and half-a PNGs show actual Sahur,terrain,grass and trees at the fixed forest view. They are not whole-combat/UI or temporal-smoothness evidence. Native frozen time21.1976375579834 equals end time exactly. Realistic style,PC quality,LOD bias0.5/shadow distance180m,output1080p,target-1/VSync0;other Editors remain open.

| Phase | Render scale | Frames | Avg FPS | Median main ms | Median Present wait ms |
|---|---:|---:|---:|---:|---:|
| full-a | 1 | 198 | 66.05 | 6.218 | 8.922 |
| half-a | 0.5 | 235 | 78.37 | 6.240 | 6.554 |
| full-b | 1 | 197 | 65.88 | 6.324 | 8.897 |
| half-b | 0.5 | 234 | 78.08 | 6.095 | 6.754 |
| full-c | 1 | 194 | 64.92 | 6.450 | 8.871 |
| cpu-control | 1 | 128 | 64.24 | 14.219 | 1.689 |
| empty-control | 1 | 788 | 394.02 | 2.521 | 0.006 |
| full-recovery | 1 | 198 | 66.02 | 6.202 | 8.890 |

Full repeated controls show64.92–66.05FPS (~15.1–15.4ms), main work~6.2–6.45ms and Present wait~8.87–8.92ms. Half scale78.08–78.37FPS reduces wait to~6.55–6.75ms while main work remains~6.1–6.24ms. Empty control394.02FPS/2.54ms confirms actual rendering-load sensitivity; restoring full returns66.02FPS. These are paired diagnostic effects, not product FPS gains or a proposal to lower quality. Timing-enabled frozen Realistic data cannot establish a regression against batch022's different moving/style matrix.

## Positive controls and limits

The8ms CPU control raises median main time by7.914ms against full-c/recovery. CPU counters qualify independently. Raw GPU medians are0.008192ms full and0.011264ms empty: no material response, and the direction is wrong for this scene's geometry control. GPU qualification fails; every accepted gpuMs is-1. Raw values are retained as rawGpuMs for investigation, not zero-cost/GPU-headroom evidence. Earlier discovery3433 frames also fail GPU qualification (0.012288 vs0.011264ms); replay validates the corrected publisher without accepting those raw values.

A material GPU response requires enough unique records and a full-minus-empty median difference exceeding both0.1ms and10% of full. This is a conservative qualification criterion for this workload, not a universal driver-support test. CPU control needs30 baseline/control records and at least4ms response to the8ms work. A synthetic finite GPU response checks the positive publication path; missing CPU control and invalid timestamps/nonfinite CPU shapes fail closed. Synthetic numbers do not validate the hardware counter.

20 final native checks pass, including camera/timescale ownership/idempotent destruction, cloned-pipeline/frame-policy teardown, independent unavailable GPU handling, actual discovery-data replay, synthetic positive response and missing-control negatives. Earlier15 checks overlap and are not counted again. Both source recompiles pass. A transient Pipeline connection failure during reload recovered; actual successful preparation/state was verified before Play. No duplicate Editor or blind scene entry.

The evidence identifies a resolution-sensitive rendering/presentation path. It does not localize a GPU shader pass, driver stall or CPU render-thread root cause. Present wait is not ordinary C# gameplay work; do not blindly optimize AI,thin foliage,alter graphics buffers or change saved synchronization based on these rows. Next: collect qualified native Player profiler markers to locate the~9ms wait and render submission, with reversible connection/profiler ownership; if unavailable, record the blocker and advance actual combat/UI/tutorial/precision feet.

Unity documents delayed timing availability and cautions that returned timing data may still be inaccurate: [Frame Timing Manager](https://docs.unity.com/en-us/engine/6000.0/manual/analysis/graphics-performance-profiling/profile-rendering/frame-timing-manager/manager). The timing-field and wait semantics are described in [FrameTiming](https://docs.unity.com/en-us/engine/6000.0/script-reference/unityengine/frametiming). Native control qualification above determines what this project's run can support.

## Preservation and checkpoint

Both Players restore all100 native preference values/types,including7 legacy8-byte DWORDs;both fresh saves unchanged. Original5 scenes clean/stopped,timescale1,frame timing settingfalse,preloads/build policy/backend/ProjectSettings bytes restored. Four unrelated Plastic settings/recovery files remain hash-identical;old user checkouts preserved. No original art/model/UV/rig/clip/collider or game design changes. Four styles/pixel mode,duck enlargement/chicken repairs and606 missing plants remain.

Evidence: AUTO_QA/Evidence/batch024/proof.json,20-check verification,build/safety/native-state receipts,2 inspected native PNGs,and2 raw-report gzip archives with exact byte/SHA256 round trips. Discovery archive is explicitly rawGPU-unqualified;final2172-frame report is accepted. Private snapshots/logs/binaries remain .codex/performance024;immutable discovery/final builds and runs must not be overwritten or repeated unchanged.

Checkpoint title: Qualify paired forest timing and isolate unavailable GPU data. Baseline Git401f770/Plasticcs207;resolve actual remotes before sync.
