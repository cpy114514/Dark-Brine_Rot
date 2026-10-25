# Cloud appearance and render cost — v251

The user found the existing clouds visually strange. This revision changes only `Assets/Ocean/ProceduralSky.shader`. The existing ocean sky renderer, quality settings, weather progression, day/night palettes, scene assets and gameplay systems are retained.

## Changes

- Coherent horizontal weather columns and a stable world-space cloud height replace height-dependent column noise and camera-relative altitude. The volume now spans every possible local base/top, avoiding clipping shifted cloud columns against a single thin slab.
- Cloud altitude/thickness are1100m/800m. Larger connected forms have soft erosion and useful blue-sky gaps; the old distance-rescaled noise no longer stretches the same cloud into a different shape as its distance changes.
- Noise octaves normalize against their actual amplitude sum. Ray-footprint filtering fades unresolved fine noise instead of undersampling it into distant stripes. Remove the discrete1600m/3500m step-count switches that exposed hard viewing-angle rings. Quality tiers keep8/14/20 view steps; the maximum does not increase.
- Absorbed energy is composited with `1-exp(-extinction)` and matching transmittance rather than adding an unbounded linear step. Daylight ambient and broad sun attenuation give lighter highlights with soft shaded undersides. Existing sunset/night palette inputs still drive the result.
- A single advected field drives both body and lighting. Wind mapping changes from95/-58 to16/-9 metres per second times the existing speed control, slowing the old racing drift. A hidden material-local clock offset defaults to0 and is used only by preview tools.
- Remove the unrelated sinusoidal angular cloud layer. Distant clouds fade into atmosphere with range/elevation falloff. The existing depth-tested single sky-sphere pass remains; no extra layer, texture, renderer or post-process is introduced.

## Native review and measurement

Evidence: `.codex/clouds-20261024/`. The existing Editor was already playing Main/Environment/Gameplay/Lighting/Enemies at Time.timeScale0; that state and camera/player were retained. Temporary cameras/materials isolate the actual sky renderer, with the main camera's post-processing configuration. Before/after pictures share the same position, time and controlled palette/coverage. `continuous/` is the final static/benchmark revision. Intermediate passes are retained: the first was too much like a soft blanket, and the filtered intermediate exposed angle-dependent step boundaries; the final pass removes those boundaries.

Native views reviewed: actual game camera, horizon and upward views,62/85/96% coverage, sunset and night. The9-view quality/weather sheet covers low/medium/high with0/62/96% weather inputs. The final96-frame,12FPS cloud-drift preview advances the material-local clock through8s while keeping the world paused. Its6 checks pass: supported/compiled shader,9 valid quality/weather outputs, observable motion, bounded consecutive-frame mean pixel changes, restored source material/layer/speed and no temporary camera leak. This is a deterministic native shader preview, not a recording of world simulation or human final artistic approval.

Paired performance cases alternate old/new sky-only960x540 camera renders, including URP CPU work and a1px readback to synchronize the GPU. Each case warms8 iterations then records24 measurements per variant. Median combined render/readback times:

| Coverage | Quality | Before | After | Change |
|---|---|---:|---:|---:|
|62%|Low|2.923ms|2.502ms|-14.4%|
|62%|High|4.661ms|3.271ms|-29.8%|
|96%|Low|3.344ms|2.700ms|-19.2%|
|96%|High|4.788ms|3.357ms|-29.9%|

These are matched-setting Editor camera workloads with a changed visual model; they are neither isolated GPU timestamps nor a full-game/low-end hardware FPS claim. The comparison shader was temporary and removed through AssetDatabase after review. The original shader text and all timing arrays remain in the evidence folder.

The first baseline capture completed its images but its metadata writer encountered Unity Vector3's recursive normalized property; the helper now writes numeric arrays. No production shader or gameplay exception resulted. Final Console reports no new errors. World/day-night sources, current human save, saved Story1 bytes, combat, deck-clearance and loading-bar hashes are preserved. No scene save, new Editor, commit/upload, build or automation was performed. Current Play Mode/pause state is retained.
