# TungTung：战斗 VFX 首批升级与验证

2026-11-01 · task050 · Unity 6000.3.23f1 · URP 17.3.0

本轮升级已接入原有游戏代码，重点是材质接触、实体碎屑、地面尘烟和 Bombardino 爆炸。保留上一轮克制的灰白气痕与短促接触方向。当前成果经过第一岛原生画面检查；**没有据此宣称达到商业成品的最终标准**。

## 已整合的重点

| 层次 | 变化与接入 |
| --- | --- |
| 近战气痕与接触 | 增加沿真实空间变化的三维噪声。修复端点 `sin(PI)` 浮点误差进入分数幂产生 NaN 的问题，消除无效网格包围盒断言。保留 `CombatImpactVisuals`、`BossWeaponTrails` 对最终武器姿态和实际命中窗口的采样 |
| 金属、木材反馈 | 金属使用短促冷却的立体火星，木材使用受光照的有厚度碎屑。沿用 `CombatHitFeedback` 的材质分类与格挡结果，不改伤害、格挡或动画时间 |
| 碎屑与接地 | 有限的重力、阻力、碰撞、低弹性反射及停留；地面碎块落点沿实际表面取样，减少跨越高度突变的尘土网格。共用 6 组材质碎屑，每组至多 10 块；仅作表现，不添加可伤害碰撞体或施加物理力 |
| Boss 地面尘烟 | Capri 命中/硬直/突进演出与奶龙原有地面技能的通用入口改用非自发光三维密度体积。`BossCombatVfx.Burst` 保留原调用时刻，资源缺失时保留旧系统回退；专属声波和火焰喷流继续使用原表现 |
| Bombardino 爆炸 | 三维核心增加内部吸收，较冷外缘更早转为烟色；增加默认最高 1.5 像素热折射；叠加 Unity 官方 64 帧六方向光照烟体模拟。直接使用原来的 `BombardinoBlastFX.Initialize` 入口；辅助烟层约 1.4 秒退场，热体积约 1.05 秒退场 |

六方向烟层是模拟贴图驱动的浅曲面，随观察方向朝向相机；它是三维核心的辅助层，并非额外的三维流体模拟。四个方向截图未见代理方盒边缘或明显交叉片；快速连续绕拍及所有 Boss 完整交战仍需后续验收。

## 实际导入的资源

完整筛选表、准确链接及手动领取步骤见 [资源调研](VfxResourceReview-20261101.md)。本轮实际使用：

- Keijiro NoiseShader 的 MIT 授权 HLSL，仅导入 SimplexNoise3D、Common 与完整通知，固定来源 commit；已在本项目 URP Shader 中编译。
- Unity 官方 GroundExplosion A 两张六方向光照图。原始文件各为 4096²、8×8 帧；本项目导入 2048²、线性采样、Standalone BC7。运行时内存查询合计 **11,972,608 字节**。

六方向素材官网文档写 CC0，但实际下载的 `LICENCE.md` 写 Unity Companion / Unity Package Distribution License。本项目保留下载通知及两份条款，按 Unity 项目的集成使用限制处理，**不标为 CC0**。相关通知及 MIT 通知同时放进 `Assets/StreamingAssets/ThirdPartyNotices`，可随 Player 分发。

Asset Store 的 Cinematic Explosions FREE 与 Distortion Shockwaves VFX 均未领取、下载或导入，也未声称已在 Unity 6 实测。JangaFX 的 CC0 VDB 为候选，本轮未下载或导入。

## 原生验证

最终连续记录位于 `.codex/vfx050/final`：

- `TungTung-VFX-Native.mp4`：403 张原生帧，30 fps，13.43 秒；包含棍击、蓄力、重击、双刀、旋转、正式爆炸入口和 Capri 原生硬直尘烟调用。
- `result.json`：18 项检查通过；连续记录期间 Error / Exception / Assert 为 0。
- `.codex/vfx050/acceptance.json`：22 项专项检查通过，覆盖真实碎屑碰撞、6 组上限、每帧限流、不同材质的深度/颜色复制需求、拥有者清理、暂停、网格构造及四个 Shader。
- `.codex/vfx050/occlusion/result.json`：完整不透明遮挡墙前后画面逐通道差异为 0；比较关闭临时反射灯，避免把合法受光变化误判为穿透。
- `.codex/vfx050/angles-final`：四个方向原生检查，另有旧体积 Shader / 当前体积 / 混合层组件对照；不是另一段完整战斗录像。
- `.codex/vfx050/reload`：重载前后均为一个全局池、6 组碎屑、一个共享辅助烟网格、玩家 3 个武器/蓄力节点。`worldMeshes` 仅计当前绑定到 MeshFilter 的不同网格；重载后尚未再次绑定轻击网格时可少 1，不是全部分配量。

录制使用真实玩家预制体、原有攻击动画、虚拟手柄与带实际 `Health` 的训练柱，位于第一岛原有 Environment + Lighting 中。附近 AI 暂停，训练柱切换响应材质覆盖木材/金属分支。Capri 调用验证原生演出入口，**不能代替完整 Boss 交战验收**。测试对象不保存，没有另存展示场景。

固定模拟步长录制用于检查同步。`BrineVFX.BoundedWorld` 脚本更新采样 P95 为 **0.6004 ms**，不包含全部初始化/投影查询、场景渲染或 GPU 开销。未完成独立 Player 包的 GPU 与帧率测试，不能宣称整体帧率提升。

第一岛 Environment 打开时仍报告三组旧植物 Prefab 缺失（GUID `ad3d230ef428f6049ab82c5136f00a47`、`8e67ad946c4264b479267ee1643fd2ce`、`2bde5fdeb204bc6418608a5fe3993ad1`）。本轮未删除对象掩盖提示；连续记录的无错误结果不等于整个项目没有遗留场景问题。

## 配置与回退

主要入口：`Assets/Resources/Combat/BrineVfxStyle.asset`。`smokeDetailOpacity` 控制辅助烟层；`heatRefraction` 和 `heatRefractionPixels` 控制热折射。深度仅在需要时租用；颜色复制仅用于活跃热体积，随后恢复原相机选项。已修复将唯一 PC 档的索引 0 错当成低画质的逻辑。

源码及风格资源的原始副本在 `.codex/vfx050/rollback`，保留任务开始前已有的未提交更改，没有用仓库 HEAD 覆盖用户工作。关闭本项目 Unity 编辑器后，执行 `python Tools/RestoreBrineVfx050.py` 查看清单；加 `--apply` 执行。工具检查本轮文件哈希，遇到后续改动即拒绝覆盖，只恢复本轮备份和移除本轮新增文件。清单：`.codex/vfx050/installed-files.json`。

未提交、推送或发布构建。

测试后恢复第二岛原场景和实体键鼠，存档及其备份与任务开始前逐字节一致。恢复场景前，第一岛两个带脏标记的内存场景以副本保存到 `.codex/vfx050/workspace-recovery`；没有覆盖原场景文件。治疗包偏好恢复到本轮开始时的值。

两次早期改动前连续录制被同项目另一个任务的脚本重载中断，未作为有效前后对比。最终的 `angles-final/baseline-volume.png` 只比较同镜头、同时间的旧体积 Shader，不能冒充完整的旧版战斗录像。`hybrid-a` 是较早的有效迭代；交付以 `final` 为准。

交付复核：`.codex/vfx050/delivery.json`；最终实现/导入资源 44 个文件的哈希在 `.codex/vfx050/source-sha256.json`。恢复工作区后再次核对，验证过的实现与资源未发生变化。
