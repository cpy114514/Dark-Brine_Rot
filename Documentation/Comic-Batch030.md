# Color Comic and B&W Manga — batch030

At the user's request, the two added style slots now provide **Color Comic** and **B&W Manga**. Origami's fold treatment and the previous film-style grayscale effect are replaced. Realistic, Dark Fairytale, four-card selection, arbitrary-pair comparison and independent pixel mode remain.

## Appearance and controls

Color Comic uses depth/luminance contours, depth-guarded color smoothing, cel-shaded bands, restrained color saturation, shadow ink and regular printed dots. B&W Manga uses paper-white/ink-black zones, stronger outlines, halftone screentones and diagonal shadow hatching. Its grayscale result includes transparents and remains grayscale at minimum intensity; it does not simply desaturate the image.

Both styles expose **Ink weight / Tone contrast / Screentone / Shadow ink**, plus the existing40–100% style intensity. Comic controls have independent versioned `Comic1` preference keys. Previous fold/film values remain untouched. Saved IDs `origami` and `monochrome`, numeric modes2/3, preset GUIDs and preview GUIDs are retained for compatibility. The legacy Origami enum alias also remains for existing external verification tools. Players see only the new names/descriptions.

Both transforms now run after world transparents and postprocessing, before overlay UI. The batch029 complete opaque-depth copy is shared by both comic paths so terrain/grass missing from the SSAO normal prepass still contribute to contours. Depth boundaries use the full scene, while luminance edges also include transparent effects. The copy never replaces URP's shared depth. Realistic enqueues no transform; Dark retains its original before-transparents branch and atmosphere. Old in-place paper sky/water branches are inactive; no ocean/sky asset edits were needed.

Printed cells are fixed in screen space with antialiasing, no time/random-frame seed, and a coarser pixel-mode pattern. They are intentionally screen-anchored, not surface-anchored. Existing grass blades/layout are preserved; normal wind, projection and final visibility still change.

## Native evidence

All preview images are real1280×720 Unity captures, fixed noon, the same excluded Sahur pose/camera, intensity89.7673%. Four current preview PNGs were refreshed together under their original GUIDs. Actual camera positions/rotations and render callbacks are recorded in `gallery-accepted.json`.

![Color Comic](../AUTO_QA/Evidence/batch030/day-color-comic.png)

![B&W Manga](../AUTO_QA/Evidence/batch030/day-bw-manga.png)

The first palette was too bright in grass and was refined. An intermediate capture before explicit shader import was not accepted as proof of that refinement. All accepted views use the explicitly imported shader.

Midnight controls exposed crushed character detail in the initial Color Comic curve. A low-light blend and source-luminance floor preserve that information. In the fixed face/body sample rectangle[635,205,690,380], mean RGB rises15.52→23.06; this is a limited image control, not an illumination/FPS improvement claim. Original nighttime lighting remains dark. Accepted noon/night, actual4-sample target/pipeline and independent320×180 pixel views were inspected. At pixel resolution fine facial detail is naturally reduced.

## Executed checks

**77 accepted native checks pass; overlapping intermediate runs are excluded.**

- 40 rendering/control checks: both styles' four sliders and intensity visibly affect real pixels, Manga world pixels have equal channels, independently saved controls reload on native manager re-enable,20 switches retain cached materials, the full Near grass layout remains, and the fixture cannot save a game. These are saved-key/read/re-enable checks, not a new Play/domain-reload persistence test.
- 27 UI checks: four cards, new labels without fold/film controls, normalized sliders, all16 comparison pairs, updated matching Color/Manga reveal, and actual composited red overlay remains red in both comic modes.
- 10 transparent-world checks: cyan alpha-blended probe remains visible/colored in Realistic and Color Comic, contributes visible pixels after renderer off/on controls, becomes gray with the entire Manga world, and both comics use the composed-world/depth pass. This is a rendering probe, not a full combat-effect encounter.
- Four styles×two poses at noon, midnight,4×MSAA and pixel mode. Image/nonempty controls and callback records are retained.24 captured orbit positions return with3,998/921,600 pixels differing by more than2 channel levels; this rejects wholesale reshuffling, not perfect temporal stability or measured gameplay FPS.

A compiler warning about the implicit `ComicColor` return was removed by explicitly initializing one result and assigning both branches before returning. This control-flow rewrite preserves rendering arithmetic. The latest shader builds successfully, and native compilation of both normal-keyword variants reports no messages. Earlier accepted rendering/control captures use the same arithmetic before that return-initialization rewrite.

The same Editor17184 built all seven enabled scenes as Windows64 Mono Development: **Succeeded,82.95s,493 warnings,4 existing missing-plant messages** covering606 instances. The binary was not launched. No new standalone FPS/GPU/GC result, device benchmark or full active-combat/animation acceptance is claimed. Comics add a fullscreen depth copy/R32 transient plus color/depth samples; this patch is not advertised as a performance optimization.

## Restoration and checkpoint scope

The user's live session was stopped normally before isolation; fresh post-stop save/backup snapshots were preserved. Original five First Island scenes are restored clean/stopped with zero owned objects, timeScale1 and no shader messages. All14 protected settings/material/prefab/profile/unchanged-preset/source hashes match. Existing preset/preview GUIDs match. No original mesh, UV, rig, clip, collider, source material or authored scene was rewritten.

All981 original project-registry value kinds/values and the key set match after native campaign-cache restoration and removal of one qualified unused fixture healing ledger. The eight temporary comic-control preference keys from persistence checks were removed in `finally`. Private values/saves are never published.

The build cleanup guard initially rejected Unity's native `_MainTex` alias representation even though the serialized original alias was empty. The build itself had completed successfully; actual task fault/completed/building state was inspected before recovery. Native alias clearing reproduced the exact original material hash. Flags/preloads/settings restoration had already executed before that guard; settings bytes are independently verified. The helper now qualifies the known alias by exact post-restoration bytes. `build-cleanup-recovery.json` records the rejected cleanup and recovery; no rebuild was needed.

Changed scope: renderer/shader, manager/settings/preset/UI/installer, the two replacement preset descriptions/default controls, four derived PNGs, QA/docs and external verification helpers. Build caches, private snapshots, temporary resource files and unrelated Plastic checkouts are excluded. `Tools/Comic030.cs` and `Tools/BuildComic030.cs` use new private roots/label guards; do not rerun over their completed evidence. Private `.codex/comic030` contains the build, snapshots and intermediate takes.

Git/Plastic synchronization uses an exact owned manifest. Local Plastic base212 deliberately retains older already-published patches; never partial-update this pending workspace. Publish by scoped shelveset/server-side merge and verify remote content, preserving independent cs213 work and unrelated settings/Recovery checkouts.

Next: active combat/Boss/tutorial readability in the two comic modes, longer forest traversal and a qualified standalone rendering-cost measurement. Do not reintroduce Origami or film-only black-and-white; these replacements are explicit user direction.
