# Whole-game performance pass — 2026-10-06

Measured in Unity 6000.3.23f1 on this workstation (RTX 4060 / i5-14400F), with VSync and the frame cap disabled for comparison. These are Editor measurements, not standalone-build frame-rate guarantees. Forest and panorama use the same fixed camera poses in both runs. The shore camera starts during arrival, so its small improvement should not be treated as a precise gain.

| View | Before average frame | After average frame | Before 95th percentile | After 95th percentile |
| --- | ---: | ---: | ---: | ---: |
| Main menu | 1.63 ms | 1.56 ms | 1.81 ms | 1.78 ms |
| Island shore | 7.17 ms | 7.00 ms | 7.91 ms | 7.63 ms |
| Forest | 12.96 ms | 12.50 ms | 13.27 ms | 12.76 ms |
| Panorama | 12.03 ms | 11.36 ms | 12.24 ms | 11.54 ms |
| Open map | 9.95 ms | 2.73 ms | 10.37 ms | 3.01 ms |

The map's average frame cost fell by 73%. Counted rendering triangles, including render passes, fell from 23.32 million to 16.10 million in the forest (31%) and from 10.70 million to 7.98 million in the panorama (25%). Gameplay GPU improvements are modest on this machine; the largest measured gain is the open map.

## Changes

- Added authored coastal-tree LODs to the runtime library. Five heavy tree meshes also have a lighter middle-distance mesh; their prior near mesh remains within 18 metres of their bounds. UVs, material slots, bounds and every disconnected foliage piece are retained. Source meshes, collision meshes and scene placement are untouched.
- Foliage distance checks use cached render state and scalar bounds distance. Grass submits only populated batches. Runtime caches wait for the additive scene group to finish loading.
- Open maps stop background world, shadow, depth/color-copy and postprocessing work; closing, disabling or destroying the map restores the camera settings. Map terrain uses reusable vertex/index buffers, including all submeshes and base vertices.
- Ambient occlusion runs at half resolution with its existing blur and appearance settings. The scene's render scale remains unchanged.
- Static translated labels and displayed health/coordinates refresh when their values change. Vitals and checkpoints cache their player references instead of repeatedly searching.
- Enemy sight checks reuse a hit buffer and grow/retry on overflow, preserving obstruction checks.
- Muted Nailong, Cappuccino and grass interactions skip synthesizing unused audio. Their attacks and foliage interaction remain active.
- The earlier ocean sampling and wreck buoyancy optimization is preserved.

## Validation

- Unity script compilation succeeded; no new Unity console errors during the scene and gameplay checks.
- `VerifyWholeGameOptimization.Run`: additive loading/cache initialization, camera restoration on map close/disable/destroy, visible map terrain, multiple submeshes/base vertices, language changes and dynamic labels, foliage material slots/bounds/UVs/normals, manager reenable, checkpoints, muted spit attack and cleanup.
- `VerifyOptimizedLockOn.Verify`: middle-click toggle, center priority, walls, short/long obstructions, death/range/destruction release, map input, attack/combo heading and camera tracking. Also tested more than 32 self colliders plus a wall to exercise buffer growth without missing an obstruction. The fixture disables the title menu's cursor controller while testing gameplay input.
- `VerifyStory1WreckFinale.Verify(true, false, true, false)`: 202 ship fragments, water combat, timed finale, four Sahur counters and three shark counters, lost stick, lethal finish without respawn, and unconscious/unarmed island handoff. Audio remains muted after the scene change.
- Inspected the forest and map images. Test runs restore the user's save; the Editor is left stopped on MainMenu.

Raw comparisons: `.codex/performance/whole-game-before.json` and `whole-game-after-ao.json`. Visual checks: `.codex/performance/visuals/`. `ProfileWholeGame.Run` is the repeatable Editor benchmark. `BuildBalancedFoliage.Run` rebuilds missing middle-distance assets while Play Mode is stopped.
