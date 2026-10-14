# User constraint — stop repeated development Player network prompts

2026-10-13: another chat relayed the human user's explicit instruction to stop repeatedly triggering the DarkBrine network-access permission dialog. Do not launch another independently versioned standalone-vXXX development EXE with network listeners. Do not disable Windows Firewall, add allow rules or click Allow to work around it.

Our known current-turn Players were55288(v157),54452(v161),33908(v165); all completed and exited. Current process inventory shows no DarkBrineOpening.exe. Original Unity Editor22012 remains open and is not to be terminated. Save and continuation evidence preserved. Continue animation/runtime work through this existing Editor only for now.

Standalone testing may resume only after unnecessary PlayerConnection/Pipeline listeners have been addressed and a fixed runtime path has been adopted and verified not to generate repeated permission requests. Another chat is investigating this issue in the shared project; do not concurrently modify boot/configuration/network resources. Latest actual165 boot.config contains player-connection-mode=Listen. No firewall rules or permissions were changed here.

This constraint supersedes older continuation steps telling agents to build and launch new numbered development EXE paths. Keep source/build provenance versioned without creating a new network-listening runtime identity on every launch.
