# Deployment verification — 2026-10-06/07

**Installed:** portable Blender **5.2.2 LTS**, isolated Python 3.12.14 environment, mcp-blender 0.4.0, the general Blender knowledge skill and six modular animation skills.

**Configured:** Codex `blender_animation` stdio MCP → Blender addon at 127.0.0.1:9876. Existing Codex TOML values and existing MCP servers were compared with the original backup and preserved. The two owned addon compatibility changes are recorded in `C:/Users/pinyu/.codex/tools/blender-workflow/blender-5x-compatibility.patch`.

**Created:** design/review templates, stage/approval rules, shared project context, manifest schema, seven Blender helpers (including the six requested names), Unity staging importer, versioned output/no-overwrite guards and local launch/verification scripts.

| Actual check | Result / evidence |
|---|---|
| Installed Codex skill discovery | All seven root skills enabled; `codex-discovery.json` |
| Official skill format validator | Six custom + generic root + six generic subskills = 13 valid files; `skill-contract-report.json` |
| Description/UI routing contracts | Distinct design/character/combat/boss/review/export descriptions and implicit invocation; reviewed statically, model auto-routing not separately benchmarked |
| Design gate | 1–3 adaptive questions, concrete specification, actual user approval/waiver before production; prior approval retained for targeted revisions |
| Review loop | Frame/bone-specific corrections, real evidence, stage acceptance, unobserved categories explicit, no whole rebuild for local defects |
| Codex MCP server/tool discovery | mcp-blender server responded; 21 configured animation tools exposed out of 218 upstream tools; no discovery error |
| Real Blender contact | Actual stdio MCP handshake and get_version/scene_info/object_list/action_list responses, Blender 5.2.2 LTS |
| Armature/bone/Action operations | Temporary two-bone weighted fixture: created Action, posed bone, inserted keys, read F-curves, edited an existing key to value 2 and back to 0 |
| Animation playback | Native goto-frame, play and stop succeeded |
| Preview | 12 PNGs across front/side/three-quarter/test-camera views, frames 1/24/49; changing pose verified, sampled stills visually checked |
| Validation | Accepts matching loop endpoints; rejects pending design, FPS mismatch, mid-clip root/object travel and an existing export filename |
| Single-Action FBX export | Exactly one selected action, frames 1–49 at 24 FPS = 2 seconds; metadata includes source frames/seconds/normalized gameplay windows |
| Generic Unity import | Live Unity 6000.3.23f1: valid Generic Avatar, one clip, 30 transform curves, 24 FPS, 2 seconds, non-looping; `unity-generic-import.json` |
| Scale/orientation/skinning | Unity fixture root→tip = 0.9999999 m, upward axis dot = 1, keyed pose angle = 20.0535°, eight unique deformed mesh vertices (24 imported vertices at hard seams) |
| Humanoid Unity import | Existing Standing Melee source copied to staging with matching source Avatar: valid Human Avatar, one clip, 30 FPS, 2.266667 s, 130 muscle/transform curves; `unity-humanoid-import.json` |
| Avatar mismatch handling | Existing PbrSahurAvatar cannot be copied onto Mixamo's different bone names; diagnostic test exposed that mismatch and importer now rejects incompatible mappings before reimport |
| Read-only source helpers | Actual inspect_actions.py/inspect_animation.py runs on shark master, seven Actions, 42 Tail_Strike F-curves, 60 FPS |
| Non-destructive cleanup | Temporary Blender objects/Actions/scenes removed; original object/Action inventory, scene FPS/range/frame restored; Unity staging assets/root removed through AssetDatabase |
| Source/config preservation | Final read-only inspection checked unchanged hashes for the shark master, old character master, standing-melee FBX and player controller; `environment-verification.json` |
| Unity state after verification | Edit Mode, active MainMenu scene, scene not dirty; no gameplay controller replacement |

The generic fixture's design waiver is solely for the user's requested isolated tooling verification. It is not approval for new game animation or an artistic quality score.

**Needs manual action:** restart Codex once so normal turns receive the new MCP tool catalog. The owned helper Blender session is running and listening locally; if closed later, use `Start-AnimationBlender.ps1` described in the workflow README. No API key, cloud generation account or additional installation is required.

**Action-specific verification remains:** artistic quality/weapon contact/water placement, foot support intervals, retargeted Humanoid gameplay poses, runtime root ownership, timing receivers and gameplay camera. These checks require the actual approved action and scene; no final game animation was authored or approved by this setup.

Versions: MCP commit `4ae5645aa1f0c8f099c2a90969fdd392c0cc9851`; general skill commit `59dda21f52824ddfb1a3fe07ad843fc0cdc23334`. Portable Blender ZIP passed the official SHA-256 checksum recorded in environment-verification.json. Local tools and licences remain outside Unity Assets; project helpers/context/evidence are under Tools/AnimationPipeline and Documentation/AnimationWorkflow.
