# Pixel Style revision — 2026-10-27

User requests stronger pixelation, an ultra-light style label without an internal resolution number, and consistent pixel rendering for everything except text.

The existing Graphics option is now **PIXEL STYLE**, described as **Ultra-light rendering with bold pixels; text stays sharp**. Its enabled value is **ON / ULTRA LIGHT**. The render-scale override reads **PIXEL STYLE**. No internal resolution number is shown by this option. Its existing `DarkBrine.Settings.Pixel360` preference, option key and public helper type remain to preserve saved settings and callers. Realistic and Dark Fairytale still combine independently with it.

The internal world target changes from 360 to 180 lines with point upscaling. At ordinary 16:9 output this reduces the old pixel preset's world raster area by 75%. This is a render-work reduction, not a measured FPS guarantee. Camera projection, display resolution, input coordinates and gameplay remain unchanged. The existing URP minimum render scale still applies at very large output sizes; the UI grid follows the same clamp.

`PixelStyleUI` extends the existing mode rather than adding another resolution system. It temporarily redirects uGUI's shared default to one cached runtime pixel material, preserves explicitly assigned UI/Default variants, and restores the original references when disabled. Original builtin/material assets are not modified. The shader samples imagery, color gradients and clipping on the scene-aligned screen grid and snaps visual vertices, while retaining the original UI rects/raycast targets. No duplicate UI camera or full-resolution UI render target is introduced. Canvas discovery is periodic; material and scene discovery are not repeated per frame.

The authored main-menu artwork shader shares the same conditional pixel helper, preserving its color adjustments, hover texture registration, original materials and hit areas. Photos, gallery previews, icons, bars and menu backgrounds therefore share the coarse appearance. Legacy and TMP text retain their native shaders; existing world-caption mirroring remains. Central text factories explicitly retain the native legacy-text material. The effect is reversible, including newly assigned explicit UI materials.

Changed/new implementation files:

- `Assets/Scripts/Performance/Pixel360Rendering.cs` — stronger world target, UI-service ownership and cleanup.
- `Assets/Scripts/Performance/PixelStyleUI.cs` — new non-text UI integration and material restoration.
- `Assets/Resources/Rendering/PixelStyleUI.shader` — resource-retained pixel uGUI shader with stencil/clipping support.
- `Assets/Shaders/PixelStyleUI.cginc` — shared display-grid, vertex and sample helpers.
- `Assets/MainScene/HandDrawnContrast.shader` — conditional pixel path for authored artwork.
- `Assets/Scripts/Performance/Pixel360WorldText.cs` — native material for mirrored captions.
- `Assets/Game/Prefabs/UI/GameUITheme.cs` — native legacy-text factory path.
- `Assets/Scripts/VisualStyles/VisualStyleUIController.cs` — native text and updated Pixel Style wording.
- `Assets/Game/Prefabs/UI/SettingsMenu/Scripts/PauseSettingsMenu.cs` — renamed option, ultra-light description and nonnumeric values.
- `Tools/VerifyPixelStyleV2.cs`, `Tools/AnalyzePixelStyleV2.py` — native runtime and captured-pixel validation.

Validation on the installed Unity 6000.3.23f1 / URP 17.3.0:

- 27 native checks pass: world/UI activation, original artwork references, legacy/floating native text, real UI raycasts/clicks, value-label fit, hidden internal resolution, output scaling, center aim, both visual styles and exact disable/restoration behavior.
- Native 1920×1080 captures have 57,600 uniform 6×6 world blocks, with zero mixed blocks.
- Screen-overlay photo and authored-artwork crops have respectively 5,301 and 3,162 uniform blocks, with zero mixed blocks. The corresponding normal photo crop has all 5,301 blocks mixed.
- The native text crop is byte-identical between normal and Pixel Style screenshots.
- Both shaders compile without messages; no new runtime/render errors occurred during validation. User preferences and the pre-existing unsaved CombatTest workspace were preserved; no scene was saved or reloaded.

Evidence: `.codex/pixel-style-v2/native.json`, `pixel-analysis.json`, `acceptance.json`, `normal.png`, `pixel-style.png` and `world-pixel.png`. These supersede the historical v260 pixel-mode captures. No standalone performance benchmark was run for this revision.
