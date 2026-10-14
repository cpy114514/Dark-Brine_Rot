"""Record latest human scope; never let historical heartbeat instructions restore surfing."""
from pathlib import Path
import hashlib
root=Path(r'D:/indiegameDev/unity/Dark Brine_Rot')
path=root/'Documentation/AnimationWorkflow/Actions/CH1_SahurSharkEncounter_v001/resume-at-0250.md'
old=path.read_text(encoding='utf-8-sig')
previous,history=old.split('## HISTORICAL NOTES',1)
save=Path(r'C:/Users/pinyu/AppData/LocalLow/DefaultCompany/Dark Brine_Rot/dark-brine-rot-save.json')
expected='9BDCCBCC580D5ED35458A4FD52ED9C72A71520609ACF944340FCB619D2F4DB58'
assert hashlib.sha256(save.read_bytes()).hexdigest().upper()==expected,'Restore the authoritative save AFTER native Play Mode has stopped first.'
current='''# CURRENT — 2026-10-12 Vancouver — v055; opening optimization INCOMPLETE

## Latest human instruction overrides all earlier plans and heartbeat text

User: 把把木板当作冲浪板去掉吧，这个太奇怪了.
**REMOVE vehicle controls and leg paddling. Do NOT restore automatic surfing,
W/S board propulsion, A/D board steering, paddle animations or surf hints.**
Wreckage is naturally floating physical walking support. Keep ordinary walking,
running, rolling, mobile right-hand combat/charge, jumping with platform momentum,
swimming, automatic moving-edge climbing, mesh collision, real gravity/freefall,
paired board hops/no-neighbour variant, shark combat, silent audio and shore ending.
Authoritative design: design-v039.md plus latest approved amendment design-v055.md.
Older design-v006 / automated prompts and every surfing/paddle instruction below
are SUPERSEDED. Continue solo, no questions, commits or pushes.

## Current editor / save safety

Original Unity PID22012 is STOPPED after the latest v055 native regression. Native
compile and Play succeeded. Always confirm actual isPlaying before edits/restores.
Pipeline port most recently7800; always target the exact original project path.
ONLY backup `.codex/opening-optimization-20261012/backups/save.json` is authoritative.
Restore AFTER stopping Play to
`C:/Users/pinyu/AppData/LocalLow/DefaultCompany/Dark Brine_Rot/dark-brine-rot-save.json`.
Restored SHA256: 9BDCCBCC580D5ED35458A4FD52ED9C72A71520609ACF944340FCB619D2F4DB58.
Never restore historical51C saves. Protect pre-existing user changes and scene files.
Independent snapshot editor7 PID33784 was CLOSED after Build4, no owned player runs.
Other Unity processes are unrelated; do not stop them. Snapshot is a full separate
copy, companyDarkBrineDiagnostics/productOpeningBenchmark; do not copy it wholesale
back to original. Snapshot/Build4 still have PRE-v055 vehicle source/controller;
explicitly sync the seven changed runtime files and native original controller before
the next build, retaining snapshot paired-action trials and its separate save identity.

## v055 implemented and natively verified

Story1WreckRider no longer mounts or claims BoardRiding, locks walking, drives wood,
sets paddle parameters or spawns a surf wake. Normal controller owns movement on
real supporting geometry. Jump keeps actual support velocity and .35s grab grace.
Moving-edge climbing returns to ordinary walking. Story1WreckPlank has no vehicle
FixedUpdate/forces; historical Drive calls are inert compatibility only. FloatBody
uses normal water drag; buoyancy/mesh/physical breakup retained. Finale4–6.4s no
longer drives its wood. MovementPose removes paddle/board-foot code, preserving
original locomotion and climb support. MotionSnapshot uses actual standing support.

Native RetireWreckSurfV055 removed Wreck Surf and three vehicle parameters from
the active v006 controller, preserving every other motion/speed. Versioned native
backup Resources/Encounter/v055/PreviousSahurGameplay.controller. Historical
v022/v054 source/export/assets retained INACTIVE. Do NOT run their installers.
`retire-wreck-surf-v055.json` passed; active controller now30base states.

Latest `wreck-walking-v055.json` 18:54:55 PASSED real native controls on an isolated
ORIGINAL hull fragment (diagnostic relocation only): actor walked1.314m across deck,
board planar drift.0319m under W input + historical Drive(1,1) (no propulsion), moving
ground attack, jump3.048m measured including moving water support, inherited board
speed2.369m/s, fast swim3.912m, actual moving-edge automatic climb returns to walking,
broken support releases. Original walk/run/roll retained. This is scoped controls
acceptance, not all-layout/animation/performance acceptance. Old18:51 error is STALE:
initial fixture incorrectly selected a26m-thick rolling hull section, then corrected
to an actual thin timber. Earlier paddle/traversal tests are HISTORICAL ONLY.

## Retained opening progress and outstanding work

Shark1000HP and immediate protected shark<=300/player<=30%/random20–35s finale latch;
safe handoff, no new chains. Actual native health-policy and mobile charge tests
passed previously. Charge1.8s gradual and~.9s release, first hit~.2s; legacy light
upper Combo1/2/3 restored. Original sails/flags GPU wind/heavy hull/bow split retained.

v052 actual original caudal fin311vertices +4capsules, exact spatial native-part grid;
48native brute-equivalence queries passed, disabled collider experiment rejected.
v053 strict stance admission.0004 and mathematically dominated route pruning;
camera checks feet too. Dense layouts still fail some contacts/performance.

v053 continuous actual native 20.8s, source60FPS/recording30FPS offline four-view
review COMPLETED:625frames per view and encoded game/front/side/three-quarter mp4
at `.codex/opening-optimization-20261012/native-paired-v053/`. Native full six real
contacts4hero/2shark, gaps[0,.03143,.02715,0,.02614,0], support1.162mm/slide1.203mm,
breach1/hops0 (same-board variant). This is ONE layout, not final artistic acceptance;
actual video keyframes show upright first probing shark and occasional wood/feet
occlusion. Continuous files exist; normal-speed whole-film review remains outstanding.
Offline capture is NOT performance evidence. Time.captureFramerate restored0.

Build4 v053 succeeded errors0/warnings488, independent1080 D3D11 actual RTX4060
render, `standalone-v053-pruned.json`: full film4hero/1shark, reversal guard miss10.594m
FAILED; film mean21.85ms/p9949.99/max86.2ms/10frames>50, maxFacing40.57/maxSolid39.81ms,
entry.782s but max entry214ms. 60FPS / 1%low>=50 / transitions<=50ms FAILED. Repeat
isolated after fixes; original renderer config unchanged. Do not claim final60FPS.

Snapshot paired-action trial: Guard/Divev050,Slashv051,Brace/Hopv047,Parry/Missv040,
Generic original sharkv048. Original definition/shark controller still historical;
v050/v051 staged only. Preserve original rigs/Avatar and all historical sources.
v054 paddles were authored, exported and native73frames×2clips×3views reviewed,
but human CANCELLED this feature; they are retained history, not next work.
Sole-scale-audit-v054 proves existing BakeMesh(true)+TransformPoint matches actual
native CPU bone skinning within2micrometres at origin. Do NOT change it to false:
that double-applies scale on this Unity6000.3 setup (observed10.6m error).

Next: continue opening paired-action/contact/performance polish, genuine real hop
case, all-layout safe exits, low FPS/pause/water/threshold/shore regressions and
continuous native/gameplay review. Preserve v055 NORMAL walking on wood throughout.
Overall opening work incomplete; retain the existing continuation automation.

'''
if not previous.startswith('# CURRENT — 2026-10-12 Vancouver — v055'):
    history='\n\n### Superseded v053 working record (all state/status historical)\n\n'+previous+'\n'+history
path.write_text(current+'## HISTORICAL NOTES'+history,encoding='utf-8')
print(path)
