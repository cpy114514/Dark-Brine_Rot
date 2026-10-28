# Editor input recovery — v271 / 2026-10-27

User reports all input stopped responding after animation work. In the live MainMenu Play session, native RawInput Keyboard(1) and Mouse(2) were disabled; current devices instead pointed to anonymous non-native Keyboard1(15) / Mouse1(16). No PlayerInput component was present, so this was a global device/current-selection failure, not a character action-map issue. No active CombatCombinationTests lease was set. The original owner cannot be identified after its dynamic assembly/domain lifetime ended.

RecoverPhysicalInputV271 inspected and recorded state, removed the unpaired anonymous synthetic keyboard/mouse, enabled the existing native devices, and made those devices current again. Existing Play Mode, scene, pause/time scale and save were preserved; no Editor restart or input-binding reset. Follow-up roster contains only enabled native devices 1/2; Menu Event System and its action asset are active.

Hardened seven jump-review scripts outside Assets. They now arm cleanup before mutating input, restore on Play exit, before assembly reload and Editor quit, and restore hardware before fallible report serialization/file writes. Devices have explicit fixture names. A stopped fixture's wait loop cancels rather than carrying on into later human Play. Cleanup is idempotent and retains the original current devices.

Native regression in the current menu: simulated Stop callback and exception teardown both pass. A disposable disabled backup device exercises re-enable behavior; real hardware was never disabled for this regression. Synthetic test devices are removed, current real devices restored, and the exact original device roster retained. This verifies cleanup logic without stopping the user's Play session. Actual recompile/domain reload and physical keyboard gameplay input were not forced during this repair; user confirmation remains requested.

Evidence: .codex/input-recovery-v271/before.json, repaired.json, cleanup-regression.json. No production runtime/scene/animation bindings changed by this repair.
