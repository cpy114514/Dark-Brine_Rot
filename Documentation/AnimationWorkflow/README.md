# Blender → Unity animation workflow

Installed locally for this project's Unity 6000.3.23f1 Editor. Start with the [project facts](project-animation-context.md); use the actual current source assets when facts change.

## Working contract

User → **action-designer** → character/combat/boss animator → Blender → **animation-review** → targeted revision → **unity-animation-pipeline**.

A vague important move starts an interview of 1–3 questions per turn. The designer shows a concrete specification and waits for the user's approval or explicit waiver before production. Already approved intent is preserved during local fixes. A template does not constitute approval.

Stages: DESIGN, BLOCKING, FIRST PASS, SECOND PASS, POLISH, FINAL, APPROVED. Review grades the requested stage using actual stills, motion previews and gameplay evidence. It names problem frame ranges/bones, revises those sections and rechecks affected transitions. Automated numeric validation cannot prove weight, silhouette, clipping or artistic acceptance. Final approval must cite actual approval evidence.

## Installed skill tree

Current user discovery root is `C:/Users/pinyu/.agents/skills/` ([official Codex skill documentation](https://learn.chatgpt.com/docs/build-skills)).

```text
.agents/skills/
├── blender/ → .codex/skills/blender/ (generic upstream knowledge)
│   ├── SKILL.md
│   ├── actions-fcurves/
│   ├── addons-panels/
│   ├── cameras-lights/
│   ├── linked-libraries/
│   ├── material-nodes/
│   └── python-api/
├── action-designer/          SKILL.md, agents/openai.yaml, references/
├── character-animation/     SKILL.md, agents/openai.yaml
├── combat-animation/        SKILL.md, agents/openai.yaml, references/
├── boss-animation/          SKILL.md, agents/openai.yaml
├── animation-review/        SKILL.md, agents/openai.yaml, references/
└── unity-animation-pipeline/SKILL.md, agents/openai.yaml, references/
```

Descriptions route vague move design, character locomotion/traversal, weapon/combat timing, large creature attacks, quality review and export/import separately. Explicit invocation such as `$action-designer` works when routing is ambiguous. Skill discovery was checked with the installed Codex app-server, not inferred from folder existence. `deploy_skills.py` records provisioning and refuses to overwrite existing skills.

## Blender connection

Local tools: `C:/Users/pinyu/.codex/tools/blender-workflow/`.
Portable Blender 5.2.2 LTS; isolated Python 3.12 environment; pinned [RFingAdam/mcp-blender](https://github.com/RFingAdam/mcp-blender) 0.4.0. The addon listens only on **127.0.0.1:9876**. Codex's `blender_animation` stdio server connects to it. Existing Unity/node MCP configuration is retained, with the original TOML backed up under `backups/`. Tool inventory and connection configuration follow [the official Codex MCP contract](https://learn.chatgpt.com/docs/extend/mcp?surface=cli).

After a Codex restart, the new MCP is available in normal turns. The helper Blender is currently running. If closed, start it with:

```powershell
& 'C:/Users/pinyu/.codex/tools/blender-workflow/Start-AnimationBlender.ps1'
```

This starts a separate hidden factory session and never replaces an existing listener/session. When the user explicitly wants a visible Blender window, pass `-ShowWindow`. For an existing Blender session, enable the installed addon and start its bridge on the same loopback port after closing the owned helper; do not kill a user's session or open a different master over unsaved work. Inspect identity/file/scene before mutations. Save versioned working copies outside original masters.

Upstream's Blender 5.x Action channelbag reader and existing-keyframe editing were corrected locally and verified. `blender-5x-compatibility.patch` records those changes. Replacing/upgrading the addon requires rerunning compatibility and import checks; do not silently discard the patches.

Generic [LevyBytes/AI-SKILL-blender](https://github.com/LevyBytes/AI-SKILL-blender) knowledge remains general. Only incompatible root frontmatter was adapted (`covers` moved under metadata); technical references and licence files were preserved. No cloud model, generation API key, Ollama or Blender AI service is needed for this bpy/MCP workflow.

## Reusable helpers

`Tools/AnimationPipeline/blender/` contains:

| Helper | Purpose |
|---|---|
| inspect_rig.py | Hierarchy, bone names, constraints, meshes, transforms |
| inspect_actions.py | Actions, slots, frame/key ranges and bindings |
| inspect_animation.py | F-curves/keyframe diagnostics (`--keys` for full records) |
| render_animation_preview.py | Front/side/three-quarter PNGs and optional cloned gameplay camera |
| validate_animation.py | Finite/resolvable curves, FPS, sampled root travel, loop endpoints and supplied contact stability |
| export_unity_fbx.py | Validated one-Action versioned FBX + timing/import manifest |
| inspect_project_sources.py | Read-only combined audit of an existing master |

Every CLI helper accepts `--help`; run under Blender's Python, not ordinary CPython. Names are explicit or autodetected only when unambiguous. Scripts fail instead of guessing between multiple rigs/slots, missing meshes, missing root data or existing output revisions.

Example inspection (use an inspected file path):

```powershell
& 'C:/Users/pinyu/.codex/tools/blender-workflow/portable/blender-5.2.2-windows-x64/blender.exe' --background 'D:/indiegameDev/unity/Dark Brine_Rot/ArtSource/SharkAnimation/tralalero_animated.blend' --python 'D:/indiegameDev/unity/Dark Brine_Rot/Tools/AnimationPipeline/blender/inspect_actions.py'
```

Through MCP `blender_execute_script`, import the helpers and call their functions against the inspected rig/Action. Reload a helper module after changing its source in an already running Blender session. Preview uses a temporary subject scene and restores source frame/Action/selection state; default front assumes -Y and must be checked. It shows skinned meshes, not every weapon/prop/environment. Add scene/gameplay captures to review contacts, weapon paths and water interactions. A supplied Blender camera is optional and is not automatically the actual Unity camera.

`--frames` picks key stills; `--every 1` renders a motion sequence. Review timing/arcs from a sequence/playback, not three images alone. Keep each revision's design, manifest, previews and review together. The numeric contact check uses explicit bone support windows and optional gameplay velocity; no windows means **not observed**, not passed feet. Root travel is measured across the whole clip including object motion; default in-place axes are Blender X/Y, while Z bob is allowed. Declare axes/root ownership per action.

## Export/import contract

Copy [action-manifest.example.json](action-manifest.example.json) into an action-specific version directory, resolve its undefined fields and record real design/review approval evidence. It deliberately begins pending/DESIGN and **cannot be exported**. [The schema](action-manifest.schema.json) covers both design and exported metadata; the exporter applies stricter production gates and numeric checks.

Export first outside Assets, for example `ArtSource/AnimationExports/<action>/<revision>/`. Do not overwrite a master or prior FBX. The helper reports exact Action/source/range/FPS/duration, looping/root policy, rig type, major animated bones and Unity clip name. It uses -Z/Y axes, source file units, no added leaf bones, step=1, simplify=0, one selected Action, and no all-Actions/NLA bake. It publishes a completed FBX and `.animation.json`; gameplay windows include source frames, seconds and normalized coordinates. Bound rig controls/constraints may need an export-copy bake. A numeric pass does not waive visual review.

Run `Tools/AnimationPipeline/UnityAnimationImport.cs` with Unity MCP `run_script`, entry `UnityAnimationImport.Stage`, arguments `[externalFBX, exportedManifest, newAssetsAnimationStagingRevision, optionalSourceAvatarAsset]`. Use a fresh `Assets/AnimationStaging/<revision>` folder. It validates take/clip count, source range/FPS/duration, looping, scale settings, root identity and Avatar validity. Humanoid can explicitly copy a compatible **source skeleton** Avatar; Generic can select the root motion node. Copying Sahur's PbrSahurAvatar onto a different Mixamo skeleton fails because names/hierarchy differ. Create/map the source Avatar, then retarget Humanoid clips to Sahur at runtime. It does not replace gameplay controllers or wire unverified event receivers. Root ownership, foot contacts, retargeted pose quality and gameplay camera must still be reviewed for the particular move. [Unity's importer API](https://docs.unity.com/en-us/engine/6000.0/script-reference/unityeditor/modelimporter) and [clip settings](https://docs.unity.com/en-us/engine/6000.0/script-reference/unityeditor/modelimporterclipanimation) describe the native import controls used here.

For a disposable staged revision, `UnityAnimationImport.Cleanup(folder)` only deletes a folder carrying this helper's matching ownership marker. Keep failed-import diagnostics; the helper cleans only its newly created staging revision on failure. Initial imports disable animation compression and optimization and retain readable geometry for inspection. After gameplay validation, choose measured compression/optimization and disable mesh Read/Write if unnecessary; do not ship diagnostic defaults blindly.

## Verification and maintenance

See [verification/report.md](verification/report.md) for actual tests and limits. Local pinned repositories, environment metadata, dependency lock, config backup, patch and rerunnable stdio/Codex tests remain in the local tool directory. No production Action or controller was replaced during setup.

New Codex turns may load a refreshed skill list, but this already running turn has a fixed tool catalog. **Restart Codex once** to activate normal Blender MCP calls. No design approval is needed for installation; new animation production follows the design gate requested above.
