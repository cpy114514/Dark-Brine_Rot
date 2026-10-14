# v175–179: bounded articulated advance, exact planning geometry, control regression

Stage: runtime integration; whole approved objective remains INCOMPLETE. Existing original Editor only. No Player/network listener, extra Editor, scene save, commit or push. Original animation resources/rigs/Avatar and ordinary attacks/walk/run/roll were not edited in this revision.

## Runtime revisions

`Story1SharkWreckContact` v175 preserves the four original articulated advance attempts at full root travel. After they all fail, it tries 75%, 50%, 25% of both root travel and authored bone progression. Every intermediate sampled pose retains all original anatomical/convex mesh tests and the stricter 0.99mm clearance. The caller now consumes the actual solved root and rotation instead of overwriting a successful partial advance with the full destination. Original impulse/retained/escape fallback remains if none succeeds. This is not a universal removal of emergency escapes.

Native `partial-root-v175.json`: 256 hypothetical paths against actual released meshes. All 104 original successes match their prior root/bones; 27 additional paths succeed, 131 total; zero comparison mismatches and zero residual on accepted poses. Largest floating-point distance excess 0.0556mm. Actual rig poses were temporarily sampled and restored within the diagnostic; no wood was moved. This diagnostic does not prove every physical trajectory or game FPS.

v176 caches exact capsule radius/height pairs for repeated hypothetical pose queries, at most 32 geometries per anatomical segment (128 total); other dimensions retain the original mutable capsule. No rounding, smaller shells, new broadphase rejection or collision tolerance change. Native `planning-geometry-v176.json`: 320 poses, 3200 cost/maximum comparisons, all 320 used a cached capsule, 259 native penetrating cases, zero mismatches/accepted difference. The bounded Editor microbenchmark measured 21.149ms mutable versus 19.377ms reused for its accumulated query work; this is not independent game performance evidence and does not establish the whole FPS target.

## Actual full films and limits

Native175: 20.804s, first probe honestly MISSED 3.671m, other five contacts zero gap; three hero/two shark contacts. 625 observed frames, zero travel overruns, maximum residual 0.796mm, feet 1.269mm, slide 1.258mm. Fixture queries ran earlier in this same diagnostic: do not use its counter totals or Editor timings as a clean performance comparison.

Native176: 20.806s, probe/reversal tail honestly MISSED 2.298/2.878m; other four contacts zero. 625 frames, zero film travel overruns, residual 0.970mm, feet 1.132mm/slide 1.735mm. 88 genuine partial root advances counted (geometry fixture did not call the advance solver). One 1.997m emergency escape occurred during playable Fighting, before the film. Dense-layout contact/escape robustness remains FAIL. Different evolving layouts preclude causal A/B claims.

Native177, clean 10FPS simulation: 20.854s, all six contacts PASS (largest gap 14.377mm), four hero/two shark contacts. 209 film frames, zero travel overruns, maximum residual 2.966mm within existing 3mm fallback tolerance; feet 1.126mm/slide 1.209mm. 51 genuine partial root advances, zero whole-run emergency escapes in this layout; 128 cached geometries and 9494 reuses. Same-board fallback, no safe neighbour hop. This is one low-frame-rate case, not proof all dense layouts or independent FPS. CLI response timed out, but observer/report files proved exactly one run completed; no duplicate was started.

## Actual player control regression

Versioned prefab-only fixture `Tools/VerifySwimJumpV178.cs`, report `controls-v178/runtime-verification.json`: real CharacterController/input/Animator; 60/20/10FPS jump heights 2.698/2.697/2.697m against unchanged 2.7m, one takeoff/landing each. Coyote jump, buffered landing jump, inherited board velocity (4.472m/s) and ceiling cancellation PASS. Native swim poses across headings/speeds minimum facing dot 0.999736; eight actual movement cases normal/fast × four directions, movement dot1 and visible facing minimum0.999948. Temporary fixtures/devices restored and excluded from saves; no genuine moving-edge climb acceptance is inferred.

`Tools/VerifyChargeSprintV179.cs`, `controls-v179/result.json`: actual input Update paths preserve charge timer when sprint starts and changes/reverses direction, preserve mobile heavy release through another turn, and maintain 16.8m/s settled sprint. The ordinary left attack continues through its existing moving upper-body overlay. The historical fixture's expectation that light attack cancels on movement was updated to the already implemented approved behavior; production combat source was not changed to satisfy this test.

## Remaining acceptance

Dense first probe/reversal routes and playable emergency escapes remain defective; the independent 1080p performance target remains FAIL/unverified. Do not restart numbered development Players: follow `network-test-launch-constraint.md`. Latest all-six successful four-view movie remains174 pending current180 recording; lowFPS177 is numerical evidence, not a retroactive video. Continuous normal-speed artistic approval remains unobserved. Same rig/source171/128/086/078/097/064, original062 left attacks/walk/run/roll, no surfing/audio, real gravity/mesh collisions, minimalist BW UI/colour images and shore ending remain protected.

After verified stops175/176/177/179, diagnostic saves were preserved separately and the pre175 user save (AB575DF9484F0327E8FC7AD7B7BD9B123ADB85D13F4E9A627F3F40A38D1EC0F3) restored. Current180 recording is in the original Editor; do not edit Assets or duplicate it before completion/ending review.
