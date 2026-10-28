# Animation review

## Baseline — 2026-10-28

No new visual observations yet. Preserve current combat design and existing uncommitted V278 jump, V272/V273 blade, V274 momentum, V275 spin movement and V276 transitions. `Documentation/JumpStable-v278.md` records historical native verification and pending human artistic approval; do not relabel it as a current test or final approval.

Combat visual lifecycle exceptions are repaired and verified with native reload/pool sampling. `Evidence/baseline-editor.json` records installed controller states, timing and transitions. The existing `VerifyStableJumpV278.Run` was rerun this batch: all 30 installed-controller/Humanoid/timing/root contracts pass across grounded, gameplay and rifle controllers. No clip/controller was edited. Code and numeric contracts cannot establish perceived smoothness, arbitrary-pose clipping freedom, silhouette or weight.

`Evidence/first-island-after-reload.png` is an actual native Game-view still, inspected this batch: Sahur is standing in dense grass beside the sea in the existing pixel style. It establishes rendered scene availability after reload; it contains no attack sequence and does not verify smoother animation. A transient black Game view during earlier loading was observed through desktop UI and subsequently resolved.

Next review: moving attacks, combo buffering, charge release, jumping/landing recovery, dodge cancel, weapon switching and hit-window alignment using current native gameplay captures. Do not change clips or controllers without a reproduced issue.
