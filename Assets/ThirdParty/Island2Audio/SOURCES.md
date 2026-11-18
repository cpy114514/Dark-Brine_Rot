# Island 2 audio sources

All selected source recordings are CC0 1.0. Individual `SOURCE.json` files record the public source page and downloaded archive checksum. `SelectedFiles.json` records checksums for the exact source recordings retained here. The full download archives and HTML snapshots remain local under `.codex/island2-audio`.

| Pack / author | Original source | Licence |
|---|---|---|
| Impact Sounds — Kenney | https://kenney.nl/assets/impact-sounds | CC0; provider licence copied beside the recordings |
| Interface Sounds — Kenney | https://kenney.nl/assets/interface-sounds | CC0; provider licence copied beside the recordings |
| RPG Audio — Kenney | https://kenney.nl/assets/rpg-audio | CC0; provider licence copied beside the recordings |
| 40 CC0 water / splash / slime SFX — rubberduck | https://opengameart.org/content/40-cc0-water-splash-slime-sfx | CC0 as stated on the individual asset page |
| 5 break, crunch impacts — Independent.nu, submitted by qubodup | https://opengameart.org/content/5-break-crunch-impacts | CC0 as stated on the individual asset page |
| wind whoosh loop — SketchMan3, adapted from Loopable Dungeon Ambience | https://opengameart.org/content/wind-whoosh-loop | CC0 as stated on the individual asset page |

Edits include trimming, resampling, pitch changes, filtering, transient layering, seam crossfades and level matching. Processed masters are in `Assets/Resources/SecondIslandAudio`; `Authoring.json` lists durations, peak and RMS values. `Tools/BuildIsland2Audio.py` reproduces these edits from the selected sources. Long PCM masters are imported by Unity as streaming Vorbis; short mono cues are preloaded PCM.

CC0 summary and legal text: https://creativecommons.org/publicdomain/zero/1.0/ . Attribution is retained here for provenance even though CC0 does not require it. No noncommercial, share-alike, account-only original download or paid sound library is used.
