# Shoreline foam v252

The shoreline now reads as a dense, narrow white ribbon. The earlier moving front spread a porous foam sheet over much of the 17–28 m surf mesh; the ribbon now concentrates its opacity at the actual shallow water contact, with a small, fading lace outside it.

## Changes

- `Assets/Island/ProceduralShoreFoam.shader`: horizontal world-distance estimate from the baked terrain/water separation and its screen derivatives. The core remains dense throughout the shared swell cycle; noise varies bubbles and the outer edge instead of opening large holes. The default 1.8 m width varies approximately 1.17–1.98 m with the swell, before small edge variation and filtering. Added material Inspector controls **Shoreline band width (m)** and **Shoreline foam density**. The existing displaced surface, depth occlusion, lighting, shadows and fog remain in use.
- `Assets/Ocean/ProceduralOcean.shader`: narrow, dense depth-contact foam supplements the island ribbon. The weaker breaker fringe stays within another 0.7 m rather than filling the full shallow depth range. Open-water whitecaps and ship wakes are unchanged by this revision.
- No new texture samples, render passes, particles, per-frame managed work, or mesh changes. No wave/physics, scene, gameplay or save changes.

## Native review

Existing Unity 6000.3.23f1 only, stopped Editor. The helper temporarily opens Environment and Lighting, renders the real coastline with URP at three camera angles, and closes those scenes without saving. It explicitly pairs the temporary shore materials with their temporary ocean, rather than the still-open Story1 ocean.

`Tools/ReviewShoreV252.cs` creates temporary copies of the before/current shaders that replace only their shader clock. This permits paired renders at clock 104 and current renders at clocks 104, 106, 108 and 110 without advancing gameplay. All 12 final phase/view images have non-black native output and were reviewed as a contact sheet. Three additional views use the unmodified production shader clock. Both production shaders report no compilation errors. Temporary comparison shaders were removed through AssetDatabase.

Evidence: `.codex/shore-foam-20261025/paired-before-retry/`, `final-phases/`, `final-live-shader/`, `comparison.jpg`, `phases.jpg`, and `render-checks.json`. Earlier black comparison captures are rejected evidence.

The repeated temporary scene render/unload procedure exposed an Editor URP ReflectionProbeManager/ZBinning cache exception. The tool's black captures were discarded, the renderer was reset, and the original clean Story1 scene was reopened without saving. See final acceptance for the subsequent domain-refresh and Console observation. This is not a claim that the entire game is error-free. Standalone Player, low-end hardware, full-game performance and all quality tiers have not been tested in this visual task.

Scene files, the human save, cloud shader, island generator, OceanWorld, shared wave code and QualitySettings are hash-checked against the start of this task. No commit, push or upload was performed.

Final recovery: refreshed the existing Editor domain. The first command after reload timed out during initialization; the next check completed in 2.2 seconds. The Editor responds normally, the original Story1 scene is clean, and native SceneView repaint plus both shader imports pass. No ReflectionProbeManager/ZBinning exceptions recurred after the domain refresh. All 14 preserved file hashes still match.
