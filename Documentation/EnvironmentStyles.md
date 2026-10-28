# Cloud and sea style synchronization

Clouds and ocean draw after the opaque illustration pass. Keep that pass before transparents to preserve readable combat effects; apply the selected palette inside the two environment shaders instead.

- `VisualStyleManager` supplies runtime global intensity, saturation, bands, shadow/highlight and ink colors, resetting the active flag on teardown. Weather/day-night material inputs stay authoritative.
- `EnvironmentVisualStyle.hlsl` supplies tonal grouping, muted storybook colors, shaded ink and stable grain. Realistic intensity is zero and takes the original shader path.
- Ocean reduces specular glints in Dark Fairytale, groups foam, and shades existing crests/contact lines. Waves, depth, hull masking, wake calculations and buoyancy are unchanged.
- Pixel Style renders clouds in the same coarse world buffer, caps volume integration at eight steps and groups alpha/color into broad pixel clusters. It adds no texture, render target, camera or full-screen pass.

Verification: C# compilation and both shader error checks passed. `Tools/VerifyEnvironmentStyles.cs` uses the actual sky/ocean shaders on temporary geometry and the existing URP renderer/post-processing path, at 960×540 output. Eleven checks cover clouds and sea in normal/pixel modes, intensity changes, exact visual restoration to Realistic and night palette behavior. Saved style preferences and time scale are unchanged; temporary objects are removed.

`Tools/AnalyzeEnvironmentStyles.py` confirms zero mixed blocks across 57,600 3×3 blocks per pixel capture, for both styles. Captures and JSON reports: `.codex/environment-styles/`. In `comparison.png`, left is Realistic, right is Dark Fairytale; the lower row enables Pixel Style. Tests are isolated native render requests, not a full scene playthrough. Initial probe cameras with post-processing disabled used URP's linear final blit; final captures enable post-processing to match the actual Main Camera and verify its point upscaling path.
