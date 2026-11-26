# Duck size, locomotion, wave UI and browser setup — Batch026

Batch 026 increased the duck’s size by 50%, based its locomotion on actual travel and moved the wave caption inside its card below the encounter HUD.

## Confirmed evidence and corrections

Native baseline duck capsule4.76 scene units was shorter than Sahur's scaled5.30071354 capsule. Uniform visual scale increases1.5× to6.508359;capsule height7.14000034,radius1.155,centerY3.5700002. This is2.1× the original3.4-height duck after the prior1.4× revision. Visual offset,capsule,melee/lunge ranges,separation andsurround radius grow consistently. Health,damage,world travel speeds,attack windows,gravity and original spherical Strike body remain unchanged. No model/UV/bone/FBX clip overwritten.

QA-037 reproduced:slow Chase emittedMotion2/Run at normalizedSpeed1,and zero horizontal travel also stayedRun. Shared Present() used only the AI mood;the no-slot circling branch travels at walk speed while stayingChase. A duck-only opt-in now records net horizontal displacement across both CharacterController.Move calls (motor.velocity alone reflects the later vertical move). Normalize byrunSpeed,damp acceleration over.12s and immediately select zero for no movement. Existing chicken policy defaultsfalse and passes native patrol/chase checks. Reactions/actions take priority over locomotion,includingIdle/cooldown with staleSpeed1.

New duckWalk/Run controller rates0.8428571/0.799999952 compensate the extra1.5× limb scale,retaining the previous world-stride product. Idle/Walk/Run fixed blend durations grow.10→.16s;self transitions remain disabled. Attack/reaction rates and blends remain1/.10s and1/.06s;authored duck strike/lunge durations remain.53/.8s. An initial fixture assertion incorrectly assumed the shared AI defaults.95/1.15s;that failing row is rejected as test evidence of a game bug. No duration correction was applied to the game.

QA-038 reproduced in native2560×1440 GameView:the wave text extended above both card and screen and the card overlapped actual BossHudPanel geometry. GameUITheme.Label anchors to its parent's top;this label needed a center anchor. The card now uses top-center pivot,reference1920×1080 offsetY-132,size740×64,and a centered700×48 label. Native accepted card y1178.67–1264,text1189.33–1253.33 and encounter HUD bottom1285.33 give a21.33px gap. Caption remains noninteractive and retains the existing wording/lifetime/wave logic.

## Native acceptance and animation review

31 final checks pass:card/text/HUD placement and noninteraction,size relative to scaled Sahur,actualWalk/Run/Idle state settlement,immediate stopping,stride products,no self restart,default chicken behavior,real surround/chase decisions,7 action/reaction priorities,and original sphere hit/repeat/far/cover controls. A close hit reduces100→98 after current mitigation;repeat cannot multiply damage,far target and cover stay protected. Character placement uses the scaled capsule sole and native collision synchronization. Source compiles successfully. Earlier16/26 intermediate sets overlap and are superseded,not added to31.

Review revision:LongLeggedDuck-controller-batch026,existing authored duck design,user-requested targetedPOLISH,2026-10-29. Native sample frames0/4/10/20/29 are observation indices,not a constant-FPS movie. Before frames0–29 wrongly stayedRun during slow/stationary travel. An intermediate acceleration-damping candidate leftWalk at stopping frames0/4;final zero handling selectsMotion0/Speed0 from frame0 and settles in actualIdle after the existing blend. Walk/Run similarly settle in their actual state without a pending transition.

Actual before/after size+UI and finalWalk/Idle GameView PNGs were inspected. Silhouette/size and caption readability are observed;continuous motion smoothness,weight,anticipation,impact and precision foot contact are not numerically scored from stills. No perfect-footIK or full-animation-release approval is claimed. This is bounded functional/state and rendered layout acceptance for the requested revision;full first-island waves,mixed enlarged flock/terrain and authored foot-contact polish remain next checks.

## Playwright MCP

A separate browser smoke test used Microsoft Playwright MCP 0.0.83 with Codex and Node 24.15.0. The client discovered 25 tools, opened Example Domain and closed its isolated headless Chrome session. This checked the browser protocol only. [Recorded result](../AUTO_QA/Evidence/batch026/browser-setup.json)

Setup references: [Microsoft Playwright MCP](https://github.com/microsoft/playwright-mcp) · [Codex MCP configuration](https://learn.chatgpt.com/docs/extend/mcp?surface=cli).

## Preservation and checkpoint

Same Editor only. Original5 scenes restored clean/stopped,fresh save+backup byte-identical and running marker clear. FBX/import metadata/material and chicken prefab match the Git baseline;original Strike code body matches the fresh source snapshot. Runtime pixel/pipeline/camera/time ownership restores. No standalone build/full FPS or full registry preference-restoration claim:this Editor batch did not intentionally write PlayerPrefs,and no fresh100-value pre-run registry snapshot was taken. Preserve user edits/unrelated Plastic checkouts and existing606 missing plants.

The batch directory contains the reproduction, accepted checks and final Editor state. The baseline was Git `314b6f1` / Plastic changeset 209. [Batch 026 evidence](../AUTO_QA/Evidence/batch026/)
