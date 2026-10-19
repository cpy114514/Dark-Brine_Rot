# Sahur opening film pose revision v215

Source: `sahur-pose-v215.blend`. Three Actions on the preserved 28-bone actual
PbrSahur rig, retaining mesh, rest/bind pose, bone names, uniform .01 armature
scale and all original Actions. No Unity scene/runtime edits are made here.

| Action | Frames at 60 FPS | Duration | Purpose |
| --- | --- | --- | --- |
| CH1_SahurOverhead_v215 | 1–169 | 2.8 s | Two hands raise the fitted stick above the bowed head, then bring it down. Contact remains 1.3 s / frame 79. |
| CH1_SahurProneDodge_v215 | 1–145 | 2.4 s | Dive to timber with the left palm supporting and the right hand holding the stick alongside the body. |
| CH1_SahurGetUp_v215 | 1–151 | 2.5 s | Push up, gather feet with clearance, transfer weight to the feet and stand. |

The source rig uses centimetres inside a uniform .01 object scale. Reported
world measurements are metres at model scale 1. The live hero's scale 3 multiplies
them by three. `Hips` is connected to `root`; directly translating Hips does not
lower its evaluated position. The authored vertical root movement lowers the
prone hips from .7046 m to .285 m, while root X/Y remains in place. Runtime must
not apply a second cinematic hip/root lowering on top of this motion.

The weapon proxy in previews comes from the actual Unity fitted capsule endpoints
expressed in RightHand local coordinates. It has a .045 m source radius and is
rendered solely to check grip and arc; it is not saved into the working blend or
exported. The temporary z=0 plane is a support reference, also excluded from FBX.

The original short arms and long rigid head constrain the overhead pose. The head
bows to let both hands reach above it; the approved rig is not stretched or rebuilt.
The existing hand mesh has no finger articulation, so this revision does not add
an independently animated finger closure. Native avatar retargeting, original wood
mesh alignment and the paired shark contact still require Unity review.

Failed/intermediate source passes and previews are retained to explain the fixes:
pass01 exposed ignored connected-Hips translation; pass02 exposed vertical fingers
passing through the support; pass03 corrected palm axes and soles; pass04 added
guard joins and lifted foot gathering. The main blend includes the final grip
reach correction and neck clearance. `finalize_contacts` and `refine_existing`
are one-time migration operations and must not be rerun on the final main blend.

Stage is BLOCKING. A successful numeric validation/export is not final art,
gameplay, Avatar or release acceptance. See the versioned review and metrics for
the observed evidence and remaining limitations.
