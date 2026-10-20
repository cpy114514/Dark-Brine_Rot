# Nailong readability revision — v229

The user confirmed that the effects were difficult to see **inside Unity**, and requested larger saliva. The v228 checker-floor/closeup checks established that pixels rendered, but did not establish sufficient readability at the actual game camera distance. This pass uses the native island, lighting, equipped boss and Sahur instead.

## Changes

- The glob's unscaled radius increases from0.12m to0.24m: **2× diameter**. Its visible main body and continuous sphere sweep use the same radius. On the native island boss this is a measured0.5366564m radius, or about1.07m diameter; the boss's own scale remains unchanged. Damage remains16 in the verification setup.
- Water refraction increases2→4pixels, transmission tint becomes slightly cooler, the silhouette reflection is stronger and the specular highlight gains a soft shoulder. Transmission alpha increases0.44→0.62 when scene colour is available and0.24→0.45 in the fallback. The body still transmits the background rather than returning to a green solid sphere.
- Pressure rim opacity increases0.045→0.18, with a wider compression crest, a faint0.035 body tint and an adjacent dark edge. Refraction increases3→6pixels. The neutral light/dark front remains readable against both shaded grass and brighter ground while the broad body stays transparent.
- Wave presentation width is1.8× the gameplay ring width; the mesh crest rises0.24→0.50m above its sampled terrain, with0.10m low edges. The fixed15-damage rule, collision width,0.6m jump clearance, pulse count and timing are unchanged. Width/height and saliva radius are exposed with English Inspector descriptions.
- Updated persistent materials with Unity's asset APIs via `Tools/InstallNailongReadabilityV229.cs`. The existing single-shot parabola, water bead pool and v228 animation clips remain in use. No additional particle count, meshes or per-frame allocation were introduced.

## Actual island review

`Tools/ReviewNailongIslandV229.cs` loads First Island/Main and its real subscenes additively in the existing Editor Play Mode. The authoring Story1 scene stays loaded without saving. It uses the actual Nailong spawn and world environment, holding other AI still while invoking the real attack cues/updates for two spits and a three-ring slam. This is a controlled native encounter recording, not a complete autonomous boss-fight playthrough.

The camera retains the native camera's rendering settings and60.4065° FOV. Its position follows the game's lock-on framing formula and the current **11m camera-distance setting**, with Sahur about9m from the boss. The original equipment, terrain, trees, foliage, shadows and ocean remain visible.

The initial `island-before/` setup interrupted arrival without fully releasing its cinematic pose/protection. It is a superseded diagnostic. The harness was corrected to release arrival, restore standing locomotion and clear temporary arrival protection. The accepted baseline is **`island-baseline/`**, and the revised pass is **`island-after/`**.

- Accepted baseline:451 sampled simulation frames. Revised:452frames over15 simulated seconds, with normal enabled-camera1280×720 captures sampled at10Hz.
- Both runs retain a maximum of **one glob**. Target health reaches84 after each spit and55 after three rings, confirming unchanged16-damage spit and45 total ring damage.
- The enlarged glob measures **42.6–66.6pixels in diameter** along its visible flight in the revised recording. Baseline radius was half as large. Native shots and the wider, faint pressure fronts were inspected from the battle camera, including `frame-0013.png` and `frame-0069.png`.
- **32 contracts pass** in `Tools/TestNailongReadabilityV229.cs`, including the new radius assertion plus the prior single-release,10/30/60Hz ballistic hit, thin-wall sweep, duplicate-hit, interruption, jumpable-ring, cue-mapping and pool-reuse checks.1000 warm projectile/visual updates still allocate0 managed bytes.
- Both shaders report **zero compiler messages**, and the final Console has **zero errors**. Production-source whitespace checks pass.

Evidence: `.codex/nailong-readability-20261020/`. `summary.json` records native framing, health and projected diameter. `Nailong_Readability_v229.mp4` is encoded directly from the revised native frames, at10fps sampling; this sampling rate is not a measurement of game FPS. No synthesized/interpolated frames are used.

This turn does not replace other attack animations or repeat low-end hardware, release-build, extended multi-boss or all-weather testing. The no-scene-texture fallback was strengthened in the shader, but its prior isolated v228 render checks were not repeated in the island recording. The source now provides stronger defaults; do not rerun the v228 installer to reset the materials to its old faint settings.

## Preservation

The human save was newer than v228. Its protected/restored SHA256 is:

`968BD555DD0288F031ADFE4B4E714E8F6626E381A788C95DFBE8702F9AEF337A`

Do not restore the older A7A/3EA saves over it. The existing Editor is stopped/unpaused and the original dirty Story1 scene remains unsaved. No scene/prefab YAML was edited, and no second Editor, Player, build, commit, push or automation was created. Other user modifications, v227 ship motion, Sahur combat and original animation assets remain preserved.
