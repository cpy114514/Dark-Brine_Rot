# Blender and Unity animation workflow verification

Codex was connected through the mcp-blender addon to inspect and operate Blender during workflow setup. The October 2026 check used Blender 5.2.2, Python 3.12 and Unity 6000.3.23f1. The seven top-level skills were discovered by the Codex app server. Twenty-one configured Blender tools were visible to Codex. The original setup record is archived with the installation details. [Workflow README](../README.md) · [Original verification](https://github.com/cpy114514/Dark-Brine_Rot/blob/2d2a862bba490081ab4e8607dba4b35bb73bce8b/Documentation/AnimationWorkflow/verification/report.md) · [Recorded skill discovery](https://github.com/cpy114514/Dark-Brine_Rot/blob/2d2a862bba490081ab4e8607dba4b35bb73bce8b/Documentation/AnimationWorkflow/verification/codex-discovery.json)

| Check | Result |
| --- | --- |
| Blender | The two-bone fixture supported pose edits, F-curve changes, playback and a 49-frame FBX export. The review captured four views. |
| Unity Generic import | Valid Generic Avatar, one clip, 30 transform curves at 24 FPS. [Import check](unity-generic-import.json) |
| Unity Humanoid import | The existing standing melee clip imported with a compatible Mixamo Avatar. An incompatible Sahur Avatar was rejected. [Import check](unity-humanoid-import.json) |
| Cleanup | The Blender fixture and Unity staging assets were removed. The source files retained their original hashes. [Environment record](environment-verification.json) |

The setup check used an isolated fixture and made no production animation or controller changes. Action reviews still check silhouette, timing, foot support, weapon contact and root motion in the actual scene. [Action sources and credits](../project-animation-context.md)
