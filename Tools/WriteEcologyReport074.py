"""Build the handoff from measured native Unity evidence."""
import json
import shutil
from pathlib import Path

root=Path(__file__).resolve().parents[1]
work=root/'.codex/island-ecology074'
evidence=root/'Documentation/EnvironmentReview074'
evidence.mkdir(exist_ok=True)
for name in ['before-0.png','after-0.png','before-1.png','after-1.png','before-3.png','after-3.png','specimens-3.png','performance-baseline.json','performance-sample.json','performance-final-bark.json','validation-island.json','meadow-validation.json','gpu-wind-interaction.json','repeatability.json','gameplay-regression.json','serialized-scene-diff.json','non-target-verification.json','final-state.json','botanical-library.json','placement-island.json','protected-gameplay.json']:
    if (work/name).exists():shutil.copy2(work/name,evidence/name)
def read(name):return json.loads((work/name).read_text(encoding='utf-8-sig'))
before=read('performance-baseline.json')
after=read('performance-final-bark.json')
species=read('placement-island.json')['species']
total=sum(s['count'] for s in species)
uncanny=sum(s['count'] for s in species if s['uncanny'])
names=['耐风莎草','矮羊茅草','宽叶草本','鹿舌状蕨','羽状蕨','海岸鼠尾草状灌木','林下阔叶灌木','阔叶幼树','窄叶风弯幼树','赭黄野花','紫色钟形野花','象牙白野花','栗色菌群','垫状苔藓','铜褐落叶','枯枝','墨色卷叶蕨','潮汐微光菌','盐雾卷须藤','暗红种子花苞']
rows='\n'.join(f'| {i+1} | {names[i]} / {s["name"]} | {s["count"]} | {"怪诞" if s["uncanny"] else "自然"} |' for i,s in enumerate(species))
def image(name):return f'<{(evidence/name).as_posix()}>'
report=f'''# Island 1 环境与植被改造 · 2026-11-04

已在 `codex/island1-ecology074` 分支实现并保存到 `Assets/Scenes/First Island/Environment.unity`。先制作半径 68 米的样板，再完成画面、贴地、风摆和性能检查，随后扩展至第一岛。当前交付为可以继续编辑和运行的生态改造版本，不等同于最终商业美术验收。

## 本地基线与保护

最初工作区并不干净：Environment、ProceduralIsland、地形和植被 Shader、角色、Boss、任务及性能脚本均存在本地新版改动；NaturalGrassScatter068、task071 长草修正及多份工具尚未提交。本轮以本地文件为基线，没有用 GitHub/HEAD 覆盖它们。原草为 389,971 簇，最高共享草叶约 2.2–2.34 米，保留该高度及近景轮廓。

修改前建立独立分支，完整原场景位于 `.codex/island-ecology074/backup/Environment.disk-before.unity`；工作区 patch、status 和本轮改写的原 C#/Shader 文本也在 backup 中。没有提交、推送或发布构建。

审查了 FoliageWindDriver、FoliageInteractionSystem、MavisFoliageWind、FoliagePerformanceBuilder、ProceduralIsland、HunyuanEnvironment 文档、HunyuanCandidates、ArtSource/Environment，以及最近的 NaturalGrassPlacement 和 TallGrassCorrection 文档。

## 新增生态层

共 {total:,} 个共享网格实例；其中怪诞 {uncanny:,} 个，占新增生态层 {uncanny/total:.1%}，集中在内陆、林缘和隐蔽湿润聚落。比例不把原有数十万簇基础草叶混入计数。品种名称是程序化美术形态命名，并非植物学精确复刻。

| # | 植被类型 | 簇/实例数 | 类别 |
|---|---|---:|---|
{rows}

自然与怪诞植物均有近、远景网格、共享材质及可单独摆放的 LODGroup Prefab。20 种近景模型约 190–4,096 三角形；远景约 48–736 三角形。没有逐株 Update、碰撞体、Animator 或 GameObject。全岛生态层只有一个 IslandEcologyRenderer。

程序化生成的叶脉、表面起伏、粗糙度、遮蔽、透光和微光纹理使用 256px 共享贴图、Mip Map 和压缩。几何叶片形成实际轮廓，不依靠大面积透明卡片堆叠。14 棵原有未贴图浅灰树改用单独共享的 Weathered_Bark，复用项目树皮和法线贴图，通过三平面采样减少 UV 拉伸；源模型与碰撞保持。

## 分布与地面

种子 740311。布局按聚落中心、双尺度空间噪声、海滩高度、坡度、现有树冠遮蔽代理、岩石邻近度和湿润度代理分配植物。遮蔽/湿润度是美术规则，不是实时日照或水文模拟。不同高度、大小和朝向均可复现。

海岸种低草、莎草及稀疏灌木；开阔地配置野花、矮草和少量幼树；树旁和岩石附近偏向蕨类、菌类、苔藓和碎屑。植物整体足迹及根部均采样地形，过滤坡折和障碍，不只检查中心点。

保留 14 个明确保护区与两条带宽度的路线，包括三个 Boss 区域、按 ChickenBroEncounter.PlaceInland 推导的鸡群战斗区、补给箱、复活点、登陆恢复、捡棍剧情点和现有触发器。新增层不修改导航、角色碰撞或触发逻辑。

长草继续沿用 NaturalGrassScatter。新 LongGrass.asset 从原布局派生，为林下、聚落及通道形成疏密变化，保留 185,170 簇，原 389,971 簇资源完整保留。近景仍为 36 片/180 三角形；中景保留 12 片/60 三角形，远景 4 片/20 三角形；距离约 32–48m、75–100m 之间互补抖动过渡，沿用原有 200–240m 总视距渐隐。没有把高面数 AI 草替换到数十万簇草上。

## 风摆、交互和渲染

继续由现有单个 FoliageWindDriver 发布统一方向、时间、强度和阵风；FoliageInteractionSystem 的角色与脚印场保留。没有重写这两个系统，也没有重新生成原有 FoliagePerformanceBuilder 的重型优化库。

新增植物通过顶点颜色 alpha 储存根部到顶端的弯曲权重：草较快，灌木适中，幼树缓慢且根部更硬，怪诞植物低频缓摆，苔藓和碎屑固定。频率分别约 1.65、0.8、0.45、0.32；幅度由各共享材质控制，空间风场提供不同相位。

Mavis/FoliageWind 增加默认关闭的 Botanical PBR 分支，使用 URP 的光照、法线、粗糙度/金属度、遮蔽、透光与少量发光。新植物保留紫、赭、褐及深色叶片，不再被旧草色重映射全部拉绿。旧材质默认仍走原有分支。Forward、DepthOnly、ShadowCaster 共用顶点变形、裁剪和 LOD 覆盖；草阴影策略保持，新增植物近景投影、远处关闭投影。

## 外部资源与混元

本轮没有下载第三方资产、登录 AI 网站、消耗混元额度或上传模型。20 种新网格和共享表面纹理由本轮版本化 C# 工具生成；14 棵旧树复用工程已有树皮资源，未引入新的来源或许可依赖。

既有 HunyuanCandidates 与 ArtSource/Environment 原文件保留，既有卡片草/岩石不批量替换。历史文档记录原始 AI 草/岩石约 150 万面，经过清理、降面和草卡派生；本轮没有重新导入高模，也没有运行 Blender 或新的混元处理链。未调用混元服务，因此本轮没有验证当前网站额度、自动化能力或授权条件，也不对旧混元资产的商业授权作新的保证。任何以后新增混元资产仍需按实际官方条件确认。

## 实测性能

硬件：{after['hardware']}；{after['cpu']}；Unity 6000.3.23f1。固定同一镜头、1920×1080 RenderTexture、URP renderScale=1、关闭 VSync，90 帧预热后记录 240 帧。场景为实际 Environment + Lighting 的隔离 Play。渲染计数可能包含编辑器其他视图，内存为 Editor 总已用内存，不是独立 Player 或 VRAM。功能测试的固定 60Hz 时间步不被用作 FPS 结果。

| 指标 | 改造前 | 最终版本 |
|---|---:|---:|
| 平均帧时间 | {before['meanMs']:.2f} ms | {after['meanMs']:.2f} ms |
| 平均 FPS | {before['fps']:.1f} | {after['fps']:.1f} |
| P95 帧时间 | {before['p95Ms']:.2f} ms | {after['p95Ms']:.2f} ms |
| Draw Calls | {before['drawCalls']:.1f} | {after['drawCalls']:.1f} |
| 三角形计数 | {before['triangles']:,.0f} | {after['triangles']:,.0f} |
| Editor 已用内存 | {before['usedMemoryMiB']:.1f} MiB | {after['usedMemoryMiB']:.1f} MiB |

主要收益来自基础长草的距离 LOD 和生态疏密，而非显著减少 Draw Calls。内存略增，新增贴图、网格及编辑器缓存都可能贡献。该镜头超过 60 FPS 参考线，不据此保证整个游戏或独立构建持续 60 FPS。

原始报告：`.codex/island-ecology074/performance-baseline.json`、`performance-sample.json`、`performance-final-bark.json`。中间的 performance-final 为树皮修复前一次记录，最终表格采用 final-bark。

## 验证结果

- 全岛新增层：337,329 个根部采样，悬空 0、缺失地面 0、保护区侵入 0、越出裁剪包围盒 0、非有限顶点/法线 0。最大根间隙 0，最深局部埋入约 17.9cm；这项技术检查不能代替逐株美术检查。
- 新草：185,170 个中心全部检查；283,608 个草根抽样，沙滩侵入、悬空、缺失地面及越界均为 0。草根约埋入 1.7–9.5cm。
- GPU 实际图像夹具确认风摆与角色挤压改变叶片，根部轮廓稳定在像素容差内；原有 Forward/Depth/Shadow Pass 存在，Shader 编译错误为 0。
- 固定种子重新生成全岛，位置、朝向、法线、缩放的 SHA256 完全一致。资产保存并多次重载后 7,998 个生态实例和 185,170 簇草均恢复。
- 复用 ReviewGameFeel072 的 terrain 模式：796 帧、20 项检查全部通过、无错误/异常/断言。包括走跑、跳跃落地、翻滚、棍棒/双刀连段和取消、鼠标相机、避障、锁定侧移。使用明确排除存档的测试角色；测试期间真实 Gameplay/Enemies 根被暂时停用，仅在 Play 内生效。
- Environment 原碰撞体仍为 39，新增层碰撞体 0。序列化对比显示两块已有生成网格只重分配 fileID，数据内容完全一致，包括引用它的地面 MeshCollider；原对象 Transform 未改变。14 处材质覆盖、草数据引用和一个生态根为预期场景改动。
- 玩家存档逐字节校验未变化；Main、Gameplay、Enemies、Lighting、第二岛及原 Scatter.asset 均未保存改动。最后恢复原五场景编辑器布局，停止 Play。

报告：`Documentation/EnvironmentReview074/` 中的 `validation-island.json`、`meadow-validation.json`、`gpu-wind-interaction.json`、`repeatability.json`、`gameplay-regression.json`、`serialized-scene-diff.json`、`non-target-verification.json`。原生截图及关键性能数据也复制至该目录，可随项目保留。

## 原生前后截图

以下来自 Unity Camera.Render，成对镜头的位置、朝向、分辨率一致；不是概念图或 AI 修图。风与海面时间并未锁死。

草地改造前：

![草地改造前]({image('before-0.png')})

草地改造后：

![草地改造后]({image('after-0.png')})

林缘改造前：

![林缘改造前]({image('before-1.png')})

林缘改造后（含原白模树皮修复）：

![林缘改造后]({image('after-1.png')})

整体布局改造前：

![布局改造前]({image('before-3.png')})

整体布局改造后：

![布局改造后]({image('after-3.png')})

怪诞植物原生材质检查（独立背景，不是岛上构图）：

![怪诞植物]({image('specimens-3.png')})

## 编辑、回退与尚未验证

可编辑参数在 `Assets/Generated/IslandEcology074/Settings.asset`：种子、聚落间距/密度、怪诞比例目标、坡度、海岸余量、样板范围、林地/草地留草比例、保护区、路线。Ecology.asset 中可分别编辑各品种视距、投影距离、LOD 距离；各 Prefab 可用于手工点景。

用 Unity Pipeline 执行原生作者工具，停止 Play 并先保存其他编辑：

```powershell
unity command run_script --file Tools/BuildIslandEcology074.cs --entry BuildIslandEcology074.Expand
unity command run_script --file Tools/BuildIslandEcology074.cs --entry BuildIslandEcology074.DressIsland
unity command run_script --file Tools/BuildIslandEcology074.cs --entry BuildIslandEcology074.SaveIsland
```

回退布局可运行 `BuildIslandEcology074.DisableEcology`：禁用新增层、恢复原长草资源及旧白模树材质，保留所有新资产；完整原场景和本轮修改前的源文件另有备份。该回退工具已通过编译检查，本轮没有实际执行回退。

尚未完成：独立 Windows Player 的 1080p 长时间测试、完整 Boss/剧情通关回归、GPU 独立帧时间与 VRAM、其他显卡/API、所有 LOD 距离的系统性运动镜头检查和逐株近景美术验收。本轮验证为原生 Editor 环境采样与现有角色地形回归；没有生成新的安装包。既有部分高模树仍依赖原性能库，未重做所有树木资产。若追求扫描资产级近景质感，仍需要对英雄植物和地面做专项美术迭代。
'''
(root/'Documentation/Island1Ecology-20261104.md').write_text(report,encoding='utf-8')
print(json.dumps({'report':'Documentation/Island1Ecology-20261104.md','instances':total,'uncanny':uncanny,'percentage':uncanny/total,'fps':after['fps']},ensure_ascii=False))
