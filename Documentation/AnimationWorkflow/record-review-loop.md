# Recording and revision loop — human requirement, Oct 14

For every completed change affecting animation, swimming, wreck buoyancy or their integration:

1. Use the already running Unity Editor. Protect the latest human save before Play Mode; never launch another Editor/Player for capture.
2. Record consecutive actual game-camera frames through the changed section and its incoming/outgoing transitions. Retain the continuous MP4, frame/clock map and failure takes. A pretty still or successful compiler output is insufficient.
3. Visually inspect the recording, including closely spaced consecutive frames around defects; use front/side/three-quarter views when anatomy/support is unclear. State the actual review method. Opening a video tab is not evidence the agent watched it at normal speed.
4. Check feet against the real mesh AND the real water. Measure head/chest clearance, support-frame motion, visible contact and weapon direction separately. A millimetre sole-to-board error does not prove the board is above water.
5. If a material defect remains, revise its cause and record/review again. Name failures honestly; do not use a lucky earlier take to approve a later revision or manufacture hit feedback when actual contact misses.
6. Check normal and low render rates, pause, water/ground admission and held unconscious float as applicable. Simulation-clock captures are functional evidence, not a performance benchmark.
7. Stop the diagnostic run, preserve its test save separately, restore the protected latest human save, and verify its hash. Keep the existing rig, Avatar, source/history and unsubmitted work. Do not commit/push.

Current reusable tools: `Tools/ReviewFloatSwimV211.cs`, `Tools/TraceSharkSwimV211.cs`, `Tools/AnimationPipeline/encode_float_swim_v211.py`. Tool C# runs ephemerally via the live Editor's `run_script`; it is outside Assets. Outputs are in `.codex/chase-entry-20261014/<take>/`. The JSON records actual frame clocks, including admission and exits; use these clocks for review sheets rather than assuming every last frame belongs to the main fight.

This applies to future continuations as well. The human explicitly requested independent record → inspect → repair, without further questions.
