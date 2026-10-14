# v096–v098 animation and transition revisions — integration review pending

## Actual new shark animation v097

Authored new `CH1_SharkTail_v097` and `CH1_SharkShipSmash_v097` on a versioned copy of the original 16-bone Tralalero rig. Rear spine, tail base/mid/tip, head and fins use staggered anticipation and recovery timing. The massive Body leads unchanged; the caudal chain follows with up to 70 ms source-time lag, converging before contact. Mesh, rest/bind, bone identifiers, original Actions and the original prefab remain intact.

Source 60 FPS / frames 1–91 / 1.5 s / contact 0.65 s. Source front/side/three-quarter anticipation, contact and recovery poses reviewed for FIRST PASS staging. Final normal-speed paired performance remains unapproved. An initial stronger lag exceeded the 10 degree/source-frame local step guard and was rejected before saving/export; the staged revision uses the reduced lag.

Actual native original-rig comparison PASS across 91 samples for both Actions: 170 curves each, 60 FPS, duration 1.50000012 s, valid Generic Avatar; unchanged local rotations 0 degrees, maximum local position differences 3.815/4.046 micrometres. Revised bones differ up to 4.652/5.362 degrees from v048. Native contact pose rotation and Tail_Marker difference both exactly zero. Maximum local angular steps 8.557/9.942 degrees/source-frame.

Active `EncounterShark.controller` now references the new tail and ship-smash clips. `Assets/Resources/Encounter/v097/PreviousEncounterShark.controller` preserves the previous controller; installer already ran, never rerun over this backup. Original rig/Avatar, bite v078, locomotion, original left attacks and throw v064 are unchanged.

Source: `ArtSource/Encounter/v097/shark-tail-inertia-v097.blend`; source audit/review and FBX export manifests alongside it. Native comparison: `.codex/opening-optimization-20261012/tail-import-v097.json`.

## Actual v096 four-view review and newly found defects

Recorded 614 native offline frames, four encoded continuous views in `native-paired-v096`; supported diagnostic fixture (requested water=false, but actual admission still entered from swimming). Four hero contacts PASS; both shark contacts FAIL (1.962/1.668 m), triggering the honest no-contact exhaustion exit. New v086 hop not exercised, zero natural-neighbour transfers. Supporting height FAIL 14.744 cm at 17.257 s; fixed slide 1.369 mm. This take is not accepted as complete choreography.

Displaced-water visibility samples: 174 snapshots, six head sightlines blocked. Screen framing FAIL: eight cropped-crown frames out of 541, max lens 85 degrees. The game/diagnostic contact sheet shows clearer early exchanges, but layouts differ from earlier takes, so do not attribute all differences solely to camera code or claim final art approval.

Ending review exposed a real missed-contact-path bug: no upright return or clock drift, loss of stick and unconscious island handoff PASS, but wave bob/rock were both zero, head remained 1.154 m above water. `fromWater` was the admission state, so exhaustion after a real climb mistakenly disabled gravity.

## Runtime fixes v098

- Exhaustion now determines immersion from the actual chest relative to the current ocean. A character who climbed out during admission falls with gravity from that actual board; a currently immersed swimmer retains the water variant. Final native wave/pose regression pending.
- Switching shot sides now interpolates around the focus on an orbit, instead of crossing through it. Aim follows that already-smoothed orbit directly after the opening blend, removing the second rotation lag that could put actors behind the lens. Framing regression pending.
- v096 displaced-wave sightline checks and bounded elevated camera alternative remain active; native screen inclusion still does not prove geometry visibility or artistic quality.

Latest capture `native-paired-v099` is running in the original Editor at this checkpoint, with actual water admission and v097 tail/v098 fixes. Do not edit Assets, duplicate the recording or start another Editor until it completes. Ending observer must finish before stopping Play. Preserve the AB575 authoritative save after confirmed stop.

## Standalone performance status

v095 independent rendered 1920x1080/DX12 seed14: 2,326 completed GPU readbacks, zero errors; 20.803 s film. Max frame 37.784 ms, zero film frames over 50 ms, mean 16.908 ms, but 1% low 37.292 FPS FAILS the >=50 target. Max facing planner 13.661 ms / solid 2.904 ms in this particular layout. Progressive facing search used 27 extended batches; first and reversal contacts still failed (4.202/0.677 m). Do not claim cross-layout contact or FPS acceptance.
