# 开发与检查工具

这里保留场景配置、模型导入、动画制作和玩法验证工具。它们位于 `Assets/` 外，按需通过 Unity Pipeline 执行，不参与游戏编译。

- `Diagnostics/VersionControl/`：Unity Version Control 的历史诊断脚本。
- `AnimationSources/`：动画源文件与授权说明。
- `ProfileWholeGame.cs`、`VerifyWholeGameOptimization.cs`：性能对照与功能检查。
- `AuditProjectCleanup.cs`：记录场景依赖、运行时资源和恢复目录引用。
- `CleanProjectAssets.cs`：可预览的整理工具；归档并验证备份后，使用 Unity API 移动资源，保留 GUID。

本目录根部的 PNG、GIF、WebP 为工具生成的检查图，不是游戏图片；Git 和 Unity Version Control 会忽略新生成的文件。历史检查图和旧修复备份已经归档，详见 `Documentation/ProjectCleanup-20261006.md`。部分历史恢复工具需要旧备份作为输入，使用前从归档还原对应文件。

可编辑的正式 Blender 源文件在 `ArtSource/`。`BlenderWork/` 仍保留 Sahur 的模型源文件、制作脚本与数据，自动备份和旧预览已经移出项目。
