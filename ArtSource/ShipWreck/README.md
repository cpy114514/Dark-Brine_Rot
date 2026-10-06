# Original ship fracture source

`ship_g_fractured.blend` is the actual project ship split in Blender into separate
objects. The "Original ship - reference" collection preserves the hidden intact
geometry. "Actual ship fragments" contains 20 deck sections, 24 hull sections and
158 pieces of rails, cabin, masts, sails and fittings at their original positions.
Original ship textures are packed into the file.

The original source is `Assets/Game/Prefabs/Environment/Props/Ship/ship_g.fbx`.
The source FBX and its existing materials are unchanged. Cuts preserve original
UVs and materials. Deck surfaces have undersides and closed cut edges. Small
hardware in detail pieces is simplified for the live encounter.

Run Blender in background with `Tools/RunBlenderShipWreck.py -- --build` to rebuild
the native file and `.codex/ship-wreck-blender/fragments.json`. Run
`ImportBlenderShipWreck.Build` through Unity Pipeline in Edit Mode to regenerate
`Assets/Resources/Story1Wreck/ShipWreckPieces.asset` and its referenced mesh assets.
Run `VerifyShipWreckSource.Verify` to validate source geometry, textures and UVs.

The 44 deck and hull pieces replace the previous procedural boards and support
the existing walking, jumping, climbing, surfing and combat controls. The other
158 parts scatter outside the main platform area. The encounter still ends in
Sahur's unavoidable defeat and unconscious arrival on First Island.
