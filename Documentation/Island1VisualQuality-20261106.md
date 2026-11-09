# Island 1：环境画质验证

## 实机对比

Unity 6000.3.23f1、URP。日间取景以同一组地形坐标和视角为准，设为 09:00、64% 云量。基线与成图都来自 Unity 渲染；最终使用实时主相机，波浪、风、反射更新时间可能不同。三个远景构图基线是在材质与光照阶段之后、放置岩屿之前拍摄的。

| 取景 | 修改前 | 修改后 | 成图观察 |
| --- | --- | --- | --- |
| 海岸入口 | [前](Island1VisualQualityReview/coast-day-before.png) | [后](Island1VisualQualityReview/coast-final.png) | 海天与草坡的层次更清楚 |
| 草坡 | [前](Island1VisualQualityReview/meadow-day-before.png) | [后](Island1VisualQualityReview/meadow-final.png) | 草地和裸土有更明确的受光差别 |
| 林地 | [前](Island1VisualQualityReview/woodland-day-before.png) | [后](Island1VisualQualityReview/woodland-final.png) | 枝叶投影和草簇根部更清晰 |
| 林间高坡 | [前](Island1VisualQualityReview/ridge-day-before.png) | [后](Island1VisualQualityReview/ridge-final.png) | 阴影区、树梢和天空分层更明显 |
| 近地材质 | [前](Island1VisualQualityReview/ground-day-before.png) | [后](Island1VisualQualityReview/ground-final.png) | 地表纹理与植被投影能同时辨认 |
| 西岸水线 | [构图前](Island1VisualQualityReview/west-shore-composition-before.png) | [后](Island1VisualQualityReview/west-shore-final.png) | 近处水线之外出现低矮的岩屿剪影 |
| 北岸和海角 | [北岸前](Island1VisualQualityReview/north-shore-composition-before.png) · [海角前](Island1VisualQualityReview/headland-view-composition-before.png) | [北岸后](Island1VisualQualityReview/north-shore-final.png) · [海角后](Island1VisualQualityReview/headland-view-final.png) | 从不同方位检查岩屿与开阔海面的比例 |

其他取景：[黄昏](Island1VisualQualityReview/coast-18h.png) · [夜间](Island1VisualQualityReview/coast-22h.png) · [任务引导回归](Island1VisualQualityReview/guidance-final.png)。

## 项目变更

- **地表与岸线：** IslandBlendedTerrain 改用 URP PBR 光照，接入环境光、遮蔽和反射；保留原有生态遮罩、纹理和几何。干草土、岩石和湿沙的平滑度基准为 0.08、0.23 和 0.57。加入世界坐标低频变化，细节法线强度在 35–125 米间逐步降低。海水着色器和水面玩法沿用原实现。
- **植被：** 现有植物统一接入距离雾和环境光；植物材质的适用叶片近根部略暗，使用现有 SSAO 与 MSAA 覆盖平滑草叶边缘。沿用现有植物位置、数量和风动画。
- **日夜与色调：** 第一岛启用海岸日夜配色及指数平方雾（密度 0.00062）；克隆了独立的 Coastal grading Volume，曝光 −0.12 EV、对比度 +5、饱和度 −6。Volume 默认关闭 Bloom 和暗角，运行时画质设置仍可覆盖。第一岛云量范围由 0.62–0.98 改为 0.48–0.84。
- **阴影与采样：** PC URP 资产的主光阴影图 2048→4096、最大距离 600→240 米、级联分割 (0.025, 0.075, 0.22)→(0.06, 0.18, 0.45)、深度偏移 0.1→0.05、法线偏移 0.5→0.23；MSAA 1×→4×。目标是把精度集中到玩家近处。使用同一 PC 资产的其他场景也会应用这组配置。
- **环境反射与构图：** 草坡、林地各一个 128 像素 HDR 盒投影反射探针，分帧且错开刷新。原海洋探针保留。第一岛西、北方向增加三组无碰撞、双级 LOD、无投影的离岸岩屿。当前使用三色环境光、现有 SSAO 和局部反射探针；[Unity 6 URP APV](https://docs.unity.com/en-us/engine/6000.3/manual/lighting-overview/lighting/probevolumes/concept) 需要布置、烘焙和专门的日夜验证，本轮未切换。
- **画面干扰：** 跨过天空的白线来自任务路径显示。引导离地高度由约 2.2–3.8 米降至 0.42–0.82 米，半径 0.055→0.039 米，从约 28 米起逐渐淡出。路线求解、目标和触发条件沿用原行为。

## 验证与边界

RTX 4060，1080p、4× MSAA，编辑器 Play Mode 的 Environment + Lighting 测试。每处预热 120 帧、采样 240 帧。实测 P95：西岸 **7.51 ms**，草坡 **12.79 ms**，林地 **14.37 ms**。[完整数据](Island1VisualQualityReview/final-review.json)。这是环境负载的短测，尚未测独立构建、显存或完整 Boss 战的最坏帧时间。

[场景比较](Island1VisualQualityReview/scene-comparison.json)确认原地形碰撞与海洋网格内容一致，Main、Gameplay、Enemies 文件字节一致。[引导测试](Island1VisualQualityReview/guidance-validation.json)从 Shore Camp 真实检查点到原有 Cappuccino 目标求解成功。Unity Test Runner 未发现已注册测试，使用项目实机取景和专用验证脚本。已恢复五场景编辑工作区；测试前的存档及其备份按字节恢复，退出最初 Play 会话生成的自动存档另行保留于本地工作区。
