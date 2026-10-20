# Ship wind and wave motion — v227

The intact Story1 ship now responds to the same world wind direction as its sails, with a gradual wind heel, slowly changing sail pressure, wave-driven pitch and roll, and a damped return towards equilibrium. This modifies the existing `ShipSailingMotion`; it does not add a second transform writer, Rigidbody, animation clip or visual-only hull root.

## Implementation

- The effective five-point buoyancy span defaults to 65% of hull length/beam. The old 96 m bow/stern separation was almost the primary 95 m wavelength, attenuating that wave's pitch response. The new probes represent the working buoyancy footprint, while hull dimensions, sailing speed, steering and wake dimensions remain unchanged.
- Wave tilt strength defaults to 1.2. Existing maximum angles remain 4 degrees pitch and 5 degrees roll. Heave uses 60% centre height plus 10% of each surrounding sample, reducing short-wave vertical bobbing. The recorded vertical excursion reduced from 4.72 m to 3.46 m while pitch became more readable.
- Wind direction is `ocean.wave1.direction.normalized`, matching `ShipSailWind`. Broadside wind heel defaults to 1.8 degrees, with slow Perlin pressure variation (strength 0.55, time scale 6 seconds). Sails retain their authored local flutter; the heavier hull does not follow every cloth ripple. Head/tail wind produces no lateral heel.
- Closed-form damped angular springs (damping 0.78) retain momentum and settle without unstable frame-by-frame Euler integration. Roll axis is derived from the sampled beam. The actual ship has a root flipped 180 degrees around X: its original native roll sign was appropriate for that import; simply reversing that sign would be incorrect. Upright, flipped and turned roots are covered by the direction tests.
- A single `OceanSurfaceSampler.Snapshot` is shared by the same five probes per update. No extra physics contacts, cloth simulation, mesh baking or per-frame managed allocations were added. `Ship.SailingMotion` is available in the Profiler. Inspector fields and descriptions are English.
- The existing kinematic hull root carries its native colliders and passenger. Existing passenger sole alignment remains active. Existing camera code keeps the horizon upright. The original cinematic takeover disables sailing and passenger controllers and retains the current hull pose before the shark crush.

## Actual validation

Used only the already-running Unity 6000.3.23f1 Editor on port 7800. Temporary observers and isolated tests live outside Assets in `Tools/ReviewShipMotionV227.cs`, `Tools/TestShipMotionV227.cs` and `Tools/AnalyzeShipMotionV227.py`. Evidence: `.codex/ship-motion-20261020/`.

- 24 isolated contracts passed: constant-load spring agreement at 10/30/60/120 Hz, bounded overload and recovery, zero-delta freeze, bow/beam/wind direction on three root orientations, unchanged forward travel and no lateral translation drift, actual-wave tilt limits and allocation checks.
- 5,000 warmed native five-probe simulation calls allocated **0 managed bytes**, averaging **0.0608 ms/call** on this machine in the Editor. This is a component microbenchmark, not a whole-game FPS or low-spec hardware result.
- Native intact sailing recorded for 24 seconds at 30 Hz (721 frames) and 10 Hz (241 frames). After initial 0.4 s sole initialization, both feet found the native deck in every sampled frame. Left sole clearance stayed approximately 0.035 m; right sole clearance was 0.034893–0.035113 m at 30 Hz and 0.034920–0.035089 m at 10 Hz, matching the existing 0.035 m support offset. No unintended passenger release occurred.
- Observed pitch was bounded to ±4 degrees and roll to ±5 degrees. Maximum frame steps were 0.176/0.251 degrees pitch/roll at 30 Hz and 0.527/0.748 at 10 Hz.
- Both runs started the real shark crush from the moving hull. Immediate hull position and rotation jumps were **zero**. Sailing and passenger controllers stayed disabled during takeover; the intact hull fractured/deactivated and both recordings reached Tutorial. The 30 Hz observer initially waited on scaled time after the tutorial intentionally paused; its rows were exported and stopped through `FinishExisting`. The harness was corrected to finish immediately on reaching Tutorial before the 10 Hz run.
- Normal enabled-camera render captures include six voyage images and six handoff images per final run; representative ship, deck and breakup images were inspected. Console errors: **0**. `git diff --check` passed for the changed production source.

This verifies the intact passenger voyage and takeover into the tutorial. It does not repeat the complete v225 shark/wreck collision suite, freely controlled deck walking/ladder tests, all weather configurations, release builds or low-end hardware tests. Existing whole-game performance limitations remain as recorded in `WholeGamePerformance-20261020.md`.

## Preservation

The current human save was newer than the previous v226 save and was protected before Play. Restored and verified after final Stop:

`A7A0DBD92F9F0E762BB6573D407A824CA2EB4253C4008ED62A2F6DAEE9984A1E`

The Editor is stopped/unpaused. Story1's pre-existing dirty authoring scene was retained without save, reload or discard. No scene/prefab YAML was edited. Other user changes, combat work, shark scale and animation assets were preserved. No second Editor, standalone Player, build, commit, push or automation was created.

Final production source SHA256 (`Assets/Scripts/Ships/ShipSailingMotion.cs`):

`CC1D6F859F67E101003596439572E78D919CA09AAEFF12CDAB0AF11FE6A5243B`
