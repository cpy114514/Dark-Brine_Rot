# 凌晨 2 点续做交接（2026-10-07 America/Vancouver）

**交接已执行完成。以下是 2 点之前的历史状态，当前成果、修订、验证和录像以 `final-review.md` 为准。**

用户因 GPT 用量不足中断本轮，要求定时凌晨 2 点继续，重点打磨角色动作，而不只是运镜。用户已批准整段重做，并明确不要再问问题；打斗完成后，自主重设计并制作鲨鱼甩尾击碎船。不要只交付计划，完成实际制作、集成和视觉评审。此文件记录当前状态，不代表工作已完成。

## 已完成的新内容

- `ArtSource/Encounter/v001/shark-encounter-v001.blend`：保留原骨架、网格和旧 7 Actions，新增 60 FPS 咬击、尾扫、受击；身体到尾端分段延迟发力，受击一次衰减。
- `ArtSource/Encounter/v001/sahur-encounter-v001.blend`：用户指定 StandingMeleeAttackDownward 源动作，重定时为 1.6 秒，接触 .9 秒；左手取初始姿态加平衡摆动。尚未实际视觉验收，可能仍需较大修订。
- 同目录单 Action FBX、元数据和数值验证报告。最初冻结 Hips 两轴未通过验证，修订冻结完整根位移后已通过；腿部接地仍需实际检查。
- Unity 隔离导入验证通过：单 take、60 FPS、时长正确；Sahur 复制同源 Mixamo Avatar，有效 Humanoid；鲨鱼有效 Generic。旧/新鲨鱼骨架、单位与静态骨骼变换一致。
- 已用 native Editor 安装到 `Assets/Resources/Encounter/v001/`。克隆 `EncounterShark.controller` / `EncounterShark.prefab`，新咬击/尾扫/受击替换克隆控制器同名状态；原控制器、原 FBX、原 rig prefab 保留。`Assets/Resources/SharkAnimation/TralaleroAnimations.asset` 已指向新版 prefab，更新 tailStrikeContact。`OriginalAnimationSet.asset` 是安装前快照。
- 新 Sahur 斜劈已经导入但**尚未接入 finale**。新船尾击尚未制作或安装。

## 当前未完成的工作，优先顺序

1. 重写/修订 `Assets/Scripts/Story/Chapter1/Story1WreckFinale.cs`。本轮尚未改此文件，它仍是旧版本（大量旧跳帧/瞬移）：`EvaluateHeroPose(contactPhase)` 在命中帧直接跳姿势；LateUpdate 将整个鲨鱼瞬移到 WeaponTip/Tail 接触点，然后才计算接触距离，指标失真。删除这些视觉跳变，使用连续共享时间轴和预先规划的轨迹，检测实际接触距离，不能通过先瞬移再统计零误差来通过测试。
2. 按批准 `design.md`：14.4 秒主分镜（游近/爬板另计），势均力敌 .6–3.0，Sahur 上风 3.0–7.2（单手斜劈 4.0、投棍4.7/命中5.5/接回6.6），逆转7.2–10.8（假咬转尾扫9.2），重尾击11.6/丢棍11.7/落水败北至14.4。根据实际动作校准可以自主调整。用现有动作做合适部分，新斜劈接入，必要时制作更多角色动作。骨盆—躯干—右肩—手的发力、支撑脚、接触、受击反应和跌落重心需要实质打磨。
3. 保持正常操作阶段随机20–35秒或 Sahur <=20%血量进入自动打斗。当前代码未使用鲨鱼30%血量，保留当前条件。保持蓄力可移动/跑步/换方向、跳木板/游泳/爬板/冲浪移动、无新增音效、写实低特效。
4. 原 `Tools/VerifyStory1WreckFinale.cs` 断言英雄4次/鲨鱼3次来自旧分镜，需按实际新动作数量更新有意义的测试，同时真实验证接触/暂停/时间与残血触发/落败/无复活UI/昏迷丢棍上岛。捕获真实游戏画面、看角色动作和支撑，修订后再评审。
5. 完成打斗后，自主设计新的甩尾碎船，再制作 Blender Action（可复用 builder 的 ship 模式但必须经过视觉检查和修订，不能直接当成最终）。需要身体水下蓄力、尾端加速真实碰船、船倾斜受力后解体、碎片受初速度和重力自由落海/浮平。调整 `Story1WreckBattle.BreakShip` 现有 .65s接触时瞬移校正，避免先跳位再零误差。镜头跟随因果，但不能仅靠镜头/震动/粒子代替动作。
6. 船实物碎片保留202个可见碎片、20块物理可站甲板，保留原模型的拆分和物理浮平/重力/自由散落，不预排目的布局。检查真实帧和接触，鲨鱼大部分身体保持水下。

## 工具与恢复

- Blender exe：`C:/Users/pinyu/.codex/tools/blender-workflow/portable/blender-5.2.2-windows-x64/blender.exe`。
- `Tools/AnimationPipeline/blender/build_encounter_revision.py` 目前支持 shark/hero/ship。**新 FBX 导出拒绝覆盖**。修订请选择新版本路径/名字，保留旧 source。`finish` 保存 working blend；不要运行旧 `Tools/BlenderSharkAnimation.py`，它重建并覆盖原 rig/source。
- 脚本可 headless Blender 运行，原主文件不变。用户的 hidden MCP Blender 127.0.0.1:9876 是 factory connectivity session。当前 tool catalog 无 Blender tools；可用 KIT venv Python 的 MCP SDK 调 `blender_execute_script`，或本轮已用的 headless Blender。原主 Blender 来源不是 active Sahur，不要误用 `BlenderWork/character_fixed.blend`。
- 已读并应用 action-designer/combat-animation/boss-animation/animation-review/unity-animation-pipeline、Blender router/actions-fcurves/python-api、Unity pipeline。恢复后查看相关 skills 以遵守制作/评审流程；用户已批准，无需再索取阶段审批。
- Unity MCP：6000.3.23f1，最后状态 ready/stopped/MainMenu；autotick已启用16ms。恢复先查 editor_status。脚本通过 mcp__unity recompile/recompile_status，native场景/资产操作通过 run_script，不手改 Unity YAML。`Tools/AnimationPipeline/UnityAnimationImport.cs`新增 StageEncounter；`InstallEncounterRevision.cs`安装版C#在Assets外，可临时运行。
- `Tools/AnimationPipeline/blender/render_animation_preview.py` 多视角预览，默认只rig/skinnedmesh，武器接触必须真Unity画面。不要编造视觉评分，未观察项明确记录。
- 本轮开始保存备份到 `.codex/encounter-v001/backups/`：Finale/Battle/TralaleroSwimAnimator现有dirty版本、source-hashes.json、save.json。原存档路径 `C:/Users/pinyu/AppData/LocalLow/DefaultCompany/Dark Brine_Rot/dark-brine-rot-save.json` SHA256 `8D8664A0E3EEA197CD5FA6D48D998783932DB75077DE62CEC62AC628DC725B57`。测试必须备份**恢复时当前存档**，结束stop Editor后字节恢复，不覆盖用户之后的新存档。
- 用户已有其他dirty修改，不能 reset/revert 全仓库；本轮没有commit/push请求。仅完成动画、集成、评审并报告。
- 本轮 `source-review.md` 仅 BLOCKING 进入集成的技术接受，**不表示最终视觉验收**。完成后记录真实 review/测试证据，标 FINAL；用户已免后续询问，不虚构额外用户批准。

最后一项成功操作：运行 InstallEncounterRevision.Install(false)；控制器与新版鲨鱼安装成功，接触 local `(0.06973, 0.11482, -0.07518)`。未进入 Play Mode，未开始 runtime 打斗验证。
