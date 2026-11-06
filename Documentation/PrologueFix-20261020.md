# 序章流程阻断修复 — 2026-10-20

## 结果

当前工程已从主菜单 New Game 正常运行至第一岛上岸并恢复控制。正常流程不需要使用新增的故障跳转入口。异常路径另通过 19 项回归检查。

## 修复内容

- 从 `ArtSource/Encounter/v050/CH1_SahurGuard_v050.fbx` 恢复实际防御动画，导入到 Resources。使用该文件自身的有效 Humanoid Avatar，60 FPS、1.6 秒循环、锁定根运动；不修改现有玩家模型或 Avatar。
- 缺失的 `CH1_SahurBrace_v047` 使用已保留的 `CH1_SahurBrace_v027`；缺失的 `CH1_SahurMiss_v040` 使用已保留的 `CH1_SahurSlash_v128`。保留原有防御/挥空动作语义、时间线和实际接触判定，日志明确记录兼容选择。
- 在禁用玩家控制器和交出动画控制权之前，校验实际时间线、游泳、攀爬与跳跃等所需资源。兼容选择与运行时加载共用同一解析逻辑。
- 捕获初始化异常，清理部分创建的动画图、追逐组件和后处理，恢复碰撞体、动画参数、角色位置、移动、战斗组件与控制权。清理支持重复调用。
- 为结尾的异步准备和播放设置 120 秒游戏时间上限；超时同样回到恢复状态，暂停期间不计时。
- 故障时停止鲨鱼攻击并保护角色，显示 `R: retry ending / Enter: continue to shore`。玩家仍可移动；R 重试实际结尾，Enter 明确选择加载第一岛。目的场景不可用时保留恢复状态和重试提示。

没有给缺失的新版鲨鱼动作伪造资源或改名替代。六项新版演出资源仍不在当前副本中，原有资源检查会选择可玩的兼容遭遇。本次确保该路径完整可用；不宣称新版整套编排已恢复。

## 验证

Unity 6000.3.23f1，Editor Play Mode。

1. 编译通过，0 编译错误。
2. 从主菜单真实 New Game 入口开始，不强制推进剧情；依次记录 Waiting、Breaking、Fighting、Finale、Defeat、Finished，最终自动进入第一岛 Gameplay。约 98.4 秒后已上岸醒来、角色 enabled=true、ExternalControlLock=false、生命 100。正常流程范围内没有运行错误。
3. `Tools/VerifyPrologueRecovery.eval.cs` 在单独的 Story1 Play 会话中执行故障注入，19 项检查通过，包括：
   - 运行时替换一个时间线名称，确认缺失资源被预检发现，并恢复移动/战斗/碰撞体与控制权。
   - 通过真实前进输入确认角色恢复移动。
   - 通过 Input System 的 R 按键启动实际结尾。
   - 模拟准备超时，确认部分初始化的结尾被释放。
   - 模拟初始化中的空引用，确认捕获异常并再次恢复控制。
   - 通过 Input System 的 Enter 按键进入第一岛，等待上岸结束并确认交还控制。
   - 所有测试中的时间线和临时引用均在 finally 中恢复，没有写回资源。
4. 观察并核对故障提示截图；原存档及 .bak 恢复后 SHA-256 均与测试前一致。
5. 已停止 Play Mode，恢复 Environment、Gameplay、Lighting、Enemies、Main；未保存或改写这些场景。

正常流程验证与故障注入验证分开进行。故障测试会产生预期的恢复警告；这些警告不是正常流程发生的异常。

证据保存在本地 `Temp/PrologueFix-20261020/`：`normal-route-final.json`、`recovery-regression.json`、`final-island-arrival.png`、`recovery-prompt.png` 和 `restore-verification.json`。同目录的存档备份不属于发布内容。

本次未重新构建独立可执行版本，未对其他 Boss 或战斗平衡作变更。
