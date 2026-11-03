# 双刀空中攻击与穿群冲刺修复（task053）

范围：修复双刀无法进入空中攻击，以及蓄力冲刺被鸡哥身体挡停；确认命中时保留短暂停顿。沿用现有攻击动画、伤害窗口、伤害、蓄力和体力规则。

## 修改

- `SahurGrounded.controller` 和 `SahurRifle.controller` 缺少 `Air Combo 1/2`、`Capri Air Combo 1/2`。`StartJumpSlash()` 从第一段进入，因此虽有第三段 `Capri Jump Slash`，仍无法开始。通过 Unity 原生 Animator API 从现有 `Encounter/v006/SahurGameplay.controller` 补齐缺失状态、原片段和返回过渡，并补齐同源 `Air Attack Legs` 蒙版层。已有 v278 下劈动作保持原样。
- 步枪组件首次装备后会继续使用 `SahurRifle.controller`，因此同时修复这套图，覆盖步枪切回双刀的实际路径。
- `CombatBodyCollision` 仅在双刀冲刺有效窗口临时忽略玩家与 `ChickenBroAI` 身体碰撞体的接触；武器的扫掠/重叠查询继续命中。场景几何和 Boss 不在放行范围。离开冲刺后恢复碰撞；若结束点尚与某只鸡重叠，仅保留该已有接触对至分离，避免瞬间弹回。组件禁用时释放持有的忽略对。
- `SahurTwinBladeMotion` 在已有 `CombatLocalHitStop` 生效时不消耗冲刺进度、不移动角色，停顿结束后继续剩余距离。没有增加全局慢动作或为挥空制造停顿。

## 验证与边界

使用 Unity 6 / 6000.3.23f1 原生 Play Mode、项目实际 Sahur/ChickenBro/Nailong 预制体、排队键鼠输入与真实 `CombatHitFeedback`、`IDamageable` 路径；记录每三帧的原生渲染和逐段结果。固定模拟步长用于复现，不能当作实机帧率数据。

测试入口 `Tools/TestBlades053.cs`，图修复入口 `Tools/RepairBladeControllers053.cs`。测试用敌人 AI 暂停并提高生命值，让身体碰撞保持存在；这不是完整 AI 围攻或完整 Boss 战验收。空中伤害用前方可受击体检查实际武器接触，没有调用测试专用伤害捷径。

`final3-60`：27 项通过、473 张原生记录图、测试期间零运行/动画错误。覆盖三套 Animator 图的普通跳跃空中攻击、实际伤害及恢复、步枪实际装备后切双刀、高台完整三段、普通走路身体阻挡、八只鸡哥穿群、命中位移冻结、分离后碰撞恢复、鸡群后的墙体、Nailong 身体阻挡。命中停顿连续采样的最大平面位移为 0m。

`island-30`：在项目实际第一岛 `Environment` 与 `Lighting` 场景、原地形碰撞上重复全部 27 项，248 张原生记录图，全部通过，测试期间零运行/动画错误。八只鸡哥的阵列最远约 8.6m；冲刺实测前进 17.326m（平地 60Hz 为 17.6923m），确认真实武器伤害一次，连续命中停顿采样最大平面位移 0m。没有将身体穿越伪造为范围伤害。部分停顿比三帧截图间隔更短，由逐帧断言验证。

场景加载前已有缺失草地预制体报告（GUID `ad3d230ef428f6049ab82c5136f00a47`，195 个实例）；未修改或声称修好这项现有资源问题。测试不是完整第一岛内容验收。

工作区已恢复干净 MainMenu、停止且取消暂停，物理键盘/鼠标启用；`restored.json` 确认存档字节未变。场景和原动画文件未保存改写；先前走路体力/抓走过场改动保留。源文件和回退文件 SHA256 对照见 `.codex/blades053/source-sha256.json`。

早期诊断 take 保留，不能作为最终验收：`iteration-60` 的受击体超出刀刃实际范围；`final-60` 的 Nailong 测试初始化错误；`final2-60` 的报告器直接序列化 Vector3 导致循环引用。这些属于测试夹具问题，已修正，不修改游戏伤害距离来迁就测试。

原生视频 `Blades-Aerial-Rush-Native.mp4` 和 `Native-Review.jpg` 位于对应 take 目录。视频按记录的模拟帧号保留每段速度，段间切换，不是实机性能录制。复用已有动作和 v278 下劈；已查看实际空中姿势、接触和冲刺恢复截图。本轮是玩法缺陷修复验收，不代表动作、美术或完整岛屿达到人类 FINAL/APPROVED 标准。

## 回退

改动前四个目标文件完整副本位于 `.codex/blades053/rollback/`。停止 Play 后，可恢复 `CombatBodyCollision.cs`、`SahurTwinBladeMotion.cs`、两份 controller 并让 Unity 重新导入。不要回退整个工作区：本次保留了先前的走路体力和飞机抓走/第二岛衔接修改。没有改写场景文件或原动画片段。
