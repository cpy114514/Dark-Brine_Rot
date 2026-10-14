"""Preserve historical handoff notes while replacing their authoritative header."""
from pathlib import Path
root=Path(r'D:/indiegameDev/unity/Dark Brine_Rot')
path=root/'Documentation/AnimationWorkflow/Actions/CH1_SahurSharkEncounter_v001/resume-at-0250.md'
old=path.read_text(encoding='utf-8-sig')
marker='## HISTORICAL NOTES'
if marker not in old:raise RuntimeError('Historical boundary missing; no document changed')
current='''# CURRENT — 2026-10-12 continuation, Vancouver — v051 INCOMPLETE

This section supersedes every historical save, state and health threshold below.
Approved optimization follows design-v039.md (user 开始实施计划 / 继续). Solo;
no commit or push. Keep original walk/run/roll, rigs/Avatar/history, moving charge,
right-hand stick, silent audio, monochrome UI and original-colour images.

## Save and editor protection

ONLY restore `.codex/opening-optimization-20261012/backups/save.json` AFTER stopping
original project Play Mode to
`C:/Users/pinyu/AppData/LocalLow/DefaultCompany/Dark Brine_Rot/dark-brine-rot-save.json`.
SHA256: `9BDCCBCC580D5ED35458A4FD52ED9C72A71520609ACF944340FCB619D2F4DB58`.
Never restore historical51C backups. Original editorPID22012 recovered using native
CompilationPipeline CleanBuildCache after stale Bee compilation state. It now
compiles and plays successfully; original native Play was STOPPED after v051
review, scene clean, authoritative save restored and hash verified. Do not kill it.
Current Pipeline ports: ORIGINAL7801, SNAPSHOT7800; always target by project path.

Independent copy `.codex/opening-optimization-20261012/project-snapshot` owns all
diagnostics. Snapshot editor6 PID10644, batch-force-d3d11, companyDarkBrineDiagnostics /
productOpeningBenchmark, separate persistentData. Earlier D3D12 owned editors
crashed887a0006; D3D11 runs are diagnostic, not original renderer config changes.
CLI editor_stop/play return before state settles; confirm actual isPlaying before
mutating/recompiling. Do not recompile or change assets during a benchmark.

## Implemented native source and evidence

Shark1000HP, immediate shark300/player30%/random20–35sec protected finale latch;
safe action/airborne handoff, no new chains. Existing health-policy.json passed.
Charge1.8sec gradual windup, accelerating right-arm release, walking/sprint/turn
preserved. Latest REAL GameView/input test16:15 passed sprint/direction/reverse,
mobile heavy/light; hit0.200sec, release0.900sec. charge-validation.json is current;
its16:09 error file is STALE. Latest native core test independently passed monotonic
release, exact pose join0deg, firsthit0.17566 / release0.87564sec. Original native
upper light Combo1/2/3 states restored; original walk/run/roll retained.

GPU original-texture sails/flag, filtered heavy hull and bow displacement implemented.
Previously truncated ProceduralSky repaired from unchanged HEAD plus bow patch:
31,724chars, native ShaderUtil supported/no errors. First build unrendered/broken
shader is INVALID. Warm original fragment geometry/shards + mesh volumes are cached;
shards stay inactive/kinematic until physical release. Source mesh/gravity retained.

Actual shark solid geometry now includes original rigidly skinned caudal fin311
vertices +4capsules. Dynamic fin follows existing Tail_Tip; no rig/bind replacement.
Spatial mesh-part grid produces same full ComputePenetration cost as brute force.
LATEST fin-spatial-validation.json PASSED:12native poses,72queries/69nonzero,
skin contact vs native BakeMesh error0.185mm, max cost difference1.907e-6.
Disabled planning colliders experiment FAILED (34.375cost difference) and was
rejected: production planning capsules and fin remain enabled. Capsule lengths
use original local vectors before world rotation/large-world translation to avoid
float-ULP native geometry recooking.

Entry planner: coarse all real edges before fine attitudes; budget2ms/frame;
water entry now coarse all reachable boards before fine refinement. Four actual
hero contact poses are sampled, original pose restored before yielding. Physical
wood clearance governs admission; exposure/route length preferences no longer
cause repeated otherwise-clear searches. Board-local candidate and grip anchors
survive moving physics frames. Walking corridor uses cached original walk's actual
retargeted soles; guard footprint alone no longer rejects a narrower safe walk.
Invalid supported entry now exits once through existing exhaustion/unconscious
ending too (previously only water entry rejected invalid stance). No fake contact,
teleports or invented fragments. EntrySelectedBoard/PoseCost/WoodCost/
PreparationSeconds/FromWater are recorded separately from later hop-search scratch
fields; old ContactObstructions stance=Infinity can mean a failed optional hop,
not failed admission. Latest source telemetry in OpeningBenchmark includes them.

Actual tail microcorrection is <=5deg/15cm, applies guard and final chest; rejects
wood crossings/increased true body penetration. Bounded right-arm IK unchanged.
Physical attack-route cache now ignores exposure and artistic detour-length penalties
when retaining an actually clear CURRENT route; never switch instantly to a different
detour. Eight route candidates, fallback up to16 only if all first8 remain blocked.
Successful opening parry now visibly samples actual Hit_Recoil, rather than having
that feedback overwritten by another Bite_Lunge. All active contact windows retained.
Camera six-shot data, actual shark-camera clearance, closer sameboard framing;
no-neighbour phase no longer forces the wide jump camera. Full visual review pending.

## Art assets — trial, not final original activation

SNAPSHOT trial: Parry/Missv040, Brace/Hopv047, Guard/Divev050, Slashv051;
original Generic shark4actionsv048. Original active definition/shark controller
still historical; original v050/v051 are ISOLATED Assets/AnimationStaging only.
Never copy snapshot Assets/ProjectSettings wholesale. InstallOpeningTrialV051 ran
only snapshot, keeps versioned native backupPreviousEncounterDefinition.

v050 original28-bone Guard breath: root/legs/bind unchanged; source foot drift0,
native97samples/291frames/3continuous60fpsmovies. Native uncorrected ankle motion
3.43mm, hand motion9.74mm, closed loop. Actual held stick points diagonally forward.
ArtSource/Encounter/v050 source/native reviews accept FIRST PASS integration trial.
Native movies `.codex/opening-optimization-20261012/native-continuous-v050`.

v051 retained65-bone diagonal Slash: smooth30ms Gaussian phase offset at unchanged
0.63sec hit; positive time derivative>=.848, startup/recovery retained. Native
85samples/255frames/3movies show hand30.3cm higher at hit and shaftY+.182 instead
of-.290, max shaft step22.50deg/frame. Source contains no skin: Blender correctly
refused an empty preview, native original-avatar images reviewed instead. Native
source Avatar must be StandingMeleeAttackDownward65-bone source; copying28-bone
PbrAvatar was rejected safely, corrected. ArtSource/v051 reviews accept FIRST PASS
trial only. Original gameplay charge clip untouched.

v047 source original28-bone Guard/Brace/Hop and v040 source65-bone Parry/Slash/Miss
remain retained. v048 original16-bone shark reduces accumulated tail-chain bend;
1362native frames/3views. Original rig/skin/Avatar/bone names preserved. No final
artistic acceptance from numeric checks alone.

## Current native validation / explicit failures

Latest benchmark RUNNING with v050/v051, latest global coarse-first board search,
entry telemetry, recoil and camera/source. Do not launch another or mutate its
scene until completed. Report:
`C:/Users/pinyu/AppData/LocalLow/DarkBrineDiagnostics/OpeningBenchmark/opening-benchmark.json`
images siblingopening-benchmark-images. Check modification times; images retain
older filenames if a newer run never reaches a phase. Native helper
RunNativeOpeningV047.Start(capture,water,seed) explicitly renders1920x1080 plus
GPU fence, owned diagnostic standing fixture only, no production teleport.

Completed logs retained under `.codex/opening-optimization-20261012`:
- native-profile-v049-heightfix-timeout / exposure-timeout / localanchors-timeout:
  FAILED slow admission. Do not count missing contact999 as completed film.
- native-profile-v049-coarsefirst-invalidstance: entry+lead4.517sec but bad
  supported admission, 1hero/1tail only. Fixed rejection; NOT acceptance.
- native-profile-v050-supported16:31: full20.813,3hero/1tail, gaps
  [0,.62845,0,0,.086264,.018428], foot1.06mm/slide1.39mm. First slash and guard
  tail FAILED; all supporting-sole targets passed. Latest v051 intended to raise
  the first slash's low contact arc, pending full runtime proof.
- native-profile-v050-recoil16:36: varied natural wreck/full20.826,2hero/1tail;
  gaps[4.882,0,.1793,0,7.567,.0297], foot1.22mm/slide1.47mm. Dense wood still
  blocks some approaches. Do not count as whole-scene pass or regression verdict
  against one change: live waves/scatter differ even with same Random seed.

All native editor profiles diagnostic only, screenshot/other editor overhead
included. Standalone build2 zero errors rendered sea correctly but failed admission.
Latest native source/art still needs standalone build3 and rendered1080p no-PNG
performance. 60FPS/1%low>=50/no>50ms acceptance NOT achieved yet.

Next: inspect running v051 full contact/entry/camera results; fix remaining physical
approaches without enlarging hitboxes or fake impacts. Run distinct water/dense/
no-neighbour/lowFPS/pause cases, real controls/traversal and held defeated float /
lost-stick shore. Produce full normal-speed game/front/side/3q continuous previews;
review weight/contact/camera and revise. Build independent latest standalone,
actual1080p no-PNG performance; activate ONLY specific accepted assets in original
with native backups, compile/Play verify, stop and restore9BDC. Keep daily automation
active while incomplete. Original save/state and unrelated pre-existing edits protected.

---
'''
path.write_text(current+old[old.index(marker):],encoding='utf-8')
print(path)
