# Automated QA batch record

Batches 001–034 cover targeted Unity Editor and standalone Player checks. The batch log includes Codex-assisted automation. The original pass/fail results, rejected runs, and limits on human or hardware verification are recorded in [test results](TEST_RESULTS.md), the [bug report](BUG_REPORT.md) and the [animation review](ANIMATION_REVIEW.md).

| Batches | What was tested | Evidence |
| --- | --- | --- |
| 001–010 | Scene and reload baselines, queued native input, combo windows and cancellation. The first native-input take failed to restore its settings object. | [Input results and rejected take](Evidence/native-input/README.md) · [Footprint repair provenance](Patches/README.md) |
| 011–017 | UI and pause restoration, death during active actions, elapsed-time aircraft updates, checkpoints, camera obstruction and foliage ownership across reloads. | [Regression fixtures](Tools/) · [Test results](TEST_RESULTS.md) |
| 018–025 | Foliage CPU cost, standalone baselines, flock behavior, island startup and forest profiling. Editor observations and Player timings have separate test conditions. | [Startup measurements](../Documentation/Performance-Batch023.md) · [Player CPU profile](../Documentation/Performance-Batch025.md) |
| 026–032 | Duck and UI behavior, grass stability and distance, origami, comic and painting styles. | [Grass layout](../Documentation/Grass-Consistency-Batch028.md) · [Visual styles](../Documentation/VisualStyleSystem-V2.md) · [Grass distance](../Documentation/Grass-Distance-Batch032.md) |
| 033–034 | Version-control integration and scene checks, followed by goose scale, cadence and preview checks. | [Integration](../Documentation/Version-Sync-Batch033.md) · [Goose review](../Documentation/Goose-Polish-Batch034.md) |

The goose preview did not establish live fight behavior or precise skinned-foot contact. Follow-up questions from that batch are kept in the [coverage notes](NEXT_TASK.md). The original dated batch entries are available in the [full progress log](https://github.com/cpy114514/Dark-Brine_Rot/blob/adbe8cc794659768260f8ff7b007003fb6de6264/AUTO_QA/PROGRESS.md).
