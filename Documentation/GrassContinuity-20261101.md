# Grass distance continuity — 2026-11-01 / batch038

The user still noticed grass changing when approaching/retreating after batch032. They explicitly selected **fixed balanced density, consistent near/far appearance, with smooth performance prioritized** over permanently rendering all legacy source geometry.

## Production change

- The legacy manual-instancing layer used a hard mesh replacement at a bounds distance of 35 m. It now uses the same economical library mesh at every distance. This intentionally fixes the old layer to a balanced density; it does not preserve its previous maximum close-up density.
- The recognized `VerifiedGrass*` family retains its complete original Near layout. Its old native screen-height fade could clear a whole patch around 60–80 m at FOV60/lodBias0.5. Mavis wind grass now has a single native LOD with threshold0 and no native patch fade, independent of camera FOV/lodBias.
- Distant Mavis grass retires through a 140–176 m spatial coverage band, evaluated per fragment in undeformed world coordinates. Small stable cells spread retirement across the field rather than clearing an entire patch together. Forward, depth and shadow passes share the rule. Normal wind/player bending remains active; trees are excluded through the existing grass flag.
- CPU grass residency is190 m measured to bounds, beyond the shader's retirement band. Existing instancing, complete batch snapshots, frustum bounds padding and bounded refresh remain in use. The batch property block explicitly carries the grass flag.
- Unsupported/authored LOD policies stay outside this override. Recognized patches using URP/Lit retain the preceding native fade because Lit does not implement the custom spatial coverage function. Legacy Lit ground meshes keep fixed geometry but retain conservative CPU distance culling. The first-island VerifiedGrass audit found all recognized patch renderers using the custom shader/grass flag.
- Original membership, enabled flags, thresholds and fade modes restore when ownership ends. Runtime disable clears the global fade vector. An Editor play-state callback additionally clears it upon entering Edit Mode, because the Editor can restore shader globals after runtime `OnDisable`.

Only `WorldPerformanceManager.cs` and `MavisFoliageWind.shader` contain production changes. Scene, mesh and material assets are unchanged by this task. Earlier combat/VFX/controller work is preserved.

## Evidence and limits

Private evidence is in `.codex/grass038`. `Tools/GrassContinuity038.cs` preserves/restores the editor and save state; `Tools/VerifyGrassContinuity038.cs` provides repeatable native probes outside Assets.

| Probe | Result |
|---|---|
| Fresh Environment+Lighting runtime |11,298 registered VerifiedGrass groups;0 incorrect policies;11,298/11,298 renderer property blocks carry the grass flag.7,013 legacy entries within170 m all use the expected fixed mesh. |
| Isolated original patch |Frozen scaled time, wind disabled on temporary materials, fixed orthographic projection, camera distance20–200 m.20 through140 m have0 changed RGB pixels and0 changed silhouette pixels;19,682 visible pixels. Distance141 m changes1 silhouette pixel; visible coverage decreases monotonically thereafter and is0 by185 m. |
| Policy/lifecycle |36 assertions passed across custom shader, Lit fallback, authored CrossFade and unsupported mesh families, two ownership/restore cycles each. |
| Compiler |C# compilation successful; native shader diagnostic messages0. |
| Continuous camera travel |`baseline-coast.mp4` / `final-coast.mp4`:240 native captures each,960×540,30 m forward then return, noon lighting, active wind. The8-second videos use fixed30 Hz capture; this is NOT measured game FPS. |
| Visual review |Matched forward/return stills in `continuity-contact.png`; isolated coverage images checked. Perspective, wind, cloud and lighting changes remain natural image changes. No claim of pixel-identical moving-camera whole-scene imagery. |

Manual instancing must be explicitly submitted before `Camera.Render()` in these probes. The final coast captures do this, with automatic submission disabled temporarily and restored in `finally`. Initial exploratory `before/after` captures did not include this positive control and are not the accepted whole-field visual evidence. Runtime mutation previews, including the initial highest-density comparison, are exploratory rather than final performance measurements.

### Editor performance sample

Exact pre-task source was recompiled and run for the baseline; final source was then run fresh. Same two camera positions/orientations, noon frozen, same quality settings,90 samples per view after a2-second warmup. No screenshot/readback inside the sample loop. `FrameTimingManager` and Unity render counters were used. Median values:

| View | GPU ms before → final | CPU frame ms before → final | Triangles before → final | Draw calls before → final |
|---|---:|---:|---:|---:|
| Hillside |17.647 →13.719 |17.770 →17.198 |16,457,188.5 →15,656,781 |1,039 →1,090 |
| Coast |14.265 →6.011 |16.828 →16.004 |16,838,057 →15,466,547.5 |1,316 →1,329 |

These short Editor samples support rejecting the initial full-source candidate, which was substantially heavier, and show no observed regression for the selected balanced policy in these views. They are not a standalone Player benchmark, hardware-independent guarantee, or proof of a sustained FPS increase. Background Editor work and rendering timing vary.

## Workspace restoration

Before tests, the five first-island scenes were clean and stopped. The isolated test session restored Main, Gameplay, Lighting, Environment and Enemies, all clean; keyboard/mouse stayed enabled. Save and backup bytes compared unchanged during restoration. Healing-pack preferences were restored from the task snapshot. Stopped authored grass policies had0 incorrect thresholds/modes across16,874 loaded groups (the complete editor setup includes more groups than the isolated environment).

The first stopped audit caught a nonzero shader-global value and prompted the explicit Editor cleanup callback described above. That callback compiled successfully; a subsequent isolated repeat was refused because the Editor had already entered normal gameplay in the restored scenes. The running user session was preserved. No second save rollback, scene switch, commit, push or build was performed.
