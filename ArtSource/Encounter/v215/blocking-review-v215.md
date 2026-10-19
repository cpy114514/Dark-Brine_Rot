# Sahur pose revision v215 — BLOCKING review

Review date: 2026-10-18, America/Vancouver. Reviewer: Codex animation review.
Production design and human authorization:
`Documentation/AnimationWorkflow/Actions/CH1_SahurSharkEncounter_v001/redesign-20261017.md`
and the user's “可以，那开始做吧”, followed by “继续制作”. This is stage
acceptance for export and Unity integration review, not human FINAL approval.

Source: `sahur-pose-v215.blend`, finalized contacts (pass05). The source keeps
the actual 28-bone rig, names, rest/bind pose, original Actions and uniform .01
armature scale. Only the three named v215 Actions are exported. Temporary stick
capsule and floor review objects are excluded from source and export.

| Action | Source frames | FPS | Duration | Root ownership |
| --- | --- | --- | --- | --- |
| CH1_SahurOverhead_v215 | 1–169 | 60 | 2.8 s | In place, native runtime controls actor movement; hit at frame 79 / 1.3 s |
| CH1_SahurProneDodge_v215 | 1–145 | 60 | 2.4 s | Horizontal in place; authored vertical root descent .419614 m |
| CH1_SahurGetUp_v215 | 1–151 | 60 | 2.5 s | Horizontal in place; authored vertical root ascent .419614 m |

Evidence inspected: all 114 images in `preview05/`, as nine view contact sheets
(`*-front.jpg`, `*-side.jpg`, `*-three-quarter.jpg`). These show actual skinned
mesh and a proxy made from the fitted Unity stick capsule. The plane is z=0.
Overhead frames include 1,16,31,49,61,67,73,79,85,97,109,129,145,169; prone
and get-up frames include 1,7,13,19,25,31,49,67,85,109,133 and the last frame.
This was still-frame review; normal-speed video, native Avatar retargeting,
paired shark contact and moving timber interaction are not observed here.

Observed outcomes:

- Overhead frame 49–67: both palms and fitted shaft sit above the bowed head.
  Frame 79 brings the shaft down and forward while both feet stay fixed.
  Silhouette/readability 7/10 for BLOCKING; foot stability 8/10 from sampled
  mesh plus stationary bone-contact validation. Weight/impact remain unscored
  until paired game playback, since no shark is present in this source preview.
- Prone frame 25 onward: the body is low and horizontal, the left palm supports
  on the reference plane, the right hand retains the stick, and feet stay close
  to the support. Readability 7/10; held support 7/10. Body lowers through root
  translation rather than ineffective connected-Hips translation.
- Get-up frame 1 matches prone's final pose exactly. Through frames 49–85 the
  knees gather and feet clear the plane; through 109–151 the character returns
  to the original guard. Readability 7/10; weight 5/10 because the support change
  remains a broad blocking transition rather than a polished push-off.

Measured evidence: `validation-v215.json` and independent
`revalidation-export-v215.json` both pass all three clips without errors or
warnings. Overhead feet have zero measured bone-origin drift; prone left-palm
hold has zero drift, and get-up palm frames 1–45 drift less than .000001 m.
Root horizontal excursion is at most .000000024 m. Final prone/start get-up
channel translation matches exactly. `pose-metrics-v215.json` samples the
deformed mesh at every source frame and reports maximum held left-palm distance
to the fitted shaft axis .023298 m, within the .045 m capsule radius. These
checks do not establish complete self-intersection freedom.

Important remaining limitations and integration requirements:

- Overhead frames 31–73: the long rigid head must bow substantially to clear
  the short arms without stretching the rig. Readable at this stage, but this
  needs native game-camera review and later pose polish. Do not rebuild or
  rescale the rig to disguise the limitation.
- Get-up frames 45–109: hand-to-feet support transfer and torso follow-through
  are broad blocking shapes. Polish the support release after native footage
  establishes the moving-board and avatar result.
- The source mesh's minimum z is about -.008152 m at the inherited standing
  sole baseline; source scale 1 differs from the live actor scale 3. Native
  sole-to-timber planting must account for this (~.02446 m at scale 3), and all
  old extra cinematic prone/get-up hip lowering must be removed to avoid
  double descent and penetration.
- Native grip, shark/shaft contact, water and moving board clearance, transition
  clocks, shadow/occlusion and gameplay damage are unobserved by this review.
  They require the actual Unity game camera recording before wider acceptance.

Outcome: accepted for BLOCKING export and isolated Unity import/game review.
No FINAL or release approval is asserted. Preserve retained failed previews and
pass01–04 sources. Do not rerun one-time source migrations on the finalized blend.
