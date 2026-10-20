# Nailong volumetric pressure rings and deeper saliva — 2026-10-20

The user's revised target is a complete 3D pressure ring and visibly deeper saliva colour in the Unity Editor. This revision extends v229's existing effects and retains the actual attack actions, timings, damage, enlarged projectile and original character assets.

## Implementation

`NailongGroundSlam` now constructs a closed, terrain-conforming torus: 64 sectors around the boss and 12 faces around its elliptical cross-section, with exact duplicate seam positions/normals. Each pulse uses 845 vertices and 1,536 triangles. Its rounded top, inside, outside and underside replace the old three-row ground strip. The presentation crest is 0.80 m above terrain; the lower surface is offset 0.08 m. At birth the tube grows with radius, avoiding an inverted inner surface. The existing cached ground samples and reusable vertex arrays are retained.

The sonar shader uses surface-normal refraction and Fresnel contours on the closed geometry. Its cross-section profile is periodic, eliminating the former UV-edge transparency seam. Outward-facing triangles permit backface culling in one pass, avoiding doubled translucent surfaces. Final material values are 6 px refraction, 0.26 rim opacity and 0.012 body opacity; lower body haze and stronger contours make the rounded pressure front readable without turning it into a solid white belt. The existing no-scene-texture fallback remains.

Saliva transmission is deep teal (0.10, 0.46, 0.55), with a separate body colour (0.015, 0.22, 0.28) and adjustable absorption 0.42. Stronger body opacity and less white rim wash preserve that colour against sky/grass while retaining refraction, environment reflections and wet specular highlights. The resource materials were updated through Unity asset APIs by `Tools/InstallNailongVolumeV230.cs`; do not run older installers over these settings.

The larger v229 visible/contact radius remains 0.24 before native boss scaling. Spit still releases one glob along the existing swept parabola. Ground-wave gameplay width, 15 damage and 0.6 m jump clearance remain unchanged; volume thickness is presentation only. Roar's pressure visual adds no second damage event.

## Verification

`Tools/TestNailongVolumeV230.cs` passes 40 assertions: closed 360-degree geometry and both seams, continuous seam normals, finite birth geometry, visible vertical extent and underside, outward triangle winding, persistent deep water material, original single-shot release, 10/30/60 Hz swept arcs, thin-wall collision, no duplicate damage, interruption, jumpable rings, cosmetic roar, animation cue mapping and projectile pool reuse. Both 1,000 warmed torus updates and 1,000 warmed projectile/visual updates allocate zero managed bytes. This is an allocation check, not an end-to-end frame-rate claim.

An initial fixture ran its release assertion before a positive-delta Play frame; it is superseded by the final harness, which waits for three actual frames before testing. No production attack logic changed for that fixture issue.

`Tools/ReviewNailongIslandV230.cs` records normal camera rendering in the actual island, with native equipped Nailong/Sahur, authored terrain/foliage, native lighting and 11 m gameplay framing (FOV 60.4065 degrees). It manually drives the existing attacks while holding unrelated AI. Frozen normal-rendered side/above/rear views inspect a complete pressure torus on the same terrain. These are diagnostic camera views, not new gameplay camera behaviour.

The preliminary `island-volume` run verifies the first surface profile. `island-final` is the accepted recording after refining the pressure contours. Raw evidence is under `.codex/nailong-volume-20261020/`. The final preview samples actual captured frames at 10 Hz without interpolation; it does not measure game FPS. Native action health is 84 after each 16-damage spit and 55 after three 15-damage rings; at most one glob is active. Its collision radius remains 0.5366564 m at native boss scale, projected diameter approximately 42.6–66.6 px.

Full autonomous boss-fight behaviour, all weather/view angles, packaged builds, mobile fallback rendering and low-end hardware performance were not repeated. No universal collision/art/performance approval is implied.

## Preservation

Only the existing Editor on port 7800 was used. The user's original dirty Story1 scene stays open unsaved. No second Editor, standalone Player, build, commit, push or automation was created. Current human save was backed up before testing and must be restored after the final Stop, SHA256 `968BD555DD0288F031ADFE4B4E714E8F6626E381A788C95DFBE8702F9AEF337A`. Future work must protect any newer human save rather than blindly restoring this snapshot. Prior ship, shark/contact, combat and world-performance work remains in place.

Final verification: both shader diagnostic arrays are empty; Console errors 0. Existing Editor is stopped and unpaused, active Story1 remains dirty. Current save and unchanged ship source hashes are verified in `final-hashes.json`. Accepted final preview: `.codex/nailong-volume-20261020/Nailong_Volume_v230_Final.mp4` (4,607,429 bytes).
