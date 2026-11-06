# Runtime stalls and flock progression — task066 / 2026-11-03

User reports intermittent complete picture/audio freezes, plus missing Chicken Bros in the Windows exe. Preserve all previous animation, combat, guidance, settings and rendering changes.

Evidence is under `.codex/freeze066/`; original affected source snapshots/hashes are in `rollback/` and `source-before.json`. The user's latest actual `Player.log` and `Player-prev.log` were copied before tests. The fresh human save is separately protected and is not replaced with a test/new game.

## Findings

- The prior exe contains Enemies.unity and the chicken, long-legged duck and bully-goose dependencies. This was not a missing Build Settings scene or an excluded prefab.
- First-island flock activation waits for Cappuccino's defeat and proximity to a dynamically chosen inland location. Objectives, guide and map omitted this encounter and sent the player toward the next Boss. There was no clear reason to approach the flock's otherwise empty spawn area.
- The fresh human save currently continues on the second island with 70 marshmallows already defeated and the Ronin defeated. Continue intentionally preserves those victories. Do not clear the user's progress or respawn all previously defeated enemies to hide this fact.
- Flock activation called synchronous `NavMeshSurface.BuildNavMesh` and instantiated the wave's models/Animators/physics in one frame. A controlled native measurement of the old navigation build took 39.68 ms on this machine. This is an avoidable combat-entry hitch; it does **not** establish the cause of a complete picture/audio hang.
- The old exe's 5 s coast/forest/Boss-zone samples completed without errors or >50 ms measured frames (maxima 33.30/35.17/35.07 ms). They do not reproduce the reported intermittent freeze or establish long-session stability.
- Windows Event 1001 at 16:28:30 America/Vancouver includes `RADAR_PRE_LEAK_64` for TungTung.exe. This is an OS resource-growth heuristic, not proof of a specific code leak or the cause of a freeze. No matching display-driver recovery event was found in the inspected three-hour window.

## Changes

- Prepare the flock location before the preceding Boss is defeated. Prepare its NavMesh with the installed AI Navigation package's asynchronous update API, then add completed data. No synchronous combat-entry BuildNavMesh remains. Cancellation/disable/destruction cancels and releases owned navigation data.
- Initialize at most one flock member per frame with a minimum 0.06 s interval. The original 4/6/8 wave sizes, species mixture, maximum living count, attack slots and combat behavior remain.
- Add the flock to the objective list, next-target world guide and island map. Fresh progression is Cappuccino → Chicken Bro flock → Nailong → Bombardino. Confirmed later Boss victories in old saves bypass the newly explicit flock prerequisite, preserving their existing access. Flock clear remains persisted through the existing stable identity and save system.
- Add bounded, local-only standalone stall diagnostics. A managed timer detects a >5 s absence of main-thread heartbeats while focused; the last main-thread snapshot contains scene, loading flags, player/control state and native/managed memory. It labels save commits and flock initialization. It suppresses duplicate reports and ignores inactive/paused applications; it does not alter timescale or unlock cinematic controls. The timer never calls Unity APIs. Log: `%USERPROFILE%/AppData/LocalLow/DefaultCompany/Dark Brine_Rot/runtime-stalls.log`, capped to approximately64 KiB. No remote telemetry or automatic dump upload.
- Add an explicit `-stability-check` standalone QA route. It never installs in an ordinary game session. It excludes test players from saves, exercises real production scenes/components and damage, injects ordinary attack/movement input, samples runtime resources and traverses both islands. It is not a full campaign playthrough or hardware compatibility certification.

## Verification so far

- `flock-result.json`: all13 native actual-island checks pass: assigned species/prefabs, prerequisite, guide/map target, async readiness, player-triggered start, all three waves, persisted clear, next-Boss unlock and corpse cleanup. First harness take failed because the test accessed a flock after its completion closed it; corrected test checks completion after each await. It is retained under `first-flock/` and is not passing evidence.
- `progression-result.json`: all5 checks pass, including preservation of confirmed legacy Nailong/Bombardino access.
- `heartbeat-result.json`: background reporting, duplicate suppression and inactive-session suppression pass without freezing the main thread.
- First standalone QA take is rejected: its unfinished shore-arrival coroutine moved the test player away from the flock target. The corrected opt-in QA cancels/releases that opening before placing its excluded player. This was test setup, not evidence that the shipping encounter failed.
- Final build and standalone results, preservation proof and observed limitations are appended after completion.

## Scope and rollback

These changes address the omitted encounter and remove a measured synchronous entry hitch. A claim that the user's intermittent total freeze is fully solved requires a matching reproduction or runtime evidence; none existed in the initial logs. Keep that distinction in user-facing reporting. Old source files and the previous exe remain available. Restore only these task066 edits when rolling back; do not overwrite the task063/064 animation changes or other pending project edits. Build callbacks restore their original preload list, material bytes, settings bytes and build flags.

## Confirmed native mesh lifetime defect

A native before/after test (`mesh-lifetime-result.json`) instantiates the retained original ProceduralIsland implementation and the revised implementation in an owned temporary scene. After destroying the original owner, its generated island Mesh still exists; after destroying the revised owner, its Mesh is released. The test explicitly disposes the old leaked mesh and closes only its temporary scene. `ProceduralIsland.OnDestroy` now releases its owned generated Mesh; disable still retains it for reuse. This is a proven lifetime fix, not proof that every reported freeze comes from this mesh. Repeated standalone scene/resource samples are used to check the wider result.

The first corrected standalone build (before the final pause guard/mesh-release changes) passes17 checks, including all three real Player flock species/waves, real virtual-device attack input, 120 s of second-island combat, and repeated loads of both islands. Soak native allocated memory is669.2–670.8 MiB with439 meshes/167 materials throughout. A later repeat reaches682.9 MiB and469 meshes/173 materials; these count differences prompted the lifetime audit. Maximum measured frame is318.64 ms, so this is not a claim of hitch-free60 FPS. No total freeze was reproduced. This preliminary build is not the final delivered build.

## Final verification and delivered build

- Final `flock-result.json`:14 native checks pass, including no spawning while the map pauses time. The additional early-start Editor take was rejected because its fixture had not cancelled the still-running arrival; it is preserved in `rejected-flock-arrival/`.
- Final `standalone-stability.json`:17 checks pass with no captured runtime errors. Actual Player builds contain BullyGoose, ChickenBro and LongLeggedDuck, complete all three waves through production damage, accept virtual keyboard/mouse attacks, run120 s of second-island combat, and load each island again.
- Final second-island soak:662.5–663.2 MiB native allocated memory,454 meshes and175 materials throughout. Repeat second island:674.5 MiB,467 meshes and176 materials. This confirms a stable short combat window and the specific mesh lifetime fix, not zero cache growth, a multi-hour memory-leak certification, or universal hang freedom. Maximum measured frame184.47 ms; intermittent hitches remain possible. No complete picture/audio freeze was reproduced.
- The final ordinary launch (without QA flags) displays the main menu and existing save status normally. It was closed through a normal Windows close request and logs orderly input/physics shutdown. The earlier UI Quit click was not sufficient to close that smoke process; no forced process termination was used.
- Final build: `C:/Users/pinyu/Downloads/TungTung-Fixed-20261103/TungTung.exe`, Windows64, non-Development, Mono,8 production scenes,2,789,555,432 reported bytes. Build result Succeeded. The report still includes the existing Environment missing-prefab messages for3 plant assets; those are not repaired by this task. Preserve the accompanying data/DLL folders.
- `build3/cleanup.json`: original build flags, preloads, settings bytes and duck-material bytes restored. `restored.json`: human save bytes and five-scene layout restored; Editor stopped/unpaused, physical keyboard/mouse enabled, original InputSettings alive and captureDeltaTime0. `prefs-restored.json`:110/110 original game registry values verified after the final Player exits. Unity float preferences include7 eight-byte values tagged REG_DWORD; restoring via ordinary .NET DWord setters rejected them, so their original raw bytes/types were preserved through the native registry API. No empty or truncated baseline was applied.
- `source-after/` and `source-after.json` freeze the delivered production source/meta revisions. Rejected/preliminary Player folders were moved out of Downloads into their evidence directories, leaving the original user build plus the final repair folder. No commit/push, source scene rewrite, clearing of defeated enemies, or human artistic approval is implied.

The reported total hang remains unproven as solved. Proven corrections are the missing encounter guidance/order, synchronous encounter-entry work, pause behavior and owned Mesh lifetime. Local heartbeat evidence is available if the original symptom returns.
