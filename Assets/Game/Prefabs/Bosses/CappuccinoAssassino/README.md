# Cappuccino Assassino

> **动作/特效第二轮打磨：** 已接入新下载的 KayKit CC0 双持素材，修正新源握刀手骨骼映射，加入惯性衔接、技能入场混合、连续刀刃网格带、柔边蒸汽和轮廓残影。新版本位于 `Resources/CapriPhase2/Polish`；重建请使用 `Tools/BuildCapriMotionPolish.cs`。录像与本轮验证见 `Documentation/CappuccinoPolish-20261108.md`。

> **2026-11-08 更新：** 第二阶段已升级为独立四技能与 4.2 秒专用转换动画。完整时序、资产和原生测试见 `Documentation/CappuccinoPhase2-20261108.md`。第一阶段保留；新技能为瞬影连斩、范围回旋、俯冲及可跳跃冲击波、延迟收尾双刀组合。新动画/URP 网格效果/原创音频位于 `Assets/Resources/CapriPhase2`，动作实现为 `CappuccinoAI.PhaseTwo.cs`。

将 `CappuccinoAssassino.prefab` 拖入场景，根节点放在地面上即可。模型、身体与双刀的 URP 材质、实体碰撞和头顶血条已连接。默认高度 5.4 米，与当前 First Island 中 scale=3 的 Sahur 接近。

## 目前功能

- 使用现有 `Mavis.Health : IDamageable` 和 Enemy 标签，接收 Sahur 的挥棒、连击与落地攻击。
- 当前最大血量固定为 999；血条由现有 Boss HUD 与头顶血条管理。
- 主动发现和追击 Sahur，挥刀造成伤害；受伤、被贴身或低血量时短暂后退再重新进攻。前进和后退均使用行走动画，沿地形移动并避开障碍。
- 受击短暂闪色和后仰；血量耗尽时播放死亡动画，关闭碰撞及血条，完整播放后移除。
- 大招 **Caffeine Surge**：50% 血量时每场触发一次。先播放 4.2 秒专用全身转换动画，再进入第二阶段；保留 5 秒强化窗口后的 200 点回血与 3 秒行动锁定。生命和伤害均不翻倍。新回旋与俯冲使用咖啡色范围轮廓，第一阶段原有预警策略不变。

## 可调整与扩展

- `Health` 的 `maxHealth` 和 `currentHealth`：初始最大血量与当前血量；需要满血开局时同时设置。
- `Health.OnDamaged`、`Health.OnHealthChanged`、`Health.OnDeath`：可连接后续音效、任务、掉落。
- `CappuccinoEnemy.onDefeated`：角色被击败事件。
- `CappuccinoEnemy.removeAfterDeath`：设为 0 保留尸体；正数为移除延迟。
- `CappuccinoEnemy.attackOnHit`：独立受击反击开关；AI 开启时由 AI 控制攻击时机，关闭此直接反击。
- `CappuccinoAI`：追击速度、发现范围、攻击范围、每刀伤害和后退行为。
- `CappuccinoUltimate`：触发血量比例、转换时长（至少 4.2 秒）、5 秒强化窗口、3 秒行动锁定和回血量。旧倍率字段只为兼容序列化保留，不再生效；被击杀不会复活。
- `CappuccinoEnemy.PlayAttack()`：供后续敌人 AI 调用；Play Mode 中也可从组件菜单选择 `Play Attack`。
- `Animations/CapriCombat.controller`：Idle、Walk、Retreat、Attack、AttackMirrored、Death 状态。Attack 使用 `Sword And Shield Slash.fbx`（1.5 秒），Death 使用 `Sword And Shield Death.fbx`（3.9 秒）；死亡不会返回 Idle。
- `CappuccinoMotionRig`：14 个按杯身与四肢位置拟合的骨骼。杯身及刀刃保持刚性，原贴图和两把刀保留；人体动作通过隐藏的骨骼源转移到 Capri，并校正脚底与刀刃的地面穿透。
- `CappuccinoCombatDemo.unity`：含当前 Sahur 的独立试打场景；其中的玩家带 `GameSaveExcluded`，不会写入正常游戏存档。

## 来源

原始压缩包中的 USDZ 和全部 7 张贴图均保留。USDZ 没有骨骼及动画轨道；转换生成 GLB，保留 20,832 个顶点、32,580 个三角形、法线、UV、变换及两种材质绑定。现另存蒙皮网格并加入拟合骨骼，原 GLB 不变。为 URP 转换了金属度/光滑度贴图。双刀的基础颜色采用 USDZ 内嵌版本，压缩包中提供的另一版本也保留。
