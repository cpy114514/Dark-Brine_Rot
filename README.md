# Dark Brine_Rot

Unity `6000.3.23f1` / URP `17.3.0`。主场景为 `Assets/Scenes/MainScene.unity`，包含程序化海洋、天空、太阳，以及第三人称 Sahur 角色。

## 快速开始

用上述 Unity 版本打开项目，进入 `Assets/Scenes/MainScene.unity`，点击 Play。
当前控制为 WASD 移动、鼠标转动视角、滚轮缩放、左 Shift + W 加速、空格跳跃、Esc 释放鼠标；点击重新锁定鼠标。
场景中序列化的参数优先于脚本默认值。角色右手下 `Sahur Stick` 的 Transform 是人工调整结果，维护时保留其位置、旋转和缩放。

## Layout

| Path | Purpose |
| --- | --- |
| `Assets/` | Unity assets (scripts, shaders, scenes, models, textures, URP settings). |
| `Assets/Ocean/` | Procedural ocean world, free-flight camera, and the matching shaders (`DarkBrine/Procedural *`). |
| `Assets/Player/` | `ThirdPersonPlayerController.cs` 与 `SahurAnimator.controller`。 |
| `Assets/Models/` | 当前角色 `PbrSahur.fbx`、手持 `stick.fbx` 及旧版角色资源。 |
| `Assets/Models/Materials/` | 角色和木棒材质。 |
| `Assets/Models/PbrSahurTextures/` | 从角色 FBX 提取的 PBR 贴图。 |
| `Assets/Models/SahurAnimations/` | Mixamo 动作和原始模型副本。 |
| `Assets/Scenes/`, `Assets/Settings/` | 主场景、URP 与质量配置。 |
| `Assets/3d model/` | Nature props (boulders, trees, grass) — each folder ships with its `.blend` source + `textures/`. |
| `Assets/TextMesh Pro/`, `Assets/TutorialInfo/` | 第三方字体、示例和模板资源，保留原目录。 |
| `Tools/` | Unity 连接脚本与只读检查脚本 `audit-project.ps1`。 |
| `Documentation/` | 项目维护说明，`Previews/` 收纳历史验证截图。 |
| `Packages/` | UPM package manifest and resolved packages. |
| `ProjectSettings/` | Unity project settings. |
| `UserSettings/` | Per-user editor state (gitignored). |
| `BlenderWork/` | Blender pipeline working directory — `.blend` files, automation scripts (`sahur_*.py`, `tungtung_*.py`), and inspection outputs (`*.png`, `*.json`). Scripts use absolute paths under `D:/indie_game_dev/...`, so they are location-independent. |
| `Hackatime_Offline_Package/` | Offline copy of the `com.daniel-geo.unityhackatime` UPM package, kept as a fallback when the GitHub URL is unreachable. Install via `Window > Package Manager > + > Add package from disk > com.daniel-geo.unityhackatime/package.json`. |
| `Library/`, `Temp/`, `Logs/`, `Obj/`, `Build/`, `Builds/`, `MemoryCaptures/` | Unity-generated, gitignored. |
| `.vsconfig`, `.wakatime-project` | Editor + Wakatime config. |

## Version control

Both `.git/` (git) and `.plastic/` (Plastic SCM) are present. Existing version-control configuration is retained.

- `.gitignore` — git exclusions (already comprehensive: `Library/`, `Temp/`, `Logs/`, `*.csproj`, `*.sln`, `*.slnx`, etc.).
- `ignore.conf` — Plastic SCM ignore file (mirrors `.gitignore` for the most part).

## Procedural ocean

`Assets/Ocean/OceanWorld.cs` builds the water mesh, sky dome, and procedural cloud layer at runtime. Quality presets (`Low` / `Medium` / `High`) cap mesh resolution and shader detail. `Assets/Ocean/OceanFreeCamera.cs` is a retained free-flight controller; the current character uses `ThirdPersonPlayerController`.

海浪由 `ProceduralOcean.shader` 的 Gerstner 位移计算。维护优化保留原波形、配色、视距与质量配置；网格按视角分成前方形状和俯视全覆盖形状，两种形状按需缓存。水面 Shader 不使用 UV，因此生成网格省略 UV 通道。

## Sahur / Tung Tung pipeline

1. Author / rig in Blender under `BlenderWork/` (`character_fixed.blend`, `tungtungtungsahur*.blend`).
2. Run the inspection scripts (`BlenderWork/sahur_v2_*.py`, `BlenderWork/separate_tungtung_club.py`, …) to cross-section, fix fingers, separate the club, etc. Outputs land next to the scripts as PNG/JSON QA reports.
3. 当前主场景使用 `Assets/Models/PbrSahur.fbx`，已导入、提取贴图并配置人形 Avatar。模型和各 Mixamo 文件分别使用自己的 Avatar。

## 动画与资源维护

`SahurAnimator.controller` 当前关联 Idle、Walk、Walk Backward、Run、Jump。
Run 状态当前使用 `Run To Rolling.fbx`；此次整理保留现有映射和操作。
`3feca72b7209e2dd8a0aed65292573fc.fbx` 为模型资源，无动画片段。

`PbrSahur.fbx`、`sahur.fbx`、`SahurAnimations/20260909135110_82dffd37.fbx` 的 FBX 字节相同，但 `.meta`、Avatar 和材质映射可能不同，不可直接当作可互换文件删除。
移动 Unity 资源时使用 Project 窗口或 `AssetDatabase.MoveAsset`，保留 GUID 和 `.meta`。历史截图可在 `Documentation/Previews/` 查看。

运行只读检查：`pwsh -File Tools/audit-project.ps1`。优化内容与验证结果见 `Documentation/Maintenance.md`。
