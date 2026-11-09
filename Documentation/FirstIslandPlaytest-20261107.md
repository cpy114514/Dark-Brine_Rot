# 第一岛完整试玩与回归记录

测试日期 2026-11-07。硬件为 Windows 11、Intel Core i5-14400F、32 GiB RAM、NVIDIA GeForce RTX 4060。独立 Player 使用 Unity 6000.3.23f1、Direct3D11，产品名为 `DarkBrineRot_Playtest`。构建、存档和测量的原始证据在 [试玩目录](../.codex/first-island-playtest-20261107/)。

## 问题和修改

| 顺序 | 触发步骤与观察 | 实际修改 |
|---|---|---|
| P1 死亡后旧战斗继续 | 在 Cappuccino 战斗中受击死亡，屏幕暂停时按 Enter 复活。预期回到检查点并重新开始遭遇。基线中 Cappuccino 仍为 `FightActive`、`Recover`、641/999，并继续追向检查点。重复出现。 | `PlayerDeathRespawn` 恢复血量前通知 `FirstIslandBossProgression`，只转发给第一岛中正在和该玩家交战的 Boss。三个 Boss 沿用各自的返回状态。Cappuccino 在解除暂停的下一次更新返回，保留原有大招退出清理。飞机共用已有返航和危险物清理。 |
| P2 新存档落岛镜头过近 | 清空测试产品设置，从主菜单开始新游戏，正常完成教程并站到手杖前。主菜单没有可读的 Sahur 实例，写入 4.2 m，落岛画面上缘裁掉角色头部。 | `PauseSettingsMenu` 的本轮测试快照将首次无角色的默认值对齐当时 Prefab 的 8.4 m，保留测试时已有镜头设置。工作区之后另有 14 m 镜头更新，本报告的 8.4 m 对比属于测试快照。 |
| P3 落岛提示重复 | 落岛并恢复控制后，未走到手杖附近时左侧目标卡和底部同时说明找手杖。 | `FirstIslandArrival` 有启用的目标卡时，由其承担远处引导。近距离显示根据当前绑定生成的交互键，拾取后保留短提示。没有目标卡的配置保留原远处文案。 |

相关项目文件为 `Assets/Scripts/Combat/PlayerDeathRespawn.cs`、`Assets/Scripts/Story/FirstIsland/FirstIslandBossProgression.cs`、`Assets/Scripts/Story/FirstIsland/FirstIslandArrival.cs`，以及 `Assets/Game/Prefabs/Bosses/` 下 Cappuccino、Nailong 和 Bombardino 的 AI 脚本。镜头默认值在 `Assets/Game/Prefabs/UI/SettingsMenu/Scripts/PauseSettingsMenu.cs`。本轮 QA 未保存场景、Prefab、材质或画质资产。

死亡基线见 [原 Boss 状态](../.codex/first-island-playtest-20261107/first-run/death-cycle-3-boss.json)。修复后同一进程完成三轮读档、交战、自然死亡和 Enter 复活。Boss 在死亡时分别为 835、863、844 HP，复活后进入 Returning，随后回到原位和 999 HP。玩家恢复 100 HP 并可操作。飞机在完整路线复活后恢复 Patrol 和 2,100 HP。[三轮日志](../.codex/first-island-playtest-20261107/soak/repeat-memory.jsonl) · [飞机复测](../.codex/first-island-playtest-20261107/fixed-run/bombardino-reset.json)

## 前后画面

| 旧默认与重复提示 | 测试快照中的 8.4 m 与单条引导 |
|---|---|
| ![修改前落岛手杖前](../.codex/first-island-playtest-20261107/first-run/shore-before-pickup.png) | ![修改后落岛手杖前](../.codex/first-island-playtest-20261107/fixed-run/shore-before-pickup.png) |

两次均为 1920×1080、PC、4× MSAA、0.75 渲染比例。角色水平位置相差不到 0.1 m，记录的 yaw/pitch 为 263.54° / 30.74°。镜头距离是比较变量，抓帧时刻和海浪相位有差异。近距离正常按 F 拾取。[近处交互提示](../.codex/first-island-playtest-20261107/fixed-run/shore-near-pickup.png) · [拾取结果](../.codex/first-island-playtest-20261107/fixed-run/pickup-after.json)

## 完整路线与保存

修复版 Development 从无存档主菜单开始，使用经本地测试输入接口送入 Unity Input System 的普通键鼠事件，经过开场船难和全部教程、落岛与手杖、Cappuccino、鸡鸭鹅、Nailong、飞机、被抓走及第二岛对话。没有传送、改任务状态或直接伤害 Boss。观察窗口 22.6 分钟。另一次非 Development 启动从正式游戏菜单 Continue，复读了落在第二岛的测试存档，确认角色位置、82 HP、M4A1 及 15/30 弹匣、60 备用弹。

- 走路约 30.65 m，体力 100 → 100。冲刺 100 → 81.2 后自然回满。练习攻击、闪避、锁定、武器切换、治疗、射击和换弹均有实际输入及状态记录。
- Cappuccino 死亡后正常拾取双刃并按 2 装备。鸡鸭鹅阶段死亡后能继续，已完成的 Boss 记录和装备保留。打完 Nailong 后正常拾取并按 3 装备 M4A1。
- 飞机通过正常射击从 2,100 HP 到 0，触发抓走与第二岛场景切换。落地对话按 Space 结束。交接发生一条 PlayerInput 设备配对警告，鼠标最初未锁定，单按 W 没有移动；普通左键点击后 W 移动约 3.65 m。完成保存退出。警告和焦点交接保留为待独立复现项。
- 实际写盘快照见 [Cappuccino 后](../.codex/first-island-playtest-20261107/fixed-run/after-capri-save.json)、[鸡鸭鹅后](../.codex/first-island-playtest-20261107/fixed-run/after-flock-save.json)、[Nailong 后](../.codex/first-island-playtest-20261107/fixed-run/after-nailong-save.json)、[结束后](../.codex/first-island-playtest-20261107/fixed-run/finished-save.json)。[飞机战](../.codex/first-island-playtest-20261107/fixed-run/aircraft-rifle.png) · [切换后落下](../.codex/first-island-playtest-20261107/fixed-run/captured.png) · [第二岛对话](../.codex/first-island-playtest-20261107/fixed-run/handoff-dialogue.png) · [非 Development 继续游戏](../.codex/first-island-playtest-20261107/release-continue/continued-window.jpg)

## 观察和构建

`IslandPerformanceBaseline` 增加显式参数 `-baseline-observe-only`，沿用 `-baseline-report=<绝对路径>`。观察模式只读取现有状态和渲染回调，记录阶段、日志、帧时间、进程内存和 Unity 分配量，不输入、传送、触发剧情或更改设置。旧 `-sahur-baseline` 入口保留。新实现位于 `Assets/Scripts/Performance/IslandPerformanceBaseline.Observe.cs`。观察文件包含每 15 秒更新的 `observe.json`、每秒更新的 `state.json` 和阶段/日志 `events.jsonl`。

构建由 `Tools/FirstIslandPlaytestBuild.cs` 使用编辑器 API 生成，测试脚本位于 `Tools/FirstIsland*Input*`、`FirstIslandWalkTo.ps1`、`FirstIslandRespawnCycles.ps1`。完整通关后进一步剔除了观察模式中一个可能间接触发布置的目标查询，最终 Development / 非 Development 包均含这项修订。该版构建时原工作区并行的编辑器宣传脚本有独立编译错误，最终两包在隔离项目快照构建，排除了该编辑器脚本。原工作区之后重新编译为 0 错误。最终包均构建通过，Development 0 错误 / 195 警告，非 Development 0 错误 / 196 警告。Development 的输入服务仅监听本地环回，非 Development 没有测试输入服务。

## 性能和内存

性能采样目标为 1920×1080、PC 画质、4× MSAA、1.0 渲染比例、限帧关闭、VSync 0。阶段帧时间来自相邻观测帧的墙钟时间，计入实际呈现期间的卡顿。完整路线落岛后才切至该设置，期间另有 Unity 批处理导入，下面的路线数据属于当次观测，不能单独归因于战斗渲染。首次加载与开场冷帧在原始报告中单列。

| 完整路线阶段 | 均值 ms | P95 ms | 该次是否符合目标 |
|---|---:|---:|---|
| Cappuccino | 13.35 | 15.70 | 是 |
| 鸡鸭鹅 | 24.36 | 40.70 | 否 |
| Nailong | 29.53 | 41.60 | 否 |
| 飞机 | 8.08 | 11.20 | 是 |
| Second Island | 8.54 | 10.40 | 是 |

首次观测 90 分钟计划在约 61 分钟因进程退出而失败。Windows 事件记录 PID 25008 的 `0xc0000005`，指令地址落在已卸载的 `mono-2.0-bdwgc.dll+0x3f8560`。保留了原生日志和转储。事发时有并行桌面工作，当前没有证据可归因于脚本、显卡或某个具体回调，也没有据此改动草地、缓冲或诊断线程。[原生崩溃事件](../.codex/first-island-playtest-20261107/soak/native-crash-events.json) · [转储摘要](../.codex/first-island-playtest-20261107/soak/native-crash-minidump-summary.txt)

| 同进程轮次 | 读档后约 5 秒私有内存 MiB | 复活后约 9 秒私有内存 MiB |
|---|---:|---:|
| 1 | 4,478.6 | 4,667.2 |
| 2 | 4,839.5 | 4,734.3 |
| 3 | 4,922.3 | 4,759.9 |

复活后同口径三轮增加 92.7 MiB，后续样本约 4,770 MiB。三个周期不能证明长期泄漏或无泄漏。第二轮为非 Development 版本，从鸡鸭鹅完成后读档，独立测试第一岛连续渲染。其结果以 [最终观测](../.codex/first-island-playtest-20261107/release-soak/observe.json) 和下面的补充记录为准。

## 工作区保护与未关闭事项

原编辑器的 5 个第一岛场景、dirty 状态和 6,098 个 Prefab 根实例已记录。原产品名 `Dark Brine_Rot`、正式存档及备份、PlayerPrefs、质量/构建设置和包文件已核验。原编辑器保持 Main 激活、场景 clean、Play 停止；首次核验的场景字节一致，后续 Environment 有独立改动，已保留。当前工作区还包含测试快照之后的镜头和第二岛修改，快照结果不能作为这些后续改动的验收。

- Test Runner 查询实际发现 0 用例。原生 Boss 顺序检查复核了三个固定 Boss ID，正常路线也验证了鸡鸭鹅门控。测试脚本或进程操作失败单独记录。
- 云层条纹单独跟踪。本轮没有编辑天空。已观察的长测机位没有出现此前的大白光或明确的旧横纹，仍可见细采样纹理。该机位不能覆盖整个岛屿或不同时间。[长测机位](../.codex/first-island-playtest-20261107/release-soak/first-island-view.jpg)
- 原生编辑器复核了当前 Environment。草甸 136,643 个，两层生态 2,724 / 738 个，生态烘焙自管碰撞分别为 145 / 40 个。沉船 49 个原开场构件、65 个 Renderer、18 个实体碰撞、0 个刚体、0 个缺材质。重跑 3,333 个沉船保护路线胶囊查询，0 次阻挡，18 个碰撞均可查询。上述查询属于专项检查。 [环境核对](../.codex/first-island-playtest-20261107/environment-audit.json) · [沉船复测](../.codex/first-island-playtest-20261107/shipwreck/validation.json) · [环境维护说明](Island1EnvironmentMaintenance-20261107.md)
