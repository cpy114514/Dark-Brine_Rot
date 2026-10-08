# Wreck board buoyancy — 2026-10-07

Playable boards now average water height and slope over their footprint, smooth the
water response over 0.6 seconds, and use damped heave at the body's center. Vertical
drag also brakes upward motion. This replaces the eight corner lift forces that
amplified roll and bounce on long fragments. Righting follows a softened water
normal, with a maximum target tilt of 8 degrees and stronger angular damping.
Gravity, free flight, physical collisions, steering and horizontal water drag remain.

Non-playable floating timber uses the same softer spring and damping response in
its existing substepped simulation. Its launch, gravity and sinking-piece behavior
remain. No extra Rigidbody components are added to the decorative fragments.

Validation in the live Unity Editor:

- `VerifyBoardBuoyancy.Verify("final", true)`: three playable board fixtures plus
  two decorative fixtures, including edge-first entry and a 12 m drop. Playable
  settled maximum tilt 2.46–3.26 degrees; RMS vertical speed 1.44–1.70 m/s.
  Decorative RMS vertical speed 1.84–1.96 m/s. The ocean's existing large swell
  remains; measured board height range over nine seconds is 2.94–3.93 m.
  The dropped body falls 1.93 m in the first 0.6 seconds; sinking debris sinks.
  Fixtures ignore the large moving ship collider to isolate water response.
  The initial `before.json` fixture run includes ship collision interference,
  so its tilt and bounce values are not a controlled comparison.
- `VerifyStory1WreckTraversal.Verify()`: actual wreck surfing, steering, jumping
  between boards, swimming, authored climbing, attacks, and low-health release pass.
  Surf travel 3.15 m; steering 13.92 degrees; jump height 2.65 m.
- `VerifyStory1WreckFinale.Verify(false,true,false,true,false,"buoyancy-final")`:
  202 real ship fragments, three cross-board jumps, two breaches, low-health
  trigger, contact, defeat and unconscious island transition pass. Maximum
  contact gap 0.00876 m; landing error below 0.000001 m; no contact correction.
  Actual game captures reviewed at `.codex/encounter-buoyancy-final/preview`.
- Scripts compile without errors; final Unity error console empty.
- Play Mode stopped and the user's current save restored byte-for-byte afterward.

Source backups, fixture results and the current save backup are in
`.codex/board-buoyancy-20261007`.
