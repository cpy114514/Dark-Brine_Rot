# 青玉猪浪人 / Jade Pig Ronin

基于用户指定的肥嘟嘟左卫门方向制作的 3D 改编模型，已绑定 30 根骨骼并完成蒙皮。保留宽脸、粗眉、双孔猪鼻、短胖身形、叉腰姿态和武士佩刀；重新设计青玉色短羽织、铜橙腰带、深紫灯笼裤、背部纹章、编绳刀柄和木屐。

## 文件

- `PigRonin.blend`：可编辑的绑定源文件，打开时选中 Rig 并处于 Pose Mode。角色和摄影棚分别放在独立 Collection 中。
- `PigRonin_Unrigged.blend`：绑定前的模型快照，保留原始减面修改器，方便后续修改外形。
- `PigRonin.glb`：带骨骼、权重、材质和测试动作的通用模型。
- `Assets/Game/Prefabs/Characters/PigRonin/Models/PigRonin.fbx`：带骨骼及蒙皮的 FBX 模型。
- `Assets/Game/Prefabs/Characters/PigRonin/PigRonin.prefab`：已配置 URP 材质、Generic Avatar 和 Animator Controller 的 Unity 预制体，默认播放叉腰姿态。
- `PigRonin_Unity.unitypackage`：包含模型、材质、蒙皮网格、预制体和控制器的 Unity 导入包，需要 URP。
- `Assets/Game/Prefabs/Characters/PigRonin/PigRonin_Static.prefab`：保留的旧版静态预制体。
- `PigRonin_Hero.png`、`PigRonin_Front.png`、`PigRonin_Back.png`、`PigRonin_Side.png`：Blender 实际模型渲染图。
- `PigRonin_Unity.png`：Unity 预制体实际渲染图。
- `PigRonin_BindTPose.png`、`PigRonin_RigCheck.png`：绑定中立姿势和变形测试图；带 `Unity` 前缀的对应图片来自 Unity 实际渲染。

## 使用说明

- Unity 预制体约高 1.73 米，脚底为坐标原点附近，正面朝向 +Z；原始 FBX/GLB 高约 3.15 个单位。
- 约 10.4 万三角面，16 种无贴图依赖的材质。Unity 版本合并为一个 SkinnedMeshRenderer、16 个子网格。
- 30 根 FK 骨骼覆盖根节点、骨盆、脊柱、胸部、颈部、头部、肩臂、手与拇指、腿脚、脚趾，以及独立耳朵、三节卷尾和佩刀。每顶点最多 4 个归一化权重，没有未绑定顶点。
- 骨骼以 T 姿势绑定。原先弯曲手臂的关节区域已重建为均匀环线，适合弯伸。Blender 中旋转 Rig 的 Pose Bones 可直接摆姿势。
- 包含三个动作：`HandsOnHips`（原叉腰展示姿态，Unity 默认状态）、`BindTPose`（T 姿势）、`RigCheck`（抬手、屈膝、转头及配件随动测试）。在 Unity 的 Animator 中切换对应状态可检查动作；在 Blender 的 Action Editor 中选择同名 Action。
- Unity 当前使用 Generic 骨架；尚未制作行走、攻击等正式动作，也没有 IK 控制器、表情骨骼、碰撞体或游戏行为。
- 展示底座、灯光和摄影机仅存在于 Blender 的 STUDIO Collection，FBX、GLB 和 Unity 预制体中不含底座。
- 所有着色依赖材质参数；在不同灯光和色调映射下，颜色会略有区别。

## 再生成

在项目根目录用 PowerShell 执行：

```powershell
& '.\Tools\blender\blender.exe' --background --factory-startup --python '.\ArtSource\Characters\PigRonin\build_pig_ronin.py'
```

以上脚本会在生成基础外形后调用 `rig_pig_ronin.py` 完成骨骼、权重和导出。若只需重新绑定已有模型，可单独运行同目录的 `rig_pig_ronin.py`。

在 Unity 连接中运行 `Tools/PigRonin/BuildRiggedPigRoninPrefab.cs` 的 `BuildRiggedPigRoninPrefab.Main` 可重建蒙皮网格、动作控制器、预制体和导入包。脚本使用独立预览场景，并比较合并前后在三个动作下的烘焙顶点，检查绑定一致性。旧的 `BuildPigRoninPrefab.cs` 仅用于静态模型，遇到带骨骼源文件时会拒绝覆盖。

角色特征参考：[朝日电视台角色介绍](https://www.tv-asahi.co.jp/ch/recommend/shinchan_buriburi/)。
