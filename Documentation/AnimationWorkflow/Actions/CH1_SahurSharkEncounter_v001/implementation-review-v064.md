# v063–v064 native integration — revision required

Latest user instruction: finish the animation and correct Sahur facing backwards
while throwing the stick. The opening remains incomplete.

v064 corrects the cinematic throw's imported root orientation. On the original
Avatar, PLAYER_Throw_v006 started with the head facing -176.205 degrees relative
to the calibrated guard direction. A versioned FBX copy keeps the existing rig,
Avatar and animation, with root rotation offset increased by 180 degrees.
PLAYER_Throw_v064 starts at +3.795 degrees; near release the head faces forwards.
The isolated native sampling reports are throw-facing-PLAYER_Throw_v006.json and
throw-facing-PLAYER_Throw_v064.json in .codex/opening-optimization-20261012.
The active definition, historical-beat normalization and preload list now use
v064. Original source assets and ordinary gameplay boomerang are preserved.
PreviousEncounterDefinition.asset in Resources/Encounter/v064 preserves the
definition before activation. Do not rerun installers over their backups.

v063 introduces raised camera candidates on the same authored side, with hysteresis
and visibility evaluation at approximately 8 Hz. Backface ray queries are scoped
and restored. This is an integration trial; complete artistic acceptance is pending.

Native capture native-paired-v064 completed 625 frames across game, front, side
and three-quarter views at 30 FPS, covering the full 20.8-second 60-FPS source.
Four continuous MP4 previews and a contact sheet are available. Contact sheet
and game frame0249 (throw release at about8.32s) were actually inspected.
The frame shows Sahur facing the target; the original backwards import is resolved.
No claim of observing all movies at normal playback speed is made.

Contact acceptance FAILS: three hero counters and two shark counters, with the
first probe missing by3.93211722m. Other five contact attempts reached zero gap.
Supporting foot error0.001072506m and planted slide0.001292736m are scoped measured
results, not artistic approval. No safe neighboring board was selected: zero hops,
one breach. The same-board variant ran; a real hop still needs separate review.
The first probe requires correction; its missed contact does not trigger a fake hit.

Visible remaining issues: the side review view is obscured by real timber, actors
are small in some fixed views, and the first shark approach does not actually meet
the held stick. Game-camera throw is clearer in the inspected frame, but camera
quality is not accepted across all random layouts. Final defeat remains face-up
in the final sampled row; complete shore transition still needs regression.

native-profile-v064-offline.json is offline evidence only, not hardware FPS evidence.
EntryPoseCost12.42369 is another unresolved admission issue. Earlier v062/v064
captures use the same fracture seed but wall-clock scenario waits created different
boat travel and wreck settling times. They are separate real native runs, not matched
layout comparisons, and changes in contact count cannot be credited to the camera.

v062 previous left-mouse attacks and mobile charge input/core regressions passed.
No additional Editor, scene save/revert, commit or push. Original Play Mode was
stopped after the capture and the authoritative save restored. Subsequent native
diagnostics must check the actual live state rather than this historical checkpoint.
