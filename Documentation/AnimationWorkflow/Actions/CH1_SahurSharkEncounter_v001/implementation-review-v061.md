# Opening optimization v058–v061 — 2026-10-12 Vancouver

Status: implementation in progress; not final animation approval or performance acceptance.
Design remains design-v039.md plus the approved no-surfing amendment design-v055.md.
Use only the existing original Unity Editor. Do not open a snapshot Editor.

Implemented:

- Native penetration scoring can stop after a proven nonnegative lower bound
  exceeds both clearance and the best candidate's score. Accepted scores remain
  exact; discarded bounds are not used to rank a winning route. Applied to
  entry attitudes, entry swimming paths, attack paths, shaft reach and tail grading.
- Removed synchronous duplicate whole-board edge search during Initialize. The
  actual combat stance is still chosen by the budgeted mesh/support/pose search.
- Entry swimming-route search now yields between native query batches.
- Existing encounter model/clip resources preload asynchronously during ship
  breakup, before the player-to-film handoff. No duplicate character is instantiated.
  Native Resources loading retains a synchronous fallback if preparation is interrupted.
- Diagnostic scatter seeds now control the actual System.Random used for fracture.
  Prior benchmark seed labels controlled Unity.Random but NOT that GUID-seeded
  generator. Older reports are valid individual runs, not matched-layout comparisons.
  Ordinary play still uses fresh GUID-seeded randomness; the override exists only in
  Editor/development builds and is assigned only by the opt-in benchmark.
- Escape search checks all distances that could possibly remain within the root
  travel budget before considering the retained pose. Triangle inequality guarantees
  any further escape would invoke the same original retained-pose decision.
  Reused retry bone arrays also remove allocation on each rejected large correction.

Native evidence in `.codex/opening-optimization-20261012/`:

- `bounded-planning-v058.json`: PASS,150actual queries,900limit comparisons,
  141nonzero cases,232exact accepted results,668rejected bounds,zero exact/reset
  error,identical winning candidates in both search directions. Four query repetitions:
  full486.8599ms versus bounded397.0907ms (~18% for this query workload only).
- `escape-decision-v061.json`: PASS,80actual frozen-wreck decisions,39blocked,
  48retained placements,zero final-position error against exhaustive native search.
  Full121.4251ms versus bounded117.8065ms; modest scoped improvement, not FPS proof.
- `native-profile-v058.json`: normal-speed original Editor1920x1080,full20.8s,
  4hero/2shark contacts,maximum gap.0377640575m,foot error.00122982229m,
  planted slide.00137500465m,water entry preparation1.32055187s. Film mean19.67ms,
  max46.34ms,zero film frames>50ms. Entry still had131.53ms spike.
- `native-profile-v058b.json`: another random layout,4hero/2shark contacts,
  preparation10.6466s;film had9frames>50ms. Do not generalize the previous success.
- `native-profile-v059-fixed-normal.json`: first actual fixed-fracture-seed run,
  4hero/2shark contacts,maximum gap.0477794558m,preparation3.0361s. Film mean20.79ms,
  max173.69ms;SolidContact139.13ms at4.46s and attack planning73.48ms at7.31s.
- `native-profile-v059-low20.json`: FAILED contact acceptance at50ms simulation
  steps in another (then still GUID-random) layout. Landing counter missed4.143m,
  reversal tail missed2.268m;3hero/1shark confirmed contacts. Geometry correctly
  suppressed those two hits. Support remained within1.2mm. This was offline coarse
  timestep testing, not a measured20FPS hardware result.
- `native-profile-v060-fixed-normal.json`: exploratory early-retained-pose policy
  gave4hero/1shark contacts. That policy was RETIRED: a fixed.1m search radius
  could discard reachable current-pose placements. Source is preserved in
  `backups/rejected-early-hold-v060.cs`; v061 uses the proven radius instead.

Native compile passed after v061. `native-paired-v061/review.json` is INVALID:
the recorder bound an old completed encounter while its own benchmark asynchronously
loaded a new scene, producing only one frame. Do not treat it as a continuous review.
The recorder now uses its own `benchmark.Battle` and rejects a missed beginning.
The separately completed `native-profile-v061-recorder-race.json` is a valid
benchmark, but FAILED contact acceptance:3hero/1shark contacts, probing bite gap
.7902068m and reversal tail gap.436733723m. No fake hits were accepted. The v062
capture is a fresh scene/play run using the corrected recorder; await its report.

Still required: resolve long admission in dense layouts, failed actual contacts
under coarse timesteps, physical safe-neighbour hops, attack/solid CPU spikes,
shark head/root attitude and camera occlusion, continuous normal-speed art review,
pause and end-to-shore regressions, and independent standalone1080p acceptance.
Do not relax collision/contact tolerances, move wreckage to stage a shot, invent
successful hits or claim whole-game60FPS. No commits or pushes.

Technical reference for asynchronous preloading:
[Unity Resources.LoadAsync](https://docs.unity.com/en-us/engine/6000.3/script-reference/unityengine/resources/loadasync).
Potential cooked-mesh bounds discrepancy remains unresolved; Unity documents
[convex flat-mesh thickness](https://docs.unity3d.com/jp/2021.3/ScriptReference/MeshCollider-convex.html),
but that alone does not prove the cause of the project's v056 native anomaly.
