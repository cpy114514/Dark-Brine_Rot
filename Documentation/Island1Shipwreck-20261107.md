# 第一岛：复用开场沉船残骸

后续重做已保存到同一 Prefab。下面保留首次摆放的证据，现行构图、数量和路线验证见 [沉船重做记录](../.codex/island-shipwreck-redesign-20261107/review.md)。

## 实际修改

已在第一岛出生点东侧外海岸建立独立沉船地标，主要船首约在世界坐标 `(378, 4.28, 395)`，碎片沿约 85m 海岸散布。这里只增加静态环境摆放，没有修改开场、角色、战斗、拾取物、检查点、Boss 或主路线。

| 文件 | 用途 |
|---|---|
| `Assets/Generated/Island1Shipwreck/Sahur opening wreck - washed ashore.prefab` | 新增独立 Prefab，直接引用开场原船网格与材质；65 个渲染物体，49 个原船构件/碎片实例，加 16 个既有海岸岩石/海藻实例。总计 21,700 三角形、10 份共享材质、18 个静态碰撞；0 Rigidbody，0 运行时脚本 |
| `Assets/Scenes/First Island/Environment.unity` | 新增上述 Prefab 实例，Hierarchy 名称 `Island 1 - Sahur's washed-ashore shipwreck`。原场景物体和摆放保持不变 |
| `Tools/IslandShipwreckPass.cs` | 本次原生编辑器审计、选址、摆放、Prefab 保存、来源核验、路线与碰撞检查工具，位于 Assets 外，不进入玩家代码。构建方法有防覆盖保护；日常编辑使用 Prefab/实例覆盖，Validate 可单独运行 |
| `Documentation/Island1EnvironmentMaintenance-20261107.md` | 补充此静态层与现有生态烘焙系统的职责边界 |
| `Documentation/Island1ShipwreckReview/` | 五类机位前后对比、最终图、开场验证截图与测量数据 |

主船首保留原船的船锚、栏杆、甲板和弯曲船壳，使用原来的拼接关系，整体倾斜并将下部埋入沙地/浅水。附近有脱落的船壳、甲板、木制细节和潮线碎片簇，搭配原有礁石、碎石、漂木与海藻。干湿变化来自现有海岸湿沙、潮线和局部入水高度，没有改色原船材质，也没有新增贴图或全局特效。

首轮实机检查发现两类问题，已小步修正：散落甲板过长且翘离沙面，改用更小的原始碎片并分簇贴地；一块船壳在北侧视角呈现竖直大矩形，改用原甲板碎片并调整邻近船壳埋深。过程截图保留在 `.codex/island-shipwreck-20261107/first-review`、`second-review`、`third-review`。

## 资产来源与故事一致性

- 完整原船：`Assets/Game/Prefabs/Environment/Props/Ship/ship_g.fbx`，Story1 场景的 `ship_g` 对象由 ShipSailingMotion 和 Story1SharkCollisionSequence 使用。
- 实际开场碎片库：`Assets/Resources/Story1Wreck/ShipWreckPieces.asset`，类型 Story1ShipWreckAsset。它保存 20 个甲板片、24 个船壳片、158 个细节片，及部分片段的子碎片。网格来自现有 Blender 切割结果，保留原 UV、原材质和 sourceSize/sourceCenter/floatRotation。
- 开场的 Story1WreckBattle 在撞击时按这些数据生成带浮力/刚体/交互的临时对象，并按撞击位置进一步释放部分子碎片。本次自然播放测得初始预期和实际均为 214 个运行时碎片，因此运行时数量不必等于库中 202 个顶层片段。
- 岛上直接复用上述网格、子碎片和 `Props/Ship/Materials/acmat_*.mat` 共享材质，采用开场船体 `.lossyScale * .01 = 2` 的构件尺度。没有重新建模整艘船，也没有复制开场的运动、战斗或浮力组件。
- 岛上原先少量 `Island1Decoration` 船木仍保留；新地标使用原船材质，不替换那些旧构图数据。

开场场景、原船、碎片库及其依赖的文件哈希在修改前记录，交付时核对；没有保存或修改 Story1。源列表和哈希见 `.codex/island-shipwreck-20261107/fragments.json`、`opening-source-hashes.json`。

## 实机画面

固定 09:00、云覆盖 .64、云速 0、风暂停，PC 画质、1920×1080、4× MSAA、后处理开启。同一次 Editor Play 中保持机位和画质，切换新地标可见性后逐对保存修改前/后。海水继续使用实际时间，所以波峰形态不逐像素一致。

- [全部五组对比，左前右后](Island1ShipwreckReview/all-viewpoints.jpg)
- [接近地标](Island1ShipwreckReview/approach-comparison.jpg)、[近岸主景](Island1ShipwreckReview/shoreline-comparison.jpg)、[船首近景](Island1ShipwreckReview/bow-comparison.jpg)
- [沙地碎片与接触](Island1ShipwreckReview/debris-comparison.jpg)、[沿海岸回望](Island1ShipwreckReview/along-coast-comparison.jpg)
- [最终主景](Island1ShipwreckReview/shipwreck-final.jpg)

## 验证结果

- 来源核验通过：49 个船构件/碎片实例都直接引用原碎片库内的网格与原船材质，无缺网格、缺材质或 shader 编译错误。Prefab 重新加载、Environment 重开和进退 Play 后保留。
- 18 个碰撞查询全部通过：大型弯曲构件使用原生凸包碰撞，较大散片使用简化 BoxCollider；小碎片/海藻无碰撞。没有添加 Rigidbody 或每帧生成逻辑。
- 3,333 个保护路线、清场区和海岸通道胶囊查询中，没有新增沉船碰撞阻挡。另用高度 4.8m、半径 .65m 的 CharacterController，在三条平行海岸通道从 z=347 走到 z=467，均到达终点，接触新增沉船障碍次数为 0。这是通行夹具检查，不冒充完整真人游玩。
- 原 Story1 自然播放通过 Waiting → Breaking → Fighting/教程交接。观察到原船隐藏、214 个预期/实际碎片、正确的移动教程门控、有效摄像机和角色引用，没有可渲染网格缺材质或 Error/Exception/Assert。保留了开场航行、撞击和残骸交接截图。
- 初次测试曾把教程有意保留的控制锁，以及 154 个没有网格的 `Cloud Entity` 空标记误判为故障。已检查当前开场实现并修正验证范围，原脚本和空标记均未改动；首次失败记录保留，最终结果见 `opening-review.json` 与空标记诊断 JSON。
- 本次没有进行完整教程、鲨鱼决战终幕、转场到第一岛的端到端输入测试；只验证了原开场自然推进至残骸教程交接，不能据此声称整个序章全流程已通关。
- 原地形、海面及地面碰撞网格的序列化 payload 哈希保持一致；Environment 原有对象只有原生保存造成的网格引用 ID 更新与 SceneRoots 新增实例入口。其他场景、开场源资产、存档及备份在恢复时检查。
- 最终复核通过：420 个开场场景/源资产依赖文件均未改变，Main / Lighting / Gameplay / Enemies 与开始快照一致，存档及备份逐字节一致。原五场景工作区已恢复，Main 激活、Play 停止、所有场景 clean，新 Prefab 保持启用；136,643 个草甸实例、2,724 / 738 个生态实例及原 199 个装饰碰撞均保留。

## RTX 4060 编辑器测量

Unity 6000.3.23f1 / URP / NVIDIA RTX 4060；Environment + Lighting 的 Editor Play，1920×1080、4× MSAA，每组 100 帧预热、180 帧采样。原始数据：[measurements.json](Island1ShipwreckReview/measurements.json)。

| 机位 | 平均帧时间 前→后 | P95 前→后 | Draw calls 前→后 |
|---|---|---|---|
| 接近 | 6.14 → 5.73ms | 6.88 → 6.18ms | 78 → 205 |
| 近岸主景 | 6.65 → 5.67ms | 7.50 → 6.09ms | 68 → 203 |
| 船首 | 6.71 → 5.05ms | 7.17 → 5.44ms | 56 → 185 |
| 碎片带 | 3.75 → 4.01ms | 4.31 → 4.72ms | 51 → 171 |
| 沿岸 | 6.45 → 6.53ms | 6.88 → 8.38ms | 105 → 255 |

新增模型和阴影确实增加提交次数。部分机位帧时间下降同时受到船体遮挡高开销海面像素和测量波动影响，不能称为整体性能优化。总三角形量约 2.17 万，材质复用且物体设为静态；未增加常驻模拟。该测试窗口 Unity allocated 约 2.4GiB，未作独立构建显存或长期内存基准，不能保证其他硬件上的 FPS。

## 后续维护与独立问题

选择 Environment 中的新地标根对象编辑实例覆盖，或打开新 Prefab。它独立于两套 IslandEcologyRenderer 数据、EditorOnly 摆放标记和 IslandEcologyCollisionBaker，生态 Bake 不负责修改它的分布或 18 个碰撞。调整大型位置后重新运行 `IslandShipwreckPass.Validate`，检查出生、武器拾取、检查点、海岸通道和碰撞，并渲染同机位比较。

本轮没有修改天空、云、Bloom、草地提交或内存稳定性代码。先前 199 个装饰实体碰撞保留，新地标另有 18 个。云采样噪声/条纹复查及既有 Editor Graphics Ring Buffer 警告仍是独立事项，未将本次沉船摆放当作它们的修复。

完整证据：`.codex/island-shipwreck-20261107/`。没有删除旧场景、历史证据或用户文件。
