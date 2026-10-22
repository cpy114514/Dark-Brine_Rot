# Sahur–shark film continuity polish v237 — 2026-10-21

The user requested the same record/review/polish process used for the twin blades, retaining the established opening. This pass covers the existing crush, tutorial, three pursuit bites, climb, overhead counter, rear ambush/prone dodge, throw/catch, feint/tail strike and unconscious drift. Existing action intent, order, source animation clips, original shark scale, tutorial bindings, swimming control and nonlethal damage rules remain.

## Recorded findings and revision

The baseline is a complete native-camera recording, `baseline-30`, under `.codex/shark-animation-polish-20261021/`. It contains2534 observed frames/2533 camera frames through Finished,70HP, two real tutorial hits, three cinematic contacts and9 native plank fractures. Subsequent records and comparisons remain beside it. These are deterministic simulation recordings, not hardware FPS measurements.

Observed major defects in consecutive final-camera frames:

- Pursuit entry at frame1194 jumps the shark nose39.70m and changes the rendered hero arm pose by103.16deg. The next bite changes direction by18.77m at4.03s, then24.11m at7.53s.
- After the rear pass, the long body turns while still across the wreck field. Repeated native solid separation produces alternating nose steps up to51.03m around Duel13.1–13.4s. Zero overlap after such a correction does not make that movement acceptable animation.
- The prone shot aims above the standing board position and can lose Sahur behind foreground wreckage. The three-quarter native views demonstrate the occlusion; numeric contact success alone did not establish shot readability.

Targeted changes:

- Transfer the existing live swimming clip/phase into the cinematic Playable before blending into the approved pursuit swim over.28s; ease the existing facing handoff over.35s. No new authored action or rig change.
- Carry the previous **resolved** shark nose and orientation into each movement segment. Blend only the real cinematic sample, not hypothetical contact/route probes. Keep the existing.18s bone-pose handoff across actual actions, with an optional explicit identity for separately authored cinematic clips.
- Dive before changing direction through the dense timber field. After the rear pass, keep the body outboard, submerge before yawing, turn below the wreckage and return to the established throw target. The original approved-size animal's renderer bounds exceed40m vertically; the initial shallow turn did not clear submerged timber. Retreat uses a longer1.4s handoff rather than forcing the prior escape offset back into the next path in.35s.
- Retain the native collision solver's resolved avoidance offset between movement samples, with a time-based exponential decay once the route clears. Record the resolved pose only after the final solids pass. The original fitted overhead and tail-contact paths retain exact ownership; no smoothing delays their real contacts.
- The submerged search now follows a continuous180-degree rotation. Linear interpolation of opposite direction vectors previously crossed zero and flipped the body instantly. Keep the original-size animal below the water during that turn.
- Keep shot34 on the actual prone torso and permit a closer clear view past foreground fragments. It stays on the existing side with the same FOV, using a higher nearby angle to see past wave crests; objects are not hidden and the original frontal climb/rear reveal remain.

Only existing C# sources `Story1WreckBattle.Redesign.cs`, `Story1WreckBattle.Camera.cs` and `TralaleroSwimAnimator.cs` change for this pass. Earlier v234 tutorial-contact changes and v235/v236 blade work remain. There is no animation/model/prefab/scene asset rewrite. Runtime additions reuse cached pose arrays and a few transform offsets; the prone camera's additional range candidates run in the existing cut/obstruction search. No new runtime VFX, materials or per-frame asset loading are introduced. Whole-game performance is not certified by captured Editor timing.

## Evidence and acceptance record

Reproduction tools outside Assets:

- `ReviewSharkAnimationV237.Start(label,fps,images)` records the entire original route with actual synthetic Input System keys/buttons and final SRP camera completions.
- `VerifyTutorialContactsV237.Start(label)` independently bakes the rendered shark at both confirmed tutorial hits, checking actual weapon/skin contact, measured soles, body envelopes and original timber.
- `AuditSharkFilmV237.Start(label)` independently rebuilds the posed shark triangle index at cinematic contacts and regular later-film samples, checking body envelopes and exact timber triangles.
- `AnalyzeSharkAnimationV237.py`, `VerifySharkAnimationV237.py` and `CompareSharkAnimationV237.py` produce consecutive review sheets, measured seam checks, normal-rate movies and aligned before/after footage.

Review method is consecutive actual-camera frames, key-pose sheets and final-pose geometry. It is not a claim of watching the entire MP4 at normal speed or of human FINAL approval. Full-angle/hardware audio/release performance and arbitrary wreck layouts remain outside this targeted pass.

Retained iteration history:

- `after-30`: rejected because the recording fixture lost cursor focus at the Move gate. No forced success or tutorial skip. The fixture now maintains focus with its actual input injection.
- `after2-30`: all native contacts and route completion pass, but shallow turning still causes a41.12m native correction and some support/camera height is poor. Rejected for presentation.
- `after3-30`: full route,70HP, two native tutorial contacts, three cinematic contacts and79 later geometry samples pass, with no shark/timber overlap. Retreat converges too quickly,7.91m maximum step, and the prone view remains occluded. Rejected; its data is not substituted for final acceptance.
- `after4-30`: functional checks pass, but a6.20m support correction remains during retreat and the distant prone view still loses the actor. Rejected; increase outboard space before the turn and bring the same-side shot closer.
- `verified-30`: full functional/contact checks pass, but the return-to-surface movement peaks at4.258m/frame against the unchanged4m/30Hz continuity criterion. Preserve this rejected take; widen the existing rise from0.6s to0.9s and raise the nearby prone camera.
- `final-10`: a916-frame low-step run passes functional/contact/geometry checks and the rate-scaled continuity checks. This precedes only the final submerged-search direction correction, so the frozen-source replay must still be repeated. The original low-rate threshold used a fixed distance instead of scaling with the longer timestep; the verifier now uses the same120 units/s threshold at all simulation rates.

Final normal-step replay `accepted-30` has2534 observed frames/2533 camera frames,70HP, two independent real tutorial hit passes, three cinematic contacts and6 native wood fractures. All23 functional/geometry/continuity checks pass.79 fresh later-film mesh samples have minimum shark/body gap0.004944m and zero shark/timber triangle intersections. These are sampled checks, not a proof that every vertex is collision-free throughout every possible playthrough.

Compared at aligned simulation windows: pursuit entry shark step39.70→3.12m; side bite18.77→3.58m; climb approach24.11→3.63m; rear retreat51.03→3.84m. Entry arm discontinuity103.16→9.47deg. Same narrow seam checks and the unchanged rate-scaled continuity threshold are retained; do not confuse a camera cut with a transform discontinuity.

Consecutive final-camera sheets reviewed at swimming/side bite, climb, overhead, prone/get-up, throw/catch, feint/tail and flight. The prone actor is clearer near the end of shot34. **Remaining presentation defect:** around Duel10.35–10.6s a high crest washes over the actual support and obscures much of the actor; raising the lens does not fix immersion at the subject. Some foreground wreckage also remains prominent. Do not label this fully clear, clipping-free, AAA quality or human FINAL approval. This checkpoint accepts targeted movement continuity and contact preservation only; a further support/crest pass is required for complete prone-shot readability.

Movies: `.codex/shark-animation-polish-20261021/accepted-30/SahurShark-accepted-30.mp4` (full route, silent) and `.codex/shark-animation-polish-20261021/SahurShark-BeforeAfter-v237.mp4` (aligned comparison). Frozen source hashes are in `frozen-source-sha256.json`; final verification includes the independent low-step replay. Original-size/native fracture/contact requirements are mandatory; artistic approval remains with the user.

## Preservation

Existing Unity Editor7800 only. Protected current human save SHA256 **80AE18C49EB1023655B5BA73412809312F95500C2FC3655062AE3EBF4285E976**, backed up before tests. Each diagnostic save is kept separately and the current human save restored only after confirmed Play Stop. Original dirty Story1 remains unsaved, with a separate save-as-copy outside Assets. No second Editor/Player, build, commit, push or new automation.

Final frozen-source low-step replay `verified-10`:955frames,70HP,79 independent posed-mesh samples, minimum body gap0.028450m, zero shark/timber intersections;23/23 checks pass. Together the two final replays pass46/46 checks. Source hashes match the frozen manifest. Editor stopped/unpaused, original dirty Story1 still loaded, Console errors0. Human save restored and SHA256 verified after Stop. Both encoded MP4s exist (15,038,160-byte full route;1,250,504-byte comparison). See `final-verification.json`.
