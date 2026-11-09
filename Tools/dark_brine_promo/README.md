# Dark Brine_Rot — promo video pipeline

Three cut-downs live here:

| output | what it is |
| --- | --- |
| `out/ads/ad1_chant.mp4` … `ad5_story_v.mp4` | **five brainstick ads**, 13–19 s each, beat-locked, 16:9 and 9:16 |
| `out/Dark_Brine_Rot_Ad_wide.mp4` / `_Ad_vertical.mp4` | **the 15.5 s looping ad** — real gameplay + AI shots, one hook line repeated three times |
| `out/Dark_Brine_Rot_Ad_v2_wide.mp4` / `_Ad_v2_vertical.mp4` | **the brainstick ad v2** — fresh AI shots, 132 BPM, the hook announced word by word and then twice in a row |
| `out/Dark_Brine_Rot_Trailer.mp4` | **40 s / 1920x1080 trailer** — real gameplay frames + AI generated shots, English cards, procedural score |
| `out/Dark_Brine_Rot_Teaser.mp4` | 23 s fully procedural teaser (every frame drawn with numpy/Pillow, no game capture) |

The trailer pipeline is described below. The teaser pipeline is the older
`make_promo.py` entry point and still works unchanged.

---

## 0. The brainstick ads

```bash
python make_ads.py                    # all five, full resolution
python make_ads.py --only ad1_chant   # one of them
python make_ads.py --preview          # half resolution, fast look
python make_ads.py --stills ad1_chant:2.1,4.15,12.15   # PNG frames instead of video
```

| key | format | length | bpm | idea |
| --- | --- | --- | --- | --- |
| `ad1_chant` | 1920x1080 | 16.0 s | 120 | the hook: DARK / BRINE / ROT |
| `ad2_chant_v` | 1080x1920 | 16.0 s | 120 | the same hook, re-composed for a phone |
| `ad3_weapons` | 1920x1080 | 13.7 s | 140 | one weapon per beat, metal hits |
| `ad4_bosses` | 1920x1080 | 19.2 s | 100 | three boss dossiers, numbered |
| `ad5_story_v` | 1080x1920 | 18.8 s | 128 | island → survivor → arsenal → bosses → title |

### Why it is addressed in beats, not seconds

A brainstick ad is a metronome, not a mood piece. The whole recipe lives in
`promo/ad.py` and every element declares **which beat** it lands on, so the
chant, the drums and the cuts cannot drift apart — change the BPM and both
sides move together.

```python
Shot("figure", 4, {"keys": ["sahur_club", "sahur_blades"], "size": 0.46,
                   "labels": ["CLUB", "DUAL BLADES"], "label_y": 0.17,
                   "chant_y": 0.80, "chant_size": 84.0,
                   "shore_y": 0.66}),
```

* `Shot(kind, beats, payload)` — `kind` picks a renderer, `beats` is its length
* `keys` — the figure on each beat, one cut per beat
* `shore_y` — where the dark foreground bank starts; figures are placed so
  their feet land on it (measured from the sprite's own alpha box, so a
  crocodile and a humanoid both stand correctly)
* `chant_y` / `chant_size` — where the sung syllable appears in that shot
* `hide_chant` — keep the title card clean

The chant slots themselves are `ad.chant = [(bar, beat, word, vowel), ...]`
and the score is generated from the same list, so the word on screen and the
syllable in the speaker are the same event.

### Shot kinds

`black` · `words` · `figure` · `boss` · `title` · `tagline` · `shards`

### The sound

`promo/adscore.py` synthesizes everything from scratch, on the ad's own grid:

* **the chant** — a formant synthesizer: a band-limited saw glottal source
  pushed through three resonant peaks (`ah` / `eh` / `oh`), deliberately flat
  and mechanical, the way a stadium chant is broadcast
* **the kit** — kick on 1 and 3, snare on 2 and 4, eighth-note hats, sub bass
* **the hits** — `ad.hits` places metal hits, booms and stabs on beats
* the last 0.12 s is silence: a gap is more catchy than a fade

### Performance notes

* The backdrop hands back a **pooled buffer** it reuses every other call —
  copy it before compositing onto it in place, or the previous frame's
  artwork bleeds into the next one.
* Bloom is done at quarter resolution; a full-res Gaussian blur costs more
  than the rest of the frame put together.
* Figure sprites are cached by key and size, and their punch-scaled variants
  by a coarse 0.05 grid.

---

## 1. The trailer cut

```bash
python build_trailer.py             # full 1920x1080 MP4
python build_trailer.py --preview   # 960x540, roughly 2x faster
```

Requirements: Python 3.10+, `numpy`, `Pillow`, `imageio-ffmpeg`.

The cut is defined in one place — `SHOTS` and `OVERLAYS` at the top of
`build_trailer.py`:

```python
{"kind": "video", "src": ASSETS/"ai"/"shark_fin.mp4", "start": 0.2, "dur": 4.0, "dark": 0.0},
{"kind": "still", "src": ASSETS/"play"/"live_a.png", "dur": 1.0, "zoom": 1.10},
{"kind": "card",  "src": CARDS/"card_blades.png", "dur": 1.4},
```

* `kind="video"` — a generated or captured clip, trimmed from `start` for `dur` seconds
* `kind="still"` — a real gameplay frame, pushed in with a slow zoom
* `kind="card"` — a graphic card, played full frame
* `dark` — extra brightness pull-down for shots that are brighter than the rest
* `OVERLAYS` — captions placed on the finished timeline, in seconds

### Pipeline stages

1. **segments** — every shot is normalised to 1920x1080 / 24 fps / yuv420p / AAC 48 kHz
2. **concat** — joined with the concat demuxer; the script verifies the joined
   length matches the sum of the shot lengths and fails loudly otherwise
3. **overlay + grade** — captions are shifted onto the main timeline with
   `setpts` so their fade in/out lands at the right second, then the whole
   picture gets one colour grade (cool teal, crushed blacks, vignette)
4. **mix** — the procedural score from `promo/score.py` at full level, the
   generated clips' own audio at 20 %, then EBU loudness normalisation

### Timing

| time | beat |
| --- | --- |
| 0.0–4.0 | black sea, the raft, "the crossing was supposed to be silent" |
| 4.0–10.8 | the wreck, the moon, something already waiting |
| 10.8–15.4 | landfall — a real gameplay frame from the island |
| 15.4–23.0 | the shark, the rising sea |
| 23.0–26.0 | the island |
| 26.0–30.2 | weapon cards: club, twin blades, M4A1 |
| 30.2–32.8 | "every island hides something" |
| 32.8–38.2 | title reveal and end card with the itch.io link |

Cards are generated by `promo/cards.py`; the score by `promo/score.py`.
Both are plain Pillow/numpy so they can be re-skinned without touching the
edit.

---

## 1b. The looping ad (real gameplay + AI)

```bash
python build_ad.py              # 1920x1080
python build_ad.py --vertical   # 1080x1920
python strip.py out/Dark_Brine_Rot_Ad_wide.mp4 out/ad_wide_strip.png 4   # contact sheet
```

15.48 s at 124 BPM — eight bars, so the picture cuts land exactly on the kick
and the last bar falls back into the first one for a seamless loop. Both
versions start and end on black, so an ad slot can repeat it without a seam.

| bar | picture | card |
| --- | --- | --- |
| 1 | the moon over the raft | "IT KNOWS YOUR NAME" (small) + `FIRST ISLAND · 03:12 AM` |
| 2 | the shark fin | "IT KNOWS YOUR NAME" (medium) |
| 3 | the breaching whale | "IT KNOWS YOUR NAME" (large) |
| 4 | the real island (AI-extended gameplay frame) | `RUN.` |
| 5 | the shore | `FIGHT.` |
| 6 | the night sea | `SURVIVE.` |
| 7–8 | logo, end card with the itch.io link | |

The hook line is the same sentence three times at three sizes, always in the
same place, always on the same drum pattern — that is the earworm. The score
(`promo/loop_score.py`) repeats one bell motif on the same beat of every bar
for the same reason.

### Traps in this pipeline

* **the portrait crop has to follow the subject** — the shark fin sits at ~70 %
  of its 16:9 frame and the whale head at ~71 %, so a centred 9:16 crop cuts
  both off. `crop_x` in `SHOTS` moves the window; `scan_crop.py` renders the
  candidate offsets side by side so the value is chosen by eye.
* **cards must be drawn at the delivery size** — a 1920x1080 card stretched to
  1080x1920 squashes the type into a narrow column. `promo/ad_cards.py` takes
  `w`/`h` and writes into `out/ad_cards/<w>x<h>/`, so the 16:9 and 9:16 renders
  can even run at the same time.
* **portrait hook lines are two lines** — "IT KNOWS / YOUR NAME" instead of one
  long line, which keeps the type huge in a 1080 px wide frame.
* the score, the work directory and the card folder are all per-version, so the
  two renders never fight over the same file.

---

## 1c. The brainstick ad v2 (all new AI shots)

```bash
python ai_shots_v2.py             # submit + poll + download the new shot list
python build_ad2.py               # 1920x1080
python build_ad2.py --vertical    # 1080x1920
```

14.55 s at 132 BPM. Every shot is a fresh generation, and the whole edit is
built around one rule: **the picture has to come back**. The same red-eye
motif opens bars 1, 2 and 7, and the hook line is announced one word per bar
for four bars, then lands twice in a row on bars 6 and 7.

| bar | picture | text |
| --- | --- | --- |
| 1 | the red eyes in the fog | `IT` |
| 2 | the same eyes, another take | `KNOWS` |
| 3 | an eye opening | `YOUR` |
| 4 | the eyes again | `NAME.` |
| 5 | four cuts on the beat: lamp, claw, pulse, wave | `RUN.` |
| 6 | a silhouette on the cliff, staring | `IT KNOWS YOUR NAME.` |
| 7 | the eyes return | `IT KNOWS YOUR NAME.` |
| 8 | logo, end card | — |

* `ai_shots_v2.py` submits three clips from the **same prompt** on purpose —
  near-identical frames read as a repetition, which is what makes a motif
  stick. `state.json` in `out/ai2/` keeps the task ids, so a failed run can
  be resumed without paying twice.
* the generator allows only **five tasks in flight**; a sixth submission is
  rejected with `video generation limit reached`, so the script is safe to
  re-run — it resubmits only what has no task id yet.
* `promo/loop_score_v2.py` puts the bell motif on the same two beats of every
  bar and answers it with a blip half a beat later.
* `promo/ad_cards2.py` draws the word cards at slightly different heights and
  adds a progress dot row, so four static cards still feel like they move.

---

## 1d. Real gameplay footage (the release client)

The trailer that ships with the project was cut from two still frames and a
pile of AI shots. The island, however, only exists at runtime, so the only way
to get real footage is to drive the game itself.

`Tools/dark_brine_promo/capture_window.py` and `promo_capture.py` do that:

* they drive the standalone build with real input events (WASD, mouse),
* they grab the game window with GDI (`PrintWindow`, `PW_RENDERFULLCONTENT`),
* they check every take: a black picture or a frozen one is reported as a
  failure instead of quietly becoming part of the trailer.

```bash
# launch the client that is built for playtests
.build/development/DarkBrineRot_Playtest.exe -screen-width 1920 -screen-height 1080

# one frame of whatever is on screen
python capture_window.py shot out/frame.png --method print

# a scripted session: click, walk, fight, open the map
python promo_capture.py --pid 12345 plans/flow_island.json out/raw
```

`plans/*.json` are the sessions. Each action is one line:

```json
{"do": "click", "x": 958, "y": 669}
{"do": "shots", "name": "island_walk", "keys": ["w"], "seconds": 5, "look": [3, 0]}
{"do": "record", "name": "03_opening", "seconds": 45, "fps": 12}
```

Coordinates are in the game view, with (0, 0) at the top left of the picture.

Three traps, all of them paid for in lost takes:

* **the game pauses itself when it loses the foreground**, and the Unity
  editor sits maximized on top of it. A click that lands on the editor both
  steals the foreground and pauses the game. `focus_window()` raises the game
  before every shot and the focus is held for the whole take.
* **the pointer must stay inside the window.** A relative mouse move that
  walks off the edge makes the next click hit whatever is underneath.
* **the capture rate is the frame rate of the grab, not of the game.** The
  frames come in at about 8–12 fps, so the trailer builder interpolates them
  back up to 24 fps.

`make_sheet.py` prints a contact sheet of a take, `slice_frames.py` cuts a
time range out of one.

```bash
python make_sheet.py out/raw/island_walk out/raw/sheet.png
python slice_frames.py out/raw/03_opening out/raw/boss_fight 0 7.5
```

### The cut that uses it

```bash
python build_promo_v2.py            # 40 s, 1920x1080
python build_promo_v2.py --preview  # 960x540
```

The real footage and the AI shots are graded into one palette: the island is
shot in daylight while the sea is shot at night, so the daylight takes get an
extra `DUSK` grade (`build_promo_v2.py`) instead of cutting from black to
bright in the middle of the story. Every caption also gets a dim plate under
it, because light type on transparent disappears over grass.

---

The island is generated at runtime, so nothing meaningful exists in edit
mode — frames have to be captured while the game is running.

`Assets/Editor/PromoCapture.cs` exposes a tiny file-based command channel to
the already running editor:

```bash
python Tools/dark_brine_promo/promo_cmd.py ping
python Tools/dark_brine_promo/promo_cmd.py shots out/cmd_scenes.json
```

* drop a JSON command into `<project>/Temp/promo_cmd.json`
* the editor answers in `<project>/Temp/promo_out.json`
* `shots` renders off-screen into a `RenderTexture`, so the Game View is
  never touched and scenes are not dirtied
* it refuses to switch scenes while the current one has unsaved changes

The editor only reimports scripts when it gets focus (`m_RefreshImportMode: 0`
in `ProjectSettings/EditorSettings.asset`), so the editor window has to be in
front for a new version of the script to compile.

---

## 3. AI generated shots

Shots are generated through the `matrix` connector and land in `assets/ai/`.

```bash
python ai_shots.py      # submit + poll the shot list
python fetch_ai.py     # re-download by task id
```

* `MiniMax-H3` (2K, ~15-30 min) for the hero shots
* `MiniMax-Hailuo-2.3` (768P, seconds) for the cheaper fill-ins
* the credit budget is limited: a 8 s H3 clip costs 888 credits, a 6 s Hailuo
  clip 416
* first-frame image-to-video works too — uploading a real gameplay frame and
  asking for motion keeps the art style consistent with the capture

---

## 4. Layout

```
Tools/dark_brine_promo/
├─ make_ads.py          the five brainstick ads (edit promo/ad.py for the recipes)
├─ build_trailer.py    the 40 s cut (edit this for the timeline)
├─ build_ad.py         the 15.5 s looping ad, 16:9 and 9:16
├─ build_ad2.py        the brainstick ad v2, 16:9 and 9:16
├─ ai_shots_v2.py      submit + poll + download the v2 shot list
├─ strip.py            contact sheet of a finished video, for a quick look
├─ scan_crop.py        preview the portrait crop at several horizontal offsets
├─ promo_cmd.py        drive the Unity capture channel
├─ ai_shots.py         submit + poll AI shot generation
├─ fetch_ai.py         download finished AI shots
├─ download_ai.py      download whatever the shot list knows about
├─ promo/
│  ├─ ad.py             beat grid, shot renderers, the five recipes
│  ├─ adscore.py        beat-locked score: chant, kit, hits
│  ├─ ad_cards.py       the looping ad's cards (hook, verbs, logo, CTA)
│  ├─ ad_cards2.py      the v2 word cards and the progress dots
│  ├─ loop_score.py     the 124 BPM / 8 bar loop score with the bell hook
│  ├─ loop_score_v2.py  the 132 BPM score for the v2 ad
│  ├─ cards.py         captions, weapon cards, title, end card
│  ├─ score.py         the 40 s procedural score
│  ├─ entities.py      the vector figures (sahur, bosses, weapons)
│  ├─ background.py    the ocean backdrop
│  ├─ config.py        palette
│  ├─ util.py          fonts, noise, maths
│  └─ audio.py         synthesis primitives reused by score.py / adscore.py
└─ out/                generated media
   ├─ ads/              the five brainstick ads + their scores
   ├─ Dark_Brine_Rot_Ad_v2_wide.mp4 / _Ad_v2_vertical.mp4
   ├─ Dark_Brine_Rot_Ad_wide.mp4 / _Ad_vertical.mp4
   ├─ Dark_Brine_Rot_Trailer.mp4
   ├─ score_40s.wav
   ├─ ai2/              task ids for the v2 shots
   ├─ ad_cards/         the ad cards, one folder per aspect ratio
   ├─ cards/           rendered graphic cards
   └─ build/           per-shot segments (safe to delete)
```
