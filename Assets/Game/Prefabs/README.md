# 游戏预制件目录

这里集中存放游戏使用的预制件，按类型分类：

- `Characters/Sahur/`：Sahur 预制件、模型、动画、贴图、材质和专属脚本。
- `Bosses/Nailong/`：奶龙预制件及其专属资源和脚本。
- `Environment/Trees/`：各类树木；同一树种的预制件变体放在 `Variants/`，共用模型和贴图放在 `Shared/`。
- `Environment/Grass/`：草的各个预制件变体分别位于 `Variants/<预制件名>/`，共用资源在 `Shared/`。
- `Environment/Rocks/`、`Environment/Props/`：岩石和场景道具。
- `Environment/Shared/`：多种环境预制件共同使用的材质、碰撞数据和脚本。
- `UI/SettingsMenu/`：暂停与设置界面预制件，以及它自己的脚本。

新增预制件时，先选类别，再给预制件建立独立文件夹。仅它使用的资源放进自己的文件夹；多个变体共同使用的大模型、贴图和脚本放到同系列的 `Shared/`，不要复制出多份。第三方包与 TextMesh Pro 示例保留在原位置。

尚未确定归属的独立模型暂放在 `Assets/Game/Shared/Models/`，确认用途后再归入对应预制件目录。
