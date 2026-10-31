# Origami surface rendering — batch029

The user reported that Origami looked weak and did not resemble folded paper. Native first-island captures confirmed a functional defect as well as weak material treatment: most ground and grass bypassed the paper pass.

## Confirmed cause and repair

SSAO causes the PC renderer to produce a depth-normal prepass. The custom island terrain and foliage shaders lack a DepthNormals pass, so their pixels were absent from the depth sampled by the illustration shader. Its far-depth guard consequently returned their original color. Requesting only Depth on the feature did not fix this while SSAO still required the prepass.

Origami now copies the actual opaque depth into a private transient R32 texture using URP's native CopyDepthPass, including its MSAA resolve. This texture is sampled only by Origami; URP shared camera depth and other styles remain intact. Surface normals come from reconstructed world-position derivatives. There is no added scene geometry pass or material conversion.

A qualified diagnostic rendered valid geometry gray and retained color for bypassed pixels. In the same foreground grass/ground region, the old depth path produced **0/80,000 gray pixels**, while the complete opaque-depth path produced **80,000/80,000**. See the original native `depth-before.png` / `depth-after.png` and their reports in [batch029 evidence](../AUTO_QA/Evidence/batch029/proof.json).

Paper shading now uses triangular world-projected cells, alternating folded lighting, depth-guarded cell-center color sampling to suppress photographic texture, matte tonal/chroma bands, warm highlights, sage greens, crease edges and footprint-faded fibers. The sampling rejects a cell center on another depth surface to limit silhouette bleeding. It replaces the earlier parallel diagonal striping exposed by the coverage repair.

This is a paper/papercraft **surface treatment**. It does not create folded-model silhouettes, geometry thickness or physically folded grass blades. Original meshes, UVs, rigs, source clips, colliders and gameplay are preserved.

## Actual visual acceptance

- Native 1280×720 captures of four styles at two camera poses, fixed noon, same excluded Sahur pose, intensity89.7673%. Camera framing is shared across each set; the player camera derives from the actor's renderer bounds through `Origami029.Arrange`.
- Before/after Origami shows ground triangles and flatter character lighting; neither image is an AI-generated concept. Both are included below. The other three shader branches are unchanged; separate captures still differ slightly due to live world/SSAO/grain effects and are not claimed pixel-identical.
- Final-source 4×MSAA uses both an actual4-sample target and4-sample pipeline. Independent pixel mode uses the installed320×180 world target. Both retain readable character and environment rendering with valid native camera callbacks and nonempty-image controls.
- A24-position camera orbit and return were rendered. Return differs at6,230/921,600 pixels by more than2 channel levels. This rejects wholesale pattern reshuffling; it does not establish perfect temporal stability or measured gameplay FPS. Five representative original frames are retained.
- Four current gallery previews were regenerated from the same native final capture; their existing Unity GUIDs are unchanged. Actual four-card UI, all16 comparison pairs, Origami/Realistic divider and unmodified red overlay color pass native checks. Gallery and comparison Game-view images were inspected.

![Before: incomplete world coverage](../AUTO_QA/Evidence/batch029/origami-before.png)

![After: complete depth and triangular paper treatment](../AUTO_QA/Evidence/batch029/origami-after.png)

## Executed checks and limits

**54 native checks pass:**30 render/control/lifetime/grass-preservation checks plus24 gallery/comparison/UI checks. These include all four detail controls, intensity,20 style switches retaining the same cached transform/depth-copy materials, complete-grass single-layout preservation and a save-excluded fixture. Original V2's141 checks are historical, not an additional execution in this batch.

The same Editor17184 built all seven enabled scenes as Windows64 Mono Development: **Succeeded,91.20s,493 warnings and4 pre-existing missing-plant messages** covering606 instances. The illustration shader has no compiler messages. The binary was not launched; no new standalone runtime or FPS acceptance is claimed.

The additional Origami rendering work is one fullscreen depth copy and an R32 pooled transient texture, approximately7.91MiB at native1920×1080. Revised paper shading also adds color/depth sampling. No qualified isolated GPU/FPS or zero-allocation claim is made. Realistic enqueues no transform, Dark retains its normal/depth path, and Monochrome retains its postprocessing path. Mobile-device/XR/reversed projection variants and a full active boss encounter remain untested in this batch.

## Restoration and reproduction

`Tools/Origami029.cs` drives a guarded, excluded Environment/Lighting fixture, capture, verification, UI and restoration. `Tools/BuildOrigami029.cs` uses the current Editor without changing build target. Fresh labels and snapshot guards prevent overwriting prior evidence. For another run use a new private batch root, snapshot current state, then Prepare → enter Play → confirm state → Stage → Arrange → freeze fixture noon → Capture/Verify/UI → stop → RestoreWorkspace. Do not reuse an old snapshot over newer user state. `RestoreBuildAlias` restores only the known build-generated unused duck `_MainTex` alias and refuses other material changes.

Two unused healing-pack campaign ledgers created by automatic Play initialization were identified against the original preference key set; their IDs were absent from both protected saves. Cleanup first restores the original native cached campaign preference, then removes only those two qualified unused keys. An initial cleanup guard correctly refused the fixture's cached active ID; nothing was deleted on that rejected take. All980 original registry value kinds/values and the complete key set match afterward. No private preference values are published.

The original five First Island scenes are restored clean and stopped, with zero fixture objects. Fresh post-user-stop save and backup bytes match. All16 protected source/settings/material/prefab/preset hashes match; preview GUIDs match. Build settings/preloads are restored, and the known build-only duck texture alias is restored through the native material API. No authored scene was saved.

Rejected probes remain private: the initial actor-in-water/unsettled foliage views, candidates reset by pipeline recreation before warmup, ineffective depth-input-only attempt, and malformed helper invocations. Accepted capture helpers warm the pipeline and verify the actual installed candidate material/pass. Earlier night captures are valid night observations, not fixed-noon comparisons. The final noon captures use the final24/world-unit fiber frequency.

Only the two renderer/shader sources, four derived preview PNGs, documentation, QA records, external helpers and selected native evidence belong to this patch. Preserve existing grass028 changes, unrelated Plastic checkouts, original assets and606 missing plants. Local Plastic base remains212; publish an owned shelveset by server-side merge, never partial-update this pending workspace. Independent cs213 work remains unimported/untested locally.

Next: active combat/Boss/tutorial readability across paper lighting and a qualified standalone cost measurement with the complete grass layout. Stronger folded silhouettes require a separately scoped art/geometry change; they are outside this surface-only patch.
