# Island 1 route: original encounters only

The island now follows its existing progression spatially: arrival → Cappuccino → chicken/duck/goose waves → Nailong → Bombardino aircraft → capture and transfer to the second island. No additional enemy encounters remain. The previous 20 roadside patrol enemies, patrol grouping script and AI leash extension were removed; ordinary bird AI is back to its prior implementation.

## Locations

| Existing encounter | Ground position X, Y, Z | Checkpoint X, Y, Z |
| --- | --- | --- |
| Cappuccino | 241, 38.39, 465 | 275, 30.10, 408 |
| Chicken/duck/goose flock | 142, 51.20, 565 | 191, 46.44, 518 |
| Nailong | 20, 55.34, 710 | 62, 54.20, 660 |
| Bombardino aircraft home | -114, 32.94, 826 | -32, 46.16, 754 |

The flock retains its original 4 / 6 / 8 waves, three prefab references, eight-alive cap and one goose per wave. Its existing `placeBesideCappuccino` setting now supports an authored position when false; older scenes using automatic positioning retain that behavior. Boss identities, progression policy, health, damage and capture implementation are unchanged.

The route continues across the island instead of sending the player from northern Nailong back to the central aircraft encounter. The four existing checkpoints from the prior pass were moved to the approaches. Only the first is a default; later checkpoints require visiting. All keep their stable save IDs. The aircraft checkpoint is 109 m from its home, outside its 100 m engagement radius.

The existing M4A1 pickup is now just beyond Nailong at XZ (5, 727), with its original prerequisite intact. Two existing supply caches and six existing ammunition pickups follow the later route; no extra rewards were added.

## Island construction

Four softly graded encounter clearings and two route saddles modify the inland terrain. They affect 1,444 of the existing terrain vertices, with at most 3.4 m of vertical change. Mesh topology, island outline and ocean settings stay unchanged. The same height function drives rendering, collision and world-height queries, so the layout survives scene reload.

Seven obstructing trees were moved to clearing/route shoulders; nine tree roots were reseated in total. Protected plant clearings and worn paths follow the new route. The final rebake has 136,643 grass instances and 2,724 small dressing instances. It reports zero unsupported grass roots, route incursions, missing materials or shader errors across 430,120 grass-root samples. A separate 199,502-point decorative-root check also passed after seating actual contact vertices on the graded slopes. Existing tree variety and woodland habitat work are retained.

## Validation

The static route check sampled 357 positions every approximately 2 m: no missing ground or static capsule obstructions; maximum sampled slope 22.4°. All four checkpoint positions sit on the ground. The authored scene contains zero ordinary bird enemies; only the existing wave encounter creates them at runtime.

All 40 runtime checks passed: original boss roster, all four checkpoint activation/respawn/save-restore flows, damage immunity before unlocking, the original 18 flock enemies (10 chickens, 5 ducks, 3 geese), sequential boss unlocking, rifle placement, aircraft capture and successful delivery into the second island. No runtime errors were recorded. The actual player save files were byte-checked unchanged; the editor save override was cleared and the five-scene workspace restored.

Evidence: [full story checks](Island1RouteReview/story-validation.json), [route sampling](Island1RouteReview/route-validation.json), [placement details](Island1RouteReview/layout.json), [vegetation](Island1RouteReview/vegetation-validation.json), [decorative roots](Island1RouteReview/placement-validation.json), [relocated flock](Island1RouteReview/flock-clearing.png). The scripted test uses disposable save storage and accelerated enemy defeats to check sequencing; it is not a manual combat balance playthrough.

## Authoring

`Tools/ReflowIslandRoute.cs` contains the authored positions, terrain grading, tree relocation, protection settings and native scene editing operations. Apply is deliberately guarded against running twice. Edit the scene objects and the ProceduralIsland landforms directly for further tuning, then rebake with `Tools/BakeIslandRealism.cs`.

`Tools/VerifyIslandRoute.cs` starts from isolated Environment + Lighting Play Mode, loads disposable Gameplay/Enemies scenes, checks checkpoints and the full encounter/capture chain, then stops Play. Use `ReflowIslandRoute.Restore` afterward to clear the save override, verify real saves are unchanged and restore the editor scene setup.
