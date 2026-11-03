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
