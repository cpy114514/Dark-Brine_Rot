# Moving charge, partial blade rush, and rifle controls — v255

## Requested behavior

Preserve the existing weapon/attack system and authored clips. Holding RMB charges melee while moving. Partial blade charge releases a lower-damage, farther-traveling rush. Rifle RMB aims while standing or walking; sprint carries the rifle and permits fire but cannot aim. English equipment help reflects the controls.

## Implementation

Melee RMB presses use the existing combat input-buffer duration while the button remains held, so an eligibility boundary cannot discard the only press event. Grounded charging uses the controller's existing brief ground grace across steps/slopes, while jumps, dodges, swimming and external control end charging. The current moving upper-body charge animation and normal release timing remain.

Blade release records its charge amount before clearing the charge state. A deliberate release after 0.10 s can rush instead of being discarded by the former 1.2 s gate. Damage interpolates from 0.65 to the existing 2.2 multiplier, retaining the existing blade balance factor. Travel interpolates from 1.25 × the configured distance down to the original distance at full charge: up to 20 m for partial charge and 16 m at the existing 2.6 s full charge. The amount is latched for each rush, so later inputs cannot change travel mid-attack. The existing collision-limited controller motion, active interval, immunity and once-per-swing hit bookkeeping remain.

The rifle exposes a single `IsAiming` state from RMB, valid locomotion, sprint intent and reload state. Sprint wins if RMB is held; releasing sprint allows held RMB to aim again. Actual sprint movement retains stamina ownership and runs at 85% of unarmed sprint speed, while rifle walking retains its previous speed. The base run cycle supplies sprinting legs, with the existing independent rifle upper-body layer and grip solver carrying the weapon. The solver lowers the carry pose between shots and retains muzzle-aligned firing during hip fire. Aiming tightens the crosshair and uses the existing larger shoulder-camera offset. Shooting, bullets, recoil, ammo and reload retain their existing implementation.

## Verification status

Native input and pose evidence is recorded under `.codex/weapon-flow-v255/`. The original human Play session was saved with `GameSaveManager.SaveCurrentGame` before stopping for compilation; the prior disk save is retained separately. Prior pending source hashes and copies of all edited files were captured before changes. Temporary tests run through the existing Editor only, with a save-excluded actor, native Input System events and no manual movement/attack Update calls.

The old `VerifyChargeSprintInput` fixture could not settle its actor on its floor and is not counted as a passing baseline. The prefab retains a world placement (including z = 850) which must be reset before placing the isolated actor on its test floor. Rejected early takes retain the failed grounding evidence; a temporary diagnostic also had duplicate anonymous-property names and was corrected before successful execution. These tooling failures do not establish a gameplay pass.

## Native acceptance

`native-60-04/result.json` passes 26 assertions plus one measurement record over 895 actual frames at a fixed 60 Hz. The test uses native queued keyboard/mouse state; it never invokes movement/attack Update manually. It verifies:

- Starting a club charge while sprinting, changing run direction without resetting charge time, retaining base locomotion plus charge arm layers, releasing into the heavy attack, and beginning movement during an already-held stationary charge.
- A one-frame physical floor interruption retains charge. Removing support for longer than the existing ground grace correctly cancels it.
- A roughly 0.45 s moving blade charge releases below the former 1.2 s gate and retains rush immunity. It travels 19.5276 m with 18.3654 configured per-hit damage; full charge travels 16.0000 m with 44.0000 per-hit damage. These are attack damage amounts before enemy mitigation/weak points, not a new enemy-contact certification.
- Rifle RMB aiming at rest and while walking, firing while walk-aiming, sprint taking priority over held RMB, higher sprint movement speed, firing while sprinting with or without RMB, resuming held RMB aim after sprint release, leaving aim on RMB release, and reload suppressing aim.
- Rifle sprint uses base running legs while retaining the existing two-handed holding layer. Unfired sideways sprint turns body/carry into its direction of travel. Native screenshots in `native-review.jpg` review the club/blade moving charge and rifle carry/aim/run/fire poses.

No weapon, character, source clip, Animator asset or scene is re-authored. No new effects, per-frame allocations, searches or physics work are added by the gameplay changes; the rifle component lookup is cached. No release build, full boss playthrough, arbitrary-terrain animation certification or frame-rate benchmark is claimed. Review tooling is outside Assets and is not shipped in the Player.

`final-editor.json` records the restored human campaign and save bytes, all five original island scenes reopened clean, and stopped Play Mode. `acceptance.json` retains final production-source hashes and confirms previous pending work was preserved; the pre-existing controller edit was extended in place. No commit, push, UVCS check-in or new automation is performed.
