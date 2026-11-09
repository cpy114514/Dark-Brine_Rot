# 开发与检查工具

> 2026-11-08：按用户要求，约 77.7 GiB 的本地 QA 构建、副本、截图/录像和部分一次性 Boss 检查脚本已清理。制作/资源构建工具保留；旧测试输出链接可能失效。见 [本次清理记录](../Documentation/LocalTestCleanup-20261108.md)。

这里保留场景配置、模型导入、动画制作和玩法验证工具。它们位于 `Assets/` 外，按需通过 Unity Pipeline 执行，不参与游戏编译。

- `Diagnostics/VersionControl/`：Unity Version Control 的历史诊断脚本。
- `AnimationSources/`：动画源文件与授权说明。
- `ProfileWholeGame.cs`、`VerifyWholeGameOptimization.cs`：性能对照与功能检查。
- `AuditProjectCleanup.cs`：记录场景依赖、运行时资源和恢复目录引用。
- `CleanProjectAssets.cs`：可预览的整理工具；归档并验证备份后，使用 Unity API 移动资源，保留 GUID。

本目录根部的 PNG、GIF、WebP 为工具生成的检查图，不是游戏图片；Git 和 Unity Version Control 会忽略新生成的文件。历史检查图和旧修复备份已经归档，详见 `Documentation/ProjectCleanup-20261006.md`。部分历史恢复工具需要旧备份作为输入，使用前从归档还原对应文件。

可编辑的正式 Blender 源文件在 `ArtSource/`。`BlenderWork/` 仍保留 Sahur 的模型源文件、制作脚本与数据，自动备份和旧预览已经移出项目。

Unity 固定为 `ProjectSettings/ProjectVersion.txt` 中的版本；当前为 **6000.3.23f1**。在项目根目录执行命令，指定 `--project-path`，使用现有 Editor。工具不是启动游戏所需的运行时资产。

## 日常检查与清理

| 目的 | 入口 | 行为 |
| --- | --- | --- |
| 检查打开场景的缺失 Prefab、脚本和材质 | `InspectProjectIntegrity.cs` → `InspectProjectIntegrity.Run` | 只读，保留 dirty 状态，不开关或保存场景，报告写入 `.codex/project-integrity/` |
| 构建场景、Resources 与备份引用审计 | `AuditProjectCleanup.cs` → `AuditProjectCleanup.Run` | 只读；每轮传入唯一 label 保留旧证据；冷缓存耗时可能超过 CLI 默认超时，应先核对输出文件和 Editor 状态，不连续重复启动 |
| 清理工具 Python 缓存 | `ClearToolPythonCaches.ps1 -Mode Preview`，确认范围后 `-Mode Apply` | 仅清理 Tools/AgentScripts 下未跟踪、有 `.py` 源码的 `__pycache__` 文件；保存清单和哈希；不处理 Unity 缓存、构建、截图、视频、存档或历史记录 |
| 既有局部代码回归 | `RunProjectMaintenanceChecks.ps1` | 专项检查引导线/地图等既有维护修改，不代表全流程通关 |

原生只读检查示例：

```powershell
unity command --project-path 'D:/indiegameDev/unity/Dark Brine_Rot' run_script --file Tools/InspectProjectIntegrity.cs --entry InspectProjectIntegrity.Run --result-only
pwsh -NoProfile -File Tools/ClearToolPythonCaches.ps1 -Mode Preview
```

## 内容维护入口

| 内容 | 入口和说明 |
| --- | --- |
| 第一岛五场景工作区 | Unity 菜单 `Tools > Scenes > Open Full Workspace`；职责见根目录 `COLLABORATION.md` |
| 当前地形、草地、生态和碰撞 | [第一岛环境维护说明](../Documentation/Island1EnvironmentMaintenance-20261107.md)；使用既有 BakeIslandRealism / 标记 Bake，不叠加运行旧草地工具 |
| 海岸沉船 | `IslandShipwreckPass.cs` 的 Validate；编辑现有 Prefab，不能把初建入口当成日常重置工具。见 [沉船说明](../Documentation/Island1Shipwreck-20261107.md) |
| 普通植被性能网格 | `Assets/Editor/FoliagePerformanceBuilder.cs` 与 `BuildGrassRenderMeshesV226.cs`；保留紧凑布局和实际相机回调提交修复 |
| 动画导入与来源 | `AnimationPipeline/`、`AnimationSources/`；资源版本与 `.animation-pipeline-owned` 标记必须保留 |
| 隔离存档的第一岛试玩构建 | `FirstIslandPlaytestBuild.cs`；使用既有隔离规则，不覆盖玩家存档和正式产品配置 |
| 宣传片与截图 | `dark_brine_promo/README.md`；out/raw/镜头输出包含人工记录，不视为可随意删除的编译缓存 |

## 历史工具的处理

`CleanupGeneratedBuildsV243.ps1`、`CleanupSupersededPlayers.ps1` 是针对以往固定清单的清理脚本；它们操作 `.codex` 中当时批准的旧构建，**不是本轮清理入口**。大量带版本号的 Build/Review/Verify 脚本记录了既有专项流程，不仅凭文件年龄或搜索不到调用就删除、迁移或自动运行。先检查写入路径、输入资源和报告约束。

项目目录、保留原因和本轮实际结果见 [项目清理记录](../Documentation/ProjectCleanup-20261107.md)。
