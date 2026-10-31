# Visual Style System V2 — six styles (batch031)

Implemented in Unity 6000.3.23f1 / URP17.3.0. Batch030 replaces Origami with **Color Comic**, and the previous monochrome film effect with **B&W Manga**, at the user’s request. Batch031 additionally installs Ink Wash and Van Gogh without replacing the four existing choices; see [Painting-Batch031.md](Painting-Batch031.md). Realistic and Dark Fairytale remain. See [Comic-Batch030.md](Comic-Batch030.md) for current evidence and limits. Older V2/batch029 acceptance below remains historical where identified.

## Player controls

Open **Main Menu → Visual Style**, or **Settings → Graphics → Visual Style**. Select Realistic, Dark Fairytale, Color Comic, B&W Manga, Ink Wash or Van Gogh. The selected card, Graphics value and persisted preference share one manager. Style changes require no scene reload.

Style Intensity retains the existing40–100% range. Both comic modes expose Ink weight, Tone contrast, Screentone and Shadow ink. Normalized overrides are independent per style and saved separately. Their new Comic1 preference keys preserve old fold/film settings without treating them as new controls. Existing saved style IDs (`origami` / `monochrome`) and asset GUIDs remain compatible; these technical IDs are not displayed to players. B&W Manga always keeps the world grayscale, including at minimum intensity.

The gallery uses two rows of three cards. Ink Wash controls are Ink bleed, Brush texture, Paper grain and Mist; Van Gogh controls are Brush size, Swirl, Impasto and Color richness. Each new style saves its own normalized controls and selection.

COMPARE STYLES offers all six choices on both sides, including the same style on both sides (36 ordered pairs). The divider supports pointer dragging and Left/Right navigation. Both images retain the complete, identical framing under a reveal mask. Comparison uses stored screenshots and adds no scene cameras.

The existing Graphics Pixel Style option is independent. Its installed V2 implementation internally targets 320×180, despite the older Pixel360 class/option identifier; this task preserves that existing behavior. World rendering and non-text UI can be pixelated; UI text retains native resolution and normal colors.

## Rendering

- Realistic enqueues no color transform. Dark Fairytale keeps its original shader branch, depth/normal inputs, atmosphere and sky/water path.
- Comics and the new painting styles process the composed world after transparents and postprocessing, before overlay UI. They share the batch029 private complete-opaque-depth copy, now used by both styles; the camera SSAO prepass alone omits custom terrain/foliage. Shared URP camera depth remains untouched. No extra scene geometry pass, material conversion or silhouette rewrite.
- Color Comic uses depth/luminance ink contours, depth-guarded color smoothing, cel color bands, printed shadow dots and restrained warm/cool palette. A low-luminance floor retains night face/terrain information rather than collapsing it into the darkest band.
- B&W Manga combines paper-white/ink-black tone zones, contours, regular halftone dots and diagonal shadow hatching. It is not a plain luminance filter or film-grain effect. Transparent water, sky and effects participate in the composed transform. Overlay UI and dedicated UI-only cameras retain their original colors/text.
- Print cells are fixed to the rendered screen, antialiased and adjusted for the independent pixel target. They use no time/random-frame seed. This is screen-anchored print, not object-anchored texture; normal projection/wind/visibility still vary as the camera moves.
- Ink Wash adds depth-guarded wet ink, dry brush, paper grain and mist; Van Gogh adds local rotated color/depth-guarded brush dabs, blue/gold color and impasto ridges. Both use screen-anchored marks without time jitter.
- The two existing PC/Mobile renderer assets retain one feature each. One cached transform material and native CopyDepth material survive style switches. Comics add an R32 transient depth target and one fullscreen copy plus the color transform. Uniforms update on revision/quality/pixel changes. No new isolated GPU/FPS/GC claim; mobile-device/XR acceptance is outside batch030.

## Actual previews and UI

Current six1280×720 previews are native batch031 first-island renders, fixed noon, same frozen Sahur pose/framing, intensity89.7673%, existing preview GUIDs. All six styles at two views and both new painting styles at4×MSAA,320×180 pixel and midnight are recorded in AUTO_QA/Evidence/batch031. Source actors are save-excluded temporary clones. No authored scene was saved.

![Current six-card gallery](../AUTO_QA/Evidence/batch031/gallery-ui.png)

![Current Ink Wash / Van Gogh comparison](../AUTO_QA/Evidence/batch031/comparison-ui.png)

The original V2 and batch029 previews remain archived with their reports; they do not represent the current replacement effects.

## Current batch031 acceptance

142 native checks pass:47 rendering/preferences,79 UI and16 transparent-world. Six cards,36 comparison pairs, full sixth dropdown row and native selection pass. SameEditor WindowsMono buildSucceeded66.09s/493warnings/four known missing-plant messages; shader messages empty. Original state restored. See [Painting-Batch031.md](Painting-Batch031.md) and AUTO_QA/Evidence/batch031/proof.json; no new standalone FPS claim.

## Historical batch030 acceptance

77 native checks pass:40 rendering/control/cache/persistence/grass checks,27 actual gallery/comparison/UI checks and10 transparent-world checks. See [Comic-Batch030.md](Comic-Batch030.md) and AUTO_QA/Evidence/batch030/proof.json for final build/restoration and visual limitations. Native re-enable tests check saved controls; a fresh Play/domain-reload persistence test was not repeated in this batch.

## Historical original V2 acceptance

**141 scripted checks passed:** 93 gallery/settings/persistence/comparison/independent-resolution/enemy-animation/profile checks plus 48 native-input/rendering checks. Full check names and raw timings are in verification.json and rendering-verification.json under AUTO_QA/Evidence/visual-style-v2.

- All four styles, twenty same-frame selections, unknown IDs, intensity clamps, original scene material IDs and actor transforms; fresh Play restores saved Origami/intensity.
- Actual authored Main Menu integration and Graphics entry; four cards, selected/hover state, runtime details, separate per-style saved controls; all sixteen comparison pairs, real pointer divider events, fixed image alignment and no extra camera.
- Real queued keyboard/mouse movement and melee for all four styles. Each style switch occurs during an active action and preserves the exact Animator state, normalized time and actor position in that frame. Six actual Chicken Bro skeletons advance through live rendering profiles.
- Eight native/pixel style combinations. Whole-world Monochrome pixels, including the cyan transparent probe, have equal RGB channels. Dark/Origami preserve the foreground transparent probe. Actual combat pressure rings contribute visible pixels in both added styles using reversible renderer off/on controls and stable restored frames.
- Fold and contrast controls change real rendered pixels, verifying cached material updates. The installed Mobile forward renderer renders all three stylized modes and remains gray in Monochrome; this is desktop execution of that renderer, not a mobile-device benchmark.
- An additional actual script-reload probe preserves Monochrome 85%, four working cards, open gallery, one manager and one gallery. A native dropdown pointer event selects Dark Fairytale. Native/pixel overlay captures were visually inspected.
- Final source recompile is up to date. The visual transform shader has no compiler messages. Ocean/Sky have no shader errors but emit three warnings in unchanged SailingFoam, CloudDensity and ShadowDensity helpers. No model/rig/UV/animation/collider/prefab/scene asset edits. Original Realistic/Dark preset tuning is unchanged; their serialized schema only gains V2 fields and their previews are regenerated under the same GUIDs.

## Historical original V2 performance findings

| Style | CPU frame median (ms) | GPU frame median (ms) | Rendered samples |
|---|---:|---:|---:|
| realistic | 17.751 | 15.845 | 60 |
| dark-fairytale | 17.671 | 15.813 | 60 |
| origami | 17.268 | 16.049 | 60 |
| monochrome | 17.114 | 15.901 | 60 |

These numbers belong to the original Origami/film effects, **not the current comic shaders**. Measured through FrameTimingManager on RTX 4060 / Direct3D12, 960×540, renderScale 1, with six active Chicken Bros, 60 actual camera renders per style. Editor frame medians were approximately 17.1–17.2 ms. These include Editor pacing, display synchronization, AI variance and the rest of the scene. They establish that this fixture rendered all modes successfully; small differences are **not isolated shader costs or a claimed FPS improvement**. Full first-island standalone/low-end/1080p performance and long moving-camera temporal stability still need a dedicated benchmark. No zero-allocation or memory-leak claim is made.

## Installation, safety and remaining work

Assets are already installed and referenced; no manual setup is required. For a missing V2 asset, Dark Brine/Visual Styles/Install or repair V2 assets creates only missing profiles/presets and validates one existing feature per renderer. Assign V2 matching previews reimports and assigns the four real captures.

No geometry, UVs, rigs, controllers, clips, colliders, models, authored scenes, weather ownership or gameplay logic changed. Existing source volumes/materials remain recoverable and no per-object material swap is performed. Fresh save and backup bytes match their pre-task copies. Original selected style/intensity, input settings object/flags/current devices, device enabled states, pipeline and five clean stopped scenes were restored.

Known project issue: 606 plant instances still reference three unavailable prefab GUIDs. Their objects were preserved. This limits scene completeness but does not originate in this feature. No user action is required to use the styles. Device/standalone performance qualification and broader boss/timing/long-motion visual review remain subsequent QA work; the static capture and native state tests do not establish smoother animation or every encounter's visibility.

## Changed file manifest

See the exact task manifest appended below. Unity-generated .meta companions are included. Verification tools stay outside Assets; generated build caches and ignored diagnostics are excluded from checkpoints.

- Created: `Assets/Editor/VisualStyleSetupV2.cs`
- Created: `Assets/Editor/VisualStyleSetupV2.cs.meta`
- Modified: `Assets/Ocean/ProceduralOcean.shader`
- Modified: `Assets/Ocean/ProceduralSky.shader`
- Modified: `Assets/Resources/VisualStyles/Presets/dark-fairytale.asset`
- Created: `Assets/Resources/VisualStyles/Presets/monochrome.asset`
- Created: `Assets/Resources/VisualStyles/Presets/monochrome.asset.meta`
- Created: `Assets/Resources/VisualStyles/Presets/origami.asset`
- Created: `Assets/Resources/VisualStyles/Presets/origami.asset.meta`
- Modified: `Assets/Resources/VisualStyles/Presets/realistic.asset`
- Modified: `Assets/Resources/VisualStyles/Previews/dark-fairytale.png`
- Created: `Assets/Resources/VisualStyles/Previews/monochrome.png`
- Created: `Assets/Resources/VisualStyles/Previews/monochrome.png.meta`
- Created: `Assets/Resources/VisualStyles/Previews/origami.png`
- Created: `Assets/Resources/VisualStyles/Previews/origami.png.meta`
- Modified: `Assets/Resources/VisualStyles/Previews/realistic.png`
- Created: `Assets/Resources/VisualStyles/Profiles.meta`
- Created: `Assets/Resources/VisualStyles/Profiles/Paper.asset`
- Created: `Assets/Resources/VisualStyles/Profiles/Paper.asset.meta`
- Created: `Assets/Scripts/VisualStyles/PaperMaterialProfile.cs`
- Created: `Assets/Scripts/VisualStyles/PaperMaterialProfile.cs.meta`
- Modified: `Assets/Scripts/VisualStyles/VisualStyleComparisonController.cs`
- Modified: `Assets/Scripts/VisualStyles/VisualStyleManager.cs`
- Modified: `Assets/Scripts/VisualStyles/VisualStylePreset.cs`
- Modified: `Assets/Scripts/VisualStyles/VisualStyleRendererFeature.cs`
- Modified: `Assets/Scripts/VisualStyles/VisualStyleSettings.cs`
- Modified: `Assets/Scripts/VisualStyles/VisualStyleUIController.cs`
- Modified: `Assets/Shaders/DarkFairytale.shader`
- Modified: `Assets/Shaders/EnvironmentVisualStyle.hlsl`
- Modified: `AUTO_QA/ANIMATION_REVIEW.md`
- Modified: `AUTO_QA/BUG_REPORT.md`
- Created: `AUTO_QA/Evidence/visual-style-v2/comparison-dropdown-native.png`
- Created: `AUTO_QA/Evidence/visual-style-v2/comparison-ui.png`
- Created: `AUTO_QA/Evidence/visual-style-v2/file-manifest.json`
- Created: `AUTO_QA/Evidence/visual-style-v2/film-contrast-high.png`
- Created: `AUTO_QA/Evidence/visual-style-v2/film-contrast-low.png`
- Created: `AUTO_QA/Evidence/visual-style-v2/final-native-state.json`
- Created: `AUTO_QA/Evidence/visual-style-v2/gallery-native.png`
- Created: `AUTO_QA/Evidence/visual-style-v2/main-menu.png`
- Created: `AUTO_QA/Evidence/visual-style-v2/mobile-forward-monochrome.png`
- Created: `AUTO_QA/Evidence/visual-style-v2/mobile-forward-origami.png`
- Created: `AUTO_QA/Evidence/visual-style-v2/native-dark-fairytale.png`
- Created: `AUTO_QA/Evidence/visual-style-v2/native-monochrome.png`
- Created: `AUTO_QA/Evidence/visual-style-v2/native-origami.png`
- Created: `AUTO_QA/Evidence/visual-style-v2/native-realistic.png`
- Created: `AUTO_QA/Evidence/visual-style-v2/overlay-native-color-proof.png`
- Created: `AUTO_QA/Evidence/visual-style-v2/overlay-pixel-color-proof.png`
- Created: `AUTO_QA/Evidence/visual-style-v2/overlay-verification.json`
- Created: `AUTO_QA/Evidence/visual-style-v2/paper-fold-full.png`
- Created: `AUTO_QA/Evidence/visual-style-v2/paper-fold-zero.png`
- Created: `AUTO_QA/Evidence/visual-style-v2/pixel360-monochrome.png`
- Created: `AUTO_QA/Evidence/visual-style-v2/pixel360-origami.png`
- Created: `AUTO_QA/Evidence/visual-style-v2/pressure-off-monochrome.png`
- Created: `AUTO_QA/Evidence/visual-style-v2/pressure-off-origami.png`
- Created: `AUTO_QA/Evidence/visual-style-v2/pressure-on-monochrome.png`
- Created: `AUTO_QA/Evidence/visual-style-v2/pressure-on-origami.png`
- Created: `AUTO_QA/Evidence/visual-style-v2/preview-provenance.json`
- Created: `AUTO_QA/Evidence/visual-style-v2/rendering-verification.json`
- Created: `AUTO_QA/Evidence/visual-style-v2/safety.json`
- Created: `AUTO_QA/Evidence/visual-style-v2/verification.json`
- Modified: `AUTO_QA/NEXT_TASK.md`
- Modified: `AUTO_QA/PROGRESS.md`
- Modified: `AUTO_QA/TEST_RESULTS.md`
- Created: `Documentation/VisualStyleSystem-V2.md`
- Created: `Tools/CaptureVisualStylesV2.cs`
- Created: `Tools/VerifyVisualStyleRenderingV2.cs`
- Created: `Tools/VerifyVisualStylesV2.cs`
- Created: `Tools/VisualStyleV2OverlayProbe.cs`
- Created: `Tools/VisualStyleV2Workspace.cs`

Checkpoint scope note: Unity-generated .meta empty YAML fields retain their native trailing spaces; source whitespace checks pass with .meta excluded. Plastic also detects a line-ending-only EditorBuildSettings.asset delta: its normalized content equals the server and Git HEAD, so it is preserved and excluded along with ShaderGraphSettings.asset and the two recovery entries.
