# v088–v090 parry and contact tests

In the v088 four-view capture, all six strikes met their contact threshold, but Sahur’s planted foot slid 9.692 cm during recovery at 1.717 s. A separate v088 Player run missed the first two strikes and failed its frame-rate target. The layouts evolved independently.

## Parry revision

The v090 parry starts with Guard v047 on Sahur’s existing 28-bone rig. The hips shift weight over the planted soles, the right arm deflects the strike and the free left hand balances the torso. The initial hand target was outside the arm’s reach. Moving the target inside the upper- and lower-arm span preserved the original bone lengths. The clip runs for 1.1 s at 60 FPS, with contact at 0.484 s.

The Unity import used the original Avatar and contained 130 Humanoid curves. Source foot drift stayed below 0.32 micrometres. The isolated Unity preview had 4.061 cm ankle drift before runtime IK. The integrated take measured 1.393 mm of foot slide. The installer replaced one parry beat and saved the previous definition in `Resources/Encounter/v090/PreviousEncounterDefinition.asset`.

## Native integration, seed 14

| Check | Observation |
| --- | --- |
| Six strike gaps, in order | 0.029621, 0, 0, 0.031210, 0.005495 and 0 m across 625 recorded frames. |
| Support and framing | Foot height error was 1.395 mm. Sahur’s renderer stayed inside the frame in all 541 measured frames. Foreground waves and timber still covered parts of the weapon or actor in some views. |
| Ending | Across 264 frames the fallen pose held, with no clock drift. The actor stayed still through 25 paused frames, lost the stick and arrived unconscious on shore. Head clearance above water was 0.04 m. |
| Coverage | Four continuous views were encoded. Review covered the contact sheet and throw frames 249 and 267. This same-board take did not exercise the v086 hop or establish final visual approval. |

The v089 escape check tested the most recent blocking anatomy first, retaining the original 3 mm threshold and escape order. All 150 native probes agreed across five poses (122 clear, 28 blocked). The independent v090 build took 34.1 s and reported one error and 488 warnings. The exception and standalone performance are documented in the [v092 build review](implementation-review-v092.md).
