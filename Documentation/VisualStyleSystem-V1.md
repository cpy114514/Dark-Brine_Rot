# Universal visual styles V1

Current update: the independent resolution option is now named **Pixel Style**, uses a coarser world grid and also pixelates non-text UI. See [PixelStyle-v2.md](PixelStyle-v2.md). V1's original measurements and implementation history below are retained.

Implemented and verified on 2026-10-27 with Unity 6000.3.23f1, URP 17.3.0 and the existing RenderGraph configuration. No package dependencies were added.

## Player entry points

- Main Menu → **Visual Style**. The existing New Game, Continue, Settings and Quit actions remain; the vertical stack gains one entry.
- Settings → Graphics → **Visual Style**, immediately after the existing Pixel 360p row.
- Both open the same gallery, use the same manager and immediately save the same selection. Other graphics options retain their existing Apply/draft behavior.
- Realistic and Dark Fairytale cards include actual-scene images, descriptions, selection states and hover feedback. **Compare Styles** opens an aligned, draggable split image; mouse dragging and the existing UI slider navigation are supported. Escape returns from comparison/gallery without also closing the parent settings menu.
- Style Intensity ranges from 40–100% and is enabled for illustrated styles. Static reference images use 85%, identified in the interface. Resolution remains a separate setting.

No manual Unity setup steps remain for the installed V1 assets. The system starts automatically in Play/builds; no scene edits are needed to install its manager or UI.

## Rendering and architecture

`VisualStyleManager` loads resource-backed `VisualStylePreset` ScriptableObjects once, restores stable preset IDs before the first scene, survives scene changes and owns style/intensity events. `VisualStyleSettings` stores only `DarkBrine.VisualStyle.Id` and `.Intensity`; campaign progress and existing graphics preferences are separate. Selection does not reload scenes, recreate actors or modify time scale.

**Realistic** preserves existing PBR materials, scene lighting, volumes, SSAO and player quality choices. It adds no illustration pass or normal prepass. It does not silently force a higher graphics quality or change resolution.

**Dark Fairytale** adds one RenderGraph raster pass, one transient camera-color destination and no copy-back pass. It requests depth/normals, detects relative-depth and normal edges, fades distant detail, introduces restrained static ink variation, soft log-luminance bands, muted color, cool shadows, parchment highlights, fine fixed paper grain and subtle shadow hatching. Outline width follows the actual render dimensions; 360p uses one scene pixel and lower-frequency paper/hatching. There is no animated noise seed or temporal feedback texture.

The pass runs **before transparents**, so transparent ocean, pressure waves, trails and particles keep their existing renderers and do not produce their own depth/normal ink outlines. Native overlay UI renders afterward. Existing water/cloud shaders are not replaced. The manager applies reversible fog/ambient tint from the current active scene's baseline; `EnvironmentChanged` is the extension point for future ocean, sky, cloud or atmospheric adapters.

Both PC and Mobile renderer data contain exactly one Universal Visual Style feature. Existing PC SSAO remains. Renderer feature recovery maps are populated with the proper native subasset IDs. Unity migrated the older Mobile renderer serialization from version 2 to 3 without changing its existing render mode/stencil settings. Original renderer bytes are retained under `.codex/visual-style-v1/renderer-before/`.

Presets remain immutable during gameplay. No original material/shader asset is swapped or rewritten, no per-object style update loop is installed, and the illustration material is cached per renderer feature. Additional presets can reuse the data schema and stable IDs; new effect families can extend the renderer/environment event. V1's gallery composition is tuned for the initial two styles.

## Actual scene previews and regeneration

`Assets/Resources/VisualStyles/Previews/realistic.png` and `dark-fairytale.png` are 1600×900 captures of the actual First Island **Environment** and **Lighting** scenes, with Sahur and an existing enemy prefab. Ocean and original vegetation are visible. The two captures freeze the same world time, model transforms, camera and baseline lighting; only style-specific rendering changes. No generated concept artwork is used. Textures import without mipmaps, compression or power-of-two rescaling, so the comparison retains exact image alignment.

`VisualStyleComparisonController` reveals the left Realistic image over the right Dark Fairytale image with a rectangular mask. It preserves the full image width when moving the divider. Neither the gallery nor the comparison creates another game camera or renders a second world per frame.

To regenerate after adjusting preset parameters, use the existing Unity CLI:

1. With a stopped Editor and saved workspace, run `unity command run_script --file Tools/CaptureVisualStylesV1.cs --entry CaptureVisualStylesV1.Prepare`. This records the existing scene setup and temporarily opens the real Environment/Lighting scenes additively.
2. Enter Play with `unity command editor_play`, then run the same file with entry `CaptureVisualStylesV1.Start`. It writes a matching frozen pair, configures their importers/asset references and stops its test Play session.
3. Run entry `CaptureVisualStylesV1.Restore` to restore the recorded workspace. Source scenes are not saved by this workflow.

The Editor menu **Dark Brine → Visual Styles** can repair/install preset/feature assets and assign regenerated previews. Repair preserves existing preset tuning and is idempotent. Full camera/time/actor provenance is in `.codex/visual-style-v1/preview-provenance.json`.

## Verification performed

81 checks pass across the final reports:

| Evidence | Result |
| --- | --- |
| `verification.json` | 57 checks: repeated switching, original material references and actor state, safe intensity bounds, preference restore, actual Main Menu/Graphics controls, hover, selection synchronization, native overlay UI, real pointer divider drag/alignment, bounded divider, four independent resolution/style combinations, and six active Chicken Bros |
| `rendering-verification.json` | 16 checks: a new Play session loads the saved style/intensity; actual queued keyboard/mouse movement and melee work under both styles; native and 360p captures differ by style; transparent foreground stays readable without ink outlines; existing production pressure effects remain active; Forward/Mobile renderer produces an illustrated frame on the desktop backend |
| `motion-verification.json` | 8 checks: Cappuccino Assassino, Nailong and Bombardino retain their original live animator/controller and collider references under both styles; consecutive frozen native frames have zero mean byte difference; 16 native orbit frames are captured and visually reviewed |

Native UI screenshots are `main-menu.png`, `gallery.png` and `comparison-ui.png`. The first UI review found an overlong divider; its final handle sizing and image clipping are corrected and rechecked. Four rendering combinations are shown in `rendering-combinations.jpg`; the orbit contact sheet is `camera-motion-review.jpg`. Earlier incomplete importer/first-pass evidence is retained separately and is not final acceptance.

Protected-file SHA256 comparisons cover Sahur's source FBX, current jump clips, attack/movement scripts, boss progression and original Environment/MainMenu scenes. All match the pre-validation snapshots. Source changes for this feature are confined to the rendering/UI files listed below; no model, UV, rig, animation, collision, AI, combat or progression source is authored or replaced.

These are bounded native tests and short Boss smoke checks, not complete campaign/boss playthroughs. The mobile renderer path was exercised on desktop; actual mobile hardware, a shipping build and long live-island/camera stability runs remain unmeasured. Existing missing vegetation-prefab references in the island scene were observed and left outside this feature's scope. The final graphics/render checks introduced no new shader or graphics exceptions.

## Performance findings

Final sample: NVIDIA GeForce RTX 4060, Direct3D12, Editor CombatTest, six live Chicken Bro prefabs, 1920×1080 viewport, **0.6 scene render scale**, 12 warm-up frames and 60 measured frames per style. FrameTimingManager supplied CPU and GPU timings.

| Style | Median GPU frame | Median CPU frame | Observed Editor FPS |
| --- | ---: | ---: | ---: |
| Realistic | 18.56 ms | 19.22 ms | 52.8 |
| Dark Fairytale | 19.81 ms | 20.42 ms | 50.1 |

The measured GPU difference is approximately 1.26 ms / 6.8% in this short sequential Editor sample. It includes normal/depth requirements and is not an isolated shader microbenchmark or a standalone/1080p-native performance guarantee. Existing render scale/360p controls remain the main scene-resolution quality choices. No frequent material instantiation or per-object CPU stylization loop is added.

## Files created and modified

Created runtime files in `Assets/Scripts/VisualStyles/`:

- `VisualStylePreset.cs`
- `VisualStyleSettings.cs`
- `VisualStyleManager.cs`
- `VisualStyleRendererFeature.cs`
- `VisualStyleUIController.cs`
- `VisualStyleComparisonController.cs`
- `VisualStyleCardHover.cs`

Created assets/tooling:

- `Assets/Shaders/DarkFairytale.shader`
- `Assets/Editor/VisualStyleSetup.cs`
- `Assets/Resources/VisualStyles/Presets/realistic.asset`
- `Assets/Resources/VisualStyles/Presets/dark-fairytale.asset`
- `Assets/Resources/VisualStyles/Previews/realistic.png`
- `Assets/Resources/VisualStyles/Previews/dark-fairytale.png`
- `Tools/InspectVisualStylesV1.cs`
- `Tools/CaptureVisualStylesV1.cs`
- `Tools/VerifyVisualStylesV1.cs`
- `Tools/VerifyVisualStyleRenderingV1.cs`
- `Tools/ReviewVisualStyleMotionV1.cs`
- `Documentation/VisualStyleSystem-V1.md`

Modified:

- `Assets/Game/Prefabs/UI/MainMenu/Scripts/MainMenuController.cs` — installs one entry after the existing layout.
- `Assets/Game/Prefabs/UI/SettingsMenu/Scripts/PauseSettingsMenu.cs` — installs one row, opens the shared gallery, refreshes its value and protects parent Escape handling.
- `Assets/Settings/PC_Renderer.asset` — appends the feature and recovery map while retaining SSAO.
- `Assets/Settings/Mobile_Renderer.asset` — appends the feature/recovery map and native serialization migration.

New Unity files/folders also have their generated `.meta` sidecars. `.codex/visual-style-v1/file-manifest.json` enumerates the exact paths, including metadata. `.codex` screenshots, provenance, backups and reports are QA artifacts rather than shipped runtime assets.

## Reversibility

Selecting Realistic restores the current scene's captured fog/ambient baseline and bypasses the illustration pass. To disable the feature entirely, select Realistic, deactivate Universal Visual Style in both renderer assets and remove the two UI installation hooks/manager bootstrap if uninstalling. Original renderer snapshots are available for comparison/recovery; do not replace unrelated newer renderer edits blindly. All original model/material/animation assets remain available. No manual integration steps are pending for the implemented V1.
