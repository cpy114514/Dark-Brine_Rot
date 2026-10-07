# Natural VFX update — 2026-10-06

Downloaded and integrated one CC0 smoke density texture from [Kenney Smoke Particles](https://kenney.nl/assets/smoke-particles). Source URL, download hash and original license are included alongside the resource in `Assets/Resources/NaturalVfx`. No sample scenes, scripts or extra particle textures were imported.

Changes:

- Blast dust and missile exhaust use the textured mask with random rotation, lighting, fog and depth fading. Desktop import uses a 256px BC7 texture with mipmaps, no CPU readability. A Resources material preserves the textured shader variant for builds.
- Ship strikes emit smaller, faster-falling droplets and a brief, low-opacity mist. Ocean parameters are captured once for the whole burst rather than for every surface sample.
- Small cosmetic wood chips use a tapered 12-triangle mesh and the original hull material/UV sample. The 202 real wreck fragments and all traversal physics are unchanged.
- Near impacts keep their full burst; particles thin out beyond 35 metres and stop emitting beyond 140 metres. Gameplay damage and encounter events do not depend on the cosmetic burst count.
- Particle ceilings are lower for swimming ripples, splash droplets, optional underwater bubbles, wreck effects and missile exhaust. A repeated blast initialization reuses its existing emitter/material.
- Particle lighting is calculated at vertices, and untextured droplet masks no longer calculate a sine and square root per pixel.

Fixed-seed inspection after 0.32 seconds (these are effect workloads, not an FPS benchmark):

| Effect | Before | After |
| --- | ---: | ---: |
| Ship contact droplets | 100 | 64 |
| Ship contact mist | 0 | 12 |
| Cosmetic wood chips | 24 × 65 triangles | 18 × 12 triangles |
| Blast smoke | 32 | 24 |
| Total particles in the inspection | 156 | 118 |

Validation through the running Unity Editor:

- C# compilation and VFX shader compilation pass; no new console errors.
- `Tools/VerifyNaturalVfx.cs`: fixed-seed before/after render, repeated-burst limits, blast reinitialization, near/far emission and swimming surface contact. Optional bubbles are enabled only in the test fixture; the game's current disabled setting is preserved.
- `Tools/VerifyStory1WreckFinale.cs`: 202 fragments, contact VFX, random fight timer, four hero counters and three shark counters, defeat, lost stick, no respawn and unconscious/unarmed island arrival pass.
- `Tools/CaptureNaturalVfxGameplay.cs`: real ship-impact preview, with save bytes restored afterwards.
- User save SHA256 remains `8D8664A0E3EEA197CD5FA6D48D998783932DB75077DE62CEC62AC628DC725B57`.

Local visual inspection files are under `.codex/vfx/visuals`. Sound remains disabled. This change has not been packaged into a standalone player build; measured particle reductions do not imply an equivalent overall frame-rate increase.
