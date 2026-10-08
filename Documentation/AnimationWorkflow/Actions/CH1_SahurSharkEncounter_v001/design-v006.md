# v006 approved production design

Approval: user message, 2026-10-07, “PLEASE IMPLEMENT THIS PLAN: 开场打斗、玩家操控与常用动画整体提升”. That message is the authority for the complete specification and authorizes implementation without another design gate.

The film is 20.8 seconds at 60 FPS, following playable combat. Equal exchange 0–4s; Sahur gains the advantage 4–10s; submerged flank, breach and support destruction reverse the advantage 10–16s; a missed counter and heavy tail defeat Sahur 16–20.8s. Right hand alone holds the stick. Preserve the final fallen pose, wave response, lost stick and island handoff.

Player movement owns ground/air/swim displacement. Mounted movement belongs to the actual dynamic board. Climbing tracks its moving edge. A cinematic takes ownership only after a safe action node, never midway through airborne locomotion. Board mounting and deliberate swimming toward a clear edge are automatic. Jump grace 100ms, buffer 150ms, regrab exclusion 350ms, climb intent 200ms and proximity 1.2m. Board limits remain 10/4.5m/s with 90deg/s steering.

Author new Actions on copies of the original rigs, retaining names, bind pose and previous resources. Improve causal sequencing, grounded stance, anticipation, extension, follow-through and recovery. The film chooses reachable real debris, or an equally long supported variant when no valid adjacent board exists. Do not manufacture or rearrange debris.

Runtime order: pose sampling, support correction, anatomical wood collision solving, final contact verification, reaction and camera. Visual targets: support error 3cm, planted sliding 5cm, critical contact gap 5cm. Unobserved or failed targets must be reported honestly. Scripted cues alone never prove contact.

Review continuous game/front/side/three-quarter views, ordinary controls and mobile attacks on island and wreck, swimming/climbing, sparse and dense debris, support breaking, pause and low frame rate. Preserve save data, current modifications, mesh collisions, natural buoyancy, ocean style, monochrome UI and disabled audio. No commit or push.

Implementation/review status: in progress. Source audit: `.codex/encounter-v006/source-audit.json`. The Mixamo source has no bound character mesh; its final visual acceptance requires actual Unity Sahur retargeting, not a Blender empty-rig render.
