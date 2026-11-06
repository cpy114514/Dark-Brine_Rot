# 首次启动自动应用推荐配置

新玩家第一次启动游戏时，标题场景的设置组件会自动读取、保存并应用推荐配置，不需要先打开设置窗口。完成标记为 `DarkBrine.Settings.RecommendedDefaultsV1`，保存在同一系统用户的 PlayerPrefs 中；后续启动读取玩家自己的设置，开始新游戏不会重置配置。

默认采用远景优先预设（后续按用户要求增加可见距离、降低其他开销）：

| 设置 | 推荐值 |
| --- | --- |
| 帧率上限 | 60 FPS |
| 垂直同步 | 关闭，以使桌面端帧率上限生效 |
| 渲染比例 | 75% |
| 抗锯齿 | FXAA |
| 阴影贴图 / 距离 | 1024 / 60 米 |
| 纹理 mipmap 限制 / LOD bias | 2 / 1.5 |
| 后处理 / 阴影 | 保留 |
| Bloom / 暗角 / 动态模糊 / Pixel Style | 关闭 |
| 分辨率 / 全屏 | 沿用首次启动时的显示状态 |

这是固定的项目推荐预设，不是硬件自动跑分或对所有设备 60 FPS 的保证。桌面端启用 VSync 时会优先按刷新率同步，因此默认关闭 VSync 来落实 60 帧上限；玩家仍可以在菜单手动开启。[Unity VSync 说明](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/QualitySettings-vSyncCount.html)

草地空间淡出范围由 140–176 米延长为 200–240 米，CPU 驻留上限由 190 米提高到 260 米；树木外层剔除上限 550→750 米，旧植被外层上限 260→350 米。具体显示距离还受原有 LOD、包围盒、视角和遮挡限制，详见 `DistancePrioritySettings-20261102.md`。

旧玩家即使没有新标记，也会先读取已有设置，再保存解析后的配置；不会被新默认值覆盖。旧的 LowGraphicsDefaultV1 / ShadowDistanceV2 / ShadowDistanceV3 强制迁移逻辑已移除，避免覆盖旧玩家的自定义画质和 50 米等合法阴影距离。无效存储值继续使用原有校验和回退规则。

初始化和菜单的“应用”使用同一个保存方法。按键、语言和游戏存档不参与初始化。DX11 是之前修复中设置的 Windows 默认图形 API，本功能不在运行时切换 DirectX。

真实标题场景还暴露了相机设置遗漏：Menu Camera 没有 MainCamera 标签，也没有预先配置 URP 附加相机数据。设置应用现在在标题模式下识别该相机，并使用 URP 提供的 GetUniversalAdditionalCameraData 创建缺失的运行时数据，使抗锯齿和后处理配置实际生效。场景与 Prefab 文件没有修改。

## 验证

- Unity 6000.3.23f1 编译通过，编译错误为零。
- 21 项原生配置检查通过，覆盖首次初始化、持久保存、重复读取不重置、旧版自定义设置保留、无效数值回退，以及语言和按键不被修改。
- 当前远景预设的真实标题场景首次启动 15 项检查通过：未打开设置窗口时，运行时渲染比例为 0.75、帧率上限 60、VSync 为 0、阴影贴图 1024、阴影距离 60、LOD bias 1.5、纹理限制 2、FXAA 已应用，首次标记已保存。
- 通过实际 ApplyDraft 菜单回调保存 120 帧、自定义渲染比例 1.1 和阴影距离 55，再退出并重新进入 Play Mode，3 项检查确认自定义值恢复且不会再次初始化。
- 两次早期标题相机验证未通过，分别暴露未标记相机和缺失 URP 数据的问题；修复后完成上述最终验证，不将早期结果计为通过。
- 测试后原 PlayerPrefs、五个场景工作区与存档内容均已核对恢复。已知植物 Prefab 缺失日志仍在，不属于本功能新增问题。本轮没有创建或替换独立 Player 安装包。

实现：`Assets/Game/Prefabs/UI/SettingsMenu/Scripts/PauseSettingsMenu.cs`。显式测试工具：`Tools/VerifyFirstRunSettings.cs`（位于 Assets 外，不随游戏打包）。最新本地证据位于 `.codex/far-view-settings-20261102/`，包括 `fixture-result.json`、`fresh-launch.json`、`returning-launch.json` 和 `state-integrity.json`。最初 85% 预设的历史证据保留在 `.codex/first-run-settings-20261102/`。
