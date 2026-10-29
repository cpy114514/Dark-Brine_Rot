# Foliage CPU optimization — batch018

Measured and implemented in the existing Editor without lowering vegetation density, view distance, shadows, render scale, models or gameplay. One production file changed: Assets/Scripts/Performance/WorldPerformanceManager.cs.

## Evidence and changes

Initial native first-island Environment/Lighting traces show foliage visibility/submit at about1.40ms and the bounded refresh coroutine around1.1–1.3ms. Pixel/UI work was not a leading script hotspot in these traces. The bounded refresh ownership/iteration system from batch017 is unchanged.

The visibility kernel previously recomputed three absolute plane-normal components for every bounds/plane test. It now calculates the six absolute normals once for each camera submission and reuses them. Every camera still recalculates its exact culling planes, including custom culling matrices; no transform/padding/frustum/visibility state is skipped across frames or cameras. Four MaterialPropertyBlock wind-array names now use cached Shader.PropertyToID values, avoiding repeated string lookup/marshalling. Array contents, lengths and names are preserved.

## Executed validation

Final native regression passes192168 cases/assertions: 12000 bounds across16 camera configurations, production method versus retained old arithmetic, zero-size/non-finite padding, actual RenderGrass per-camera setup and custom culling matrices, plus all four production property-array bindings. The preview fixture queues no draws while exercising per-camera setup. Earlier runs are discovery and are not added to this accepted total. Production recompilation passed errors=[]; final request is up_to_date.

Warmed alternating paired kernel samples,15 per path, same ephemeral compiler and workload:

| Kernel | Old median | New median | Scope |
|---|---:|---:|---|
|96000 frustum queries|10.1556ms|5.6464ms|44.4% lower inner-kernel CPU time|
|4800 property-array calls|0.4386ms|0.1688ms|61.5% lower inner-kernel CPU time|

Actual RTX4060/D3D12 Editor scene traces,34 captured main-thread frames per phase:

|Phase|Visibility incl. before ms|After ms|Self before ms|Self after ms|
|---|---:|---:|---:|---:|
| native-world | 1.400 | 1.100 | 0.810 | 0.519 |
| pixel-world-four-card-gallery | 1.417 | 1.108 | 0.812 | 0.532 |

Native world is1920×1080 renderScale1; the second phase uses the existing pixel mode with the four-card gallery. WorldPerformanceManager is resolved through Resources because its DontSave object is invisible to ordinary searches; collection waits for readiness. Each before/after camera capture is fenced by endCameraRendering. A culling-mask-zero control differs at all2073600 pixels, qualifying the actual rendered world. Native PNGs were inspected. Water/weather time advances, so PNGs are not a pixel-identity test.

These short before/after scene windows include profiler overhead and scene variation and do not independently isolate GPU cost or establish a whole-game FPS improvement. The paired kernels and exhaustive classifications establish the targeted CPU improvement and unchanged math. NPC components in Environment remain present; raw scope wording excludes the Gameplay scene, not every NPC.

## Safety and continuation

No authored scene/prefab/mesh/model/rig/UV/animation/collider/shader or visual-style setting changed. Save and backup bytes match fresh copies. The five original scenes, original pipeline/style/intensity/frame cap/vsync/time and profiler state are restored. Unrelated Plastic ShaderGraph/recovery paths and line-ending-only EditorBuildSettings remain excluded. Detailed raw data and proof.json are in AUTO_QA/Evidence/batch018.

Next: qualify longer standalone first-island GPU/frame timings and examine bounded refresh workload; avoid claiming the current coroutine budget is a bug or reducing foliage density without evidence. Do not repeat batch017 ownership tests or this matrix unchanged unless later edits affect them.
