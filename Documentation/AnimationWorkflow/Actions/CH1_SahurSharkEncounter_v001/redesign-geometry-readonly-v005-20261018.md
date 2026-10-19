# Opening redesign geometry audit v005 — October 18, 2026

Status: read-only source/evidence review. The next take and bounds probe are pending. This note does not approve the animation or certify absence of clipping. No Unity/Blender operations or NearPass/runtime edits were performed by this reviewer during this audit.

## Retained near-pass evidence

Source: `.codex/encounter-redesign-20261017/sixteenth-rigging-15/near-pass.json`, `frames.json`, and auxiliary `near-pass-audit/top-004.png` / `side-004.png`.

The 46 recorder rows contain 23 distinct diagnostic clocks (46.2253265–49.1586151). The intermediate frame repeats are cached values, not additional geometry samples. Three distinct clocks have negative conservative skull-envelope gaps. At clock 47.42531 (frames 1029–1030), the smallest gap is **−2.75610328 m**, exactly the skull capsule radius; its axis/triangle witness points coincide at approximately (1142.879, 3.626, 1124.031). Chest and hips envelope gaps there are +.5293 m and +.8991 m. The welded source topology reports closed and the diagnostic reports no inside uncertainty.

This proves a shark triangle intersects the **measured capsule axis**, not that the actual wooden head mesh penetrates by 2.756 m. The current skull radius is about 2.756 m; chest/hips radii at that clock are about 4.064/3.975 m. Earlier in the pass the chest radius reaches about 8.178 m. The worst capsule-axis witness is about 5.9 m from the native Head bone. A long wooden head has its pivot near its base, but these dimensions warrant checking the baked world space and selected weighted vertices before using the capsule as a staging target.

The auxiliary top/side images establish proximity and projected overlap of the original-size shark and board/hero. They do not independently establish the complete vertical surface intersection or validate the numerical capsule world space.

## Meaning and limits of NearPass metrics

- `SignedGap` is triangle-to-capsule-axis distance minus capsule radius. Negative values mean conservative envelope/surface overlap; they are not measured skin penetration depth. No unsigned zero is presented as clearance.
- Skull PCA uses posed vertices whose Head+Neck weights total at least .5 and dominate the three selected body regions. Normal hand/forearm-dominated vertices do not join that group. The independent MeshFilter/MeshRenderer stick is excluded. Shoulder/arm vertices dominated by Head/Neck in the original weights could still join it.
- PCA takes the native weighted vertices' principal long axis, then min/max axial projections and maximum radial distance plus .025 m. This reduces the bottom-pivot sphere problem but retains conservative capsule end caps/empty space. Chest and hips envelopes remain deliberately conservative.
- `EnvelopeMeasured` means all three groups contained some selected native vertices. It does not establish all-body coverage or correct baked-to-world space.
- The `Inside` flag is set only by the containment fallback when the initially nonnegative surface gap is revised by multi-direction parity. A false flag does not mean every axis point is outside: already-negative surface gaps skip that fallback.
- `InsideUncertain` covers nonclosed/inconsistently oriented welded topology and disagreeing parity votes. Positive surface clearance with uncertain containment is not an exterior-clearance certificate.
- Metrics update every .1 s by default. Review and summaries must deduplicate `SampleClock`. Hands, stick, legs, feet and board clearance are outside this three-region metric.

## Native bounds probe prepared

`Tools/AuditNearPassBoundsV217.cs` is an ephemeral read-only live observer, started with `AuditNearPassBoundsV217.Start(take)`. Default behaviour samples the first shot-34 frame once, writes `near-pass-bounds-v217.json` into the take, then disables only itself. It does not alter the main camera, actor/shark transforms, clips, source scene, or Play state.

It records each native hero SkinnedMeshRenderer's renderer world/local bounds, source mesh bounds, `BakeMesh(false)` local bounds and transformed actual vertex bounds, local/lossy scale and matrix, root-bone pose, native Head/Neck/Chest/Spine/Hips poses, and private PCA capsule axis/radius/count. A rotation+translation-only reconstruction is included as a diagnostic comparison, not as an automatically valid replacement transform. The diagnostic sample clock is retained to reveal any cache delay relative to the observer frame.

If native/baked bounds disagree substantially, explain and validate the vertex world space before changing clearance thresholds. If the same reconstruction pattern is wrong for the hero, audit the shark Contact reconstruction too; do not assume the error affects only the three-region diagnostic.

Minimal follow-up actual mesh confirmation is outlined in `.codex/encounter-redesign-20261017/mesh-confirmation-plan-v217.md`: save same-frame posed native meshes around the worst sample; test actual triangle/triangle crossings with tolerance and actual-vertex containment against reliable closed topology; retain triangle IDs/witness points; distinguish capsule empty-space overlap from demonstrated rendered-surface interpenetration.

## Water source review after parent corrections

The parent corrected the following mathematical issues in Water.cs; the reviewer confirmed their presence statically, not through a new Play recording:

1. A future rotated part AABB now rotates its centre around the body's COM and calculates absolute rotated extents. The group broad-phase reach includes the offset between bounds centre and COM. This removes obvious broad-phase misses for off-centre/rotating long pieces.
2. Baseline and candidate drift paths use the same 1.6 m envelope margin, with .5/1/1.5 s forecast samples and the authored .35 m/s drift included in offsets. They are compared on the same objective.
3. Nominal splash coverage now samples 1.3/1.65/2.0/2.35 s, with post-splash forecasts based at 1.65 s, informed by recent actual ~1.6 s flight durations rather than the older 2–2.6 s assumption.
4. Candidate selection preserves current avoidance velocity by default and explicitly compares the stationary path. It no longer unconditionally brakes towards an untested zero-velocity path when the current path is safest.
5. The native capsule query is enabled inside its query scope and disabled in `finally`, with its actual scene transform far below the world. New recordings must establish the resulting query costs; older zero costs do not validate the newly active query.

Remaining forecast limitations, ranked for follow-up rather than immediate expansion:

- **P1 evidence:** establish correct native baked-world body envelopes with the new probe, and inspect a complete take after native query activation. The source fixes do not substitute for runtime evidence.
- **P2 forecast:** finite .5 s samples can miss a fast thin fragment between samples. Long-horizon forecast is approximate: vertical velocity is damped analytically and angular rotation is capped at .7 s. Cost zero covers the evaluated poses only.
- **P2 trajectory:** candidate forecasts assume immediate candidate velocity, while actual avoidance uses bounded acceleration. Clearance margins provide some tolerance but do not mathematically validate the transient path.
- **P2 pose:** landing planning previews the final Fall pose; the real first drift seconds blend AirHit into Fall with changing root rotation. Bone capsules are sampled live, but fixed radii were calibrated in the final Fall pose, so their union is not independently proven to cover every vertex throughout the transition.

No new take has yet verified these Water changes or the new outboard/late-yaw/+3 m near-pass path at the time of this note. Original actor/shark scales must remain unchanged.
