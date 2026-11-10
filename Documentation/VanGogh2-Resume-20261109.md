# Latest authoritative status — renderer revision completed

Read Documentation/VanGogh2-Results-20261109.md first. The new renderer, art profile, same-view comparisons, dynamic routes, controls, lifecycle and GPU measurements are implemented and checked. Historical analysis below is superseded; do not interpret its old "not yet written" entries as unfinished work.

New production files are VanGoghProfile.cs / VanGoghRenderPass.cs, Resources/VisualStyles/VanGoghPainting.asset and painting/skin-anchor shaders, Shaders/VanGoghPigment.hlsl, scoped feature/manager hooks, VG-only ocean/sky branches and VG-only preference/label changes. DarkFairytale.shader legacy code and other style shaders are unchanged. Source rigs/models/scenes are not modified by this revision.

Final evidence under ignored .codex/van-gogh-2: accepted-native1080 (native1920x1080, renderScale1, actual4xMSAA,15 original/legacy/new shots), original-legacy-new.jpg, controls.json (10 parameters effective,10 exits return to1 template), performance-final (RTX4060, native1080 single-sample RT), motion-final and motion1 (all4 existing P2 attacks collectively, real threshold transition, no errors), camera-and-boss-final.mp4 and moving-blades.mp4. Two dynamic takes were recorded; sampled keyframes inspected. No human FINAL/allcampaign/zero-swimming claim.

Final single-sample GPU medians: Economy paint.978ms, Balanced1.409ms, Fine1.740ms; anchors/tensor~.02ms. Entire camera5.26/5.38/5.41ms, but realistic baseline varied4.15–4.72ms with clocks; do not claim a fixed exact total increment/FPS. Texture budget35.60MiB single-sample,59.33MiB logical4x; driver allocation estimate rose35.6MiB and returned close to baseline. Exact practical limits are in report.

Other-style return tests:4 styles pixel-identical; realistic at most1/255 in57pixels, with similar1LSB variation observed in an earlier repeated-control run. No keyword residual. New shaders compile; existing ocean/sky warnings remain. Editor stopped, formal saves byte-unchanged, original clean Second Island setup restored. Quality settings restored; no EXE rebuilt or remote push.

Gameplay feel completed separately through bbef1e6b, with929 blade checks plus input/actual-island routes; keep those changes. Scoped rendering implementation/report commit: ffc9cd0c. Existing unrelated second-island changes remain preserved. If automated continuation finds both revisions complete, report completion briefly and do not manufacture extra work or rerun completed stages. The user explicitly asked that schedules only recover actually unfinished work.

---

# Van Gogh Rendering 2.0 — scheduled continuation checkpoint

User requested a five-hour delay before continuing on November 9, Vancouver time. Resume the actual implementation, not only a reminder or proposal.

## Request and boundaries

Read the complete request at `C:/Users/pinyu/.codex/attachments/9b1b5c5c-f4ce-4c0a-b695-52c3eee4da1a/已粘贴的文本.txt`.
Only replace the Van Gogh mode. Preserve every other Style Changer mode, its assets and visual output; do not redesign the changer. Do not edit scene layouts, source models, rigs, animation or game balance to simulate an improvement. User requests art research, directional multiscale strokes, surface anchoring/temporal coherence, expressive daylight-aware color, impasto, independent tuning, actual same-view original/legacy/new comparisons, moving-camera/combat checks and measured 1080p RTX 4060 cost. Do not invent FPS or memory results.

Project: `D:/indiegameDev/unity/Dark Brine_Rot`. Unity CLI 0.6 pipeline works. Unity 6000.3.23f1, URP 17.3.0, ForwardPlus PC and Forward mobile, Render Graph enabled. Hardware is RTX 4060 8188 MiB, D3D11, driver596.49. FrameTimingManager.IsFeatureEnabled() is true. No production rendering changes made yet.

## Completed inspection / baseline

- Native tool `Tools/InspectVanGogh2.cs` produced `.codex/van-gogh-2/inspection.json`.
- Existing source backups under ignored `.codex/van-gogh-2/before/Assets`, plus initial git status. Preserve the extensive unrelated local changes from the earlier second-island revision, including promotional images and VersionControlSettings.
- `Assets/Editor/VerifyVanGogh2.cs` is a NEW compiled isolated baseline test. `Prepare()` snapshots formal save bytes and scene setup, enters Play, uses an isolated save path and loads First Island/Main. It captures all six styles at1920x1080 using `RenderPipeline.SubmitRenderRequest(camera, new UniversalRenderPipeline.SingleCameraRequest {destination=rt})`, not obsolete camera APIs. Play exits and original scene setup is restored automatically.
- Baseline succeeded: `.codex/van-gogh-2/baseline-result.json`. 24 screenshots (coast/ocean/vegetation/boss ×6 styles) are in `before`; camera poses in `views.json`, world info in `world.json`.
- The coast capture happens during the unconscious arrival, so Sahur lies afloat. Improve the final comparison fixture: `FirstIslandArrival` has public shore/driftStart and RestoreProgress; its OnDisable stops cinematics/restores controls. End only the TEST arrival, place Sahur at the shore on actual collider ground, play idle, use the same pose for realistic / old VG / new VG within a single frozen Play. This does not authorize production story changes.
- Formal save bytes unchanged. Editor independently verified stopped, clean Second Island scene only, matching pretest setup.
- `preservation.json` incorrectly reports sceneSetupRestored=false because its string comparison removes spaces from file paths on only one side. Fix it by deserializing SceneSetup[] and comparing canonical JsonConvert.SerializeObject results on both sides. This is a TEST REPORT bug; native scene setup was actually restored.
- Do not upload `.codex` evidence: it includes private save snapshots.

## Existing rendering architecture

`Assets/Scripts/VisualStyles/VisualStyleManager.cs`: persistent manager, Select(id,persist=false) supports nonpersistent probes; intensity, Details Vector4; only DarkFairytale changes atmosphere globals. Existing mode IDs0..5, VanGogh5.
`VisualStyleRendererFeature.cs`: one cached IllustrationPass, for modes>=ColorComic runs after transparents/postprocessing, copies true opaque depth via URP CopyDepthPass then full-screen shader. Other modes must retain this exact behavior. Feature installed once in PC_Renderer and Mobile_Renderer.
`Assets/Shaders/DarkFairytale.shader`: old VanGoghColor uses nine screen-grid dabs, universal direction, fixed screen sky curl and bristle noise. This swims with the camera. KEEP its old branch byte-identical for same-frame legacy comparison and rollback; bypass it only for the new Van Gogh path.
`VisualStyleUIController.cs`: existing four style-specific detail sliders. VG labels currently Brush size / Swirl / Impasto / Color richness. Can rename/match these four only for VG; put remaining independent artistic controls in a dedicated ScriptableObject. Preserve other modes' controls.
`VisualStyleSettings.cs`: perstyle PlayerPrefs detail keys; consider a Van Gogh2-only key revision, preserve other keys and legacy values.
`Assets/Ocean/ProceduralOcean.shader`: custom displaced water, Transparent-100, ZWrite On, actual Gerstner world shape. Drawn AFTER opaque; existing shader has input.positionWS, waveNormal/normalWS, wave direction and original p coordinate. Ends with StyleEnvironmentColor then return. Need a VG-only in-place surface branch before return, with a dedicated global shader keyword so other variants have no extra brush instructions.
`Assets/Ocean/ProceduralSky.shader`: sky dome Transparent-50, ZWriteOff, raymarched clouds. Has rayDirection, cloudAlpha, cloudColor, _CloudSpeed, _Daylight, _NightFactor. Ends StyleEnvironmentColor then return. VG-only spherical/direction-anchored stroke shading follows cloud arrangement/wind, not screen pixels. Leave other variants identical.
`Assets/Shaders/EnvironmentVisualStyle.hlsl` belongs to existing styles: avoid changing its behavior.

## Proposed implementation direction (not yet written)

Use an independent Van Gogh pass at BeforeRenderingTransparents for opaque color. Water and sky receive VG-only native surface shader branches, drawn later; this avoids painting UI/combat particles and avoids reconstructing water from opaque seabed depth.

For opaque static surfaces, reconstruct world position from a private actual opaque-depth copy, derive normals/tangent planes, world-space directional strokes, vegetation inferred from source green/vertical orientation. For characters, use UV anchors (stable through skeletal deformation) from a dedicated RGBA16F buffer rendered only for visible SkinnedMeshRenderers using override materials. NO source material replacements, no rig edits, no renderer MaterialPropertyBlock modifications.

Skin anchor buffer can encode UV scaled by physical model height in RG, stable identity in B, valid in A. Per-skin cached override material stores scale/seed; render submeshes with RasterCommandBuffer.DrawRenderer, original renderer automatically supplies actual skinned vertices. This API was VERIFIED via native reflection: RasterCommandBuffer exposes DrawRenderer(Renderer,Material,int,int). Reject hidden fragments in anchor shader against the private copied opaque depth, so no mismatched MSAA depth attachment is required. Only include opaque original submaterials; skip inactive/outside camera culling planes. Cache scans sparsely, release materials on mode exit. Static geometry requires no duplicate draw.

Structure tensor can be computed at half/quarter resolution from source color gradients, smoothed separably, used for restrained local direction/edge-aware abstraction. Main dab centers/hash are surface UV/world anchored, no time-random seeds. Convert nearby stroke centers to screen offsets through coordinate derivatives/Jacobian, depth and identity reject samples crossing silhouettes. Use multiple stroke scales, irregular curved tapered footprints, adjacent pigment variation and bristle-height lighting, not a repeated fullscreen texture. Fine scales fade analytically with fwidth; no unstable pixel-grid size changes. Temporal Stability controls transient image-flow influence relative to stable surface direction. Avoid history accumulation initially (no ghosting/extra history VRAM); honestly document this choice rather than claiming motion-vector reprojection.

Suggested independent VanGoghProfile: brushSize, density, directionStrength, curvature, colorAbstraction, contrast, impasto, outline, temporalStability, overallIntensity, quality preset. Global keyword e.g.VAN_GOGH_SURFACE, global packed values shared by opaque/water/sky. Manager ApplyEnvironment calls a small VG-specific runtime global update; other modes disable keyword and enqueue no VG work. Use separate resource shaders/profile so builds include them.

Legacy comparison flag should route Van Gogh back through its unchanged shader branch and disable new globals/keyword. Capture original / legacy / new at the same frozen scene/camera/time; also switch every other style before/after newVG and compare pixels/state exactly. Do not permanently expose the legacy flag as a user menu redesign.

## Art and graphics research already done

Primary sources opened:
- https://unravel.vangogh.com/en/story/29/thick-brushstrokes (fetch intermittent, search excerpt explains outlines/shared simplified hues)
- https://www.moma.org/collection/works/79802
- https://www.metmuseum.org/art/collection/search/436535
- https://www.nationalgallery.org.uk/paintings/vincent-van-gogh-a-wheatfield-with-cypresses
- https://www.getty.edu/projects/van-goghs-irises-a-closer-look/ (violet/yellow complementaries, pigment change, surface reconstruction)
- https://www.vangoghmuseum.nl/en/collection/s0029V1962 and museum gallery texts for The Sower
- https://research.adobe.com/publication/interactive-painterly-stylization-of-images-videos-and-3d-animations/ (GPU sprite strokes, optical-flow particles for coherence)
- https://isgwww.cs.uni-magdeburg.de/~stefans/npr/entry-Bhattacharjee-2006-TGR.html (texture-guided object-space stroke locations and LOD)
- https://www.kyprianidis.com/p/pg2009/jkyprian-pg2009.pdf (smoothed structure tensor, anisotropic Kuwahara, edge-preserving coherent abstraction)

Local research `.codex/van-gogh-2/references/starry-night.jpg` from MoMA was downloaded and visually inspected: directional broken short parallel marks organize large rolling sky masses; differentiated vertical cypress, horizontal village planes; pale yellow/teal marks over dark blue underpaint and raised bristles. Do not copy a whole painting into the game. Getty research page HTML and Met wheatfield image also downloaded; view the latter and obtain/reference Getty/Sower images as useful. Use primary sources in documentation with modest paraphrases, no long museum text copies.

## Next work

Implement the scoped renderer, iterate actual screenshots until it looks like organized oil painting rather than a running prototype, validate dynamic rotation/character/combat and switching. Measure GPU markers and frame timing/VRAM at1080p on actual RTX4060. Profiler marker sources: Tools/SystemPerformanceRoutes.cs; FrameTimingManager is enabled. RenderGraph ProfilingSampler can record GPU milliseconds; locate its exact package source with `rg --files --no-ignore Library/PackageCache` because normal rg respects ignore. CLI output can yield a session ID; preserve it to poll rather than discarding the command envelope.

No subagents unless user or applicable skill explicitly asks. Read unity-cli/unity-pipeline skills, shader-programming skill and COLLABORATION.md; all already applied in this chat. Use Unity APIs for native scene/material/profile changes; never hand-edit Unity YAML. Danger-full-access, approval never: never pass sandbox_permissions. Keep progress updates under60seconds. No new build/push authorized by this new rendering request; earlier EXE is old.

## Later user steering while this task was scheduled

User separately requested twin-blade attacks while moving. Completed by two movement/root-motion conditions in SahurAttack.cs; read Documentation/TwinBladeMovingAttacks-20261109.md and preserve that work. Native regression passed929 checks/48 directional cases. This does not cancel the scheduled Van Gogh rendering objective.
