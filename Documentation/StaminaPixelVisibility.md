# Stamina HUD pixel visibility

- The HUD solid sprite used only a 1×1 region of Unity's 4×4 white texture. UVs ended at 0.25, so the pixel shader rounded both vertical edges down and collapsed the original 3-unit stamina strip.
- Use the full white texture for HUD solids and the shared UI white sprite. The stamina strip is now 8 units tall; SP text is 13 points and vertically centered.
- HUD bars resize a simple rectangle instead of cropping sprite UVs. Positive amounts retain a visible pixel cell; zero disables the fill and leaves the track.
- No stamina costs, regeneration, or combat behavior changed.

Verification: compilation passed. `Tools/VerifyStaminaPixel.cs` builds the actual HUD in an isolated overlay during Play, captures normal and pixel modes at 100%, 50%, 1%, and 0%, and checks layout, native text, and restored pixel preference. `.codex/stamina-pixel/native.json` contains 28 passing checks. Image analysis in `image-checks.json` confirms decreasing visible fill and no fill at zero in both modes. Current Play session remains running at time scale 0, Pixel Style enabled.

During the existing Play session, script recompilation also exposed null references in CombatImpactVisuals and NailongHealthBar. Full scene validation requires restarting Play; the isolated HUD captures were validated independently.
