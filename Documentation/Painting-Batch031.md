# Ink Wash and Van Gogh — batch031

The user requested two additional styles alongside Realistic, Dark Fairytale, Color Comic and B&W Manga. **Ink Wash** and **Van Gogh** are installed as separate selectable presets, with independent saved controls and actual native previews. No original character/environment geometry, materials, rigs, animation or gameplay is replaced.

## Rendering and player controls

Ink Wash uses depth-guarded wet color sampling, luminance-based ink density, dry-brush variation, softened boundary ink, warm paper grain, distant mist and sky wash reduction. Low-light luminance is remapped before ink density so nighttime faces do not simply become solid black. Controls: **Ink bleed / Brush texture / Paper grain / Mist**.

Van Gogh is a procedural oil-paint interpretation: overlapping local elliptical dabs, curved sky flow, blue/gold palette shifts, impasto grooves and local color-range rejection around small features. Controls: **Brush size / Swirl / Impasto / Color richness**. Dark source-luminance preservation limits crushed shadows. This is an in-engine surface/image treatment, not replacement hand-painted models or a reproduction of a particular painting.

Both use the existing composed-world transform after transparents/postprocessing and the private complete opaque-depth copy. The same cached feature/material is reused; no added camera or geometry pass. Existing four style IDs/mode values remain; new numeric modes4/5 and IDs `ink-wash` / `van-gogh` are appended. The previous comic and Dark shader bodies are byte-identical to the prior checkpoint. Only dispatch adds the two new branches. Existing presets retain their exact bytes/GUIDs; all six matching previews were refreshed together, preserving the original four preview GUIDs.

Painting marks are anchored to the rendered screen, without time-random seeds. They move relative to surfaces as projection changes. They are not surface-locked textures, and ordinary wind/projection/final visibility continue to vary. Van Gogh uses nine local brush candidates with color/depth guards; this is additional shader work, not a performance optimization. No qualified isolated GPU/FPS/GC result is claimed.

## Six-card gallery and confirmed dropdown repair

Six styles use a two-row, three-column gallery with compact preview/text panels. All six cards stay within the tested Game view and have no overlaps. Four detail sliders show style-specific labels. Every one of the36 ordered comparison pairs works with aligned static captures and no extra scene cameras. The actual native sixth-item toggle selects Van Gogh after scrolling.

**QA-043: confirmed/fixed last dropdown row clipping.** The default UI factory content was28 units high, while existing style code enlarged each item to34. Unity derived negative top/bottom padding, leaving the last row clipped even at scroll position0. In the native2560×1440 view its bottom was920px against a924px viewport. Matching the template content to34 removes the negative padding. The final test scrolls the sixth entry fully into view and selects its actual Toggle.

An initial direct `Dropdown.Show()` was called before the newly activated control's Unity Start, so the factory alpha tween was uninitialized. That fixture invocation is rejected; the final helper awaits native frames after showing the comparison. The first UI result also used an already-open detail panel incorrectly; final helpers inspect state before toggling. Neither issue was treated as a production bug. Only the independently measured clipped row caused the production correction.

## Actual evidence and tests

![Native Ink Wash / Van Gogh comparison](../AUTO_QA/Evidence/batch031/comparison-ui.png)

![Six native style cards](../AUTO_QA/Evidence/batch031/gallery-ui.png)

All six1280×720 previews share fixed noon, the same frozen excluded Sahur pose/camera, intensity89.7673%. Exact camera positions/rotations, input/depth policy and native rendering callbacks are in `gallery-accepted.json`. Accepted noon, midnight, actual4-sample target/pipeline and independent320×180 pixel views were inspected. Nighttime remains dark, but preserves the actor's original silhouette and some face detail; this is not full encounter-readability acceptance.

**142 accepted native checks pass:**

- 47 rendering/preferences checks: two styles×four visible slider responses, both intensity responses, independent saved controls, manager native re-enable, saved-selection restore, six unique slots,24 cached-material switches, both camera-return thresholds, complete original Near grass layout, save-excluded fixture.
- 79 UI checks: six cards/previews, bounds/overlap, style-specific controls,36 comparison pairs, overlay proof color, fully scrolled sixth row, its actual Toggle and final Ink/Van Gogh reveal.
- 16 transparent-world checks: an actual cyan alpha-blended probe contributes visible pixels through off/on controls in both additions and old modes; ink neutralizes the color, oil retains color, Manga remains gray, all relevant transforms use complete depth after world postprocessing. These are rendering probes, not a battle-effect playthrough.

The two painting modes each have24 orbit positions. At the restored camera,2,693 Ink Wash pixels and1,547 oil pixels out of921,600 differ by more than2 channel levels. This rejects wholesale pattern reshuffling; it does not prove pixel-perfect temporal stability or measured game FPS. The actor was frozen; no smoother-animation claim.

The rejected first oil prototype rotated absolute pixel coordinates and produced dense interference rings. Final dabs rotate around their own centers with color-range rejection to protect eyes/small marks. The first wash was too pale, then its density was increased; an additional low-light remap was verified before final captures. Intermediate overlapping test counts are not added to142.

The UI source repair triggered a real Play-mode domain reload. The fixture remained excluded, the manager retained six styles, and foliage recache was awaited before more scene rendering. The one initial public object search returned null because the world manager is hidden; the qualified lookup uses Resources filtered to valid scenes. `after-domain-reload.json` records ready state and preserved single grass layout. This is not a new fresh-Play saved-selection test.

## Build and preservation

Same Editor17184, seven enabled scenes, Windows64 Mono Development: **Succeeded,66.09s,493 warnings and4 pre-existing missing-plant messages** covering606 instances. The new shader compiles both tested normal-keyword variants without messages. The binary was not launched; desktop capture does not qualify mobile/XR or standalone performance.

Original five First Island scenes are restored clean/stopped, timeScale1, zero owned objects. Fresh save/backup bytes match. All24 protected settings, original-material/prefab/profile/source/preset/GUID hashes match. All981 original project-registry kinds/values and the complete key set match after native original-campaign restoration and removal of one qualified unused fixture ledger. Eight temporary painting-control keys and the temporary saved-selection override were cleaned in finally blocks.

The build helper restored flags, original preloaded assets, ProjectSettings bytes and the known build-generated unused duck texture alias through native APIs; all four cleanup checks pass. No authored scene, model, UV, skeleton, original clip, collider or original material was rewritten. Existing full grass layout, large ducks, wave UI, comic styles and pixel mode remain.

Tools are `Tools/Art031.cs` / `Tools/BuildArt031.cs`. Their ignored `.codex/art031` root stores immutable snapshots, intermediate takes and the binary; do not reuse old snapshots or rerun completed unique labels. Evidence/proof contains only selected native images/results, never saves/private preferences/cache files.

Only this batch's manifest is checkpointed/synced. Local Plastic remains212 with older published pending patches and unrelated checkouts; independent cs213 stays unimported/untested. Use a scoped shelveset/server-side merge, never update this pending workspace. Next: actual gameplay/encounter readability and a qualified standalone cost measurement of the full grass plus painterly shaders.

Late sync inspection found unrelated ChickenBro/duck awareness-range edits in the shared working tree and a new external Plastic cs218. They are excluded from this batch, preserved byte-for-byte and not claimed tested by the painting acceptance. Do not revert them or include them in the owned manifest.
