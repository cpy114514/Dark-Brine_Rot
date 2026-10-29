# Native1080p standalone qualification — Batch022

The previous qualified baseline used saved renderScale0.6 and60fps/vSync1. It could not establish native1080p throughput. The opt-in IslandPerformanceBaseline now supports explicit -baseline-native1080, -baseline-all-styles and -baseline-orbit flags, while normal gameplay remains unchanged.

## Diagnostic changes

Native mode temporarily clones the loaded URP asset, releases pixel presentation, sets renderScale1/targetFps-1/vSync0 and requests1920×1080. Configuration is checked at route start and every measured frame; a mismatch fails closed. Original pipeline/frame policy/pixel/style/intensity are restored on teardown. Styles use the existing manager without preference writes; other PC settings, density/distance, LOD and shadow controls remain as saved. Only the opt-in diagnostic component executes these changes.

The four-mode matrix measures coast/forest/boss-vicinity routes and summarizes all phase names. The camera orbits8°/s only during measurement; finite visible/empty render controls pause orbit at a matching pose. Camera callbacks qualify every accepted interval. Missing boss phases are explicit. A valid ProfilerRecorder handle now also needs a finite1MiB managed-allocation positive control above a5-frame background peak; failed qualification emits-1, not misleading allocation numbers.

## Executed validation

18 native isolated contract checks pass: separate render asset/original preservation, frame policy/pixel override and idempotence, complete teardown, style restoration, positive-control buffer release, actual camera motion/radius/frozen proof pose, visible/empty pixels, unavailable GC reporting, dynamic phase summaries and camera/background cleanup. The Editor counter control qualifies (background47139,positive1060575bytes). This does not validate the separate GetAllocatedBytesForCurrentThread API rejected in batch021.

Built the current7 enabled scenes in the same Editor PID57472: Windows64 Mono Development,158.91s,2729604757bytes. Succeeded with493 warnings and4 precisely known Environment/missing-prefab messages (234+177+195 unavailable plants606 total). No asset deletion. The authored Second Island scene remains outside these7 enabled scenes; no full-project scene-registration/transition acceptance is implied.

The resulting Player exited0, completed12 six-second phases,6963 accepted frames,9110 camera callbacks,0 unrendered intervals and0 recorded runtime errors. All12 real visible/empty controls pass (minimum1577370 differing pixels). Four representative native PNGs were inspected: Realistic coast,Dark Fairytale forest,Origami forest,Monochrome boss vicinity. Protected Sahur, live terrain/vegetation/ocean and the boss appear; overlay UI/full combat is not qualified.

Hardware: Intel Core i5-14400F / NVIDIA GeForce RTX4060 / D3D12. Settings:1920×1080 output/renderScale1,target-1,vSync0,pixel off,PC,LOD bias0.5,shadow distance180m, style intensity0.85. Other Unity Editors remained open; this is a single Development offscreen-camera diagnostic, not isolated GPU/shipping-build throughput. The orbit changes actual view during each measured phase. Sequential boss actions/time differ, so rows are not matched shader-only comparisons or an FPS-gain result.

| Phase | Frames | Average FPS | P95 ms | Max ms |
|---|---:|---:|---:|---:|
| realistic-coast | 727 | 121.22 | 11.28 | 13.36 |
| realistic-forest | 469 | 78.16 | 14.78 | 16.64 |
| realistic-boss | 647 | 107.89 | 12.43 | 15.71 |
| dark-fairytale-coast | 725 | 120.89 | 11.21 | 12.81 |
| dark-fairytale-forest | 463 | 77.25 | 14.93 | 16.22 |
| dark-fairytale-boss | 525 | 87.66 | 14.22 | 15.91 |
| origami-coast | 727 | 121.29 | 11.18 | 14.08 |
| origami-forest | 472 | 78.77 | 14.65 | 16.41 |
| origami-boss | 501 | 83.50 | 14.39 | 15.89 |
| monochrome-coast | 740 | 123.48 | 10.59 | 13.23 |
| monochrome-forest | 479 | 79.95 | 14.48 | 16.28 |
| monochrome-boss | 488 | 81.38 | 14.75 | 18.99 |

No sampled interval exceeds50ms. Forest77.25–79.95FPS is consistently slower than coast120.89–123.48FPS. Identify CPU/GPU attribution before changing game quality or claiming a bottleneck cause. Later boss phases vary81.38–107.89FPS with active encounters; do not attribute that solely to style shaders.

The standalone GC control did not qualify: background peak2282743bytes exceeds positive-window1468294bytes. All GC samples/maxima remain-1. This is a conservative noisy-warmup rejection, not proof the recorder is universally unsupported. Requalify under stable paired conditions before any allocation claim.

Reported island loading is82.70s, excluding executable/menu initialization; it is not an executable-to-interactive time or a proven regression against batch019's different run. Native D3D12 startup log reports upload/ring-buffer resizing. Loading-stage attribution/hidden-window behavior/upload costs are suspected follow-ups; no cause or startup speedup established.

## Safety and continuation

Both fresh saves unchanged; all100 native preferences/types restored, including7 legacy8-byte REG_DWORD entries. Corrected raw restorer reused; launcher retains its process handle and exits cleanly. Private snapshots/logs/binary/caches stay ignored. Four unrelated Plastic settings/recovery paths match pre-build hashes. Original5 scenes clean/stopped, build flags/backend/preloads restored, ProjectSettings bytes exactly restored. The sole incidental material _MainTex serialization was rolled back, and its unchanged checkout cleared with exact-path filtering. No original art or gameplay changed.

Next: attribute the forest CPU/GPU cost and82.70s loading stages using qualified controls; optimize only the evidenced workload while preserving density/art/design. Then broaden actual combat/UI/tutorial and fine foot contact. Do not repeat this unchanged12-phase matrix or infer low-end/temporal-flicker/whole-game gains.

Evidence: AUTO_QA/Evidence/batch022/{proof,standalone-report,verification,build-report,player-safety,final-native-state}.json and4 native PNGs. Checkpoint title: Qualify native1080p style matrix and benchmark counter reporting.
