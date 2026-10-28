# AI-assisted Sahur jump chop — v269 / v270

Installed a retargeted full-body diagonal jump chop from Sahur's own upper right to lower left. Club stays one-handed. Twin blades retain a primary diagonal chop and outward off-hand guard. Existing 0.85 s / 60 fps timing, real swept hit windows, damage, controls and physical jump ownership are unchanged.

Production clips: ClubJumpChopV269 and BladeJumpChopV270. Only the two jump state motion references changed in each of the gameplay, grounded and rifle controllers; byte comparison against fresh controller backups confirms all other serialized bytes are unchanged. The combination installer preserves these revisions. Original clips, Actions, rigs and source files are retained.

## Source and review

Meshy Motion Prime candidate `Sahur_Aerial_Diagonal_Chop_02`, retargeted offline onto the retained Sahur rig. Only the user-approved disposable gray proxy was uploaded under CC BY 4.0; no Sahur assets were uploaded. Total generation spend: 20 credits across two candidates; no additional generation during fitting. Canonical downloaded source and provenance are in ArtSource/Combat/v268.

Candidate 01 and raw retarget draft 1 were rejected. World-space retarget draft 2 kept the useful body/tuck/timing but had native weapon/head and off-hand/body intersections. v269 fits actual grip axes and forearms offline, preserving body motion; its entry still put the dagger through the tucked knee. v270 changes only the off-hand entry before .14 s. Existing Animator blends remain authoritative. Accepted as agent-reviewed FIRST PASS, not human FINAL art approval.

## Native verification

Final bound controllers, no preview overrides: 27 actual-input/contact/miss/recovery checks pass across 366 native camera frames. Both melee modes confirm exactly one true swept hit per victim; early Space+LMB and falling attacks work, a miss invents no landing damage, landing restores normal control, and single-hand club spin remains functional. 24 controller/timing/mask checks pass.

85 sampled weapon poses / 765 conservative rays against the current deformed Sahur mesh. Dominant hand/finger contacts are the held grip. The remaining conservative thigh warning at the recovery blend is disproved by 8,148 actual knife mesh edge tests (zero non-grip surface crossings). The earlier genuine knee intersection is absent after the v270 entry correction. Probe parent-scale calibration includes a known torso positive control; earlier incorrectly scaled/incomplete recordings are not acceptance evidence. This is sampled clearance in the fixture, not a guarantee for every pose/layout.

Presentation take: 108 normal-speed native frames / 17 contact/input checks, wider framing to show the entire weapon. Silent MP4: `.codex/jump-meshy-v270/presentation-30/Sahur-AI-JumpChops-v270.mp4`. Detailed evidence and acceptance JSON are in `.codex/jump-meshy-v270/`.

No new runtime IK, procedural bone controller or global time-scale code. Runtime continues to use two ordinary baked Humanoid clips. This is not a measured low-end hardware FPS improvement; full island playthrough, shipping build and subjective audio were not tested for this animation-only change.

Fresh resumed campaign save is byte-identical after testing. Original five additive scenes restored from latest on-disk versions, preserving other users' saved scene changes; the pre-resume transient empty/Environment/Enemies setup is also inventoried. No scene files saved, no second Unity Editor, build, commit, push or schedule.
