# Story1 wreck encounter

## 2026-10-07 animation revision

The live encounter now loads versioned EncounterShark assets preserving the original
rig and library. New Blender bites, tail strikes, recoil and ship smash are coupled
to a continuous 14.4 s paired finale: equal exchange, Sahur advantage, reversal and
defeat. New Humanoid slash/guard/brace actions retain a right-hand weapon and the
original throw/hit/death clips. A single manual clock evaluates both actors after
Animator evaluation, including pause and crossed contact cues. Contacts measure
the actual trajectories without impact-time root corrections. Tilted board sole
proxies ground the actor; falling uses head/chest buoyancy rather than a feet floor.

Live shark damage follows swept nose/tail markers during active windows, once per
attack, allowing committed attacks to be dodged. Moving charge, traversal and
the 20–35 s / player <=20% narrative entry remain intact. Ship contact uses the
new .65 s tail pose with proximal-to-distal delay, hull recoil before release and
20 rigidbody decks plus 182 ballistic/water detail pieces. No new sound is added.

Current evidence and source paths are recorded in
Documentation/AnimationWorkflow/Actions/CH1_SahurSharkEncounter_v001/final-review.md.
Use Tools/AnimationPipeline/VerifyEncounterBuoyancy.cs for the current 20-body
physics contract; the earlier 202-rigidbody verifier is historical.

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

The 20 original deck sections use dynamic rigidbodies and solid colliders. The
24 curved hull sections and 158 secondary pieces retain their original geometry,
UVs, source positions and launch motion, using lightweight ballistic/water motion
without box colliders filling the empty interiors of curved hulls. This reduces
physical fragment bodies from 202 to 20 and keeps decorative debris from blocking
weapon sweeps. Non-wood debris stops rendering once it sinks below the encounter.

Fragment objects are prepared six at a time while sailing, then activated at the
fracture frame. Deck forces still use eight submerged corners. A three-sample local
water plane refreshes at 12.5 Hz and predicts height between samples, avoiding eight
full ocean inversions every physics tick per fragment. Continuous speculative
collision preserves fast fragment contact at lower cost. Actual body movement,
free yaw, righting forces and F surfing remain physical.

Landing chooses a stable, flat, less submerged original deck, including for the
finale; positions still come from the source ship and physical flight. A late
root-motion edge guard prevents grounded attack lunges from stepping off the
supporting deck. Movement cancellation and deliberate jumps remain available.

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
The original mesh remains available for body collider bounds. A separate capsule follows the visible head, and lock-on distance/facing uses this head instead of the FBX pivot. Impact flashes target
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
VFX include water droplets, splinters using a dedicated eight-vertex mesh and a
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
all three Sahur counters and three shark pressures, real defeat, lost stick and
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

## 2026-10-06 combat reach refinement

Combat pursuit and recovery position the actual nose at a reachable distance from
Sahur, with a smooth exposed-head counter window. Tail motion commits its starting
pose and approaches the actual tail contact point; a moving deck carries the
selected strike point without tracking the player's subsequent dodge. Cinematic
stick counters use explicit nose positions instead of assuming the model centre
is its head. Deck-local cinematic foot positions are bounded by the real deck.
Story boomerangs receive a 22-degree facing-cone aim correction from the animated
release point toward the exposed head; real projectile sweeps and occlusion still
determine damage. Island throws retain their previous trajectory.

Current validation: ProjectRepairBackups/StoryCombatOptimization-20261006.
Historical tools expecting 44 solid boards or 202 rigidbodies describe the earlier
implementation; the current encounter retains 202 visual pieces and 20 physical
decks.

Standing support temporarily ignores only the passenger/controller pair with its
current deck, while explicit deck-local grounding follows the real moving surface.
This prevents an effectively immovable character collider from pinning buoyant
wood underwater. Walking off or jumping restores the ordinary collision pair.
Jump detection uses the controller's own vertical speed, so upward wave movement
is not mistaken for a player jump. Surfing uses the same passenger support rule.
Pursuit/recovery moves the visible nose smoothly rather than moving a model-centre
reference while rotating a long body. Recovery keeps the head exposed for a 2.4 s
counter window. The shark takes its combat position near the selected landing deck.

The actor's detailed solid body hitboxes are excluded from wreck-deck collision
throughout the encounter; their damage queries remain intact. Those animated
kinematic feet previously pinned rising wood even after controller support was
separated. The main character controller retains normal collision when jumping
or walking off a deck. Pair exclusions are scoped to these runtime wreck decks
and cleaned up when the rider is destroyed.

A jump from a wet floating deck gets the existing swimming system's 0.6 s exit
grace, allowing ascent through a passing crest. Ordinary island jumps are unchanged.
