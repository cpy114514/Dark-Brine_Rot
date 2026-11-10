# 岛二云海与模型精修第一批

Unity 6000.3.23f1，URP。岛二的新增布置保存在 Second Island 场景的 `Second Island - Cloud Sea` 根节点。角色资产已接入生产 Prefab 和材质。Blender 工作文件按资产保存在 `ArtSource/ModelRefinement`。

## 岛二

- 东、北、南三处崖边各有碎石引路、座椅、灯和短木栏。小屋院路接到入口坡道，营地入口补了木箱、灯和植被。所有新增布置沿原地形放置。木栏为视觉标记，边缘仍用原地形的碰撞和坠落行为。
- 云顶约为世界 Y 2289，三个观景地面约为 Y 2391 至 2428。近处漂移的云团和下方连续云层在同一画面可见。渲染使用世界空间噪声、深度裁剪和远距离近似。原有高空散云仍参与画面。
- 崖岩源网格在 Blender 中从 153,472 三角面精修至 16,114 面，并调整了岩石色调。灌木由 156,012 面减至 21,841 面。两者使用 1K 贴图。原始模型为 CC0。 [Mountainside](https://polyhaven.com/a/mountainside) · [Shrub 01](https://polyhaven.com/a/shrub_01) · [许可](https://polyhaven.com/license)
- Unity 实景以相同机位截图对比。Sahur 在南侧观景点接触原地形时，第三人称相机可看到云海。另检查了写实模式和梵高模式。
- RTX 4060、1920×1080、渲染比例 1，东侧相同机位两轮开关云层，每段采 48 帧。GPU 中位数分别为 2.696 → 3.366 ms、3.715 → 4.519 ms。云层相对成本约 0.67–0.80 ms。512² R8 云噪声贴图为 0.25 MiB。计时是编辑器内固定机位的采样。

## 已接入的模型

| 生产资源 | Blender 精修 | Unity 校验 |
| --- | --- | --- |
| Sahur 木棍 | 清理重叠顶点与异常平滑，沿原轮廓减面，调整局部曲面。重做环绕 UV、木纹与凹凸 | 50,000 → 7,500 三角面，28,625 → 3,838 Blender 导出顶点。握持坐标、Prefab 引用和 BoxCollider 保留 |
| Sahur 身体 | 基于原贴图区分干木、浅色高光表面的粗糙度 | 499,949 三角面与 53 骨生产蒙皮沿用。工作文件中的原始绑定少于生产骨架，Unity 沿用原身体网格 |
| Cappuccino 身体 | 为杯身纸和布袖增加细法线，保留塑料高光和原配色 | 23,794 三角面与 14 骨。在生产网格补 13,212 个切线，核对坐标、三角索引、UV、法线、Bind Pose、13,518 条骨权重逐项相等 |
| Nailong 身体 | 分开哑光衣料、黄色皮肤和眼部的光泽响应 | 499,752 三角面与 23 骨蒙皮沿用，沿用原材质 GUID |
| 长腿鸭远景 | 从生产网格的顶点、UV 和骨权重建 Blender 工作副本，保护眼睛、嘴和腿的轮廓 | 原近景网格 499,984 面。远景 64,996 面、43,378 顶点，共用原材质、13 骨和 Bind Pose。LOD 以 11% 屏幕相对高度切换 |

在真实第一岛场景中，以同灯光、相机和骨骼矩阵采集三名角色的正、侧、背面和两组动作姿态。Sahur 采样移动和木棍挥击，卡布奇诺采样第二阶段俯冲和转换，奶龙采样移动和踉跄。三者 Avatar 有效，采样前后骨骼矩阵一致。木棍命中与原有移动、武器规则回归分别通过 14 和 18 项检查。阶段动作截图为指定时刻采样。

木棍网格在编辑器 Play 内的 Unity Profiler 对象内存约 3.19 → 0.44 MiB。两张 512² 压缩木纹贴图合计约 0.53 MiB。三张 1K 角色贴图各约 1.42 MiB，奶龙的新贴图取代材质中的原光泽贴图引用。卡布奇诺身体的切线增加约 0.32 MiB。这些是 Unity 资产对象内存估算。

长腿鸭远景三角面减少约 87%，额外的远景网格对象约 7.37 MiB。近景资源保留，两套网格共用贴图。实景比较了正、侧、背面及走、跑、啄、扑、倒地共八个机位和动作组合。姿态骨骼矩阵相同，最大外接范围偏差约为体型对角线的 0.006%。受击盒使用原网格和骨骼。

## 审计范围与优先级

`Tools/AuditModelRefinement.cs` 从 9 个启用的场景和已知运行时敌人资源里识别到 78 个候选 Prefab。清单记录资产路径和 local file ID、面数、骨骼、UV、法线、切线、Blend Shape、材质和贴图尺寸。动态生成的场景装饰和直接放入场景的网格还需按使用点盘点。

| 优先级 | 当前状态 |
| --- | --- |
| 高频近景玩家和主要 Boss | 本批完成上述精修。Sahur 身体的骨架、权重和手指附件需要以生产网格为准。奶龙和 Sahur 的高面数近景身体仍待单独做变形对照后决定减面或 LOD |
| 三种普通敌人 | 已完成长腿鸭远景网格。鸡哥 499,918 面 / 28 骨、鹅 499,972 面 / 19 骨仍待针对各自轮廓与动作处理 |
| 其余 Boss | 黄眉 276,424 面 / 340 骨主体，八戒兽形身体和毛发共 141,964 面 / 289 骨、人形 111,091 面 / 397 骨，九齿钉耙 210,457 面 / 9 骨。另找到飞鳄和鲨鱼的生产视觉资源，鲨鱼战斗使用动画 FBX |
| 环境与船骸 | 岛二已改上述观景线、院路与营地。序章船骸用 `Assets/Resources/Story1Wreck/ShipWreckPieces.asset`，对应 Blender 分件源在 `ArtSource/ShipWreck`。船骸与其余环境资源仍待逐项精修 |

## 本机证据与回退

验证记录和同机位 PNG 位于 `.codex/model-refinement`。其中 `comparisons` 为截图对照，`island2/player-overlook.png` 为角色实景。木棍原资源、角色原材质、Capri 网格和长腿鸭 Prefab 的备份分别在 `stick`、`hero-surfaces`、`duck`。岛二场景保存在工作区。开工前与完成后的场景快照分别在 `island2/Second Island.before.unity` 与 `island2/Second Island.after.unity`，两份均包含此前已有的岛二工作。Git 回退点记录资产和复建工具，岛二场景文件保留在工作区以分隔此前已有的场景改动。

岛二单项回退用原生 Unity 命令 `unity command run_script --file Tools/BuildSecondIslandCloudSea.cs --entry BuildSecondIslandCloudSea.Remove`。该命令要求打开已保存的岛二场景，按根节点移除这一批布置。需要重新应用时使用同一工具的 `BuildSecondIslandCloudSea.Run`。布置复用了工作区已有的 `Assets/Resources/SecondIslandRealism` 道具。

在隔离 Play 中用 `ReviewSecondIslandCloudSea` 和 `ReviewRefinedModels` 复测。原生审计入口为 `AuditModelRefinement.Run`。局部导出和安装入口在 `RefineSahurStick`、`InstallHeroSurfaceRefinement` 与对应的 Blender 脚本中。
