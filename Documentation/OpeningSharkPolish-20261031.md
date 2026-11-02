# Opening shark animation polish — 2026-10-31 / task 035

The user requested improvements to movement/impact, camera/pacing, and clipping/floaty transitions. This pass revises the established encounter, retaining its action order, animation assets, shark scale, eight-second helm, real tutorial input, native fracture/contact solving and nonlethal health budget.

## Changes

- Distribute the pursuit look-back over chest, neck and head instead of applying a 100-degree head-only rotation. Search on the board also uses upper-body rotation while retaining the supported root heading.
- Increase the pursuit head waterline and reduce the two authored dips. Carry the actual outgoing swimming height into the existing climb over its initial 22% rather than snapping to the low climb support point.
- Remove pursuit close-up cuts 21 and 23. Retain the look-back and two-shot reveals, with bounded clear-camera candidates and a more visible frontal climb.
- Resolve camera composition after final actor/shark contact corrections. Two-shots now use the current resolved nose; the final camera query includes the original posed shark triangles for pursuit occlusion as well as native wood. The look-back can choose a side angle past a nearby horizontal beam. No geometry is hidden or removed for the camera.
- Keep the prone view and get-up on the same side, lower the former top-down composition, and limit extra prone-camera lift.
- Shorten counter/launch slow motion. Raise the counter's approaching nose pitch from .55 to .85 vertical/forward ratio to reduce jaw/deck conflicts; native geometry still decides whether the club actually connects.
- Keep the original tail turn/contact fit. The earlier-start tail-fit experiment in `after-2` is rejected and reverted; `Story1WreckBattle.Solids.cs` has no net change.

Only `Story1WreckBattle.Redesign.cs`, `Story1WreckBattle.Camera.cs`, and the opt-in `OpeningTailReview.cs` change under Assets. The replay now uses a disposable Editor InputSettings copy to avoid background-focus resets and restores input/settings on disable, Stop, reload and quit. It requires both tutorial hits and all three film contacts for success. Its timeout distinguishes a missing tail strike from an airborne exit that has not reached water.

## Evidence

Authoritative take: `.codex/shark-opening035/final/`. Native 30 Hz functional capture: 3,091 frames, eight-second helm, two real tutorial hits, three film contacts, tail/flight/splash/drift, 70 HP, zero captured runtime errors. Eleven functional/geometry checks pass. Seventy-five independent posed-mesh probes, including all three contact frames, have minimum measured body-envelope gap .003246 m and zero sampled shark/timber intersections. Original shark scale remains (3,3,3); twelve native plank fractures were observed.

Observed baseline → final measurements:

| Measure | Baseline | Final |
| --- | ---: | ---: |
| Head above local ocean during pursuit | 65.7% | 96.8% |
| Lowest pursuit head relative to local water | -1.514 m | -.145 m |
| Maximum vertical root step at climb entry (8.9–9.2 s) | 1.661 m | .127 m |
| Duel frames in slow motion, at 30 Hz | 2.633 s | .833 s |
| Camera lens inside native timber | 0 frames | 0 frames |

Takes have different freely floating timber layouts; these are observations, not a controlled all-layout quality/performance benchmark. Head-above-water and viewport inclusion do not prove unobstructed visibility.

Review artifacts: `SharkFight.mp4` is the silent native-rate pursuit-through-drift movie; `BeforeAfter.jpg` compares corresponding narrative times. The four stage sheets, `metrics.json`, `acceptance.json`, and `independent-film-geometry.json` retain the detailed evidence. `frozen-source-sha256.json` matches the final runtime/tools/scene bytes.

Earlier takes remain: `before` was interrupted after a background-focus input failure; `baseline` is the complete old presentation with the repaired recorder; `after-1` passes contacts but has bad pursuit occlusion; `after-2` is rejected for a missed counter and incomplete exit; `after-3` passes mechanics but the first shot remains behind a beam. Its passing checks do not supersede final-source evidence.

## Review limits and remaining work

This is a targeted first pass, not human FINAL approval. Consecutive captures and stage/key-pose sheets were inspected; no claim of a full normal-speed/audio/hardware-performance review is made.

Some occlusion remains: final metadata reports two timber-obstructed focus frames and 29 shark-obstructed pursuit sightline frames. Wave wash remains physical. The original fast tail rotation still moves the snout up to 9.45 m per 30 Hz frame; the rejected early-fit experiment must not be restored as an accepted fix. Counter contact is placement dependent: `after-3` connects at Duel 5.303 s, but final connects late at 6.043 s after native separation. Three contact counts alone do not certify that counter's artistic timing. A future pass should solve its approach/board relationship across layouts and review the actual downswing/contact/recoil together.

Current scene remains the original clean, stopped/unpaused Story1. Physical keyboard 1 and mouse 2 are enabled; no synthetic devices remain. The fresh human save is byte-identical to the pre-test backup (SHA-256 A16153925DC3527DFC1026CB41F0ADF8ABB649B353172065DF6854D0E1BB2AEF). No source scene/animation/model save, new Editor, build, commit, push, upload or automation was performed. Existing combat/goose/ChickenBro changes remain.
