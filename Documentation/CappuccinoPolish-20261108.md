# Cappuccino 动画与特效第二轮打磨

用户反馈：上一版动作不够丝滑，特效也需要继续提升；允许从网上寻找动画。

## 新素材与动作

从作者的 [KayKit Character Animations](https://kaylousberg.itch.io/kaykit-character-animations) 免费下载入口取得 Free 1.1 原包，使用其中双持横斩、双持下劈、双持刺击、旋转、受击与召唤动作。包内 CC0 许可保留在 `Assets/ThirdParty/CapriPolishKayKit/License.txt`，原始 FBX 保留；没有使用付费资源或创建账户。

新源骨骼有 wrist → hand 两个关节。直接复制旧月球动作 Avatar 会漏掉握刀手的局部旋转，因此只为新源创建独立 Humanoid Avatar，将 LeftHand / RightHand 分别映射到 hand.l / hand.r。原模型、拟合骨骼、旧 Avatar 与第一阶段素材不变。

六个版本化动画保存在 `Assets/Resources/CapriPhase2/Polish`，原六个动画保留。控制器原有第二阶段状态引用新版本，第一阶段与死亡状态引用未变。

本轮保留原技能总长度、起手和伤害窗口：

- 使用新素材完整肩、肘、手腕运动，替换上一版稀疏的手臂大姿势覆盖。
- 连招段落以 0.18 秒 Hermite 惯性修正衔接，传递前一段速度并平滑消退。
- 各动作起止手臂回到共同双刀架势；运行时进入技能以约 0.16 秒姿势混合承接当前姿态。
- 第二阶段姿势在动作时钟更新后立即评估，骨骼转移和刀轨在同帧读取，减少一帧滞后。
- 保留原 CharacterController 位移、朝向、冷却、伤害、回血、阶段进度和中断规则。

重建新版本：`Tools/ImportCapriPolish.cs` → `Tools/BuildCapriMotionPolish.cs`。旧 `BuildCappuccinoPhase2.cs` 会恢复上一版素材引用，若使用旧构建器，需再运行新构建器。

## 特效

- 新增两把刀各自的连续网格带，采样实际刀刃，约 0.19 秒渐隐，带咖啡色边缘与奶金亮部。原第一阶段刀轨未改。
- 残影依实际位移/旋转采样，缩短存留并使用轮廓菲涅尔、噪声溶解，减少重叠实体感。
- 蒸汽加 UV 柔边、流动噪声与轻微摆动；扩散环改为有流动明暗的羽化轮廓，范围仍来自原实际判定。
- 有界池和预热网格保留，无每次攻击生成/销毁大量效果对象。

## 本轮验证

`CappuccinoPolishReview/native-tests.json`：228 条原生 Play Mode 断言通过，0 个错误/异常，含原有三种攻击、转换、四种新技能、命中、收招、隔墙、禁用、残影尺寸与空中死亡。断言数包含逐帧检查，不是 228 个独立人工用例。

`CappuccinoPolishReview/Cappuccino-Polish-Native.mp4`：519 张原生运行画面按记录的游戏时间编码为 30 FPS，录像无声；包含全部新技能和转换。`NativeCombatReview.jpg` 为画面总览。原五场景、Play 启动场景设置与两个存档已恢复，见 session-preserved.json。

代码编译与 URP Shader 检查通过。旧版本源和控制器备份在 `CappuccinoPolishReview/before`。本轮属于针对反馈的动作/特效打磨；画面与动作最终观感仍需用户审阅，不能用自动断言代替美术验收。验证为隔离原生战斗场景，未声称全岛人工通关或硬件帧率认证。
