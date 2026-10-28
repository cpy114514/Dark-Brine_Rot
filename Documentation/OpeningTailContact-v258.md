# Opening finale tail contact — v258

Windows report: Sahur remains in the counter stance when the shark should finish
the encounter with its tail strike.

## Cause and correction

The final beat waits for a visible tail-to-body gap of at most 35 mm. Its
full-body separation solver advances in steps of at least 150 mm, with an
additional safety pad. That is appropriate for a near miss, but it can overshoot
the contact window. After the authored turn finishes, subsequent frames repeat
the same fit, leaving no way to advance if that gap is too large.

The tail beat now requests a 3 mm clearance and refines the last blocked/clear
translation bracket against the current posed shark mesh and Sahur's measured
body capsules. It retains the proven clear endpoint; neither the actor nor the
tail marker is forced through the body. Refinement is bounded to 14 iterations
with a 1 mm translation tolerance. Other separation calls keep their existing
coarse solve. Shark scale, animations, camera timing, damage budget and water
exit planning are unchanged.

## Validation

- Original complete Editor replay: passed once, tail gap 28.33 mm. This confirms
  the failure is placement dependent rather than an unconditional failure.
- Corrected complete Editor replay at 30 simulation Hz: 2,669 frames; two real
  tutorial hits; three film contacts; tail hit, airborne motion, splash and
  unconscious drift all observed. Final tail gap 5.56 mm; 70 HP; no runtime errors.
- Evidence: `.codex/tail-fix-v258/before.json` and `precise-30.json`.
- Actual Windows x64 Mono release replay at 60 simulation Hz: 5,267 frames,
  two tutorial hits and three film contacts; tail registered once at a 3.30 mm
  gap, followed by flight, splash and drift; 70 HP; zero runtime errors.
  Rendered 482 final-beat frames and saved 80 images. Reviewed contact, airborne
  and face-up drift images. Build `build_c829b2f44bc9` succeeded with zero build
  errors; its sole warning is the intentionally absent Runtime Pipeline config.
- Regression sweep at that actual Windows contact pose: 12 initial translations
  from 30 to 167.5 mm. The old solve missed the 35 mm window in 9/12 cases;
  refined results were 3.04–3.39 mm in all 12, with full-body separation retained.
  The earlier Editor sweep sampled before contact and is not this regression
  evidence.
- An initial Windows replay stopped before the tail at Duel 11.919 with no
  exception. It is retained as an incomplete test. Hardware keyboard/mouse
  devices are now isolated inside the replay process and pause/menu state is
  logged; the replacement full replay completed with no menus open. Production
  pause behavior was not changed.
- Windows report: `.codex/tail-fix-v258/tail-review-v258-60b.json`; images in
  `windows-images/`; build report and assembly fingerprint in the same folder.
  The human save's SHA-256 remained identical before/after standalone testing.
- Final artifact:
  `C:/Users/pinyu/Downloads/Dark Brine Rot Windows 2026-10-25 Tail Fix/Dark Brine Rot.exe`.

`OpeningTailReview` is an opt-in production-path input replay. It is instantiated
only by `--tail-review <report-path>` (or the Editor review tool). It marks its
players `GameSaveExcluded`, uses real Input System events to pass tutorials,
captures final-beat renders, and reports errors, contacts and progress. Normal
game startup creates no review object. `--tail-review-fps=60` selects a 60 Hz
simulation replay; this is a functional check, not a frame-rate benchmark.

The player's save and original Player.log were backed up before testing. The old
running Windows executable is retained. No VCS submission is part of this fix.
