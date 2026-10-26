# Tutorial and checkpoint comfort v253

## Playable changes

The wreck walking lesson now offers an eight-second control window after the first movement input, retaining the existing two-metre movement requirement. Overlays pause the lesson clock. A temporary walking constraint projects controller motion onto the real hull fragment with a capsule-sized inset. Jump and dodge inputs are held back only in this walking lesson; its restriction is removed before cinematic positioning, later intentional jumping, swimming and normal island play.

Attack staging starts from the player's actual feet after exploration, then retains the existing turn, authored walking clip, turn-to-shark and weapon-contact preparation. The ordinary and heavy tutorial strikes still require their native final rendered contact confirmation.

Healing supply capacity is three. Actual respawn refills the same campaign's supply and preserves the existing full health/stamina and equipment behavior. The persistent ledger records both consumption and refill, so an older checkpoint cannot refund spent packs or reverse a completed respawn refill. Calling Respawn on a living actor does not refill anything. HUD and tutorial/death text use the new capacity.

## Checkpoint landmark

A compact stone watchtower from [Kenney Pirate Kit](https://kenney.nl/assets/pirate-kit) is imported with its original CC0 license and atlas. Only the selected FBX and atlas are included, rather than the whole pack. The prefab is 7 m high and about 5 m wide in this game's character scale, with 748 vertices, one mesh renderer and a simple solid collider. Materials use URP Lit, receive lighting/shadows and retain the small palette atlas.

The tower sits 12 m from the existing island checkpoint. The checkpoint's x/z, identity and activation behavior remain; its ground height is aligned to the upper native terrain shell, correcting a 0.30 m ground mismatch. The measured horizontal clearance from spawn to tower collider is 10.45 m. Only Environment is saved through Unity APIs; no scene or prefab YAML is edited by hand.

## Native verification

Evidence is retained under `.codex/comfort-v253/`.

- `native-take-01/result.json`: 17 checks pass in the existing Editor. 241 native walking samples span approximately eight seconds. Alternating diagonal movement plus repeated jump/dodge presses produce zero swimming samples and zero samples outside the actual deck footprint. The boundary is released before attacks. Authored automatic walking is observed; both tutorial strikes confirm contact, with the final recorded gap approximately 0.000031 m.
- The same Play session runs real death/respawn twice on an isolated actor. Three-pack capacity, depletion, no refund from an old checkpoint, refill after each respawn, preserved campaign identity, no refill for a live Respawn call, full health/stamina and the 3/3 HUD format all pass.
- `checkpoint-review.json` and native screenshot verify terrain alignment, landmark scale and collider clearance. `tutorial-review.jpg` reviews native walking/positioning/contact frames. No shipping build or exhaustive wreck-layout/performance certification is claimed.

The first native test command exceeded the Pipeline dispatcher timeout, but its already-started observer completed successfully; the actual result and images, not that tool timeout, establish the test outcome. A diagnostic initially looked for the prefab instance's name with spaces, while Unity used the asset filename; the lookup and installer idempotence were corrected. Teardown initially released its render target after the story camera had been destroyed; the helper now detaches that target before stopping Play. No production test failure is hidden as a pass.

Reopening the island reports three pre-existing missing grass prefab GUIDs (2bde5fdeb204bc6418608a5fe3993ad1, 8e67ad946c4264b479267ee1643fd2ce, ad3d230ef428f6049ab82c5136f00a47). Their reference counts match the byte-backed-up Environment from before this work; this revision does not replace them. See `preexisting-grass-references.json`. Consequently the full environment is not certified error-free, despite the passing feature tests.

Human save bytes and the original campaign preferences are restored after the test session; existing island scenes are reopened for editing. No commit, check-in, push or new automation is performed for this revision.
