# Island 1 environment realism

The production Environment scene uses a shared habitat bake for terrain shading, grass and ground dressing. The bake retains the island's collider and elevation profile. Rocky ledges and smaller surface irregularities come from normal detail and low-profile geometry. The ocean, seabed and shore-foam scene data retain their original serialization.

## Terrain, grass and coastal detail

- A mipmapped 512 × 512 world-space map controls turf, trail wear, exposed stone and damp sand. Inland soil shows through gaps in the grass, trail edges feather into turf, and the lower beach darkens with a subtle wet highlight. Stone faces use separately blended normal detail.
- The seeded scatter places 136,941 tufts from the original candidate pool. Broad and fine patches leave open glades and connect dense meadow pockets. The bake uses terrain slope, a sea-level coastline distance field, trail widths, rock and tree proximity, and proxies for moisture and coastal exposure. The latter two are art-direction inputs.
- Four blade forms cover taller meadow grasses, lower fescue, dry grass and coastal sedge. Curved blades vary in scale, width, inclination and color. Height-weighted blade tips respond to the existing world wind and player interaction, with anchored roots and spatially offset phases.
- New dressing includes 166 shingle clusters, 90 fractured ledges, 37 stranded branches, 91 shell clusters and four coppercoil ferns. The existing flora, woodland litter, moss and fallen twigs remain in the palette. Existing flowers were thinned, and unusual plants were reduced to 51 specimens across five species, including the new fern.
- Both foliage layers use the existing GPU-instanced renderers. Each grass form has 140, 45 and 12 triangles at its three LODs. Props and the fern have two mesh LODs with distance culling. The new dressing adds zero scene GameObjects and zero colliders.

## Paired Unity captures

The screenshots use fixed matched camera positions in the production Environment and Lighting scenes in Editor Play Mode. Wind, clouds and waves continued to animate.

| View | Before | After |
| --- | --- | --- |
| Meadow approach | [Before](Island1RealismReview/meadow-before.png) | [After](Island1RealismReview/meadow-after.png) |
| Woodland edge | [Before](Island1RealismReview/woodland-before.png) | [After](Island1RealismReview/woodland-after.png) |
| Shoreline | [Before](Island1RealismReview/shoreline-before.png) | [After](Island1RealismReview/shoreline-after.png) |

[Overlook](Island1RealismReview/ridge-after.png) · [Stone and shells](Island1RealismReview/stone-and-shells.png) · [Coppercoil fern in the woodland](Island1RealismReview/coppercoil-fern.png)

## Performance

Measured in the same Unity 6000.3.23f1 Editor Play session on an RTX 4060 and Core i5-14400F. The fixed ridge view rendered at 1920 × 1080 with render scale 1 and VSync off. The before/after/after/before sequence used 120 warm-up frames and 240 measured frames per run. The table averages the two runs per state.

| Metric | Before | After |
| --- | ---: | ---: |
| Mean frame time | 11.31 ms | 9.87 ms |
| P95 frame time | 12.73 ms | 11.23 ms |
| FPS from mean frame time | 88.4 | 101.3 |
| Draw calls | 1,152 | 956 |
| Rendered triangles | 7.16 million | 5.83 million |
| Visible grass batches | 408 | 271 |

Editor rendering counters can include other Editor views. Standalone Player performance, dedicated GPU frame time and VRAM remain unmeasured. [Raw benchmark](Island1RealismReview/performance-abba.json)

## Validation and scope

- Unity compiled the project and rendered the updated materials. The bake reported zero missing references in the dressing materials and zero terrain or foliage shader errors. All 136,941 grass centers had valid support, and 431,080 sampled blade roots had no floating gaps. The dressing check found zero centers in the protected routes and clearings. [Bake validation](Island1RealismReview/validation.json)
- The GPU fixture verified that wind and player displacement moved blades while their roots stayed within pixel tolerance. [Wind fixture](Island1RealismReview/gpu-wind-interaction.json)
- All 20 gameplay checks passed on the actual island terrain over 796 recorded frames. The fixture covered locomotion, jump and landing, dodge, combat chains, camera collision and lock-on. The full boss and story sequence remains untested. [Terrain regression](Island1RealismReview/terrain-regression.json)
- A second bake with the same seed reproduced all 38 generated assets byte for byte. The scene comparison found only three authored component changes, all references or bounds for terrain habitat and the two baked layers. The ocean mesh, terrain collider mesh, other colliders and gameplay object data were preserved. [Repeatability](Island1RealismReview/repeatability.json) · [Scene comparison](Island1RealismReview/serialized-scene-diff.json)

## Later woodland and encounter follow-up

The subsequent [woodland pass](Island1WoodlandAndEncounters.md) adds varied tree silhouettes. The current [island route](Island1Route.md) relocates the original encounters and four checkpoints, grades inland clearings, and supersedes the vegetation counts above. The additional patrols from the intermediate pass were removed at the user's request. The measurements above remain evidence for this original environment pass.

## Authoring and licenses

Open **Tools > Island 1 > Habitat Review** to inspect the habitat mask, visualize protected corridors and clearings, and locate rare plants and ground dressing in Scene view. The baked data is selectable from the window. When editing an instance transform in the asset directly, keep it within its cell bounds and reopen the scene to refresh renderer batches. Rebaking overwrites generated placement edits.

Adjust seed, density, patch scale, slope, trail wear, coastline band and detail spacing in [Island1RealismSettings.json](../Tools/Island1RealismSettings.json). The ecology settings asset under `Assets/Generated/IslandEcology074` contains the authored gameplay protection. With the Environment scene loaded and Play Mode stopped, run:

```powershell
unity command run_script --file Tools/BakeIslandRealism.cs --entry BakeIslandRealism.Bake --timeout_ms 180000
```

Inspect the bake before saving the Environment scene. Unity may regenerate preview mesh serialization during an ordinary scene save, so review scene diffs if preserving other authored serialization is required.

| Added content | Source and license |
| --- | --- |
| Grass, shingle, ledge, driftwood, shell and coppercoil meshes, habitat and placement assets, shared materials | Authored by the project bake tool. Existing project terrain, vegetation and bark inputs retain their original terms. |
| Rock 030, 1K color and normal maps, plus a packed roughness and occlusion map | AmbientCG, Creative Commons CC0 1.0 Universal. [Asset](https://ambientcg.com/a/Rock030) · [License](https://docs.ambientcg.com/license/) · [Included notice](../Assets/ThirdParty/AmbientCG/Rock030/LICENSE.txt) |
