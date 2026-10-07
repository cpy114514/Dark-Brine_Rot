# 项目清理记录 · 2026-10-06

本次整理项目结构并移走历史产物，保留现有玩法、画面、彩色图片、动画、模型、声音源文件、插件和此前未提交的修改。

## 整理内容

- 29 个零散逻辑脚本归入 `Assets/Scripts/` 下的剧情、船只、系统、场景加载、性能和特效目录。
- 根目录的独立 FBX 移入 `Assets/Game/Shared/Models/Unclassified/`。
- 三个 Plastic SCM 诊断脚本移至 `Tools/Diagnostics/VersionControl/`，不再随 Unity 项目自动编译。
- 已确认没有正式资源引用的 `Assets/_Recovery/`、`Assets/ProjectRepairBackups/` 和空 `Assets/Temp/` 已归档并移出 Unity 的资源目录。
- 项目外归档 1,726 个本地历史文件，合计约 2.04 GB：旧修复快照、旧测试构建、Tools 检查图、根目录海面截图、Sahur 自动 Blender 备份和旧预览、临时诊断脚本与历史检查帧。
- 更新 Git 与 Unity Version Control 的忽略规则，排除新生成的预览和恢复文件；正式游戏图片不受这些规则影响。
- 添加脚本目录说明和开发工具说明。Blender 可编辑源文件、制作脚本、动画授权文件及当前性能报告继续保留。

这里的体积指从项目目录移出的文件；归档仍保存在同一磁盘上，并未永久删除。

## 归档位置与恢复

`D:/indiegameDev/unity/ProjectCleanupArchive/Dark-Brine-Rot-20261006-201500/`

归档保留原相对路径：

- `local-artifact-manifest.json`：本地产物原路径、体积和归档路径。
- `asset-cleanup-plan.json`：Unity 资源移动前后的路径及 GUID。
- `Assets/`：资源整理前的副本；复制后已校验 SHA-256。恢复场景、截图及脚本的原 `.meta` 同样保留。
- `user-save.before-cleanup.json`：检查前的用户存档副本。

如需恢复历史预览或备份，将对应文件按原相对路径复制回项目。恢复 Unity 资源时，文件与 `.meta` 应成对恢复；已迁移到新目录的脚本应通过 Unity 移回原位置，不要同时放入两个具有相同 GUID 的副本。

## 验证

- Unity 编译通过，检查期间没有新的控制台错误。
- 7 个构建场景及其 334 项依赖 GUID 与整理前完全一致；252 项运行时 Resources 的路径和 GUID 完全一致。
- 30 个移动资源的文件内容及 `.meta` 与整理前副本逐字节一致，包括此前未提交的剧情与性能改动。
- 没有重复 GUID 或孤立 `.meta`；Unity 中的旧恢复资源已清空。
- 岛屿加载、植被缓存、地图显示和关闭、语言切换、复活点、静音状态下的攻击检查通过。
- 碎船鲨鱼剧情检查通过：202 个碎片、双方交手、Sahur 战败、丢失棍子，以及无武器昏迷上岛。
- 存档已恢复；编辑器停在 MainMenu，场景文件、构建场景配置和插件未修改。

检查记录在本地 `.codex/cleanup/`。重复资源审计可运行 `Tools/AuditProjectCleanup.cs` 的 `AuditProjectCleanup.Run`；清理工具 `CleanProjectAssets.Run` 默认只预览计划。
