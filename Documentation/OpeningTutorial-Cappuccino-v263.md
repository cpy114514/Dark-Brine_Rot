# Opening tutorial and Cappuccino balance — v263

> The 18-second voyage timing below is historical and is superseded by v264: eight seconds. The remaining tutorial and boss changes still apply.

## Changes

- The natural opening now preserves 18 seconds of helm control. Both automatic impact paths respect this minimum; an old legacy approach path previously bypassed the first timer. Ship speed and steering remain unchanged.
- Sahur uses the normal gameplay idle during the extended voyage, then plays the existing club fidget only for the final 0.3 seconds plus the 2.7-second cinematic lead-in. The club fidget remains approximately three seconds total.
- Walking retains its eight-second minimum and the existing physical deck constraint. The English prompt introduces sprint, followed by a separate sprint gate using the configured Sprint binding (default Left Shift). Completion requires the real sprint controller, movement, and one second of sprinting. The protected deck constraint remains active through both lessons.
- After the heavy-strike lesson, E introduces the club special skill. Correct input plays the existing authored throw, sends a copy of the held club out over open water, and recalls it before the jump/stow lesson. The later cinematic throw retains its own release/contact state.
- Neutral tutorial waits sample `Rig|Idle_Loop`, the same imported clip used by ordinary gameplay. Its local animation clock advances while tutorial world time is paused. Existing attack, charge, dodge, stow and traversal clips are retained; the new idle blends from the entry pose.
- Cappuccino always has a 999 maximum. The native prefab is authored at 999/999, and Health initialization also normalizes legacy scene overrides while retaining their health fraction. The ultimate no longer multiplies the health cap or attack damage. The existing once-per-encounter 200 HP heal and recovery animation lock remain.
- After that heal completes, a single 1.10 frequency multiplier controls attack windups, swings, inter-swing gaps, recoveries, orbit waits, and special attack cooldowns. The previous separate phase-specific faster windup/orbit timings are removed. Existing phase-specific combo selection remains.
- All three attack kinds share a 0.75 damage scale. Existing player damage mitigation, invulnerability/recovery and real contact checks still apply.
- After engagement, distance and the old arena radius no longer reset pursuit. A missing/dead player still ends the encounter; terrain, water and obstacle safety still apply.

## Verification

Unity Editor 6000.3.23f1, using the existing Editor only. Compilation succeeded and `git diff --check` passed.

- 16 live boss checks: native prefab HP, fixed cap through every ultimate phase, exactly one heal, post-heal timing, target retention 150 m beyond the old leash, real contact damage for Twin Slash / Dash Thrust / Whirlwind, and encounter reset.
- 25 opening acceptance checks: a natural 18.02007-second handoff, actual sprint and E inputs, correct special throw/return order, normal idle clip/time/hand motion while world time is zero, protected walking/sprinting without swimming, both real tutorial strikes, native plank fracture, final tail contact, launch, splash and nonlethal health.
- Final replay: 3,497 native 30 Hz frames, 3 confirmed cinematic contacts, 2 confirmed tutorial strikes, 70 HP at the end, no runtime errors. Contact sheet visually reviewed. Camera-rendered evidence does not include the screen-space overlay; tutorial prompts are also recorded as runtime beat text.
- Existing campaign save was captured after explicitly saving the user's active play session and stopping it. It remained byte-identical throughout the isolated tests. Original five-scene workspace restored.

Evidence: `.codex/tutorial-cap-v263/boss-result.json`, `acceptance.json`, `observation.json`, `opening-final/review.json`, and `tutorial-final-sheet.jpg`. The pilot replay caught the legacy timer bypass; its 11.45-second handoff is superseded by the final replay.

The existing uncommitted v262 animation/camera/contact changes and ProjectSettings changes were preserved. No commit, push or Windows build was requested for this pass. Native frame capture is a functional animation check, not an FPS benchmark.
