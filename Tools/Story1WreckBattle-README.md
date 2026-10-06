# Story1 wreck encounter

Story1SharkCollisionSequence pursues the real ship while keeping its nose about
26 m clear of the bow. It then turns alongside the camera-facing hull flank.
An authored Ship_Smash tail whip breaks the hull. Story1WreckBattle scatters 20 actual
deck sections and 24 curved hull sections, cut in Blender from ship_g.fbx. Another
158 pieces of masts, sails, cabin, rails and fittings scatter from the same ship.
The playable pieces retain their original materials and UVs. Every piece begins
at its source position and orientation before spreading and floating on the ocean.
Wooden detail fragments remain afloat; other fittings and cloth gradually sink.

Story1ShipWreckAsset supplies the 202 unique meshes and source transforms through
Resources/Story1Wreck/ShipWreckPieces.asset. The original FBX is preserved.
ArtSource/ShipWreck/ship_g_fractured.blend contains the editable cut ship and a hidden
original reference. BlenderShipWreck.py rebuilds it; ImportBlenderShipWreck.cs imports
the cut geometry into Unity mesh assets. Deck surfaces receive closed undersides.
Tiny hardware in secondary fragments is simplified; deck and hull silhouettes,
wood grain and source materials are retained. VerifyShipWreckSource.cs checks source
identity, reconstruction, all 29 original mesh objects, materials and UVs.

The original Sahur controller, attack animations and hitboxes, stamina,
boomerang, enemy lock-on and player health run throughout the live fight.
Story1WreckPlank supplies buoyancy and carries standing feet without taking
over movement controls. Stepping off enters the existing swimming system.

Fragments now have dynamic rigidbodies and begin at their real source positions,
rotations and sizes in the ship. There are no prescribed scatter destinations,
spacing constraints, layout grids, radius rings, resizing or interpolation toward
landing poses. The shark supplies initial directional linear/angular velocity;
Unity gravity and collision response determine each fragment's trajectory and landing.
Story1WreckFloatBody samples eight submerged corners for buoyancy and water drag.
A water-only righting torque tips each fragment onto its broad face; either side
may face upwards, while yaw, free falls and collision response remain physical. Wood floats;
heavier fittings and cloth continue to sink. Standing riders follow the rigidbody's
actual movement, checking footing against the previous frame before carrying the player. F surfing adds force and yaw torque; collisions remain physical.
Either broad face of an overturned board can be boarded. Swimming/climbing checks
use the tilted deck's horizontal outline, and actual deck support prevents a passing
wave from switching a grounded fighter into swimming.
Story1WreckRider displays context-sensitive black-and-white hints:
normal Jump crosses decks; Jump beside a board while swimming plays the existing
UAL2 ClimbUp_1m animation and restores ground combat on landing. The camera
follows the head and stays above the waves during the pull-up.

F toggles surfing on the supporting board. Forward/Back accelerate the board
(10 m/s forward, 4.5 m/s reverse); Left/Right steer. Releasing input brakes.
Solid colliders prevent steering or driving through other hull fragments.
The rider keeps a deck-local foot anchor through turns and buoyancy, and the
ocean displays a moving foam wake. Existing melee and boomerang inputs remain
available while mounted. Jump leaves the board with its velocity; F returns
to walking. Rebound movement/jump keys are respected and shown in the hints.
Defeat, scene unload and disabling the rider clear its movement and camera locks.

Tools/SetupStory1WreckTraversal.cs adds the climb and surf states to the existing
SahurGrounded controller using imported animations; it can be safely rerun in
Edit Mode. No scene wiring or new external animation download is needed.

The shark alternates telegraphed tail sweeps and lunges, followed by a 1.45-second
recovery window. A weapon hit visibly recoils the shark and interrupts its attack
(with a 1.8-second interruption cooldown). Its Health subclass accepts ordinary
melee and boomerang hits but remains alive. Live combat keeps Sahur at least at
one HP so low health or the random live-combat deadline can start the narrative finale.

TralaleroSwimAnimator now loads the original shark's Blender-authored anatomical
rig from Resources/SharkAnimation. Cruise/fast swim blend smoothly; tail strikes,
ship smash, bite, recoil and threat use separate generic animation clips. The
original static renderer is hidden and real skinned bones drive the visible mesh.
The original mesh remains available for collider bounds. Impact flashes target
the visible skin. Contact markers are sampled from the baked clips at .65s.
ArtSource/SharkAnimation/tralalero_animated.blend retains the editable actions and
packed original texture. The older deformation is only a resource-missing fallback.

Each encounter samples a live-combat duration of 20–35 seconds once when controls
return after the ship breaks. Game time and the pause menu suspend the countdown.
Shark health no longer starts the finale; it remains damageable during live combat.
At the deadline, or as soon as Sahur's HP is <=20% of maximum, Story1WreckFinale
takes over for 14.4 seconds. Whichever condition happens first starts the movie once.
When triggered while swimming, the lead-in first swims to the nearest large deck
at the existing fast-swim speed and climbs aboard; this adds travel time.
Sahur regains footing on the current/nearest large deck,
counters with a stick strike, rolls away from a tail sweep, throws and catches
the actual stick, performs a jumping chop, blocks a counter, and strikes again.
A final native tail slap knocks Sahur into the water and sends the stick flying.
The final hit sets real player HP to zero and hands off to the existing blackout.
The cinematic locks normal hitboxes/input, releases surfing/climbing and uses
camera shots tracking both actors. The deck continues floating below them.

SetupSharkFinaleAnimations.cs adds guard/stagger clips from the existing UAL2
animations and authors SahurWreckKnockdown by reversing the baked shore-get-up
clip. Existing combat clips supply attacks, dodging and throwing. Hull impact
VFX include water droplets, splinters using actual hull geometry and a
story-specific camera shake. Combat tail contact and damage share the native .65s beat.
The opening ship attack turns alongside the hull for 1.3s, then plays the Blender
Ship_Smash action. Its animated Tail_Marker meets the hull flank at the .65s
contact frame (1.95s after the lead-in starts). Contact is synchronized before
spray, camera shake and the ship's 16-degree sideways kick. The nose remains clear.
The hull fractures .14s later. Pieces receive lateral impact impulses and individual angular motion,
and retain full ship geometry scale throughout their physical flight and landing. The 4.6s break observation phase
lets the real fragments fall and float under physics. The full-size shark follows through and dives
before taking its smaller combat position. Chase and ordinary combat now derive
the waterline from the original mesh bounds and current rotation, keeping about
15% of total height above water instead of placing the torso center 2.3m above it.
Tail strikes briefly raise the contact area; cinematic tail shots retain their
authored contact heights while ordinary shots use the same submerged waterline.
Live windup, strike, recovery and recoil briefly lift the head, taking Sahur's
actual deck height into account so normal melee and horizontal boomerangs can
connect during the counter window. The head sinks again as recovery finishes.
Story shark lock-on uses its exposed dorsal area, avoiding the submerged skin
bounds center and a line of sight blocked by the supporting board.
The camera frames both the hull and
attacking shark, then follows the actual debris mass without moving any fragments. Broad spray and foam accompany the contact. Soft particle textures
are generated at runtime, and the
water shader is referenced by a Resources material so builds retain it.

A per-player narrative defeat handler bypasses the island respawn screen and
hands black-out back to Story1OceanAwakening. FirstIslandArrival still begins
unconscious and unarmed. Ordinary player death has no handler and is unchanged.
Story players have GameSaveExcluded, including before the ship breaks.

The unused Story1TurnBattle and StoryBattlePresentation scripts were removed.
All gameplay audio is temporarily muted by GameAudioPolicy.SoundEnabled=false.
The volume setting routes through the same policy, so applying settings or changing
scenes cannot restore sound. Sound assets and stored volume settings are retained.
All wiring uses the existing Story1 scene references; the scene's ocean, ship,
lighting and other serialized changes are preserved.

Tools/VerifyStory1WreckBattle.cs runs the movement, real weapon collision,
boomerang, lock-on, shark damage, defeat, save and island-transition checks.
VerifyNaturalEncounter separately checks the uninterrupted sailing-to-impact
route and the random deadline even while the player is protected.
VerifyStory1WreckFinale checks random durations across encounters, pause behavior,
the sampled deadline remaining fixed, shark health not ending combat early,
Sahur's health above/below the 20% boundary and early low-health entry,
all four Sahur counters and three shark counters, real defeat, lost stick and
unconscious island arrival. It can start from swimming and capture preview frames.
Tools/VerifyStory1WreckTraversal.cs drives the actual keyboard bindings in Play
Mode to verify forward surfing, steering, mounted combat, jumping off, jumping
between separate decks, fast swimming, authored climbing, walking/surf toggling
and release during the inevitable defeat. Both verifiers preserve campaign saves.

Tools/VerifyShipImpact.cs captures the natural sailing-to-tail-strike sequence and checks
actual tail contact, nose clearance, cruise/combat exposure, hull recoil, debris
flight, scale retention and control return.
Tools/ExportShipImpactPreview.py exports the captured game-time frames to MP4.

Tools/VerifyWreckPhysics.cs verifies all 202 dynamic fragments, gravity-driven falls,
unchanged geometry size, water buoyancy, response to an external impulse, swimming,
climbing onto a moving fragment, surfing with rigidbody forces and mounted combat.
It also drops a real deck section into the water edge-on and checks recovery onto
its broad face, and verifies that applying full master volume keeps audio muted.
Earlier scatter-layout and guaranteed-gap traversal checks apply only to the superseded
placed-board layout; physical wrecks may require swimming between distant fragments.
