# 岛二西谷巧克力水系

## 流动与瀑布修订

河面的流纹现在按烘焙的沿程时间向下游移动。湖口使用固定出流方向，岸边保留低速回流。北崖瀑布沿真实崖面加速，到了外缘分成三股落入云层。沿用现有河床、浅滩和接触数据。

新版正式场景的四段固定机位录像位于 `.codex/chocolate-motion/island2-chocolate-flow.mp4`，依次为湖口、河弯、瀑布侧面和下段分流。修订前的场景与 Shader 备份同在 `.codex/chocolate-motion/`。下文的独立构建帧时间属于西谷水系的初次重构。

## 西谷水系初次重构

正式场景使用 **Chocolate Waterway - West Valley**。大湖从西岸溢出，沿西谷向北，最后在云崖落入云海。新河长约 351 米，宽约 20–31 米。西谷有三个浅滩，北侧浅滩接通观瀑路线。取样站移到湖口下游。

- 地形从共享原始岛面重新烘焙。新网格用局部细分处理岸线，以顶点数据混合湿可可色。湖口过渡使用实体河床和连续流向。Boss 区、营地、小屋及跳跃路线共 405 个地面采样点与原场景一致。
- 湖、河和瀑布使用独立流速。湖心高光缓动，湖口向外汇流，河弯靠岸减速并有回流，瀑布形成宽帘后在低处拉成三股。
- `SecondIslandChocolateWaterway` 保存可渲染的水线轮廓、断面、流速、过河点和旧河槽位置。渲染、Sahur 接触和岛二声音读同一来源。旧存档在改造路段如嵌入地面，读档仅修正高度。

## 工作区与证据

- 正式场景为 `Assets/Scenes/Second Island/Second Island.unity`。候选场景位于同目录的 `Waterway Preview` 文件夹。旧正式场景的回退副本位于 `.codex/chocolate-rebuild/Second Island.before.unity`。
- Unity CLI 原生工具为 `Tools/RebuildIsland2ChocolateWaterway.cs`。实机输入与隔离存档检查入口为 `Tools/VerifyIsland2ChocolateWaterway.cs` 的 `Run` 和 `Saves`。
- `.codex/chocolate-rebuild/waterway-preview.mp4` 依次展示湖口、河弯和瀑布侧面。独立 Windows 构建在 RTX 4060、D3D11、1280×720、关闭垂直同步与帧率上限的条件下，从相同机位各采样 180 帧。新版中位帧时间是湖口 4.13 ms、北崖 4.43 ms，旧版是 3.93 ms、4.15 ms。采样器未返回独立 GPU 帧时间。
