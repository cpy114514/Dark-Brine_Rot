# Unity 编辑器退出问题：DX11 修复与验证

2026-11-02，America/Vancouver。使用项目锁定的 Unity 6000.3.23f1 和 Unity CLI 1.0.0-beta.15（已检查最新版本）。

## 已实施的修复

通过正在运行的 Unity Editor API 将 Windows 图形 API 的自动选择关闭，图形 API 列表设为 `Direct3D11`。保存到 `ProjectSettings/ProjectSettings.asset`，只增加 WindowsStandaloneSupport 的图形 API 条目，没有修改其他 Player 设置。

先使用 `-force-d3d11` 安全启动，再写入持久配置；随后正常关闭并重新打开编辑器，第二次启动**没有强制 DX11 参数**，原生 SystemInfo 仍报告 `Direct3D11` / NVIDIA GeForce RTX 4060。因此正常启动项目也会使用这项设置，不依赖一次性的启动参数。

这会同时让该项目的 Windows Editor 和后续 Windows Player 构建默认使用 DX11。没有升级 Unity、显卡驱动或包，没有调整游戏渲染质量、材质、动画、战斗逻辑和场景。Unity 官方说明自定义 API 列表适用于 Editor 和 Player，并按列表顺序选择：[配置图形 API](https://docs.unity3d.com/6000.3/Documentation/Manual/configure-graphicsAPIs.html)。

## 原因与范围

最近一份原始崩溃记录为 `Crash_2026-11-03_002411207/Editor.log`，对应温哥华时间 11 月 2 日 17:24：

```text
d3d12: Device removed reason (887a0006).
d3d12: GfxDevice was not out of Local memory
d3d12: GfxDevice was not out of Non-Local memory
d3d12: Unrecoverable GPU device error!
Crash!!!
```

旧 Windows Error Reporting 记录另有多次 D3D12Core.dll 异常。采用 DX11 是绕开已确认的 DX12 故障路径；尚未确定究竟由驱动、引擎还是特定渲染操作触发，不能称已修复底层 DX12 实现。[微软 DXGI 错误码说明](https://learn.microsoft.com/en-us/windows/win32/direct3ddxgi/dxgi-error)

10 月 28 日还有独立的线程创建失败：`Couldn't create thread. Error 0x5af`，后续 Mono worker_thread 崩溃。0x5AF 对应 ERROR_COMMITMENT_LIMIT，即系统提交内存上限不足，并不能仅凭异常出现在 Plastic 调用栈就认定是 Plastic 的缺陷。目前 Windows 已启用自动管理分页文件，磁盘也有可用空间；本轮没有更改系统分页文件配置。[微软系统错误码说明](https://learn.microsoft.com/en-us/windows/win32/debug/system-error-codes--1300-1699-)

## 实际验证

- 两次启动成功，其中一次是无强制图形参数的正常启动，确认持久配置生效。
- 脚本状态检查为 up_to_date，无项目编译错误；本轮工具脚本的原生内存编译成功。
- 连续三轮进入和退出 Play Mode，每轮验证真实玩家、启用的主相机、地图打开/暂停/关闭以及 DX11 状态；每轮保留 25 秒运行窗口。后两轮 settled 样本分别记录 1,254 和 1,306 帧。
- 实际相机截图 `dx11-world.png` 已检查，场景可正常渲染，没有截图可见的粉色失效材质；这不是全场景视觉验收。
- 约 359 秒的 Windows 资源采样共 72 条，编辑器 PID 始终为 62376；线程 191–207，最终 199。句柄从启动阶段的 2,556 增至 6,557，最高 6,573，末段约 6.5k；私有内存峰值约 8.79 GB。启动/导入与 Play 切换会增加资源，本次短测不能证明长期没有泄漏。
- 新编辑器日志没有 GPU 致命错误、设备丢失、Crash、线程创建失败、C# 编译错误或 Shader error 匹配。
- 原存档以及 `.bak`、`.tmp` 的内容/原先不存在的状态已逐字节核对恢复。五个原有打开场景的活动状态、根节点数量及未修改状态与测试前一致。
- 编辑器留在打开、停止运行的 Edit Mode；包清单保持一致，未安装后台监视或自动重启任务。

第一次 Play 测试跨脚本域重载时，CLI 请求发生 socket hang up，编辑器进程没有退出。夹具先退出并恢复存档，再增加重载期间的有限重试后完成三轮检查。没有把这次通信中断当作 GPU 崩溃或通过的 Play 测试。

控制台仍有已知的 606 个植物 Prefab 缺失日志；此外 CLI 的启动探针曾请求未被当前 Pipeline 包支持的 `editor status` 命令。两项均单独记录，没有将它们当作本轮新增游戏运行时异常，也没有通过清空日志隐藏。旧线程/提交内存耗尽本轮未复现，长期稳定性仍需实际使用确认。

## 证据与工具

本地证据目录：`.codex/editor-stability-20261102/`。

- `normal-launch-arguments.json`、`normal-start.json`、`final-edit-mode.json`：无强制参数启动、实际 API 与最终场景状态。
- `play-*-smoke.json`、`play-*-settled.json`：三轮 Play 检查与原生运行状态。
- `resources-*.jsonl`、`verification-summary.json`：OS 资源采样与汇总。
- `save-integrity.json`、`console-final.json`、`confirmed-crash-excerpts.json`：存档恢复、原有错误与脱敏崩溃摘录。
- `ProjectSettings.before.asset`：本次图形配置变更前的设置快照。

`Tools/EditorStabilityRepair.cs` 由 Pipeline 的 run_script 显式运行，位于 Assets 外，不安装常驻编辑器回调；其中保存/恢复入口为本次诊断的一次性防护，不应重放旧存档快照。`Tools/MeasureUnityEditorResources.ps1` 显式运行时采样指定编辑器 PID，运行结束即停止。
