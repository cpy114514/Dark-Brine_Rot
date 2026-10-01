# Cappuccino Assassino

将 `CappuccinoAssassino.prefab` 拖入场景，根节点放在地面上即可。模型、身体与双刀的 URP 材质、实体碰撞和头顶血条已连接。默认高度 5.4 米，与当前 First Island 中 scale=3 的 Sahur 接近。

## 目前功能

- 使用现有 `Mavis.Health : IDamageable` 和 Enemy 标签，接收 Sahur 的挥棒、连击与落地攻击。
- 默认血量 300，头顶血条显示剩余数值，面对主摄像机；超过 65 米隐藏。
- 受击短暂闪色、后仰，血量耗尽时倒下，关闭碰撞及血条，4 秒后移除。
- 目前是可攻击的敌人基础，尚未加入追击、敌方攻击、掉落或 Boss 阶段。

## 可调整与扩展

- `Health` 的 `maxHealth` 和 `currentHealth`：初始最大血量与当前血量；需要满血开局时同时设置。
- `Health.OnDamaged`、`Health.OnHealthChanged`、`Health.OnDeath`：可连接后续音效、任务、掉落。
- `CappuccinoEnemy.onDefeated`：角色被击败事件。
- `CappuccinoEnemy.removeAfterDeath`：设为 0 保留尸体；正数为移除延迟。
- `CappuccinoCombatDemo.unity`：含当前 Sahur 的独立试打场景；其中的玩家带 `GameSaveExcluded`，不会写入正常游戏存档。

## 来源

原始压缩包中的 USDZ 和全部 7 张贴图均保留。USDZ 没有骨骼及动画轨道；转换生成 GLB，保留 20,832 个顶点、32,580 个三角形、法线、UV、变换及两种材质绑定。为 URP 转换了金属度/光滑度贴图。双刀的基础颜色采用 USDZ 内嵌版本，压缩包中提供的另一版本也保留。
