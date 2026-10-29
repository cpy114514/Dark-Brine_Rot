# Native Player CPU profile — Batch025

The forest wait is now localized to main-thread DXGI.WaitOnSwapChain,not an unidentified C# script block. In the complete full-recovery control it averages7.550ms of a14.598ms main-thread frame. The render thread spends10.158ms waiting for main-thread commands and4.434ms in RenderLoop,including2.421ms at PutGeometryJobFence. This identifies where CPU time waits; it does not establish a GPU/shader/driver/queue root cause or justify changing synchronization/buffers.

## Capture and validation

The existing optional forest experiment gains -baseline-forest-profiler. It starts a binary CPU log only after world readiness and emits ForestPhase.<name> markers in measured frames. The8ms CPU-control spin is inside its phase marker. Paired configuration,real camera fences,pixel controls and independent unavailable GPU reporting remain. Native profiler enabled/binary/log state is restored on completion or component teardown. Normal gameplay never installs the optional sampler or enables this flag.

Same existing Editor57472 builds the7 enabled scenes as Windows64 Mono Development with timing enabled only for this diagnostic:44.37s/2,729,622,211bytes,2 warnings/0 errors.21 native checks pass,including the added unflagged-profiler ownership test and the prior independent-counter/teardown/replay controls. These21 overlap prior batches and are not an additional41-check total. The Player exits0,2,101 fenced samples,0 missing render intervals,0 recorded runtime errors,7 actual image controls. binaryProfileCaptured and binaryProfileRestored aretrue. No new animation/visual-smoothness or whole-combat/UI acceptance.

The completed native binary profile is711,404,418bytes and remains private. At import the existing Editor history exposes only2,000 frames,index1878–3877. No history/buffer setting was changed to expand it. Early full/half phases are unavailable and full-c retains only22 measured frames; that partial phase is excluded from headline conclusions. The complete retained CPU/empty/full-recovery controls have131/681/206 labelled frames versus130/680/205 fenced sampler intervals. The one-frame difference is the intentionally unsampled first interval after resetting the frame clock. There are1,018 complete labelled control frames;the partial22 give1,040 tagged frames in total.

| Native profile observation | Full recovery | CPU8ms control | Empty control |
|---|---:|---:|---:|
| Labelled frames | 206 | 131 | 681 |
| Main-thread frame ms | 14.598 | 15.307 | 2.938 |
| Main DXGI.WaitOnSwapChain ms | 7.550 | 1.014 | not a top cost |
| ForestPhase.cpu-control ms | n/a | 8.003 | n/a |
| Render waits for main commands ms | 10.158 | 11.319 | 2.082 |
| RenderLoop ms | 4.434 | 3.989 | 0.854 |

The explicit native8ms marker validates phase correlation and CPU profile units. Inserting work consumes much of the prior swap-chain wait;empty geometry removes most of the frame cost. Thread durations overlap and must not be added together. Profiling/binary I/O changes timing,so current frame rates are not an optimization gain/regression against024 or022.

Other observed full-recovery CPU work:bounded WorldPerformanceManager.RefreshFoliageWork1.513ms,GrassVisibility self0.798ms andGrassSubmit self0.385ms. The refresh remains near its existing1.5ms target;this is not by itself a defect or permission to thin vegetation. No speculative extra cache,reduced quality/vegetation or graphics-buffer setting was applied. Native GPU qualification still fails and accepted gpuMs stays-1.

## Original profiler preservation

Before any test/import,the existing disabled Editor capture0–676 was saved privately as a754,531,172byte native profile. Capture flags,connection-1,selected path and four complete main-thread frame fingerprints were snapshotted. Analysis temporarily imports the owned Player file,then restores the original range0–676,all four name/time fingerprints,selection andenabled/GPU/editor/deep/connection settings. Native readback confirms the settings and range. This is range/fingerprint verification,not full binary byte identity of a reserialized profile.

The first offline reader encountered an unnamed native sample and failed;its finally block still restored the original profile and fingerprints. The reader now retains unnamed samples in an explicit bucket and guards missing names during phase search. Reanalysis uses the same immutable Player file;no repeated Player capture/build was needed. Native whole-thread unnamed roots are not discarded or mistaken for a gameplay exception.

Both fresh saves remain unchanged and100 native preference values/types restore,including7 legacy8byte DWORD values. Original5 scenes clean/stopped,timescale1,frame timing settingfalse,build flags/backend/preloads/ProjectSettings bytes restored. Four unrelated Plastic settings/recovery files remain hash-identical. No original art/model/UV/rig/clip/collider/design edits;four styles/pixel,chicken/duck fixes and606 missing plants preserved.

## Continuation

The presentation wait is identified,but its lower-level GPU/driver cause remains unqualified. Do not repeat unchanged scale/profile/counter matrices or blindly adjust queues/synchronization. Next prioritize actual Sahur combat/UI/Boss/tutorial and precision foot contact to broaden QA. Return to GPU/presentation profiling when a qualified pass-level tool/control can add new evidence;preserve current captures/settings and avoid another project Editor.

Evidence: AUTO_QA/Evidence/batch025/{proof,profile-analysis,verification,build-report,player-safety,final-native-state,profiler-restoration-proof}.json plus a byte/SHA256-verified lossless report archive. The two native binary captures,raw logs,snapshots andPlayer build remain .codex/performance025 and must not be overwritten/rebuilt/relaunched unchanged. Profiler binary logging follows [Unity's API](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/Profiling.Profiler-enableBinaryLog.html);only this project's native capture/phase controls support the measured values above.

Checkpoint title: Capture phase-labelled native Player CPU profiles. Baseline Git4ef510a/Plasticcs208;verify actual remotes before sync.
