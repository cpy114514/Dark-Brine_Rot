# v101–v103 continuation — incomplete

## Articulated solid movement v101

The native solid solve now first attempts a swept, collision-limited blend from the last actual articulated pose into the requested pose at the bounded root placement. All original anatomy volumes and native mesh penetration tests participate. Four pose fractions (1/.75/.5/.25) are considered; accepted intermediate samples use a stricter 0.99 mm test than the existing 1 mm translation trigger. Failed attempts restore the original requested transforms before the impulse/translation solver. No rig, mesh, collision or contact marker is replaced.

Translation passes are bounded at eight. A still-clear previous actual pose is preferred before exhaustive seam escape searches. Native damage/feedback uses only the final solved pose. This intentionally changes constrained motion; it is not an equivalence claim about the old 24-pass escape placement.

Actual native v101: 625 observed main-film samples, maximum native residual 1.669 mm, no large-escape records during the main film (three remained in player combat). One small frame travel overrun recorded: 2.121 cm beyond the existing 24*dt+0.30 diagnostic budget at 1.216 s; do not claim full movement-budget acceptance. Five contacts PASS, first counter FAIL 2.164 m; final defeat authentic, no exhaustion exit. Zero genuine new-v086 neighbouring-board transfers. Supporting height 1.492 mm/fixed slide 1.351 mm. 750 articulated advances, eight partial advances. Native solid worst main-film cost 43.468 ms, attack-facing worst 75.787 ms in that different layout; still no independent performance claim.

## Combat admission v102

Removed the combat setup relocation nine metres in front of a random landing board. The complete shark could begin inside freshly released hull pieces there. Setup now preserves the actual tail follow-through/dive root and heading and starts surface-height smoothing from the actual depth. Pursuit and the solid owner bring it back. Health, thresholds, normal player controls, scale policy, rig and original animation resources remain unchanged.

Development build v102 succeeded (67.480 s, zero errors). The 488 warnings were inspected through the actual native BuildReport: predominantly package Sentis/ConvGeneric shader variant/modulus warnings, plus the existing ocean SailingFoam uninitialized-variable warning and absence of optional runtime Pipeline config. No package/source shader was patched to silence warnings.

Independent actual rendered 1920x1080/DX12/i5-14400F/RTX4060 v102 seed14, 2,591 completed GPU readbacks/zero errors:

- Main film 20.801 s, mean 16.798 ms, max 26.256 ms, zero frames over 50 ms. 1% low 44.500 FPS still FAILS >=50 target. Max facing 7.754 ms/solid 5.437 ms in this layout.
- All four hero contacts and final defeat PASS [0.001916,0.007169,0.011357,0.032027,*,0] m. Reversal-tail FAIL 10.762 m, no fake impact. Actual final defeat occurs, exitReason null. Zero new-v086 transfers.
- Player combat max frame 96.560 ms despite solid max 8.571 ms; entry max 76.814 ms; breakup max 51.999 ms. Whole-opening performance is not accepted. Four large-escape records remain during player combat/admission (about 2 m), none during main film. Full residual maximum 2.777 mm.
- Original user save hash AB575 remained unchanged after the Player. Original Editor autotick restored TRUE after exit. No second Editor, commits or pushes.

## Targeted staging v103 — under verification

The missing reversal diagnostic had an unobstructed endpoint but an obstructed approach: actual tail was 10.762 m away, native correction about 17 m from intended, route score 709133. The flank still pinned the nose while rotating the whole long animal around it, sweeping the rear through the support. It now interpolates the actual body centre into the open-water lane while turning and diving. Actual solid correction remains authoritative.

The same-board breach had another concrete staging problem: with no destination board, it always chose negative board-local X. That can be the occupied side of a large hull even when the selected stance opens towards a different edge. This variant now uses the actual stance's nearest open-water edge with the original clearance. The real-neighbour variant retains its existing opposite-destination staging. No wood was placed, steered or teleported.

v103 source compile/native continuous review is pending at this writing. Preserve all unsubmitted work. Backups of pre-change solid/finale/battle are under backups/v101, v102 and v103. Do not claim whole task complete or delete the one-time continuation solely because this run has made progress.

Current active clips: bite v078, tail/ship smash v097, Sahur parry v090/hop v086, corrected throw v064 and original light attacks/walk/run/roll. v097 installer already ran; preserve PreviousEncounterShark.controller. Existing no-sound/no-surfing/monochrome-UI/colour-image/real-wreck/float-and-shore contracts persist.
