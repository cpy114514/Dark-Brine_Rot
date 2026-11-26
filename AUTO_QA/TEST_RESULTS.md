# Test results

Batches 001–034 cover Unity Editor and standalone Player checks. The batch log includes Codex-assisted automation. See the issue and coverage notes before using a historical result to assess a newer build. [Bug reports](BUG_REPORT.md) · [Animation reviews](ANIMATION_REVIEW.md) · [Coverage notes](NEXT_TASK.md)

| Batches | What was checked | Evidence |
| --- | --- | --- |
| 001–010 | Scene reloads, native input, weapon combos and cancellation. | [Native input results](Evidence/native-input/README.md) · [Fixture repair](Patches/README.md) |
| 011–017 | UI and pause recovery, death during actions, aircraft, checkpoints, camera obstruction and foliage ownership. | [Regression fixtures](Tools/) |
| 018–025 | Foliage CPU cost, flock behavior, island startup and standalone Player baselines. | [Startup measurements](../Documentation/Performance-Batch023.md) · [Player CPU profile](../Documentation/Performance-Batch025.md) |
| 026–032 | Duck behavior, grass stability and draw distance, UI and visual styles. | [Grass layout](../Documentation/Grass-Consistency-Batch028.md) · [Visual styles](../Documentation/VisualStyleSystem-V2.md) · [Grass distance](../Documentation/Grass-Distance-Batch032.md) |
| 033–034 | Version-control integration, scene checks and goose gait. | [Integration](../Documentation/Version-Sync-Batch033.md) · [Goose review](../Documentation/Goose-Polish-Batch034.md) |

## Recorded verification

These results apply to the specific build or test run listed below.

| Check | Status | Result and scope |
| --- | --- | --- |
| Batch 005, corrected input run | Verified | 28 checks passed with scripted keyboard and mouse input. The original input settings were restored. The first take remains rejected because its settings restore failed. [Second take](Evidence/native-input/batch005-take02/result.json) · [Restore check](Evidence/native-input/independent-input-restore.json) |
| Batch 033, Windows integration build | Verified | The eight-scene Windows x64 Development build succeeded. The executable was not launched in this check. [Build evidence](Evidence/batch033/proof.json) |
| Batch 034, BullyGoose checks | Verified | 70 checks of assets, the animation controller and enemy behavior passed with zero failures. Live combat and precise foot contact remained open. [Check results](Evidence/batch034/validation.json) |

The Markdown archives include exact timings, failed takes and restoration checks. Per-batch JSON and captures are under Evidence. [Detailed results](https://github.com/cpy114514/Dark-Brine_Rot/blob/b7a10f2327e3622828e3228ec9f8a21292a54ebb/AUTO_QA/TEST_RESULTS.md) · [Original batch log](https://github.com/cpy114514/Dark-Brine_Rot/blob/adbe8cc794659768260f8ff7b007003fb6de6264/AUTO_QA/PROGRESS.md) · [Evidence files](Evidence/)

## Finding a test

Use each report for its fixture and setup. Scripts outside `Assets` are run on demand. Pre-cleanup snapshots hold the retired local-session scripts and the original paths for byte-identical QA copies. [Current diagnostics](../Tools/Diagnostics/) · [Historical diagnostics](https://github.com/cpy114514/Dark-Brine_Rot/tree/2d2a862bba490081ab4e8607dba4b35bb73bce8b/Tools) · [Historical QA evidence](https://github.com/cpy114514/Dark-Brine_Rot/tree/2d2a862bba490081ab4e8607dba4b35bb73bce8b/AUTO_QA/Evidence) · [Older benchmark launchers](https://github.com/cpy114514/Dark-Brine_Rot/tree/b7a10f2327e3622828e3228ec9f8a21292a54ebb/Tools)

The standalone performance scripts share one Windows Player launcher that restores the original Unity preferences and checks save-file hashes. The individual batch scripts still set their own build, flags and report directory. [Shared launcher](../Tools/PerformancePlayer.ps1) · [Baseline procedure](../Documentation/Performance-Batch019.md)
