# Shore get-up

Uses the existing Quaternius UAL2 `Armature|LayToIdle` animation from
`Gameplay/QuaterniusUAL2/UAL2_Standard.fbx`. The complete humanoid motion is
copied into SahurShoreGetUp.anim. Root height and planar travel are baked into
the pose; muscle curves remain authored by the source animation.

FirstIslandArrival holds its face-up opening pose throughout the unconscious
wave drift, oriented with his head leading toward the shore. The authored
landing sits one metre above sea level at the waterline, rather than inland.
Terrain contact at 0.75 metres above sea level immediately ends planar drift;
the wave recedes over a 0.25-second vertical settle at that contact position.
After the survivor rests on the sand, it plays the complete motion
over 2.3 seconds, visibly pushing up, sitting and standing. Movement stays
disabled until the final 0.18-second blend to Locomotion completes. The stick
remains missing until the existing pickup is collected.

Tools/InstallSahurGetUp.cs installs the clip, Base Layer Shore Get Up state,
ArrivalGetUpPhase parameter and scene references. It also samples the actual
player's baked mesh at 61 poses to store a bottom-height curve. This keeps the
animation on the sand without baking the 279,411-vertex mesh during gameplay.
Re-run Install after changing the player's scale, avatar or get-up source.

Tools/VerifyFirstIslandArrival.cs checks face-up, head-first drift, no sliding
after shore contact, visible get-up,
locked controls until standing, pickup and armed/unarmed saved-game restores.
Tools/CaptureSahurGetUp.cs captures the actual in-game get-up for review.
