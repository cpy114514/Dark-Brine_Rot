# Combat, aerial chops and wearable Nailong drops — v265

User scope: resume running after the twin-blade rush; slightly increase Nailong damage; repair intermittent moving charge; remake both weapon jump chops; make Nailong equipment visibly wearable. Later clarification requires integer damage. Existing rifle zoom and UI work from the shared workspace is retained.

## Runtime changes

- After the twin-blade damaging rush window finishes, an upper-body recovery layer continues the same authored cut while the normal locomotion layer resumes walking/running. The damage window, 16 m full/longer partial rush, wall collision and travel-only invulnerability remain. Cancel, disable, swimming and completion clear this layer.
- Ground charge now tolerates a nearby walkable step-down (at most 0.25 m, limited by controller step offset), including landing/recovery state ownership. A non-allocating sphere probe checks real support; jump, roll, air flip, swimming, external ownership and an actual fall remain excluded. The held-RMB buffer from v261 remains. Ground transitions no longer force the charge legs into an aerial state while that support is valid.
- Nailong authored base damage increases from 6 to **8**, sonar from 4 to **5**. Existing per-attack multipliers, player mitigation and recovery protection remain. Player hostile damage is rounded after mitigation; generic enemy Health and Nailong incoming damage also round to whole units. Narrative damage budgets retain their existing path.
- New full retained-rig actions `PLAYER_ClubJumpChop_v265` and `PLAYER_BladeJumpChop_v265` replace v003 only in the three existing gameplay controllers. Both retain 60 FPS / frames 1–52 / 0.85 s, in-place physical jump ownership, normalized 0.34–0.67 weapon contact windows and original stamina/damage rules. Restrained head/torso motion and a compact asymmetric leg tuck replace the rejected exaggerated bend; club remains single-hand, blades use separate overhead lanes. Originals and other actions remain intact. The existing combination installer points at the new revision to avoid reverting it.

## Wearable loot

- The three ordinary Nailong drops (round glasses, trousers/garment and clogs) now support Equip/Unequip in the existing I-menu instead of being collectible-only. Existing Weakness Glasses retain their ability; head slots are mutually exclusive.
- Generated wearable display meshes reference the original materials, with a head mount, hip/thigh weights and two separate foot bindings. Models follow the native skeleton; there is no runtime procedural limb posing. No cosmetics add colliders, damage or new combat stats.
- `GameWorldProgress.Snapshot.wornItems` persists item IDs. Restore verifies existing inventory ownership before equipping. Old saves without this field safely restore no ordinary worn items. Pickup ownership and current equipped weapon remain separate.
- Original source models remain. Their approximately 500k/500k/50k faces are represented by separate display LODs of roughly 7k faces per item. Shoe components are separated along their actual source-pair axis, then fitted to each foot's toe direction. Generated meshes are updated explicitly through Mesh arrays: `CopySerialized` alone failed to update native vertex data in a rejected intermediate take.
- Generation binds against the running native actor pose; Edit-mode prefab-only sampling showed an incompatible reference pose and is retained as rejected evidence. Final generated assets are saved through Unity APIs. World drop models are unchanged.

## Verification status

Evidence root `.codex/combat-v265/`. Actual native input tests, source and native animation sheets, UI tests and save audits are retained there. Final acceptance is recorded in `acceptance.json` when complete. Earlier failed takes and intermediate visual fits are not final results.

Blender MCP at localhost:9876 was unavailable; the existing working Blender 5.2.2 completed authoring, previews, validation and FBX export without downloading or replacing the installation. Source work is versioned under `ArtSource/Combat/v265/`; stage is SECOND PASS, not human artistic FINAL approval.

Final acceptance: 60 Hz native input replay passes (1,749 frames; 51 records including one damage/travel measurement). The 30 Hz replay passes (887 frames, same contracts). The replacement jump-chop replay passes 25 actual input/contact/recovery checks over 371 final SRP camera frames, including simultaneous jump/attack, falling input, per-victim real swept contact, miss handling and single-hand club spin regression. Equipment UI passes 27 checks (actual button handlers, equipment state, one model per slot, valid bone bindings, geometry budget, no new colliders, old-save compatibility); three-controller animation checks pass 24 contracts. Native front/side garment, glasses and footwear fitting images were visually reviewed.

Test fixtures use isolated scenes and excluded players. Original five-scene workspace is restored, Editor is stopped, and campaign save is byte-identical to the post-stop human backup. Existing shared rifle zoom/localization changes and v262-v264 opening work are preserved. No second Unity Editor, rebuild, commit or upload.

Limits: no full-island boss playthrough, Windows Player rebuild, hardware FPS measurement or exhaustive equipment/weapon/body intersection audit. Source/export review is SECOND PASS, not human artistic FINAL. An intermediate rifle sideways-run assertion ran out of stamina during its extended fixture; replenishing test stamina before the orientation check passes without changing rifle gameplay.
