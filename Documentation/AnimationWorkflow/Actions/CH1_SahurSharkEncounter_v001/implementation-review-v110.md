# v108 / v110 — actual mechanics pass in one native layout; overall incomplete

## Foot validation v108

v107 native contact/motion pass still had an infinite support error at16.146s. The stance validator covered the slash only at its landing-counter origin (+0.15m), not at first-counter(-0.4m) and final-counter(-0.3m). It also skipped full authored-sole validation when the coarse standing circle fit.

v108 checks all three actual slash origins and all cached authored sole paths on every stance candidate, including full standing footprints. Actual four-view native v108:625frames, supporting height1.158mm/fixed slip1.490mm, zero cropped head/feet in542framing samples, maxFOV57.469; genuine final defeat, ending266samples/25pause/zero upright or clock drift/zero paused motion/unarmed unconscious shore/waves PASS. Contacts [0,0.023361,0.475384,0,0.550463,0]m: two FAIL, not accepted as complete choreography. Four encoded movies/contact sheet exist in native-paired-v108.

## Actual single-arm contact adaptation v110

The remaining sub-metre moving-support mismatches can now use more of the existing right arm's reachable range: counter allowance up to0.55m, guard allowance0.60m, actual adjustment<=0.65m. Original two-bone solver preserves limb length; any world-length deviation>1mm is rejected. Wrist orientation, original rig/Avatar and left arm remain unchanged.

Additional reach beyond the old limits is accepted only when the actual translated held-weapon capsule has a clear route through the real wreck meshes. A separate rejection-only planning capsule is inflated by half the sampling interval plus3mm to cover intervening positions; it never changes the held weapon or damage collider. A blocked sweep restores original upper/lower/hand rotations. Contacts are still verified from the final actually posed weapon/head/fin geometry; no timing-window expansion or fake feedback.

Native v110 actual-water diagnostic PASS for this layout:

- Main film20.821s, genuine final defeat, all six real gaps [0.021218,0,0,0,0,0]m.
- Supporting sole1.225mm/fixed slide1.484mm.
- Zero residual native penetration, zero large escapes across all phases;1087 articulated advances/five partial.
- 625 observed film frames, maximum root move1.097777m, zero frame-budget overruns.
- Largest actual right-hand adjustment0.383415m; limb-length and extended-sweep guards ran in the runtime.
- Staging full-swim-cycle flagfalse/default setback2m; physical admission waited the bounded1s. Preparation7.9995s remains too long for final polish; do not hide this limitation.
- No genuine v086 neighbouring-board transfer (same-board variant). No screenshot/artistic/FPS acceptance can be inferred from this diagnostic.

Detailed outputs native-profile-v110.json/pose-motion-v110.json/solid-stages-v110.json. Source backups pre-contact-adaptation under backups/v110. Compilation PASS.

Natural ship-tail verification using versioned Tools/VerifyShipImpactV109.cs is next in the original Editor (Play requested; validate actual isPlaying before Start). Its outputs will use .codex/ship-tail-strike-v109 without overwriting earlier ship evidence. No Asset edits while tests/recording/build run.

After confirmed stopped v110, diagnostic save backed up save-after-v110-native.json and authoritative AB575 restored. Preserve original rigs/Avatar/history, controllers/lights/locomotion/old-left-attacks/throw064, mobile charge, no surfing/audio, real wreck, blackwhiteUI/colour images and ending. No commits/pushes/additional Editor. Whole objective remains incomplete pending multiple-layout/art/new-hop/whole-opening independent FPS review.
