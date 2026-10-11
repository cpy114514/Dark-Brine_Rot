# Native integration review — 2026-10-09

Overall status: INCOMPLETE. The current six-contact encounter has not passed final acceptance. No commits or pushes. Original walking, running and rolling remain restored; all new animation Actions and imports are versioned copies of the retained rigs.

## Leg-paddled small timber

The v022 forward/reverse paddle Actions are active, replacing the rejected v021 two-bone source experiment. Authored pelvis loading and reduced right-thigh sweep retain the original skeleton and Avatar. The left foot supports Sahur; the right foot performs a stroke beside the actual hull mesh.

Native propulsion requires the right sole to be outside the real board footprint, submerged in the sampled ocean, and the left sole to remain within 3 cm of actual support. Stroke phase is shared by the clip and rigidbody force. Recovery coasts. Longitudinal drag is reduced only for eligible narrow surfboards; lateral resistance, gravity, vertical buoyancy and ocean waves are retained.

Evidence: `.codex/encounter-v022/native-paddle-contact-verification.json`. All 86 observed paddling samples had valid left support, maximum error 1.392 mm. The 26 water-engaged samples had left error at most 1.093 mm, the right sole at least 14.967 cm outside the board, and at least 1.445 cm below the actual water surface. There was no unsupported or over-deck propulsion in this take.

Native traversal passed: actual forward/reverse acceleration, steering during a right-hand light attack, physical braking, jumping with inherited board velocity, fast continuous swimming, automatic climbing of a moving board edge, and broken-support release. Evidence: `.codex/encounter-v022/traversal-supported-verification.json`; continuous native movie: `.codex/encounter-v011-controls/v022-real-supported-paddle/native-controls-review.mp4`.

An actual 10.24 × 2.91 × 17.04 m hull section rejected throttle, steering and rider ownership while remaining an ordinary movement/combat platform. Evidence: `.codex/encounter-v020/large-platform-verification.json`. The large-platform and traversal fixtures isolate genuine released hull meshes only during tests; production choreography does not stage debris.

## Safe cinematic ownership

A native airborne test exposed stale/proximity-based grounding at handoff. Cinematic admission now requires actual CharacterController grounding with nonpositive vertical speed, an already mounted board, or swimming. Ordinary snapshot state refreshes from current physical contact before capture; attack grace near a deck no longer authorizes an airborne cinematic.

Current native evidence:

- `.codex/encounter-v024/airborne-handoff.json`: jump motion continued after low-health request; takeover waited for water entry and captured Swimming with the actual downward velocity.
- `.codex/encounter-v024/charged-handoff.json`: a genuinely held right-hand charge remained active until the bounded safe handoff, claimed after 1.0181 seconds, with BoardRiding context.
- `.codex/encounter-v024/unconfirmed-tail.json`: withholding the final contact window returned movement and attacks to the playable fight, retained positive health, and produced no invented defeat or unconscious state.

## Encounter action revisions still under review

v019 tail lift and v023 authored head rise retain the original shark skeleton/skin. Ordinary bite root pitch is bounded to 35 degrees; explicit breaching retains its own trajectory. v023 Generic import is valid, but current full encounter contact reliability remains insufficient, especially around dense original sails and occupied timber. A failed v023 edge-footprint experiment was reverted; its source and failed native evidence remain preserved.

The additive v024 edge experiment was rejected after a native water take measured 10.38 cm of planted-foot sliding. It has been reverted to the original conservative stance-circle search. Both rejected v023/v024 sources and their native failure evidence remain preserved. No contact tolerance or collision geometry has been relaxed.

Outstanding: current normal, water/low-health and low-frame-rate full encounter acceptance, genuine reachable neighbor-board traversal or a verified same-duration supported variant, actual timber fracture, and continuous game/front/side/three-quarter review after revisions. Historical successful takes do not certify the newer Actions.


## Native planning and caudal contact, 2026-10-09

v025 separates hypothetical anatomical route checks from the four live solid shark capsules. Four fixed enabled trigger planning proxies remain far below the world; native ComputePenetration still uses the real anatomy dimensions and original mesh shapes. An exact capsule AABB broad phase rejects only nonoverlapping bounds. Native 90-pose comparison across three original shark Actions matched all 71 blocked and 19 clear classifications; maximum equivalent aggregate depth difference was 0.078 mm. Microbenchmark 47.51 to 31.38 ms is evidence for this check only, not a whole-game FPS claim. Disabled planning capsules were rejected because Unity returned incorrect zero overlaps; the enabled implementation is active. See `.codex/encounter-v025/planning-geometry-verification.json`.

v026 permits the real Tail_Tip bone and its fully weighted visible fin vertex to comply within an existing near contact. It rotates at most 5 degrees, moves the vertex at most 15 cm, and rejects wood penetration. Distant misses remain misses, actual contact tolerance remains 5 cm, and the shark root/body is unchanged. An isolated native test closed an actual 7.5 cm vertex gap and preserved an 80 cm miss. This is a geometry proof, not full encounter acceptance: `.codex/encounter-v026/tail-compliance-verification.json`.

Current v026 full water take failed early/reversal contacts and exposed a capsized broad support: torso reached 2.408 m below the water at 5.63 s, support tilt 167.5 degrees. The final tail did genuinely contact and the horizontal unconscious body remained held with wave motion, but this does not accept the preceding film. Game and side sheets were reviewed; all five continuous movies are encoded under `.codex/encounter-v026-tail-fin-water`.

Current v026 low-frame-rate take also failed contacts. Native 518 frames averaged 102.87 ms (9.72 FPS); foot error 2.35 mm, sliding 2.71 mm, and actual timber collision held, but three contact cues missed. No lethal hit was invented. All five movies are encoded under `.codex/encounter-v026-current-lowfps`; game and side sheets show the missed counter/finish and repeated late frames after the film returns control, not a defeat.

## v027 supported combat blocking

New versioned Blender source narrows the actual guard/parry/slash/brace lower-body stance and adds hip/knee loading while retaining the original rest rig, upper-body right-hand arcs, timing and original Actions. Source ankle target error is below 0.00005 cm; right-arm local rotation difference is zero. Diagnostic source tubes are explicitly not Sahur skin and never exported. Source blocking accepted only for isolated native import.

Four original-Avatar Humanoid FBXs imported at 60 FPS, correct 1.6/1.1/1.4/1.3 s timing and scale 1. Actual Sahur skin was sampled in 156 native front/side/three-quarter frames, and all three pose sheets reviewed. Maximum sampled ankle/root radius is 0.912 m; the skin sole footprint is larger and remains authoritative. The walk/run/roll and ordinary attack controller were not changed.

First full water trial failed: two head counters missed, foot support became invalid on a 2.30 m wide real splinter (conservative sole footprint radius 1.259 m), and sliding reached 52.3 cm. Genuine first parry, throw and both tail contacts did not rescue the invalid support. Continuous native game/side sheets were reviewed. This trial is not accepted.

The subsequent runtime experiment selects another real, nearby flat surface before a water-entry swim/climb if the closest fragment has no verified stance circle. It verifies the actual guard/parry/slash/brace contact space using the existing conservative mesh tests, updates the swim distance and moving-board anchors, and does not move debris or teleport the actor. Full native verification is pending; do not mark v027 production accepted based on source or isolated retargeting alone.
