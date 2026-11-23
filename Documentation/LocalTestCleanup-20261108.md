# 2026-11-08 本地 QA 产物清理记录

此次清理移除了 156,108 个本地文件，共 83,407,974,200 字节（77.680 GiB）。其中包括三个隔离测试项目副本、一个第一岛试玩构建目录和八组性能比较构建，以及旧截图、录像、采样和八个一次性 Boss 检查脚本。Unity 正在使用的 11 个编译日志保留。

清理后按路径、大小和修改时间核对了 11,406 个正式输入文件，缺失和变化均为零。检查覆盖 `Assets`、`Packages`、`ProjectSettings`、`ArtSource`、`BlenderWork` 和 `ConceptArt`。当天没有为此次清理重新打包或测试游戏。部分旧报告中的截图、视频和测试构建需要重新生成。

原始计数、清理计划哈希和验证记录可在当时的版本查阅。[清理结果](https://github.com/cpy114514/Dark-Brine_Rot/blob/adbe8cc794659768260f8ff7b007003fb6de6264/Documentation/LocalTestCleanup-20261108/result.json) · [验证记录](https://github.com/cpy114514/Dark-Brine_Rot/blob/adbe8cc794659768260f8ff7b007003fb6de6264/Documentation/LocalTestCleanup-20261108/verification.json)
