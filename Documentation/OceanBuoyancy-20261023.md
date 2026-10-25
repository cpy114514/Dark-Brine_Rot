# Ocean, wreck buoyancy and shore white water — v244

Updated the existing sea and wreck system in the existing Unity 6000.3.23f1 Editor. The work preserves the current boat fracture sequence, combat, tutorial and authored save/quality settings.

## Changes

The four existing Gerstner waves now have independently advected wave-group fields. Height envelopes and phase curvature build, cross and disperse over visible timescales, rather than retaining almost stationary rows of crests. The CPU sampler mirrors the HLSL constants. This uses the existing four noise fields and four waves, with deterministic continuous motion. The authored wave sizes, wavelengths, directions and motion-speed setting stay in use. Displacement culling bounds cover the new bounded envelopes and crest harmonic.

The forward ocean patch now concentrates its existing triangles within the nearby view: its half-width at the camera changes from 1,440m to 90m for the 3km sea, and positive depth uses an exponential distribution. Low, Medium and High retain their previous vertex/triangle budgets, including the separate downward-looking square mesh. Medium forward coverage remains 22,057 vertices / 43,520 triangles. This improves the geometric match to the same wave field, particularly near beach contacts.

Wreck buoyancy retains native gravity, mass, compound contacts and real-fragment pressure probes. Broad decks have slower, critically damped angular responses; small offcuts can respond more quickly and tilt farther. Nearby sampling changes from 80ms to 60ms; distant small timber from 240ms to 120ms, with a 40ms fast-entry interval. Prediction remains bounded to 80ms so changing crests do not amplify stale velocity. Optional mesh lookup uses Unity-safe TryGetComponent.

The shoreline ribbon bakes actual terrain-triangle heights into UV1 once when built. Its white-water front follows the shared unwrapped swell phase: a short rush, spreading broken lace and a slower retreat/fade. Wetness and terrain depth keep it at the sea contact, rather than painting a detached band on dry sand. A 14cm rasterization bias accommodates the independently tessellated ocean/ribbon. The water shader's unrelated depth-sine stripes are replaced by actual wave-compression foam, with a restrained depth-contact layer. The ribbon extends 8m inward to accommodate real high-water contacts, with existing lighting, shadows and fog.

## Verification

Evidence: `.codex/ocean-buoyancy-20261023/acceptance.json` and its referenced captures.

- GPU/CPU comparison: 4,608 real GPU samples across three quality levels, four clocks through one hour and irregularity 0 / 0.85 / 1. Maximum height difference 9.32mm; maximum normal-versus-finite-difference dot error 0.000736. The 16,384-query sampler window allocated 0 managed bytes and took about 133ms in the Editor. This is a sampler measurement, not a whole-game allocation claim.
- Same-scene simultaneous reference/current buoyancy fixtures: small-board mean absolute centre-to-water offset 0.102m → 0.058m; narrow timber 0.145m → 0.081m. Large-deck offset remained approximately 0.113–0.115m, including intentional freeboard, with peak settled tilt 9.37° → 9.06°. These offsets include equilibrium draft and are not pure tracking error. Gravity and non-floating sinking remained active. The frozen prior implementation exists only in the outside-Assets test helper.
- Six generated mesh configurations pass finite-coordinate, index-validity and unchanged-budget checks. Sampled forward triangle-centroid surface error is recorded separately in `mesh-verification.json`: approximately 0.132m mean before and 0.0265m after at the recorded viewpoint. The two meshes have different centroid densities, so this is a geometry diagnostic rather than a fixed-point equivalence or FPS comparison.
- The current wave/physics implementation completed the entire native opening: 17 charge and 23 geometry/contact/continuity checks pass across 2,773 recorded steps. Both tutorial strikes confirm; three cinematic contacts occur; real wood still fractures; original shark scale remains 3; Sahur finishes at 70HP. Eighty independent posed-film probes have minimum conservative body gap 0.0283m and zero sampled shark/timber triangle intersections. This run preceded only the final render-mesh redistribution; no subsequent wave, physics, collision or gameplay timing changes were made.
- Exactly one valid generated shore ribbon remains. Both water shaders report zero errors. The ocean's pre-existing SailingFoam uninitialized-variable warning remains; it is not a new compilation failure. No new captured Console errors occurred in this verification session.
- Final 21-second silent native recording: `.codex/ocean-buoyancy-20261023/final/OceanBuoyancy-v244.mp4`, 504 frames at 24fps across shore, oblique coast and open sea. Reviewed temporal keyframes and the native contact sheet. Final raw frames remain available. Intermediate takes retain encoded videos, metadata and selected keyframes; only this task's superseded intermediate raw frames were compacted after decode/hash checks.

Final steady fixed open-sea Editor observation: 452 frames, median frame time 16.67ms at the existing 60fps cap; GPU mean 5.24ms on RTX 4060. Runtime renderScale was the user's existing 0.6. The initial human-controlled shore/forest view differs, so these numbers do not establish a whole-game before/after FPS gain. Near-shore GPU cost with every gameplay system active, low-end hardware and a new standalone build have not been validated. Low internal resolution still limits coastline anti-aliasing in the review video.

## Rejected observations and preservation

The first asynchronous pre-change fixture capture was stopped before completion and is not a valid baseline. Replaced it with simultaneous old/new bodies under identical current waves. An early 180ms distant prediction horizon increased heave error and was rejected. An early terrain-lifted run-up ribbon left detached white sand bands; final foam follows actual water height and wetness. The first low-opacity revision was too faint against the ocean depth and was retuned before final recording.

Human save, saved Story1 scene, QualitySettings and recorded scatter playback hashes match their pre-work snapshots. Existing v243 edits remain intact. All experiment objects/cameras were confined to Play Mode; the Editor was stopped after verification. No source assets were removed and no scene/prefab serialization or version-control upload was performed for this task.

## Tools

`Tools/ReviewOceanV244.cs` runs paired native buoyancy fixtures. `Tools/VerifyOceanWavesV244.cs` and the outside-Assets validation shader compare GPU displacement/derivatives against the CPU. `Tools/CaptureOceanV244.cs` records native coast/sea views. `Tools/VerifyOceanMeshV244.cs` measures tessellation error; `Tools/VerifyOceanMeshBudgetsV244.cs` validates all mesh configurations. Existing v242 native contact auditors and the v243 passive profiler were reused.
