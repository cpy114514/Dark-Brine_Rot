# Oct 14 — full encounter admission restored; choreography still incomplete

Human report: the Sahur/shark fight animation had almost disappeared. Reproduced in the original Editor, using real released fragments and the production safe handoff. No actor placement fixture, moved wood, second Editor or standalone Player.

## Cause and scoped fix

`Story1WreckFinale.PrepareEntry` rejected admission and called `BeginUnconsciousExit("No reachable supported combat surface")`. This sets the film clock to the final 2.2-second exhaustion ending: none of the six exchanges had run. Active clips were present, not deleted.

- Native entry planning was sampling a combat hand pose while the swimming weapon retained its stowed local offset. `SampleEntryCombatGrip` now restores the original right-hand grip during hypothetical pose evaluation. The visible entry/swimming pose is restored between coroutine slices; combat colliders remain disabled. No rig/Avatar or clip edits.
- Candidate swim range was capped at the lesser of 30 metres and four seconds of ordinary swim speed. The stronger real scatter can leave suitable deck fragments farther away. Search now uses 60 metres, matching the existing alternate-water-support policy, and still swims/climbs to actual moving geometry.
- Admission required both later tail contacts, at 14/18 seconds, to fit against the intact wreck geometry at entry. Admission now verifies real continuous foot support and both opening weapon-contact endpoints. The later tail approaches retain their live anatomical collision, route solving and 5 cm hit authorization. A missed tail does not trigger a fabricated stagger/hit. The emergency exhaustion path remains for genuinely unavailable support.

Only production source changed in this request: `Assets/Scripts/Story/Chapter1/Story1WreckFinale.cs`. Preserve the immediately previous `Story1WreckBattle`/`ThirdPersonPlayerController` smooth camera changes and the 140-distance Story1 sailing camera. Original ordinary attack/walk/run/roll, mobile charge, no surfing, 1000 HP/30%/20–35s, mesh collisions, gravity/buoyancy, no audio, colour images and wave-held unconscious shore ending retained.

## Actual evidence

Artifacts: `.codex/finale-admission-20261014/`. `Tools/ReviewFinaleAdmission20261014.cs` loads Story1 in the existing Play Mode, initiates the real fracture, leaves the actor and debris to normal gameplay physics, and waits for production takeover. Fixed-step recordings are functional evidence, not measured game FPS.

| Run | Result |
|---|---|
|before, 30 Hz|No supported admission, zero contact attempts; only 2.2s exhaustion ending.|
|after-grip intermediate, 30 Hz|Grip/range corrections alone still rejected the future tail endpoint; no full film. Retained as failed intermediate evidence.|
|after-entry, 30 Hz|625 native consecutive game-camera frames, clock 0.0102–20.8101; ExitReason null, 0.643s preparation; real support error 1.302mm/slide 1.387mm. Full lost-stick/defeat segment. `full-film.mp4` encoded and contact sheet inspected.|
|alternate-10fps, different scatter seed|209 consecutive native frames, clock 0.0357–20.8357; ExitReason null, 0.700s preparation; support error 1.230mm/slide 1.557mm; full defeat.|

**Not full artistic/contact acceptance:** first completed run authorized only 1 hero/1 shark hit; opening contacts missed 5.890/3.877/3.573m, reversal 1.981m. Alternate run authorized 3 hero/1 shark hit; first probe missed 4.026m and reversal 6.029m. The throw/final defeat genuinely contacted in both. The missing-whole-film regression is repaired in these two natural water handoffs. First approach and reversal route/contact remain unfinished; do not describe all six contacts or the whole project as complete. No fake hits or relaxed contact thresholds were used to make the film run.

## Protection and current state

**NEW human save authority:** at this request's start the save was already newer than DA0. SHA256 **853AB760C2638AA1AE0300DF8C3676299FFEC1ABE3CC8F440960612B878E91BD**, `.codex/finale-admission-20261014/save-before.json`. Each diagnostic save was preserved separately; this fresh snapshot was restored AFTER each confirmed Stop, including the final alternate run. Never restore earlier DA0/AB/51C snapshots over it; preserve any newer human progress on subsequent turns.

Compilation completed without errors. Own tests/recorders have ended. Existing sole Editor stopped; no scene saves, checkin/push, new Editor/Player or network changes. Full broader animation objective remains incomplete as above and v208; continue from this source, not a pre-admission or pre-camera backup.
