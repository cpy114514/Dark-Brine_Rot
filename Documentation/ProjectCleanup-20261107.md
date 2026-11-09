# 项目清理与资源完整性检查 — 2026-11-07

本轮以当前整个 Unity 工作为范围，执行只读盘点、可再生成文件清理、已确认损坏资源修复和维护入口整理。遵守 COLLABORATION.md；未删除、搬迁或重命名游戏资产、原始美术、恢复场景、备份、试玩截图/视频及 `.codex` 历史证据，也未清空正在使用的 Library/Temp。没有更改玩法、关卡、角色动画、画质、插件或 Unity 版本。

## 实际修改

| 文件 | 问题与处理 |
| --- | --- |
| `.gitattributes` | SahurSupport 的 PreferBinarySerialization 资产继承了 Unity YAML 的 LF 规则。增加该目录的 binary / 非文本规则，清除 eol 与 YAML 语言标记；现有二进制 CombatTest.unity 同样加入精确路径例外。其余 YAML 资产与现有 LFS 规则保持原样 |
| `Assets/Resources/SahurSupport/SoleSkin_a014b4d32ef6c454db25d9d90d6814ae_v126.asset` | 原生加载失败，实际 3,400,268 字节而头部声明 3,400,288 字节。备份后，按原生成器的同一 mesh、骨骼索引及 RefreshSahurSoleSkinCache 算法，通过 CreateAsset / 原生保存重建。GUID `c887f626f5c460246898a25609ff22c3`、local ID `11400000` 均保持不变 |
| `Assets/Resources/SahurSupport/SecondIslandSahurSoles.asset` | 本地原本有效，原生重新保存后的文件逐字节不变。Git HEAD 中的版本也被缩短了 20 字节；新属性使 Git 能按正确的原始二进制字节记录它，因此现在会显示为修改。未重建其数据 |
| `Tools/AuditProjectCleanup.cs` | 保留既有 root-only `runtimeResources` 字段兼容旧比较，新增 `nestedResources`，补查此前漏掉的 52 项嵌套 Resources |
| `Tools/InspectProjectIntegrity.cs` | 新增只读检查：已打开场景的缺失 Prefab/脚本、空材质槽状态、未识别序列化资产、所有 Resources、构建列表、StreamingAssets 和 AssetBundle 名称。不开关或保存场景 |
| `Tools/ClearToolPythonCaches.ps1` | 新增默认 Preview 的限定清理入口；实际 Apply 删除 38 个有对应 `.py` 源码、Git 未跟踪的 Python 缓存，571,075 字节，约 0.54 MiB。验证绝对路径、链接、源码、缓存哈希后逐文件移除；保留清单；不递归删除目录 |
| `Tools/README.md`、`README.md` | 整理现行维护/内容入口和历史工具用途；修正项目介绍的第三人称说明与不完整的切武器表格。依据现有 ThirdPersonPlayerController 和 SahurWeaponLoadout 数字键输入 |

Git 的 binary 属性会禁用文本换行转换与文本合并；这是本次规则修正的依据，见 [Git 官方属性说明](https://git-scm.com/docs/gitattributes)。Unity 资产重建使用 [AssetDatabase 原生接口](https://docs.unity.com/en-us/engine/6000.3/script-reference/unityeditor/assetdatabase/createasset)。

## 损坏原因的可复核证据

重建文件正好为头部声明的 3,400,288 字节，包含 20 个 CRLF 序列。**仅在内存中**把重建文件的 CRLF 转成 LF，结果与备份的坏文件逐字节完全相同；没有手动修补或编辑 Unity 二进制数据。原始 mesh、279,411 个源顶点、70,700 个脚部顶点、53 个 bind pose 以及骨骼索引 17/21/18/22 均保留。两份缓存的 Git 过滤后 hash 已等于其原始字节 hash，证明新规则不再转码。

两份缓存和 `.gitattributes` 应一起进入后续用户提交；本轮没有暂存、提交、推送或重写 Git 历史。

## 目录职责与保留原则

| 目录 | 当前职责与处理 |
| --- | --- |
| `Assets/Game`、`Assets/Scripts`、`Assets/Scenes` | 游戏对象、运行时代码和关卡；保留现有组织与所有用户改动 |
| `Assets/Resources` 与嵌套 `Resources` | 动态加载入口；不能仅凭场景依赖搜索删资源 |
| `Assets/Generated`、`Assets/AnimationStaging` | 已生成的在用内容及动画管线输入；不是通用缓存。修改入口见 Tools/README.md 和环境维护说明 |
| `Assets/Editor`、`Tools` | Unity 编辑工具与外部构建/验证流程；带版本号不代表无用，不批量运行旧生成器 |
| `ArtSource`、`BlenderWork`、`ConceptArt` | 原始制作文件与美术过程，保留；`.blend1` 也作为用户恢复副本保留 |
| `Assets/_Recovery`、`Assets/Temp`、修复备份目录 | 恢复/历史内容，保留；审计未发现常规资产对这些备份的依赖，但不据此删除 |
| `.codex`、`AUTO_QA`、`Documentation`、宣传片 out | 测试证据、记录和交付内容，保留。仅第一岛试玩目录约 32.98 GiB，是本轮盘点中最大的单个子目录 |
| `Library`、`Temp`、`Logs`、`UserSettings`、`.git`、`.plastic` | 编辑器和版本管理工作数据；当前会话正在使用，不清空、不改工作区元数据 |

本轮未证明存在可安全删除的游戏资源，资源删除数量为 0。释放空间较少；主要成果是修复实际二进制损坏、补齐审计覆盖及明确可重复使用的维护入口。

## 检查与验证

- 原生资源审计前后：4,031 个资产条目、8 个启用的构建场景、2,083 条场景依赖路径/GUID、891 条根 Resources 路径/GUID 完全一致。新增覆盖的 52 项嵌套 Resources 包含 Nailong 装备和 TMP 示例资源，保留原路径/GUID；全部 Resources 入口共 943 项。
- 4,690 个 metadata GUID 无重复，无孤立 `.meta`。136 个无 `.meta` 文件均为 `.animation-pipeline-owned` 隐藏所有权标记，属于管线协议，予以保留，不当作丢失 metadata。
- 4 份原生二进制序列化文件已盘点；最初仅旧脚底缓存长度异常，修复后长度均匹配。当前未识别的 Unity 序列化资产数量为 0。
- 当前第一岛五场景检查：缺失 Prefab 0、缺失 MonoBehaviour 0。旧报告中列出的三个草 Prefab GUID 当前均能解析到 Assets/Game/Prefabs/Environment/Grass/SimpleGrassChunks 下的原资源，本轮未替换或删除它们。
- 两个空材质槽分别是 Lighting 的禁用 `Procedural Sun` Renderer 与 Environment 中 inactive 的 `Sunset Island (1)`；不参与当前可见渲染，保留原状态，不声称修复了两个画面故障。
- 缓存原生强制重新导入通过，现有构建前缓存刷新入口通过，Resources.LoadAll 能加载两份缓存。实际第一岛 Play 运行到第 1,795 帧，Sahur 存在，两份缓存均加载成功，随后退出 Play。
- Test Runner 检出 0 个 EditMode 用例，未声称所有自动测试通过。未生成新的独立 Player、完整通关、重开整个 Editor或执行 90 分钟压力测试；这些仍未验收。
- 最终 8,887 个受保护文件核验：仅本轮重建的旧脚底缓存字节变化，无文件缺失；第二岛缓存逐字节不变，正式存档及 `.bak` 保持原样。五个打开场景的 root 数、active 场景和 clean 状态保持一致，无 Prefab Stage，Play 已退出，测试存档路径覆盖恢复 null。
- Play 区间日志：运行时 Exception 0、Ring Buffer 0、脚底缓存加载失败 0；保留 2 条 CLI 初始化连接超时错误，不把控制台描述为绝对零错误。
- 冷缓存的首次全资源审计超出 30 秒 CLI 响应期限，但 Editor 后续完成并写出报告；Play 初始化期间还发生过连接超时。操作失败记录保留，不将其当作游戏 Bug，也不以超时响应宣称测试通过。清理 helper 初版路径祖先检查在删除前停滞，已停止自己的 helper 并修正，最终 Apply 清单独立保存。

本轮原始数据在 `.codex/project-cleanup-20261107/`，依赖列表在 `.codex/cleanup/dependencies-20261107-cleanup-*.json`；当前工作区检查在 `.codex/project-integrity/`。入口清单见 [Tools/README.md](../Tools/README.md)。最终文件保护和日志核验见本轮 `verification.json`、`final-workspace.json`、`play-log-result.json`。
