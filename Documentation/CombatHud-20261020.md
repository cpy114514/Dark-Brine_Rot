# 战斗 HUD 精简（2026-10-20）

交战时隐藏左侧岛屿 Boss 任务列表及完整武器快捷键说明；脱战时自动恢复。当前敌人血条、玩家生命与体力继续使用原有 HUD。装备菜单仍可打开，任务进度逻辑在折叠期间继续运行。

战斗状态复用 BossHudPanel 的现有显示请求与当前敌人选择，不额外扫描场景。短暂的击败展示结束后恢复探索信息；停用、销毁的敌人不会让 HUD 持续折叠。地图、暂停、死亡等界面继续遵循原有显示规则。

修改文件：
- Assets/Game/Prefabs/UI/BossHudPanel.cs
- Assets/Game/Prefabs/UI/SahurLoadoutUI.cs
- Assets/Scripts/Story/FirstIsland/FirstIslandObjectives.cs

验证：Unity 编译通过。在 First Island Play Mode 使用真实任务、装备、生命体力组件，以临时 encounter panel 驱动状态切换，Tools/VerifyCombatHud.eval.cs 的 10 项断言全部通过，覆盖探索、交战、装备菜单、敌人停用、重新交战、结束和销毁。此验证为受控运行时状态测试，不是完整三 Boss 实战流程。

结果：Temp/CombatHud-20261020/verification.json。测试后退出 Play Mode，恢复原始主存档、备份存档和治疗包 campaign 设置；两个存档逐字节一致，五个场景均无未保存修改。没有修改场景或生成新构建。
