# 第一岛环境统一与系统整理：实际交付

## 本轮实施

保留写实海岛与荒诞角色之间的反差。本轮沿用现有资产，先处理材质色响应与重复模板这两项最有证据的问题，没有增加装饰数量、替换角色或改动战斗。实际修改通过 Unity 原生 API 保存材质、Prefab 实例 Renderer 覆盖与 Environment 场景。

| 修改 | 涉及文件 | 解决的问题 |
|---|---|---|
| 树皮与草地的色相处理分开 | `Assets/Shaders/MavisFoliageWind.shader`，`Island_Trunk / Island_Branches / Small_Bark / Small_Branches.mat` | 原 shader 对树皮同样执行草地绿色重映射和阳光去色，使树干偏青灰。增加默认保持旧行为的可调参数，树皮单独禁用；保留原贴图、根部延伸与风变形 |
| 草、树叶和草甸小幅校色；收敛旧草风强度 | 下表九份实际共享材质 | 保留自然的物种色差，降低旧草与地面的色温差异；草甸不再使用完全相同的白色乘色。未盲改植物/石头的所有粗糙度和法线参数 |
| 停用已有草甸覆盖区的重复大模板 | `Assets/Scenes/First Island/Environment.unity` | 两轮分别停用 473 个 Grass_Stems 大模板和 481 个 Forest001 / Grass_12 模板 Renderer，减少矩形轮廓、偏青密集块和重复受光。条件为大型模板且包围盒已有至少 12 个现有草甸实例；其余分别 154 / 146 个保留，避免稀疏地区失去覆盖。仅改 Renderer.enabled，原层级、变换、网格、碰撞和资产都保留 |
| 明确生态数据选择 | `Tools/BakeIslandRealism.cs` | 原泛型 Single 查找在现有两套 IslandEcologyRenderer 下会失败；现在按基础生态输出资产路径选择，仍兼容旧 `IslandEcology074/Ecology.asset` 输入。只编译并执行 Validate，未运行会覆盖摆放的全岛 Bake |
| 维护面板列出实际两套系统 | `Assets/Editor/IslandHabitatReview.cs` | 避免 FirstOrDefault 随枚举顺序选错输出。分别列出两份命名数据，增加标记编辑入口，两套生态都显示检查标记 |
| 显式烘焙同步固体碰撞 | `Assets/Scripts/Performance/IslandEcologyCollisionBaker.cs`、`IslandDecorationLayout.cs`、`Tools/BakeIslandRealism.cs` | 原先移动标记并 Bake 后，199 个实体装饰碰撞中的烘焙代理会停留在旧位置。现在两条 bake 入口同步更新其各自所有的静态代理，处理移位、移除、重复烘焙与 Undo；保留树碰撞和用户其他碰撞，不参与每帧更新或视觉 LOD |

没有删除、迁移、重命名资产，没有修改敌人类型、Boss 顺序、路线、检查点、角色动画或战斗代码。本轮未修改 WorldPerformanceManager / GrassRenderSubmission 的既有渲染提交与内存修复。没有合并职责不同的两套生态数据。

## 材质原值与更新值

前八份材质位于 `Assets/Game/Prefabs/Environment/Shared/Foliage/Materials/`；草甸材质位于 `Assets/Generated/Island1Realism/`。原值来自修改前原生审计与材料备份，而非 Git HEAD，因为工作区原本已有未提交修改。

| 材质 | BaseColor RGB：原→新 | WindBend：原→新 | 新草色参数 / 阳光去色 |
|---|---|---|---|
| Grass_12 | (1,1,1) → (.94,.92,.78) | .55 → .40 | 1.25 / .55 |
| Grass_Stems | (1,1,1) → (.90,.85,.58) | .55 → .36 | 1.25 / .55 |
| Island_Trunk | (1,1,1) → (.95,.88,.78) | .40 保持 | 0 / 0 |
| Island_Branches | (1,1,1) → (.95,.88,.78) | .40 保持 | 0 / 0 |
| Small_Bark | (1,1,1) → (.95,.88,.78) | .40 保持 | 0 / 0 |
| Small_Branches | (1,1,1) → (.95,.88,.78) | .40 保持 | 0 / 0 |
| Island_Leaves | (1,1,1) → (.91,.94,.79) | .80 保持 | .55 / .20 |
| Small_Leaves | (1,1,1) → (.91,.94,.79) | .80 保持 | .55 / .20 |
| Grass - meadow, fescue and sedge | (1,1,1) → (.96,.95,.87) | .63 → .48 | 1 / .35 |

原 shader 草色混合系数固定 .82、太阳去色固定 .55；新 `_GrassTintStrength` 默认 1，`_SunDesaturation` 默认 .55，未配置的材料仍保持旧行为。草色 0 用于保留树皮原色，1.25 允许旧草充分脱离原始蓝绿色。草甸保留 `_AuthoredGrass=1` 的网格顶点色。BakeIslandRealism 的草甸材质生成值同步更新，避免后续 bake 冲掉校准。近/远网格仍使用同一共享材质，所有 LOD 阈值、贴图分辨率、实例位置与缩放保持原值。

## 渲染对比

Unity 6000.3.23f1 / URP / Windows / NVIDIA RTX 4060，PC 画质，1920×1080，4× MSAA，后处理开启。固定游戏时间 09:00、云覆盖 .64、云速 0；对比用风暂停，机位位置与旋转均写入 JSON。海水仍使用编辑器时间波动，因此海岸对比不承诺波峰逐像素相同。摄像机在实际地面上方约 1.4–2m，使用原生 SRP 摄像机完成渲染后读取图像。

- [全部六类机位，左前右后](Island1EnvironmentReview/all-viewpoints.jpg)
- [海岸](Island1EnvironmentReview/coast-comparison.jpg)、[沙丘草地](Island1EnvironmentReview/dune-comparison.jpg)、[草坡](Island1EnvironmentReview/meadow-comparison.jpg)
- [林地树皮](Island1EnvironmentReview/woodland-comparison.jpg)、[Boss 路口](Island1EnvironmentReview/boss-junction-comparison.jpg)、[近地细节](Island1EnvironmentReview/ground-comparison.jpg)
- [6 / 31 / 40 / 49 / 74 / 88 / 101m 移动机位及 LOD](Island1EnvironmentReview/lod-review.jpg)

实机图中树皮由青灰回到棕灰，Boss 路口中心的大型青色重复模板消除。近地草甸色温更统一。仍保留的旧模板里，个别细长草仍偏青，草叶几何也依然较粗，不能把本轮描述成已达到 AAA 或全岛所有颜色、尺度与接触问题都已消除。岩石现有 PBR、地形混合、根部延伸及稀有植物未重做；没有证据的粗糙度/法线问题留在当前状态。

## 验证结果

- 进行了两次完整 Editor 关闭/重开，以及场景关闭/加载、重复进退 Play。最终 136,643 个草甸实例、基础生态 2,724、局部生态 738、199 个实体装饰碰撞均保留。最终六个机位回归无环境错误、无缺材质；普通草实例源 Renderer 重复渲染数为 0。
- 另以正常风场启用状态渲染六个机位并检查近地草与林地，未见新增根部脱离或异常整株位移；风场回归窗口无错误。完整摄像机测量参数在 wind-review.json 中以 windActive=true 区分。
- 当前全岛验证器编译执行通过：430,120 个草根样本，悬空 0、缺地面支持 0、基础生态路线侵入 0、缺材质 0。局部构图 738 个实例、29 组全部通过地面、路线、cell bounds 与 shader 检查；最大根锚点误差约 0.00002m。完整根网格审计不是对整个岛每一根旧草的全覆盖声明。
- 两份生态数据的碰撞生成回归分别通过 145 / 40 个代理：重复 bake 不增殖、移动同步、移除物种不遗留代理、保留自定义和树碰撞、Undo 清理通过。测试使用临时场景和数据副本，没有重新烘焙用户已有摆放数据。
- 当前草近/中/远网格分别保持每变体 180 / 63 / 20 顶点；已渲染检查过渡区。实例数与根矩阵不改变，没有发现整块消失；细节减少仍可观察到，未提高远景几何开销来掩盖它。
- 未渲染摄像机 30 秒回归通过：反复调用旧草提交接口和 scatter.Submit 未造成队列积累，原生 private memory 变化约 -242MiB，恢复摄像机后草甸正常。此结果证明短回归未重现增长，不是长时间无泄漏承诺。
- Unity Test Runner 发现 0 个可发现的测试用例；执行了上述项目原生验证器、自定义碰撞回归、shader 与渲染检查，没有把“0 个测试”写成完整测试套件通过。
- Editor 日志仍有启动/退出阶段的 Graphics Ring Buffer 空间警告、Mono 内存脚本工具路径 assertion，以及隔离场景无摄像机时的 AudioListener 提示。渲染测量窗口的 Error/Exception/Assert 为 0；本轮未宣称解决原有全部闪退风险，也未扩大图形 ring buffer 掩盖警告。
- 场景 diff 的所有原有 Mesh payload 哈希一致；变更限于 954 个 Renderer 的 Prefab 覆盖，另有原生保存重新分配的网格引用 ID。Lighting / Gameplay / Enemies / Main 和存档在交付恢复时做字节检查。
- 最终恢复原五场景工作区，Main 激活，Play 已停止，五场景均 clean、测试摄像机为 0。存档与备份逐字节一致，上述四份其他场景与任务开始快照哈希一致；树根原有 _RootSeatDepth=.14 保留。本轮文件的 git diff --check 通过，未去修改原有 Lighting 中的空白差异。

## 性能与限制

每机位预热 100 帧、采样 180 帧；画面和计数来自 Environment + Lighting 的 Editor Play，非独立构建。原始数据见 [measurements.json](Island1EnvironmentReview/measurements.json)。修改前后进行了 Editor 重开，缓存状态与后台负载存在差异，因此以下只作当次测量，不把全部差值归因于本轮修改。

| 机位 | 平均帧时间 前→后 | P95 前→后 |
|---|---|---|
| 海岸 | 6.06 → 5.72ms | 7.38 → 6.40ms |
| 沙丘草地 | 13.05 → 9.95ms | 15.31 → 11.44ms |
| 草坡 | 13.40 → 10.23ms | 16.14 → 12.28ms |
| 林地 | 12.30 → 11.31ms | 14.66 → 14.76ms |
| Boss 路口 | 12.01 → 10.35ms | 14.33 → 12.55ms |
| 近地 | 9.91 → 8.36ms | 12.06 → 10.38ms |

最终 Unity allocated 约 2,322–2,332MiB；Editor 原生 private 约 7,857–7,944MiB，working set 约 5.6GiB，包含编辑器、CPU/GPU 资源和缓存。基线托管 Process 内存 API 返回 0，故未使用那组 0 作内存改善证据；最终改用 Windows 原生计数。独立构建 GPU 帧时间、显存峰值和长时间压力测试未完成，不能据此保证玩家配置上的 FPS 或闪退已全部修复。

## 维护及独立问题

使用 [第一岛环境系统与资源维护说明](Island1EnvironmentMaintenance-20261107.md)。云条纹和装饰碰撞上一轮修复仍保留，本轮未重新改云，补齐了碰撞烘焙同步。云采样噪声及原有 Editor 图形警告仍是独立后续事项。5 小时后的续作定时已设置；当前可完成的工作没有刻意留给定时任务。

证据根目录：`.codex/environment-unification-20261107/`。基线、过程图、原材质备份、场景快照、依赖图和测试 JSON 均保留。
