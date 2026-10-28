# AI animation sources — v267

## User direction

- Hunyuan 3D: https://3d.hunyuan.tencent.com/ — preferred for AI image-to-model work; user reports generous daily allowance.
- Meshy: https://www.meshy.ai/zh/workspace?sidebar=animate — animation sourcing, avoid generating replacement character models.
- Tripo: https://studio.tripo3d.ai/zh/workspace/animate — alternative animation sourcing.
- Conserve animation credits. Inspect previews, required rigging and export permissions before batch generation. Do not purchase subscriptions or upgrades.
- The user rejects v266 jump-chop artistic quality. Passing numerical trajectory/contact checks did not establish good animation. Keep sources for rollback; do not treat v266 as approved.
- Approved attack remains one diagonal aerial chop, from the actor's upper right toward lower left, ending slightly left. Both melee modes; club is one-handed. Preserve responsive input, damage and physical jump ownership.

## Browser verification

The three user-opened tabs were inspected through Computer Use in the already signed-in accounts. No credentials were read or copied.

### Meshy

- Motion Prime text-to-animation dialog displays 10 credits for a 3-second candidate.
- Generated **one** candidate, `Sahur_RightHigh_LeftLow_JumpChop_01`, and saved it into the AI animation library. Balance visibly changed 320 -> 310. Do not generate duplicates to resolve a delayed balance update.
- Prompt:

  > One powerful single right-handed jumping diagonal chop. Start in a relaxed combat stance. Lift the right hand above the right shoulder, jump with bent knees, then rapidly slash diagonally from own upper right to lower left. Twist hips and torso naturally, left arm balances. Land softly, absorb weight, recover. One attack only, no spins or flips, no walking, full-body grounded motion.

- Candidate has a browser mannequin preview; its card says a rigged model is required. Its More menu offers Rename/Delete only. No independent skeletal download was exposed in the inspected preview.
- Own-model upload defaults to **CC BY 4.0**. Selecting Private opens a subscription modal. No Sahur/project assets were uploaded and no license was accepted on their behalf.
- Prepared a newly authored, untextured, static mannequin: `.codex/ai-motion-v267/TemporaryMotionProxy.obj`, with matching `.blend`. It contains no existing project mesh, skeleton, texture or scene. Builder: `Tools/AnimationPipeline/blender/build_motion_proxy_v267.py`. 6,312 vertices / 9,765 polygons. It is a temporary motion carrier, never a replacement Sahur model.
- User explicitly confirmed this exact temporary proxy upload under CC BY 4.0. Uploaded the 693.24 KB OBJ, retained the existing 1.7 m framing, and completed the two-legged automatic rigging flow. The rigged proxy is now an account asset. No original Sahur data was transmitted. Balance remained 310 after binding.
- Candidate screenshot: `.codex/ai-motion-v267/meshy-candidate-preview.png`. A still frame and generation success do **not** establish animation quality or Unity suitability.
- On continuation, the rigged proxy accepted the candidate in its added list. Downloaded a 60 fps, skeleton-only FBX to `C:/Users/pinyu/Downloads/Meshy_AI_TemporaryMotionProxy_All_Animations.fbx`. The download counter shown beside the button changed x10 to x9; the visible credit balance remained 310, so the counter must not be assumed to be credits.
- Inspection in a separate Blender process: one action `target_character|Scene`, frames 2–180 at 60 fps. With NLA disabled, 21 samples show the right hand remains below the head throughout; from roughly frame 46 onward sampled joint positions stay essentially constant. This file does not exhibit the requested raised diagonal chop at those samples. It is not verified that the site exported the intended candidate rather than another current animation. Inspection data: `.codex/ai-motion-v267/inspection-no-nla/inspection.json`.
- After the prior turn was interrupted the browser reported that local browser use is disabled in Work settings. Do not use another browser automation channel to bypass that setting. Correct candidate selection/export remains unverified. Existing Unity Editor was playing when checked; do not interrupt its play session or overwrite its save.
- Full integer-frame audit (179 frames, frames 2–180) confirms this is not a sparse-sampling artifact: right-hand height relative to head is -0.454 to -0.212 m; the last inter-frame joint displacement exceeding 1 mm occurs at frame 40. Hips height ranges 0.859–0.923 m. All 360 imported curves extend to frame 180. Evidence: `.codex/ai-motion-v267/full-frame-audit/inspection.json` and `summary.json`. These measurements reject this downloaded file for the required action; they do not prove why the website selected/exported it.

### Tripo

- Signed-in UI shows 200 credits. Text motion panel displays 20 credits at 5 seconds; no generation was started.
- Opened an existing demo model, inspected preset library and export panel. Final Export opens **Upgrade to export in multiple formats** subscription modal. No paid action, upload, rigging or generation was performed.
- Export was checked before spending scarce animation credits.

### Hunyuan

- Animation page resolves to `/studio/creation/ae`; UI currently shows 23 daily generations remaining.
- Inspected existing animation templates and character selector. Offers preset male/female mannequins and bound assets. The inspected page exposes presets (charged attack/punch, locomotion, jumps, etc.), with no matching diagonal jump-chop or custom text input exposed.
- No generation spent and no model uploaded.

## Historical gate before browser recovery

The downloaded FBX has not been established as the intended candidate and is not suitable as sampled; the candidate has **not been fully reviewed, retargeted or installed**. Do not claim the jump chop is fixed. Review the entire action, then native Sahur weapon path/silhouette, landing/recovery, transitions, both weapon modes and true hit windows before changing controller bindings. Keep current user scenes/save and unrelated Chicken Bro/rifle work intact.

## Browser recovery and native retarget continuation

Restarting the desktop app restored browser control. The prior open tabs had closed; opened Meshy again in the same in-app browser. Login and candidate 01 remained, with 310 credits. Explicitly selected the rigged proxy and added candidate 01, then exported **current** animation with skin at 60 fps. This export (`Meshy_AI_TemporaryMotionProxy_01a17c1c_d55d_7358_a7.fbx`) differs from the initial all-actions skeleton file. Its reviewed complete mannequin sequence has excessive forward leaning and insufficient high diagonal preparation; rejected for this move.

Generated exactly one further candidate, `Sahur_Aerial_Diagonal_Chop_02`, for 10 credits, leaving 300. Prompt:

> A warrior performs ONE jumping sword chop. He crouches, raises his RIGHT hand high above his RIGHT shoulder with elbow bent, then jumps UP with BOTH feet off the floor and knees tucked. While airborne he swings his right arm diagonally DOWN across his chest to his LEFT hip. His torso twists with the strike. He lands on both feet, bends his knees and recovers. Left hand stays free for balance.

Explicit Add to Character completed; current export with skin is `Meshy_AI_TemporaryMotionProxy_01a17c3e_ae70_7126_87.fbx`. Canonical local copy: `ArtSource/Combat/v268/Meshy_Sahur_Aerial_Diagonal_Chop_02.fbx`. Candidate 02 has readable raised right arm / leg tuck / diagonal motion, but excessive generated root travel. Retarget source offline to the retained Sahur rig, exclude generated translation, retain controller-owned physical jump. Correct world-space rotation transfer between differing armature spaces; raw draft 1 is rejected. Working draft 2 and review are in `ArtSource/Combat/v268/`.

Both 0.85 s / 60 fps clips staged successfully with valid native Humanoid Avatar. Temporary runtime overrides only: native `draft2-30` completed 368 rendered frames and 27 actual-input / true-contact / miss / recovery / single-hand-spin checks. Do not equate these with final artistic acceptance. Exact deformed-mesh rays and unobstructed front-camera review are being recorded separately. No production controller changes yet. Human Play progress was saved and backed up freshly in `.codex/jump-meshy-v268/`, then original five clean scenes inventoried before isolated review.

The three downloads consumed the displayed free-download counter x10 -> x7, not credit balance. Total actual animation generation spend is 20 credits (320 -> 300). No subscription or new model generation.

## Completed native continuation

See Documentation/MeshyJumpChop-v270.md and .codex/jump-meshy-v270/acceptance.json. Installed fitted club v269 and off-hand-safe blade v270 after actual native input/contact and calibrated mesh review. Earlier pending/rejected/incomplete takes above are historical, not the final project state. Total generation remains 20 credits; no later generations.
