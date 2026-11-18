# Island 2 sound redesign

## Scope and audit

Island 2 previously reused the global cue library, had three fixed cotton movement clips, generated short sine-based melody notes and boss seal sounds, and had no dedicated chocolate-river sound field. This pass adds an island-local palette and environmental mix while preserving damage, animation clocks, puzzle note identities, enemy AI, save rules and other island scenes.

## Delivered sound

- 59 edited audio masters: three environmental beds; four cotton steps, three lifts and three landings; four wet chocolate steps and three wet landings; swing/dodge/hurt variations; pickups, warning and UI clicks; material-aware contact, biscuit crunch, soft-body, metal and low-impact layers; boss windup, charge, rake, mandala bell, seal impact and defeat accents; four revised melody notes; sparse bubble and confection creak accents.
- Cloud wind is a quiet stereo bed. Chocolate current and thicker bubbles are separate mono spatial beds positioned at the nearest river reach, using 126 baked samples. Walking away fades them out. Forty sampled confection anchors support sparse local creaks instead of placing a source on every decoration.
- Entering a boss encounter reduces ambient gain to 42%, leaving space for warnings and impacts. Environment and accent voices pause with the world, resume without recreation, and stop/suspend outside the island. All sources follow the existing master listener volume.
- Four cotton step recordings alternate. Steps and landings inside the widened river select wet/slime layers; dry terrain keeps soft cotton/sugar compression. The existing movement cadence and landing callbacks own timing.
- Boss presentation readers observe existing states. They do not alter AI, hitboxes, damage, movement or windup durations. Huang Mei's existing effect callbacks use recorded bronze-like and weighted impact layers; shared imported clips are not destroyed during scene teardown.
- The melody garden keeps C4 / E4 / G4 / C5 and its original sequence logic. New notes use offline bell-like harmonics with a recorded glass attack, preserving pitch recognition. Runtime sine generation remains only as a missing-resource fallback.
- Confirmed hits use a separate audio-only `CombatFeelProfile`; gameplay parameters still come from the original profile. The scene's player swing references change; the shared Sahur prefab and original global WAV files do not.

## Sources and processing

Selected recordings are CC0: [Kenney RPG Audio](https://kenney.nl/assets/rpg-audio), [Impact Sounds](https://kenney.nl/assets/impact-sounds), [Interface Sounds](https://kenney.nl/assets/interface-sounds), rubberduck's [water/splash/slime pack](https://opengameart.org/content/40-cc0-water-splash-slime-sfx), Independent.nu's [break/crunch impacts](https://opengameart.org/content/5-break-crunch-impacts), and SketchMan3's [wind loop](https://opengameart.org/content/wind-whoosh-loop).

Only 70 selected originals are retained under `Assets/ThirdParty/Island2Audio`. Provider licences, source pages and archive/file hashes are documented in `SOURCES.md`, per-pack `SOURCE.json`, and `SelectedFiles.json`. All 70 selected-file hashes were checked. No Freesound recording, paid library or noncommercial sample is included.

Offline edits include trimming, resampling, filtering, pitch changes, transient layering, overlap crossfades and level matching. All 59 masters contain finite samples; maximum decoded peak is approximately 0.78003, with headroom before the runtime mix. The largest loop boundary sample difference is approximately 0.0066. These are technical checks, not a loudspeaker/headphone listening approval.

Processed PCM authoring masters total about 10.4 MB under `Assets/Resources/SecondIslandAudio`. Three long beds import as streaming Vorbis; short cues import as preloaded mono PCM at 32 kHz. The wind bed retains stereo. Source recordings outside Resources are unreferenced by runtime assets. There is no runtime waveform generation in the normal path and no runtime file/network IO.

The environmental component uses three loop sources plus six bounded accent voices. River proximity is sampled at 10 Hz; actor lookup is throttled. It reuses the game's existing global and per-actor effect pools for ordinary cues and confirmed hits.

## Authoring and validation

`Tools/FetchIsland2Audio.py` downloads public CC0 source packs to local staging. `Tools/BuildIsland2Audio.py` reproduces the processed masters. `Tools/InstallIsland2Audio.cs` configures native Unity import settings, creates `Island 2 - Soundscape`, bakes the river field, assigns the audio profile and scene-local swings, and saves through the Editor. The pre-pass scene is backed up locally at `.codex/island2-audio/before.unity`.

`VerifyIsland2Audio.Prepare()` runs an isolated Play session. Checks cover native installation, streaming playback, near/far river gain, wet/dry selection, real W-input footstep triggering, pause/resume, master volume routing, boss warning readers and ambient ducking, all four melody clips, original cue fallback outside Island 2, additive scene suspension, disable/re-enable without duplicate sources, and resource survival across scene reload. Formal save bytes are checked after Play.

The local `.codex/island2-audio/audition.wav` is an 18-second offline sound-design reel, not a captured gameplay mix. Runtime reports and save snapshots are under `.codex/island2-audio/playtest`; those snapshots must stay local. No standalone build, commit or upload is included.

Final Unity 6000.3.23f1 compilation and isolated regression passed without runtime errors. The report contains 20 check/measurement records. River gain measured approximately 0.606 at the river and 0 far away. Warmed listener output was non-silent (maximum sampled RMS about 0.0234, sampled peak about 0.0758); this is an ambient output snapshot, not a worst-case loudness test. Pause/resume, master routing, state-driven warnings, source re-enable, scene reload and non-island fallback passed. Formal save files remained byte-identical; the editor returned stopped to the clean saved scene.

Before/after scene comparison found only the three serialized objects of the new soundscape root and the existing player's swing-clip references as substantive scene changes. Unity regenerated an equivalent local ocean mesh ID; its payload remained identical. Terrain, decorations, triggers, enemies, animations and puzzle layout were preserved.
