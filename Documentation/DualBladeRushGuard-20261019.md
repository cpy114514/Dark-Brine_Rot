# Twin-blade rush and frontal guard — 2026-10-19

The user requested a substantially longer rush, damage immunity during the rush, a longer charge, and twin-blade guarding against all frontal attacks. This revision changes those existing mechanics and preserves the authored v002 poses, 1.2x blades, combo timing, club charge and opening.

| Parameter | Current behavior |
| --- | --- |
| Rush distance | 16m, previously 7m; CharacterController resolves walls |
| Minimum blade charge | 1.2s; early release cancels immediately, spends no attack stamina and never queues a delayed dash |
| Full blade charge | 2.6s; X-pose raise takes .9s, then holds |
| Rush damage/immunity phase | Shared authored .20–.56 normalized window on the existing .75s rush clip |
| Guard direction | Frontal 180-degree hemisphere; rear attacks still damage |
| Guard stamina | Existing 12 per blocked attack and 2/s hold drain; a final active block absorbs the whole hit before stamina exhaustion ends guard |

Immunity reads the live equipped/enabled attack phase; it is not a lingering timed buff and does not alter global time. Startup, charge and recovery remain vulnerable. Cancel, unequip, death, disabling combat and external/story control handoff remove protection immediately. The last low-rate CharacterController movement segment remains protected inside Move collision callbacks, then clears. Continuous actual blade queries and once-per-swing victims remain authoritative.

PlayerHealth now has one directional hostile-damage entry point. Existing melee delegates to it. Nailong spit uses incoming projectile velocity, sonar uses its ring center, Bombardino blasts/fire use the blast origin, and the strafing sweep uses its pre-move segment origin so a fast pass cannot reverse front/back classification after contact. The confirmed-hit resolver reports Blocked through the original single hit event, without duplicate defender events or fabricated hit feedback during immunity. Generic narrative/hazard damage retains its separate entry point and existing nonlethal health floor. The rifle already excludes player targets and was left unchanged.

The Inspector exposes minimum/full/raise times, rush distance and frontal guard width. The new frontalGuardArc field deliberately supersedes the former serialized 130-degree melee cone. Gear instructions are English and describe charge thresholds, rush protection, wall stopping and frontal guard.

## Verification

Final source passes **43 assertions at each 60Hz and 30Hz simulation rate** using actual Input System mouse/keyboard input in the existing Editor, a temporary preview arena, the real player prefab, equipment, Animator, collision controller and native camera. Open-ground travel measures **15.99988m / 16m**; a physical wall limits travel to **1.58191m / 1.58179m**. All 47 / 25 observed rush Move collision callbacks remained protected. Per-frame direct damage, directional damage and unified heavy-hit feedback were rejected during travel. Before/after damage, charge thresholds, early cancellation, real swept blade contact, cancel/disable/unequip/death/handoff cleanup and unchanged club early release also pass.

Frontal/rear tests execute the actual spit projectile, bomb/missile/fire Blast path, expanding ground sonar crossing and high-speed strafe sweep. A 10,000-damage frontal attack blocks at -89, 0, +89 and +90 degrees; -91, +91 and 180 degrees remain vulnerable. Last-block stamina behavior, single unified block event, narrative health floor and unchanged global time pass.

Evidence: `.codex/dual-blade-rush-guard-20261019/final-60hz/` and `final-30hz/` retain **1,703 / 883 consecutive native camera frames**, frame/clock maps, event records, assertions, dense review sheets and continuous silent MP4s. `Tools/TestRushGuardV003.cs` and `Tools/ReviewRushGuardV003.py` reproduce the diagnostics. Earlier native-60hz evidence is retained separately. Before/final source hashes and final verification summary are retained. Native input and simulation clocks are functional evidence, not measured release FPS. Raw sweep-marker observations are available in the maps; no total CPU/GPU/GC or release performance approval is claimed.

Review method: inspected dense consecutive native-camera sheets at rush entry, real contact, immunity exit and wall stop, including final 30Hz source. This verifies timing/readability and body stopping, not a normal-speed full-film or final-art approval. The temporary AI-disabled target can visibly overlap the attacker's presentation during contact, and authored blades can enter the test wall while the capsule stops; universal enemy-body and environment/weapon visual clearance are not certified by these damage/travel tests. Full island AI combat, slopes/ledges/water, all camera angles, hardware audio, journal layout and release performance have not been retested in this targeted pass.

Existing Editor7800 only; no second Editor/Player, scene reload/save/discard, build, commit, push or scheduling. Final Editor stopped, compilation errors0, runtime console errors0, loaded dirty Story1 preserved. Latest human save protected before testing, diagnostic saves preserved separately, restored after Stop and SHA256 verified **3EA875D36D67463D847BDDB30494E75A689A349F1416A89AB60110931EE864A5**. Do not restore the older F338/EE58 saves over this newer progress.
