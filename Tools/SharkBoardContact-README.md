# Shark contact and irregular wreck fragments — 2026-10-07

The live wreck now blocks the shark's animated head, body and tail against convex colliders built from the actual wood meshes. Bite and tail attack sweeps damage timber once per active attack. Two normal successful strikes can destroy an intact deck fragment; breach/finale contacts can break unprotected timber immediately. Player damage uses the solved shark pose, after wood contact correction.

`Tools/BlenderShipWreckV2.py` cuts the original ship into 20 unequal angled deck fragments, 24 hull fragments and 158 original details. Deck cuts use jittered Voronoi seeds projected onto the actual source mesh. Each deck/hull has 3–5 separately cut child meshes, 184 total. Original materials and UVs remain. Source: `ArtSource/ShipWreck/ship_g_fractured_v002.blend`; old ship and Blender master are retained. `Tools/ImportBlenderShipWreck.cs` imports `.codex/ship-wreck-blender-v002/fragments.json` into the active Resources asset while retaining existing base mesh GUIDs.

Broken pieces inherit their parent's location, rotation and point velocity, receive the contact impulse and fall under gravity. Larger children remain usable for traversal. Breaking an occupied plank releases its rider. Boarding and support use the actual deck footprint and surface, rather than its rectangular bounds. Existing softened buoyancy remains in place.

Unity verification:

- `VerifyShipWreckSource.Verify`: 202 base fragments; source mesh reconstruction maximum error 0.0613 m, original ship unchanged.
- `VerifySharkBoardContact.Geometry`: 184 child meshes, all deck UVs present, maximum projected area discrepancy 0.122%, 20 distinct deck dimensions.
- `VerifySharkBoardContact.Fight`: real AI attacks broke a real isolated ship board after three attacks; rider released, five playable child pieces retained gravity; maximum final anatomical collider penetration 0.000962 m.
- `VerifyStory1WreckTraversal`: surfing, steering, jumping, board crossing, swimming, climbing, normal combat and release on low health passed. This control-only fixture disables shark/wood correction; the separate fight test verifies physical contact and destruction.
- `VerifyStory1WreckFinale`: ground/low-health and swimming/random-timer entries passed, including pause, three hops, two breaches, defeat, floating pose and unarmed island handoff. Maximum solved anatomical penetration was 0.00101 m and 0.000798 m respectively; five/four wood pieces broke.

Finale staging walks to a board edge using the existing walk animation, faces toward open water after landing and sends the tail-strike body path outside the support board. The core 20.8 s animation clock and existing rigs/actions are preserved; lead-in duration follows actual walking distance.

Actual Unity LateUpdate captures and results: `.codex/encounter-contact-v004-edge/` and `.codex/encounter-contact-v004-water/`. The game-camera video is `encounter-contact-v004-edge/encounter-gameplay.mp4`. Capture occurs after shark contact solving. The existing large ocean swell can still briefly obscure the first exchange and later tail beats; the current ocean style/height was not changed in this contact revision. Measured collider separation does not prove every rendered fin or weapon contact is free of occlusion.

Current task save backup: `.codex/shark-board-contact-20261007/backups/save.json`, SHA256 `B238EDEAAEB57E24FE0F482004EF2017E044B917BF213E30915896DD2C5CDD50`. Restore after stopping Play Mode, because exit may flush queued save data.
