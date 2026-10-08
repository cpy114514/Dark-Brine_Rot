# Wreck contact v004 integration review

User requested blocking shark/wood penetration, less regular ship fracture shapes and the ability for shark attacks to break floating wood. Scope was approved directly; no new character rig or animation Action was reconstructed.

Integration passed actual Unity gameplay and cinematic regression checks. The edited ship source is `ArtSource/ShipWreck/ship_g_fractured_v002.blend`; existing v003 character and shark animation assets remain active. Live attacks now apply damage from their solved anatomical pose, occupied boards release their rider when broken, and child fragments use the original ship geometry and free fall.

Actual review artifacts: `.codex/encounter-contact-v004-edge/preview/`, multi-view contact snapshots in its `multiview/` directory, and its `encounter-gameplay.mp4`. Ground/low-health and water/random-timer verification results are in the corresponding `verification.json` files. Measured final anatomical shark/wood penetration stayed below 1.1 mm across these runs. The independent normal-combat test also broke a real deck board and preserved five usable gravity-enabled fragments.

Frame-specific revision: the second counter at approximately core 8 s reads more clearly with Sahur at an actual board edge and the shark nose outside it. Walk-in uses the existing locomotion clip; hop landings face the nearest outside edge. Tail approach keeps the body path farther outside the occupied board. Pause now preserves the last solved shark placement in addition to the sampled pose.

Remaining visual observation: the pre-existing large wave field still briefly covers parts of the first exchange and some later tail beats. Review frames near core 2 s and 15.6–18 s demonstrate that occlusion; it should not be interpreted as proof of an anatomical collision failure or as full animation art approval. Ocean amplitude and animation production scope were preserved in this targeted integration revision. Final status: contact/destruction integration verified; human art approval is not assumed.

See `Tools/SharkBoardContact-README.md` for source contracts and test details.
