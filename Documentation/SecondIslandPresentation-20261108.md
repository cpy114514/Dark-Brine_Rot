# Second Island presentation revision — 2026-11-08

## Delivered content

- I opens a flat black equipment panel with white/gray text, straight separators and grayscale selection. The generated book sprite and runtime sketch drawing are no longer used. Equipment, skill categories, pause and close controls retain their existing logic.
- Six Poly Haven CC0 assets supply PBR wooden crates, benches, lanterns, apples, mossy rocks and stumps. Resource redirects replace the existing camp/village props without changing their outer interaction/collision logic. Sixteen rock/stump groups add detail around four island locations, with static collisions and distance culling. The cotton-candy terrain, story characters and routes retain their existing design; this is a prop/detail revision, not a replacement of every island asset.
- Nineteen rig-specific clips: eight Huang Mei, six human Zhu Bajie, five boar. Downloaded humanoid actions are retargeted with native Unity avatars and baked to the existing skeletons. The boar retains its own supplied attack clips, with a baked diagonal gait for chase/charge and breathing at idle. The AI owns planar travel; clips own skeletal animation.
- Human Zhu Bajie receives idle/carry walk, a two-part rake action, dodge, rise and death. Huang Mei receives idle/carry walk, two sweeps, raised staff slam, palm release, invocation, three ritual plants and death. Compositions preserve the existing windup, impact and recovery clocks. Short crossfades bridge state changes.
- Huang Mei's staff grip/open palm constraints are baked into clips. Sleeve helper chains follow the arm attachment while maintaining a downward drape and small cyclic sway. Original model files, weapons and old procedural fallback are preserved.
- Huang Mei VFX now combine lit bronze mandala geometry, a rotating dimensional seal projectile, a tapered white-gold staff trail, etched danger areas, pooled ground resonance and low-density textured ash. Existing damage zones and audio cues remain synchronized to the original attack logic. Effects clear on cancel/reset/death.

## Online sources and licensing

All new downloaded resources use CC0. Source licenses are kept beside the assets.

| Resource | Official source | Project location |
|---|---|---|
| Quaternius Universal Animation Library 2, free Standard FBX | https://quaternius.com/packs/universalanimationlibrary2.html and https://quaternius.itch.io/universal-animation-library-2 | `Assets/ThirdParty/SecondIslandMotion` |
| KayKit character animations, already imported CC0 library reused for compatible body actions | https://kaylousberg.itch.io/kaykit-character-animations | `Assets/ThirdParty/CapriPolishKayKit` |
| Poly Haven photogrammetry models, 1k glTF/PBR maps | https://polyhaven.com/license | `Assets/ThirdParty/SecondIslandRealism/Sources.json` lists individual asset pages |
| Kenney Particle Pack selected textures | https://kenney.nl/assets/particle-pack | `Assets/Resources/SecondIslandHuangMei/Textures` |

The new model downloads were checked against the provider's MD5 values. Existing character models and their licensing were not changed by this task.

## Authoring and review

Native Editor tooling is under `Tools/BuildSecondIslandMotions.cs`, `BuildSecondIslandRealism.cs`, `ReviewSecondIslandUpgrade.cs` and `ReviewSecondIslandTimelines.cs`. Invoke through the Unity CLI `run_script` command. Builders use Unity APIs for native assets and scene saves. Do not hand-edit serialized clip/prefab/scene YAML.

Animation manifests and visual review sheets are in ignored `.codex/island2-upgrade`. The first preview passes were rejected for incompatible twist/cloth deformation and corrected before final baking. Preview skinning explicitly recalculates per render so consecutive samples show the actual deformed mesh, rather than a cached first pose.

Runtime regression lives in `Assets/Editor/VerifySecondIslandPresentation.cs`, which uses isolated save paths, the real I/Esc input path and the existing Zhu Bajie combat regression. A sloped-ground collision probe could slide the player sideways, leaving the old melee probe out of range. The test now places that separate melee probe just outside the actual solid front surface; no player damage rules were relaxed.

Formal save snapshots, scene restoration checks, runtime screenshots and final results are kept in ignored `.codex/island2-upgrade/playtest`. Those files include private save snapshots and must remain local. No new standalone EXE or GitHub upload is included in this revision. The earlier Downloads build does not contain these changes.

## Final validation

- Unity 6000.3.23f1 recompile completed without compiler errors.
- Final isolated Play regression passed: real I/Esc menu input, six equipment categories, sixteen scenery groups, original solid boar collision, real player melee damage, charge, automatically aimed tail sweep, half-health transformation, both rake strikes, reactive dodge, defeat and Continue persistence.
- Huang Mei sweep, slam, palm and mandala all completed their windup/strike/recovery flow. A real half-health threshold triggered invocation and mandala; defeat stopped movement and cleared particles. The new impact component's Unity 6 constructor allocation error was corrected by creating its native material property block during explicit initialization.
- Final runtime interval contained no exception, compiler-error or shader-error log lines. Earlier rejected authoring/test errors remain in the existing Editor log; the user console was not cleared.
- Reviewed skeletal timelines and runtime screenshots, corrected sleeve deformation and separately grounded the rock meshes on slopes. Sampled timelines have finite transforms and zero skeletal-root drift. This is targeted regression/visual review, not a human animation approval or a complete campaign/performance benchmark.
- Formal save bytes and original scene setup were unchanged. Editor left stopped, with no temporary test scene/camera/input device active. Pre-existing VersionControlSettings and promotional frame modifications were preserved.
