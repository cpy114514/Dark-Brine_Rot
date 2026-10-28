# AI jump chop v268 — blocking review

Design remains a one-handed club diagonal aerial chop, own upper right toward lower left; both melee modes. Meshy candidate 02 supplies the full-body timing and pose, retargeted offline onto the retained Sahur skeleton. Source root translation is removed, source rest/bind and destination rest/bind are untouched. Original attack duration 0.85 s / 60 fps and contact window are retained.

Candidate 01 is rejected: insufficient overhead preparation and forward lunging motion. The first downloaded all-actions skeleton file was a different result and must not be used as evidence about the newly selected candidate. Candidate 02 has a readable high right arm and coordinated leg tuck / body lean, but generated vertical travel is excessive and must remain excluded from gameplay.

Raw retarget draft 1 is rejected: incompatible armature coordinate spaces caused excessive body pitch and backward weapon direction. Draft 2 corrects world-space rest rotation transfer. Front/side/three-quarter samples at frames 1,7,13,19,25,31,37,43,52 show a raised club and diagonal follow-through with a compact leg pose. Accepted only for BLOCKING native preview. Weapon/head clearance, native weapon attachment, contact alignment, transitions and twin blade presentation require actual Unity review before any production binding. No final artistic or user approval is claimed.

Review evidence: `preview-draft2/Club/`; source candidate and credit/download details are in `Documentation/AIAnimationSources-v267.md`.

Final continuation: see Documentation/MeshyJumpChop-v270.md. Installed club v269 / twin-blade v270 pass bound native review. v268 and blade v269 remain preserved rejected/intermediate sources.
