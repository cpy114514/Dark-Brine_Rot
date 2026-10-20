Latest rush/guard revision: [DualBladeRushGuard-20261019.md](DualBladeRushGuard-20261019.md) supersedes the historical 7m/3m rush, blade charge timing and melee-only guard below. Authored v002 poses and 1.2x blades remain.

# Dual blade targeted revision — Oct19

## Completed targeted FIRST PASS

Both active Sahur controllers now use the new authored `BladeFinisherV002` and `BladeRushV002` clips. Original clips, v001 Blender source and prior dirty code/controller files are retained. Blade sizing is1.2x around the existing palm anchor; actual equipped lengths and hit volumes are2.52m. Source rigs/rest poses, the first two slashes, controls, damage values and opening remain.

| Motion | Native source | Runtime timing / contact |
| --- | --- | --- |
| Combo3 | frames1–91 /60fps /1.5s, nonlooping | state speed1.65, ~.909s; active .28–.74 (~.255–.673s); arms extend before the turn and recover after.78 |
| Rush | frames1–46 /60fps /.75s, nonlooping | speed1; .20–.56 active (.15–.42s), same committed CharacterController travel window; down/back follow-through after.35s |

The finisher owns a baked body revolution; the former scripted actor-root spin was removed. No additional root rotation/travel is layered over it. Rush configuration is7m; actual open-ground totals7.289m/7.138m at60/30Hz include existing movement settling. A real wall stops travel at1.582m. Existing continuous weapon queries, once-per-swing victim ownership, local hit-stop and unified actual-hit event remain authoritative.

Final evidence: `.codex/dual-blade-polish-20261019/final-60hz/` and `final-30hz/` contain40 passing assertions each, eight extra metric rows,1,664/850 consecutive final-camera frames, frame maps and full silent MP4s. Source/native source views and consecutive finisher/rush/light/heavy reaction sheets were visually inspected. Measured turns360.033/359.113deg; blade vertical component <=.001063 during the turn. Follow-through components are about -.621 down and +.351 backward relative to the committed facing. The main `dual-blade-controls-preview.mp4` selects complete combo/charge/rush sections without retiming; complete diagnostics remain in the folders.

Victim feedback: existing Nailong/Cappuccino presentations now recoil in the actual force frame, with bounded light/heavy amplitudes, an attack/settle envelope and a clock that waits for local hit-stop. Nailong additive offsets are removed safely before the next authored pose and on disable; pause retains the evaluated pose. Boss poise, heavy interruption rules, collision roots and death/AI ownership are unchanged. Vertical weapon motion falls back to the actual attacker/victim planar direction for recoil. Final30Hz native angular peaks: Nailong6.41/23.94deg; Capri12.83/17.82deg, all measured force projections nonnegative.

The60Hz full run precedes only the later pause-preservation guard; its nonpaused execution path is unchanged. The final30Hz full run, focused60Hz pause record and performance sample use the final source. `pause/tests.json`:63 consecutive native-camera frames, paused pose error0deg and clock error0; recovery/disable succeeds. Source hashes are retained separately before and after that guard, with current hashes verified after testing.

Warmed enlarged dual-blade sweeps over180 observed native frames: median.0518ms, P95 .3192ms, max.5503ms; arena transforms160→160 and particle systems16→16. This excludes cache setup, capture, total frame cost, GPU and release FPS. No extra particle/audio systems were introduced by this revision.

Limits: native gameplay poses and continuous-frame sheets were reviewed; no claim of watching the entire MP4 at normal speed or final AAA approval. NPC recoil assertions directly exercise real health/confirmed-hit contracts with AI disabled; the Capri fixture's absolute floor placement is not a grounded locomotion/body-contact acceptance. Full island/autonomous boss combat, all terrain/camera angles, hardware audio listening and release performance remain unverified. Earlier sampling/serialization/direction failures are preserved, not counted as passes.

Final existing Editor7800 is stopped, compiling false, console errors0; dirty Story1 remains loaded without saving/reloading/discarding. Latest human save restored and verified **F33809D2D1C1F0A819E2F53BBFDD5E49F094B758EF7044628C56CE8F0AAA3827**. No new Unity Editor/Player, build, commit, push or scheduling.

The human explicitly requests 1.2x existing blade dimensions, LMB combo3 as one complete revolution with both arms held sideways and blades horizontal, a longer charged rush, a downward/backward tip direction after the X opens, and readable victim recoil. This authorizes these targeted revisions; preserve the existing character, combat rules, first two slashes, charge controls and opening.

Initial tuning: blade length2.1→2.52m at native player scale, with the same palm anchor and matching actual collider. Open-ground rush3→7m, same .20–.56 normalized window and CharacterController wall admission. Combo3 retains about .91s runtime duration (old1.5/1.65), .28–.74 active window; the authored pose turns exactly360 degrees during that interval. The source clip has anticipation/arm extension before the turn, lowered weight and alternating pivot steps, then arm recovery. Collision root remains facing-stable; no extra scripted root rotation. Rush retains .75s and the established X entry, opens outward/downward, follows through with both tips down/back, then blends to ready. Original v001 actions and imported clips remain backed up.

Stage is a targeted FIRST PASS revision, not final AAA/art approval. Source previews, native retargeting, real inputs/contacts, wall stopping and victim recovery must be reviewed before acceptance.

Existing Unity Editor7800 only. Preserve loaded dirty Story1 without saving/reloading/discarding. Blender MCP currently unavailable; portable project Blender5.2.2 background authoring is the fallback, using versioned working copies. No build, commit, push or scheduling.

Latest human save authority for THIS revision is **F33809D2D1C1F0A819E2F53BBFDD5E49F094B758EF7044628C56CE8F0AAA3827**, backed up in `.codex/dual-blade-polish-20261019/before/user-save.json`. Do not restore the older EE58 test save over this newer progress.

Initial native source/retargeting inspection:48 front/side/gameplay captures. Actual blade length2.52m; native matrix/mesh tip directions are authoritative (TransformDirection on a scaled hand gave misleading diagnostic axes). Complete initial mechanics runs at60/30Hz pass40 checks each; measured finisher rotation360.033/359.113deg, vertical blade-axis component<=.001063, rush follow-through down-.621 and backward+.351. Open rush7.289m includes existing movement settling; authored controller displacement7m. These recordings precede only the following victim-direction correction.

Rejected measurement attempt: Update-time recoil sampling missed LateUpdate presentation; a partial JSON serialization also rejected raw Unity Vector3 properties. Preserved failure footage and salvaged raw frame arrays. Correct final-pose samples show Nailong light/heavy6.70/24.00deg and Capri13.40/18.61deg. Direction check then revealed only7.6mm of Nailong head displacement along the hit force despite angular motion. Replaced model-local-axis offsets with world/parent force-frame tilts; the vertical-strike fallback derives a planar force from the actual attacker/victim positions. New final directional tests and recordings are required before acceptance.

Directional correction verified in the final posed frame: Nailong head displacement along the incoming force now peaks .080m light/.291m heavy (all sampled projections nonnegative), compared with .0076m in the rejected local-axis fixture. Capri top-axis projection also leans away from the hit for both strengths. Light/heavy angular peaks remain bounded at6.70/24.00deg (Nailong) and13.40/18.61deg (Capri). `direction-after-60hz/tests.json` contains exact native measurements. Final full-rate/low-rate recordings follow this source; earlier `verification2-*` videos are superseded only for victim-direction presentation.
