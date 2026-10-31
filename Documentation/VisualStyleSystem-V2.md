# Visual Style System V2

Implemented in Unity 6000.3.23f1 / URP 17.3.0. Extends the installed V1 feature and manager; no replacement gameplay or per-object material conversion.

## Player controls

Open **Main Menu → Visual Style**, or **Settings → Graphics → Visual Style**. Select Realistic, Dark Fairytale, Origami or Monochrome. The selected card, Graphics value and persisted preference share one manager. Style changes require no scene reload.

Style Intensity retains the existing 40–100% range. Origami DETAILS exposes Paper texture, Fold shading, Matte surface and Paper edges. Monochrome DETAILS exposes Contrast, Film grain, Vignette and Shadow depth. Four normalized overrides are saved separately per style. Preset assets remain defaults and are not modified by the player. Bloom and dithering are optional preset parameters. Monochrome intensity adjusts its tonal curve while keeping the world gray.

COMPARE STYLES offers Left Style and Right Style, including the same style on both sides. The divider supports pointer dragging and Left/Right navigation. Both images retain the complete, identical framing under a reveal mask. Comparison uses stored screenshots and adds no scene cameras.

The existing Graphics Pixel Style option is independent. Its installed V2 implementation internally targets 320×180, despite the older Pixel360 class/option identifier; this task preserves that existing behavior. World rendering and non-text UI can be pixelated; UI text retains native resolution and normal colors.

## Rendering

- Realistic continues to enqueue no visual-style color pass. The existing Dark Fairytale opaque shader branch, tuning and environment path are preserved.
- Origami uses world-position reconstruction, triangular world-projected paper cells, depth-guarded texture flattening, banded fold lighting, antialiased creases, footprint-faded fibers, matte tones, warm/sage color response and adjustable soft edges. Batch029 fixes incomplete depth coverage of custom terrain/foliage; see Origami-Batch029.md. It changes surface shading without changing silhouettes or geometry. The separate Paper Material Profile supplies reusable defaults, fold scale and normal quantization.
- Existing ocean geometry, waves, foam and physics remain intact. An Origami-only fragment branch simplifies water bands/glints; sky/cloud branches add restrained layered shading. Switching away restores the original paths through runtime globals.
- Monochrome runs after world postprocessing and transparents, using Rec.709 luminance, contrast/toe/shoulder response, adjustable shadow depth, fine stable grain, vignette, optional highlight bloom and dithering. Overlay UI is composited afterward. Dedicated UI-only cameras bypass the feature.
- Both installed PC and Mobile renderers retain their single existing feature. Each stylized camera uses one transient RenderGraph color destination and one color transform. Dark requests camera depth and normals. Origami requests depth and copies the actual opaque depth once into a transient R32 texture because custom terrain/foliage are omitted from the SSAO depth-normal prepass; its normals are reconstructed from world-position derivatives. The copy does not replace URP shared depth and adds no scene geometry pass. There is no duplicated model or material per renderer.
- Presets and one feature material are cached. Uniforms update when manager/preset/profile revision, quality or pixel state changes. Lowest quality reduces paper fibers and disables the optional four-tap film highlight bloom. Pixel rendering reduces fine grain and edge width.
- Manager/UI OnEnable reconstructs transient managed bindings after Editor script reload without duplicating the manager/gallery. Original fog/ambient baselines are retained; user-selected styles and detail preferences survive a new Play session.

## Actual previews and UI

The current four 1280×720 PNGs in Assets/Resources/VisualStyles/Previews are actual Unity batch029 renders of the same first-island Environment + Lighting setup, fixed noon, same camera and excluded Sahur pose, at intensity 89.7673%. Source GUIDs are retained. No authored scene was saved. See AUTO_QA/Evidence/batch029/gallery-final.json and Documentation/Origami-Batch029.md. The original V2 1600×900 / 85% Sahur/Nailong previews and their historical provenance remain in AUTO_QA/Evidence/visual-style-v2; the screenshots below show that original acceptance, not the current revised Origami.

![Native four-card gallery](../AUTO_QA/Evidence/visual-style-v2/gallery-native.png)

![Arbitrary aligned comparison and dropdown](../AUTO_QA/Evidence/visual-style-v2/comparison-dropdown-native.png)

The gallery shows planar facets/creases on the original character surfaces and layered turquoise water in Origami; Monochrome retains character and water detail with controlled gray tones. Native and pixel composited overlay captures retain a red proof indicator and crisp native text. The proof indicator belongs only to the external verification fixture and is not shipped in gameplay.

An early camera-space UI capture method produced misleading black or blurred UI and was rejected. Acceptance uses the actual composited Game view. The final verification helper now uses that method too; earlier camera-space images are excluded.

## Executed acceptance

**141 scripted checks passed:** 93 gallery/settings/persistence/comparison/independent-resolution/enemy-animation/profile checks plus 48 native-input/rendering checks. Full check names and raw timings are in verification.json and rendering-verification.json under AUTO_QA/Evidence/visual-style-v2.

- All four styles, twenty same-frame selections, unknown IDs, intensity clamps, original scene material IDs and actor transforms; fresh Play restores saved Origami/intensity.
- Actual authored Main Menu integration and Graphics entry; four cards, selected/hover state, runtime details, separate per-style saved controls; all sixteen comparison pairs, real pointer divider events, fixed image alignment and no extra camera.
- Real queued keyboard/mouse movement and melee for all four styles. Each style switch occurs during an active action and preserves the exact Animator state, normalized time and actor position in that frame. Six actual Chicken Bro skeletons advance through live rendering profiles.
- Eight native/pixel style combinations. Whole-world Monochrome pixels, including the cyan transparent probe, have equal RGB channels. Dark/Origami preserve the foreground transparent probe. Actual combat pressure rings contribute visible pixels in both added styles using reversible renderer off/on controls and stable restored frames.
- Fold and contrast controls change real rendered pixels, verifying cached material updates. The installed Mobile forward renderer renders all three stylized modes and remains gray in Monochrome; this is desktop execution of that renderer, not a mobile-device benchmark.
- An additional actual script-reload probe preserves Monochrome 85%, four working cards, open gallery, one manager and one gallery. A native dropdown pointer event selects Dark Fairytale. Native/pixel overlay captures were visually inspected.
- Final source recompile is up to date. The visual transform shader has no compiler messages. Ocean/Sky have no shader errors but emit three warnings in unchanged SailingFoam, CloudDensity and ShadowDensity helpers. No model/rig/UV/animation/collider/prefab/scene asset edits. Original Realistic/Dark preset tuning is unchanged; their serialized schema only gains V2 fields and their previews are regenerated under the same GUIDs.

## Performance findings

| Style | CPU frame median (ms) | GPU frame median (ms) | Rendered samples |
|---|---:|---:|---:|
| realistic | 17.751 | 15.845 | 60 |
| dark-fairytale | 17.671 | 15.813 | 60 |
| origami | 17.268 | 16.049 | 60 |
| monochrome | 17.114 | 15.901 | 60 |

Measured through FrameTimingManager on RTX 4060 / Direct3D12, 960×540, renderScale 1, with six active Chicken Bros, 60 actual camera renders per style. Editor frame medians were approximately 17.1–17.2 ms. These include Editor pacing, display synchronization, AI variance and the rest of the scene. They establish that this fixture rendered all modes successfully; small differences are **not isolated shader costs or a claimed FPS improvement**. Full first-island standalone/low-end/1080p performance and long moving-camera temporal stability still need a dedicated benchmark. No zero-allocation or memory-leak claim is made.

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
