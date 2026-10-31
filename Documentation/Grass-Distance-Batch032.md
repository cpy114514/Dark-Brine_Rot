# Extend grass visibility — batch032

The user reported obvious nearby grass loading and requested a longer rendering distance. The actual player camera used FOV60.4065° and lodBias0.5. Native grass groups had size8.5595 and a final screen-height threshold near0.092659, giving an approximately39.67m final distance; fading began around30.70m. The outer WorldPerformanceManager grass cull is already180m and was not the limiting boundary in this case.

## Minimal change

Only `WorldPerformanceManager.ConfigureGrassLod` changes. The verified complete-grass family's final screen-height threshold is halved, doubling its native LOD visibility distance. Its new band starts at75% of the final distance. For the captured player camera this is **59.51–79.35m**, instead of30.70–39.67m. No global quality/lodBias, source mesh, density, material, grass placement, tree LOD or gameplay parameter is changed.

The original complete Near renderer remains the only visible grass layout; thinner tiers remain disabled. The existing authored/native ownership restoration remains intact. Other mesh families and deliberately authored CrossFade groups are excluded. The outer180m bound is retained, so high-lodBias cameras can still be limited by that bound; distances also depend on FOV, group size and scale. This is not an unlimited terrain-detail draw setting.

## Evidence and tests

The user session was stopped normally and fresh post-stop save/backup snapshots were preserved. Tests used isolated Environment/Lighting, fixed noon and a save-excluded fixture. Matching native1280×720 captures use the exact previously observed player camera position, rotation, FOV and lodBias0.5. Images show grass covering the formerly bare strip farther up the slope. Clouds/wind are live, so whole-scene captures are not pixel-equality controls.

![Before, original player camera](../AUTO_QA/Evidence/batch032/before.png)

![After, fresh Play and original player camera](../AUTO_QA/Evidence/batch032/after.png)

**39 explicit native assertions pass:**

- 30 policy/ownership assertions across repeated recognized, authored-CrossFade and unrelated-mesh fixtures. Verify doubled distance threshold, new fade width, original Near layout, suppressed alternate renderers, idempotence, exact membership/enabled/mode/threshold restoration and exclusion of unowned groups.
- 9 fixed-projection image assertions. A native cloned patch uses copied materials with wind disabled and a size-only LOD sweep, so projection is identical. The old policy has zero visible patch pixels at equivalent41m; the new policy retains10,032 pixels at50m,9,752 at60m,4,733 at70m and197 at79m, then zero at81/90m. The20m positive control is10,032 pixels. This demonstrates a farther gradual final fade, without mesh replacement. Equivalent distances use the measured representative group and player FOV/bias, not a camera physically moving through the patch.

A separate **fresh Play** audit finds11,298/11,298 groups owned by the manager, zero wrong thresholds/layouts, all six styles available and no savable gameplay actor. The fresh same-camera image reproduces the79.35m result. These field counts are additional observations, not39 more assertions.

The native scene camera was sampled at24 advancing/turning/returning positions over a16m excursion. Each capture waits for a complete bounded foliage refresh and real camera callbacks. This verifies sampled composed frames, not continuous gameplay frame pacing or asynchronous-stream latency. Representative images and exact positions remain in evidence. B&W Manga with the independent320×180 pixel target also renders the extended field.

More complete grass is drawn: at the original camera, the potential range-only full/fading group count is89/47 before and233/86 after. Potential source triangle totals ignoring frustum rise3.14M→7.37M. These are geometric visibility estimates, not actual GPU submissions or FPS. No performance improvement or unchanged-FPS claim is made; the request deliberately extends full-detail coverage.

## Rejected probes and remaining suspicion

The first isolated capture inherited lodBias2 rather than the player's0.5, and was rejected as a matching baseline. Final helpers explicitly apply only the captured runtime bias and restore it afterward. Initial boundary lookup selected a native group absent from the managed ownership dictionary and failed before production mutation. The qualified boundary test uses a registered group and a fresh unique label.

After the real source domain reload, all11,298 native groups had the correct new layout/threshold, while the managed ownership dictionary contained only12 entries despite a ready71,326-entry foliage cache. This is a **suspected Editor reload ownership issue**, with no established original cause; this distance-only patch does not claim to fix it. Fresh Play rebuilt all11,298 ownership records correctly. Do not treat the hot-reload dictionary as lifecycle acceptance. A later focused reload investigation should use before/after positive controls rather than guessing. Normal new Play and isolated policy restoration are verified here.

## Build and preservation

SameEditor Windows64 Mono Development seven-scene build **Succeeded,86.43s,493 warnings and4 existing606-plant messages**. All build flags/preloads/settings/duck-alias cleanup checks pass. The binary was not launched.

All26 protected hashes match. All982 original native registry kinds/values and the key set match after removing two qualified unused fixture healing ledgers and restoring the original native campaign cache. Original five scenes are clean/stopped, timeScale1, zero fixtures, fresh save+backup unchanged.

Build/restoration results are recorded in `AUTO_QA/Evidence/batch032/build.json`, `build-cleanup.json` and `final-audit.json`. Only one existing Editor is used. No standalone FPS, mobile/XR or full active-combat acceptance is implied by a successful Windows build or sampled view.

Original scenes, fresh save/backup and native preferences are restored; exact final counts/hashes are in proof. Private snapshots, the build and rejected captures stay in `.codex/grass032`. Existing unrelated ChickenBro awareness/duck prefab changes are preserved and excluded from this checkpoint, as are unrelated Plastic settings/Recovery files and606 missing-plant references.

Only this batch's source/helper/docs/evidence manifest is synced. Local Plastic remains212 with published patches pending; no workspace update. Use a scoped shelveset/server-side merge and preserve independent external work. Next: qualify standalone cost of extended full grass and investigate the Editor reload ownership observation in isolation.
