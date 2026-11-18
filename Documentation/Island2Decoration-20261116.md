# Island 2 environment decoration pass — 2026-11-16

## Latest revision — connected lake, river and cloudfall, 2026-11-17

The small source/terminal pools and 329 m river described below are superseded by a roughly 170 × 124 m headwater lake, a 1,051 m river, and an island-edge chocolate waterfall. This revision locally shapes a scene-specific terrain copy and regrounds affected scenery; historical statements below that terrain was unchanged apply only to the earlier decoration pass. See [the current water-system implementation and verification](Island2ChocolateWaterSystem-20261117.md).

## Current revision — wider flowing river and confectionery dressing

This revision follows the user's correction that decoration should emphasize candy, biscuits and chocolate. The initial pass counts and slower flow documented below are historical.

- The same 329 m river is now 1.9 times its previous width. Source and terminal pools expand to 10 m and 16 m radii. Bank props move outward, and all three shallow crossing markers span the wider channel.
- Flow speed increases from 0.38 to 1.6 m/s. Stronger animated milk-chocolate streaks, changing normals and 7.5 cm surface folds make downstream motion easier to see. The material remains opaque and chocolate coloured. Sixteen consecutive Play-mode frames were captured to inspect actual motion.
- 199 new confectionery groups replace most of the previous natural groves: bevelled chocolate-chip biscuits, layered biscuit stacks, leaning waffles and segmented dark/milk chocolate bars. There are now 10 added trees, 125 added rocks and 53 shrubs, down from 102 / 286 / 154 respectively. Original candy gardens and interactive objects are preserved.
- Biscuit bodies, baked pore texture, chocolate chips and bevelled chocolate segments are reusable generated assets. Waffles reuse the project's Kenney Food Kit (CC0, local licence in `Assets/Game/Resources/SecondIslandProps/Kenney/License.txt`). No external downloads or purchases were needed. Grounding uses actual mesh vertices and slope normals, avoiding the overly large bounding boxes of rotated round biscuits.
- Current generated decoration uses 630 groundcover cells, 23 supported materials and approximately 1.60 million unique vertices across both LODs. Packed mesh libraries total approximately 110.6 MB. Added scenery contains 221 simple colliders; small grass/debris and the river remain non-colliding.
- Final verification: all 1,119 route probes clear; all three real-player shallow crossings pass (about 24 m each); final isolated Play session has no errors/exceptions/asserts; formal saves unchanged. Editor median frames at river / arrival / coast remain approximately 16.5–16.7 ms in this machine's paced Editor session. This does not replace a standalone GPU benchmark.

Current screenshots and continuous flow frames are under `.codex/island2-decoration/playtest`. The scene before this correction is backed up locally in `.codex/island2-decoration/candy-revision/before.unity`.

## Audit before authoring

Live Unity 6000.3.23f1, active scene `Assets/Scenes/Second Island/Second Island.unity`. Existing modified scene, characters, materials, animation and unrelated files are preserved. A pre-pass scene copy and screenshots are kept locally in `.codex/island2-decoration`.

The island is a very large cotton-candy cloud island, not a conventional sea-level tropical island. Ground bounds are approximately 2471 × 1876 m. Its playable central valley contains candy groves, residents, Zhu Bajie, Huang Mei, camp, cottage and the northern hop challenge. Cloud-sea outlooks are far beyond that valley. Existing natural dressing consists of only 16 rock/stump groups and a handful of cottage/outlook props. Most slopes and connections are bare, with little middle-scale vegetation, grounded debris, edge definition or environmental storytelling. Original candy clusters are repetitive and concentrated near arrival. Cottage and distant outlooks supply existing landmarks, but intervening space has weak visual guidance. Boss arenas require open sightlines; camp, awakening, collectibles and hop challenge require undisturbed ground and interaction space. The shared pink terrain treatment makes distant regions look alike.

## Research and adopted direction

Searches included tropical island game environment, stylized realistic island, coastal rock/vegetation layouts, fantasy island ruins/wreckage, surreal/chocolate rivers and dessert natural environments. Reference images were viewed through web image search. Images are reference only; no reference artwork is imported.

- [Marooned developer environment update](https://www.indiedb.com/games/marooned/news/marooned-progress-update-2-tweaking): irregular coastal rocks, plants gathering around rock bases, weathered ruins and open sandy walking space. Adapt this composition to cloud cliffs, rather than replacing the island's geography.
- [Tropical shoreline reference](https://www.moddb.com/news/update-127-steam-workshop-map-editor-steam-achievements): large rocky silhouettes with intermittent tree groups, open foreground and uneven shoreline density. Reference only.
- [Friars melted chocolate photographs](https://www.friars.co.uk/blogs/news/how-to-melt-chocolate-like-a-pro-no-burnt-bits): warm cocoa body colour, continuous broad highlights, rounded folds and slow viscous ribbons. Use these cues, avoiding transparent brown water, white surf and copying film-specific sets.
- [Poly Haven licence](https://polyhaven.com/license): CC0 assets permit commercial use and redistribution. Reuse the project's existing moss rocks, stump, wooden crates, lantern, shrub and chalk cliff; retain their existing source manifests.
- [ambientCG licence](https://docs.ambientcg.com/license/): CC0 PBR grounds are a legal future source for gravel/soil transitions. Existing project maps suffice for this pass; no purchase or new third-party download is needed.

Design: dusty sage and copper foliage against pink slopes; dark cocoa mineral banks; crooked groves, exposed cliff ribs, driftwood and an abandoned small freight wreck. The river is a local natural anomaly, with a weathered collection station and oversized sampling ladle, rather than an island-wide candy theme. Preserve original arenas and routes through authored exclusion zones. Use shared meshes/materials, combined spatial detail cells and LODs; decorative small details have no colliders.

Implementation and measured validation results are recorded below after authoring.

## Delivered scene

The saved scene now contains one independently removable root, `Island 2 - Environment Decoration`. Other island scenes, original terrain, enemies, triggers, rewards, NPCs, camp and challenge objects are preserved.

- 102 crooked/upright coastal trees, 286 individually grounded moss boulders and 154 shrub groups. Their placement follows authored valley groves and intermittent outlook approach clusters, with large clearings between them.
- 6,999 grass tufts, 939 reduced fern clumps, 230 stranded branches, plus shingle patches. These are baked into 655 spatial material cells with near/far meshes, rather than thousands of updating scripts. These numbers count source placements, not individual runtime objects.
- Windward, northern and southern cloud-coast routes have irregular vegetation/rock groups and drift wrack. The pre-existing cloud sea and outlook structures remain.
- A broken freight skiff at approximately X=-246, Z=1970 uses exposed ribs, torn deck planks, a broken keel/mast, freight crates and scattered timber. A weathered sampling A-frame and oversized brass ladle at X=17, Z=1963 provide a restrained absurd landmark.
- Grove rootbeds use low-opacity feathered deposits sampled onto the original ground. All new material variants are separate assets; original Island 1 materials/meshes remain shared and unmodified.
- Large trees and boulders use 205 simple primitive colliders in total. Groundcover, twigs, cargo scenery, ford stones and the chocolate itself add no collision meshes. The river is a shallow, freely crossable spill; it adds no swimming, damage, slowdown, quest or save logic.

## Chocolate River — The Couverture Run

Approximately 329 m of winding ground-following channel descends from the split-rock spring at X=40, Z=2005, Y=2519.14 to a chocolate seep/eddy at X=-140, Z=1781, Y=2467.85. The approximate 51 m height change follows the existing landscape. Its controls pass west of the cottage and through the central slope; the original terrain/collider is not carved or replaced.

The existing ocean shader is an open-ocean Gerstner-wave system. Its transparent/deep-water and wave behavior would make a poor viscous shallow chocolate stream. This pass therefore uses a dedicated small URP material/shader, retaining the project's lighting/fog/environment colour integration and reusing its terrain-overlay approach for the banks. No ocean settings or common water materials change.

`Assets/Shaders/SecondIslandChocolate.shader` supplies opaque warm couverture colour, broad slow moving folds, low-amplitude vertex displacement, continuously changing surface normals, PBR lighting, warm cocoa-butter highlights and restrained clearcoat. Flow is about 0.38 m/s; waves are centimetre scale. This is GPU animation driven by Unity time, with no per-frame CPU mesh generation or allocations. Opaque shading avoids muddy-water transparency/refraction. Sparse chocolate domes, stained gravel shoulders, spring geology and flat exposed stepping shale finish the banks. Three marked shallow crossings keep the valley traversable.

## Validation and scope

- Unity 6000.3.23f1 recompile completed without compiler errors. All 20 decoration materials resolve supported shaders; mesh checks found no non-finite vertex data. Both realistic and the existing alternate visual style were rendered for review. A missing shader import discovered in an intermediate visual pass was corrected; final captures have no error-material patches.
- Six principal route centerlines were sampled every roughly 3 m with player-sized capsule overlap probes: 1,119 probes, zero new decoration obstructions. Original obstacles were intentionally excluded from this new-decoration check.
- An isolated Play session used the real player's W input at three river crossings. Forward travel was approximately 24 m at each, with valid ground support. No runtime error/exception/assert messages occurred in the final measured session. Formal save files remained byte-identical; the editor returned to the saved Second Island scene, stopped and clean.
- Before/after serialized-scene comparison found no changes to existing gameplay objects or terrain. Unity's existing edit-time ocean component regenerated its scene-local mesh ID during saving; the ocean mesh payload was verified identical. Only the new decoration root and its objects are substantive scene additions.
- Near grass uses the project's existing middle mesh; fern clumps use its existing reduced mesh. Groundcover cells switch detail around third-person viewing distances and cull beyond approximately 200 m. Distant tree LODs and small shrubs omit shadow casting. Shared materials enable instancing where supported; spatially combined detail cells reduce object/draw overhead. There are no new Update components or spawned runtime decoration systems.
- Generated geometry contains approximately 1.61 million vertices / 1.74 million triangles across all unique near/far assets, not simultaneous visible geometry. Five binary mesh libraries total approximately 112 MB, reduced from the first 492 MB text-mesh output. Existing textures are reused; there are no new external downloads or paid assets.

Final Editor samples used 35 frames per state/view, toggling only the new decoration root:

| View | Without decoration median | With decoration median | Additional reported batches | Additional reported triangles |
|---|---:|---:|---:|---:|
| River | 16.76 ms | 16.71 ms | ~519 | ~3.75 M |
| Arrival | 16.60 ms | 16.69 ms | ~360 | ~1.29 M |
| East cloud coast | 16.72 ms | 16.66 ms | 36 | ~0.30 M |

These Unity Editor counters include rendering/shadow passes and potentially other visible Editor views. The similar ~60 Hz frame times are subject to frame pacing and do not establish zero GPU cost. This is targeted route, visual and Editor performance validation, not a standalone-build GPU benchmark or a full campaign/Boss regression. New rendering and approximately 112 MB of source mesh resources are a real additional cost.

## Review locations and authoring

Prioritize the arrival-to-camp view, the cottage/rock spring, all three river fords, the terminal seep near Zhu Bajie's valley, the stranded skiff west of the cottage, and the east/north/south cloud outlook approaches. Also fight both Bosses near the arena margins to assess foliage/camera readability under active combat.

Authoring entry points: `Tools/BuildIsland2Decoration.cs` (`Run`, `Polish`), `Tools/InspectIsland2Decoration.cs` (`Run`, `RouteClearance`, `PackMeshes`, `PruneUnusedArchiveMeshes`) and `Tools/Island2DecorationAudit.cs` (`Before`, `After`). Run through the Unity CLI `run_script` command. `BuildIsland2Decoration.Run` rebuilds only its own named root and refuses an unsaved scene; it reuses binary library meshes by name. Use `PackMeshes` after introducing new mesh names, then `PruneUnusedArchiveMeshes` to remove obsolete generated subassets. Do not hand-edit scene/prefab/native asset serialization.

The repeatable isolated Play check is `VerifyIsland2Decoration.Prepare()` in `Assets/Editor/VerifyIsland2Decoration.cs`. Local audit, scene backup, screenshots and measurements reside in `.codex/island2-decoration`; save snapshots there are private and should stay local. No standalone executable was built and no commit/upload was performed.
