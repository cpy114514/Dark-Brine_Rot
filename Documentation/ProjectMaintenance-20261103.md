# 项目清理与局部性能优化 — 2026-11-03

## 已完成的清理

删除 `.codex` 中 9 份已被后续测试版本替代的 Windows Player 输出，共 3,339 个文件、24,559,325,643 字节（24.56 GB）。范围为 performance019/022/023/024 的旧 player，以及 grass027/028、origami029、comic030、art031 的 build/player。

每个目录均与成功构建报告中的可执行文件路径匹配，检查绝对路径、目录链接、运行进程及意外源文件，并在删除前两次核对 SHA256 清单。构建报告、截图、录像、项目快照和源资源保留；sync033、grass032、performance023/adaptive-build、performance024/final-build、performance025 和 standalone-v165 的 Player 保留。没有清空 Library 或着色器缓存。

工具：`Tools/CleanupSupersededPlayers.ps1`。本次 Plan/Apply 已执行完毕，清单和结果位于 `.codex/project-maintenance-20261103/player-cleanup-*.json`；该脚本针对这批明确目录，不用于无差别清理。

## 运行时修改

- `MainQuestGuidance` 在创建网格时初始化固定三角形索引，并预计算圆环的八个方向。每帧更新顶点属性，复用索引，避免反复清空网格和上传相同拓扑；最近路径点搜索复用玩家位置。
- `IslandMapUI` 在每次打开地图重建标记时取得重力腕带奖励引用，刷新标记时读取同一对象的最新营地状态。关闭期间发生的对象变化在下次打开时重新发现。标记直接保存 Text 引用，省去补给和营地颜色更新的重复 GetComponent。

没有修改场景、Prefab、动画、材质、渲染质量、战斗数值或存档格式。

## 验证与测量

Unity CLI 1.0.0-beta.15（已检查为最新），使用现有 Unity 6000.3.23f1 编辑器。

脚本重新编译通过，编译错误为零。`Tools/RunProjectMaintenanceChecks.ps1` 从 Git 基线 ca59178 提取原始引导线实现，在内存编译后与当前实现比较；不会把基线脚本导入 Assets。

最终 Edit Mode 原生验证通过 84 项检查：12 组路径长度/变换条件覆盖 2、19、257、1024 个路径点及均匀/非均匀缩放，顶点、法线、UV、颜色、三角形索引和边界保持一致。地图验证覆盖活跃、死亡、恢复、禁用及销毁目标，补给冷却颜色、营地清理颜色，以及奖励销毁和标记重建。

在 1024 点测试路径上，四轮交替测量、每个窗口 150 次调用，原始 Draw 平均 2.1549 ms，修改后 1.3816 ms，降低约 35.9%。这是编辑器中该方法的局部基准，不能当作整场游戏 FPS 或独立 Player 性能承诺。

早期测试夹具在 Edit Mode 调用了运行时 Destroy 接口，导致控制台报错；该结果不作为最终验收。夹具改用立即清理后重跑，最终 84 项通过，进入 Play Mode 前确认控制台游标 21 之后无新增错误。早期结果保留在 `verification-editmode-disposal-rejected.json`。

实际第一岛 Play Mode 冒烟检查通过：当前玩家存在引导线组件，地图成功打开并填充 7 个标记和 1 个岛屿，地图期间暂停生效，随后关闭地图并退出 Play Mode。启动时重报原有四条缺失植被场景日志，未发现本轮新增运行时异常。这不是完整任务流程、第二岛或 Boss 战验收。测试使用独立存档路径，退出后原存档及 `.bak`、`.tmp` 的内容/原先不存在的状态均与测试前一致；记录在 `play-smoke.json`、`save-restoration.json`。

资源审计前后完全一致：8 个构建场景、1,749 条场景依赖的路径/GUID、806 个 Resources 路径/GUID。4,291 个 metadata GUID 无重复，无孤立 metadata；Git 检查确认 Assets/ProjectSettings/Packages 中非 C# 文件无变化。原有五个打开的场景保持相同根节点数量、活动场景和未修改状态。

证据目录：`.codex/project-maintenance-20261103/`，包括 `verification.json`、`asset-integrity.json`、`dependency-comparison.json` 和清理清单。完整的前后资源列表位于 `.codex/cleanup/dependencies-20261103-before.json` 与 `dependencies-20261103-after.json`。

## 已有问题

第一岛 Environment 仍存在 606 个缺失植被 Prefab 实例（234 + 177 + 195），对应 GUID：

- `2bde5fdeb204bc6418608a5fe3993ad1`
- `8e67ad946c4264b479267ee1643fd2ce`
- `ad3d230ef428f6049ab82c5136f00a47`

这是 AUTO_QA/BUG_REPORT.md 中已记录的 QA-003。现有资源未找到原 GUID，未使用其他植物替换，也未删除实例掩盖错误。需要恢复原始 Prefab 才能解决，因此不宣称整个项目没有遗留错误。
