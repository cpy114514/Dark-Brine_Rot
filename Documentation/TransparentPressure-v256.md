# Transparent combat pressure — v256

## Scope

Replace the existing player combat impact particles with short-lived, closed 3D pressure rings, and replace the old coloured weapon ribbon material with transparent air distortion. Reuse the existing confirmed-hit event for light/heavy hits, blocks, ranged hits and received damage. Add one cosmetic pressure ring at an actual jump-chop landing and at the beginning of actual dual-blade rush travel. Both blades sample their real fitted capsule path. Existing damage, hit detection, charge/rush timing, hit stop, camera response, audio, poses and controls remain authoritative.

Nailong's existing sonar is the visual reference. It is left intact. This revision does not replace unrelated fire, smoke, underwater or environmental systems throughout the game.

## Rendering and budgets

`Combat Pressure` is a URP transparent shader with a thin Fresnel edge, soft depth contact and at most 2.6 pixels of scene-colour displacement at default intensity. It applies the displaced-minus-original opaque colour difference with premultiplied compositing, retaining already rendered transparent backgrounds such as the ocean. It does not force scene copies or enable new renderer features. When the camera has no opaque/depth texture, it uses the same transparent geometry with a restrained visible rim. No bloom, fullscreen pass, noise textures or new particles are required.

`CombatImpactVisuals` owns eight preallocated rings (one shared 441-vertex / 768-triangle torus per actor) and two bounded 16-sample weapon ribbons. At most four rings may begin in one frame. Sustained bursts recycle slots rather than creating objects. Ring geometry stays immutable; animation changes transform scale and a reused MaterialPropertyBlock. Distant pulses are discarded beyond 100 m. Renderers are disabled when idle; none cast shadows, use probes or own colliders. Typical lifetime is 0.23–0.38 seconds. Heavy hits have a smaller second front, blocks one compact ring, and firearm hits a smaller single front.

Inspector controls expose visibility and impact lifetime. Existing combat profile VFX strength and impact brightness still scale confirmed-hit effects. Setting visibility to zero suppresses the new presentation. Pause holds the effect clock; disabling clears active rings/ribbons. Gameplay code adds no per-impact object/material creation and no new physics queries.

`Assets/Resources/Combat/PressureAir.mat` is created through the Editor API and retains the shader in Player dependencies. Original scenes, source animations and other materials are not edited.

## Verification record

Evidence, backups and previous pending-source hashes are under `.codex/pressure-vfx-v256/`. The current human Play session was saved before compilation and its campaign/save bytes are retained for restoration. The native fixture uses the existing Editor, a save-excluded actor and real queued movement/charge/fire input, then resolves confirmed hits against a test damage receiver. Test results, image comparisons and remaining limits are appended after execution.

`native-60-02/result.json` passes 43 assertions over 1,397 native frames at a fixed 60 Hz. It rechecks the preceding moving charge / partial blade rush / rifle input behavior, then verifies actual rush and jump-chop landing hooks, resolved light/heavy/blocked contacts and their damage, exactly one light/block front versus two heavy fronts, zero actor particle systems, same-frame and sustained-hit bounds, unchanged renderer instances, expiry, pause, disable cleanup, far culling, visibility zero, missing-scene-texture fallback, and successful referenced shader compilation. Pause did not advance wave lifetime; no new effect objects were created by the stress bursts.

Native contact screenshots have paired `-without` renders from the identical frame/camera with only the pressure renderers hidden. This separates the wave from existing damage-colour feedback. `image-metrics.json` records changed wave pixels for all four light/heavy/block/fallback cases. The heavy pair changes 1,532 pixels by more than three RGB levels (peak 14/255); fallback changes 5,666 (peak 17/255). These are intentionally subtle air effects, not opaque white discs. Native rush and landing screenshots review their gameplay positions.

The first take passes the functional assertions but its contact rings were too close to the target surface and visually weak. The accepted take launches pressure opposite the incoming hit direction, outside that surface, with a slightly stronger thin rim. Both takes are retained. Pipeline dispatch timeouts during cold in-memory compilation did not imply a test failure or a pass; the already-started native observer's saved result establishes completion.

This verifies the targeted feature in an isolated native arena, not every boss/cutscene/material combination or release-build GPU frame rate. The ocean compositing is designed to retain transparent backgrounds; a full shoreline playthrough was not performed. The complete original island workspace and the latest human save/campaign are restored after testing. Previous pending changes are preserved and no check-in, commit, push or build is performed.
