# Opening optimization v039 — approved 2026-10-12

Approval: user “开始实施计划”, following the jointly specified optimization plan.

Optimize existing content only. Keep original walk/run/roll, the original rigs,
single right-hand stick, mobile charge, silent audio, monochrome UI with coloured
images, real freely floating ship fragments and the unconscious shore ending.

## Runtime contract

- Shark begins at 1000 HP. Shark <=300 HP, player <=30% maximum HP, or one
  sampled 20–35 second deadline latches the finale request. Protect both actors
  immediately. Finish the current safe action within one second; airborne actors
  retain physics until a real landing/splash. Do not accept new attack chains.
- Charge reaches full damage and gradually develops its pose over 1.8 seconds.
  Release begins from the current pose and accelerates through actual weapon
  contact. Preserve damage/stamina and movement. Approximate full release 0.85s.
- Strong wind: GPU sail billow/edge flutter and flags with fixed attachments;
  heavy hull inertia and displaced bow water feeding existing side foam/wake.
- Profile sailing, breakup, playable combat, handoff and full film separately.
  Target an independent 1920x1080 build on i5-14400F/RTX4060: 60 FPS, 1% low >=50,
  remove transition stalls over 50ms. Editor profiles are diagnostic only.

## Paired animation and review

20.8 seconds authored at 60 FPS. Close exchanges alternate with wide ocean shots.
0–4s equal probe/parry; 4–10s board evade, genuine jump/landing, diagonal counter
and throw/retrieve show Sahur's advantage; 10–16s submerged flank, breach and tail
break force an unstable defensive landing; 16–20.8s missed counter, tail defeat,
sequential chest/hip reaction, grip release and unconscious fall.

Modify actual bone poses/weight transfer/arcs on versioned source copies. Six
major camera beats support action rather than replacing it. Routes use real
reachable fragments. No safe neighbour => same-duration supported-board variant.
No viable water entry => existing safe unconscious ending; no invented boards,
teleports, false contacts or endless retries. Contact <=5cm, support <=3cm and
planted slide <=5cm are measured acceptance targets, not presumed guarantees.

Review continuous gameplay/front/side/three-quarter views at normal speed, plus
pause, low FPS, thresholds crossed by a single heavy hit, charge/sprint/direction,
water/air/climb/dense wreck/no neighbours/broken support and held floating defeat.

## Evidence and protection

Current save and source backups: `.codex/opening-optimization-20261012/backups/`.
Restore this current save after stopping each verification, never an older save.
Baseline editor (1440p): `profile-before.json` in the same directory. Film mean
30.56ms, p99 324.37ms, maximum 2495.99ms. These are not standalone 1080p results.
No commit/push. Original assets and unrelated user modifications remain intact.

## Approved amendment v055 — 2026-10-12

Latest direct user instruction: remove treating timber as a surfboard because it feels strange. This supersedes ALL earlier automatic board driving, W/S propulsion, A/D board steering and leg-paddle requirements, including older design-v006 and automation text. Wreckage remains a naturally floating physical walking platform. Retain normal walking/running/rolling, mobile combat, jumping with platform momentum, swimming, continuous moving-edge climbing, mesh collision, breakup and the paired finale. Neither player input nor the film may propel or steer wood as a vehicle. Retain historical source/rig/animation assets as inactive versions; do not restore old paddle states. Full opening optimization and standalone performance acceptance remain incomplete.
