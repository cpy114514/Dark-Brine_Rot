# Qualified standalone baseline — batch019

## Repairs

The opt-in IslandPerformanceBaseline previously used an ordinary scene search for a DontSave WorldPerformanceManager. Native tests confirm that search returns no manager, silently bypassing the readiness wait. It now resolves valid loaded runtime objects through Resources, waits for initialization, and reports an explicit timeout when the manager is absent/incomplete.

The old sampler synchronously read a pixel back from the GPU in every measured LateUpdate. That charged diagnostic synchronization to the game's frame time. It now counts endCameraRendering only for the selected camera and owned target, rejects nonrendered intervals and unrelated-camera/target callbacks, and performs finite visible-versus-mask-zero image controls outside measurement. The capture camera uses a target texture so an occluded test window still renders. No extra camera rendering is manually submitted.

It also distinguishes unavailable GC data with -1 and availability metadata, and restores prior camera targets without forcing cameras on, plus the original run-in-background setting. These changes run only when the existing -sahur-baseline diagnostic flag is supplied. Normal gameplay/design/art remain unchanged.

## Executed native and standalone validation

22 instrumentation checks passed in isolated CombatTest. They cover the original hidden-manager miss, real native render callbacks and samples, no-render/other-camera/wrong-target rejection, actual image positive control, mask restoration, summary arithmetic, ready/wait/ready-again/timeout cases, and target/enabled/background cleanup. Their timeout output is an intentional failure control and is not a performance result.

Built all seven enabled scenes through the same existing Editor, Windows64 Mono2x Development,220.34 seconds,2728805367 bytes. BuildResult Succeeded. Its4 error messages are the existing Environment opening summary and three unavailable plant groups234+177+195.493 warnings remain. No asset deletion or recovery-path check-in was used. Binary, build caches, raw logs and private preference snapshots stay ignored.

The resulting standalone Player completed all three route samples with0 runtime errors,2159 completed camera callbacks,1798 measured frames and0 rejected unrendered intervals. Each phase's actual world-versus-empty native image control changed over2.07million pixels. The three PNGs were visually inspected and show Sahur, live foliage/ocean and the boss in its vicinity. They are camera renders and do not establish overlay UI appearance.

Hardware: Intel(R) Core(TM) i5-14400F / NVIDIA GeForce RTX 4060. Saved PC settings:1920×1080 output, renderScale0.60 (approximately1152×648 internal), target60fps, vSync1. These settings were observed, not increased or lowered for a claimed optimization gain.

| Phase | Measured frames | Average FPS | P95 ms | Maximum ms | Frames over50ms |
|---|---:|---:|---:|---:|---:|
| coast | 600 | 59.99 | 18.25 | 18.58 | 0 |
| forest | 600 | 60.01 | 17.19 | 18.70 | 0 |
| boss | 598 | 59.80 | 17.42 | 33.35 | 0 |

The optional sampler reports first-island loading24.86seconds, excluding process/menu initialization and later route warmup. It is not an executable-to-interactive startup measure or a startup improvement. There is no pre-optimization standalone comparator: this establishes a qualified baseline for the already optimized build, **not an FPS increase or native1080p throughput/headroom result**. The player is protected and the camera fixed; no full boss encounter, long navigation, all-style matrix or low-end hardware claim. GC values remain recorded counter data without an allocation positive control.

## Safety and launcher repair

Both real save files match fresh snapshots.100 native game PlayerPrefs values and types are restored and verified. Editor prefs/style/intensity are preserved; original5 scenes are clean/stopped, and build flags/backend restored. Other Dark* Players are a launch/restore guard. The launcher tracks only its own process and hides its window.

The first launcher cleanup failed because7 observed legacy Unity float entries are tagged REG_DWORD but carry8 bytes and are returned as Int64. Managed RegistryKey.SetValue rejected this representation. Recovered from the immutable private launch snapshot using the exact original native type, byte length and bits; verified all100 values/kinds and unchanged saves before further work. The corrected helper includes that case. The first process exit code was not retained, so it is recorded as unavailable; runtime report completed and process termination were verified. No old save was written back.

The scoped helper uses [RegSetValueExW](https://learn.microsoft.com/en-us/windows/win32/api/winreg/nf-winreg-regsetvalueexw) to restore the observed raw registry representation. Snapshot data never enters a checkpoint.

## Next coverage

Profile uncapped renderScale1 at1080p with explicit opt-in diagnostic overrides and full restoration; compare all four looks and moving-camera stability. Separately profile the24.86s island initialization path and a real extended boss/combat sequence. Do not infer GPU cost from capped FPS or attribute one sample's GC peak without a counter positive control. Existing606 unavailable plants remain preserved.

Evidence: AUTO_QA/Evidence/batch019/{proof,verification,build-report,standalone-report,player-safety}.json and actual native PNGs. Source: IslandPerformanceBaseline.cs; external tools: VerifyBaseline019, BuildBaseline019, RunBaseline019.ps1 and RestoreBaseline019Prefs.ps1.

Build cleanup also detected one temporary App UI preload reference added to the on-disk PlayerSettings by the build. Native PlayerSettings already exposed the original one-entry list; restored that native list and rolled back only the confirmed build-owned disk delta to the clean baseline. The builder now snapshots/restores original settings bytes as well as the native preload list. No semantic YAML or unrelated asset edits.

Five build-touched text assets/settings were byte-identical to their cs201 originals after cleanup but remained in Plastic pending status. Compared original bytes and cleared only those exact paths with cm undo --unchanged. No source/art changes were discarded; the four original unrelated pending paths remain. See build-normalization.json.
