# Chicken Bro grounding and larger duck — Batch020

The user reported Chicken Bro shaking and floating, and requested a larger duck. Continued from Git461a66a / Plasticcs203 after integrating contributorcs202. The existing four visual styles and independent pixel mode remain intact.

## Accepted changes

- ChickenBroAI's wander circle is4.5m, but the old return-home threshold was0.8m. Crossing that threshold changed the goal back and forth. Return now starts outside6m and finishes inside0.8m. A separate patrol timer prevents NavMesh repathing from overwriting patrol selection. Pool activation resets both new fields.
- Within melee range during cooldown, the old branch only faced the player and retained Chase/run presentation. It now integrates gravity and presents Idle while waiting. Damage, windows, cooldowns, original spherical hit volume and reaction rules are preserved.
- Chicken model child Y changes0.7371→0.08710003 (-0.65m). Root scale, CharacterController, FBX, rig, clips and chicken controller stay unchanged.
- Duck visual size increases40% uniformly; capsule height3.4→4.76m and radius0.55→0.77m. Separation, surrounding and lunge ranges scale with size. Melee range3.21m preserves prior horizontal reach padding; attack radius1.35→1.89m. Movement speeds, damage and attack timing are unchanged.
- Duck Walk speed1.77→1.2642857 and Run1.68→1.2 compensate for40% larger stride at unchanged travel speed. Other state speeds/clips/transitions are unchanged. No source animation file was edited.

## Native verification

27 final native checks pass in isolated CombatTest: fixed-target movement beyond the old boundary, independent repath/patrol timers, home return/reset, pooled activation, cooldown gravity/idle, configured melee-edge overlap, actual strike damage, charged reactions and landing,12° uphill travel, windup exclusion, active windows and one hit per swing for both species; elevated-player and wall controls pass. Sahur placement uses the scaled capsule sole and allows native settling. The actual first-island encounter references both updated prefabs.

Across separate240-interval roaming captures, chicken direction reversals fall158→2. Random deliberate target changes remain; this is not a paired frame-identical animation test. Qualified skin sole mean falls0.70665→0.07666m above the floor. Idle sole gap is0.06729m for chicken and0.03435m for duck. Final walking range is-0.11088…0.14358m: certain poses still intersect the floor slightly. Precision foot planting and full island swarm contact remain follow-up work; no perfect-foot-contact or whole-FPS claim.

The foot diagnostic was independently qualified: BakeMesh(useScale:true) proxies differ at only5/518400 pixels from the actual skinned render, while the false-scale control differs92148 pixels. A1m actor translation gives exactly1m sole displacement. Eight original chicken states sampled at21 normalized phases each (168 comparisons) move down exactly0.65m, within3µm tolerance. PNGs are actual native960×540 end-camera captures. Motion0/80/160, final roam and frozen Idle are included. Before/after use the same camera/stage but different wandering poses.

## Rejected candidates and limits

Early false-scale baking gave misleading feet; unreadable CPU mesh access yielded invalid Infinity. Those measurements are rejected. Two fixture expectations checked home reset one update too early or let the faster duck reach its target and choose another; corrected without additional movement changes. Early melee tests ignored Sahur's scaled sole offset and falsely implied a tall attack-volume bug. The candidate capsule volume was fully reverted; final acceptance uses the original sphere. These failures are not accepted fixes.

No geometry, UV, rig, clip, material, texture, save, scene or core-design replacement. An incidental URP _MainTex serialization was rolled back after confirming it was the sole material delta. Scene restore timed out at30s but completed; native readback confirms the original five scenes clean/stopped, marker clear and both fresh save files unchanged. Tests reused the existing Editor; unrelated Plastic settings/recovery/checkouts and606 missing plants remain preserved. No new player build or full island/GPU benchmark was run.

Evidence: AUTO_QA/Evidence/batch020/{proof,regression,before,after,final-native-state,before-clips,after-clips}.json and native PNGs. Helper: Tools/EnemyGround020.cs. Checkpoint title: Fix chicken patrol jitter and ground offset; enlarge duck.
