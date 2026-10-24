# Sahur / shark direction and action ownership revision v241

User request: carefully repair reversed actions throughout the existing opening fight. Approved story, controls, contact rules and original shark size remain.

## Observed causes and actual changes

1. The imported `PLAYER_Walk_v006` faces opposite the current guard rig. Native skeletal measurements and baked front/side previews confirm approximately -0.995 chest/root alignment. v240 checked root travel only and consequently missed the rendered reversal. The new `PLAYER_Walk_v241` retains the full action and uses a 180 degree import orientation correction; tutorial staging uses this revision.
2. `PLAYER_Climb_v010` has the same opposite forward convention. Its old outside-root compensation also reversed the root on entry, making transitions inconsistent with the forward swim/guard rig. `PLAYER_Climb_v241` normalizes the import, removing the external compensation.
3. More seriously, Pursuit sampled Swim unconditionally, then Climb, on every climbing frame. Baseline recorded 102 climb frames with maximum current-clip mixer weight **0**. The transition restarted every frame, rendering a swimming body instead of the intended climb. Only one complete action now owns a pursuit frame; the climb finishes its 0.18s entry blend and plays uninterrupted.
4. Tutorial dodge previously held the roll at 0.95s, abandoned its authored 1.4667s recovery at 1.05s, and snapped the root back toward the shark. Play the complete recovery, blend entry, pivot into the selected open direction and return to the unchanged attack heading during the authored gather. Adaptive destination/path selection and bite clearance remain.
5. Separate cinematic bites can reuse one shark clip with a rewound phase. Their phase handoff now gets a cached segment/action identity, allowing the existing local anatomical blend to work even when the clip name is unchanged. Identity strings are created on segment/action changes, not per frame. Exact overhead/tail contact ownership remains.

## Preservation and production scope

Both revised FBXs are copies created through Unity APIs; source bytes are identical to their originals. Only the duplicated import take orientation/name changes. Original models, actions, avatars, skeletons, timing, looping and all earlier v239 combat revisions are retained. No arbitrary runtime skeletal posing replaces the actions. This is targeted integration/polish acceptance, not invented human FINAL approval.

Existing Editor only. No scene/prefab/controller serialization edits, second Editor, player build, push, UVCS change or scheduling. Native simulation-rate recordings are functional/art-review evidence, not hardware FPS benchmarks. Removing the per-frame Swim/Climb restart also removes repeated playable recreation during that section.

## Evidence and limitations

Evidence root: `.codex/shark-direction-20261023/`.

- `baseline-30`: 2535 native rows, 2534 gameplay-camera frames, full route at 70HP; two tutorial contacts and three cinematic contacts, real wood fracture. Original climb has zero visible mixer ownership. One shark transition also has a 14.97m native separation correction in this layout; retained as a failure rather than suppressing its check.
- Initial isolated `source` rendering did not refresh skinned geometry; `baked-source` / v2 used inconsistent bake scale. They are rejected preview experiments. Use **`calibration` and `normalized`** only: scale-aware native baked skinned geometry, actual weapon transforms, front/side views. `normalized` samples 20 actions / 180 poses / two views, including original/revised walk and climb, swim variants and the full roll. Do not interpret head bowing for an overhead or face-up unconscious drift as reversed locomotion.
- `revised-30`: 2518 native rows, 2517 gameplay-camera frames; 23 contact/geometry/continuity regression checks pass, 70HP, ten actual fractures, 80 independently baked later-film samples, minimum conservative body gap 0.00517m and no sampled shark/wood triangle intersections. Climb reaches full weight, rendered walk points forward. The independent board-relative walk observer was attached/revised during the take and missed the first lesson; this is **not** final two-lesson coverage. All recordings preserve their logs.
- Final frozen-source recording and acceptance results are appended after verification below.

Review method is actual consecutive frames and contact sheets, including native front/side debug views. Encoding or opening the silent MP4 is not a claim to have watched it at normal speed or reviewed sound. Timber/wave occlusion in several shots remains a separate readability issue; neither the numeric direction checks nor sparse geometric probes certify every layout/frame as clipping-free.

## Final existing-Editor verification and review

`final-30` uses the loaded `redFilmPoseSegment` sentinel and frozen runtime/import hashes. It records **2511 native rows / 2510 gameplay-camera frames**, finishes at **70HP**, confirms two actual tutorial hits and three film contacts, and physically fractures ten native timber pieces. All **23 contact/geometry/continuity checks plus 15 direction/action checks pass (38 total)**. Console errors: zero.

- Both lesson approaches have independent final board-relative motion samples: 25 samples; minimum **rendered chest vs actual travel alignment 0.989963**. This validates the mesh/skeleton, not just the root heading.
- The 90 settled climb samples all have clip weight **1.0**, replacing the baseline's 102 frames at weight0. Root handoff yaw increments remain below12.85deg at30Hz simulation; there is no half-turn entry compensation.
- Actual tutorial roll time reaches **1.466657s**, retaining the recovery. Largest root yaw increment is35.45deg across the adaptive pivot/return.
- New walk uses retained imported frames **1–73 at60FPS**,1.2s,loop; new climb uses **1–70 at60FPS**,1.15s,nonloop. Both normalized imports have rotationOffset180. Source FBX hashes are unchanged (copies, not remodeled characters).
- 80 independent later-film posed mesh/body/timber samples have minimum conservative body gap0.004908m and no shark/native timber triangle intersections. Both independent tutorial contact/sole/timber probes pass. This remains sampled evidence, not a universal clipping guarantee.

Actual inspected evidence: `normalized/forward-comparison.jpg`; final `poses-walk-{front,side,game}`, `poses-dodge-*`, `poses-climb-*`, `poses-overhead-*`, `poses-rear-dodge-*`, `poses-get-up-throw-*`, `poses-tail-flight-*`; consecutive native transition sheets from `AnalyzeSharkAnimationV241.py`. Debug front/side renders temporarily isolate the actual live actor's render layer in a try/finally, restoring it before the gameplay camera renders. They do not replace the action, drive bones or remove obstacles from the real game. Sheets label actual captured frame/clip/stage and cannot cross the climb/duel camera-heading boundary when selecting nearby detail frames.

Review outcome: **accepted for this targeted direction/ownership repair**, reviewer Codex,2026-10-23. Forward gait, climb ownership and roll recovery are observed improved; overhead downswing, rear turn/prone, stand/throw and tail flight retain their approved direction intent in the inspected actual poses. No invented numeric art scores or human FINAL approval. Readability still needs work where wave crests or foreground timber obscure the low gameplay camera; posing/support polish outside these corrections is not certified.

Silent full gameplay-camera video: `.codex/shark-direction-20261023/final-30/SahurShark-final-30.mp4`. The MP4 is encoded from native frames without interpolation. Contact sheets/consecutive frames were reviewed; normal-speed audiovisual review is not claimed.

Original user-saved Story1 file is byte-identical to the fresh backup (SHA256f13d1a3d625c04acf199c0713dde07f17fb25ddfd2329024bc514422c94f0c4d). Human save was backed up again this turn and restored **after confirmed Stop** (SHA2568c65d7bcabe0c563aad31ed60e0bee2ac363d93edb721826986062b035357000); diagnostic test save is retained separately. Existing Editor ends stopped,unpaused,original Story1 clean. Earlier uncommitted melee work remains. No commit/push/build/UVCS operation or scheduling.
