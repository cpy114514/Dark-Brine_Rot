# Sahur 操控手感优化

2026-11-04 · task072 · Unity 6000.3.23f1 / URP · 分支 `codex/sahur-game-feel072`

本轮已接入现有角色 Prefab、三套 AnimatorController 与 CombatFeelProfile，默认 Balanced。沿用已有输入、缓冲、CharacterController、Root Motion、打击反馈与镜头系统。没有新增控制器架构、提高移动上限、改变伤害/体力消耗或重做全部动画。

## 五阶段结果

1. **审计与基线**：检查主控制器、攻击及 Partial Classes、GameControls、SahurMotionContext、RootMotionRelay、EnemyLockOn、CombatFeelProfile、PlayerStamina、相机/命中顿帧，以及现有动作与输入检查工具。实际 First Island/Gameplay 中角色为3倍缩放，速度9.6、冲刺倍率1.75、加速68、制动84、转身780°/s、翻滚0.7927928s、镜头跟随22；Awake会把 Grounded controller 换成 SahurGameplay。基线在修改前记录，源码/原配置保存在 `.codex/game-feel072/source-before/`、profile-before.asset、tracked-before.patch。
2. **移动**：普通加速、制动、方向变化和空中控制分别调节。身体朝新意图转向，不再必须等残留速度掉头后才转。动画速度继续取实际胶囊位移，减少叠加的动画参数延迟。斜向限速、跳跃积分、海水、第二岛弹跳及原有动作拥有权保留。
3. **动作**：普通翻滚独立播放倍率与实际时长同步，去掉过长的尾段占用；原地额外位移的连续积分保持同一设计，移动输入下总距离不保证完全相同。修复近地攻击取消被误判为空翻、立即落地的根因；复用已有近地探测。伤害窗结束前不允许方向键直接抹掉双刀全身攻击；已有棍击上半身迁移保留原阶段。缓冲、合法取消、连击衔接、接跳跃/下一次闪避与真实退出点一致。完美闪身的0.28s、2m和反击规则保持，切预设也不会改变正在执行的翻滚时间线。
4. **镜头与反馈**：玩家视角输入直接驱动旋转，位置跟随独立平滑，碰撞收缩立即处理、退出障碍单独平滑；镜头交接保留原速度。修正脚底局部偏移未乘角色缩放造成的过高焦点；锁定时将焦点前移量补偿到距离，避免贴近角色。锁定侧移用现有前/后/左右动作做腿部混合，遮罩不改 Root、躯干和武器双臂，攻击/闪避/剧情拥有优先权。命中顿帧适度缩短，保留力度层级、音效与原特效。
5. **运行验证与调参**：见下表及原生报告。已查看实际镜头、翻滚退出、落地、棍击移动、双刀移动和锁定侧步图像；第一岛实景中重新验证过控制与镜头。未以脚本通过代替最终人工手感评价。

## 参数与实测

| 项目 | 优化前实际值 | Balanced |
|---|---:|---:|
| 移动 / 冲刺倍率 | 9.6 / 1.75 | 保持 |
| 起步加速 | 68 | 74.8 |
| 制动 | 84 | 105 |
| 大角度改向初段加速 | 通常84 | 123.42 |
| 转身上限 | 780°/s | 904.8°/s |
| 普通翻滚完整时长 / 90%退出 | .793s / .714s | .650s / .585s |
| 退出后冷却 | 配置.18s另加未播放尾段 | 从实际退出点计.18s |
| 输入缓冲 | .22s | .26s，命中局部冻结时保留余量 |
| 轻击取消 / 连击恢复阈值 | .90 / .90 | .74 / .80 |
| 重击取消阈值 | .62 | .64 |
| 普通 / 终结 / 重击顿帧 | .055 / .110 / .095s | .045 / .085 / .080s |

取消阈值与**本段伤害窗结束+.02**取较大值，不能理解为所有轻击到74%都能取消；第一段棍击仍需约88%。连击也保留伤害窗后的交接余量。轻击播放曲线只调整接触结束后的返回段（.88处 .85→1.05），起手和主要发力峰值保留。

| 60Hz固定步长的逻辑响应 | 基线 | Balanced |
|---|---:|---:|
| 起步明显响应 | 1帧 | 1帧 |
| 达到九成步行速度 | 133ms | 117ms |
| 急停时间 / 距离 | 117ms / .47m | 100ms / .36m |
| 冲刺反向到九成速度 | 467ms | 333ms |
| 移动落地恢复 | 183ms | 150ms（实岛约117ms） |
| 翻滚占用 | 700ms | 583ms |
| 鼠标输入30°后的首个已评估镜头帧 | 约9.18° | 30° |
| 测试中约.64阶段请求后，棍/双刀合法闪避等待 | 未出现可见地面翻滚 | 约150 / 100ms |

这里测的是 Unity 输入事件进入后的逻辑/动画响应，不是硬件到显示器的端到端延迟。相同的申请阶段不保证在同一毫秒就合法，应分别看缓冲和动作承诺。第一段攻击近地问题在实时回放中也复现并修正。

## 预设与诊断使用

选择 `Assets/Resources/Combat/SahurCombatFeel.asset`，在 **Sahur control experiments** 中切换 **Control Preset**，展开 Responsive / Balanced / Weighty 修改各自响应。倍率作用于实际 Prefab/场景参数，不覆盖原序列化速度字段。

| 预设 | 加速 / 制动 / 改向倍率 | 转身倍率 | 翻滚时长倍率 | 实测60Hz冲刺反向九成 / 翻滚占用 |
|---|---|---:|---:|---|
| Responsive | 1.25 / 1.50 / 1.85 | 1.35 | .75 | 283ms / 533ms |
| Balanced | 1.10 / 1.25 / 1.65 | 1.16 | .82 | 333ms / 583ms |
| Weighty | 1.00 / 1.10 / 1.45 | 1.05 | .90 | 400ms / 633ms |

三个预设共用相同输入、武器、伤害、体力、跳高、完美闪身及剧情功能。角色上的 **Use Control Preset** 和资源的 **Use Control Tuning** 可停用参数实验；这不是完整源码回退。

开发时选中角色，打开 **Development diagnostics / Show Control Diagnostics**。面板显示输入时间、当前状态、移动锁、速度、攻击阶段、过渡和最近接受/缓冲/拒绝原因；64条有界事件可由 `ControlEvents` 读取。GUI、采样和调用在非开发 Player 中被条件编译移除，默认关闭。

## 原生验证清单

所有正式证据位于 `.codex/game-feel072/`，总表为 verification-summary.json。

| 正式回放 | 检查 / 帧 | 结果 |
|---|---|---|
| accepted-balanced-30 | 20 / 427 | 通过，无记录异常 |
| accepted-balanced-60 | 20 / 801 | 通过，无记录异常 |
| accepted-balanced-120 | 20 / 1562 | 通过，无记录异常 |
| accepted-responsive-60 | 20 / 782 | 通过，无记录异常 |
| accepted-weighty-60 | 20 / 827 | 通过，无记录异常 |
| accepted-realtime-actions | 12 / 518 | 通过，无记录异常 |
| actual-island-final | 20 / 796 | 通过，无记录异常 |
| integration-island-60 | 14 / 441 | 通过，无记录异常 |
| handoffs-30 | 4 / 51 | 通过，无记录异常 |
| handoffs-60 | 4 / 96 | 通过，无记录异常 |
| handoffs-120 | 4 / 187 | 通过，无记录异常 |
| weapons-final-60 | 16 / 408 | 通过，无记录异常 |
| 原有 Wukong step/counter 回归 | 37项 | 通过：含真实Nailong、键鼠/手柄、武器命中、朝向与遮挡 |
| 飞机抓走→第二岛醒来 | 726帧 | 通过：暂停、物品/武器/弹药/血量、单角色、对话与恢复移动 |
| Animator/Prefab资源完整性 | 334项 | 通过：原动作名与基础速度保留，根/躯干/手臂遮罩边界、参数与脚本引用有效 |

30/60/120 指固定模拟步长一致性检查，另有60Hz目标的真实时钟输入回放；不是保证硬件/GPU达到这些FPS。实时回放有截图开销，不能用它宣称帧率表现。最后一次冷却、接跳跃和运行中切预设修正由三档 handoffs-* 回放单独复核；最后锁定镜头距离修正在 actual-island-final 中验证。其他共用运动/攻击逻辑未再改变。

被排除的早期记录：baseline-60 在序列化 Vector2 时失败；早期A/D界限错误地包含竖向贴地速度，首帧镜头值在LateUpdate前采样，已由 baseline-summary.json 的正确平面速度/已评估帧分析替代；actual-island-60 的测试启动回调跨Domain Reload丢失，未接管到角色；actual-island-60-b 的假目标放在了起伏地面里，锁定按真实遮挡规则拒绝。修正测试设置后 actual-island-final 全部通过，不将这些失败记录冒充验收。

## 修改文件

- `Assets/Game/Prefabs/Characters/Sahur/Scripts/ThirdPersonPlayerController.cs`
- `Assets/Game/Prefabs/Characters/Sahur/Scripts/ThirdPersonPlayerController.PerfectDodge.cs`
- `Assets/Game/Prefabs/Characters/Sahur/Scripts/ThirdPersonPlayerController.Feel.cs`
- `Assets/Game/Prefabs/Characters/Sahur/Scripts/ThirdPersonPlayerController.Diagnostics.cs`
- `Assets/Game/Prefabs/Characters/Sahur/Scripts/SahurAttack.cs`
- `Assets/Scripts/Combat/CombatFeelProfile.cs`
- `Assets/Scripts/Combat/EnemyLockOn.cs`
- `Assets/Resources/Combat/SahurCombatFeel.asset`
- `Assets/Game/Prefabs/Characters/Sahur/SahurPlayer.prefab`
- `Assets/Game/Prefabs/Characters/Sahur/Animations/SahurGrounded.controller`
- `Assets/Resources/Encounter/v006/SahurGameplay.controller`
- `Assets/Resources/Equipment/M4A1/SahurRifle.controller`

另有新 Partial 的 `.meta`（原资源GUID不变），以及 Tools/AuditFeel072.cs、InstallFeel072.cs、InstallFootwork072.cs、ReviewGameFeel072.cs、ReviewAbductionGameFeel072.cs、ValidateFeelAssets072.cs 和本文/AnimationWorkflow上下文。没有改动原FBX/AnimationClip、输入绑定、Boss/剧情实现或场景资源。

## 限制、试玩与回退

- 本轮是既有操控的功能与衔接优化，不标记为人工最终美术批准。锁定侧移复用现有腿部动画，胯部和上半身重心并非专门重做的近战侧步，建议重点体验自然度。
- 请重点试玩急转身的惯性、翻滚末段接跳/攻击/下一次闪避、锁定镜头与鼠标灵敏度、斜坡脚步和重击顿帧。全部Boss整场战斗、所有岛屿布局/碰撞与长时间性能/硬件延迟并未穷尽验证。
- 原场景与人类存档/治疗包偏好在测试前保护，停止Play后恢复并校验字节一致。主阶段恢复并校验了初始五场景。补测武器前的新快照 weapons-workspace 当时仅打开 Main，补测后按这份最新布局恢复为 Main；两次存档字节校验都通过，键盘/鼠标恢复，诊断默认关闭，未重放旧存档恢复。
- 回退时先停Unity，对照 source-before.json，仅恢复本轮12个改动文件中已有的源码/Prefab/Controller副本；资源配置用 profile-before.asset 恢复原 SahurCombatFeel 内容并保留现有meta/GUID。确认不再引用新增 Partial 后再移除其源码及meta。不要用 Git HEAD 覆盖前几轮尚未提交的工作。
- 用户同时要求清理本地测试视频：已移除625个（含6个原已跟踪的AUTO_QA视频），释放约2.193GiB。manifest与result保留在本目录。截图、测试JSON、回退备份、游戏资产和构建不在清理范围。本轮没有新录制视频。
- 没有提交、推送或重新打包exe；下载目录旧构建不含本轮更新。
