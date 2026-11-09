# Unity 闪退排查与修正

## 崩溃证据

本机 `Crash_2026-11-07_021157142/Editor.log` 明确记录 `System out of memory`，失败分配只有 13,824 字节，Renderer 标签。此时 `ALLOC_DEFAULT` 已使用 91,137,573,311 字节，约 91.14 GB。托管调用栈为：

`NaturalGrassScatter.LateUpdate → Submit → Draw → Graphics.DrawMeshInstanced → Internal_DrawMeshInstanced`

此前日志反复出现 `Ran out of Graphics Ring Buffer space`。这证实本次崩溃是渲染提交路径的内存耗尽，而非普通 C# 空引用异常。不能据此推断用户提到的所有历史闪退均由同一原因引起。

## 修正

- `NaturalGrassScatter` 从每次 LateUpdate 提交改为 SRP 的 `beginCameraRendering` 回调，仅向实际开始渲染的 Game / SceneView 摄像机提交。
- 保留其 `Submit` 接口，但拒绝渲染回调之外的直接调用，避免工具或不活跃视图重新积压绘制。
- `GrassRenderSubmission` 和 `WorldPerformanceManager` 的旧草地路径采用同样的摄像机回调方式。旧接口不再遍历所有已启用摄像机排队。
- 组件停用时解除事件注册，重启时重新注册，保留原网格、实例布局、裁剪、LOD 和材质。

Unity 官方说明：指定摄像机的 DrawMesh 提交分配会保留到该摄像机实际渲染。此前的更新循环可能持续向未渲染的摄像机排队；这个机制与本地崩溃栈吻合。[官方 DrawMesh 文档](https://docs.unity.com/en-us/engine/6000.3/script-reference/unityengine/graphics/drawmesh)

## 验证范围

验证工具：`Tools/VerifyGrassStability.cs`。原始记录位于 `.codex/stability-20261106`；崩溃前日志已单独保留为 `crash-editor.log`。

本轮使用实际第一岛 Environment + Lighting，检查 20,000 次未渲染摄像机提交被拒绝，并用实际相机渲染确认草地仍然显示。持续测试期间交替渲染和不渲染相机，反复启停草地组件，记录 Windows 进程 Private Bytes 和 Unity 已分配内存。测试设有内存增长保护，不刻意重现会耗尽系统内存的旧行为。

首次测试因初始化期间的一条图形环形缓冲区警告提前停止。第二轮保留并统计该警告，继续观察内存是否持续增长。该警告与内存泄漏分别报告，不以增加缓冲区大小掩盖问题。

Unity 的崩溃场景副本已保存在 `Assets/_Recovery`。本轮不保存或重写生产场景，不修改玩家存档，不升级 Unity 或修改图形驱动。独立 Player、其他关卡与数小时连续编辑尚未覆盖。

## 最终结果

- Unity 编译通过；现成 Test Runner 测试数量为 0。本轮使用原生编辑器回归脚本。
- 五分钟持续测试通过，20,000 次非渲染摄像机调用没有提交绘制，实际草地渲染正向检查通过。
- 进程 Private Bytes：测试开始 8.119 GB，结束 8.092 GB；30 秒后低点约 7.96 GB。Unity 已分配内存结束约 2.463 GB，没有重现崩溃前的数十 GB 增长。
- 持续测试捕获 1 次初始化图形环形缓冲区警告，之后五分钟内未新增。环境重开/加载时仍可出现该类警告，不能宣称全部图形压力问题已消失。
- 三次环境卸载/重开通过，每次恢复 136,643 个草簇。批量重开命令超过连接的 30 秒响应期限，但编辑器继续完成了操作；`scene-cycles.json` 记录三次成功，随后确认编辑器处于 ready、stopped，两个场景均无未保存修改。
- Environment、Gameplay、Enemies、Lighting 文件哈希保持一致；玩家存档逐字节检查通过。测试结束恢复了测试前的 Environment + Lighting 工作区。

证据副本：[持续测试](UnityStabilityReview/result.json)、[场景重开](UnityStabilityReview/scene-cycles.json)。本轮处理了有明确崩溃栈支持的草地提交风险；长期编辑、其他场景或不同崩溃栈仍需进一步验证。

## 2026-11-07：普通草丛批次开销复查

发现 2,760 个普通草丛使用的 Grass_12 / Grass_Stems 关闭了实例化，且风场逐物体覆盖的 `_PlayerPushStrength` 不是实例化属性。保留此前摄像机提交保护，将该既有属性纳入 shader 实例化缓冲，并通过原生 API 开启两份材质的 GPU Instancing；不改变草量、几何体、画质或图形缓冲大小。

相同编辑器 Game View 的 draw calls 从 4,009 降至 1,617，triangles 保持 53,558,679；受控开关测试重复确认合批差异。同机位渲染的近地取样区域像素一致。脚本域重载及两轮 Play 进出未新增 Ring Buffer、运行时异常或设备丢失日志。五场景与正式存档哈希核验未改变。此项验证不代表长期警告已根治，也不代替独立构建、完整通关和 90 分钟稳定性验收。

完整范围、原始数据和前后图：[本轮记录](../.codex/ring-buffer-review-20261107/review.md)。

