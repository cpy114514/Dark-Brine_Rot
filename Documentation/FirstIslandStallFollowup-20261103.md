# First-island complete-freeze follow-up — task067 / 2026-11-03

The user narrowed the freeze to first-island exploration or Boss combat. Prior task066 tests included flock combat and a second-island soak, not all first-island Boss phases. Keep the total-hang report open until a matching reproduction is found; do not claim it solved from ordinary FPS samples.

Fresh evidence/save/preferences are protected under `.codex/island-stall067`. Latest inspected human Player.log names the original Downloads/TungTung-20261103-1538 executable; it has no task066 heartbeat diagnostics. The latest fixed diagnostic logs contained only our completed QA sessions. Do not treat this as evidence that task066's exe froze.

## Targeted investigation

- Measured real Bombardino Tick calls in the actual island scene. 0.0167s input costs0.024–0.084ms;0.333s catch-up costs1.05–1.31ms in the sampled idle/patrol state. This did not identify a CPU blocker and the flight/attack simulation was not changed speculatively.
- The old user log contains Unity's `IDXGISwapChain::GetFrameStatistics is broken` fallback message. That warning alone is not a hang diagnosis. Consulted [Unity's Player command-line reference](https://docs.unity.com/en-us/engine/6000.3/manual/unity-editor/command-line-arguments/player) and official issue/release material; no matching verified fix was established, so no speculative rendering backend/engine migration is claimed.
- Added an explicit `-stability-first-island` QA route to the existing opt-in verification component. It runs with the normal on-screen gameplay camera/swap chain, not a render-texture-only camera. It performs30s exploration, two40s Cappuccino windows, three45s Nailong windows and two55s Bombardino windows. Each phase must observe real AI Attack frames. Virtual keyboard/mouse movement, jump/dodge and normal attack events exercise the production controller.
- The final QA route accepts real hostile hits and replenishes HP after the frame, retaining ordinary max HP, damage recovery, hurt feedback/audio and local hit-stop. It excludes the test player from saves. Boss health ratios/prerequisite IDs are controlled only in this explicit QA process to cover each existing phase without losing the player or triggering the chapter ending. This is not a full campaign victory/abduction test.
- An early protected-player sample was interrupted after its Cappuccino phase1 to extend coverage to actual received-hit feedback. Its progress/log remain under `protected/`; it is not the final complete-phase evidence.

## Production diagnostic change

The local heartbeat snapshot now records each first-island Boss's state, attack and engagement flag, plus frame delta, VSync and frame limit. The existing prerequisite registration supplies Boss references; no repeated global Boss searches run in the diagnostic update. Destroyed references are cleared on scene unload. The background watchdog still reads only managed cached strings, never Unity objects/API. Its bounded local log and inactive/pause behavior remain. These additions improve diagnosis if the symptom returns; they do not establish a freeze cure.

Original source copies are in `rollback/`. Preserve task066's async flock preparation, map/quest progression and generated-island-mesh release, plus all earlier Sahur animation/combat and unrelated project changes. No scene YAML edits, saved enemy reset, source VCS check-in or GitHub upload is authorized by this follow-up.

Final native results and preservation receipt will be appended after completion.

## Final result

The final ordinary-window first-island run passes13 checks with no captured runtime errors. It covers30s exploration, all7 controlled Boss phases, normal player attacks and actual received-hit audio/impact/hit-stop. Real AI attack frames were observed in every phase: Cappuccino685/713, Nailong1291/1329/1430, Bombardino899/926. These are active-state frame samples, not hit counts or a full campaign playthrough. The maximum wall-frame interval was110.24ms; no total hang or >5s heartbeat report was reproduced. Native allocated memory after exploration stayed approximately1355–1364MiB through the combat phases, with the largest sampled live mesh count1533. This is one roughly6-minute run on the current machine, not a multi-hour or multi-hardware stability certification.

`boss-snapshot-result.json` confirms cached snapshots include all3 Boss states, attacks, engagement flags, frame limit, VSync and delta. The independent background heartbeat/duplicate/inactive tests pass in `heartbeat-result.json`. No combat balance, original attack timeline, global timescale policy or aircraft simulation was changed from task066; the production change in this follow-up is more specific diagnostics. Retained earlier fixes remain.

Built Player: `C:/Users/pinyu/Downloads/TungTung-FirstIsland-20261103/TungTung.exe`. Downloads also contains `TungTung 第一岛版.lnk`, pointing to that executable with **empty arguments**, so opening it starts the normal game rather than automated QA. It includes all previous task066 fixes. `build2/build-report.json` reports Succeeded, non-Development Windows64/Mono,8 production scenes and2,789,565,715 bytes. The cached build reported3 warnings and0 errors; this does not claim that the source Environment's previously missing plant assets were repaired.

After the standalone exits, `prefs-restored.json` verifies110 original standalone preferences. `restored.json` verifies fresh human saves byte-identically restored and the original5-scene setup retained; the Editor is stopped, scenes clean, physical keyboard/mouse enabled. Source snapshots and build receipts remain under `.codex/island-stall067`; previous builds and unrelated work remain. No commit/push or cleared human progress.

The reported intermittent total freeze still has no matching reproduction or established root cause. The new shortcut reduces ambiguity between the user's old1538 executable and current code, and the diagnostic context can identify the last active Boss skill if it recurs. Do not present this investigation as a confirmed complete cure.
