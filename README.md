# Dark Brine_Rot

A third-person action game starring Tung Tung Tung Sahur, made for Hackclub Thirdspace. Start with the sailing prologue, survive the shipwreck, reach the first island and fight its bosses.

## Open and run

Use **Unity 6.3 LTS, 6000.3.23f1** and let Package Manager finish resolving the dependencies in `Packages/manifest.json` and `Packages/packages-lock.json`.

Open `Assets/Scenes/Main Menu/MainMenu.unity` and press Play for the complete game flow. **New Game** starts the prologue; **Continue** loads saved first-island progress when a valid save exists.

For island editing, use **Tools > Scenes > Open Full Workspace**. The island's Main scene loads Environment, Gameplay, Lighting and Enemies additively. Keep all seven enabled scenes in Build Settings; MainMenu must remain first.

Windows builds use **Windows 64-bit** with Development Build disabled. Distribute the complete output folder, including the `.exe`, `_Data` folder and Unity runtime files.

## Default controls

| Action | Input |
| --- | --- |
| Move / look | WASD / mouse |
| Sprint / jump / dodge | Left Shift / Space / Left Ctrl |
| Light attack / three-hit combo | Left mouse button; repeat to continue the combo |
| Charged attack | Hold right mouse button, then release |
| Throw the stick | E |
| Lock on | Middle mouse button |
| Heal | H |
| Equipment | I; use the mouse to browse, equip and close |
| Stick / collected Capri twin blades | 1 / 2 |
| Activate equipped weakness glasses | G |
| Map / pause and settings | M / Esc |
| Pick up the washed-up stick / toggle wreck-board surfing | F, when the contextual prompt appears |

Movement, sprint, jump, dodge and heal keys can be rebound in Settings. English and Simplified Chinese are available there.

## Equipment and bosses

- Capri's twin blades unlock after defeating Capri and collecting the dropped weapon.
- Nailong drops weakness glasses. Equip them in the head category, close the equipment panel, then press G. Weak points are revealed for 30 seconds; the cooldown is 15 minutes. Hitting a physical weak point deals 1.5× damage.
- Bombardino Crocodilo keeps circling while alive, with attack starts five seconds apart and no landing/cooling phase. Ground outlines announce the bombing and flight attack areas.
- Continue restores player position, health, stamina, arrival progress, healing supplies, weakness-glasses timers and owned/selected twin blades. Legacy saves remain readable.

## Submission

See `Documentation/Submission-20261011.md` for the final verification record and handoff instructions. An engineering handoff needs `Assets`, `Packages`, `ProjectSettings`, this README and the existing source/attribution documentation. Keep `.meta` files with their assets. `Library`, `Temp`, `Logs`, generated IDE files, local version-control metadata and repair backups are local development data.

The project declares MIT licensing. Third-party assets retain the licenses and attribution files included with them.

## AI usage

AI was used during development.

## Project layout

- `Assets/Game/Prefabs/`: characters, bosses, environment and UI assets.
- `Assets/Scripts/`: gameplay logic grouped by story, ships, systems, combat and performance.
- `Assets/Scenes/`: title menu, first story and additive first-island scenes.
- `ArtSource/`: editable ship fracture and shark animation sources.
- `Tools/`: development, import and verification scripts outside the game compilation.
- `COLLABORATION.md`: scene ownership and the full-workspace editor commands.

Local repair backups and generated previews are excluded from the game build; keep them outside the engineering handoff.
