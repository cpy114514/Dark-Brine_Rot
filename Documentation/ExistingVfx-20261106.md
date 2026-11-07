# 现有特效打磨 · 2026-11-06

沿用现有共用特效：命中高光更短，挥击纹理随轨迹保持稳定；爆炸先展示热核，已有烟层延后渐入并提前开始消散；每次接触/烟尘对原密度场取不同偏移，减少重复。体积光照统一使用局部空间方向。

未增加发射器、粒子、网格、贴图、灯光或体积采样步数。保留 16 个接触槽、6 个地面槽、最多同时 2 个爆炸/3 个尘云，以及原伤害、招式和剧情时序。

源码：`BrineVfxWorld.cs`、`BrineSurface.shader`、`BrineThermalVolume.shader`、`BrineSixWaySmoke.shader`。

原生 Play Mode 审查使用真实 Sahur Prefab 和虚拟输入，覆盖棍轻击/蓄力/重击、双刀轻击/蓄力/重击/旋转、飞机爆炸和 Cappuccino 受挫尘土。沙盒与第一岛环境光照各通过18项检查，无记录运行/着色器错误；各403帧。攻击真实扣血、有限预算、深度/不透明缓冲恢复、播放结束/显式清理均通过。编辑器重新编译成功。

证据在 `Documentation/ExistingVfxReview/`：`sandbox-result.json`、`island-result.json`、`Island-Vfx.mp4`、`blast-before.jpg`、`blast-after.jpg`。前后爆炸图为同机位早期帧。完整原始帧与调整前源文件在 `.codex/vfx-polish/`。

早期旧夹具因剧情相机远离沙盒导致距离裁剪，另在 Boss 尘土前被独立 Camera.main 裁剪；修正测试相机后全部通过。这些夹具失败不作为游戏缺陷。`reference` 的前8段仍可用于画面对比，其末尾 Boss 尘土检查不作为通过证据。

脚本采样 P95：沙盒0.504ms，实岛0.534ms，仅共用 VFX Update；录像有显式 Camera.Render 与读回开销，不代表游戏FPS或GPU性能。停止测试后恢复原五场景，玩家存档字节一致。未改写场景、材质资源或原存档。
