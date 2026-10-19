# Shorter opening voyage — Oct17, v213 WIP

Human wants opening sailing reduced to one quarter or shorter. Source implementation exists in `Assets/Scripts/Story/Chapter1/Story1SharkCollisionSequence.cs`; native after-change timing/video validation is NOT completed.

## Measured before / intended change

The actual old voyage automatically entered shipbreak at52.500s; patrol alone44.189s. A full-circle minimum, not only serialized20/30s thresholds, extended the wait. Native before take `.codex/short-voyage-20261017/before-10/` records actual automatic trigger, real tail contact and transition to Fighting; no forced BeginImpact. The preview is2FPS sampled from10Hz simulation, not an FPS benchmark.

Added `voyageDurationScale=.25`. Compute the old patrol/circle budget before shortening, then reserve the normal8s approach plus a small physical-arrival margin within the reduced budget. Current planned budget13.047s, patrol3.090s, earliest physical attack gate11.090s. These are PLANNED numbers; actual new attack time is still unknown and must be <= measured before/4 (13.125s) in normal sailing before acceptance.

Keep the shark's original approach quarter by adjusting its INITIAL orbit phase before its first visible frame. Preserve normal ship/shark speeds and original attack clip speeds. Carry the trimmed patrol offset into hunting progress so entering pursuit does not replay the distant wandering phase. The existing physical nose-to-hull distance/facing gate remains; no teleport or forced timer hit. `scale=1` retains the old timings/initial angle. No serialized scene edits, new rig/Avatar/actions, change to20.8s fight or fixed18m cinematic camera.

## Actual blocker / continuation

Unity's ILPP worker hung during compilation. Restarted only verified compiler child18860 owned by current Editor9048; compilation then completed, status failed=false/errors[]. No Editor was restarted or additionally launched. Afterwards entering Play Mode stalled; original Editor9048 reports Not Responding and main-thread CLI calls time out. A stop request was issued but its completion must be checked. The old user constraint in `Actions/CH1_SahurSharkEncounter_v001/network-test-launch-constraint.md` says to preserve the existing Editor and not terminate it. No forced close, second Editor, standalone Player or network changes were made. User recovery/restart of the CURRENT Editor is needed if it remains unresponsive.

After recovery: inspect actual Editor state first, protect any newer human save, then use `Tools/ReviewShortVoyageV213.cs` via `ReviewShortVoyageV213.Start("after-30",30)`. It asynchronously loads Story1, records actual automatic sailing→attack→break→Fighting and never forces impact. Use original Editor only. Compare actual attackAt <= old52.49977/4 with timestep tolerance; if not, adjust patrol timing/initial approach continuity without speeding up physics or clipping animation. Encode with `.codex/short-voyage-20261017/encode.py after-30`, visually review the actual continuous movie/keyframes, and revise material failures. Preserve fixed18m cinematic radius even when obstructed; user explicitly permits occlusion.

Source/backup/before data and numeric planned budget are in `.codex/short-voyage-20261017/`. Fresh save backup is `save-before.json`; current byte-match=True, SHA2565A296481F7414AF9DCE308C6F62B990A89CE67F92A1E9268D46136D87C4BF803. Do not overwrite newer user progress with this snapshot after recovery. No commit or push. Broader animation/support issues remain in prior reviews; this timer change does not sign them off.
