# Boss 死亡动画

Cappuccino 沿用原死亡片段。旧的接地代码从骨架节点找 Renderer，实际杯身网格位于骨架旁边，因此没有启用模型接地修正。现在从实际身体 Renderer 取支撑点，在倒地过程中跟随坡面。原片段和重定向在最终姿态稳定后停止更新。

Nailong 和猪武士用各自的倒地节奏。Nailong 先失衡，再由躯干带着身体倒下。猪武士的膝部先卸力，身体更快侧倒，原本的视觉落刀继续与角色分开。两者保留致死瞬间的姿势，以较低支点完成倒地，通过原有模型支撑点贴合地面。

在当前编辑器 Play 中用隔离 Prefab 复现，三个 Boss 各测试平地和 10° 斜坡，共渲染 66 帧。六组致死回调各一次，尸体碰撞均关闭，修复后采集期间记录到零 Error 或 Exception。测试中的 Cappuccino 斜坡最低模型采样点离地由 0.338 m 降至 0.065 m。Nailong 和猪武士的最终采样间隙约 0.020–0.028 m。该采样覆盖所记录的死亡方向和地形。

真实伤害、掉落和剧情仍从原有路径调用。测试副本避免产生可被当前玩家拾取的测试掉落。飞机抓人分支未纳入这轮动画调整，完整剧情与斜坡之外的接触仍待验收。

[Cappuccino 预览](../.codex/boss-death-polish-20261108/Cup-preview.gif) · [Nailong 预览](../.codex/boss-death-polish-20261108/Dragon-preview.gif) · [猪武士预览](../.codex/boss-death-polish-20261108/Ronin-preview.gif) · [原始验证结果](../.codex/boss-death-polish-20261108/after/review.json)
