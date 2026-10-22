# Twin-blade animation continuity polish v236 — 2026-10-21

The user requested recording and reviewing the current animation, keeping its overall design and improving local defects. Scope follows the preceding twin-blade restoration v235. Approved actions, source clips, rigs, meshes, three-hit combo, horizontal finisher, X charge, long rush, spin, guard, size and gameplay windows remain.

## Recorded review

`Tools/ReviewBladeAnimationV236.cs` records actual Input System keyboard/mouse actions, normal Animator evaluation and the final native SRP camera output in the existing Unity Editor. The fixture first equips the real rifle, then switches to owned twin blades. Shots cover idle/movement, three-hit combo, charged rush, early charge cancellation, guard release, spin release and actual contact. Before/after recordings contain **922 / 923 native 960x540 frames**, at **60Hz simulation**. Each shot is continuous; resets between shots are omitted. `Tools/AnalyzeBladeAnimationV236.py` encodes the frames at their original rate and produces consecutive transition sheets; `Tools/CompareBladeAnimationV236.py` aligns the important ownership changes in a side-by-side clip.

Artifacts under `.codex/blade-animation-polish-20261021/`:

- `baseline-60/BladeAnimation-baseline-60.mp4`
- `after-60/BladeAnimation-after-60.mp4`
- `BladeAnimation-BeforeAfter.mp4`
- Native frames, bone/clock maps, transition sheets, checks and comparison metrics alongside them.

Review method: inspected consecutive final-camera frames at the charge release, cancellation, movement wrist discontinuity and combo handoff, then inspected the corrected release/cancel/movement sheets. This is a targeted continuity review, not a claim of watching the whole MP4 at normal speed or final human artistic approval. Silhouette, transition timing and held-pose continuity were observed in the three-quarter gameplay view. Every camera angle, hardware audio, full-island terrain/AI combat, universal clipping and release performance were not observed.

## Defects and targeted revisions

| Severity / baseline frames | Observation | Correction |
| --- | --- | --- |
| Major,417–428; release at420 | Held X-pose layer drops from1 to0 instantly. Arms expose idle before the incoming rush takes over; upper/lower arm step peaks71.31deg. | Retain the evaluated charge pose and fade its layer over.12s while the existing rush blends in over.08s. Keep charge phase fixed during this handoff, rather than sampling it from the new rush clock. The layer is gone before the damaging rush cut. |
| Major,559–570; cancel at562 | Early release clears the pose phase and layer together; arms drop in one frame,83.56deg maximum step. | Preserve the evaluated pose during the same short fade back into continuing locomotion. Cancellation of gameplay is immediate: no queued rush, damage, stamina attack spend or immunity. |
| Minor, idle/movement27 onward | Existing ready-wrist adjustment disappears instantly when movement starts,38.03deg maximum local hand step. | Fade the existing cached local correction out over.08s. Never aim the hands toward the idle target during a moving attack. Idle blend-in remains.12s; full authored attack poses retain ownership after startup. Cache the swimming component rather than querying it each frame. |

This changes two existing C# implementations, `SahurAttack.cs` and `SahurTwinBladeReadyPose.cs`. It does not create or overwrite animation assets or replace full authored animation with procedural poses. Ready-pose and charge-fade durations are Inspector fields in English. No extra cameras, trails, particles, materials or runtime resource allocations were introduced by the gameplay change. No whole-game performance improvement is claimed from a recording pass.

## Re-review and functional checks

Maximum local-bone angular step within the first seven recorded frames after the same input ownership change:

| Observation | Before | After |
| --- | ---: | ---: |
| Rush release, arm |71.31deg|9.11deg|
| Rush release, hand |31.59deg|3.82deg|
| Early cancellation, arm |83.56deg|12.87deg|
| Early cancellation, hand |41.43deg|18.27deg|
| Idle/movement maximum hand step |38.03deg|7.95deg|

Guard/spin release metrics remain unchanged. The corrected sheets keep the X silhouette through rush entry; cancellation now lowers the authored arms across several frames. These measurements support the observed continuity fix, not a general animation-quality score.

The final focused recording passes **9 checks**. `Tools/TestBladeBlendV236.cs`, entry `TestRushGuardV236.Start30`, passes **54 checks /1008 captured frames** at30Hz simulation. It starts with real rifle-to-blade equipment, tests pause/resume during the outgoing charge blend, immediate swimming/disable/weapon-switch cleanup, early charge cancellation, original thresholds, real continuous blade contact and all prior rush/guard checks. Actual rush travel remains **16m**; a physical wall stops it at **1.58179m**; **25/25** observed movement collision callbacks are protected. Outgoing charge weight is zero during the damaging rush, startup/recovery remain vulnerable, frontal and rear damage cases retain their approved behavior, and the tutorial narrative health floor still works. Club charge behavior is retained.

Compile completed without compiler errors. A Pipeline main-thread timeout occurred during Editor reload/Play entry before the after-take; the same Editor recovered. The captured error identified the Pipeline request, not a gameplay exception. It was recorded in the task history and the console cleared after inspection. The remaining regression/exit checks report0 new Console errors. The initial launch using take name `before` was rejected because that directory intentionally contains backups; no recording was overwritten.

Outcome: targeted continuity corrections accepted for this polish pass. Existing action intent and gameplay are retained. Final artistic approval remains the user's.

## Preservation

Existing Editor7800 only. Original dirty `Story1.unity` remains loaded without save/reload/discard; its unsaved contents were saved as an independent copy outside Assets. The current human save was backed up before testing, diagnostic saves retained separately and the human save restored after confirmed Play Stop, SHA256 **80AE18C49EB1023655B5BA73412809312F95500C2FC3655062AE3EBF4285E976**. Source backups and final hashes are retained under the evidence directory. Final Editor stopped/unpaused. Previous v234 tutorial and v235 shared-controller changes are preserved. No build, commit, push, new Editor/Player or automation.
