# 完整处决段落与武器/Boss 配对

**已退役：用户随后要求移除处决，当前项目已撤除本功能。** 当前结果见 [BossExecutionRemoved-20261108.md](BossExecutionRemoved-20261108.md)。下文仅保留历史记录，旧动画/配置已归档。

接续 BossExecution-20261107。提示已改为全英文 `Press [E] Execute`；键名跟随原有输入配置。零血待处决、仅 Boss、Scripted 剧情结局和单次奖励契约不变。

## 演出与配置

`Assets/Resources/Combat/Executions/` 新建九份配对配置和十八份独立 AnimationClip：三个 Boss × 木棒/双刀/M4A1，每组包含玩家动作和 Boss 响应。片段由项目现有姿态素材重新编排、平滑重定时，并加入目标适配的身体曲线。没有下载新动画；也不逐次随机动作。

| 武器 | 时长 | 主要节奏（秒） |
| --- | --- | --- |
| 木棒 | 2.7 | 准备、举棒 .3–1.07，重击 1.2，随动与收势至 2.7 |
| 双刀 | 2.5 | 准备，两次交叉斩 .9 / 1.35，收刀恢复至 2.5 |
| M4A1 | 2.6 | 抬枪 0–.65、稳定瞄准、1.15 射击、回弹、最后 .5 秒放低枪口 |

同一武器保持一致的动作语言和节奏，面对不同 Boss 调整身体线条、瞄准高度、受击方向及站位。重复同一组合播放相同段落。

- Cup：较高的杯身受击区域，躯干/头部失衡回应。
- Dragon：较低的重心、腹部瞄准和更重的回应。
- Ronin：持刀胸前姿态，近战额外留 .7 米；通用骨架的胸、头、手臂和腿部回应。测试采用正式第二岛场景的三倍根节点尺度，不修改原尺度。

新 clip 每秒 30 帧、非循环、无伤害事件。胶囊拥有位移；骨架的源世界平移/朝向归零，不将 FBX 原轨迹带入处决。对齐后双方同时开始配对片段，完整结束才结算死亡/奖励。

## 涉及文件与维护入口

- `Assets/Scripts/Combat/BossExecutionSequence.cs`：配对资源，配置武器、双方状态/clip、命中时刻、瞄准高度、间距。每个 Boss 的 `BossExecution.sequences` 指定三份配置；运行时不按 Boss 名称判断。
- `BossExecution.cs`：演出时允许 Boss 播放完整回应，不再反复冻结弱势状态；中断恢复 Pending 及 Animator 开关/速度。猪武士暂时使用现有 Animator，退出后还原原程序动画所有权。
- `SahurExecution.cs`：选择完整片段、等待结束、按配置时刻发命中声音、英文提示。保留旧动作名兼容入口。
- `SahurRifleAnimation.cs`：避免旧持枪层覆盖完整片段；连续抬枪、瞄准、回弹、收枪，指向配对指定的受击高度。
- 三个 Sahur Controller 各新增九个状态；Cappuccino、Nailong、猪武士 Controller 各新增三个回应状态。原普通攻击状态及 clip 不替换。
- 三个可处决 Boss Prefab 通过原生 API 指定配对；飞机仍为 Scripted。
- `Tools/BuildExecutionSequences.cs`：原生资产作者工具，源素材只读；重建本任务生成的 clip/配置/状态。会覆盖生成资源，手工精修时复制为新 clip 再改引用。
- `Tools/PlaytestExecutionSequences.cs`：隔离存档和临时运行时场地，注入 E 走真实输入入口，保存九组实际渲染帧。

## 验证和证据

环境：Unity 6000.3.23f1、现有 DX11 Editor Play Mode、RTX 4060。基线、源/资产备份、失败尝试及最终结果在 `.codex/execution-sequences-20261108/`。长测试另写 `play-result.json`，CLI 超时本身不等于游戏失败。

九组检查零血待处决、E 启动、双方选择正确状态、完整播放、提前不结算、死亡一次、输入/相机还原。杯子三种武器和猪武士木棒额外检查动画中途取消、暂停、恢复与重试；三组木棒另检查 Boss 停用。其他组合没有逐一覆盖所有异常分支。

步枪在命中阶段检查枪口方向点积 ≥ .98；记录玩家骨盆到胶囊根节点的水平偏移，防止源轨迹造成穿模。原 `VerifyBossExecution` 的 13 项生命/结局检查继续通过。Test Runner 之前发现零用例，专项脚本不冒称 Test Runner 测试。

首版流程通过但木棒画面穿模，已拒绝并保存证据。根因为源 FBX 的 RootT.z≈1.63 和 RootQ 被带入新片段，现已归零，并采用中立准备姿态。步枪取消后站在交互距离外，重试需先重新靠近；测试按此修正，没有扩大交互范围。

连续帧覆盖准备、攻击、收势；GIF 为按记录时刻播放的低采样率辅助机位预览，不是原帧率录屏。实际 HUD 为 `*-prompt.png`。

最终结果：九组 Play Mode 回归全部通过，捕获 Error/Exception 为 0，共保存 148 张序列帧；三次步枪命中采样方向点积最低 .99999994，玩家骨盆水平偏移最大 .1381 米。原生资源检查 88 项通过，确认双方 clip/状态引用、非循环、时长、命中时刻和剧情排除配置。正式存档及备份哈希未变；五个场景与基线相同且均不 dirty，活动 Main，无临时相机焦点，已退出测试。

预览：[木棒](../.codex/execution-sequences-20261108/Cup-Stick-preview.gif)、[双刀](../.codex/execution-sequences-20261108/Dragon-CapriTwinBlades-preview.gif)、[步枪](../.codex/execution-sequences-20261108/Ronin-M4A1-preview.gif)、[九组动作图](../.codex/execution-sequences-20261108/nine-pairs.png)。

审片记录（2026-11-08，FIRST PASS，30 FPS，木棒 0–81 / 双刀 0–75 / 步枪 0–78）：已观察到准备/举棒、斩击姿态、步枪抬放和目标响应；拒绝了首版源根轨迹导致的穿模，已针对 RootT/RootQ 和木棒 .3–1.2 秒准备段修正并重渲染；猪武士近战间距额外 .7 米已复查。未对审美打分，未观察/认证全部足底接触与复杂地形。阶段结果为已实现并完成专项审片的第一遍演出，等待用户观感反馈，不冒称人工 FINAL/APPROVED。

本轮是现有素材上的完整演出第一遍打磨，未宣称人工 FINAL 审核、逐关节接触精修或全部地形穿模认证。斜坡/墙边镜头、独立 Windows Player、长时间运行和完整剧情通关仍待验证。普通战斗、顺序、伤害不变；云层、装饰、脚印不在本轮范围。
