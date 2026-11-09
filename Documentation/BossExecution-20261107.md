# Boss 处决系统：配置与验证

**已退役：用户于 2026-11-08 要求移除处决，当前项目已撤除本功能。** 当前结果见 [BossExecutionRemoved-20261108.md](BossExecutionRemoved-20261108.md)。下文仅保留历史记录，勿据此重新运行旧配置工具。

后续完整配对片段、英文提示和九组演出进展见 [BossExecutionSequences-20261108.md](BossExecutionSequences-20261108.md)。本文记录前一阶段基线；下文“未制作配对 clip”已由该后续阶段更新。

## 生效规则

运行时不匹配 Boss 名称。Boss 根物体上的 `BossExecution` 组件决定结局：`Execution` 延迟死亡，`Scripted` 保留原流程。不带组件的普通敌人、剧情鲨鱼沿用原来的死亡/剧情逻辑。

当前配置：Cappuccino、Nailong、猪武士为 Execution；飞机为 Scripted。猪武士仍在原有第二岛中，不改变第一岛流程。第一岛场景、角色、敌人顺序、伤害、奖励内容及检查点没有因本任务修改。

## 行为与修改入口

- `Assets/Scripts/Combat/BossExecution.cs`：Fighting → Pending → Executing → Completed。零血时暂停配置的 AI/攻击组件和攻击碰撞体，播放弱势状态；完整结束前不发死亡、掉落或进度事件。取消演出返回 Pending；玩家死亡或离开 65 米、Boss 组件停用则恢复战斗与满血，不发奖励。
- `Assets/Scripts/Enemy/Health.cs`、`Assets/Game/Prefabs/Bosses/Nailong/Scripts/NailongHealth.cs`：两种现有生命组件的延迟死亡接入。待处决期间 `IsDead=false`，额外伤害/治疗忽略。完成后调用原有死亡表现、存档记录、掉落与 OnDeath，重复 Complete 无效。未处决时保存不会记录已击败，重载后可重新挑战。
- `Assets/Scripts/Combat/ExecutionTarget.cs`：只检索 Pending 配置；距离、遮挡、锁定目标检查。旧的直接 Finish 接口保留但禁用，防止跳过演出提前发奖。
- `Assets/Scripts/Combat/SahurExecution.cs`：近距离浮动提示 `Press [E] 处决`（跟随实际键位）；独占认领、碰撞约束下移动和转向、动画结束检测、输入/相机/Animator 恢复。1.3 秒对齐失败或 4 秒动画异常则取消。无传送。步枪根据真实枪身长度留出额外空间。
- `Assets/Game/Prefabs/Characters/Sahur/Scripts/ThirdPersonPlayerController.cs`：现有 Sahur 在第一、二岛安装处决组件；处决期间禁止普通移动，实际对齐速度驱动移动动画；相机平滑偏转 18°、距离收至原来的 92%，结束后还原。
- `Assets/Game/Prefabs/Characters/Sahur/Scripts/PlayerHealth.cs`：处决演出期间保护玩家。原有武器切换、攻击、投棒沿用 ExternalControlLock；E 在处决启动/结束帧消费，避免同时投棒。
- `Assets/Game/Prefabs/Characters/Sahur/Scripts/SahurRifleAnimation.cs`：对齐时持枪、结束时开枪，使用 Boss 当前启用的身体碰撞体中心瞄准，恢复上层权重。修正 Nailong 停用旧 CapsuleCollider 后瞄向错误位置的问题。

上述生命组件中的历史 Boss 平衡和流程代码仍保留；新增处决资格判断不依赖这些类型或名称。

## Prefab 和动画

通过 Unity 原生 API 修改并保存四个 Boss Prefab：

| Prefab（Assets/Game/Prefabs/Bosses 下） | 结局 | 弱势表现 | 暂停组件数 |
| --- | --- | --- | --- |
| CappuccinoAssassino/CappuccinoAssassino.prefab | Execution | Base Layer.Retreat | 3 |
| Nailong/Nailong.prefab | Execution | Base Layer.Stumble | 5 |
| PigRonin/Pig Ronin Boss.prefab | Execution | 现有骨架的轻微失衡倾斜 | 2 |
| BombardinoCrocodilo/BombardinoCrocodilo.prefab | Scripted | 原剧情 | 0 |

Nailong 的 PhaseAnimation、HitReaction 从原有 Awake 按需添加，改为在该 Prefab 中显式配置，以便序列化暂停引用；Awake 已会复用已有组件，不重复创建。

三个现有玩家 Controller 增加 `Execution Stick`、`Execution Blades` 独立状态，分别复用原来的 Combo 3、Capri Combo 3 的 Motion 与播放速度。没有替换 clip，没有改变原连招 transition；新增状态无自动退出 transition，由处决所有者完成/取消。

- Assets/Game/Prefabs/Characters/Sahur/Animations/SahurGrounded.controller
- Assets/Resources/Encounter/v006/SahurGameplay.controller
- Assets/Resources/Equipment/M4A1/SahurRifle.controller

这是现有动作的稳定演出接入，**没有新制作按每个 Boss、每种武器配对的专属动作 clip**。Boss 使用自身弱势姿态，玩家按武器使用对应动作。需要更细的手部接触、受击反应或专属处决编舞时，应制作并指定对应状态；当前不声称达到专门配对动画的质量。

新 Boss 应在 Prefab 根节点配置 Ending、combatBehaviours、attackColliders、bossAnimator/vulnerableState 或 weakVisual、interactionRange 和动作状态名。必须支持现有 Health/NailongHealth 之一；其他生命组件需要先接入延迟死亡契约。剧情 Boss 明确设为 Scripted。

`Tools/ConfigureBossExecutions.cs` 是一次性原生资产迁移工具；已配置 Prefab 会跳过，不覆盖后续 Inspector 调整。正常维护直接使用 Inspector，不重复迁移。原文件备份保存在 `.codex/boss-execution-20261107/`，未删改历史证据。

## 已执行验证

环境：Unity 6000.3.23f1、现有 DX11 Editor Play Mode，RTX 4060。没有启动额外 Editor、修改 PlayerSettings 或保存场景。测试使用隔离存档和临时运行时场地，真实角色、武器、Boss Prefab；首轮启动走 Input System 的 E 键注入。专项测试中的传送和直接扣血仅用于复现，不算普通玩家通关验收。

1. 脚本重新编译完成，无编译错误。
2. `Tools/VerifyBossExecution.cs`：13 项通过，包括零血待处决、AI/攻击碰撞体暂停、忽略重复伤害/治疗、独占认领/取消/重试、满血复位、单次死亡、Scripted 和普通敌人正常死亡、另一种生命组件接入。
3. `Tools/PlaytestBossExecution.cs`：木棒/Cappuccino、双刀/Cappuccino、M4A1/Nailong、木棒/猪武士四组均通过零血待处决、E 启动、最终死亡一次、输入/相机还原。木棒两组额外验证禁用玩家处决组件、暂停、禁用 Boss 组件和重试。其他两组的中断行为未单独覆盖。
4. 步枪对齐曾实际失败，定位到停用碰撞体及过近站位并修复。最终截图帧枪口与目标方向点积为 1.0000006（浮点误差），回归门槛为 0.98。
5. 已渲染检查提示、木棒与步枪动作截图。Prompt 为实际 GameView；action 图片为相同测试场地的侧面辅助机位，不是完整电影镜头录像。
6. 最终回归记录 `passed=true`，捕获 Error/Exception 为 0。Test Runner 发现 0 项，不能宣称 Unity Test Runner 测试通过；以上为实际执行的专项验证脚本。
7. 退出自建 Play 后五个第一岛场景路径/dirty 与基线一致、均干净，活动场景为 Main；无处决临时 focus。正式 save.json 和 .bak 的 SHA256 与开始前一致。未保存 Play 状态、未广泛应用 Prefab 覆盖。

结果：`.codex/boss-execution-20261107/health-verification.json`、`play-result.json`、`final-editor-state.json`、`final-saves.json`。失败尝试也保留在同目录。

## 截图

![实际悬浮提示](../.codex/boss-execution-20261107/Stick-prompt.png)

![木棒演出](../.codex/boss-execution-20261107/Stick-action.png)

![双刀演出](../.codex/boss-execution-20261107/CapriTwinBlades-action.png)

![修复站位与枪口后的步枪演出](../.codex/boss-execution-20261107/M4A1-action.png)

## 尚未验收

- 正常键鼠从新存档完成全岛及原飞机抓走剧情；独立 Windows Player 构建。
- 所有武器 × 所有 Boss 的完整视觉组合；真实岛屿斜坡、墙边和低帧率下的演出与镜头遮挡。阻塞时会取消回 Pending，但这些地形尚无全面实机覆盖。
- 长时间性能/内存、退出继续、重载全部剧情奖励链的完整回归。当前验证的是原死亡回调接入与单次触发，不等于全关卡通关。
- 现有动画复用的手部/武器精确接触和 Boss 专属编舞仍有提升空间。

云层条纹、环境装饰碰撞、脚印和其他环境内容不在本次处决修改范围，原有改动保留。
