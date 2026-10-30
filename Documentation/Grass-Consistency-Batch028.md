# Constant grass layout — batch028

The user reported that grass still changed after batch027. That correction softened a change; it did not remove it. Native patches continued switching from the complete layout to models that remove blades/cards. The consistency request is reopened and supersedes that policy.

## Change

For the recognized three-level `VerifiedGrass*` family, `WorldPerformanceManager` now selects the **original complete Near renderer at every visible distance**. It does not replace its mesh, vertices, UVs, normals or materials. The two alternative layouts are disabled while owned, so removing them from the LODGroup cannot accidentally make them ordinary always-visible renderers.

Only the final distance cull fades. Its previous screen-height cutoff and fade-band start are retained by remapping the width to the single-level range. Trees, other grass families, custom authored fades, wind and gameplay remain unchanged. Normal perspective, texture sampling, wind and the far visibility boundary can still change the image; the patch no longer changes to a different blade layout at the old near/mid thresholds.

Original native membership, renderer enabled flags, thresholds, widths and mode are restored on disable/recache. An explicit ID/count/height snapshot carries the original membership through an actual Editor assembly reload; runtime restoration uses the original renderer references. No authored scene or art asset is rewritten, and no new per-frame scan is added.

## Evidence

`AUTO_QA/Evidence/batch028/proof.json` is the acceptance record. Raw captures, user snapshots and build stay private in `.codex/grass028`.

| Coverage | Result |
|---|---|
| Direct native density comparison | Same camera/projection, frozen material wind, native size sweep through both old model thresholds. Old fading policy has maximum adjacent-image delta0.1754167; fixed complete layout has **0 changed pixels at both thresholds**, across26 sampled images per policy. |
| Positive image control | Complete layout differs from empty rendering in23,193 pixels. Selected mesh remains the original23,097-triangle Near mesh. |
| Stability controls | Hold, ±18° rotation-return and ±4m dolly-return each produce0 changed pixels in the frozen isolated patch. These do not claim pixel-perfect moving world rendering. |
| Ownership / regressions |66 final assertions:31 LOD membership/flags/cull/scope/reload/lifecycle,12 core actual reload,12 lifecycle,6 additive scene recache,5 partial cancellation. Preparation9/30 overlap and are not added. Partial cancellation observed at383 entries. |
| Actual island policy | All11,298 recognized groups use one original complete renderer; the two unused renderers are suppressed.71,315 foliage entries retained. |
| Styles / pixel |192 captured camera positions across all four styles,592 render callbacks including warmup/empty frames. Actual2560×1440 pixel screenshot at runtime scale0.1250007 inspected; pixel state released and restored. |
| Geometry observation | Matched144-position ABA sweep with procedural Cappuccino excluded in the no-savable-player environment fixture. Aggregate native triangle medians:11,227,594 /14,408,668 /11,016,249; empty7. Baseline varies about1.9%; complete grass draws more geometry. This counter is not GPU time or game FPS, and no negligible-cost or FPS-gain claim is made. |
| Build | Same Editor17184, seven enabled scenes, Windows64 Mono Development: Succeeded65.32s.493 warnings and4 existing scene/missing-prefab messages for the known606 missing plants; no new compile/shader error. New Player not launched. |

The camera paths use travel±18m and yaw±22°. Videos are12fps encodings of discrete native screenshots, not measured frame rate. Representative real/style/pixel and isolated threshold images were inspected.

Initial live comparisons contained unscaled procedural Boss motion. An early one-level prototype also left removed renderers active until explicitly suppressed. Both issues were corrected before production acceptance; those private trials are rejected as performance comparisons. The first environment sweep also included the authored Cappuccino presentation, so only the later explicitly excluded-boss ABA observation is reported above. Full-detail geometry has a measurable cost; if profiling shows an unacceptable FPS loss, optimize the fixed layout while preserving its blades rather than reintroducing distance thinning.

## Restoration and continuation

The user's running session was snapshotted, then stopped normally; its newer save and backup were kept. Subsequent empty/environment fixtures had no savable player and retain both post-user-stop files byte-for-byte. Actual pre-test Editor setup was **one clean MainMenu scene**, restored exactly; the five first-island scenes observed earlier were runtime additive scenes. Do not replace that Editor setup with the previous batch's five-scene setup.

Original assets and scene files remain unchanged. Native fixtures0, marker cleared. Build flags/preloads/ProjectSettings bytes restored; fresh hashes of the four unrelated Plastic settings/Recovery paths and duck material match before build. Styles were selected with `persist=false`, pixel was runtime only; complete preference-byte equivalence is not asserted without a full fresh snapshot. Same project Editor only. Temporary CLI disconnections during Play were followed by guarded native state checks; no duplicate Play/Editor was started.

Checkpoint title: **Keep complete grass layout independent of camera distance**. Baseline Git092fd82 / Plastic cs212. Next: inspect any newly reported residual effect separately (far cull, sampling or wind), and qualify actual FPS impact if needed; do not repeat unchanged matrices or treat the old fading policy as satisfying constant density. Preserve duck/UI fixes, four styles/pixel,606 missing plants, user progress and unrelated checkouts. The missing automation remains unscheduled and must not be blindly recreated.

Sync inspection also found an independent remote Plastic cs213 with second-island work. Its paths are disjoint from this patch; it was not imported into the tested workspace. Only the owned grass patch is submitted, preserving that changeset. These test results cover the recorded baseline plus the grass patch, not that external work.
