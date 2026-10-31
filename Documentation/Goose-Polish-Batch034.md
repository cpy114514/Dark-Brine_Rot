# Goose animation and size — Batch034

User request: improve the existing in-game goose animation and make it larger. This uses the existing, initially untracked BullyGoose asset; it does not regenerate or replace the reference model.

Changes
- Visual size, offset, capsule and body-dependent attack/separation distances enlarged 30%. Capsule height 4.60 → 5.98; root scale and capsule sole remain unchanged.
- Enabled the existing actual-movement locomotion option on this goose only. A blocked patrol/chase now presents idle instead of running in place.
- Separate Idle/Walk/Run AnyState restarts replaced by one continuous Speed blend tree. Walk/run changes retain normalized cycle progress.
- Corrected stride calibration to count both stance excursions in a full left/right cycle. Walk playback 3.60 → 1.383815 and run 3.60 → 1.540028: walking 6.75 → 2.594652 cycles/s; running 9.00 → 3.850069 cycles/s. Movement speeds remain 2/5 m/s.
- Three separate Polish034 loop revisions retain source duration/interior poses and share periodic boundary tangents. Original FBX, mesh/UV/weights/bind pose, embedded clips, controller, textures and material are unchanged.
- Original attack, wing, alert, hit and death clips remain bound directly. Damage, durations and hit windows are unchanged. Installer preserves the reviewed controller and new sizing on future reinstall.

Validation
- 70 final native asset/controller/AI contracts passed, zero failed. Includes original-file hashes, 30/60 Hz walk/run continuity, all seven action transitions and recovery, idle blend weight, and actual AI Present with zero movement.
- 21 original descendant transforms match the native original snapshot; current arena references the updated prefab.
- 12 loop-boundary bone probes retain zero positional seams. Run head finite-difference velocity discontinuity 1.952002 → 0.223807 m/s; walk 0.586732 → 0.066348 m/s, despite 30% larger geometry. This is a local diagnostic, not a perfect contact/whole-clip quality guarantee.
- Qualified 180-frame/30 Hz/six-second native Unity manual-graph preview uses actual baked skins and preserves pose changes. Left original; right revised. Foot motion positive controls 0.339628/0.446735 m. Two representative rendered frames inspected. Original fast gait and new slower gait share the same timeline.
- Installer and final helper compile successfully. No new Assets C# code or project-wide domain reload, full build, FPS benchmark or complete live combat test was needed/performed.

Rejected observations and corrections
- First film used live SkinnedMeshRenderers in a preview scene while gameplay was paused: images were identical despite controller clocks advancing. Rejected, retained privately. Final film uses manual native PlayableGraph evaluation plus BakeMesh(true) and motion/image positive controls.
- Direct clip sampling initially serialized temporary bone pose overrides. Restored original native transforms and original nested transform overrides; final 21-transform comparison passes. The prefab patch now contains only requested scale/capsule/range/locomotion/controller edits.
- A preliminary BakeMesh(false)+TransformPoint sole reading was unqualified; it is not ground-contact evidence and does not establish floating or penetration.

Limits / continuation
- Current user CombatTest Play session was already paused (timeScale0) with a dead goose. It was neither stopped nor restarted; no scene/save/preference write or revival was performed. Newly spawned geese use the new prefab. Full moving/turning/collision combat and precise skinned-foot contact remain for the next appropriate live test.
- This is an agent polish revision, not human FINAL art approval. Original short-legged silhouette and authored attack poses remain; it is not a new rig or IK system.
- Original goose model/prefab/installer and eight unrelated tracked edits were already uncommitted at entry. Preserve them. The checkpoint includes only this task's new revision assets/helper/reports and precise patches against the pre-existing untracked prefab/installer. Do not blanket-add the original goose or push unrelated work. The working prefab/installer are already updated locally; full goose publication waits for integration of its pre-existing baseline.
- No Plastic update/checkin or GitHub push for this focused revision. Original workspace remains subject to batch033 Plastic safeguards.

Evidence: AUTO_QA/Evidence/batch034. Private original snapshots and rejected takes: .codex/goose034. Accepted preview: goose-before-after.mp4.
