# Unity Collaboration Rules

This Unity project uses additive scenes so people and AI agents can work in parallel without repeatedly conflicting in one large scene.

The most important rule is: **do not put normal world content into `Main.unity`.**

## Scene ownership

- `Assets/Scenes/Main.unity`: bootstrap and additive scene loading only.
- `Assets/Scenes/Environment.unity`: island, ocean, terrain, buildings, props, trees, rocks, world geometry, and environment art.
- `Assets/Scenes/Gameplay.unity`: player, camera, UI/EventSystem, spawn points, checkpoints, interactions, triggers, and general level logic.
- `Assets/Scenes/Lighting.unity`: lights, volumes, fog, probes, sky, and environment lighting.
- `Assets/Scenes/Enemies.unity`: normal enemies, enemy spawn points, and encounter setup.
- `Assets/Scenes/Bosses/*.unity`: boss-specific actors, arenas, triggers, effects, and encounter logic when those scenes exist.

Use `Tools > Scenes > Open Full Workspace` to load the normal editor workspace. Environment and Gameplay workspace commands are also available.

## Before editing

1. Check whether another person or agent is editing the same scene.
2. Identify the additive scene that owns the object.
3. Modify the owning scene, script, or prefab instead of `Main.unity`.
4. Keep the change limited to the smallest reasonable set of files.

When working through Unity automation, make one small coherent change at a time. After each change, wait for Unity to reload/recompile and inspect the Console before continuing.

## Scene and asset safety

- Do not reorder or reserialize large scenes unnecessarily.
- Do not modify multiple scenes unless the feature actually requires it.
- Prefer scripts or reusable prefabs over duplicated objects embedded in scenes.
- Do not mass-convert scene objects to prefabs.
- Do not unpack prefabs without a strong reason.
- Avoid **Apply All Overrides** unless every prefab instance should receive the change.
- Never delete `.meta` files or regenerate GUIDs unnecessarily.
- Preserve serialized references and existing prefab links.
- Do not perform broad hierarchy reorganizations as incidental cleanup.

## Global objects

Do not create duplicate Players, Main Cameras, AudioListeners, EventSystems, GameManagers, input managers, or save systems unless the architecture explicitly requires them.

## Git conflicts

If a `.unity`, `.prefab`, or `.meta` file contains `<<<<<<<`, `=======`, or `>>>>>>>`, do not blindly choose one side. Inspect both versions and preserve intentional changes from both developers. If a safe semantic merge is not possible, stop modifying that file and report the conflict.

Before committing, inspect `git status`, review the diff, and exclude unrelated or generated changes. Save only scenes you intentionally edited.
