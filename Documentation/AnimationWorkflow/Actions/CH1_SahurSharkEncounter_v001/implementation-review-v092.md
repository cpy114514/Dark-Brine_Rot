# v091–v092 build-cache repair and Player measurements

The v090 standalone build reported a `MissingReferenceException` in `URPPreprocessBuild.LogIncludedAssets`. The persistent PC render pipeline was valid. The Editor’s `CoreBuildData` singleton could retain a temporary render pipeline after Unity destroyed it on leaving Play mode.

The v092 Editor preprocessor, `RefreshRenderPipelineBuildCache`, disposes that cached data before the Core and URP build processors collect the active assets. The regression deliberately added a temporary pipeline to the cache, destroyed the clone, verified the original QualitySettings pipeline, then built the Player. The build finished in 23.888 s with zero errors and one warning.

## Standalone comparison

Both seed-14 captures rendered at 1920 × 1080 with DX12. The moving wreckage produced different layouts, so the difference between timings cannot be attributed to a single change.

| Measurement | v090 Player | v092 Player |
| --- | --- | --- |
| Encounter duration | 20.816 s | 20.802 s |
| Entry planning | 7.177 s | 2.939 s |
| Six strike gaps, in order (m) | 3.333, 2.134, 0, 0, 1.939, 0 | 0.038334, 0, 0.030808, 0, 0, 0 |
| Mean / longest film frame | 17.118 / 76.217 ms | 18.210 / 222.788 ms |
| 1% low | 23.583 FPS | 9.289 FPS |
| Longest attack / solid-collision step | 55.792 / 30.530 ms | 43.326 / 195.514 ms |

v091 limited expensive swept-anatomy route tests to four candidates and returned after finding a clear, reachable route. Endpoint sampling still ran across candidates. The exact geometry checks and 5 cm strike threshold remained in force.

The v092 take completed 2,237 GPU readbacks with zero errors. All six strikes met the threshold, but the frame-rate target failed. The largest stalls at 4.220/4.392 s and 10.920/11.029 s were in the solid-collision solver. The profile is `standalone-profile-v092-seed14.json`. The next profiling targets identified at the time were the overlap, retained placement, escape and final penetration checks.

Both takes stayed on the same board. The separate v090 Editor take also passed all six contacts. The tests left neighboring-board hops, gameplay-camera occlusion and full-speed visual approval open. [v090 animation review](implementation-review-v090.md)
