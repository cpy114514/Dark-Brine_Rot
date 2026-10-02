# Cappuccino Assassino

将 `CappuccinoAssassino.prefab` 拖入场景，根节点放在地面上即可。模型、身体与双刀的 URP 材质、实体碰撞和头顶血条已连接。默认高度 5.4 米，与当前 First Island 中 scale=3 的 Sahur 接近。

## 目前功能

- 使用现有 `Mavis.Health : IDamageable` 和 Enemy 标签，接收 Sahur 的挥棒、连击与落地攻击。
- 默认血量 300，头顶血条显示剩余数值，面对主摄像机；超过 65 米隐藏。
- 主动发现和追击 Sahur，挥刀造成伤害；受伤、被贴身或低血量时短暂后退再重新进攻。前进和后退均使用行走动画，沿地形移动并避开障碍。
- 受击短暂闪色和后仰；血量耗尽时播放死亡动画，关闭碰撞及血条，完整播放后移除。
- 大招 **Caffeine Surge**：血量降到 50% 时，每次战斗自动触发一次。当前血量、最大血量和伤害翻倍，持续 5 秒；结束后恢复原最大血量与伤害、回满血，并禁止移动 3 秒。这 3 秒内仍可以原地攻击。屏幕上显示英文警告和倒计时，强化期间模型呈橙色。

## 可调整与扩展

- `Health` 的 `maxHealth` 和 `currentHealth`：初始最大血量与当前血量；需要满血开局时同时设置。
- `Health.OnDamaged`、`Health.OnHealthChanged`、`Health.OnDeath`：可连接后续音效、任务、掉落。
- `CappuccinoEnemy.onDefeated`：角色被击败事件。
- `CappuccinoEnemy.removeAfterDeath`：设为 0 保留尸体；正数为移除延迟。
- `CappuccinoEnemy.attackOnHit`：独立受击反击开关；AI 开启时由 AI 控制攻击时机，关闭此直接反击。
- `CappuccinoAI`：追击速度、发现范围、攻击范围、每刀伤害和后退行为。
- `CappuccinoUltimate`：大招触发血量比例、倍率、5 秒强化时间、3 秒移动锁定时间和英文提示开关。强化期间被击杀不会复活，倍率不会叠加；禁用组件会清除临时强化。
- `CappuccinoEnemy.PlayAttack()`：供后续敌人 AI 调用；Play Mode 中也可从组件菜单选择 `Play Attack`。
- `Animations/CapriCombat.controller`：Idle、Walk、Retreat、Attack、Death 状态。Attack 使用 `Sword And Shield Slash.fbx`（1.5 秒），Death 使用 `Sword And Shield Death.fbx`（3.9 秒）；死亡不会返回 Idle。
- `CappuccinoMotionRig`：14 个按杯身与四肢位置拟合的骨骼。杯身及刀刃保持刚性，原贴图和两把刀保留；人体动作通过隐藏的骨骼源转移到 Capri，并校正脚底与刀刃的地面穿透。
- `CappuccinoCombatDemo.unity`：含当前 Sahur 的独立试打场景；其中的玩家带 `GameSaveExcluded`，不会写入正常游戏存档。

## 来源

原始压缩包中的 USDZ 和全部 7 张贴图均保留。USDZ 没有骨骼及动画轨道；转换生成 GLB，保留 20,832 个顶点、32,580 个三角形、法线、UV、变换及两种材质绑定。现另存蒙皮网格并加入拟合骨骼，原 GLB 不变。为 URP 转换了金属度/光滑度贴图。双刀的基础颜色采用 USDZ 内嵌版本，压缩包中提供的另一版本也保留。
