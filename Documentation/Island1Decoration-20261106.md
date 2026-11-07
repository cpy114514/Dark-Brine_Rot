# 第一岛地表装饰扩充

已保存到 `Assets/Scenes/First Island/Environment.unity`。独立根对象为 `Island 1 - habitat detail compositions`，共 738 个装饰实例、29 处小景。原有草地和植物数据继续使用。

- 海岸：窄带石砾、贝壳、搁浅海藻、落木；两处原船破损船板组成的残骸小景，共六片船板。
- 林下：落叶、苔藓、枯枝、矮蕨和少量菌类，围绕树荫布置。
- 草地：小型黄花、紫花、低草和落枝的局部组合。
- 岩地：碎石簇、低草和少量苔藓。
- 稀有植物：两株铜色卷蕨，保留少见的辨识度。

编辑期按固定种子 611060 放置，结合海拔、坡度、原植物位置和已有碰撞体筛选。海岸条带跟随局部坡面等高方向；树下、草地和石坡使用不同范围与物种组合。32 个候选点中，3 个未通过布置条件而留空。1046 次不合适的候选放置被过滤。

## 编辑方式与渲染

展开根对象下的 `Editable placements - bake after moving`。每个叶子节点是一个可移动、旋转、统一缩放和复制的实例，名称前缀是调色板索引。选中父对象上的 `IslandDecorationLayout`，点击 **Bake edited placements**。烘焙先验证地面和禁放区，再更新实例数据；之后只保存 Environment 场景。烘焙按钮不重新随机生成布局，手动修改会被保留。生成工具遇到已有布局会拒绝覆盖。

布局节点标为 EditorOnly，构建时剥离。运行时复用 `IslandEcologyRenderer`，按 24 米空间单元进行实例批处理、视锥与距离裁剪。植物和地表细节有两级网格，主要在 22–34 米过渡、65 米裁剪；海藻在 15–23 米过渡、45 米裁剪。船板自身只有 112–208 三角形，100 米裁剪。新增碰撞体为零，无运行时随机生成。

## 画面对比

Unity Editor Play 中使用相同的 1920×1080 相机，切换本次装饰层取得前后图。风、波浪与光照时间持续运行，因此植被和海面像素不完全一致。

| 区域 | 原布局 | 扩充后 |
|---|---|---|
| 海岸 | [之前](Island1DecorationReview/Coast-before.png) | [之后](Island1DecorationReview/Coast-after.png) |
| 林下 | [之前](Island1DecorationReview/Woodland-before.png) | [之后](Island1DecorationReview/Woodland-after.png) |
| 草地 | [之前](Island1DecorationReview/Meadow-before.png) | [之后](Island1DecorationReview/Meadow-after.png) |
| 石坡 | [之前](Island1DecorationReview/Scree-before.png) | [之后](Island1DecorationReview/Scree-after.png) |

## 验证

[最终验证](Island1DecorationReview/validation.json)：738 个实例全部有地面支撑；中心高度最大误差约 0.000020 米；按完整模型水平包围半径检查，道路、Boss 场地和补给禁放区侵入为 0；所有近远网格顶点均在批次包围盒内；缺失引用、着色器错误为 0。生成时还检查了周围地面，拒绝跨越明显地形折角的实例。

[场景比较](Island1DecorationReview/scene-comparison.json)：Enemies、Gameplay、Lighting、Main 文件与本轮开始前逐字节一致。Environment 除新增层及场景根列表外，只有 Unity 重新分配的内嵌网格引用编号变化；岛屿碰撞网格和原海洋预览网格的序列化内容均一致。原有对象、Boss 位置与水系统设置保留。测试结束恢复了五场景工作区，玩家存档逐字节验证未改变。

Unity 编译通过。Test Runner 发现 0 项现成测试，因此没有宣称通过 NUnit 测试。本轮完成原生 Play 渲染、支撑/禁放区/批次包围盒检查；没有完整重跑主线战斗、角色全岛徒步或敌人寻路。

## 性能与限制

RTX 4060，Unity 6000.3.23f1 Editor Play，Environment + Lighting，固定海岸相机，1920×1080、render scale 1、VSync 关闭。ABBA 四轮，每轮 120 帧预热、240 帧测量。

[第二轮原始数据](Island1DecorationReview/performance-second.json)较稳定：关闭层的平均帧时间约 6.95 ms，开启层约 7.18 ms，增加约 0.23 ms。最终模型调整后的[复测](Island1DecorationReview/performance.json)中，开启层两轮分别为 7.101 / 7.107 ms，P95 为 8.263 / 8.096 ms；末轮关闭为 7.403 ms，第一轮关闭仍有明显预热/编辑器波动。这些结果不支持宣称性能提高。稳定计数约增加 23 次绘制、4.4 万渲染三角形。

编辑器计数可能包含其他视图。未测独立 Player、独立 GPU 时间或 VRAM，也未覆盖每个小景的最差机位。细节复用现有网格，近距离仍可能辨认出重复形状；本次保留了大片留白，未用密集装饰填满整个岛。

## 素材与授权

本次没有下载或购买外部资源。石砾、贝壳、落枝、蕨、苔藓、花草均复用项目已有生成资产。新增海藻网格由本次工具制作，材质从已有项目材质派生并共享贴图，没有新增大型纹理。三种船板网格派生自项目现有 `Story1Wreck/ShipWreckPieces`，保留原 UV，复用原船木材贴图；原船及其贴图继续遵循原素材授权，未将其重新声明为 CC0。派生资产位于 `Assets/Generated/Island1Decoration`。
