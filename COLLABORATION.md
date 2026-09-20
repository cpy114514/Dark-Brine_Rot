# Working Together in Unity

The world is split into additive scenes so teammates can edit different parts of the game without constantly conflicting in one large scene file.

## Pick the right scene

- **Main**: loader only; avoid normal editing here.
- **Environment**: island, ocean, terrain, trees, rocks, buildings, and props.
- **Gameplay**: player, camera, UI, spawn/checkpoint objects, triggers, and level logic.
- **Lighting**: lights, sky, fog, volumes, and probes.
- **Enemies**: enemies, spawn points, and encounters.
- **Bosses**: boss-specific content when dedicated boss scenes exist.

Open the complete world with **Tools > Scenes > Open Full Workspace**. Smaller Environment and Gameplay workspace commands are available in the same menu.

Save only the scene you own: in the Hierarchy, confirm the object sits under the correct scene heading, then save that scene instead of using broad cleanup or reserialization tools. Before committing, run `git status` and review the diff so unrelated scenes and generated files are not included.

Coordinate before editing the same `.unity` file. Reusable objects should be prefabs when that genuinely reduces duplication, but do not mass-convert objects or apply overrides to every instance casually.

If a scene conflict occurs, stop and inspect both sides. Never resolve Unity YAML by blindly choosing “ours” or “theirs”; preserve both developers' intentional objects and references, or ask for help when a safe merge is unclear.
