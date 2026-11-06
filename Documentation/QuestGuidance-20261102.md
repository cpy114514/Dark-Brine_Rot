# Full destination guidance — 2026-11-02

The white quest thread now plans a complete route to the actual objective instead of a nearby subgoal. The old 27.5 m planning horizon, 45 m length cap and 23–31 m end fade are removed. One white mesh tube (11 cm diameter) replaces the faint three-strand treatment. Traveling shader highlights remain.

The fallback uses an adaptive 97×97 grid covering both endpoints with detour margin, a binary heap, segment obstruction checks, and a 1 ms search budget per frame. Route storage supports 1024 points and adapts sampling to path length. Incomplete searches do not commit a misleading partial route. Ground projection, obstacle checks, combat/menu hiding, target selection and gameplay progression remain in place.

Validation:
- Unity 6000.3.23f1 compiled the new component in memory without interrupting the active game.
- Native physics fixture: 350 m destination, 80 m wide blocking wall; full 383.04 m detour, 445 points, zero endpoint error, no wall crossing, endpoint opacity 1.0. The test passed in 2.81 seconds over 135 yields. Evidence: `.codex/thread060/full-route-test.json`.
- Actual First Island geometry and objective: 83.23 m full route, 99 points, from (265.75,32.79,426.12) to (238.95,38.77,498.18). The objective root is (238.95,38.83,498.18), projected onto its ground. Native player-camera and overhead renders were visually inspected; the continuous white line reaches the Boss. Shader compilation reports no errors.
- Evidence: `.codex/thread060/live-player.png`, `live-near.png`, `live-complete.png`, and `live-route.json`.

The user had the pause menu open during review. Pause, save, camera and objective progress were preserved. A temporary in-memory copy of the updated component is attached to the live player; its old renderer is disabled once the new complete route becomes visible. Both temporary components disappear on leaving Play mode. Source recompilation is deferred until Play mode exits; the existing editor preference restoration callback restores its original value (0). No forced play-mode exit or domain reload was used.

Rollback: restore `Assets/Scripts/Effects/MainQuestGuidance.cs` from `.codex/thread060/rollback/MainQuestGuidance.cs` outside Play mode. No scenes, materials, shader assets or objective order were changed by this adjustment.


## Stable route update

Walking along the route now retains the same terrain-anchored path. Replanning is requested only when the player is more than 6 m off the existing polyline, the destination moves over 6 m, or the objective changes. This replaces the old 3 m travelled-distance trigger that rebuilt a player-centred fallback grid. The opacity cursor projects continuously onto route segments, rather than jumping between nearest vertices. The tube no longer sways geometrically; the existing shader highlights continue to travel along it.

A necessary reroute retains the old path during calculation, fades it out over 0.18 s, commits the replacement at zero visibility, then fades the new path in over 0.3 s. Complete destination routing, collision checks, the white 11 cm tube, and existing mesh/index buffer optimizations remain in place.

Native Unity 6000.3.23f1 verification passed seven checks: the 350 m full destination and wall detour; zero replan requests across 40 forward-movement samples; fixed world geometry while translating/rotating the owner and advancing time (maximum 2.77 mm float round-trip error in the far-away physics fixture); continuous segment progress (under 0.98 mm error); genuine deviation and moved-target replans; and reroute commit at zero line visibility. Evidence: `.codex/stable-guidance062/native-tests.json`. The native fixture compiled the modified component in memory. Final inspection also confirmed that the actual production MainQuestGuidance type in the current running game has the new scheduling method and is visible. A ten-second live observation (597 frames) retained one solved route and measured zero world-geometry drift; the player remained stationary during that observation, so moving/turning validation is provided by the separate physics fixture. The actual camera capture `.codex/stable-guidance062/live.png` was inspected. No save, combat, player-input, or camera state was changed by these checks; the already-deferred script-compilation preference was preserved.

This task's rollback snapshot is `.codex/stable-guidance062/MainQuestGuidance.before.cs`; it includes the pre-existing mesh-buffer performance changes. Other pending workspace modifications were preserved.
