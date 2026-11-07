# Island 1 woodland and encounter follow-up

> Superseded encounter layout: the user requested relocation of the original encounters only. The 20 patrol enemies and their group/leash code described below have been removed. See [the current island route](Island1Route.md) for the corrected roster, checkpoints and terrain changes. Woodland work remains, with updated vegetation counts in that report.

This pass addresses repeated tree silhouettes, misplaced undergrowth, long empty travel sections, and long runs back to bosses. Ordinary walking remains free of stamina cost; sprint, dodge and attack costs retain their existing values.

## Woodland

- Replaced 10 repeated bare-tree visuals with four branched, leafy coastal/woodland variants. Some dead trees remain as accents.
- Varied 19 mature trees with three crown profiles while retaining their original lower stems and collision geometry.
- Recomputed woodland habitat from actual trunk geometry and canopy widths, including offset imported pivots. Plant footprints avoid protected paths, use local slope and shade, and maintain spacing.
- Reduced small dressing from 5,315 to 2,798 instances. Grass now has 136,493 instances. Ferns, fungi, moss and woody debris cluster beneath suitable crowns.
- Registered all 30 new tree mesh references with the existing foliage performance library, including distant LOD and shadow handling.

The placement check sampled 202,389 decorative roots with no unsupported roots, excessive floating gaps, invalid bounds, missing materials or shader errors. All 10 replacement trunks were grounded. No tree colliders were added; the Environment scene retains 54 colliders.

## Encounters and checkpoints

Ten two-member patrols add 20 enemies across the shore path, coastal rise, east grove, ridge flank, west meadow, west slope, north crossing, north grove, western overlook and far woodland. They reuse existing chicken, duck and goose prefabs. Each patrol has its own single-attacker budget, 17 m sight distance and 19 m home boundary. The optional boundary defaults to zero for existing arena enemies.

The original default checkpoint remains. Three new points require visiting within 5 m before becoming the active checkpoint; they have visible jade stone markers and appear through the existing map marker system.

| Checkpoint | Ground position (world X, Y, Z) | Purpose |
| --- | --- | --- |
| Cappuccino approach | 263, 34.15, 445 | Before the first boss |
| Flock and Bombardino approach | 214, 38.54, 580 | Shared ridge return point, outside the flock trigger and aircraft's 100 m engagement range |
| Nailong approach | 35, 41.14, 817 | Before the northern boss |

Patrol homes are more than 29 m from checkpoints and outside the existing boss/encounter buffers. Each new enemy and checkpoint has a stable save identity. Patrol deaths remain saved without advancing the story flock or boss progression.

## Verification and scope

The actual island Play Mode fixture passed 52 checks, including checkpoint activation, death/respawn at every new point, restored health/stamina, checkpoint save/restore, patrol grounding, independent flock membership after re-enabling, home-boundary disengagement, and enemy death persistence. No runtime errors were recorded. This is a focused regression check, not a complete playthrough of every boss.

Existing Enemies and Gameplay scene blocks were unchanged relative to the pre-pass snapshots; each scene gained its owned layer and root-list entries. Environment's existing transforms remain unchanged. Native Unity saves reassigned generated mesh file IDs, but the terrain collider geometry and ocean mesh payload match the pre-pass snapshot. The inactive legacy island gained default serialized habitat fields. The player's real save files were checked unchanged and the editor test save override was cleared.

Evidence: [placement validation](Island1WoodlandReview/placement-validation.json), [encounter checks](Island1WoodlandReview/encounter-validation.json), [placement plan](Island1WoodlandReview/encounter-plan.json), [native scene scope](Island1WoodlandReview/native-scope.json).

Matched woodland views: [before](Island1WoodlandReview/woodland-before.png), [after](Island1WoodlandReview/woodland-after.png), and [undergrowth](Island1WoodlandReview/undergrowth-after.png).

The final isolated Environment + Lighting measurement used a fixed 1920×1080 render target at render scale 1 on RTX 4060 / i5-14400F, with 90 warm-up and 240 measured frames. It averaged 14.51 ms with a 17.86 ms p95, approximately 936 draw calls and 6.17 million rendered triangles. Registering tree LOD/shadow handling reduced the same fixture's geometry from 10.06 million triangles; these two samples did not demonstrate a frame-time improvement. Editor views can contribute to counters. This measurement excludes the added patrols and does not establish standalone gameplay FPS. [Raw result](Island1WoodlandReview/performance-accepted.json).

## Authoring

`Tools/BuildIslandWoodland.cs` creates/rebuilds tree variants and registers their performance entries. `Tools/BakeIslandRealism.cs` rebakes the environment habitat. `Tools/BalanceIslandEncounters.cs` previews and authors encounters in the Enemies scene and checkpoints in Gameplay; its Apply method rejects duplicate installation and dirty owned scenes. Existing authored patrols and points can be edited directly in their scene hierarchies.

`Tools/VerifyIslandEncounters.cs` runs the focused regression from isolated Environment + Lighting Play Mode. It loads Gameplay and Enemies into that disposable session and redirects saves to `.codex/island1-woodland/encounter-fixture-save.json`. Stop Play after testing and run its `RestoreSavePath` entry point before returning to ordinary play.
