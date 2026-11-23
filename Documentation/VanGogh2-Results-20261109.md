# Van Gogh Rendering 2.0 — 实施与实测

这次只重做梵高模式。新版已接入现有 Style Changer，保留旧版作对照；模型、Rig、动画、场景布局和战斗数值没有因画风重做而改变。操作手感修改另见 `GameplayFeel-Results-20261109.md`。

## 美术方向与旧版问题

旧版在合成画面上使用屏幕网格笔触、通用方向和固定屏幕旋涡，导致沙滩、人物、海水、天空的笔触逻辑接近，镜头运动时缺少表面关联。旧 `DarkFairytale.shader` 的 VanGoghColor 分支完整保留，未改写；新模式走独立路径。

参考原作及本次转译规则（规则属于美术分析，不是论文对本项目效果的保证）：

- [《星月夜》，MoMA](https://www.moma.org/collection/works/79802)：用方向组织大块天空结构；保留冷暖关系，避免把所有白天环境强制改成夜景。
- [《麦田与柏树》，National Gallery](https://www.nationalgallery.org.uk/paintings/vincent-van-gogh-a-wheatfield-with-cypresses)：地面较横向、植物较直立，云层沿形体改变方向，使用不同尺度。
- [《鸢尾花》，Getty](https://www.getty.edu/projects/van-goghs-irises-a-closer-look/)：短促相邻色块、植物轮廓和颜料表面层次。
- [《播种者》，Van Gogh Museum](https://www.vangoghmuseum.nl/en/collection/s0029V1962)：暖光与偏冷阴影形成对照；保留照明关系，再作色彩表达。
- [厚笔触研究，Van Gogh Museum](https://unravel.vangogh.com/en/story/29/thick-brushstrokes)及[Met 馆藏](https://www.metmuseum.org/art/collection/search/436535)作为补充参考。没有把一幅原画铺到游戏屏幕上，也没有新增重复的全屏油画贴图。

技术取舍参考 [Adobe painterly stylization](https://research.adobe.com/publication/interactive-painterly-stylization-of-images-videos-and-3d-animations/)、[Texture Guided Realtime Painterly Rendering](https://isgwww.cs.uni-magdeburg.de/~stefans/npr/entry-Bhattacharjee-2006-TGR.html) 和 [Kyprianidis 等的方向滤波研究](https://www.kyprianidis.com/p/pg2009/jkyprian-pg2009.pdf)。本项目采用表面关联、局部方向分析和多尺度颜料组合，没有声称完整复现论文。

## 实际渲染结构

Unity6000.3.23f1／URP17.3／Render Graph，使用项目已安装版本的 API。

1. 在透明物体之前复制**实际不透明深度**，包含自定义地形及 AlphaTest 植被，避免只用缺失自定义物体的预通过深度。
2. 地形、建筑和静态表面由深度还原世界坐标，按表面主平面建立稳定笔触坐标。植被颜色与表面方向控制较短、偏生长方向的笔触。
3. 可见 SkinnedMeshRenderer 单独写入 UV／稳定身份缓冲。原 Renderer 提供实际骨骼变形；不替换原材质，不修改 Renderer 的 MaterialPropertyBlock、模型或 Rig。这个缓冲使身体笔触跟随形变，而不是只依靠角色所在世界位置。
4. 半分辨率结构张量提供局部图像方向。以稳定表面方向为主，按 Temporal Stability 混入少量瞬时图像方向。各尺度的抖动中心、长宽、颜色和毛刷纹理均由表面格点确定，不按帧重抽随机数。
5. 不同尺度的弯曲、断续笔触采样邻近颜料色，使用深度和身份拒绝跨轮廓取色，保留攻击动作和身体边缘。色块抽象使用平滑过渡，厚涂使用笔触内部的局部起伏明暗。轮廓使用深度二阶差分，避免把整片斜坡当作轮廓压黑。
6. 海水在自身 Shader 内使用原始波面坐标、风向和波浪法线；天空使用方向／球面坐标，并沿云层 Alpha 轮廓调节方向。两者保留原来的三维波浪、云层、光照和昼夜变化。
7. UI 和透明战斗效果维持原有绘制顺序；切出梵高后不再提交这些新 Pass，关闭专用 Shader keyword，并释放每个角色的临时锚定材质。

新版不做历史颜色累积，没有运动矢量重投影，也没有由历史积累引入的拖影。稳定性首先来自世界／UV／天空方向关联；这不等于所有近景、UV 接缝和动态植被都已被证明绝对无闪烁。新出现的骨骼 Renderer 会在最多约1秒的稀疏缓存扫描内加入 UV 路径，之前仍由通用绘画路径着色。

## 独立调节

原生创建的 `Assets/Resources/VisualStyles/VanGoghPainting.asset` 是独立配置。Inspector 修改通过修订号刷新已开启的模式，无需每帧重复写全部参数。现有界面只调整梵高自己的四个标签／控件含义，其余画风界面没有重做。

| 参数 | 默认值 | 实际作用 |
|---|---:|---|
| Brush size |.32m|基准表面笔触宽度，再受梵高界面滑块调节|
| Density |.82|按稳定格点选择笔触覆盖，并调节表现强度|
| Direction strength |.80|表面／局部方向组织程度|
| Curvature |.35|单笔弯曲与低频走向变化|
| Color abstraction |.65|平滑色块抽象|
| Color contrast |.45|颜料明暗对比|
| Impasto |.38|笔触内部的细微起伏明暗|
| Outline |.28|体积／轮廓处的克制强调|
| Temporal stability |.90|稳定表面方向相对图像方向的权重|
| Overall intensity |1.0|与原 Style Changer 总强度相乘|
| Quality |Balanced|Economy／Balanced／Fine 分别使用1／2／3层不透明笔触|

10个独立参数都通过固定镜头的像素变化检查，Inspector 刷新路径也经过验证。旧梵高控件偏好保留在原键，新控件使用 `.Painting2.Detail`；其他模式的偏好键没有改变。

## 实景验证及对照

证据位于本地忽略目录 `.codex/van-gogh-2`。其中有私有存档快照，**不要把整个目录上传**。

- `accepted-native1080`：真实第一岛、内部渲染比例1.0，1920×1080，4× MSAA。同一冻结镜头／世界时间分别捕获写实、旧梵高、新梵高；覆盖海边 Sahur、天空海浪、树木、草地、卡布奇诺。此前 iteration1/2 使用项目原有0.6比例，保留为迭代记录，不冒充原生1080p。
- `original-legacy-new.jpg`：最终同机位三列对照；每行从左到右为原版／旧梵高／新版。
- 切换往返检查：Dark Fairytale、Origami、Monochrome、Ink Wash 在接受记录中逐像素一致，专用 keyword 均关闭。写实有57个像素出现最大1级8位色阶差异（约0.0027%画面）；较早的无梵高重复捕获对照也曾出现同级差异。没有观察到画风或参数残留，但不宣称写实每次捕获都位精确一致。
- `motion-final`：1280×720原生、固定60Hz逻辑。先以120°/s绕真实 Boss 转镜头3秒，再运行真实 AI20秒。现有血量阈值触发一次过渡，进入第二阶段并实际运行 CremaCyclone、MochaDive、ShadowChain；较早 motion1 还覆盖 CoffeeTempest。两次均无运行错误。使用运行期受保护的站立目标，**不是玩家完整击杀 Boss 的录像**。
- `camera-and-boss-final.mp4`：最终345帧按15fps采样的镜头／Boss片段，无音轨；审阅了关键动作、腾空与挥刀画面。`moving-blades.mp4` 是真实输入驱动的移动双刀回放，保留攻击且无运行错误。晨间、黄昏和夜间截图已检查，白天未被强制改成夜景。
- 连续10次进出风格后，锚定 Shader 材质数每次均回到1个持有的模板，没有逐次累积角色材质。

已删除793张仅用于编码的中间逐帧PNG，回收约1029MiB；完整视频、选定关键帧、同机位原图、数值记录和回退源文件保留。需要重新编码全部无损帧时，使用新的 take 标签重新录制，不要把已经精简过的 film 目录当作连续序列。

## RTX4060 性能实测

实际 RTX4060／D3D11，Editor 中固定真实 Boss 镜头，1920×1080目标、内部比例1.0。性能目标 RT 为单采样，管线资产原设4× MSAA；另有上述实际4× MSAA功能测试，不能把单采样性能数字冒充4×性能。每档预热45帧，再采样60帧。

| 配置 | 独立绘画合成 GPU 中位数 | 身体锚定＋张量 | 整相机 GPU 中位数 |
|---|---:|---:|---:|
| 写实（前） |—|—|4.72ms|
| 旧梵高 |旧路径|—|4.75ms|
| Economy |.98ms|约.02ms|5.26ms|
| Balanced |1.41ms|约.02ms|5.38ms|
| Fine |1.74ms|约.02ms|5.41ms|
| 写实（返回） |—|—|4.15ms|

数据来自最终 `performance-final/result.json`，使用原生 GPU ProfilingSampler；整相机采样器覆盖该测试相机的 GPU 工作。FrameTimingManager 的 Editor全局帧值未能可靠对应此离屏相机，因此没有用于性能结论。GPU 时钟及运行负载使前后基线不同，不能简单相减后宣称一个精确、固定的总增量或发布版 FPS。绘画部分约1.43ms是可分离的新 Pass成本，**不包含深度复制及水／天 Shader 内新增工作的全部成本**。

单采样逻辑工作纹理共37,324,800字节（35.60MiB）。Unity图形驱动分配估计值从约1776.18MiB升至1811.77MiB，退出后约1776.17MiB；这是驱动分配估计，不是独立的显卡驻留显存计数器。实际4×目标的逻辑纹理约59.33MiB，额外 resolve／驱动池开销不包含在这个逻辑值内。没有以理论纹理字节伪称实际驻留显存。

建议默认 Balanced；这次保留用户原画质设置和0.6渲染比例，没有为了截图永久提高全游戏负载。低比例仍能使用此模式，但细笔触自然会减少。未做发布版帧率、移动平台或全部相机堆叠组合的承诺。

## 完成状态与限制

原生C#编译通过，新 Shader 无编译错误；原海水／天空已有的 SailingFoam、CloudDensity、ShadowDensity 潜在未初始化警告仍在，没有借此扩大修改范围。编辑器已停止，原先干净的 Second Island 场景设置恢复，正式存档字节未变，测试时钟／镜头／画质设置已释放或还原。没有新建 EXE 或推送远程；Downloads 里的旧包不包含本次修改。

实现、对照和上述技术检查完成。艺术偏好及极端镜头仍需玩家判断；不宣称已获得人工最终美术批准、零纹理游泳或完整全 Boss 流程通关。尤其是小型刚性附件使用世界表面路径，风吹网格没有逐叶专属UV缓冲；新角色缓存等待和UV接缝是后续视觉回归时应留意的位置。原模型／场景本身的远景边界或植被 LOD 未通过改模来遮掩。

复现入口：`BuildVanGogh2.cs`、`ReviewVanGogh2.cs`、`ReviewVanGoghMotion.cs`、`VerifyVanGoghControls.cs`。使用已有隔离存档 Play helper，先加载真实岛屿再调用验证；不要重新运行会覆盖旧 before 截图的基线工具。旧源文件备份仍在忽略目录 `before/Assets`，新旧切换对照使用不持久化的 LegacyComparison。
