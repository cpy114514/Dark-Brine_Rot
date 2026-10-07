# 游戏逻辑目录

- `Story/Chapter1/`：航船、鲨鱼、碎船木板战斗和结尾剧情。
- `Story/FirstIsland/`：漂流上岸、昏迷起身和寻找棍子。
- `Ships/`：航行、船上移动、梯子、摄像机和拖尾。
- `Systems/`：存档、存档排除标记和全局音效策略。
- `World/`：岛屿场景的分批加载。
- `Performance/`：植被模型库、运行时细节和批量绘制。
- `Effects/`：通用自然特效。
- `Combat/`、`Enemy/`：战斗辅助、锁定、复活和普通敌人。

角色、Boss 和 UI 专属脚本继续放在 `Assets/Game/Prefabs/` 的对应目录中。
通过 Unity 移动资源时保留 `.meta` 和 GUID；不要在整理文件时重新生成它们。
修复备份与测试截图放在项目外，避免被 Unity 当作正式资源导入。
