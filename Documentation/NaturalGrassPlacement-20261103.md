> 2026-11-04 / task071 更新：用户要求保留长草。原 task068 的草叶高度过低（约0.18–0.43），已将四个共享草丛改为原高度5.5倍、宽度2.6倍、弯曲3倍；最高草叶约2.2–2.34（实例缩放后有所变化）。原389971个种植位置和沙滩留空规则保持。详见 TallGrassCorrection-20261104.md；下文task068图像/形态为历史版本。

# 第一岛草地摆放修正（068）

实际整合场景：`Assets/Scenes/First Island/Environment.unity`。本次没有重新生成 Windows 安装包。

## 最终实现

- 沙滩材质的高度阈值为海面以上 14 米，原来的种植工具却只检查约 0.55–1.6 米。现在按沙滩完整范围排除草，并给噪声边界及草簇足迹留出空间；交界处逐渐降低密度。
- 原有大块草模型保留在原位置并禁用。新布局使用固定种子、小草簇、位置扰动及两层密度噪声，消除大块模型的直边和矩形缺口。移动玩家不会重新生成布局。
- 草簇由弯曲、渐窄、带轻微扭转的草叶 Mesh 组成，4 个共享模型共 1008 个顶点，每簇 36 片草叶。最终 389971 个实例按 538 个空间分区渲染，不创建逐株 GameObject，也不保存整岛重复的草叶网格。
- Shader 增加默认关闭的 `_UseBladeHeight`，通过顶点颜色 alpha 存储单片草叶的高度，使合并/实例化草在坡面上仍能固定草根并接受微风和角色挤压。已有材质继续使用原来的高度计算。
- 不新增碰撞体，不改变玩家、战斗、Boss、动画或任务机制。本次最终草叶是程序化几何，没有新增外部资源或商业授权依赖。
- 渲染沿用工程的 200–240 米渐隐范围。运行时初始化按约 3ms 时间预算分帧，逐帧提交不建立临时矩阵列表。

生产数据和材质位于 `Assets/Generated/NaturalGrassScatter068/`，运行组件为 `NaturalGrassScatter` / `NaturalGrassScatterData`。生成期间淘汰的大型网格草试稿已删除，场景不依赖这些试稿。

## 检查与限制

- 在实际岛屿场景检查山坡、近景和沙滩；另在正常 Play Mode 中用游戏相机确认画面。
- 对最终所有实例的中心与足迹进行海滩/地表检查：沙滩侵入 0，缺失地表 0。抽查 905760 个实际草根：明显悬空 0；最高根部间隙 0 米，最深埋入约 9.8cm。额外剔除了坡折处支撑不合适的 345 簇。
- Shader 编译错误 0；新草对象碰撞体 0；活动旧草 Renderer 0。工程另有 606 个原有缺失 Prefab 的空占位对象，不参与草地渲染，本次未删除它们。
- Play Mode 冒烟检查 239 帧、草簇初始化完成、实际相机有可见批次、无新增异常。该轮在最后剔除 345 簇之前记录：平均约 18.3ms，最大约 175ms；这是短时 Editor 记录，不是长时间性能或完全卡死问题的结论。
- 当前已验证的是摆放、贴地、沙滩排除与基本运行；没有宣称达到最终商业美术标准。
- 玩家存档逐字节恢复，原来的五个场景布局及键鼠输入保持正常。

本地实拍和报告：`.codex/grass-placement068/` 下的 `gameplay-grass.png`、`meadow-beach-2.png`、`final-audit.json`、`live-smoke.json`、`restored.json`。

## 回退和后续编辑

整场景原始副本：`.codex/grass-placement068/Environment.before.unity`。原草的激活状态：同目录 `legacy-states.json`。

使用 Unity Pipeline 执行（停止 Play Mode、先保存其他场景编辑）：

```powershell
unity command run_script --file Tools/NaturalIslandGrass068.cs --entry NaturalIslandGrass068.RestoreLayout
```

此操作禁用新草，恢复旧草的激活状态；不删除模型或修改其他游戏系统。

`NaturalIslandGrass068.Replant` 用于修改岛屿形状/沙滩参数之后重新采样布局，并在本地保留旧数据副本；`ShapeMeadow` 重建共享草叶，`PruneEdges` 去除坡折处不合适的草簇，`AuditMeadow` 检查最终数据。重新种植后应再次进行实际画面检查。
