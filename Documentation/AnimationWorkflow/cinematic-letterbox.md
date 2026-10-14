# Cinematic widescreen framing — October 13, 2026

The user requested the wider, flatter picture used during AAA cinematics. Story cutscenes now use a 2.39:1 visible frame with opaque top/bottom bars, animated over 0.3 seconds. The camera's aspect, projection and scene geometry are unchanged; people are never stretched. Ordinary movement, attacks, sailing patrol and player-controlled combat remain fullscreen.

`Assets/Scripts/Story/CinematicLetterbox.cs` owns a transient scene Canvas with two non-interactive Images. Explicit owners request/release framing. Duplicate requests are idempotent; overlapping requests survive handoff; disabled/destroyed owners are removed; scene unload destroys the overlay. The transitions follow scaled time and freeze while paused. Bars resize through normalized anchors, and screens at least as wide as 2.39:1 need no extra cropping. There are no added render passes, render textures, sounds, input locks or post-processing effects.

Layering uses order 450, above the boss HUD (160) but below the actual story pause/death UI (500), existing arrival subtitle/blackout (900), and wreck blackout (950). Native inspection confirmed the story pause Canvas is order 500. Its layer and the arrival UI's original order are preserved.

Integration boundaries:

- `Story1WreckBattle.Begin`: enable for tail strike / ship breakup.
- The existing handback to `Fighting`: release for player-controlled combat.
- `BeginFinale`: enable for the automatic paired fight and defeat.
- `Story1OceanAwakening.BeginBlackout`: retain during the unconscious exit; the battle releases its request after handoff. Scene unload removes both old owners.
- `FirstIslandArrival`: enable for the fresh arrival and get-up, release in the existing control-restoration path; Continue skips the arrival request. Arrival blackout/subtitles draw above the bars.
- The existing finale camera's fit of actual subjects now reserves the visible letterboxed height. It still performs collision/visibility correction and does not change animation timing, poses, supports or contact logic.

Validation artifacts are in `.codex/cinematic-letterbox-20261013/`. `Tools/VerifyCinematicLetterbox.cs` runs in an isolated native preview scene: 1080p, 1440p, 16:10, ultrawide, partial transition, owner overlap, duplicate requests, disabled-owner cleanup, input pass-through and fullscreen restoration passed. At 1920x1080 each bar is about 138 pixels tall. The actual native UI layout was rendered and inspected in `layout-preview.png`; its temporary scene was closed.

`Tools/ReviewCinematicLetterbox.cs` uses the original Editor only, captures actual Game View screenshots including overlay UI, and observes natural sailing / tail strike / player combat / finale / defeat / arrival / control restoration. It requests the finale early through the existing safe-handoff API solely to shorten this functional review; it does not move the characters or fragments. The helper stops its own Play Mode. This is visual/functional evidence, not a performance benchmark or approval of the unfinished opening animation as a whole. Historical first-pass screenshots used the elapsed clock (including lead-in); the current helper uses the film clock for its fight screenshots and actual foot bones for framing measurements.

The live user save was backed up before the diagnostic and restored after verified stop; the preserved authority is SHA256 `DA0FFBB38D301E72633F8830CCACCD5DA78F6A658E93CFF77822E7D791D20E84`. No scenes, rig, Avatar, historical animation assets or player progress were saved over. No extra Editor/Player, network listener workaround, commit or push was created.

Final native review: `runtime-review.json` has no error; all ten phase screenshots were captured at 2560x1440. Bar fraction is 0.128079981 on each edge during the cinematics and zero in sailing/player combat/island control. The full 20.8-second film was sampled for 416 frames at diagnostic 20 FPS, with zero head-bone or foot-bone points outside the letterboxed vertical frame. This is one layout/run, not a universal visibility guarantee: existing foreground wreckage can still occlude the action and remains part of the unfinished opening-animation work. `05-throw-wide.png` demonstrates that distinction. `02-ship-break-wide.png` provides a clear picture of the widescreen framing. After automatic stop, actual Editor evaluation confirmed stopped/scene clean/no remaining letterbox objects; the user save was restored and hash checked again.
