# Dark Brine_Rot 第一岛性能验证

## 测试条件

Windows 11，Intel Core i5-14400F，NVIDIA RTX 4060，Unity 6000.3.23f1，Windows 独立 Player。固定 Direct3D11、1920×1080、PC 画质、4× MSAA、渲染比例 1、阴影距离 60、LOD Bias 1.5。吞吐采样关闭垂直同步和限帧。测试产品名为 `DarkBrineRot_PerformanceQA`，测试构建和输出位于隔离项目。当前 Unity Play 保留。

独立诊断固定机位并放置玩家，运行正常的场景渲染与敌人 AI。它会写入临时战斗先决条件，因此仅用于性能测量。每段先等待场景稳定并验证相机实际渲染。FPS 以真实帧间隔均值换算，1% Low 为最慢 1% 帧的平均帧时倒数。P95 与首载耗时分别记录。记录帧数、窗口焦点及实际 Boss 交战比例。

## 实际修改

| 文件 | 修改 | 验证 |
| --- | --- | --- |
| `ProjectSettings/ProjectSettings.asset` | `meshDeformation` 从 CPU（0）改为 GPU Batched（2），通过 Unity 原生 PlayerSettings API 与原生序列化保存。 | 同路线 CPU/GPU 独立构建对照、截图，以及非 Development 构建的第一岛 13 项回归通过。磁盘差异恰为这一行。 |
| `Assets/Scripts/Performance/NaturalGrassScatter.cs` | 同一草地区块的多个实例批次共用距离、视锥和 LOD 距离计算。区块构建复用一个矩阵暂存列表。 | 实际草甸数据含 136,643 实例、524 个区块、1,581 个批次。72 个视角的剔除结果相同。五轮配对的剔除计算中位值由 0.0703 降至 0.0213 ms/视角。 |
| `Assets/Scripts/Performance/WorldPerformanceManager.cs` | 每个相机读一次位置与阴影开关。完全处于阴影范围的批次直接提交现有实例和风参数，边界批次逐实例沿用原判定。 | 相机自己的渲染回调仍负责提交。旧路线的固定草甸采样中，前后均为 1,454 个 Draw Calls 和约 821 万三角形，草可见性标记由 1.62 降至 1.44 ms。 |
| `Assets/Ocean/OceanWorld.cs` | 俯视跨越既有阈值时切换缓存海面网格。全量海水材质和天空更新留在真正的配置重建路径。 | Unity Play 中切换 120 次。两个网格 ID、边界、材质与固定时间水面高度稳定，已预热切换均值 0.00072 ms。 |
| `Assets/Game/Prefabs/UI/EquipmentPickup.cs` | 领域重载后重建道具光束的 `MaterialPropertyBlock`。 | 初始 Editor 采样重复出现 1,177 条光束空引用。修复后的采样为零。 |

草甸测试的 0.0703 和 0.0213 ms 仅计剔除计算。海面数字只计已预热的网格切换。两项均不可与整帧耗时相减来预测 FPS。阴影、材质、草地密度和 LOD 距离沿用现有值。Boss 死亡表现的独立验证见 [Boss 死亡动画](BossDeathPolish.md)。

## 前台实测结果

下面采用修正后的七段路线，前后均为 Development 构建。原版为 CPU 蒙皮，优化版包含上述代码和 GPU Batched。每段采集 7 秒，均有自动相机渲染回调、截图证据与全程窗口焦点。Cappuccino 与 Nailong 采样期间交战状态比例均为 100%。同一机位、PC 画质和 1080p 4× MSAA，主 Unity Editor 的 Play 同时保留。AI、风和海浪正常运行，因此每帧角色姿态与水面相位有差异。

| 场景 | 平均 FPS 前 → 后 | 1% Low 前 → 后 | P95 ms 前 → 后 | 主线程 ms 前 → 后 | GPU ms 前 → 后 |
| --- | --- | --- | --- | --- | --- |
| 普通探索 | 38.5 → 80.0 | 18.3 → 65.4 | 28.6 → 13.9 | 8.5 → 4.3 | 25.8 → 11.5 |
| 海岸 | 48.1 → 119.1 | 36.2 → 95.6 | 23.4 → 9.5 | 6.1 → 3.7 | 20.7 → 7.3 |
| 密集草甸 | 37.3 → 72.7 | 32.6 → 59.6 | 29.5 → 15.4 | 8.4 → 5.6 | 26.8 → 12.7 |
| Cappuccino | 39.8 → 81.4 | 30.3 → 65.3 | 27.9 → 14.0 | 8.3 → 5.0 | 25.0 → 11.2 |
| 鸡鸭鹅 | 16.9 → 59.3 | 15.4 → 51.7 | 61.4 → 18.4 | 11.9 → 5.4 | 59.1 → 14.8 |
| Nailong | 25.2 → 69.8 | 21.3 → 50.7 | 43.9 → 15.8 | 9.2 → 5.8 | 39.7 → 13.2 |
| Nailong 与密集现有特效 | 26.4 → 57.7 | 19.5 → 49.9 | 40.8 → 18.9 | 8.0 → 5.2 | 37.8 → 16.3 |

单独保留 CPU 蒙皮的代码优化版也已测量，平均 FPS 依次为 37.7、47.3、37.2、40.2、15.3、24.6、25.4。草地与海面代码修改的整机收益没有在这些短窗口中建立。主要可重复收益来自蒙皮设置，两轮较早 CPU/GPU 对照也呈现同方向的大幅改善。 [最终原始数据](../.codex/system-performance-20261107/final-foreground-comparison.json) · [只改变蒙皮的对照](../.codex/system-performance-20261107/final-skinning-comparison.json)

随后用 GPU 非 Development 构建重复同路线、同画质的前台采样。七段均完整完成，错误为零。它与上表优化后的 Development 结果接近，密集特效阶段仍低于平均 60 FPS。 [非 Development 原始数据](../.codex/system-performance-20261107/after-gpu-release/foreground/summary.json)

| 场景 | 平均 FPS | 1% Low | P95 ms | 主线程 ms | GPU ms |
| --- | --- | --- | --- | --- | --- |
| 普通探索 | 80.0 | 67.1 | 13.82 | 3.73 | 11.45 |
| 海岸 | 122.1 | 102.7 | 9.10 | 3.28 | 7.06 |
| 密集草甸 | 74.4 | 60.8 | 14.69 | 4.63 | 12.43 |
| Cappuccino | 80.9 | 66.4 | 13.62 | 4.17 | 11.28 |
| 鸡鸭鹅 | 59.9 | 50.9 | 18.20 | 4.39 | 14.65 |
| Nailong | 71.4 | 55.3 | 15.53 | 4.82 | 12.87 |
| Nailong 与密集现有特效 | 58.4 | 50.8 | 18.63 | 4.28 | 16.04 |

Unity 的 GPU Batched 蒙皮用计算着色器处理蒙皮，并在网格布局支持时合并多个网格的派发。该机 RTX 4060 与 DX11 支持计算着色器。此工程原来设置为 CPU，多个高面数角色同时存在时出现明显等待。结合对照结果，将蒙皮数据处理迁到 GPU 是本次收益最大的改动。具体到哪一条驱动同步导致等待，尚无底层 GPU 抓帧证据。其他显卡与后端仍需复测。 [Unity 蒙皮设置](https://docs.unity.com/en-us/engine/6000.0/script-reference/unityeditor/playersettings/meshdeformation) · [GPU Batched 适用条件](https://docs.unity.com/en-us/engine/6000.0/script-reference/unityeditor/meshdeformation/gpubatched)

![同机位截图，左侧原版，右侧优化版](../.codex/system-performance-20261107/final-visual-comparison.jpg)

已检查海岸、草甸、鸡鸭鹅、Cappuccino 与 Nailong 的渲染图，记录机位中未见材质丢失、蒙皮爆炸或植被消失。草甸的三角形提交数量仍约 821 万。波浪、风、动画与 AI 姿态按各自实际运行时间变化。逐像素同一动画时间的对齐、完整动作观感和所有 LOD 边界仍需进一步验收。

## 内存与加载

最终对照中 Unity 已分配内存由探索阶段的 1438.3 MiB 到特效阶段的 1443.6 MiB，优化后为 1445.3 到 1450.7 MiB。GPU 蒙皮版增加约 7 MiB 已分配内存，图形驱动内存增加约 23–41 MiB，符合额外蒙皮缓冲的开销方向。三角形与资源数量也会受 AI、特效生命周期影响，不能将全部差值归因于同一个缓冲。

首载到场景可采样的时间为 16.21 → 11.08 秒。前后文件与着色器缓存热度没有严格控制，因此尚不能认定加载优化收益。GC 计数器的正向控制在最终两次采样中均未通过，不据此宣称零 GC 或 GC 降低。内存快照、引用链泄漏分析和长时间内存趋势仍未验收。

## 构建与回归

修正路线的三份 Development 构建与一份 GPU 非 Development 构建全部成功，构建错误均为零，均报告 194 条构建警告。警告仍需逐项审查。最终前台性能采样的三个 Player 均记录零 Error 或 Exception。早期隐藏窗口曾出现 DX11 图形缓冲警告，最终三段路线的 Player 日志未记录该警告，这不能证明原间歇性闪退已经解决。

GPU 非 Development 构建通过现有 `-stability-first-island` 的 13 项检查。包括第一岛加载、七个 Boss 战斗阶段、玩家虚拟键鼠攻击，以及实际受击反馈。持续约六分钟，最大记录帧间隔 64.14 ms，正常阶段使用默认帧率策略。自动 QA 会调整先决进度、Boss 血量并放置玩家。完整从新存档通关、死亡继续、奖励和抓人结尾尚未验收。 [战斗回归结果](../.codex/system-performance-20261107/after-gpu-release/first-island/stability.json)

同一非 Development 构建另通过既有 `-stability-check` 的 17 项检查。三波真实鸡鸭鹅通过正常伤害入口清除并解锁 Nailong，随后第二岛 60 只以上棉花糖敌人的两分钟战斗采样完成，再依次重载第一岛和第二岛。捕获零 Error 或 Exception，最大帧间隔 65.56 ms。该旧入口自身使用 1280×720 RenderTexture，因此只计入功能与资源回收回归。第二岛采样已分配内存为 726.9–737.9 MiB，重载第一岛后为 1365.7–1404.9 MiB，再回第二岛后为 734.7 MiB，说明此序列能释放上一岛的大部分资源。采样不足以建立长期无泄漏结论。 [资源重载回归](../.codex/system-performance-20261107/after-gpu-release/reload-regression/stability.json)

项目 Test Runner 已发现零用例。已使用原生 Profiler 和 ProfilerRecorder。Memory Profiler、Profile Analyzer、Frame Debugger 的逐 Pass 调查尚未完成。90 分钟连续稳定性、三轮死亡与重载内存趋势，以及其他 GPU/API 的兼容性仍未验收。鸡鸭鹅与密集特效平均仍略低于 60 FPS，故“高画质全程稳定 60 FPS”目标仍未全部达到。

## 工作区保护与工具

原生设置保存前验证了五个第一岛场景均为 clean，并保留了当前 Play。正式产品名仍为 `Dark Brine_Rot`。GPU 设置前的 PlayerSettings 副本、应用收据与工程保护哈希均位于 `.codex/system-performance-20261107`。任何临时构建设置只作用于隔离快照。保留所有用户原有文件和备份。

最终核验第一岛五个场景、PC URP 资产、渲染器和画质设置与隔离快照一致。正式存档及其 .bak 的 SHA-256 与任务开始前一致。设置保存没有触发主工程脚本重载，现有 Editor Play 继续运行。 [保护核验](../.codex/system-performance-20261107/final-preservation.json) · [Editor 状态](../.codex/system-performance-20261107/final-editor-state.json)

本轮增加的诊断入口包括 `Tools/SystemPerformanceRoutes.cs`、`SystemPerformanceBuild.cs`、`BuildSystemPerformance.ps1`、`RunSystemPerformance.ps1`、`SummarizeSystemPerformance.py`、`AnalyzeSystemPerformanceProfile.cs`、`VerifySystemPerformanceKernels.cs` 和 `ApplyMeasuredSkinning.cs`。诊断路线只复制进隔离构建，以显式命令行开关启用。原生项目中的普通游戏启动继续使用原入口。最后一个工具只负责将已验证的蒙皮设置精确保存到原工程。

## 瓶颈定位

- 最初的自动渲染基线中，鸡鸭鹅的 GPU 帧时间 27.8 ms，主线程 9.2 ms。密集草甸分别为 20.4 和 8.5 ms。这两处的 GPU 等待值得优先查。
- 原生资产检查中，Nailong 的蒙皮网格有 499,752 个三角形，Chicken Bro 有 499,918，鸭有 499,984，鹅有 499,972。Cappuccino 的两个蒙皮网格合计 32,580。运行时三角形计数含多次渲染提交，不能用来计算模型数量或精确归因到单个渲染 Pass。
- PC 使用 Forward+、SRP Batcher 和 Render Graph。GPU Resident Drawer 与 GPU Occlusion Culling 保留原配置。自定义草地已有 GPU 实例化提交。海水在当前着色器读取场景深度和不透明颜色，渲染复制需要逐 Pass 确定使用范围。 [GPU Resident Drawer](https://docs.unity3d.com/cn/6000.0/Manual/urp/gpu-resident-drawer.html) · [Render Graph 优化](https://docs.unity3d.com/cn/6000.0/Manual/urp/render-graph-optimize.html)
- 旧的采样路线从鸡鸭鹅进入 Nailong 时残留上一段小怪，且截图未证明 Nailong 入镜。这两段旧数据仅保留为混合负载观察。已更新采样工具，在 Nailong 前回收测试小怪、调整镜头、记录战斗状态。隐藏窗口的手动相机结果及一次因运行配置变化中断的复跑也保留为诊断记录。

## Unity Profiler 原生记录

Unity 重新载入独立 CPU 蒙皮 Player 的原生抓取，读到 1,155 帧。主线程平均自耗时最高的标记是 `DXGI.WaitOnSwapChain`，为 23.24 ms。草地提交 0.50 ms、可见性 0.37 ms，PhysX 模拟 0.13 ms。渲染线程的 `Gfx.WaitForGfxCommandsFromMainThread` 平均 27.27 ms，`DrawOpaqueObjects` 含子项平均 2.91 ms。线程等待时间不可相加。此抓取含阶段预热、截图读回与 Profiler 自身开销，不能用作整帧 A/B。 [原始 Profiler 抓取](../.codex/system-performance-20261107/after/foreground-profile/report.raw) · [标记明细](../.codex/system-performance-20261107/after/foreground-profile/report.raw.analysis.json)

## 本机重跑

在工程根目录运行，使用新的测试标签。脚本仅使用隔离的测试构建，并保存原始帧、日志、内存采样和截图。

```powershell
pwsh -NoProfile -File Tools/RunSystemPerformance.ps1 -Variant before-route -Run local-retest -Seconds 7 -Foreground
pwsh -NoProfile -File Tools/RunSystemPerformance.ps1 -Variant after-route -Run local-retest -Seconds 7 -Foreground
pwsh -NoProfile -File Tools/RunSystemPerformance.ps1 -Variant after-gpu-route -Run local-retest -Seconds 7 -Foreground
python Tools/SummarizeSystemPerformance.py .codex/system-performance-20261107/before-route/local-retest .codex/system-performance-20261107/after-gpu-route/local-retest
```

命令中的三个构建可分别通过 `Tools/BuildSystemPerformance.ps1` 对应的 Variant 从现有隔离快照生成。已有成功构建和测试结果保持原位。加 `-Profile` 可另采 Unity Profiler 原始二进制。带 Profiler 的采样有额外开销，独立于 FPS 对照。
