# 第一岛环境系统与资源维护

核查日期：2026-11-07。入口为 `Assets/Scenes/First Island/Main.unity`，由现有 additive bootstrap 装入 Environment / Lighting / Gameplay / Enemies。环境改动只保存 Environment，光照只保存 Lighting。遵守根目录 COLLABORATION.md。

## 当前职责和入口

| 系统 | 当前职责与实际资产 | 应当怎样修改 |
|---|---|---|
| ProceduralIsland / IslandBlendedTerrain | Environment 的 Sunset Island。原生 Mesh 地形、地面碰撞、海岸泡沫；habitatMap 的 RGBA 分别表示 turf / wear / stone / damp。并非 Unity Terrain 草细节 | 地形参数在 ProceduralIsland；材质在实际 Renderer。重新生成地形后必须重新检查所有植被根部与路线 |
| NaturalGrassScatter | `Natural Meadow Scatter 068` 当前引用 `Assets/Generated/Island1Realism/Meadow Scatter.asset`，136,643 个世界空间草簇；近/中/远共享网格及一个草甸材质，不创建每草簇 GameObject | 分布输入、habitat 和参数由 `Tools/BakeIslandRealism.cs`、`Tools/Island1RealismSettings.json` 管理；只改颜色/风时直接改数据所引用的材质，不重烘焙全岛 |
| IslandEcologyRenderer：基础生态 | `Island 1 - Coastal woodland ecology` 引用 `Assets/Generated/Island1Realism/Woodland and coastal dressing.asset`，2,724 个实例，含自然植物、岩石、枯枝与稀有植物 | 全岛重烘焙使用 BakeIslandRealism.Bake；它会覆盖该输出，先备份并确认编辑意图。近/远网格、材质和距离在该数据各 species 中 |
| IslandEcologyRenderer：局部构图 | `Island 1 - habitat detail compositions` 引用 `Assets/Generated/Island1Decoration/Baked compositions.asset`，738 个实例 | 选择其 `Editable placements - bake after moving` 子物体的 IslandDecorationLayout，移动/旋转/统一缩放标记，再点 Bake。数字前缀是 palette 索引，不要随意改名 |
| IslandDecorationLayout | EditorOnly 标记层，用于编辑局部构图；打包时剥离。运行时渲染读取烘焙 asset，不读取标记，也不把标记再画一遍 | Bake 校验地面支撑、坡度、清场与路线缓冲，写入 position/yaw/normal/scale、重算 cell bounds，同时更新固体碰撞。Bake 后保存 Environment |
| 普通场景 MeshRenderer / 树 Prefab | 旧草簇、树林、树根、木材和常规场景物体。`Island 1 - varied woodland trees` 使用 `Assets/Generated/Island1Woodland` 冠层、材质与 Prefab；近/远叶片共用材质 | 普通物体改场景/Prefab 覆盖；先看实际 sharedMaterials，不能根据文件夹猜测当前用材质。保留原 Prefab 连接及覆盖 |
| WorldPerformanceManager | Play 时安装，等待 additive load 与 wind profiles，然后接管普通 MeshRenderer 的材质实例、网格细节、距离驻留、草实例批次和草阴影。使用 `Resources.Load("FoliagePerformance/Library")` | 运行时普通草的距离、阴影和恢复在此脚本；库在 `Assets/Resources/FoliagePerformance/Library.asset`。不要直接手改运行时生成的材质副本 |
| GrassRenderSubmission | 在 beginCameraRendering 将普通草批次提交给正在实际渲染的 Game camera。旧 SubmitVisibleGrass 接口保留为空操作，兼容历史调用 | 不得把 DrawMeshInstanced 放回 LateUpdate、编辑器 tick 或对未渲染摄像机预提交；这是已验证的原生渲染队列/内存稳定性修复 |
| FoliageWindDriver | 一套全局风场与普通 Renderer 的根部/高度 MPB；新草和生态网格使用既有根部顶点权重 | 全局强度先改 driver，物种响应改实际共享材质 _WindBend。不能为每套系统另加风驱动器 |
| IslandEcologyCollisionBaker | 编辑器显式烘焙静态实体碰撞，与视觉 LOD 无关。只维护输出对象的直接子物体，保留 `Solid decoration \| Rock/Timber/Branch/Sapling \| …` 命名所有权；树干碰撞和用户其他碰撞不归它删除 | Layout.Bake 和 BakeIslandRealism.Bake 已接入。仅重建现有实体类型；草、花、苔藓仍可穿行。若重建基础生态，重新检验路线 |
| 开场沉船海岸地标 | Environment 的 `Island 1 - Sahur's washed-ashore shipwreck`，独立 Prefab `Assets/Generated/Island1Shipwreck/Sahur opening wreck - washed ashore.prefab`；直接引用开场 49 个原船构件/碎片的网格与原材质，并搭配 16 个已有海岸装饰 | 编辑 Prefab 或实例覆盖。此层是静态场景物体，不属于两套生态数据、标记 Bake 或 IslandEcologyCollisionBaker。18 个碰撞随 Prefab；无运行时生成器、刚体或漂浮脚本。`Tools/IslandShipwreckPass.cs` 的 Validate 是检查入口，初始化/修整方法仅供本次构建，均有避免覆盖保护 |
| Lighting / DayNightCycle | Lighting 场景管理太阳、程序天空、雾、后处理、反射刷新；不由植被工具覆盖 | 本轮未改场景光照、天空和地形材质。不要通过加 bloom 掩盖色相或接触问题 |

## LOD、显示与重复渲染

- 草甸：32–48m 近→中，75–100m 中→远，按 cell bounds 距离过渡，dither 互补覆盖；个体根部/矩阵不随 LOD 重新随机。最终材质覆盖淡出 177–225m，CPU 240m 驻留裁剪。
- 基础及局部生态：每个 species 各自的 lodStart/lodEnd/drawDistance/shadowDistance；同一个实例矩阵用于近远。不要把它们又注册进 WorldPerformanceManager 的普通 MeshRenderer 入口。
- 普通旧草：WorldPerformanceManager 使用库细节/等价紧凑顶点布局，原 MeshRenderer 在实例化时 forceRenderingOff，避免双画；退出 Play 恢复原材质、网格、可见性和 LOD。VerifiedGrass 的完整近布局及空间淡出修复保留。
- 原 `Whole Island Grass Cover`、`Matched Density Grass Cover`、`Normal Island Grass Cover`、`Denser Normal Grass Cover` 和八个 GrassVegetationMix 根当前均 inactive。本轮未将它们当成重复显示，也没有删除。627 张 ground_close_04 Renderer 原本已 disabled，继续保持。
- 本轮额外停用 473 个大型 Grass_Stems 模板 Renderer，以及 481 个偏青的 Forest001 / Grass_12 模板 Renderer：包围盒宽度超过 3m，并且同区域已经有至少 12 个现有草甸实例。两类分别保留 154 / 146 个覆盖不足的模板。仅写入实例 Renderer.enabled 覆盖，原模型、层级、变换与碰撞不变。完整对象及覆盖计数在 `.codex/environment-unification-20261107/legacy-patch-plan.json` 与 `cyan-patch-plan.json`，不要批量启用旧草根。

## 资源关系和历史工具

实际依赖快照在 `.codex/environment-unification-20261107/dependencies.json`：由 AssetDatabase.GetDependencies 检查已启用构建场景、两份生态数据、当前草数据及 Resources 库。当前这些共享环境材质的序列化构建场景使用者为 Environment；还要考虑运行时代码及未来工具，不能据此删资源。

- `Tools/BuildIslandEcology074.cs` 生成的 `IslandEcology074/Ecology.asset` 与植物网格/贴图仍是当前全岛 bake 的上游输入，**不是废弃资源**。
- `NaturalGrassScatter068/Scatter.asset` 是当前 bake 的候选位置输入，虽然现场 scatter 已指向 Island1Realism，仍不能删除。
- 全岛 bake 实际配置入口是 `Tools/Island1RealismSettings.json`。脚本残留的私有 SettingsPath 常量虽指向 `Bake Settings.asset`，LoadOptions 并不读取它；不要把这个旧路径当成生效配置，也没有据此删除任何数据。
- `Tools/BuildIslandWoodland.cs` 是当前树资产的生成入口；`Tools/ExpandIslandDecoration.cs` 是局部构图初建工具，Build 会拒绝覆盖已存在的编辑层，日常编辑应使用标记 Bake。
- WholeIslandGrassCover / IslandGrassVisualRepair / GrassPlacementTools 和数字版本 Grass027/028/032/068 等是早期草地构建、试验或修复入口。判断依据是它们指向旧输出或 inactive 草层，当前运行时不调用它们；部分输出仍被 Resources 库、Prefab、候选数据或其他旧工具引用。保留接口和资产，使用前审查其 SetActive/写入路径，不能与现有主 bake 叠加执行。
- `Tools/BuildGrassRenderMeshesV226.cs` 属于当前性能库的维护工具，校验紧凑顶点布局的几何/法线/UV/索引等价，不能作为历史垃圾清掉。
- `.codex` 下的相机、快照和临时对比脚本是证据，不进入玩家构建；没有删除、迁移或重命名任何旧资源。

## 推荐顺序

1. 打开 Tools > Scenes > Open Full Workspace，确认所有场景 dirty 状态、Prefab 覆盖与用户存档；有未保存内容时先保留，不用重开场景覆盖它。
2. 用 Tools > Island 1 > Habitat Review 查看 mask、保护路线及两份明确命名的数据；局部构图选择标记入口。先保存同机位 09:00 / 同画质基线。
3. 优先只改实际共享材质。草的亮度、蓝绿色与树皮色响应分开调；材质/贴图改动不需要重做全岛分布。不同物种保留合理色差和稀有外形。
4. 确实要改局部摆放时移动标记并 Bake；要全岛重烘焙时先保存数据备份，再运行 BakeIslandRealism.Bake。主 bake 更新基础生态，Layout.Bake 更新局部构图，两者不覆盖对方。
5. 原生保存 Environment，核对 diff；生成海面预览可能在场景保存时被重序列化，需比较网格 payload，不能把无关预览变更一起保存。本轮 payload 已逐项哈希确认一致。
6. 检查根部接触、保护路线、实体碰撞、缺材质、两套实例计数、普通草源 Renderer 的隐藏状态；检查近中远和 LOD 过渡，进退 Play 后再重开 Editor。
7. 同机位对比并记录密集区帧时间与内存。编辑器测量包含 Editor、SceneView、缓存开销，不能当作 standalone GPU 基准。

## 独立问题状态

云条纹与装饰碰撞是上一轮独立修复：程序云分层采样/抖动过滤已改，199 个实体装饰碰撞已保存，原记录为 199 次查询、88 次 CharacterController 接触和 768 条路线样本通过。本轮保留这些结果并补上了“重烘焙后碰撞跟随”的维护缺口。云仍有采样噪声，不能将本轮材质统一描述成新云渲染修复；低画质与长时间独立构建仍需进一步人工验收。
