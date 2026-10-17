## Oct15 v212 — fixed cinematic camera, human override

Latest human requires FIXED camera-to-Sahur distance during the cinematic and explicitly permits foreground occlusion. Read `Documentation/AnimationWorkflow/cinematic-fixed-distance-20261015.md` and `record-review-loop.md` first. Shared chase/climb/fight/fallen camera now stays18m from the actual chest with48deg lens, smooth authored spherical orbit and a2m aim leash; completion follow runs after all pose/solid correction. No automatic occlusion retraction, lift, side change, waterline pushing or FOV fitting. Never reintroduce these as a review "fix". Player/sailing/shipbreak cameras and physical contact/support/rig/actions remain unchanged. Latest real recording: `.codex/chase-entry-20261014/fixed212-final-multi-ground-hold-pause-10/`, camera metrics/movies/current source hashes retained. Original Editor stopped; diagnostic ended; fresh save protected/restored, no commit/push/new Editor/Player. Broader animation/contact/performance work remains unfinished in the v211 review below.

## Oct15 v211 — swimming/floating repair and actual recorded review

Read `Documentation/AnimationWorkflow/swimming-floating-review-20261015.md` and `record-review-loop.md` before historical checkpoints. Original sole Editor16920 STOPPED; no active recorder. Current source resumes the normal shark tail after shipbreak, forward torso swimming throughout neutral chase/dock/film beats, mesh-volume timber mass/net buoyancy/time-integrated held pressure, scoped Story1 wave timing1.0/new211 native physical replay and natural dynamic drift, frozen validated support-facing, breathing/face-up wave clearance, real chest follow-through and actual timber camera visibility. Never restore surfing/XZ locks/old camera or rig assets.

Latest source recordings are `shoulder211-playable-pause-30` and `shoulder211-multi-playable-ground-hold-10`; consult their actual verification JSON and movies. Both include the full20.8s plus genuine loss/float; the10Hz grounded/hold case is an explicit fixture. Broader full-animation art/alternative-board/performance work remains unfinished; clocked captures do not benchmark FPS. Preserve original ordinary062/walk/run/roll/mobile charge/noaudio/BWUI-colourimages,1000HP/both30%/random20–35s safe handoff and shore ending. No commit/push/new Editor/Player. Protected fresh save5A296481F7414AF9DCE308C6F62B990A89CE67F92A1E9268D46136D87C4BF803 restored, `.codex/float-swim-20261014/save-before.json`; protect newer progress. Intentional scene0D7A15BBC4A446F0BDEC25A0001E21436036BD8576926336D0991B7DF85B9269.

<!-- Historical checkpoints below; superseded by Oct15 review above. -->

## Oct14 v210 — pursuit admission/fixed scatter integrated; animation quality INCOMPLETE

Latest human direction is now `design-v210.md`; read `implementation-review-v210.md` before older checkpoints. The human's ACTUAL grounded admission failure was confirmed in the original running Editor. Both water/ground now use visible shark pursuit/swim, actual departure if grounded, moving-edge climb/turn and then the full20.8s main film. New Blender210 pursuit loops (retained53-bone source→original Pbr Avatar, retained16-bone shark) are native-reviewed/staged; humanoid importer180deg matching existing swim.214original fragments replay one6s physical fracture bake, then live vertical/tilt buoyancy with fixedXZ; native repeated releases match within.144mm. This supersedes old random-scatter rules; NEVER restore surfing. Protect the actual entry fragment; safe voluntary hops must also fit later tail contacts. Tail current-arc continuity penalty remains active.

Current30Hz natural/10Hz TRUE grounded-fixture runs both play pursuit+all20.8s with genuine lost-stick/float/defeat.30Hz four movies/key sheet retained;45pause samples0motion/clock drift. HOWEVER current30Hz first probe1.793m/reversal1.337m misses; current10Hz all6contacts(max7.762mm) but foot224.396mm FAIL. Visual foreground/water occlusion and full performance remain incomplete; do not call entire animation FINAL or use older all6takes as current acceptance. Next capture WorstFootTime/sole/deck traces, fix grounded-foot residual and natural first/reversal routes, then shot visibility. Sole Editor16920 STOPPED clean MainMenu/errors[]; no active recorder/Blender/Player or checkin/push/new Editor/network changes. Preserve140 sailing/smooth break cameras and original ordinary062/walk/run/roll/mobile charge/noaudio/BWUI-colourimages. **NEW HUMAN SAVE5A296481F7414AF9DCE308C6F62B990A89CE67F92A1E9268D46136D87C4BF803**, fresh `.codex/chase-entry-20261014/save-before.json`, restored after finalStop. Never restore853/DA0/AB/51C over it; protect newer progress.

## Oct 14 — skipped fight admission repaired; broader choreography incomplete

Read `Documentation/AnimationWorkflow/finale-admission-fix-20261014.md` first. Human's almost-missing fight reproduced: admission fell straight into 2.2s exhaustion with zero exchanges. Story1WreckFinale now samples the real combat grip instead of the swimming stow offset, searches real support within60m, and admits against continuous footing plus the opening exchange; later14/18s tail endpoints are still solved/contact-checked live rather than cancelling the whole film at entry. Natural water handoffs recorded full20.8s at30Hz625frames and different-seed10Hz209frames, no early-exit reason; throws/final defeats physically contact. Opening/reversal contacts STILL MISS in these takes: no all-contact/art/performance acceptance. Preserve both completed camera changes below, clips/rig/Avatar/controls/no-surfing protections. Sole Editor stopped clean MainMenu; own reviews ended; no checkin/push/new Editor/Player. NEW HUMAN SAVE AUTHORITY853AB760C2638AA1AE0300DF8C3676299FFEC1ABE3CC8F440960612B878E91BD, fresh `.codex/finale-admission-20261014/save-before.json` restored after finalStop. Never restore historicalDA0/AB/51C snapshots over it; protect newer human progress.

## Oct 14 — smooth shipbreak camera completed

Latest human request DONE: Story1WreckBattle no longer snaps the camera at Begin. It inherits the current140-distance sailing view, eases around the hull with a stable180degree branch, damps focus/position through tail impact and fracture, then hands orientation/velocity to normal player follow. The temporary handoff sweeps its actual camera travel and avoids the prior distant occlusion contraction. See `Documentation/AnimationWorkflow/shipbreak-camera-smoothing.md` and `.codex/shipbreak-camera-20261014/`. Final native30Hz235-frame game preview/keyframes reviewed; default/10Hz opposite-quarter beginnings0m/0deg cut, handoff0deg turn,20paused samples stationary, handoff state finishes, real tail contact retained. Fixed-timestep functional evidence only; broader choreography/FPS still incomplete. Scene140camera parameters, rigs/Avatar/clips, ship1.35/gravity/buoyancy, controls, timing and no-audio policy remain. Source edits now intentionally include Story1WreckBattle and ThirdPersonPlayerController camera code: preserve them against older v208/UVCS snapshots. Sole Editor16920 stopped in clean MainMenu; own tests ended; saveDA0 restored after confirmed final stop. No commit/push/new Editor/Player/network changes.

## Oct 14 — sailing camera pulled back

Latest human camera request DONE: Story1 ShipFollowCamera followDistance110→140; default offset scaled uniformly(height45→51.272728, side35→44.545452, lookHeight22 unchanged) to retain its default observation angle. Native Editor authoring followed by component-only scope preservation; EXACTLY three scene fields differ, no generated stairs/colliders or other serialized changes retained. Actual sailing140 game frame inspected; artifacts `.codex/sailing-camera-20261014/`. Single existing Editor was used; stopped clean MainMenu after validation. Current user save DA0 restored after confirmed validation stop. Story1 scene hash is now intentionally different from the prior UVCS/v208 manifest: preserve these camera settings, never blanket-restore the older scene. No animation/controller/rig changes, checkin or push. Broader animation v208 work remains incomplete below.

## Current checkpoint — Oct 14 v208 — overall work INCOMPLETE

Read `Documentation/AnimationWorkflow/Actions/CH1_SahurSharkEncounter_v001/implementation-review-v208.md` before historical entries. No active benchmark/recorder/Blender/Player; sole main Editor16920 is stopped in clean MainMenu, compiling=false/errors[]. Current active clips:171Parry/097Tail+ShipSmash;186/194/197/198 remain inactive.207 first-probe failure at5.987m was cancelled at16.083s and198 reverted. Active source: conservative192 whole-attitude budget +204 actual current-arc bite-entry penalty +206 query counters.204 actor-only supported fixture recorded625frames/all SIX real contacts(max.299mm),1.323mm feet/1.226mm slide,0escapes/overruns and held unconscious wave/shore ending; four movies/key sheet inspected.206 admitted-film10FPS passes all four hero contacts/final tail but reversal misses2.263m; no claim of full lowFPS/performance/art acceptance.189 actual moving-board auto-climb/fracture/ordinary moving attack passed.200/202/205 no reachable support, so999 means unattempted, not successful film. Rejected200 admission-facing and202 root-rebase trials are preserved outside Assets. Next: physical supported admission/remaining root route and reversal tail obstruction, then eligible real-board hop/full game art and performance. Do not overfit a lucky wreck or loosen contacts. See review208 for exact helpers/evidence and remaining work.

Save authority DA0FFBB38D301E72633F8830CCACCD5DA78F6A658E93CFF77822E7D791D20E84, `.codex/animation-continuation-20261014/save-before.json`; restored/hash-checked AFTER stop207. Preserve newer human progress.14 merged-file hashes rechecked: only intentional Story1WreckFinale changed; scenes/other13 match. No checkin/push, second Editor, Player/network/firewall changes, rig/Avatar rebuild or surfing/paddling. Prior wider physical scatter, voyage patrol, letterbox and minimal UI remain. All older RUNNING notes are superseded. Keep the continuation while entire objective is incomplete.

## Current continuation — Oct 13 — wider physical wreck scatter and inactive-launch fix

Latest human request: stronger shark tail strike should throw ship wood farther apart to reduce cinematic clutter. Completed physics change in Story1WreckBattle and Story1WreckFloatBody. IMPORTANT native root cause: assigning velocity to prepared INACTIVE Rigidbody silently produced zero velocity. FloatBody now retains intended linear/angular launch, applies once on activation (or immediately if active), and clears pending so later enable never relaunches. Preserve this fix. Default shipBreakPower FINAL=1.35, radial source-centre fan12–22*(power-1), vertical factor1+.25*(power-1), local parent-cut shard separation1+2*(power-1); later board-attack impulse formula unchanged. Original meshes/fragment counts/spawn positions/gravity/water forces remain. First1.8 trials beforeactivationfix ineffective; actual57m/s overtuned1.8take rejected, not final. See Documentation/AnimationWorkflow/wider-wreck-impact.md and .codex/wider-wreck-impact-20261013/. Final native2runs originalEditor, diagnostic early tail request/samefractureseed (not strict controlled A/B):214pieces/31boards, actual median release31.21m/s, 12s median travel126.02m/nearestboardcentres28.37m/23usableboards, tailgap.122mm, activegravity/finite/nonkinematicPASS. Controlenabled/unlocked, immediate supportfalse both power1 andfinal; do NOT claim stable landing/climbing accepted. Last6.5sgameframe shows swimming beside realplank. Original20.8sfilm/control/contact/performance work still INCOMPLETE. Cinematic2.39bars/priorpatrol/minimalUI remain. SaveDA0FFBB38D301E72633F8830CCACCD5DA78F6A658E93CFF77822E7D791D20E84 restored/hashchecked after actualstop; preserve any newer human progress. No second Editor/Player, scene/rig/resource save, commit or push.

## Current continuation — Oct 13 — cinematic letterbox

Latest human request: flatter AAA-style framing during story animations. Implemented reusable scene-owned CinematicLetterbox at 2.39:1, .3s transitions, no projection/aspect stretching; enables for tailbreak, paired finale/defeat, blackout, fresh shore arrival/getup and releases for playable combat/island control. Continue bypasses arrival request. Actual-subject finale camera fit reserves visible height. Bars sort at450 below the existing pause/death Canvas500 and arrival blackout/subtitle900. See Documentation/AnimationWorkflow/cinematic-letterbox.md and .codex/cinematic-letterbox-20261013/ (native layout PASS; original Editor runtime phase/capture review). No animation/rig/physics/input edits for this request. Broader opening-animation/contact/performance work remains INCOMPLETE; do not treat this framing feature as final approval. Current user's save authority remains DA0FFBB38D301E72633F8830CCACCD5DA78F6A658E93CFF77822E7D791D20E84, backed up before this task; preserve any genuinely newer user progress. Prior voyage patrol and terse UI changes remain active below.

# Current continuation — Oct13 — voyage shark patrol integrated; current user save DA0

Latest user request: make sailing less empty by having the SAME shark circle the boat and occasionally breach. Implemented only in Assets/Scripts/Story/Chapter1/Story1SharkCollisionSequence.cs (SHA25660A57CFD36F161F9FCD5969CB4AC607FBEE65DF5F077D13BFC66B2FAAAE187B6). Read Documentation/AnimationWorkflow/voyage-shark-patrol.md. Uses original rig/Swim/Breach clips, actual hull/animal clearance, bounded horizontal motion, gravity flight and gradual recovery, random opportunities with actual-hull ray and FOUR sail/flag render-bound checks at current/apex positions. Does not use a filled whole-ship render AABB. Current hull implies about44.15s one lap, then8s regular bow approach and existingtailbreak. Patrol animation ownership explicitly released on the exact transition/disable, including variable timestep. No ship/Avatar/mesh/animation resources, player input or physics fragments modified for this request; no new audio or combat text. Earlier minimal boss HUD cleanup remains active (name/HP only; Cappuccino ultimate texts removed; shark attack commentary removed).

FINAL native-review/ complete:854actualframes x game/wide15FPS (simulation30), encodedtwoMP4s/contact-sheet/review.json in .codex/shark-voyage-patrol-20261013/native-review/. Circle359.45deg/THREEbreaches2.20,13.43,35.67s;413breachbody-viewportframes. Actual game keyframes personally inspected: left/right breaches visible, later player-observation angle shows breaching shark at frame edge; this is not claimed as normal-speed FINAL artistic approval. Ordinary rest-bounds exposuremax14.07%, horizontaltraveloverruns0, conservative hull-envelope clearancemin23.57m.25pausedframesheld.52.50s entersoriginaltailbreak and reachesFighting, tailgap0.122mm. Game-view camera can freely move; production patrol never steers it. The diagnostic wide camera is restored after each manualrender. Initialdiagnostic staging reset beforefirstcapturedframe, originalclock0.02s, NO mid-recording reset/teleport/placedwood. Historicalfirstpass(twojumpsoneoffcamera), late-start(incomplete,do notuse asfullproof), collider-onlyvisibility(hiddenbysails), all-renderer-visibility(overrejectedsidechances) preserved separately. Current cloth-only source is the successfulfinalrecording.

ACTUAL 10FPS/realkeyboardhelm test .codex/shark-voyage-patrol-20261013/steering-10fps.json PASS:441patrolframes, ±helmmax28deg,0horizontaloverruns, hull-envelope min2.39m,0nativepenetration,90approachframes/0frozenAnimator, entersBreaking. Temporary keyboard/queryfixture removed. CLI timedoutbutreport/actualautostopproveONEcompletedrun; no duplicate. Both test helpers explicitly prepare diagnostic starting state, not production runtime rewinds. Tests are Editor functional/visual/lowFPS evidence ONLY, not independent1080p performance acceptance.

IMPORTANT NEW SAVE AUTHORITY: user save AT THE START OF THIS REQUEST was already NEWER than oldAB. Snapshot .codex/shark-voyage-patrol-20261013/backups/save-before.json SHA256DA0FFBB38D301E72633F8830CCACCD5DA78F6A658E93CFF77822E7D791D20E84. Live C:/Users/pinyu/AppData/LocalLow/DefaultCompany/Dark Brine_Rot/dark-brine-rot-save.json restored and verified IDENTICAL DA0 afteractualfinalStopped. NEVER restore oldAB or9BD snapshots; preserve genuinelynewerhumanprogress on futureturns. Each native diagnostic save preserved separately under this task's backups. Original Editor PID22012/port7800 STOPPED, compilingfalse, sceneclean, autotickTRUE16ms. No currentcapture/input/CLI/Blender/Playerworker. No scene save/commit/push/newEditor/newnetworklistener or firewall/buildconfig changes. Human network constraint remains in network-test-launch-constraint.md.

This sailing feature and the minimal Boss-UI cleanup are done. The older full20.8s paired-opening/control/performance objective remains INCOMPLETE: denseplayable emergencyescapes and firstprobe/reversalmisses/wholeperformance/artreview remain as described in v181/v176 reviews and v184checkpoint below. Do not mark the entire animation project achieved or delete5-30 on this sailing feature alone. Physical wood-blocking kinematic geometry remains only an IDEA, neverimplemented. Existing collision181/175/176, parry171/slash128/hop086/bite078/tail097/throw064/originalordinary062/walk/run/roll/mobilecharge/1000HP/both30%/random20–35safehandoff/no surfing/freefloatingmeshcollision/unarmedfaceupshore protections retained. Historical checkpoints below superseded on saveauthority/currentextras.

# Current continuation — Oct13 — v184 checked / whole objective INCOMPLETE

Original Editor PID22012/port7800 actually STOPPED, compilingfalse, scene clean, autotick TRUE16ms. No Player/Blender/capture/CLI worker remains. Read implementation-review-v176.md and implementation-review-v181.md before historical entries. Current source Story1SharkWreckContact.cs SHA25641E97D709206A4AA7C45DF330FAE13998181C016561E3A0EFECD2928D0F2C3F6; active175 shortened root+bone advance,176 exact query geometry reuse,181 correct-centred current-pose recovery. Original158/162/163 remain;166OBB remains REJECTED/absent. No new animation assets/controllers/bone rigs were changed this turn;171Parry/128Slash/086Hop/078Bite/097Tail/064Throw and original062ordinaryattacks/walk/run/roll remain. No surfing/driving/paddle/audio; realmesh/gravity/buoyancy, BWminimalUI/colourpictures and loststick/wavefloat/shore preserved. No scene save/commit/push/extraEditor/network/config changes.

175 keeps original four full-root attempts then tests75/50/25% of BOTHroot/articulatedpose; caller keeps ACTUAL solved root/heading rather than forcingfull destination. Native partial-root-v175:256paths/104originalsuccessesunchanged/27additional/131total/0mismatch/0residual, distancefloat excess0.0556mm. Actual rootpartial counts17688/17751/182104/183113; full173earliergenuine086hop remains valid, currenttakes nohop.

176 exact radius/height capsule cache<=32peranatomy/128total, no rounding/shrink; fallbackoriginalmutablegeometry. Native3200comparisons/320cachedposes/259nativepenetrations/0mismatch/difference. Editor microquery accumulated21.149mutable vs19.377reuse ms ONLY, NOTgameFPS. Native184 currentAABBbroadphase independently checked4000pairs/1626unfilterednativehits/0falserejects. No OBB/padding edits; differentlayout doesnot retroactively classify167oldpath.184 firstCLIrequestaborted byplaydomainreload; actualevalverifiedno benchmark/observer beforeONEretry. Auditstoppeditsownbenchmark after4000, notfullfilm.

181: original retainedpose recovery stillfirst; additionally searches CURRENTarticulatedpose in permittedball centeredonpreviousACTUALroot, not only aroundalreadydisplacediterative-result. Everyacceptedpoint allnative3mmchecks/unchangedframebudget. Native183targeted160hypotheticalorigins/110accepted/EIGHToldretainedfail-newcurrentclear/0mismatch,residual,excess; temporarybones/root/queryorigin sampled/restored, nowoodmoved. Production recenter counter0in182/183naturallayouts, so naturalbranch remainsunobserved; do NOT claimallteleportsfixed. No speculativeearlyheadliftasset/timing or physicalkinematicwoodbarrier changes made.

Clean10FPSnative177 full20.854s/209filmframes all6contactsPASSlargest14.377mm,4hero2shark,0traveloverruns/residual2.966mm(existing3mmfallback),feet1.126mm/slide1.209mm,WHOLErun0emergencyescapesinthiscase. This numericalcase isNOTmovieorindependentperformance. Actualcontrol178prefabfixture: jumps60/20/10FPS2.698/2.697/2.697m against2.7,onejump/landeach,coyote/buffer/movingboardmomentum4.472m/s/ceilingPASS. Swim8normalfastxfourdirections movementdot1/facingminimum0.999948;headingposemin0.999736. Actualinput179charge+sprint+turn+reverse timercontinuous/movingheavyrelease/sprint16.8m/s/ordinaryleftmovingoverlayPASS. Fixtures/devicescleaned/saveExcluded, noclimbacceptanceinferred.

Native180 full625frames/fourmovies ENCODED native-paired-v180,contactsheet/gamekeyframePERSONALLYinspected; probe/reversalMISSED2.324/3.384m,other4<=9.567mm,3hero1shark. FilmONE3.717m emergencydelta8.925s/2.617moverrun;playable1.307m;feet1.341mm/slide1.317mm. Artdefects: largeforegroundrealcutsobstructaction/probe1.19doesnotreadasparry/sharkdidnotreach;breach12.8andhorizontalfloatreadable. Fullnormal-speedartapprovalunobserved. Ending270samples/24pause/0upright/0clockdrift/0pausedmotion/loststick/unarmedunconsciousshorePASS. Framing541/crown0/feet0PASSscreenboundsonly. Latest all-six-successfulfourviewmovie remains174, NOT180. Native182AFTER181cleanfull20.824s625frames firstprobeMISS3.132m/fiveothers<=1.474mm,film0overruns/residual1.822mm,feet1.326mm/slide1.378mm,ONEplayable1.307mescape. Native183full20.801s includesearlierhypotheticalfixture, reversalMISS2.106m/other5<=38.849mm,film0overruns/residual0,feet1.024mm/slide1.358mm,TWOplayableescapes6.031/1.957m. Differententry/evolvinglayouts, no causalA/Bclaim. Wholecontact/escaperobustness stillFAIL.

HUMAN NETWORK CONSTRAINT remains: network-test-launch-constraint.md. NO independentlynumbereddevelopmentPlayerEXEs/listeners; anotherchatinvestigatesnetwork/config. NoFirewall/rules/Allowworkaround. ExistingEditorONLYuntilfixedidentity/unnecessarylistenersverified. Independent1080ptarget60FPS/1%low>=50 remainsFAIL/unverifiedlatest; previous16136.8351%lowFAIL and165fallback59FPSNOTfullfilm. No new standalone launched this turn.

SAVE AUTHORITY pre175/save-user-after-play-052419 SHA256AB575DF9484F0327E8FC7AD7B7BD9B123ADB85D13F4E9A627F3F40A38D1EC0F3. Latestlive C:/Users/pinyu/AppData/LocalLow/DefaultCompany/Dark Brine_Rot/dark-brine-rot-save.json restored/verifiedAB AFTERactualStop184. Diagnostic175/176/177/179controls/180/182/183/184savedseparately underbackups. Protect genuinelynewerhumanprogress; neverold9BD/backups/save.json. Sourcebackups175/176/181 preserved. No animationbackupinstallers rerun.

Next: actualdense-playablewoodintrusion/emergencyescapes, firstprobe/reversaltail reachability/foregroundvisibility and finalartpolish. Current solver anatomy volumes are query triggers; a physical blocking solution for incoming dynamicwood is only an IDEA, NOTimplemented or approvedfinaltested; inspect collider ownership/player exclusions/kinematic velocity before choosing any change. No arbitrarygeometrypad/shrink/deletion/falsehits/inventedwood/vehiclerestore. Actualmoving-edge climb/continuousmixedwaterwreckcontrols stillpending beyond178/179 targetedfixtures. User wantsautonomouscontinuation/noquestions. EntiretaskINCOMPLETE; do notdelete5-30orclaimfinalacceptance. Historicalentriesbelow superseded.

# Current continuation — Oct13 — v174 checked / whole objective INCOMPLETE

Original Editor PID22012/port7800 STOPPED, actual scene clean, autotick TRUE16ms. No Player/Blender/capture/preview/CLI worker active. Read implementation-review-v171.md(updated173/174) and v169.md before historical states. Native174625frames/four movies ENCODED in native-paired-v174; actual contact sheet inspected. ALL SIX gaps10.214/45.331/0/19.224/0/0mm PASS,4hero2shark,source60/record30,film20.824s; same-board/no-safe-neighbour variant. Film motion0overruns/residual0, feet1.218mm/slide1.222mm PASS. Ending270samples/25pause/0upright/0pausedmotion/0clockdrift/loststick/unarmedunconsciousshore PASS. Entry0.918s. TWO larger emergency escapes happened during playable Fighting before film(2.163/1.464m); whole robustness NOT accepted. Continuous normal-speed artistic approval remains NOTobserved. Latest all-six successful complete preview is NOW174, replacing146.

NEW actual bone animation171 ACTIVE FIRST PASS integration accepted: original047/09028bone rig/rest/skin/parents retained, versioned ArtSource/Encounter/v171/sahur-open-water-parry-v171.blend/ActionCH1_SahurParry_v171. Right-hand forward/up arc modified with real shoulder/elbow rotations and original reach clamp, quaternion hemisphere continuity. Source ankles drift<0.315micrometres; native402originalskin images compare090/171: actual contacthand+0.299mY/total0.319m, lower-arm length variation25.5micrometres, max hand frame step36mm/60FPS. Raw native feet42.6mm before runtime IK vsold40.6mm; runtime174support PASS. Avatar originalPbrSahur valid/human,130curves/60FPS/1.1s/contact.484. Resources/Encounter/v171/CH1_SahurParry_v171 installed ONLY Parry beat; fullcurrentdefinition backup Resources/Encounter/v171/PreviousEncounterDefinition.asset. NEVER rerun installer over backup. Other attack/walk/run/roll/charge/controllers unchanged. Active128Slash/086Hop/078Bite/097Tail/064correctedThrow retained. No speculative new bite or hop-gating change was made.

IMPORTANT positive086 observation: native173 actually LANDED ONE genuine neighbour hop(BoardJumpCount increments only when LandHop changes real support). Active definition verified bothHopbeats086. No placed/moved wood.173fullfilm20.808s firstprobeMISSED3.762m butfiveothers0,3hero2shark;625motionrows/residual0/nooverrun,feet1.228mm/slide1.247mm. One4.701m emergency escape occurred in earlier playableFighting.173was numerical full-run observation, no retroactive video claim. CLI Start response timed out but observer proved ONE run started/completed; do not duplicate timed-out detached calls blindly.174is different evolving layout, so no controlled causal comparison.

Runtime169 fixes leftover surfing-oriented initial fracture: original flat deck sections(thickness<=1.2m) farther than18m from actualtailHullImpactPoint now detach whole at real source positions. Other existing original cut-source release/near-impact splinter logic remains; true gravity/buoyancy/meshcollision and later shark breaking remain. Native170initial214/214fragments/sevenwholethinoriginals/sourcepositionerror0, meshes/breakability PASS. OnlyTHREEoriginalflatcuts matched oldsmallstrip rule;169 newlypreservesTWO farther ones, not allseven. Actual later shark attacks may break these;170/173usedreal splinter,174usedoriginalHullboard3. No surfing/driving/paddle/audio restored;BWminimalUI/colourpictures preserved.

Collision ACTIVE158exactrecentblockingpart rejection(3200nativecomparisons0mismatch),162same-solve failed-retained-search reuse,163actualfinite-boundary escape probes(24positiveactualmeshcases0radiusoverrun).166oriented-box pruning REJECTED:4000pairs/1664nativehits/TWOunexpected positiveNativepredictednoncontacts(max88.304mm); unknownold-pathclassification, do NOTguess padding or enable. Candidatepreservedbackups/v166/RejectedOrientedBroadphase.cs, source restored to163 and compilationPASS. Backups/v172 unused pre-change copies; no extra diagnostic source hooks installed.

HUMAN NETWORK CONSTRAINT: read network-test-launch-constraint.md. NO new numbered standalone-vXXX network-listening EXE launches; they caused repeated Windows permission prompts. All known157/161/165Players quit; inventoryconfirmednone. Continue through existingEditor ONLY. Another chat investigates listeners/config; do not concurrently edit those resources. Never disableFirewall/addallowrules/clickAllow workaround. Resume independent profiling only after unnecessaryPlayerConnection/Pipeline listeners addressed and fixed runtime identity verified. Prior16559FPSresult is unconsciousfallback(no fight), NOT full performance acceptance. Priorfull1611%low36.835FAIL;157dense179ms/11>50msframesFAIL. Wholeperformance remains unverified/incomplete.

SAVE AUTHORITY .codex/opening-optimization-20261012/backups/save-user-after-play-052419.json SHA256 AB575DF9484F0327E8FC7AD7B7BD9B123ADB85D13F4E9A627F3F40A38D1EC0F3. Live C:/Users/pinyu/AppData/LocalLow/DefaultCompany/Dark Brine_Rot/dark-brine-rot-save.json restored/verifiedAB afteractualStop174 and preserving save-after-v174-native.json;173/170/167/164/160/159/156also preserved. Never old9BD/backups/save.json; protect genuinely newerhumanprogress. No scene save/commit/push/extraUnityEditor/duplicateautomation. BlenderMCP9876stillrefused; used existing portable5.2.2 -b CLI withoutMCPserver, workersfinished.

Next work: dense-layout contact/entry and playable emergency-escape robustness, measured planningCPU(known25+ms) and final artistic polish/normal-speed review. LowFPS and continuousplayerinput regressions remain. Genuine086positive branch now mechanically observed in173;174full allcontact same-board variant covers fallback. Do not blindly weaken support/contact or inventwood. Maintain1000sharkHP/both30%/random20–35safehandoff, oldordinary062attack/walk/run/roll/mobilecharge, originalrig/Avatar/history, realcutmeshcollisions/naturalphysics and heldfaceupwavefloat/loststickshore. User wants independent work/no questions. EntiretaskINCOMPLETE; do notdelete5-30 or claimfinalacceptance. Historical checkpoints below superseded.

# Current continuation — Oct13 — v168 checkpoint; whole objective INCOMPLETE

Original Editor PID22012/port7800 actually STOPPED and scene clean; autotick TRUE16ms. No DarkBrineOpening.exe/Blender/capture active. Current collision source ACTIVE v158/v162/v163, compilation PASS. Experimental v166 oriented-box broadphase REJECTED after native167:4000pairs/1664nativehits/TWO predicted noncontacts with native positive penetration(max88.304mm). Cause/old-path classification undefined; do not add arbitrary padding or re-enable without better evidence. Candidate preserved backups/v166/RejectedOrientedBroadphase.cs; exact candidate hash checked before restoring immediately preceding source163. No unrelated changes discarded.

NEW HUMAN CONSTRAINT: read network-test-launch-constraint.md. Stop new numbered standalone-vXXX network-listening EXE launches: they repeatedly request Windows network permissions. All our Players157/161/165 already quit; inventory confirms none. Continue via existing Editor only. Another chat investigates listeners/config; do not concurrently edit network/build configuration. Never disable Firewall/add allow rules/click Allow as workaround. Independent profiling may resume only after unnecessary PlayerConnection/Pipeline listeners are addressed and a fixed runtime identity verified. Older instructions to launch a new numbered EXE are superseded.

Read implementation-review-v158.md, v163.md and v166.md. v155 narrow stable-mesh finale handoff PASS targeted native160: CC.isGroundedfalse/actualsettledtrue; ascent/.35m broad grace/disabled board rejected; actualadmission0.0333s/fromWaterfalse. Actor-only fixture is NOT natural no-teleport acceptance. Native156 water-only/inconclusive retained. v158 exact recent-part rejection3200comparisons/170penetrating/0mismatch/0accepteddifference/1337earlyrejects PASS native159. v162 avoids repeating an identical failed retained-pose search inside one synchronous solve. v163 adds actual finite escape boundary after original ordered radii; native16424/24original-mesh query cases find a real bounded clear point where old fixed radii fail,0mismatch/0radiusoverrun. No wood/actor root moved by query fixture. None proves universal dense-layout acceptance.

Latest source163 independent165 Succeeded20.709s/0errors/1warning, actual1080p/DX12/4060/2999GPUfences0errors. NO reachable safe combat support,19.014s entry then honest unconscious fallback; counters0/0, all6gaps999(unattempted). Reported59.87FPS Finale is fallback ONLY, NOT full-fight acceptance. Playable1%low46.205FAIL;0largeescapes in this take. Prior full fight161 source158:20.806s/3hero2shark/firstprobeMISS1.204m/fiveothers<=44.328mm,1%low36.835FAIL,max34.245ms,0>50ms active phases but4largeescapes(onefilm11.925s2.809m). Prior157 all6PASS<=37.586mm butdensefilm179.363ms/11spikes>50/1%low9.669FAIL. Freely evolving layouts mean no controlled A/B FPS claim.

Native full164 source163:20.814s, gaps3.525/.760/.009962/0/9.127/0m, THREE real misses, no fakehits.625motionrows/residual.522mm/ONEoverrun.239m(actual1.339m15.481s). Whole movement/contact FAIL. No genuine086hop in current tests. Latest all-six-successful continuous four-view movies remain native-paired-v146; inspected keyframe/contact sheets, normal-speed full artistic review NOTobserved. Active128actual Blender body-led Slash remains FIRSTPASS, parry090/hop086/bite078/tail097/throw064; original062ordinaryleftattack/walk/run/roll/mobilecharge retained. NEVER overwrite installer PreviousEncounterDefinition/PreviousEncounterShark or original rig/Avatar/history.

SAVE AUTHORITY backups/save-user-after-play-052419.json SHA256 AB575DF9484F0327E8FC7AD7B7BD9B123ADB85D13F4E9A627F3F40A38D1EC0F3. Live C:/Users/pinyu/AppData/LocalLow/DefaultCompany/Dark Brine_Rot/dark-brine-rot-save.json restored and verified AB after actualStop167, preserving save-after-v167-native.json. Diagnostic156/159/160/164 also preserved. Never old9BD/backups/save.json; protect genuine newer human progress. No surf/drive/paddle/audio; BWminimalUI/colourpictures; real meshcollision/gravity/naturalfloat; heldwavefloat/loststick/unconsciousshore. No commit/push/scene save/extraEditor or duplicateautomation.

Next: existing-Editor work only; dense contact/avoidance and planning CPU remain major defects. Exact facing planner may still consume25+ms; consider measured progressive/sliced work while preserving fully native-validated candidates and honest hits. Cold/warm timing and entry preparation vary; no safe surface must retain existing fallback. Genuine safe086 branch, full normal-speed multiview/gameplay artistic review, playable motion/charge/air/climb regressions and independent performance remain incomplete. User wants autonomous continuation/no questions; do not mark whole complete or delete5-30. Historical checkpoints below superseded.

# Current continuation — Oct13 — v163 active / native164 complete; network-prompt workflow needs fixing

Original Editor PID22012/port7800 actually STOPPED, scene clean, autotick TRUE after native164 completed. No Player/Blender/capture active. Diagnostic save164 preserved and live AB575DF9484F0327E8FC7AD7B7BD9B123ADB85D13F4E9A627F3F40A38D1EC0F3 authority restored. Latest user asks why repeated network-access prompts: confirmed44 firewall rules for22 distinct standalone benchmark EXE paths; v161 boot.config has player-connection-mode=Listen and LAN addresses. These development builds keep receiving new paths. DO NOT change Windows firewall/security or launch another versioned EXE blindly. Use a fixed execution path and remove unnecessary development-network listening for the next offline benchmark; this workflow fix has NOT yet been implemented or verified. Preserve existing historical builds and original Editor control.

v155 real-mesh supported handoff native160 PASS: actual settled support despite CC.isGroundedfalse, ascent/grace0.35m/disabled-board rejection,0.0333s admission/fromWaterfalse. Actor-only test setup, no wood repositioning; not natural admission proof. v158 native recent-blocker rejection equivalence3200comparisons/170penetrating/0mismatches/0accepted difference PASS. v162 avoids duplicate identical failed bounded retained search in one synchronous solver. v163 adds actual finite-radius boundary probes after original ordered probes fail; all original native anatomy/3mm clear checks and movement limits retained. Compilation PASS. Native164 hypothetical-query fixture24/24 finds valid bounded escapes missed by old stepped radii,0mismatches/0overbudget; only query volumes temporarily shifted/restored, no actors/wood moved. Full native164 motion625samples residual0.522mm BUT ONEtraveloverrun0.2389m/maxstep1.3389m: robustness NOT accepted. Read native-profile/pose-motion-v164 and implementation-review-v163; no claim whole fixed.

Independent161 source158 completed/quit:4534GPUfences0errors, naturalTimer20.3896s,film20.8059s,3hero2shark, firstprobeMISS1.204m/fiveotherswithin44.328mm. All activephases0>50ms one evolving case, film1%low36.835FPS FAIL, entrypreparation21.846s,4largeemergencyescapes (onefilm11.925s/2.809m). Source163 newer independent measurement pending. Previous157 all6contactsPASS but film179ms/1%low9.669 shows dense-layout failure; no causal A/B claim across evolving wood. Latest four-view allcontact successful movies remain146; genuine086hop/fullnormal-speedart not accepted. Entire objective INCOMPLETE. Historical states below superseded.

# Historical checkpoint — v155

Original Editor PID22012/port7800 actually STOPPED and scene clean after v156. Autotick TRUE. BuildOpeningBenchmarkV157.Start has been dispatched ONCE; check build-report-v157.json/build-error-v157.txt before further Editor work. No Player/Blender/capture active at this checkpoint. Do not start another Editor or duplicate build.

v150 compiled and native153 completed: first-hop grading now cancels before the second lead-in; second search starts6.432s instead of old8.870s. Attempts1/2, no genuinely safe086hop. Three real contact misses8.025/.938/.702m in this dense layout; no fake hits. pose-motion153 has614 sampled rows, residual0/overrun0, but a prior playable large escape remains recorded. Latest all-six successful continuous movies remain native-paired-v146, not universal/final art acceptance.

v152 DEVELOPMENT diagnostics records the worst1% frames and explicit camera CPU submission; workload/render/fence unchanged, GPU time unmeasured. v155 compiled: pending finale accepts a physically settled floating mesh via HasStableSupport instead of permanently false retired IsSurfing. Narrow actual mesh gap−.03…+.06m, rejects climbing/swimming/ascending/broken support. v156 diagnostic did NOT establish the supported case: actual actor fell into water; water handoff passed0.033s but supportedSeenfalse. Keep this as an inconclusive fixture, not acceptance. Need real supported predicate/air-grace regression.

SAVE authority remains AB575DF9484F0327E8FC7AD7B7BD9B123ADB85D13F4E9A627F3F40A38D1EC0F3 at backups/save-user-after-play-052419.json. After actual STOP156/scene clean, preserved save-after-v156-native.json and restored live AB. v153 diagnostic save also preserved. No surf/drive/paddle/audio, original ordinary attacks/walk/run/roll and mobile charge retained, no scene save/commit/push.

Next: run independent157 actual1080p/DX12 benchmark with natural water trigger and slowest1% details, autotickFALSE only while Player profiling then restoreTRUE and verify save. Remaining whole targets: film1%low>=50 (145measured42.857FAIL), cold waiting stall, playable large escape, dense contacts, genuine safe086hop and continuous normal-speed artistic review. Historical checkpoints below superseded.

# Historical checkpoint — v150

Original Editor PID22012/port7800 STOPPED, autoplay tickTRUE16ms; no Player/Blender/capture active. Read implementation-review-v150.md before historical states. v150 source compilation was requested; check recompile_status before any Play/build. Latest source introduces a real deadline inside sliced neighbour/contact grading, so a failed first-hop search cannot consume the later hop's lead-in. Default infinite deadline preserves other entry searches. New actual source v148 retry every0.6s within lead-in; unchanged anatomical/support criteria, no late redirect/placed wood. v149 native attempt counts6/2, no safe hop; search begun4.404s occupied work until8.870s, proving need for v150 inner cancellation. v148 initial missing List namespace compile error FIXED immediately with fullyqualified Generic.List; actual priorcompilePASS. v150 regression not yet run.

Active v128 body-led Slash (actual original65bone Blender+130Humanoidcurves) FIRSTPASS integration, parry090/hop086/bite078/tail097, throw064 corrected, previous ordinary leftattacks062/walk/run/roll and mobilecharge unchanged. Never overwrite original rig/Avatar/mesh/history or installer PreviousEncounterDefinition/PreviousEncounterShark backups. No surf/drive/paddle/audio, BWUI/colourpictures, natural meshcollision/gravity/buoyancy.

v138 swimming admission now uses actual existing15% SurfaceRootY instead of raised contact root height, all whole-swim/path/native collision/movement checks retained. v140 preferred natural yaw-zero attitude only wins after full same endpoint/intermediate anatomy/speed validation; obstructed candidates use original broad search. v142 registers original animated-body/fragment physics pairs during existing2ms sailing warm-up, no movement/input ownership before actualhandoff. Real nativev144:23088inactive and19936released pair flags ALLcorrect,386cachedshapes/221active/16bodycolliders; riderhandoff0.0904ms; all6contactsPASS max36.695mm,625wholefilm motion0residual/0traveloverrun,sole1.260mm/slip1.356mm. Generic actuallocalPhysicsScene activationv139 proves ignoreflags persist through activation/body reenabling. Initial helperEditorSceneManager API usage corrected; no production collider change beyond equivalent prewarming.

Latest independent build145 Succeeded18.104s/0errors/1warning. Actualseed20261015 naturalTIMER trigger sampled20.378279s, observed20.388533s;3270completedGPUreadbacks0errors,1080p/DX12/i5-14400F/4060. Film20.802582s/fourhero-twoshark/all6contactsPASS [0,0,.040686,.005901,.004258,.004228]m,sole1.278mm/slip1.739mm,0hop. Rider0.1559ms. Break44.322/firstplayable47.118/entry42.644/filmmax33.851ms, all activephases0frames>50. ColdWaiting370.592ms still. Filmmean16.798ms/1%low42.857FPS FAIL>=50. Solidresidual0.800mm but TWOlargeescapes in playableFighting(delta1.278/2.057m); neitherinfilm, notfullmovementacceptance. No controlled causal FPS comparison with freely evolving other layouts. PlayerPID43580 finished/quit; autotickTRUE restored andABsaveverified.

Latest complete continuous native146:625frames/fourENCODEDmovies(native-paired-v146), ALL6gaps[0,0,0,.002642,.000068,.006992]m PASS,sole1.222mm/slip1.269mm. Contactsheet personally inspected. First anticipation now submerged; right-hand impacts/readabledeclines, normal-speedfinalart NOTobserved. Ending265samples/25pause/0upright/0clockdrift/0pausedmotion/loststick/unarmedunconsciousisland PASS, waves5.702m/rock15.091deg/headfreeboard.040m. No genuinehop; readOnlyhop-eligibility shows1raw eligible later neighbour but nullplans. Raweligibility does NOT prove full safe landing or contact stance. v146 observers tied to ownbenchmark (oldv141motion observer accidentally sampled an earlierendedsceneONEframe and is rejected as motion evidence). v138 earlierdense take hadtwo misses3.540/.807m,146latestallpassdoesnotprovealllayouts.

Latest native149 with retries:20.803630s/3hero1shark, firstprobe3.516m and reversaltail1.719m misses, nofakehits.625motion0residual/0traveloverrun,sole1.224mm/slip1.394mm. Neighbourgrading found no valid stance,6/2attempts. Actualnew source150 deadline cancellation needs native and independent verification; do not treat149 asfullacceptance. Files native-profile-v149/pose-motion-v149 available.

SAVE AUTHORITY .codex/opening-optimization-20261012/backups/save-user-after-play-052419.json SHA256 AB575DF9484F0327E8FC7AD7B7BD9B123ADB85D13F4E9A627F3F40A38D1EC0F3. Live C:/Users/pinyu/AppData/LocalLow/DefaultCompany/Dark Brine_Rot/dark-brine-rot-save.json restoredAB afteractualStop149 and preserving backups/save-after-v149-native.json. Previous132/135/138/141/144/146savesalso preserved. Never old9BD/backups/save.json; protect genuinely newerhumanprogress. No scene save/revert/commit/push/extraEditor or duplicateautomation.

Next: verify v150 bounded hop deadlines and actual safe neighbour branch, diagnose slowest1%frames (currentreportonlyspikes>35misses33ms frames), remaining filmFPS/cold/initialization and dense-contact/earlyplayableescape robustness, actualmultiview/fullspeedart. v146 movies usable as latest complete mechanical all-contact preview, not FINAL approval. EntiretaskINCOMPLETE; do notdelete5-30. Historical states below superseded.
# Current continuation — Oct13 — v137 measured; v138 native review IN PROGRESS

Whole objective INCOMPLETE. Read implementation-review-v137.md before historical states. Original Unity Editor PID22012/port7800 only, autotickTRUE16ms. Native continuous four-view v138 was started ONCE via Tools/RunReviewV138.cs RunReviewV138.Start. Do not duplicate captures/builds, edit Assets or launch another Editor while it is running. Check native-paired-v138/review.json, ending-v138.json, framing-v138.json and submerged-entry-v138.json before stopping and backing up diagnostic save. No independent Player or Blender worker active.

Completed v132:625frames/four encoded movies; sole1.107mm/slide1.305mm; TWO contacts fail1.498/1.810m in dense wood, no false hits. Ending278samples/25paused/0upright/0pausedmotion/0clockdrift, loststick/unarmed unconscious island PASS. Actual contact sheet inspected: ordinary probe shark is too exposed, foreground water/wood can occlude action. Normal-speed final art NOTobserved.

Current v128 actual body-led Slash remains FIRSTPASS integration trial; original mesh/Avatar/rest retained. Original ordinary left attacks062/walk/run/roll, throw064, bite078/parry090/hop086/tail097 unchanged. Never rerun installers over PreviousEncounterDefinition/PreviousEncounterShark backups. v131 native recalculateBounds/axis-aligned8corners original capsule equivalence20poses<=1.176micrometres, fullscan56.731->1.968ms. v124 fastsole/v126 readonlyPlayer footmetadata active.

v133 independent1080p/DX12/4060:all6contactsPASS,maxgap44.379mm,sole1.245mm/slide1.512mm; first playable frame59.052ms,film entry43.930ms,filmmax50.927ms/1%low33.50FPS FAIL. v134 searches fully-validated physically clear routes first, falls back to original exact blocked-route ranking. Nativev135 dense-layout motionPASS(residual2.439mm/0traveloverruns) but contacts fail3.711/3.135/1.103m. Honestmisses retained.

v137 natural-trigger actualPlayer completed and quit:3653GPUreadbacks0errors,1080p/DX12/i5-14400F/4060,20.810s/fourhero-twoshark,all6contactsPASS max4.867mm,sole1.323mm/slide1.474mm,residual2.461mm/0largeescapes,0genuinehops. Actual reasonPlayerHealth after27.889s, sampled deadline30.408s; NOT timer-path acceptance. Diagnostic180s protection is replaced by production1.25s protection at playable release. Movie mean16.965/max36.938ms/1%low33.632FPS FAIL>=50. Waiting384.647/break53.328/firstplayable53.593/entry43.269ms. Clearroute early12/exactblocked20. Same seed does not fix freely evolving layouts; no causal A/B FPS claim.

v138 targeted visual fix: previously swimming admission reused later raised bite's rootY, hovering above water through initial anticipation. Candidate whole-swim roots now use existing SurfaceRootY (15% bounds exposure), ALL original anatomical endpoint/path clearance and movement budgets retained; dynamic waiting root follows water rather than tilting board lifting shark. Contact pose, bite startup/window and breach unchanged. Source compilationPASS, actual four-view/submergence/contact/motion/ending validation running. Backups/v138.

SAVE AUTHORITY .codex/opening-optimization-20261012/backups/save-user-after-play-052419.json SHA256 AB575DF9484F0327E8FC7AD7B7BD9B123ADB85D13F4E9A627F3F40A38D1EC0F3. Live C:/Users/pinyu/AppData/LocalLow/DefaultCompany/Dark Brine_Rot/dark-brine-rot-save.json verifiedAB after v137. Stop/verifyactualfalse before preserving/restoring diagnostic state, protect genuinely newer human saves. NEVER old9BD/backups/save.json.

Next: finish v138 review, improve route planning CPU and actual handoff initialization, native + independent rendered regression, genuine reachable086hop, fullnormal-speed/multiview artistic review. No surfing/driving/paddle/audio, original rigs/history/save protected, blackwhiteUI/colourimages, no scene save/revert/commit/push/extraEditor. Do not delete5-30 or claim whole finished. Historical states below superseded.
# Current continuation — Oct13 — v131 / native v132 verification IN PROGRESS

Read implementation-review-v131.md first. Original Unity Editor PID22012/port7800 only: actually PLAYING native continuous four-view v132 verification. Tools/RunReviewV132.cs entry RunReviewV132.Start ALREADY ran; do not start duplicate capture/build/Editor or edit Assets while it runs. Autotick TRUE16ms. No independent Player or Blender worker active. Check native-paired-v132/review.json, framing-v132.json, visibility-v132.json and ending-v132.json; wait for complete ending before stopping and preserving diagnostic save.

New current paired Slash v128 ACTIVE FIRST PASS INTEGRATION TRIAL, 65-bone current v051 source retained, hip/chest power and quaternion continuity, original impact band preserved. Actual original skin510frames/3views, impact hand<=0.031mm/shaft<=0.011degree; whole-film verification pending. PreviousEncounterDefinition.asset in Resources/Encounter/v128 already preserved; NEVER rerun installer over backup. Inactive v122 draft retained. Existing bite078/parry090/hop086/tail097, corrected throw064, original ordinary left attacks062/walk/run/roll, mobile charge and ending unchanged.

v124 fast sole calculation validated24native poses <=0.273mm/33.207->7.879ms. v125 release mesh-read error rejected; v126 binary original foot metadata fixes non-readable Player mesh, no original mesh/readability change. v127 actual independent1080p/DX12/4060 2662GPUreadbacks/0errors, all6contactsPASS [.013300,0,.009112,0,0,0], sole1.393mm/slide1.327mm. Same-board/no neighbourhop. Film1%low43.131FPS FAIL>=50; handoff95.504ms/entry57.765ms/cold378.183ms remain. Never claim whole performance accepted.

v131 moving capsule bounds: preserves native BakeMesh, recalculates actual bounds then8corners ONLY axis-aligned frame;20nativeposes max1.176micrometre/56.731->1.968ms. Old stale baked.bounds approach failed~1m and was rejected. Rotated/sheared skins use original vertex path. Source compiledPASS, actual independent performance pending. Backups/v131. Profiler Sahur.ConfigureCollider added. After v132 finish/stop inspect actual current v131 Player handoff and new128 contacts.

SAVE AUTHORITY remains .codex/opening-optimization-20261012/backups/save-user-after-play-052419.json SHA256 AB575DF9484F0327E8FC7AD7B7BD9B123ADB85D13F4E9A627F3F40A38D1EC0F3. Live C:/Users/pinyu/AppData/LocalLow/DefaultCompany/Dark Brine_Rot/dark-brine-rot-save.json. Verified AB unchanged after v127; current native Play may create diagnostic state. Stop and verify actual false before backup/restoration. Never old9BD/backups/save.json; preserve genuine newer user saves.

Whole objective INCOMPLETE. Need pending128film/support/ending,131actual performance, dense-layout contacts and real reachable086hop, full normal-speed/multiview art. No surf/drive/paddle/audio, original rig/Avatar/resources/save protection, blackwhiteUI/colourimages, no scene save/revert/commit/push/extraEditor. No duplicate schedule; do not delete5-30 while unfinished. Historical live states below superseded.
# Current continuation — Oct13 Vancouver — v106 IN PROGRESS, not complete

Original Unity Editor PID22012/port7800 only. Play requested for native v106 validation; no second Editor/Player/Blender/capture active. Confirm actual isPlaying before starting Tools/RunPoseProfileV106.cs entry RunSolidProfileV106.Start. It has not started at this checkpoint. Autotick TRUE16ms. Do not start duplicate recordings/builds.

Current source: actual new original-16-bone shark tail/ship-smash clips v097 ACTIVE (native contact frame/marker exactly preserved), bite078, original rig/Avatar, Sahur parry090/hop086, original light attacks062/walk/run/roll, corrected throw064. Tail installer already ran; never overwrite Resources/Encounter/v097/PreviousEncounterShark.controller.

Runtime: v094 conservative capsule segment/AABB prune (2061 native pairs/857 rejection/95 true hits/zero false rejects); v095 progressive four-route exploration; v096 displaced-water sightline camera; v098 orbital camera/direct follow plus actual-immersion exhaustion gravity; v100 powerful tail detours and bounded endpoint classification (320 native comparisons/zero mismatch); v101 articulated collision-limited pose advance, eight MTD passes, early clear retained pose; v102 removes combat setup teleport and preserves actual dive root; v103 body-centre flank/open-water same-board breach; v104 exact geometry-cache invalidation (real 0.1deg original-mesh test: stale error20.370mm ->0.061mm, PASS); v105 refreshes actual entry origin, tests whole swim cycle at setbacks2/4/6/8/12m, at most1s additional physical admission; v106 ranks all cheap real-supported/open-water candidate edges BEFORE costly pose grading, rather than early exiting on first angle. v106 compilation PASS, native validation pending.

Latest independent rendered build is v102 (actual1080p/DX12/i5-14400F/4060,2591 completed GPU readbacks/0 errors): main film max26.256ms/mean16.798ms/1%low44.500FPS (FAIL >=50), five contacts PASS but reversal tail10.762m miss. Breakup51.999ms, fighting96.560ms, entry76.814ms => whole-opening FPS remains incomplete. Build0errors/488warnings actually audited, mostly package Sentis/ConvGeneric warnings + existing oceanSailingFoam warning + optional runtimePipeline config; no shader/package silencing.

Latest full four-view take v103:625frames, both shark contacts authentic, first two hero contacts fail [7.090758,.465545,0,0,0,0]m, height1.306mm/slide1.573mm; framing541frames0cropping/FOV53.092 PASS; ending264samples0upright/clockdrift,24pause0motion,lossstick/unarmedunconsciousisland/waves PASS. Images in native-paired-v103; encode movies/contactsheet still pending (use copy of encode_paired_v090.py with version replacement). Earlier v099 four encoded views already exist and were inspected, five contacts PASS/final miss; no normal-speed final approval.

Latest native v105 diagnostic: first FIVE contacts pass [0,0,0,0,.027179], final miss .517779m triggers honest exhaustion. Staging flagfalse/setback2/admission+.0333s in a dense layout; zero large escapes across ALL phases; native residualmax2.938mm, 1000 articulated advances/13 partial. Main travel overrun occurs on fallback clock/cue frame18.6:1.390m vs existing24*dt+.30 diagnostic max1.10m. Potential next local fix: enforce one frame's remaining travel/rotation budget across manual contact-cue ResolvePose and automatic LateUpdate; NOT implemented yet. No fake contacts/teleports/wood placement.

v106 reason: actual sliced edge search returned first clear 15deg edge regardless better open-water score elsewhere, producing inner seams near huge hull walls (17x26x19m). Now cheap real geometry is gathered in2ms slices, sorted by clearance/walk/wave score, then actual full pose/collision checks are applied. Same source feeds real-neighbour hop planning; check genuine new086 transfer (still0 in prior latest takes), never place/move wood to obtain it.

SAVE AUTHORITY: .codex/opening-optimization-20261012/backups/save-user-after-play-052419.json SHA256 AB575DF9484F0327E8FC7AD7B7BD9B123ADB85D13F4E9A627F3F40A38D1EC0F3. Live destination C:/Users/pinyu/AppData/LocalLow/DefaultCompany/Dark Brine_Rot/dark-brine-rot-save.json. Original confirmed stopped/Story1 clean before v106; v105 diagnostic backed up save-after-v105-native.json and authority restored. Always stop/confirm actualPlay=false before preserving diagnostic save and restoring authority. NEVER old9BD/backups/save.json. Protect genuine newer human progress. No scene save/revert, commit or push.

No surfing/board driving/leg paddling; wood natural physical supports, mesh collision/free gravity/buoyancy; no sound; blackwhiteUI/original-colour pictures; original charge/mobile attack/locomotion and ending contracts persist. One-time heartbeat5-30 fired; no duplicate schedules and do not mark whole work complete/delete task while required work remains. Read implementation-review-v095/v098/v100/v103/v105 plus current actual outputs for detail; old v092/historical instructions below are superseded.


# Current continuation — 2026-10-13 Vancouver — v092 checkpoint; INCOMPLETE

LATEST HUMAN: one-time continuation TODAY Oct13 05:30 America/Vancouver, continue animation independently, no questions. NEW heartbeat id5-30 CREATED ACTIVE with COUNT1 daily05:30 rule; previous id automation does not exist. Do not duplicate. No commits/pushes/new Unity Editor. Continue unfinished work from here, not from old v006/old schedule text.

ACTUAL FINAL CHECKPOINT: original EditorPID22012/port7800 STOPPED, not compiling, active Story1 clean/isDirty=false, autotickTRUE16ms restored. Current persistent quality pipeline Assets/Settings/PC_RPAsset.asset healthy. No capture/Blender worker/externalPlayer expected active. Independent v092PlayerPID46076 finished and quit. Authoritative user save HASH verifiedAB575DF9484F0327E8FC7AD7B7BD9B123ADB85D13F4E9A627F3F40A38D1EC0F3 unchanged after standalone. Last diagnostic save backup save-after-v090-capture.json. Do not leave autotickFALSE on resume.

Active production/runtime: v062 restored previous ordinary left-click attacks, v064 corrected cinematic throw180deg import facing, original walk/run/roll and mobile charge. NO surfing/paddling/wood driving (human override). v078 Body-led bite on original16bone shark; v079 caches post-solid actual pose; v082 long landingcounter2.48s phase; v083 early real-neighbour search; v084 graded actually blended detours and24m/sbite arcs; v087 eased-tail root speed grade; v088 stops searches ONLYafteractualcontactwindow; v089 boolean escape probes test last blocking original anatomy first (same3mm/allvolumes/offsetorder); v091 sweptroute shortlist4 and first verifiedclear reachable route earlyexit. Source compilePASS.

New actual28bone Sahur hopv086 and plantedparryv090 ACTIVE FIRST PASS INTEGRATION TRIALS, originalrig/Avatar/rest/bind/mesh unchanged. Source/Nativereviews in ArtSource/Encounter/v086 andv090. Hop1.85s/60FPS/takeoff.5/landing1.33, pelvic load/asymmetric tuck/loaded landing; valid isolated336imagesnative-framed-v086b. Parry1.1s/60FPS/contact.484, fixedsoles/right-arm deflect/torso recoil/freeleftbalance; valid isolated201imagesnative-framed-parry-v090, shaftmax1.08745deg/sourceframe. Sourcefoot drift<.32micrometres, nativeuncorrected4.061cm beforeliveIK. Installers ALREADYrun, do NOT rerun over PreviousEncounterDefinition.asset backups in Resources/Encounter/v086 andv090. Ordinary attacks/locomotion controllers untouched.

Latest full native pairedv090 seed14:625frames/4encodedcontinuousmovies, ALL6actualcontactPASS [.029621,0,0,.031210,.005495,0]m, supportingheight1.395mm/fixedslide1.393mm (oldparryv040 slipped9.692cm). Strict actualheroRenderer bounds framingPASS541frames/0cropping,minY.0536/maxY.9464/maxFOV57.40. Ending264framesPASS0upright/clockdrift,25pauseframes0motion,loststick/unarmedunconsciousisland,headfreeboard.04m,waves2.408m/rock12.22deg.0hops/sameboardvariant; newhop NOTexercised in latesttake. v083 had1genuinehop with old047 source, do not labelthat asnew086live acceptance. Actual contact-sheet and game249/267throwframes inspected. Screen-boundsPASS doesNOTprovevisibility: foregroundwaves/wood hideparts ofactor/weapon. Full normal-speed continuous art notdirectlyobserved, no final approval.

v089 actualnative geometry equivalencePASS150probes over5liveposes,122clear/28blocked/0mismatches. Not FPS evidence.

Build-cache failure diagnosed fromactual v090BuildReport (notassumedCLI): URPPreprocessBuild.LogIncludedAssets MissingReferenceException on destroyedtemporarypipeline. CurrentpersistentPCpipelinehealthy. Official localCorepackage source lazyCoreBuildData singleton retainedstale gatheredrefs. Editor-only v092 RefreshRenderPipelineBuildCache preprocessorderint.MinValue+1 disposesgathereddata beforeCore+50/URP+100 recollectassets. No packagecache/Userpipelineassetmodified. Native stale-cache regression deliberatelyaddedthenDestroyedONLYtemporaryclone in gatheredlist, originalQualitypipelineunchanged; buildv092Succeeded23.888s/0errors/1warning. Source compilationPASS.

Independent REAL1920x1080/DX12 v092seed14 (2237verifiedGPUreadbacks,0errors) completes20.802sDefeat, ALL6contactsPASS [.038334,0,.030808,0,0,0]m,entry2.939s,0hops. But performance FAILED:mean18.210ms/1%low9.289FPS/max222.788ms,maxattack43.326ms,maxsolid195.514ms. Largeststalls t4.220/4.392 and10.920/11.029 are solid collision solver, NOTattack planner. Read standalone-profile-v092-seed14.json. Differentfreelyevolvinglayouts cannot becontrolledcausalcomparisons withpreviouspasses. Noall-layoutguarantee.

Next meaningful work: profile SolidContact internals (iterative overlaps / NearestClear / TryRetainedPlacement / EscapeWedge / finalpenetration) attheseactualspikes beforelooseninganything. Consider collision-limited pose blending at a boundedroot as a targetedalternative to exhaustive whole-animal escapes, but this is ONLYan unimplementedproposal: requireactualgeometry≤3mm/motionbudget/contact tests, neverfakehits orteleport. Currentroot/posecode unmodifiedbeyondv089probeorder. Improve boundedprogressive attack-route exploration if4candidatecap misses viablelater routes. Continue genuine086neighbourhop/newparry/gamecamera visibility and fullnormal-speedart; independently retest FPS withoutcapture clock oromittedrendering. Performance and robust layout/art are INCOMPLETE; do not claim finished/deletecontinuation.

Save authority: .codex/opening-optimization-20261012/backups/save-user-after-play-052419.json HASH AB575DF9484F0327E8FC7AD7B7BD9B123ADB85D13F4E9A627F3F40A38D1EC0F3. NEVERold9BD backups/save.json. Afterdiagnostics stop/verify actualPlay=false, preserveversionedsave andrestoreauthoritative unlessgenuinelyneweruserprogress exists. No sceneSave/revert, commits orpushes. Detailed implementation-review-v090.md/v092.md and rawreports take precedence over historicaltext below.

## Historical checkpoints — superseded where they disagree

# Current continuation — 2026-10-13 Vancouver — v090 integration; INCOMPLETE

Latest human: continue production now and resume TODAY Oct13 05:30 America/Vancouver. One-time heartbeat id5-30 CREATED ACTIVE COUNT1; old id automation no longer exists. Do not duplicate. User approved independent work/no questions. No extra Unity Editor, no commit/push.

Latest ACTUAL live state: original EditorPID22012/7800 PLAYING/autotickTRUE. Full native-paired-v090 seed20261014 recording was started with ReviewPairedV053 (OFFLINE30FPS, not performance). Read-only ObserveApproachV082, ObserveFramingV075, ObserveEndingV074 and VerifyEscapeOrderV089 also attached. Check outputs/progress before restarting. Do NOT edit Assets while these captures run. No external Player/Blender worker expected active. Save may be diagnostic until stopped/restored; authoritative newer AB575 checkpoint remains protected.

v090 source and actual original Avatar import/native isolated201image/three60FPSmovie review completed. New original28bone parry fixes old retargeted recovery slipping9.692cm: planted ankles throughout1.1s, unchanged.484s contact, Hips loads, single right-arm deflects, torso reacts and free left arm balances. Sourcefoot drift<.32micrometres/rootplanar3.38e-9m; native uncorrectedankle drift4.061cm before live IK; shaftmax1.08745deg/sourceframe. Native-framed-parry-v090 key poses inspected, accepted FIRST PASS integration trial only. InstallParryTrialV090 ALREADY installed: Resources/Encounter/v090/PreviousEncounterDefinition.asset backup, one Parry beat changed. Do NOT rerun installer. Source originalrig/Avatar/rest/bind/mesh unchanged, old Parry_v040 retained. Default beat mappedv090; source compiledPASS. Active hopv086 actual28bone 1.85s/60FPS loaded/asymmetric hop remains.

v089 is only geometry-boolean optimization: ClearAt tests last blocking original anatomy first; all volumes must still clear same3mm threshold, and escape offset/distance order unchanged. No solve order/hitboxes/collision margin changed. Source compilationPASS. Native real-wreck equivalence150probe observer now attached; results pending. No FPS claim from diagnostic.

Native-paired-v088 seed14 completed625frames: ALL6contactPASS [0,.001202,0,0,.0000207,0]m; footheight1.394mm, but oldParryrecovery fixedslide9.692cm FAILED. Screen framingPASS541frames/0cropped, endingPASS276frames/0upright,25pauseframes0motion/loststick/unarmedunconsciousshore. Encoded4movies/contact-sheet inspected as keyposes, not final continuous normal-speed approval.0hops/sameboard. Independentv088 seed13 remainsfirst2contacts/FPSFAILED; do not conflate layouts or claim all complete.

Prior active runtime: v062 original ordinary light attacks, v064 cinematic throw180deg fix; original walk/run/roll, NO surfing/paddling/wooddriving; v078Bodyledbite, v079post-solidposejoins, v082longlanding2.48sphasefix, v083earlyrealneighbourplanning, v084blendeddetour/bite24m/sfeasibility, v087tailfeasibility, v088 stopsrouteplanningONLYafterauthoredactualcontactwindow to avoid recovery200mssearches. Original rigs/Avatar/history, mobilecharge, noaudio/BWUI/colourimages protected.

Last STOPPED restoration afterv088: backup save-after-v088-capture.json; authoritative .codex/opening-optimization-20261012/backups/save-user-after-play-052419.json SHA256 AB575DF9484F0327E8FC7AD7B7BD9B123ADB85D13F4E9A627F3F40A38D1EC0F3 restored. Never old9BD backups/save.json. Stop/verify actual Play=false, version diagnostic backup and restore newer checkpoint; protect any genuinely newer user progress.

Next: complete v090 capture/ending/framing/equivalence, stop/restoresave, encode/review, native slide/contact corrections. Tools/BuildOpeningBenchmarkV090.cs prepared nonblocking async Start but NOT run; no standalonev090 build/player yet. Independently measure real1080p once appropriate. Robust dense-layout contacts, new hop live art, full continuous art and FPS remain INCOMPLETE. Read implementation-review-v090.md for detailed provenance. Do not claim finished or delete the one-time continuation while incomplete.

## Historical checkpoints — superseded where they disagree

# Current continuation — 2026-10-13 Vancouver — v088 active, INCOMPLETE

LATEST HUMAN: continue production; one-time resume scheduled for TODAY Oct13 05:30 America/Vancouver. Previous automation id automation no longer exists; new heartbeat id5-30 was CREATED ACTIVE with COUNT1 daily05:30 rule. Do not create a duplicate. Continue current production independently, no questions, no new Unity Editor. Most recent actual UTC07:53 Oct13 =00:53Vancouver, so05:30 is upcoming.

Current active runtime v088: previous v062 ordinary light attacks, v064 corrected throw facing, original walk/run/roll, NO surfing/paddling/wood driving; v078 Body-led bite, v079 post-solid pose join, v082 long landing-counter phase correction, v083 earlier real-neighbour planning, v084 actual blended detour/24m/s bite route grade, v087 eased-tail root speed grade, v088 stops attack route searches AFTER actual contact window (follow-through retains strike heading). No collision tolerances or hit windows loosened.

New Sahur hop v086 is ACTIVE FIRST PASS INTEGRATION TRIAL: original28bone rig/mesh/Avatar/rest/bind retained; pelvis load, extension, asymmetric tuck, chest/free-arm overlap and loaded landing. Source1.85s/60FPS/takeoff.5/landing1.33; source support drift<0.5micrometres, root planar excursion6.75e-9m. Native Avatar valid130curves, full336image/three60FPSmovie review in native-framed-v086b, actual key poses inspected. v085 root-shift source inactive history. native-continuous-v086 empty visuals invalid; native-framed-v086 interrupted284frames invalid as complete review. Native-framed-v086b valid. Tools/InstallHopTrialV086 already installed, do NOT rerun: backup Resources/Encounter/v086/PreviousEncounterDefinition.asset. Two Hop beats use v086; runtime reads beat clip with v047 fallback. No locomotion/light controller change.

Standalone v087 ALL6contacts passed (largest1.996cm), but post-active recovery at15.28–15.92s spent125–280ms searching routes, filmmax313.6ms. v088 cutoff fixes this identified unnecessary search; its standalone different freely evolving layout instead misses first2contacts (11.717/6.599m), others.015095,0,0,.003475m,20.800s Defeat,0hops. Filmmean17.033ms/1%low24.330FPS/max78.867ms, maxattack57.712ms at14.54active window, maxsolid64.981ms at9.07. These remain FAILURES; no fake impacts triggered. Do not infer all-layout acceptance from prior passing ones. v0880error/1warning build succeeded; original async delayCall did not run while unfocused, replaced by detached async500ms build (actual success24.04s).

Latest actual original EditorPID22012/7800 was STOPPED/CLEAN/autotickTRUE after v088 independent Player finished/quit. editor_play now requested for full native-paired-v088 seed14 + approach/framing/ending observers; check actual play state before starting. No capture has been started at this checkpoint. No external Player expected active. Save hash still AB575 authoritative unchanged; last diagnostic backup save-after-v084-capture.json. Native four-view recording is OFFLINE, never FPS evidence.

CRITICAL newer save: .codex/opening-optimization-20261012/backups/save-user-after-play-052419.json SHA256 AB575DF9484F0327E8FC7AD7B7BD9B123ADB85D13F4E9A627F3F40A38D1EC0F3. Never restore old9BD backups/save.json. Stop/verify actual Play=false before restoring diagnostics, preserving any newer user progress.

Read implementation-review-v088.md and actual JSON reports for provenance. Full robust dense-layout contacts, new hop on real moving wood, continuous game art/camera and performance still unaccepted. Ending-v075 held pose/waves/pause/shore native checks PASS historically; recheck newest. v076 strict framing had9horizontal AABB corners outside; no final approval. Preserve user scenes/resources/saves; no commits/pushes. Whole task INCOMPLETE.

## Historical checkpoints — superseded where they disagree

# Current continuation — 2026-10-13 Vancouver — v084 active / v086 source; INCOMPLETE

Latest user: continue finishing animation; cinematic throw must face forward. v064 throw and v062 previous light attacks remain active and natively verified. No surfing/leg paddling, original walk/run/roll, mobile charge, silent audio and coloured pictures remain. Original rig/Avatar/user scenes protected; no commits/pushes.

Active runtime: v078 original-rig Body-led shark bite; v079 caches post-solid solved bone poses (three original-rig joins PASS); v082 aligns the long landing-counter phase with its 2.48s approach; v083 starts real neighbour searches at0/6.4s; v084 grades actual gradually blended detours and penalties for bite root arcs exceeding24m/s, instead of selecting a geometric route runtime cannot follow. Geometry and real5cm contact checks remain authoritative.

Native paired v083625frames has ONE actual neighbour jump, foot1.366mm/slide1.576mm, but landing contact6.411m missed. Four encoded movies/contact-sheet/hop-sequence are drafts, not final approval. v084 native paired625frames instead same-board variant, gaps3.260,0,0,0,6.701,0m; probing/reversal failed in that distinct freely evolving layout. DO NOT claim all random layouts solved.

Standalone v084 development build succeeded0errors/1warning and completed real1920x1080 DX12 rendering:2383 async GPU readbacks all completed, no errors. ALL6contacts passed in that water-entry layout (largest4.310cm),20.8135s Defeat, entry preparation2.591s, zero actual hops. Film mean16.803ms/1%low42.017FPS/max30.741ms, no >50ms. FPS acceptance still FAILED; not controlled causal comparison to different evolving layouts. v081 async diagnostic removes synchronous readback serialization; it is a measurement change, not a game optimization.

Latest actual state: original Editor PID22012/port7800 STOPPED/CLEAN/autotick TRUE after v084 capture. No recording/Player expected active; verify fresh status. Save backed up as save-after-v084-capture.json and authoritative newer AB575 checkpoint restored after actual Play=false. Do not restore old9BD backup. Newer authoritative: .codex/opening-optimization-20261012/backups/save-user-after-play-052419.json SHA256 AB575DF9484F0327E8FC7AD7B7BD9B123ADB85D13F4E9A627F3F40A38D1EC0F3.

Blender v085 source adds real pelvis loading, extension, asymmetric tuck, torso/free-arm overlap and loaded landing on retained original28bone Sahur; original mesh/rest/hierarchy unchanged. First attempt failed overly short solve, repaired with12iterations and1cm extension, supported source sole error0.0000292cm. v085 horizontal root weight shift would be lost by in-place root baking, so keep it INACTIVE history. v086 moves the weight shift to Hips while root remains planar stationary. Tools/AnimationPipeline/blender/refine_board_hop_v086.py build was launched background; inspect session/source-audit before restarting. v086 source preview/validation/export/native import/continuous pose review remain pending. No hop asset replacement yet.

Next: finish v086 source validation and three-angle review; export/stage with original PbrSahur Avatar; native action review before integration. Continue real-layout contact robustness and performance/continuous art. v076 camera strict framing still had9horizontal AABB-corner frames outside. Ending-v075 pose/waves/pause/shore PASS. Whole work remains INCOMPLETE; do not delete continuation automation.

## Historical checkpoints — superseded where they disagree

# Current continuation — 2026-10-13 Vancouver — v081 diagnostic; INCOMPLETE

v062 original left-click attacks and v064 corrected cinematic throw facing remain active and verified. No surfing, original walk/run/roll, movable single-hand charge, silent audio and original-colour images remain. Original rig, Avatar and user scenes are protected.

v078 original-rig Body-led bite is active; v079 now records the actual post-collision rendered shark pose, so transitions do not start from a rejected pre-correction pose. Three original-rig retained-pose transition tests PASS with 0-degree/0-metre joins and exact settling. Full new art/layout regression remains pending.

v080 standalone development Player completed at real 1920x1080/DX12, i5-14400F/RTX4060. All six real contacts passed in this water-entry layout (largest 4.5894cm); foot height 1.316mm and planted slide 1.279mm. Film 20.8s reached Defeat, zero neighbour hops. Finale mean18.968ms, p99 26.120ms, maximum38.837ms, zero >50ms. This FAILS the 60FPS/1%low50 target and does not resolve other layout failures. The synchronous one-pixel GPU fence serializes CPU/GPU work; v081 adds an opt-in bounded async GPU readback diagnostic with completion/error proof and a proper worst-1%-average metric. This is a measurement change, not a game performance improvement.

v081 source compiled without errors. BuildOpeningBenchmarkV081.Build was launched through original stopped clean Editor PID22012/7800; inspect build-report-v081.json/process before restarting. A command timeout does not cancel a build. v081 Player has NOT been launched at this checkpoint. v080 Player completed and quit. Editor autotick TRUE; temporarily disable only during standalone performance tests and restore afterward. Child Unity asset-import-worker processes are not additional visible Editors; do not kill them.

Authoritative save: .codex/opening-optimization-20261012/backups/save-user-after-play-052419.json, SHA256 AB575DF9484F0327E8FC7AD7B7BD9B123ADB85D13F4E9A627F3F40A38D1EC0F3. It supersedes old backups/save.json. Last native diagnostic backup save-after-v079-diagnostic.json; standalone v080 left authoritative save unchanged. Never restore old 9BD checkpoint. Stop/verify before restoring after diagnostics, and preserve any newer user progress.

v076 four-view one-layout contacts PASS, but other layouts fail and strict framing still has 9 horizontal AABB-corner frames outside. v075 ending hold/waves/pause/shore native checks PASS. v075 baked crown approach was rejected and removed. True neighbour-hop art, robust dense layouts, continuous final art review and independent performance remain incomplete. Do not delete continuation automation or claim finished. No new Editor, scene save, commit or push.

## Historical checkpoints — superseded where they disagree

# CURRENT — 2026-10-12 Vancouver — v078 trial; INCOMPLETE

Latest actual native normal1080p regression v078-normal is now running via
Tools/BenchmarkOpeningV058 in original EditorPID22012/port7800. Do not duplicate
it. Read actual status and native-profile-v078-normal.json/entry-observer file.

v075 baked head-local crown measurement FAILED and was REMOVED. v076 instead
fits actual character renderer bounds after camera smoothing, no mesh baking.
Native four-view v076625frames/20.8s completed ALL6real contacts in that layout:
0,0,0,.040083,.009018,0m; feet1.300mm/slide1.435mm;0hops/same-board variant.
Actual throw game0268 now has visible head. Framing bounds have0vertical/foot
cropping but9horizontal AABB-corner frames outside, so strict test still FAILS.
Other random-layout failures are not resolved by this one passing capture.
native-paired-v076 has encoded movies/contact sheet. Full art approval pending.

Native ending-v075.json PASS:278frames,0upright/clock-drift,24pauseframes/0motion,
lost stick and unconscious unarmed island. Body follows existing waves, minimum
head clearance.04m. Diagnostic repaired to survive scene handoff; v074 missing
ending report is not evidence. See implementation-review-v078.md.

v077 Body-axis sign source trial REJECTED, never exported/imported/activated.
v078 Body-led bite authored Body/Head/Spine_Mid on preserved original16bone rig:
body anticipation/lift, smaller neck bend,80ms delayed rear counterbend. Rest,
bind, mesh/Avatar, jaw/other local rotations and1.3s/60FPS/.52s contact preserved.
Actual side/3-quarter impact/recovery source previews accepted only for staging.
Native original-rig7sample import PASS, unchanged rotations0degrees, positions
<=2.211micrometres, original-small-prefab nose rise.012002m before runtime scale.
ACTIVE Bite_Lunge now CH1_SharkBite_v078. v078/PreviousEncounterShark.controller
preserves v069; NEVER rerun installer over backup. Source ArtSource/Encounter/v078.

Latest save restoration after v076 capture confirmed actual Play=false, newer
AB575DF9484F0327E8FC7AD7B7BD9B123ADB85D13F4E9A627F3F40A38D1EC0F3 restored.
Diagnostic saves preserved through save-after-v076-capture.json. No new Editor,
Player/build, scene save, commit or push. Original locomotion/light attacks, no
surfing, silent audio/coloured images remain. Dense-layout contacts, real hop,
strict framing/continuous final art and independent FPS remain INCOMPLETE.

## Historical checkpoints below — superseded where they disagree
# PREVIOUS — v075 trial

Latest user: finish animation and fix backwards-facing cinematic throw. v064
root-import correction and v062 original light attacks remain verified/active;
original walk/run/roll, no surfing, single-hand mobile charge and save protection
still apply. Active v069 original-rig head-reach and v067 rendered joins remain
integration trials. No extra Editor, Player, build, commit, push or scene save.

v070 native probe found actual-root solid displacement, not incorrect tail-marker
math or demonstrated stale collision geometry. v071–v074 rebase remaining tail
and bite arcs from the actual solved position, bounded3times/stroke120ms apart.
v073 keeps reduced route search for rebased tails. v074 also handles bites.
Normal original-Editor v074 completed5/6 real contacts (probing parry4.74973m miss),
foot1.595mm/slide1.472mm, but film max145.27ms and attack72.39ms still FAIL FPS.
Full native-paired-v074 recorded625frames/4views and encoded movies: only throw
and final tail verified in that supported-fixture layout; other contacts FAIL.
Selected actual game stills showed head cropping and heavily obscured/missing
shark contacts. No true neighbour hops. Do not declare the film final/approved.

v075 now fits actual rendered subjects after camera smoothing. The original
Head bone is below the visible crown, so a once-baked guard mesh supplies a
head-local crown point; there is no per-frame mesh baking or rig modification.
This latest source compiled successfully. Its native four-view/crown framing/
ending observation are about to run in the original Editor (check actual status
before restarting). Tools/ObserveEndingV074 was repaired to record after scene
handoff and use simulation time for its pause, because the previous diagnostic
lost its film object before writing. The previous ending check is NOT evidence.

Read implementation-review-v074.md and actual native reports. Remaining: robust
actual contacts in dense moving layouts, real hop/variant art, body exposure,
pause/shore and crown verification, continuous final review and independent FPS.
Do not remove continuation automation or mark whole work complete.

CRITICAL save checkpoint remains backups/save-user-after-play-052419.json,
SHA256 AB575DF9484F0327E8FC7AD7B7BD9B123ADB85D13F4E9A627F3F40A38D1EC0F3.
This supersedes old9BDCCB. Diagnostic saves through v074 capture/v075 idle are
separately backed up; the AB575 checkpoint was restored while actual Play=false.
Stop/verify before future restoration. Preserve any genuinely newer observed
progress and dirty scenes. Original Editor PID22012/port7800 ONLY.

## Historical checkpoints below — superseded where they disagree
# PREVIOUS — 2026-10-12 Vancouver — v069 active, v070 diagnostic; INCOMPLETE

Latest user: finish animation and correct backwards-facing cinematic stick throw.
v064 throw FBX version copy corrects root orientation180degrees; native head
facing and actual game release frame verified. Original left-click Combo1/2/3
restoration v062 remains verified; original walk/run/roll and no-surfing v055 remain.

v066 deeper swim-route candidates and cached timeline cue schedule are active.
v067 rendered-only shark pose joins180ms are active, exact native join/settled
verification PASS; temporary route sampling cannot start visual blends. Full
native-paired-v067 capture625frames/4views is completed/encoded. It FAILED
reversal tail contact1.22243m; final hit.0343653m reached and defeat completed.
Stills inspected, complete normal-speed art review is still unaccepted.

v068 additional12degree head lift was REJECTED before import (sharp neck fold).
v069 retained-rig6degree head lift is exported/imported/activated as a trial;
Bite_Lunge uses CH1_SharkBite_v069. Native original-rig comparison7samples PASS.
Original16bones/bind/jaw/timing preserved. Backup controller:
Assets/Resources/Encounter/v069/PreviousEncounterShark.controller. Never rerun
installers over existing version backups. v069 native normal1080p benchmark
FAILED two shark contacts, all4hero contacts PASS; fallback exhaustion ending.
Finale mean20.56ms/p9945.08ms/max52.42ms in Editor: NOT standalone FPS acceptance.
See implementation-review-v064.md and implementation-review-v067.md.

Original Editor PID22012/port7800 ONLY. No new Editor, Player or build launched.
The previous v069 benchmark was stopped. A new original-Editor v070 tail-placement
probe is now running (Tools/ProbeTailPlacementV070.cs). It compares planned marker,
actual raw bone placement and cached-vs-fresh wood geometry before solid solving.
Read .codex/opening-optimization-20261012/tail-placement-v070.json and actual
OpeningBenchmark state; do not duplicate the run. This probe is NOT FPS evidence.
Native same-time Animator reset audit rules out persistent manually retained pose
as the cause (Head/SpineRear/TailTip all0reset error). Do not patch that hypothesis.

CRITICAL newer user save: a separate Play session entered the island after our
v067 diagnostic was stopped/restored, then stopped. Preserve this newer progress:
.codex/opening-optimization-20261012/backups/save-user-after-play-052419.json
SHA256 AB575DF9484F0327E8FC7AD7B7BD9B123ADB85D13F4E9A627F3F40A38D1EC0F3.
This supersedes the older9BDCCB save. NEVER restore backups/save.json over newer
user progress. For our current diagnostic, stop and confirm isPlaying=false before
restoring the newer backup. Last observed stopped Edit scene Story1 was clean;
do not save/revert a scene or assume older dirty-state observations remain current.

Remaining: robust native tail/approach contacts in random dense layouts, submerged
body/art quality, real-neighbour hop review, pause/lowFPS/shore regression, final
continuous previews and independent performance acceptance. No commits/pushes.
Do not mark whole task complete or delete continuation automation yet.

## Historical checkpoints below — superseded where they disagree
# CURRENT — 2026-10-12 Vancouver — v062; opening optimization INCOMPLETE

## Latest human instruction and continuation

User requested previous ordinary left-mouse attacks. v062 restores original
whole-body Combo1/2/3 at rest, with the same FBX clips and1.25/1.25/1.55speeds.
Moving light attacks use a separate torso/arms layer rather than charge's right-arm
mask. Starting movement carries over the existing strike phase, without cancelling
or restarting it. Native original-pose comparison and actual PlayerInput left-mouse
three-hit combo while sprinting/changing direction both PASS. Charge core and input
regressions also PASS. See implementation-review-v062.md and its named reports.
Do not rerun InstallPreviousLightV062 over the existing versioned backup.

v058–v061 runtime work remains active: exact bounded native scoring, budgeted entry
route preparation, async encounter resource preloading, deterministic diagnostic
fracture seeds, and proven-radius retained escape decisions. Scoped verification
passes; this does NOT establish final performance. See implementation-review-v061.md.
Old benchmark seed labels before v059-fixed-normal did not control actual fracture.

IMPORTANT: native-paired-v061/review.json is an INVALID one-frame recorder race
against an old completed encounter. The independently completed v061 benchmark
FAILED six-contact acceptance (3hero/1shark). The recorder was fixed to use only
its own benchmark.Battle and reject a late beginning. Fresh native-paired-v062
four-view recording is currently active in the original Editor; verify live state
and completion/error files before any next operation. Do not run duplicate recorders.
Only this existing Editor may be used. Scene remains dirty; do not save or revert it.
Stop Play and confirm actual isPlaying=false BEFORE restoring the authoritative save.
No-surfing, original walk/run/roll, silent audio, original rigs/Avatar, no commits/pushes.

## Previous v057 checkpoint (historical; current details above take precedence)

## Latest editor preference and actual state (supersedes historical state below)

User objected to opening another Unity Editor. Diagnostic snapshot Editor PID12620
has now been CLOSED and its absence verified. Original PID22012 remains open,
STOPPED, not compiling; Story1 is DIRTY. Do not save or revert that scene.
**Use only the already open original Editor. Do not launch additional Unity Editors,
including snapshot Editors, for subsequent automated continuation.**
Original save SHA256 was verified again after stopping:
9BDCCBCC580D5ED35458A4FD52ED9C72A71520609ACF944340FCB619D2F4DB58.

## Current implementation and evidence

No-surfing v055 behavior remains authoritative. In v057, also removed the film's
legacy Surf pose: ApprovedBeats uses Guard, and cloned legacy beat arrays normalize
Surf to Guard without mutating source assets. Do not restore board driving/paddling.

Original active v006 EncounterDefinition/EncounterShark now use Guard050, Parry040,
Brace047, Slash051, Miss040, BoardHop047, Tail048, ShipSmash048, Bite057, Breach048.
Native versioned backups are in Resources/Encounter/v057. Installer
Tools/AnimationPipeline/InstallOpeningTrialV057.cs already succeeded; do not rerun it
over existing backups. Original walk/run/roll and rigs/Avatars remain preserved.
Bite057 substantively damps rear-spine and tail-chain rotation while preserving
head/jaw/body contact poses; original sources are retained. Native isolated import
and three-view render succeeded. These are integration trials, not final art approval.

Native original full paired review:
`.codex/opening-optimization-20261012/native-paired-v057/review.json` completed20.8s,
4hero/2shark actual contacts, gaps0/0/.0124657452/0/.0399741828/0metres;
support error.00116493425m, planted slide.0013669855m. This layout used the actual
no-neighbour same-board variant (0hops,1breach). Four continuous30FPS previews
game/front/side/three-quarter-review.mp4 are in that folder; keyframes/contact sheet
inspected, whole movies still need normal-speed review. Some views are occluded;
combined shark head/root pitch, real-hop layouts and broader regression remain.

v056 retained exact union-of-five-solid-bounds rejection in Story1SharkWreckContact.
`solid-broadphase-v056.json`:48actual poses/221shapes/35contacts, zero legacy
mismatches, median scan51.5548ms to19.1539ms. This is scoped scan improvement,
not whole-game FPS. Capsule-cache experiment was reverted after negligible native
benefit. Unfiltered native planning comparison still has an unresolved bounds versus
penetration anomaly (`planning-native-bounds-v056.json` FAILED); do not claim it passes.

Performance/admission remain INCOMPLETE: v057 offline entry preparation19.3611s;
v056 normal-speed different-layout entry81.733s and no supported surface/film.
Offline recording is not performance proof. Standalone snapshot executable is still
old Build4, PRE-v055; do not run it as current or claim a new build succeeded.
Snapshot files were selectively synced but no new build was confirmed before closing.
Need short safe handoff, dense-layout contact/admission, actual safe hops, full
normal-speed review, low-FPS/pause/water/health/shore regression and performance work.
No commits/pushes or further questions; preserve saves, user changes and original rigs.

## Previous v055 checkpoint (historical; current state above takes precedence)

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

## HISTORICAL NOTES

### Superseded v053 working record (all state/status historical)

# CURRENT — 2026-10-12 continuation, Vancouver — v053 INCOMPLETE

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
diagnostics. Snapshot editor7 PID33784, batch-force-d3d11, companyDarkBrineDiagnostics /
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

Latest native v051 completed, one real natural-water-entry layout: full20.8156s,
4hero/2shark contacts, six gaps0, foot1.590mm/planted slide1.787mm, no large film
escape. `native-profile-v051-sixcontacts.json`; latest sparse images preserved
under `v051-sixcontacts-images`. BoardJump0 (sameboard variant), breach1. This is
one scene/contact pass, NOT final artistic/all-layout/performance acceptance.

v052 solid grid acceleration PASSED native brute equivalence:48queries/20blocked,
maxDifference0/noClearAt mismatch/all anatomy enabled, solid-grid-validation.json.
Dense native full v052 FAILED:2hero/0shark, first/last contacts obstructed; entry
20.14s, film max solid143ms/max facing127ms. Different physical waves/scatter,
not proof of a regression against a literal identical layout. Preserve failure.

Standalone build3 v052 rendered1920x1080 D3D11/noPNG, full20.812s,3hero/2shark,
opening parry gap4.588m FAILED; other five contacts0, foot1.279mm/slide1.228mm.
Finale mean20.76ms/p9951.05/max74.04/13frames>50, maxSolid4.82/facing51.54ms;
60FPS target FAILED. Build resultSucceeded but errors1 includes synchronous
Pipeline build call timeout logged while native build continued; no compiler
error in player. Normal hidden Player stalled initialization; owned process
stopped and relaunched -batchmode with real RTX4060 D3D11 graphics device, NOT
-nographics. This report is standalone-v052-grid.json. Snapshot editor compilation
overlapped part of diagnostic run; repeat final isolated performance acceptance.

v053 source corrected inconsistent stance admission: coarse/fine search clear
limit.0004 now also governs water selection, hops and admission (old.0015 admitted
unclear poses, v052 wood.000518). Native collision limits/rigs/HP unchanged.
Attack-route scoring prunes only routes mathematically dominated by completed
nonnegative exact scores; never skips the selected winner's geometry.
Camera occlusion subjects now include feet (5 rather than4).

CURRENT snapshot PLAY: ReviewPairedV053.Start running offline native continuous
game/front/side/3q capture under `.codex/opening-optimization-20261012/native-paired-v053`.
Source60FPS, recording30FPS at960x540, actual native physics/skin/camera. Uses
Time.captureFramerate30; restores it on completion/destruction. NOT performance
evidence. Do not launch another scenario or mutate assets until review.json/error
is present. Benchmark wall timeout expands to900sec only for offline capture;
normal performance deadline remains90. Check current actual state before resuming.
No new assets installed in original definition yet; v050/v051 remain staged.

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

Next: inspect v053 continuous native capture/contact/entry/camera results; fix remaining physical
approaches without enlarging hitboxes or fake impacts. Run distinct water/dense/
no-neighbour/lowFPS/pause cases, real controls/traversal and held defeated float /
lost-stick shore. Produce full normal-speed game/front/side/3q continuous previews;
review weight/contact/camera and revise. Build independent latest standalone,
actual1080p no-PNG performance; activate ONLY specific accepted assets in original
with native backups, compile/Play verify, stop and restore9BDC. Keep daily automation
active while incomplete. Original save/state and unrelated pre-existing edits protected.

---

 — superseded where they conflict with CURRENT above

# 02:50 continuation — 2026-10-08, America/Vancouver

**Read the final `LATEST — 2026-10-08 19:17 Vancouver` section before continuing. It contains the current stopped Editor state, restored save, v018/v019 assets and native results. The user's latest requirement is to RETAIN original walking, running and rolling; the old ordinary locomotion replacement plan is superseded. Overall encounter remains incomplete.**

The user approved implementation of the complete opening encounter, responsive controls and common animation plan, and asked to keep working now and resume at 02:50 if capacity runs out. No more questions. Continue production and actual validation until the approved plan is fulfilled. Do not treat the existing functional smoke checks as final artistic acceptance.

## Protection and environment

Workspace: D:/indiegameDev/unity/Dark Brine_Rot. Unity 6000.3.23f1, Pipeline at 127.0.0.1:7800. Blender MCP disconnected; use C:/Users/pinyu/.codex/tools/blender-workflow/portable/blender-5.2.2-windows-x64/blender.exe (5.2.2). Python: C:/Users/pinyu/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe.

Keep all substantial pre-existing dirty changes and original rigs/Avatar/source assets. No Git commit or push. The live save MUST be restored after stopping Unity: backup `.codex/encounter-v006/backups/save.json` -> `C:/Users/pinyu/AppData/LocalLow/DefaultCompany/Dark Brine_Rot/dark-brine-rot-save.json`. Exact backup SHA256: `51C5630337B45FF9F73A52216E0539D70F72D6D985DB05D2BA07FAD7E173B214`. Do not use older backups from previous requests. Play mode scene tests can flush the save at shutdown even when their finally blocks restore it.

## Implemented assets and source

Read approved `design-v006.md`. New versioned sources: `ArtSource/Encounter/v006/` four retained source rigs, 26 authored Actions/FBX. `ArtSource/Encounter/v007/` forward guard/parry/brace and three swimming Actions/FBX. `Tools/AnimationPipeline/blender/build_encounter_v006.py` and `polish_encounter_v007.py` build them. Unity isolated imports all passed original skeleton and Avatar checks, reports in `.codex/encounter-v006/import-report.json` and `.codex/encounter-v007/import-report.json`. Installed into `Assets/Resources/Encounter/v006/` and `/v007/`. Original prefab/controller/animation set retained in Previous assets. Avoid rerunning InstallV006 (intentionally refuses overwrites); InstallV007 already ran.

`SahurGameplay.controller` has revised walk/run, jump and landing, board balance/drive/brake/steer BlendTree, right-arm light combo overlay, and continuous normal/fast swimming BlendTrees. Forward guard is v007 after v006's source frame24 was found to point the weapon backwards. SwimFast was authored by offsetting the right-arm cycle and adding torso roll/counter-neck motion. Source preview in `.codex/encounter-v007/previews/` inspected. Final actual Sahur gameplay review remains required. Old isolated clip PNGs in `.codex/encounter-v006/isolated/` are NOT valid skinned animation visual evidence (instant Camera.Render kept skin bind pose); numeric bone samples are valid. Fix that helper by baking the actual skin each sampled frame or use the real continuous film capture helper.

## Implemented runtime systems

- `SahurMotionContext` unified state/one scripted owner and velocity/support handoff snapshot.
- `ThirdPersonPlayerController` 100ms coyote, 150ms buffer; original speeds/jump height; moving attack/charge retains locomotion; original scenes using SahurGrounded adopt new controller. Land time .42s. New continuous swim params SwimAmount and SwimFastBlend; legacy swim state names remain compatible.
- `SahurAttack` right-arm light layers, real weapon sweep only, safe cinematic recovery node; preserved pre-existing twin blade changes. `Tools/VerifyMobileCombatV006.cs` passed actual sprint/turn/reverse charge, release, moving heavy and moving light attack at sprint speed 16.8m/s.
- `Story1WreckRider` automatic mount, W/S10/4.5 and A/D90deg/s physical drive; board velocity inherited on jump; .35s original-board regrab block; auto-climb edge<=1.2m, intent .2s, clear capsule; 1.15s tracked moving board-local climb. Bounded hand IK in SahurSupportPose and MovementPose now added but NOT yet verified. No F toggle remains.
- `SahurMovementPose` small inertial torso responses, gait rate, mounted foot IK; does not own displacement.
- `Story1WreckBattle` retains random20–35s or HP<=20%; pending safe handoff<=1s, waits airborne/climb; captures context, cinematic/unconscious ownership. All no-sound/island flow retained.
- `Story1EncounterDefinition` data beats20.8s60fps. Runtime film has live real board routes with two possible hops and full sameboard fallback. No boards placed/repositioned for film.
- `Story1WreckFinale` manual shared pose graph, physically tracked support; actual mesh ray sole height then bounded two-bone correction. New safe entry edge search avoids gaps in real mesh. Leap landing resolves real surface, not bounding-box top. Only final18.05–18.3 or air phases can break boards; fixes standing support destruction before fall.
- Exact capsule geometry contacts via `Story1CapsuleContact`, accepted only <=5cm after `Story1SharkWreckContact` solid solve. No timetable-only impact VFX. Thrown weapon copies its actual collider geometry. Film sample execution: pose+support -> solid resolve -> contact -> feedback -> camera. Contact cues also sub-sampled at low render rates.
- Fixed critical final fall: capture supported launch anchor before Fall starts reading it (previously uninitialized zero-world anchor). Final authored horizontal pose held through blackout; float follows real waves, head clearance verified.
- Tralalero uses actual revised clip durations for sample and live actions, including ship-tail contact.

## Actual evidence / remaining failures

Latest completed `.codex/encounter-v007-contactcue/verification.json` and `preview/pose-metrics.json`: real202 fragments; two real board hops, one breach; 1.6mm largest supporting-foot error, 1.2mm landing error; shark-wood penetration<=1mm; pause holds, head water freeboard>=4cm; lost stick, lethal narrative defeat, no respawn, unarmed/unconscious island, muted audio all passed. BUT only3 hero and0 shark contacts accepted, so NOT final film acceptance. Attempt gaps [0,0,1.1345,0,.8437,.8123]. Contact samples are real surface checks, never inflate margins to hide misses.

Previous `.codex/encounter-v007-supportfix/multiview/` continuous actual baked skin captures and `review-sheet.png` demonstrate new guard and sameboard variant. `.codex/encounter-v006-secondpass/` has two-hop continuous captures. Encode using installed imageio_ffmpeg and actual `times.json` / game `frame-times.json` (see Tools/AnimationPipeline/export_review_views.py). Lead-in varies; subtract actual report LeadInSeconds in contact sheets.

LATEST NOT YET RUNTIME TESTED patch: film now measures tail and powerful-tail local offsets once by sampling the LIVE Generic Animator at .65s, replacing cached FBX SampleAnimation offsets (possible reason for .8m misses). It preserves actual bone motion. Also live action recovery durations use new1.5/1.3/1.1 clips. Recompile passed. Check current Unity status before running tests; domain reload temporarily disconnects server, fresh status and retry works.

## Continue from here

1. Run `Tools/VerifyEncounterV006.cs`, entry `VerifyEncounterV006.Verify`, args `[false,true,true,false,false,"v007-live-tail"]`, timeout60000. Timeout does NOT stop Unity async task: inspect its files/scene before launching another. Tighten final assertions to all intended real contacts and foot targets only after actual fixes.
2. If tails still miss, probe actual desired vs solved tail at .65 (live bind transform, mesh-space conversion, capsule centre, wood blocking). Fix choreography approach/actor support locations, no fake hits, collider inflation or wood teleport. Second slash may need contact-time/route variation when real hull blocks the approach; keep silhouette and right-hand arc.
3. Adapt a NEW copy of `Tools/VerifyStory1WreckTraversal.cs` to automatic mounting/climbing and continuous swim. Its old F and space-to-climb assertions are obsolete; leave original test intact. Exercise W/S physical acceleration/braking, A/D steering, mobile attack board inputs, velocity inheritance, board break release, automatic moving-edge climb, regrab lockout, dense wreck and no-neighbour variants. Verify island controls/coyote/buffer and input transitions.
4. Boat destruction still uses old 4.6s root sine flight with only Stagger: replace with authored brace/air/land phases and credible gravity timing on real board landing, retaining tail impact and all202 freely falling actual fragments. Real ship-tail contact must be checked after new timing change.
5. Verify water-entry and low-health handoff, normal20–35 random range, airborne safe takeover, pause/lowfps, continuous actual-skin game/front/side/threequarter preview. Polish camera after actions/contacts. Fixed support sliding<=5cm needs a recorded metric or anchoring pass; current foot error does not prove sliding.
6. Finish actual reviews/docs, confirm compile and meaningful regression checks. Stop Unity then restore exact protected save and hash. Do not call work complete while contact/control/art requirements remain.

Collateral missing-type repairs were necessary for compilation: NailongSize, BossWeaponTrails, BombardinoPayloadMesh and BombardinoFlameJet functional compatibility implementations; BossCombatVfx FlameMaterial shader fallback prevented scene handoff exceptions. Preserve other user's boss/twinblade edits. Pre-existing FirstIsland Environment missing prefab GUID 2bde5fdeb204bc6418608a5fe3993ad1 remains; don't delete scene instance to suppress the warning.

Automation id `automation` updated to one-shot02:50 local. Notify only completion/material blocker. Delete the automation if the full approved work has been finished before it fires.

## 01:30 update (08 October): v008 and stricter acceptance

33 authored new Actions now exist (v00626 + v0076 + v0081). `ArtSource/Encounter/v008/` and `Assets/Resources/Encounter/v008/CH1_SahurParry_v008/` contain a revised high diagonal single-hand parry, preserving source rig. StageV008/InstallV008 already ran. Locomotion BlendTree Walk3.59/Run7.44m/s thresholds calibrated from actual avatar foot velocities; MovementPose rate increases above Run speed. Previous controller backed up in v008. All last recompiles passed.

Actual input tests passed: VerifyMobileCombatV006 (charge during sprint/direction/reverse, moving heavy/light); VerifyTraversalV006 (auto mount, W/S acceleration/braking/reverse, A/D turn, mobile board attack, jump with board velocity carry, fast swim, auto moving-edge climb, auto remount, broken-board release). Report `.codex/encounter-v006/traversal-verification.json`: driven4.42m, steering35.18degrees, reverse4.11m/s, jump2.55m, inherited board speed4.49m/s. VerifyJumpGraceV006 passed real60ms late coyote and buffered landing jump. The traversal fixture physically relocates a real board solely to isolate input tests, never cinematic staging.

Ship flight changed from4.6s sine to2.4s gravity trajectory with Stagger/Air/Land. Actual tail-tip collision required before ship fracture. Film tail offsets now sampled from LIVE Generic Animator at authored .65s and both tails can make exact real contact. Data contact cues, lowfps exact cue sampling, conditional shark recoil on verified hit, bounded fixed-foot support anchor and cached slash contact sampling implemented. Largest planted-foot slide now recorded.

Stricter film assertions now require all4 Sahur and2 shark real contacts, sole/landing<=3cm, fixed slide<=5cm, actual tail-tip ship hit. A previous v008-openlane random layout had4+2 contacts but a foot height problem. Fixed cached pose/IK/support anchoring: latest v008-strict-support has foot1.14mm, planted slide1.24cm, wood penetration0.85mm, but0+0 contacts in a different random dense layout. Therefore NOT FINISHED. The manual anatomical collision solver pushes/escapes shark root away from floating wreck, including tiny splinters, which ruins choreography. Never fake hits/inflate colliders/teleport wood to pass. New diagnostic ContactObstructions records actual marker coordinates, shark correction and blocking fragments. Entry clearance now considers all active wreck debris/boards, previously only boardable boards. Latest recompile passed; run strict test and inspect obstructions, then implement physical/collision-aware contact routes and dynamic wood response as appropriate.

Continuous v008-art-review videos now encoded with actual captured times using `Tools/AnimationPipeline/encode_current_review.py`; imageio-ffmpeg0.6.0 installed in bundled Python. These are unfinished review evidence (contacts failed), never final acceptance. Fix cinematic water entry: face toward board while swimming/climbing, cache corrected swim pull-start instead of resetting to Water-2, add tracking hand grips and supported turn into outward fighting stance. Common animation continuous skin review, water/lowHP/airborne safe handoff, random normal timings, multiview film and ship polish still pending.

Unity may be stopped or in a new Play test; inspect editor_status. Preserve save exact51C563... backup listed above after stopping. Automation is successfully scheduled today02:50, active one-shot. Keep working now until actual capacity limit; do not call this complete based on partial control passes.

## v009 update, 01:45 local

Latest full v008-allwood-lane normal-entry strict film PASSED:4 hero+2 shark actual surface contacts, foot1.17mm, fixed slide4.50mm,0 residual wood penetration, lost stick/lethal defeat/no respawn/muted audio/unconscious unarmed island all passed. Evidence `.codex/encounter-v008-allwood-lane/verification.json`. All-wood candidate clearance helped but does NOT prove random robustness: next v008-water-lowhp was3+0 contacts, blocked by actual detail_Object15/16/17; report has exact ContactObstructions marker coordinates and solved root correction. Do not hide this failure.

New `Story1SharkWreckContact.PoseObstruction(marker,rotation,tail)` queries full live anatomical capsules vs all actual wreck mesh parts at hypothetical contact placement, then restores transforms synchronously (never moves actual wood). `Story1WreckFinale.ChooseAttackFacing` tests yaw/pitch and both tail approach sides, samples the LIVE .52s bite/.65s tail contact pose and restores live animation age. It latches an attack approach and interpolates toward it. Parry contact tip predicted from actual v008 clip .484s; cached Slash tip and real support IK retained. This v009 posed-route change is NOT YET TESTED. Last recompile passed, currently Play running VerifyEncounterV006 [true,true,true,true,true,"v009-posed-route-water"], async may timeout while task continues. Inspect `.codex/encounter-v009-posed-route-water/preview/pose-metrics.json`, verification.json, console/scene before any next scene test. The strict tests require all4+2 contacts; no fake damage/recoil.

Actual common-action skinned preview helper fixed: `Tools/AnimationPipeline/ReviewCommonV008.cs` uses retained Animator/Avatar manual Playables and actual BakeMesh copies. Generated468 frames of12 actions, front/side/threequarter in `.codex/encounter-v008-common-review/`. Reviewed locomotion-sheet.png and combat-sheet.png: walk/run, crouch takeoff, folded flight, landing knee load, right-hand raised diagonal slash/parry and horizontal settled Fall are visible. Old v006 Climb looked like a step rather than a ledge pull, so authored NEW retained UAL2 v009 Action: arms reach, hands press, left knee steps before right leg, then support. Source `.blend`/FBX/index in `ArtSource/Encounter/v009`, `Tools/AnimationPipeline/blender/climb_v009.py`, reviewed source sheet `.codex/encounter-v009/source-climb/review-sheet.png`. Source SECOND PASS accepted only for staging; actual Sahur retarget/moving edge weight still need review. StageV009/InstallV009 already ran and passed skeleton/Avatar validation; resource v009, previous active gameplay controller backed up v009. Film and gameplay now both use PLAYER_Climb_v009.34 newly authored Actions total.

Cinematic water-entry patch: face toward board while swimming/climbing; cache actual corrected swimming soles in board-local coordinates at pull start (no Water-2 reset jump); vertical pull precedes crossing solid edge, limited hand IK follows actual moving board-local grips, then supported .25s turn into fighting stance. SwimmingWeapon exposes CinematicTraversal/RefreshPose so disabled player still stows weapon during cinematic swim/climb and returns it for fighting. Lead-in includes .25s supported turn; main film stays20.8s. FirstIslandArrival now claims Unconscious then Cinematic movement ownership and releases it after get-up. Verify actual water entry and island handoff before calling complete.

Remaining major work: ensure posed-route contacts succeed across random dense wreck and real two-hop paths; currently root solver still treats all wood as immovable, can escape5-12m from a wedge. Consider proper impulses/kinematic anatomy physics for lightweight dynamic fragments or improve path selection, never teleport debris/inflate contacts/force fake hits. Full20–35s random timing, airborne safe handoff, common actual continuous previews, ship physical flight/tail art review, lowfps and multi-view polish pending. At5h usage92% at01:45, ordinaryUsageAllowed true; continue until actual limit.02:50 automation remains scheduled correctly. ALWAYS stop Play and restore exact51C563 save backup before yielding or capacity exhaustion. Keep user dirties/no commit/push.

## v009 runtime test and water route revision

`v009-posed-route-water` strict check failed4+2 acceptance:1 hero+2 shark hits,2 REAL board jumps,1 breach. Foot1.55mm, fixed slide0.48mm, landing1.01mm, wood residual0.93mm. Both tail contacts now real, but parry/second slash/throw obstructed. Diagnostic exact gaps[1.51,0,2.18,.749,0,0], blocking details20/23 and occupied/neighbor hull. No final acceptance. For water entry the previous code climbed the nearest edge and fought there, bypassing clear-water edge selection. Fixed: climb nearest actual edge, follow moving board-local climb/grips, walk along a ray-verified continuous deck path at3m/s to FindEntryEdge, turn into outward fight stance over.4s. Full torso facing uses quaternion interpolation (no zero-vector180 turn), grips aligned with actual climb direction. No board teleport; main paired film unchanged20.8s. Lead-in can be longer for a real across-deck route. Recompile passed.

CURRENT Play async test: VerifyEncounterV006 [true,true,true,true,true,"v009-water-clearwalk"]. Inspect output files/scene before starting another; async command timeout does not stop its task. Last protected save was restored exact51C563 after stopping before this test; restore again after this test shutdown. The one-shot02:50 continuation already active. Next inspect results, review actual v009 Sahur climb and two-hop capture, then fix remaining actual contact/collision causes and full art/controls regressions. User asked to keep working to actual capacity limit; do not stop merely because another test failed.

## 01:55 corrected climb and latest acceptance failures

v009-water-clearwalk strict run FAILED:3 hero+2 shark contacts; only parry missed(.782m); no neighbour fallback; other gaps[0,.03884,0,0,0] real. Water-entry turn/walk produced one non-supported foot sample(Infinity atfilm-.09s) and fixed slide6.4cm, so no artistic/foot acceptance yet. Latest new patch in FindEntryEdge adds .9m inward stance clearance; probing bite now raises its head in open water first(0-.7s) before advancing toward the high parry(.7-1.184s), to avoid straight-line movement through hulls. These two patches compile but runtime regression NOT YET DONE.

CRITICAL reviewed actual v009 Sahur retarget: arms reached BACKWARDS. UAL2 front is -Y, not +Y. New retained original rig copy v010 corrects authoring reach/torso lean/knee axis; source sideframe35 visibly shows forward hands, chest inclination and raised leading knee. ArtSource/Encounter/v010, Tools/AnimationPipeline/blender/climb_v010.py. StageV010/InstallV010 succeeded exact skeleton/Avatar/FPS/duration checks. Active Wreck Climb controller and film BOTH now PLAYER_Climb_v010; previous controller v010 preserved. v009 remains historical rejected direction revision; do not treat it accepted for gameplay.35 new Actions total across revisions. ReviewCommonV008 now accepts selected clip string; run args["PLAYER_Climb_v010"] for39 actual skin samples. Need inspect actual direction and moving-edge support before acceptance.

Last recompile completed without errors. Unity was STOPPED before v010 staging, protected save restored exact51C563... before last asset changes. No active async test currently; cell420 completed failed, cell416 never started (Pipeline transient disconnect). Check editor_status freshly; first call after domain reload often disconnects. Restore exact save again after any next Play session. Remaining actual collision robustness, no false hits, contacts+foot goals, common animation/ship/game multiview artistic review and normal timers/airborne handoff still required. The02:50 one-shot continuation remains active.

## v010 actual retarget verified

ReviewCommonV008("PLAYER_Climb_v010") completed39 actual Sahur BakeMesh samples. Inspected side-04: hands reach FORWARD and torso inclines over ledge, corrected axis confirmed. `.codex/encounter-v008-common-review/PLAYER_Climb_v010-*-*.png`. This verifies direction, not final moving-edge support/grips. Immediately stopped Play and restored exact protected51C563... save.5h allowance98% as01:52local. CURRENT final Play check in progress VerifyEncounterV006 [true,false,true,true,false,"v010-water-regression"], cell430. Has no screenshots, writes metrics/verification in `.codex/encounter-v010-water-regression/`. Inspect outcome before further edits; required full contact/foot goals remain strict. All latest source recompiles passed, no commit/push. After test stop Unity and restore exact51C563 save before capacity ends.02:50 automation resumes complete plan from these records.

## v010 regression latest

`v010-water-regression` achieved ALL4 hero+2 shark exact contacts in actual random debris. New raised-then-advance probe path fixed parry here. Fixed support sliding4.24cm; wood penetration0.93mm; head clearance4cm. STRICT still failed supporting-foot Infinity at film-3.916s, traced to cinematic climb claiming planted feet atpullAge1.03 while authored knee/foot are still raised. Changed support takeover to actual clip completion1.15s (during hanging/pulling hands remain support). Recompile passed; this final support timing patch awaits recheck. Main paired contact choreography20.8s, water entry lead7.76s in this layout. v010 actual retarget forward hands/chest confirmed39 samples. Indicesv007-v010 revision labels corrected to own versions (export FBX metadata was already correct). Last Play stopped and exact save restored from51C563 backup. Not finished until final support and multi-layout/multi-view regressions pass.

## 03:00 heartbeat resumed production

02:50 heartbeat fired, capacity reset(ordinary usage allowed,1% window). Read approved design and this checkpoint; no restart or questions. v010-support-regression now ran with continuous capture: foot1.43mm/landing1.26mm/fixed slide0.97mm pass even two real hops, but ALL6 contacts blocked in another dense layout. Thus contact robustness remains the main blocker, not current foot support timing.

Implemented legitimate physical momentum transfer: anatomy collision applies reduced-mass normal impulse to real dynamic wood ONCE per fixed physics tick; no position/rotation edits to fragments, gravity/buoyancy and mesh solver retained. `Story1SharkWreckContact.PushFloatingWood`1500kg animal vsactual timber mass; relative posed anatomical velocity vsrigidbody point velocity, restitutionless separation. Previously the solver treated all floating wood as immovable, repelling shark metres. New v011-physical-contact actual normal-entry test PASSED all4 hero+2 shark contacts, foot1.36mm, fixedslide3.44cm, .99mm wood residual, paused pose/waves/loststick/lethal/norespawn/muted/island all passed. Report `.codex/encounter-v011-physical-contact/verification.json`. No neighbour fallback; need real-hop and water/lowHP robustness/art review still.

Initial impulses could over-excite occupied hull: updated impulse separation speed caps1m/s occupied,2.5m/s heavy mass>=30kg,6m/s tiny timber, subtract existing separating point velocity instead of repeatedly accelerating wood. Battle.BoardCarriesPlayer exposes actual current rider/support for occupied-board cap; film protects its actual board until final support loss. Recompile passed after fixing the accessor. This bounded version NOT YET runtime-tested. Keep exact50mm contact threshold; .050112m missed tail in previous failure is correctly a miss, never inflate it.

Unity currently STOPPED after v011 test and latest compile. Save backup still exact51C563; restore again after future Play. No script async running. Continue bounded-physics water/HP and normal/random/two-hop tests, actual continuous camera/review, common controls animation record, airbornehandoff and final save restoration. Don't claim finished yet.

## 03:33 heartbeat ongoing: actual sea visibility and causal reactions

v011-bounded-water PASSED strict contacts all4+2, TWO actual jumps, one breach, foot1.094mm/landing.782mm/plantedslide4.47cm/wood.793mm, water+lowHP/pause/defeat/island. Actual game/front/side/three-quarter/ship movies encoded in `.codex/encounter-v011-bounded-water/*-review.mp4`. v011-normal-timer also PASSED actual34.359s deadline,4+2,2jumps,foot1.37mm/slide2.93cm/zero wood penetration. But actual SEA GAME REVIEW exposed an untested issue: boards and Sahur can remain submerged beneath a rising wave/piled wreck despite correct mesh support. Added minimum fighting-chest clearance telemetry to VerifyEncounterV006; measured -1.77m in normal-timer. This is NOT visually accepted yet.

Fixed a real wet-board jump handoff bug: Rider clears deck flag before ordinary swimming detection, so waves could cancel a deliberate jump. LeaveMovingPlatform now optional jumping=true arms .6s water grace at actual release. VerifySafeHandoffV011.VerifyAirborne PASSED native jump, lowHP request waits for splash/support, full rising trajectory continues, stateSwimming after.632s. Latest VerifyTraversalV006 rerun PASSED v010 forward climb, auto mount/drive/steer/mobileattack/reverse/jumpcarry/fastswim/movingboardclimb/re-mount/break release:5.72m drive,37.87deg steer,2.67m jump. VerifyRandomDurations PASSED27.332,27.411,28.547s distinct, normalrange.

Important causal fix: actual verified tail contact now gates Stagger, Fall, stick loss and lethal finish. A missed final tail returns ordinary control at current supported pose and retries after3s, rather than falsely knocking Sahur down. ReleaseActors handles graph/colliders/weapon teardown. Definition revision v011 installed with retained v007Guard/Brace andv008Parry; reversalStagger14.65 (formerly15.3), recoverGuard15.4; total20.8 unchanged, previous definition v011 backed up. v011-relative-water failed contacts3+1 and correctly aborted final tail(.249m miss) with no invented death. Subsequent v011-camera-water had4+1 (reversal tail.303m miss), final actual hit/defeat, so still not accepted strict2tail. Supports/slides pass.

Latest legitimate physics/reach changes awaiting verification: buoyancy damps velocity RELATIVE to sampled water velocity, heave filter .18s (formerly.6), surfacecenter weighted.5 plus4corners.125, maxlift8g (old2.2g ceiling saturated under piled dynamic wreck and trapped timbers deep). No fragment teleport, gravity/freewaterforces retained. Hop lands at reachable ray-verified open-water edge within12m and1.5mheight using actual neighbour mesh rather than nearest interior, no staging. Upper-chest target stays INSIDE real torso capsule to let thick tail clear supporting mesh; anchor tracks actual tail-tip contact instead of snapping back to torso center. Camera scores actual head/chest/weapon visibility against wood/anatomy and can change sides with.35s hysteresis. All latest recompiles passed, Unity currently STOPPED, no async script active. Save restored exact51C563... after all Play tests. Need test current8g buoyancy/open-edge/upperchest contacts, actual game review, ordinary controls continuous capture and no-contact recovery check. Tools/ReviewControlsV011.cs written native Late camera capture; not yet invoked. Tools VerifyEncounterV006 now creates output dir even capture=false, prior v011-clear-landing ran film but report write failed missingdir; don't claim that run passed.

Full objective remains IN PROGRESS. Do not delete original assets/save, no commit/push. Quiet heartbeat while working; only notify when completed or an actual user-action blocker exists.

## 04:00 actual controls / physical support findings

Native island-ground test (no reflective/manual Update calls) PASSED twice on actual loaded FirstIsland terrain and recovered real stick: walk11.26m, sprint/turn/reverse keeps charge, moving heavy5.49m, native mobilelight, jump2.45m and landing returns control. `.codex/encounter-v011-controls/island-verification.json` and continuous `.codex/encounter-v011-controls/island-final/review.mp4` (194frames/22s) reviewed skinned gameplay sheet. Native wreck traversal with actual runtime v010 climb PASSED after waiting for .2s SIMULATION intent rather than capture-delayed wallclock: drive6.06m, yaw38.49deg, jump2.68m/carry3.88m/s, fastswim3.85m, moving-edgeclimb/remount/break. Continuous140frames15s `.codex/encounter-v011-controls/wreck-traversal-final/review.mp4`. Older initial controls recordings accidentally persisted across segments because ephemeral assembly types differ; recorder now finds its component by type name/reflection when finishing. Final recordings correctly scoped; original longer sheets remain historical.

VerifyCausalHandoffV011 ChargeDeadline PASSED native held right mouse: keeps held charge during safe wait, takes over1.018s, BoardRiding context. UnconfirmedTail fault-injection PASSED: withheld final contact window cannot cause lethal finish; current supported pose returned to playablefight,HP20, original control enabled. This proves causal gating, not a real obstruction layout. VerifySafeHandoffV011 earlier airborne native check passed. All user-save protection retained, last stopped save restore51C563.

Remaining actual dense-layout robustness/sea presentation still NOT accepted. v011-tail-angles waterLowHP strict PASSED4+2, foot1.94mm, slide3.15cm, wood.95mm, actualtailgap2.69cm; but new chest telemetry -0.85m because long deck edge submerged. v011-load-buoyancy increased thin-timber stiffness/maxlift8g/relativewaterdrag produced chest+.45m, but parrymiss4.44m. v011-shaft-parry had2+2/one actualjump and chest -1.59m at jump preparation; cannot call these passes. Main reason for submergence found: long20m deck pieces plus film extra torque forced their broad face WORLD-HORIZONTAL regardless sloping swell. Latest patch float SurfaceNormal=.75filtered water slope capped16degrees, cinematic stabilization aligns to that SAME swell normal (15/3 instead of world-up22/7), retaining natural rigidbody/gravity. This should keep real edge support near its wave rather than hiding Sahur under a crest; awaits test.

Parry now targets middle of actual weapon capsule (real shaft, not a 7m-high tip) and future offset samples that shaft; full bite/tail yaw/pitch candidates ±45 degrees. Approach now rechecks changing wreck every.12s through windup while actual pose is blocked, stops replan shortly before contact, limits correction30degrees each decision. No contact threshold inflation / no fragment staging. Retain truthful misses and support. Current latest recompile passed, Play starting; no async test at this checkpoint. Need actual final multiview/sea/ship, no-neighbour and two-hop, normal20–35 & waterHP regressions with chest clearance>0, genuine sliding/contact and no abrupt root escape acceptance. Model/anatomy solver still EscapeWedge5–12m fallback can cause visible jumps; do NOT claim cinematic polish done merely on all6 hit counts. User requested actual continuous review; address real film look.

## 04:20 substantive contact/collider diagnosis

v011-footprint-fit strict normal quick encounter PASSED all4+2, supportingfoot1.09mm, fixedslide1.59cm, wood.405mm, fightingchest+.71m, stable horizontal fall/waves/unarmedisland. Continuous main/game/front/side/three-quarter/ship videos encoded in `.codex/encounter-v011-footprint-fit/`, actual game sheet reviewed: Sahur remains visible, clear diagonal right-hand stroke and horizontal fallen body. This layout correctly used no-neighbour same-board variant. NOT sufficient final acceptance: subsequent submerged-body layout had3+2 and support4.76cm; support-cache layout4+1 final-tail miss correctly returned control.

More actual fixes now: head anatomical collider's rounded nose starts BEHIND NoseWorldPoint by its own radius; previous sphere projected invisible solid surface beyond the visual nose, falsely blocking deck edges. FindEntryEdge requires8 stance-radius.75m mesh rays and1.4m inset. A planted leg unreachable at bounded .35m correction receives bounded8cm root weight-shift in deck plane, never increases IK reach. Supporting sole picks LOWEST finite sole rather than closest absolute error, preventing the other leg from being buried. Future Parry/Slash contact offset cache now INVALIDATES after support changes or local tilt>.5degrees. Old first-board cached slash height was wrongly reused after hopping onto a differently tilted mesh. Approach cost penalizes bulk-body exposure above actual water (body bone, not guessed bounds), outside intentional breach; more physical submerged biting/tail approaches.

New decisive issue: Unity reports partial convex hulls for many actual detail meshes (real models contain up to3,613 distinct vertices/4,889 triangles). Whole convex approximations incorrectly fill rigging/sail recesses; that can repel shark metres. Confirmed Unity6.3 manual hull limit255 triangles and BakeMesh void/GetEntityId API from official docs. Implemented Story1ConvexMeshParts: exact-position weld; recursively split original actual triangles spatially to<=120 geometric vertices (hull<=236 faces); planar patches<=60 points doubled with tiny local thickness. Cached geometry/baking at inactive wreck warmup, no visual source/rig changes, same original202 dynamic fragments. Empty child colliders avoid MeshFilter auto-assign cooking oversize original hull. Story1WreckMeshCollision now uses these bounded convex surface pieces, preserving compound cuts and all original meshes/materials. Latest recompilation PASSED. This mesh correction is NOT YET RUNTIME VERIFIED; physical counts/performance can increase, inspect active shape count/logs, real contact, support, natural release, and controls before acceptance.

Currently Play entering after compile; no native script running. Protected save51C563 restored before new code. Remaining: validate bounded actual meshes (no partial-hull warnings), multi-layout contact+support+chest and genuine two-hop; actual continuous game/multiview/ship reviews and control regression; root solver5–12m EscapeWedge still needs continuity check, do not call complete on only positive contact metrics. No commit/push. Heartbeat stays quiet, full task ongoing.

## 04:50 mesh cooking, causal clocks, physical review

Bounded real mesh parts audited in actual live wreck: 202 original dynamic fragments, 1,246 convex colliders, max120 distinct vertices per part, sequence/anatomical-root scale1, actual sharkscale.65. Fresh Editor.log segment after83185 had NO partial-convex-hull warnings; old warnings are historical. Geometry preserves visual original source/Avatar. Costly collider transform/bounds queries replaced with cached once-per-frame exact transformed AABBs, aggregate broadphase then individual piece broadphase; hypothetical shark poses still use ComputePenetration without physics sync. Actual playable-frame sample13ms; latest native held-charge check under initial dense-frame load waited1.29s, so strict<=1.2s test failed and lowfps deadline regression remains required.

Fixed genuine defeat timing: actual verified final tail IMMEDIATELY selects Fall; scheduled18.05 cue cannot overwrite captured launch position/velocity. Stick loss uses actual hit+.17 and actual release time for gravity/flight; removed independent18.22 timer triggering. No-contact recovery preserved. Recompile passes.

Actual downward slash tip often ends BELOW the real supporting deck; aiming shark nose there required illegal board crossing. Future slash now aims a real point on that SAME weapon capsule's shaft >=1.15m above actual deck normal, verified actual capsule/head collision remains<=5cm. v012-visible-shaft actual WATER/lowHP run achievedALL4+2 contacts, one actual hop, zero wood penetration, plantedslide2.08cm, chest+5.05cm, horizontal floathead4cm. BUT supportingfoot5.89cm atparry FAILED3cm goal; no verification.json/acceptance. Continuous five views encoded, game sheets initially used elapsed times not main-time+lead; do not mislabel pre-entry/swim as main beat. Latest v012-root-weight had2+2/TWOactualhops/foot4.25cm/chest-.082m, so random-layout robustness remains incomplete.

Latest adjustments NOT YET tested: 24 actual edge directions (was8); bounded root heave correction after sole IK for non-fixed actions; two bounded8cm COM shifts with re-plant before fixed-anchor solve; wider actual pitch candidates +/-80deg to let head/tail rise with bulk underwater rather than laying whole shark on deck. Keep limbIK.35m and exactcontacts; no staging, no inflated contact thresholds. All compile pass. EscapeWedge5–12m/down20m remains actual discontinuity risk and requires a continuous collision-aware approach/rollback, not metrics-only acceptance.

Current Unity entering Play after last compile; no async script/test running yet. Last stop restored exact51C563... protected save. Capacity51%5h/49%weekly at04:43 local, ordinaryUsageAllowedtrue; continue production, don't end solely because tests need more work. Need latest controls/geometry regression, causalclock and main game/front/side/threequarter correctly time-aligned art reviews, dense/sparse/two-hop/no-neighbour fullnormal/random/waterHP and lowfps. Never commit/push. Quiet heartbeat remains in-progress, user notification only on actual completion or user-action blocker.

## 05:15 continuous constraints and distributed pressure

Main-time-aligned game review for v012-normal-full: ALL4+2 real contacts/onehop/chest+1.38m/zero wood penetration; main fall/horizontal-wave hold visibly continuous. Strict still FAILED unsupported entry walk atmain-1.84s (both foot rays empty). Correctly encoded five complete movies in `.codex/encounter-v012-normal-full/`. Fresh native traversal AFTER bounded mesh parts passed auto mount/drive5.46m/steer38.71deg/mobileattack/jump2.66m/swim/climb/break. All stays202 real released bodies/1246 bounded mesh pieces; fresh partial hull warnings gone.

New continuous anatomical constraint: clamp desired root travel24m/s(+15cm collision allowance) and angular300deg/s; if seam projection jumps farther, preserve the previous actual rig/root pose when still clear rather than teleporting to a distant escape. Snapshot preserves original bone locals. Telemetry ConstraintHoldCount/LargeEscapeCount added; v013-retained-anatomy actualnormal2hops reported0/0; still3+1 hits/foot4.25cm/chest+.82m, NOT accepted. v013-grounded-sole had44holds/0large escapes/3+2/chest-1.78m. v013-distributed-water had14holds/3largeescapes/4+1 final-tail miss. Thus collision-aware planning and pathological moving-wood enclosure still unresolved; do not call root continuity solved or final animation accepted.

Foot fixes: anchored foot is chosen by smallest ABS surface error and anchor stores actual mesh hit rather than freezing a residual IK gap. Missing foot ray can use a verified nearby actual mesh edge within unchanged35cm reach. Skip unnecessary zero-delta joint solves. Iteratively solve the SAME bounded world sole target after restoring authored foot rotation (scaled retargeted parents can otherwise change the sole offset). Latest COM support shifts now run up to4 bounded8cm corrections until anchor3mm, earlier2.5cm tolerance let a foot move sideways across a jagged step while ray error rose8cm. These latest tight-anchor changes compile, NOT yet run.

Substantive water physics: four true broad-face pressure points, sampled at each actual rotated corner (same5 total water queries asbefore, no extra per-tick arrays), smoothed/extrapolated relative water velocity. AddForceAtPosition uses actual mass ForceMode.Force, so real inertia handles long lever arms. Old centre-only heave left20m deck edges underwater during passing crests; corner acceleration had historically amplified chop. Pressure now produces real pitch/roll from local crests, reduced auxiliary face torque2/1.5, gravity/freefall/collisions retained, no fragment poses rewritten, ocean style unchanged. Native traversal AFTER new distributed pressure PASSED drive5.70m/steer36.72deg/jump2.74m/carry4.90m/s/fastswim3.73m/movingedgeClimb/break. Actual v013-distributed-water film4+1/onehop/chest+.338m/zero wood penetration; foot8.27cm FAILED, final tail.269m missed and correctly returned control. Need exact support and multiple layouts/normal timer/art regression.

Facing planner scores intermediate real anatomy as well as endpoint, prunes inferior endpoint lower bounds, exits on clear cheap solution, and targets bulk centre1.4m below sampled water to keep most body submerged outside breach (old.4m depth only submerged about half its radius). Must review actual film before claiming improvement. Water fixture failed once because45m teleport landed among actual scattered wood; choose verified open water for fixtures rather than falsely blaming swimming. Save restored EXACT51C563... after last stop, no async test currently, Play entering after tight-anchor compile. User objective still in-progress; capacity60%5h at05:07; continue, no commit/push.

## 06:39 continued: decisive query proof, actual recoil and render skinning

Proven native physics/render interpolation mismatch fixed in Story1WreckMeshCollision: queries map visual interpolated ray/points into Rigidbody fixed physics pose, and map results back. VerifyInterpolatedMeshQueryV013 actual moving cube PASS: native support error3.911cm vs mapped0, physics/render separation7.045cm. Geometry cache now also invalidates root position/rotation/scale within a rendered frame, not only frameCount. Actual v013-render-physics-support normal26.653s deadline strict PASS4+2, no-neighbour variant, foot1.336mm/slide1.281mm/chest+1.46m/wood.714mm/head4cm; movies retained. At that earlier stage large escapes outside/inside main were not distinguished, and physics subsequently changed, so this is historical evidence, not final acceptance.

Four distributed real pressure points retained. Latest Story1WreckFloatBody uses cached signed mesh material volume times world scale, instead of treating hollow original hull/rail/sail bounding boxes as solid. Original meshes are normalized; dimensions are their actual transform scale, so volume scaling is valid. Open zero-volume surfaces use2% bounding-box fallback; effective timber thickness volume/broadarea; mass1–2000, pressure net capped-1.5g..+2.4g, actual gravity/collisions/inertia/no fragment repositioning. v014-material-volume actual film3+1, nohop, foot2.63mm/slide2.86mm/chest+1.58m/wood2.966mm/0large escapes; missingslash7.03/reversaltail14.572. Native traversal passed before this final material-volume change; latest volume still needs full traversal regression.

Audited real stick collider vs original mesh: UNIFORM scale300, total capsule3.600m, mesh projected axial3.596m, collider radius.135 vs maximum mesh radial.296m; this is not an oversized invisible 7m stick. Do not inflate contact padding. Found general nonuniform capsule Segment formula defect (subtracting unscaled radius before scaling); now uses axial world height minus radial world radius, WeaponTip shares Segment. VerifyCapsuleGeometryV014 actual36native Collider.ClosestPoint samples PASS maximumerror0 across3different anisotropic scales/3axes/4directions. Initial same-object direction/scale mutation fixture reused stale native physics geometry; corrected fixture creates independent case and waits a physics/render tick. No original collider/mesh/Avatar rewritten.

Substantive animation defect fixed: ChooseAttackFacing temporarily sampled Bite_Lunge for reach planning and restored that bite even AFTER a verified hero hit, erasing authored Hit_Recoil. TralaleroSwimAnimator now exposes current cinematic action/time; planning restores the actual sampled timeline action/time. Actual v014-recoil-preserved film observed74recoilframes, and v014-reachable-weapon42, rather than silently overwriting recoil. Future slash/parry now selects an actual sampled shaft point against complete anatomical endpoint wood obstruction and submerged bulk cost; parry tracks the same measured shaft fraction in the current pose. No collider enlargement, fakecontact or foot teleport.

Actual game render vs actual CPU/BakeMesh review disagreed at20.7s: CPU side view stayed horizontal, game-camera skin reused upright pose. Isolated AuditFallV014 proves original v006Fall clip correct through2.24999s (head-hips verticaldot.0023;1s.0437), clipnonloop2.25s. Per Unity SkinnedMeshRenderer.forceMatrixRecalculationPerRender documentation, cinematic renderer matrices now recalc per render and updateWhenOffscreen during multiple temporary/manual pose evaluations; exact prior settings restored on ReleaseActors. Latest actual game sheet now ALSO shows horizontal body through20.66s, consistent with skin side view. This was a rendering-cache issue, not a reason to replace the original death action. Do not treat old camera capture as proof of source pose failure.

Strict verification now records main-film large escapes independently of playable-phase totals, recoil samples and detailed escape events. v014-recoil-preserved actual1hop/3+1, foot1.42mm/slide1.22mm/chest+.906m/wood2.83mm/mainLargeEscape0, missedfirstparry and reversaltail. v014-reachable-weapon actual NORMAL20–35deadline run2+2/1hop, foot1.646cm/landing.658mm/slide2.949mm/chest+.533m/wood.298mm/head4cm/mainLargeEscape0, but firstparry3.350m and firstcounter1.725m obstructed by Hull board6. Strict test FAILED, no verification.json. Five complete correctly main-time-aligned movies encoded in .codex/encounter-v014-reachable-weapon/ (game/front/side/three-quarter/ship). Actual game/side sheets reviewed, horizontalfall and real hop visible; first exchanges still obstructed by crowded hull, NOT artistically/final accepted. The run_script call timed out60s but detached native test completed and wrote metrics; no test remains active.

Newest change COMPILES, NOT YET RUN: RouteObstruction now samples actual intermediate winding-up bone poses (.35/.7 approach phases) rather than checking the fully coiled contact pose for the whole route. Earlier straight spine/tail can hit hull although coiled endpoint looks clear. Restore contact sample after candidate route checks, then restore actual timeline action. Need actual normal and water/lowHP routes with ALL4+2, no mainLargeEscape, <=3cm feet/<=5cm slide, chest above water, native material-volume traversal and safe handoffs. Capture lowfps and real safe-hop/no-neighbour variants, no staged debris. Do not mark complete on partial counters. Unity stopped at this checkpoint, latest recompilepassed; save copied authoritative backup after stop. Last capacity94%primary/55%weekly at~06:32, ordinaryUsageAllowedtrue; continue within capacity. No commit/push, quiet heartbeat, no questions.

06:42: Native VerifyTraversalV006 rerun AFTER material-volume buoyancy PASS actual independent input: drive5.667m, steer33.54deg while mobilelight, reverse-3.773m/s, jump2.729m/carry4.419m/s, fastswim3.854m, moving-edge auto climb/remount/broken support release. .codex/encounter-v006/traversal-verification.json updated. First cold Play traversal run_script timed out at60s without a new report; warmed retry completed16s and passed. Do not assume tool timeout stopped task.

Latest actual v014-intermediate-anatomy NORMAL run wrote metrics:3+0, nohop, foot2.62mm/slide2.88mm/chest+2.599m, wood.849mm, one mainLargeEscape at14.76s (1.973m), missingfirstparry5.24m/reversal.615/finaltail2.25m. Correctly aborted/released to playablefight; automatic retry was later active (current retry reached4herohits by12s), but that retry is NOT captured full acceptance. No verification.json, no false death. The route improvement does not yet solve crowded real wood; need continuous collision-aware path selection and genuinely clear combat endpoint, not making thresholds permissive. No async test active; current production auto-retry continues because Unity Play running. Stop + restore exact save before ending. User task remains incomplete, not external blocker.

06:43 turn checkpoint: account primary now100% (rounded reading), reset2026-10-08 14:50:20UTC /07:50:20Vancouver; weekly56%. No reset credit used. Latest script changes compile clean. Unity STOPPED and authoritative user save restored SHA25651C5630337B45FF9F73A52216E0539D70F72D6D985DB05D2BA07FAD7E173B214. No async native test or detached functions cell running. Latest next-step priority is real dense-wreck path/entry selection for all6 contacts, no main root escape, then final multiview/native lowfps/waterHP acceptance; complete-source action artifacts already retained, do not redo all35. Task NOT complete; no commit/push or automation deletion. Do not claim accepted cinema from partial passes. Continuation should resume this exact checkpoint and latest code.

## 2026-10-08 13:03 continuation v015 (normal user: 继续吧)

Root works solo; no child agent spawned, no commit/push. Current approved scope unchanged. This is NOT final acceptance. Primary usage29%/weekly61% at12:47 local, ordinaryUsageAllowedtrue; continue meaningful work.

Confirmed Anatomy audit BakeMesh must use true before transforming vertices by renderer transform (.65 shark scale). False baked vertices were already scaled and falsely suggested Nose_Marker was beyond skin; corrected `.codex/encounter-v015/anatomy-audit.json` proves marker aligns head (nose12.302 vs actualmax12.302). Original markers/capsules NOT shrunk/moved.

Production v015:
- `Story1WreckFinale` selects actual stance after graph/resources initialized, grades actual Parry/Slash poses plus cached actual Tail_Strike/Ship_Smash anatomy. Latest includes full footprint radius measured from standing original mesh feet, footprint-sized walk corridor, wet/unsafe hop rejection. No staged boards.
- Reversal tail wind-up begins13.322 during landing (clip strike begins13.922, contact14.572), compressed breach ends beforehand. Attack path start cached per attack, not reset by each beat; early facing establishes attitude before contact. Route scoring uses actual intermediate poses and strong endpoint penalty, restores original recoil.
- `Story1SharkWreckContact.AnatomyPose/CaptureAnatomy/PoseObstruction(pose,...)` searches hypothetical capsule geometry without repeatedly sampling/relocating rendered rig. Native24-case cache audit observed maximumgeometryerror.173mm, costs within native world-coordinate float tolerance; original rig/collision dimensions unchanged.
- Added native submerged entry route search (depth0/3/7/12, lateral0/8/16/24) across actual anatomy/debris, lead based actual root path length and turn time; main20.8 fixed. Ground entry actual walk speed matches route timing.
- Pause retains last solved hero root and bone locals after1300; avoids first paused interpolation endpoint changing arm pose. No timeline/camera advance while paused.
- `SahurSupportPose.MeasureSoles` samples original skin in standing guard once; previous actual foot audit showed old markers~3.4cm above visible sole. No model/Avatar edits. Finale's stance width uses actual measured soles.
- Thrown stick final flight sampling moved after solved shark pose inside VerifyFinalContact; no enlarged hitboxes/contact thresholds.

Native accepted HISTORICAL v015 milestones (do not claim latest all accepted):
1. `.codex/encounter-v015-anticipation-path/verification.json` quick full4+2, nohop, actualmaxgap5.2mm, foot1.42mm, slide2.46mm, chest+1.859m, wood0, mainescapes0, recoil118, pause/loststick/lethal/unarmedunconsciousisland/mute pass. Game sheet reviewed.
2. `.codex/encounter-v015-cached-normal/verification.json` actualNORMAL deadline24.345, full4+2/0gaps, nohop, foot1.609mm/slide1.294mm/chest+1.856m/wood0/mainescapes0/recoil58. Native game/front/side/threequarter/ship complete movies encoded in same folder. This predates latest pressure/soles/full-tail stance changes.
3. `.codex/encounter-v015-submerged-entry/verification.json` FROM WATER+LOW HP full4+2/0gaps, nohop, foot1.223mm/slide1.3mm/chest+1.587m/wood.712mm/mainescapes0/recoil55, pause/actualdefeat/floathead4cm/loststick/unconsciousunarmedisland/mute pass. Latest before sole/full-tail grade and final mass rebalance.
4. Native traversal after perimeter pressure change PASS: drive6.017m/speed6.254/yaw38.35/reverse-3.604/jump2.561/carry4.110/fastswim3.733/moving-board climb/mobilelight/break release. `.codex/encounter-v006/traversal-verification.json`.

Failures retained for causality:
- v015-paired-stance3+1: firstparry1.493m/reversal12.717m and chest-1.053m.
- v015-water-low-health4+2 but chest-.718m in duck -> flotation issue.
- v015-perimeter-water failed first paused arm.168deg with clockfixed -> retained paused pose fixed.
- v015-perimeter-water-pause1+1 chest+1.019m but dense sail/mast blocked entry16m/14m -> submerged entry route added, subsequent water run PASSED.
- v015-low-frame-rate NORMAL genuinely stalled native updates55ms/frame, TWO REAL HOPS,3+1: throwgap.091m/reversal4.06m, footInfinity duringentry, chest-.40 atlanding. No fake damage. Wrapper finally restores frame settings/stall removed. Improvements added corridor/fulltail/sole/throw after this.
- v015-complete-stance latest NORMAL3+1 nohop: foot1.155mm/slide1.259mm but live splinter board submerged chest-4.508m; third counter.414m/reversal.0705. Final actualtail0 and genuinefall. NOT accepted.

Current critical physics finding/fix:
`Story1WreckFloatBody` now samples four actual perimeter points, fits irregular four-point water plane (5surfacequeries/.08 unchanged), permits single wet corner lift while bounding totalpositive2.4g. Original materialvolume/gravity/meshes remain. Native `.codex/encounter-v015/buoyancy-centre-before.json` proves equalquarter force centre up to2.871m from ACTUAL registered native COM on deck3; major unphysical torque. Added pressureWeights solving two-plane moment balance.
First attempted Initialize-time fit was WRONG because newly cooked native compound colliders still reported COMzero before initial physics registration. `.codex/encounter-v015/buoyancy-transient-mass.json` and audit prove weights balanced tozero while actualCOM later2.6m away. Latest SOURCE now re-fits after registered native mass geometry: first3FixedUpdate reset nativeCOM/inertia, then compare current native localCOM to weightedCentre (1mm change) and RebalancePressure. This latest fix compiles clean (CS0136 centre shadowing repaired to nativeMassCentre), NOT YET native verified. Need run AuditWreckV012.Begin then AuditBuoyancyV015.Inspect, verify mean matches actualCOM, then flat-water stability/native traversal and fullnormal/water/twohop/lowFPS regressions. Do not repeat blind random tests while known hydrostatic imbalance remains.

Tools added: AuditAnatomyV015,VerifyAnatomyCacheV015,AuditFootSolesV015,AuditBuoyancyV015,VerifyLowFrameRateV015. LowFPS wrapper invokes latest already-loaded VerifyEncounterV006 via reflection; after domain reload run normal verifier FIRST to load it. Native stall55ms ensures real low-rate Physics/Animator/Update; use captured meanFPS proof. Wrapper timeouts do NOT stop async run; inspect metrics and livephase before next test/stop/edit.

CurrentUnity after latest clean compile is warming first Play. Last tool editor_play returnedEntered; immediately AuditWreck/AuditBuoyancy both NETWORK errors, no successful native task started/result, no functions cell active. Verify actual editor/scene/log first, warm retry as needed. Cold firstPlay often ends/stops/imports; second warmPlay and native firstscript cost~20s compile, network resets transient. Do NOT describe this as external blocker. Autotick16ms remains.

Save backup `.codex/encounter-v006/backups/save.json` authoritativeSHA25651C5630337B45FF9F73A52216E0539D70F72D6D985DB05D2BA07FAD7E173B214. Live save may have been mutated by native tests. STOP UNITY then copy authoritative backup to `C:/Users/pinyu/AppData/LocalLow/DefaultCompany/Dark Brine_Rot/dark-brine-rot-save.json` and verify exacthash before any end/yield. Backups of owned pre-v015 code under `.codex/encounter-v015/backups/`; preserve all other user modifications/originalrigs35Actions. Need final multiview continuous native art review, commoncontrol regression and remaining causalhand-off<=1s clock check (old poll measured initdelay1.29s) and lowFPS/twohop/sameboard/water tests. Do not mark complete or delete automation yet.

## 2026-10-08 14:02 continuation: v016 visible caudal fin and runtime integration

Normal user turn remains active/incomplete; solo, no commits/push. Unity source recompiles clean. Current Play is warming; no detached full test currently active.

Confirmed latest v015 registered COM re-fit native20boards maximum89.8micrometres; true irregular deck flat-water hydrostatic PASS0deg/nearzero heave. Latest v015 traversal13:09 PASS. After that full v015-balanced-full had ALL4+2 exactcontacts/mainChest+1.803m/wood0/mainEscapes0, but FOOT INFINITY entry-0.479s: NOT accepted. Subsequent v015-moving-entry3+2 and v015-wide-stance-water3+2 failed dense wood and submerged board. v015-ready-support-water1+2 had foot1.57mm/slide2.206cm/no mainescapes but chest-.267m and obstructed first three actual counters. All failing folders retain metrics/previews; never substitute historical passes for current acceptance.

Entry production fixes: ground starting feet now stored in actual board-local coordinates (world-fixed start left moving timber); walk duration matches -.4 target. BeginFinale decides ground entry by native exact surface ray and .65m actual support proximity, not <=2m footprint edge alone. FindEntryEdge inset now measured foot radius+.3m, corridor grows .25m to fullfootradius (old whole-radius circle around edge startingfoot rejected every interior candidate). Scores actual relative future water crests vsCOM over20s. FindLandingBoard skips surfaces>.6m underwater or>2.5m abovewater. Water climb landing inset widened for standing feet; hands retain actual outer edge. Before grabbing, wait/swim if targetfreeboard outside-.6..2.5 or point velocity vsactual wave derivative>3m/s; extends physical lead only, main20.8 unchanged. Latest entry first.18s blends captured original localbone pose before support rather than snapping directly to authored walk. This last entry pose blend NOT YET full-tested.

CRITICAL real art defect proven `.codex/encounter-v015/shark-surface-audit.json` first native closest-triangle audit (correct BakeMesh(true)): Nose surface gap.12mm, Tail marker2.19..2.30METRES from visible skin! Dorsal overlay from original Tools/BlenderSharkAnimation.py painted all upper fin vertices regardless rear axial location; upper caudal fin therefore followed Spine_Mid/Dorsal while tail bones/marker swung elsewhere. Historical contact metrics were to an empty tail marker, not sufficient visual proof.

v016 source production: `Tools/AnimationPipeline/blender/fix_shark_tail_skin_v016.py` reads ORIGINAL v006 source, corrects only Dorsal influence on rear Y>5.5 back into existing Spine_Rear/Tail_Base/Mid/Tip interpolated chain. 684original Blender vertices (791 FBX splitvertices) changed. Versioned `ArtSource/Encounter/v016/shark-skin-v016.blend` +tail-weights-v016.json preserve16bone names/head/tail/rest/bind/ALLold Actions. No rebuild or overwrite. Native `InstallSharkSkinV016.Install` matches all684 original rest coordinates EXACTLY (maxerror0), clones old imported sharedMesh with corrected weights and existing bindposes, samples original31-frame culling range, clones actual rig prefab/controller/Avatar unchanged to Assets/Resources/Encounter/v016/EncounterShark.prefab +TralaleroCaudalSkin.asset. Backup PreviousAnimationSet.asset in samev016. Active TralaleroAnimations set now references v016 prefab, originalv006 prefab and35Actions retained. `.codex/encounter-v016/skin-import-report.json` proves originalAvatar/controller/bind retained.

Native revised skin alone reduced marker gap4.46cm, but that still added hidden error. `TralaleroSwimAnimator.FindVisibleTailContact` selects closest ACTUAL fullyTail_Tip-weighted imported vertex once using original bindpose into bone-local. TailWorldPoint follows that exact surface vertex thereafter, no per-frame BakeMesh, no original bone/marker modifications or collider enlargement. CalibrateSharkTailSurfaceV016 recomputes static ship/tail source contacts with original .65clips and exact newpoint. Native `.codex/encounter-v016/shark-surface-verification.json`: Swim gap.122mm, Bite0, Tail.2s.000238mm, Tail.65s0, Ship.65s.0124mm. Exact visible caudal point proves true visualcontact now.

Substantive FloatBodyv016: reuse EXISTING fifth surface sample at actual nativeCOM. Half pressure represents interior, half existing perimeter probes/weights; no addedqueries (5/.08unchanged), still actual mass/nativeinertia/gravity/free release, totalup2.4g, physicaldistributedtorque. Corner-only buoyancy ignored crests under middle of broad deck. Native interior immersion also participates wet state (corners all dry should not prevent middle contacting a crest). New central-surface damping/extrapolation, native force atCOM plus original four AddForceAtPosition. Source backup `.codex/encounter-v016/Story1WreckFloatBody-before-centre-pressure.cs`. Actual native flatwater AFTER centre quadrature PASS0deg/heave8.9e-8/angular.000106rad/s/forcecentre52.5micrometres. Latest native traversal AFTER centre quadrature failed automaticclimb: .753m edge, NearbyBoard correct, hintSwimtowardsboard. Does not prove controlsaccepted yet.

Latest `Story1WreckRider` FIX, NOT YET TESTED: sustained input .2s accumulation excludes ClimbClear; only actual start checks current valid space. Previously each momentary passing crest erased sustained intent; keep same-target intent while waiting for valid space, reset on board change/inputloss. No reach increase/no collision skip. Need rerun actual moving-edge traverse. `SahurMotionContext.LastClaimTime` records only actual owner/state transition; VerifyCausalHandoffV011.ChargeDeadline now compares nativeClaimTime to actual finaleRequestedAt (one-second+nativeframe1.075), preserves separate post-initialization poll delay. Previous1.29poll failures were clock observation, source still safe1s; NEW actual timestamp test NOT YET RUN.

Latest full NORMAL v016-surface-normal: FOUR real hero contacts confirmed, reversalTail.04599m confirms, FINAL TAIL .13742m MISS because actual splinters blocked tail/body; correctly released instead of fake death. Chest+2.126m/wood.912mm/mainEscapes0/recoil59/slide1.31mm. EntryFootInfinity atinitialtime-3.840 due firstclip transition; above new capturedentryblend addedAFTER this. Source latest final/reversal target now points inside upperhurtbox .35radius rather than nearoutertip.85radius, preserving exactcolliders/5cm thresholds and actualcaudalvertex, NOT YET FULL TESTED. Game main sheet reviewed; missingfinale frames genuinely repeatedlast18.10, neverclaimfallen/unarmedisland thisfailedrun. Grounded repeatedstanding afterMISS is correctly returningcontrol, distinct fromacceptedhorizontaldeathcachefix.

Other v015Tools AuditWaveBuoyancyV015 (15s truewaves/originaldeck clone+300 test-only) proves native board response maxsettledtilt12.52deg beforecentrequadrature; huge fresh boardsunderwater in pressureaudit often stillfalling fromrealexplosion, notstaticfloatingfault. AuditBuoyancyPressureV016 captures actual20 registeredpressures. AuditSharkSurfaceV015 computes closestTRIANGLE surfaces at5poses, not AABB/guessedskin. New latest Caudal helper exported weightdata only, no newFBX/action export needed because originalAvatar/bindbones remainexact native asset. Review actualcaudal deformation continuous/native before final acceptance.

Next priorities: rerun moving-edge auto climb and native charge deadline; latest full NORMAL + WATER/LOWHP + true lowfps/twohop. VerifyLowFrameRateV015 now accepts revision string so preserve oldfails; must load current VerifyEncounterV006 first afterdomainreload. Complete realboard and no-neighbour variants (recent strictstance candidates often0hops; do notstage boards). Densewood actual endpoint path may still need fullsupport candidate selection whilefromWater and continued contact geometry/motion planning. Need visibly tail-breakboard duringfilm ifpossible actualcontact, not directtimeline damage; previous accepted historicalbrokenWood0. Final native game/front/side/threequarter +ship movies and reviews, ordinaryisland control, contact<=5cm/foot<=3/slide<=5cm/no mainescape/fallwavehold/loststick/unarmedisland/mute. User task NOT COMPLETE, don'tdeleteautomation.

SAVE: authoritative `.codex/encounter-v006/backups/save.json` SHA25651C5630337B45FF9F73A52216E0539D70F72D6D985DB05D2BA07FAD7E173B214. Live may change after native tests; STOP then restore exactbackup/hash before ending/yielding. Last STOP+restore succeeded before latestcapturedentry/climb/targetsourceedit. Current Unity firstPlay warm; no active native test yet. Continue independently, no questions.

14:13 latest native: ChargeDeadline PASS actualMotionClaimTime-requested1.002101s; post-init polling1.191727s recorded separately. Latest traversal after persistentintent change PASS: drive7.410m/speed7.580/yaw37.517/mobilelight/reverse-3.921/jump2.809/carry5.525/fastswim3.870/moving-edge climb/remount/boardbroken release. Two earlier latest-centre-pressure random traversal failed auto climb on board5(.753m) andboard16(.340m); source now diagnostic failure writes detailed intent/dot/gap/blockers before throw; subsequentrunpassed, no diagnostic json generated. Do not claim allpossiblegeometryvariants covered from onepass.

v016-solid-tail NORMAL fulllatest(entryblend+insideuppercaps target .35radius) confirmed initialsupportFIX: maxfoot1.372mm/slide2.777mm, actualhero4 contacts, FINALtail0 succeeded/fallheldhorizontalhead4cm/loststick executed, but REVERSAL tail6.004m missed dense moving details. Chest-.103m atthrow8.55s/board3tilt5.9; body brieflybelowcrest despiteplantedfoot. Strict FAILED noverification. No mainescapes/wood.466mm/recoil52. Needrobustsixcontacts+above-waterfightingposture beforefinalacceptance. ExistingSourcebuffers20.8 staysrealcontactcausality.

Latest source change AFTER that film, compilesclean, NOT TESTED: ChooseAttackFacing re-plans at .04s near impact vs.12s anticipation and no longer stops near .6tail/.48bite while floatingwoodmoves. Tail candidateyaw expanded through±180; stancegrade tailyaw alsocoverswideactualspace. Rootheadingchanges limited300deg/s based actualplanningdelta, actualrig solver stillcapsworldtravel and verifies finalskinpointcontact. ProtectsBoard now protects actualstandingcurrentboard and airborneTARGET, not abandonedtakeoffboard whileSahurairborne; physicalanatomicalhits maybreakfreelyabandonedwood without manufacturedtimelinebreak. Need continuousartreviewofheading/no snaps plusnativeone/twohops, truewoodbreak. NativeordinaryUsageAllowedtrue primary29%/weekly66%at14:13. CurrentUnity firstPlay warming afterlatestcleancompile; no asyncnative test. Save51C563 restored atlastSTOP, beforenewPlay. No commit/push/userquestions.

## 2026-10-08 14:54 continuation: native v016 contact and route fixes
NORMAL user turn active/incomplete, solo. User last continue; no questions/no commit/push. Latest Unity warming/running VerifyEncounterV006.Verify(false,true,true,false,true,"v016-contact-pose-normal"), functions yielded cell207. Do not stop/edit while native async continues; timeouts don't cancel.

Latest accepted before subsequent changes:
- `.codex/encounter-v016-physical-hop-normal/verification.json`: NORMAL random deadline22.46812, exact six4+2 contacts; foot2.597mm/slide2.451mm/chest+2.643m/mainescapes0/wood.597mm; pause/fallwavehold/head4cm/loststick/lethal/unarmedunconsciousisland/mute allPASS. No real neighbour0hops/1breach/brokenWood0. Native game/front/side/threequarter/ship movies encoded, game sheet reviewed. This predates later hard-clearance/feasible-heading/contact-hold changes.
- `.codex/encounter-v016-clearance-water/verification.json`: WATER+LOW HP full4+2 exact0gaps; foot1.589mm/slide1.217mm/chest+1.433m/mainescapes0/wood2.445mm; allpause/lethal/fallheldhead4cm/loststick/unarmedunconsciousisland/mutePASS. Native5movies encoded/game+front sheetsreviewed. No neighbour0hops/brokenWood0. Predates feasible-heading/contact-hold changes.
- latest actual caudal v016 surface test still max.122mm, all original35Actions/16boneAvatar/binds retained. No new Actions generated this turn; versioned skin weights684original/791importsplit changed only inappropriate dorsal influences on uppercaudal.

Native controls current still v01613:09 traversalPASS and14:13 causal charge claim1.002101sPASS; no new player locomotion source changes after those, aside from cinematics/support selection.

New production fixes after14:13:
1. WiderBITEyaw search through180 likeTail, native wide-bite-water fourthactualcounter fixed, reversalnearMISS.05338/final0 sofailed4+1; allfoot/contacttrue thresholds unchanged.
2. Separately track chosen stance's physical overlap COST from submerged appearance/style. Previous BeginHop poseCost>.06 erroneously refused contact-clear boards simply because sharkbulk exposed. Retain physicalcost<=.0015 forhop. IMPORTANT units: PoseObstruction returns SUM OF SQUARED PENETRATION DEPTHS, NOT METRES. Oldcomment1.5mm was wrong; latestcommentcorrected. Fieldnames chosenStanceWoodPenetration/reachableShaftPenetration mean m²cost, don't reportasmm. Native final collision/foot metrics remainactualmetres.
3. TailContactTarget now actual uppertorso AXIS endpoint rather than endpoint+radius*.35, genuine deepercaps margin, no hitboxenlargement. Normal/waterboth6passed above.
4. FindEntryEdge now grades ALL candidate realedges instead oftop6. Physical clear endpoint candidates prioritized over appearance: costpenalty10000 versus waterexposure .2; explicit clearFound tracks cost<=.0004 (m²) to choose geometrically clear orientation over submerged intersecting orientation. Route intermediatecaps cost*2000. Initialstance score penetrationCost*100000 +poseCost*.2. Body/submergence appearance secondary to collision feasibility. FindLandingBoard optional thinDeckOnly (<=1.2m) preferred when no actualrider support; readinessstill[-.6,+2.5] freeboard. Somewatertakes stillsupportactualthickboard viafallback/live Support, acceptedwaterchosen16thick3.19m neverthelesssafe.
5. ChooseAttackFacing grades ACTUAL reachable heading thisframe: candidate RotateTowards(existingFacing,candidate,300deg/s*planningDelta) BEFORE PoseObstruction/path/exposure scoring; no post-score anglelimiting. Previously idealclear45degree target couldbechecked butactual51deg transition hitwood .4m androotcorrected2.5m. Continuous heading retained; no snaps/boardrelocation.
6. Freeze selected true shaft FRACTION throughout each actualcounter (1.8..<3.2 and6.4..<7.8), parryuntil1.8. Keep updating sampled position with actual boardtilt, preventing reach target jumping betweenstickends witheach.5deg normalinvalidate. Originalclip/wholeweaponactualdimensions retained.
7. MOSTRECENT CRITICAL boundarybugfix (cleancompiled, FIRST TESTRUNNING): exactlyat t2.43/t7.03, old t<contact expression switched BITE to SWIM BEFOREactualhitverification; plannerhadgraded Bite.52 butactualcuegeometryalreadySwim0! Now hold Bite_Lunge clamped.52 through unconfirmed actualcontactwindow, only switchHit_Recoil after realverifiedcontact orSwim afterwindowexpires. Thrown Swim phase stayscontinuous untilrealhit (previousresetto0 att8.94 ifunconfirmed). No falsehit, nofakelethalcue. This mayexplain many random counter/sweptcollision failures, needsnativefullregression.

Failures retained:
- v016-adaptive-tail-water3+2, onlylandingSlash2.9m blockedoccupiedHull1; foot2.394mm/chest+1.825/mainEsc0. WidenBITE then3rdcounterfixed.
- v016-physical-hop-water4+2 foot1.051mm butchest-.13385 @11.18 thickHull15 centre-5m; failedextraabove-waterfightingcriterion, noverification.
- v016-physical-hop-lowfps1+2 (firstcounter.0575/landing3.571/throw.0866), foot18.5mm/chest+1.13. Stronggeometry prioritizationaddedafter.
- v016-clearance-lowfps3+1 firstcounter.961/final1.472 due idealversuslimitedactualheading; bodychest+2.94/foot1.343mm/slide1.107/mainEsc0. ActualfinalMISS correctlyreleasedcontrol/notfakefall.
- v016-feasible-dense1+1, counter3.31/.224/1.438 and reversal.109; foot.931mm/slide1.734mm/mainEsc0 butchest-3.028 @14.617 thinHull2 centre-6.93. Could be actualwave+physical debris load orforce response; need native buoyancy pressuretrace ifrepeat, no changingocean amplitudes tohide. CurrentOceanwaveamplitudes1.9/1.1/.42/.18 remainoriginalscene/style.
- v016-feasible-lowfps **ONE REALHOP**, landingerror.954mm/foot1.126mm/slide.783mm/chest+1.416/wood0/mainEsc0. Hero3/Shark2; ALL exceptlandingcounter4.943m, throw12.783mm confirmed. Real originalHull8→actualotherboard; no woodstaging. Shader/nativegame render/multiviewcaptured. Contact-pose-hold fix appliedAFTERthisfilm. Noverification.json, don'tclaimwholepass.

New readonly nativeTool `Tools/AuditHopEligibilityV016.cs`: Start creates ephemeral DontDestroyOnLoad observer, atmain4.05–4.5 records nearest8 actualboard eligibleconditions; onlycalls existing real FindEntryEdge ifdistance/highheight etceligible, no boardmutation. Audited `.codex/encounter-v016/hop-eligibility-audit.json` in faileddense: nearestHull1 13.045m away/dy3.685; others22–47m, so NOsafeNeighbour islegitimate. Observerdestroysitselfafterreport. No extraagents.

Next: wait latest contact-pose-normal metrics; then native LOWFPS whole6/timer + actualhop acceptance (latestonehop gotonlylandingcountermissbeforerigposefix), WATER/LOWHP, nativeordinarycontrols latest. Consider actual sharkmovement afterLandHop: nonreversal RenderTraversal still positionsshark by OLD departure+NEW Forward; body maycut abandonedboard. Ifcorrectcuepose doesn'tresolve landingmiss, route shark smoothly to real landingboard's open-water approach (cansearch actualsubmergedroutes, no stagedpieces). Need actualtail/breach boardbreak intruehopbranch; CanBreakBoards allowsactualanatomy hitsagainstabandonedboardduringairborne, nevermanufacture damage. RecentacceptedbrokenWood0, don'tclaimnewtailwoodbreakverifiedyet.

Need continuous native artreviewnewskin/heading/actionarcs;5 latestwatchablemovies. Existing current acceptedwater andnormal video files availablebutprelaterfixes, don'tsubstituteasfinalacceptance. Commonisland/airborne/no-fake-tail tests available Tools/VerifySafeHandoffV011.cs, VerifyCausalHandoffV011.cs, ReviewControlsV011.cs/VerifyTraversalV006.cs. Need tests appropriateoncecurrentcinematicfixpasses; taskNOTCOMPLETE/automationkept.

Save: authoritative `.codex/encounter-v006/backups/save.json` hash51C5630337B45FF9F73A52216E0539D70F72D6D985DB05D2BA07FAD7E173B214. Last STOP+restore succeededbeforecurrentholdfix/recompile/newPlay. Live mayagainwrite; STOPthenrestore+verifyhash BEFOREanyend/yield. Originalresources/useredits/backupsretained. Sourceownedprechangesbackups `.codex/encounter-v016/Story1WreckFinale-before-*.cs`. No commit/push.

## 2026-10-08 15:50 continuation — v017 actual contact and approach revisions (INCOMPLETE)

Solo continuation of approved design-v006, no questions, no commits/push. Preserved original rigs, Avatar, Actions, saves and user changes. The latest earlier accepted full NORMAL take is still `.codex/encounter-v016-root-body-normal/verification.json`: native 23.1906-second random timer, four hero/two shark real contacts, 1.258mm foot/1.648mm plant slide, chest +1.9266m, main large escapes0, pause/lose stick/actual lethal/unarmed unconscious island/mute PASS. Five continuous movies encoded in that folder. It predates the following v017 revisions; never present as final current acceptance.

v017 source changes `Story1WreckFinale.cs`: prepared shark entry exits beside the actual future parry position, actual first probing bite now moves the whole root instead of turning a long animal around a pinned nose; root movement stays through the native solid wreck solver. Counter shaft planning cache now distinguishes entry, first counter and landing counter. Current right-hand strike shaft selection checks full native approach anatomy for three candidates per genuine shaft fraction; final attack fraction stays frozen, no enlarged colliders. Hop pursuit now approaches the actual landing board, replacing stale departure+new-facing reference. Final tail preparation begins16s and still uses original .65s authored active clip at17.4–18.05; main length20.8 unchanged. This improves time to turn around adjacent native timber.

Bounded real right-hand contact adaptation max25cm, active-window eased, original wrist/weapon rotation and supporting feet retained. Applied AFTER native shark/wood correction, BEFORE final contact verification; solver can still miss and no fake damage. `Story1CapsuleContact.Separation` shares exact same segment geometry as existing Gap. Thrown stick now targets the real head capsule surface with the actual projectile capsule-to-mesh centre offset; launch remains original grip/model centre. Previously lowFPS mesh-centre target left real capsule gap21.7cm. No changed collision radii, extents or5cm authorization thresholds.

Latest native v017-contact-water (WATER+LOW HEALTH) and v017-contact-lowfps BOTH confirmed ALL FOUR hero contacts0gap and FINAL tail0gap; feet2.164mm and2.812mm, slide2.473mm and2.857mm, main escapes0, chest+.5403m and+2.682m. Both FAILED mid reversal tail (3.2005m /8.881m) obstructed by actual occupied support, so NO verification.json; NOT COMPLETE. Water final fall/lose-stick actual confirmed. Keep failures and previews, side sheet reviewed. Do not claim full accepted stage from these.

MOST RECENT CLEAN COMPILED FIX NOT YET TESTED: reversal tail route searches native intermediate wind-up anatomy at .1/.25/.5/.75/.9 through a bounded downward/outward dip; same original endpoint/contact, real body/collision, route offset fades tozero at impact. Replans offset continuously3m/s. Final tail remains its already working2.05-second direct preparation. No staged boards/fragments. Need native lowFPS true-hop and water/normal full regression next.

Read-only `Tools/TraceFightSupportV017.cs` added, records registered native support COM/mass/heave/centre+perimeter pressure/nearby bodies during real movie; Stop/Start identifies dynamic tool instances by type NAME across fresh run_script assemblies. First version Stop by generic concrete type could miss prior in-memory-assembly instances; fixed. Requires native Play mode before creating anything; cleans owned previous traces. Earlier traces reused filenames across subsequent films and should not be interpreted as a single take; native metrics folders remain authoritative. No production flotation changes this v017 continuation. Previous flat-water/native traversal proofs remain valid for unchanged buoyancy; no longer seeing chest-underwater in latest two takes.

Current Unity warming Play after latest clean recompile, no active detached verification yet. Cold firstPlay may stop/domain-reload and eval maytimeout; warm confirm Application.isPlaying before scripts. Await async tests; RPC60s timeout does not cancel native task. After failure cinematic may automatically retry3s later, distinguish saved first-take metrics from current livephase. Do not launch overlapping verifiers.

Last STOP then restored authoritative save exact SHA25651C5630337B45FF9F73A52216E0539D70F72D6D985DB05D2BA07FAD7E173B214 before this newPlay. STOP and restore again before any end/yield. Original backup `.codex/encounter-v006/backups/save.json`, live path `C:/Users/pinyu/AppData/LocalLow/DefaultCompany/Dark Brine_Rot/dark-brine-rot-save.json`. Owned source backups `.codex/encounter-v016/Story1WreckFinale-before-*.cs`. Automation still required because task remains incomplete.

## 2026-10-08 16:10 continuation — guarded tail / genuine same-board traversal

INCOMPLETE, solo, no commit/push. Current source is later than v01715:50 checkpoint. No active detached verification at this checkpoint; latest source compiled clean, Unity warming Play. Original rigs/Avatar/all35Action assets unchanged.

New actual reversal contact now tail SKIN vs held RIGHT-HAND WEAPON, conveying force through the guard into the authored chest/hip stagger. Final defeat still actual tail skin vs torso; no fake damage/loss. `PredictGuardWeaponTarget` samples actual future Brace.572, native capsule far end; target offset now stored in future-facing WORLD units and uses actual destination board/heading during hop anticipation. Critical bug fixed: restoring a temporary future guard sample must NOT plant an airborne hero back onto the old board. Uses landing board only while predicting, restores original support/bones/position, skips restore-PlantFeet while `Jump airborne`.

Pending reversal window retains correct Brace.572 / Tail.65 until actual authorization rather than switching to unrelated guard/stagger before late contact. Stagger only after verified real guard contact. Diagnostic verifier now records reversal surface vs final torso explicitly. v017-guard-tail-normal native first2+throw/two tails genuine0, foot2.342mm/slide2.62mm/chest+.733m/no main escapes; FAILED only landingcounter2.245m, no verification. Movie automatically reached actual fall/float; fails strict four hero contacts.

Same-board variant BUG: FindEntryEdge found a real better stance but fallback held original feet and never applied the landing offset. Now uses original authored PLAYER_Walk on actual supported path at <=3m/s with .2s acceleration/braking and distance-based gait phase; then native guard and LandHop (doesn't invent a board jump). Search radius capped by available physical walk duration; actor faces movement then real open water. Real inter-board jump preparation limited <=3m/s*time to a short supported step, replacing possible whole-deck move in .37/.48s. Landing offsets .15/-.3 and initial surf -.4→.15 now ease instead of instantaneous root jumps. Future guard target while traversing uses actual future landing board/heading; restored sampling no longer destroys airborne height. Standing guard during fallback now included in native planted-slide measurement.

`Story1WreckRider` now releases deeply submerged support (>1.1m immersion) with actual inherited velocity instead of forcing player to stay grounded underwater. Support scans don't return such submerged surfaces; auto-climb target must be <=.6m underwater. Kept .2s sustained intent,1.2m reach,.35s regrab block, real clearance checks and all base movement/jump/board speeds. Physical freeboard queries run after real support/near-edge rejection to avoid extra far-board ray/water work. FindLandingBoard now scans ALL actual planks including newly fractured splinters, not first20 original boards.

v017-walk-fallback-normal native ALL FOUR HERO CONTACTS0; foot1.465mm/slide1.652mm/chest+.804m/no main escapes; both tails blocked (6.89m/3.653m) on actual narrow splinter board5/2. NO verification. Newly created current splinter support valid (4.85×.88×14.09). Same-board walk fixes the previously missed landingcounter, but whole film NOT accepted. Current native normal has0actual inter-board hops (legitimate same-board variant). Still require current lowFPS actual hop plus water+lowHP and controls/airborne/charge/no-fake-tail regression.

MOST RECENT source `Story1SharkWreckContact.PushFloatingWood`: protected occupied boards DID HAVE IMPULSES, capped1m/s, not disabled (corrected initial mistaken user commentary). Now same mass-based cap as other physical timber (>=30kg2.5m/s, light6), native reduced-mass impulse1500kg animal, actual overlap/contact lever arm, one body push per fixed tick, no board pose moves/teleports. Support protection still prevents premature FRACTURE; no collision radius/threshold changes. Removed unnecessary occupied argument/BoardCarriesPlayer calls from force loop. Backup `.codex/encounter-v016/Story1SharkWreckContact-before-support-impulses.cs`. Latest impulse revision clean compiled BUT NOT YET native tested. Need test this, not claim proven root cause. If tail still blocked, consider selecting reachable elevated guard shaft fraction (currently far end can point down near surface); actual geometry/path/skin contact must remain <=5cm.

Native pressure/buoyancy source unchanged since v016, no ocean waveform changes. Latest failing takes now chest positive; previous dense long-board chest underwave failure retained under v017-tail-corridor-normal. Don't hide with relaxed metrics. All tests remain actual released hull collision meshes/gravity/free physics. Need latest game/front/side/three-quarter and ship continuous movie encoding/art review after full pass. Existing v016-root-body-normal accepted movies are historical, not latest acceptance.

Save STOP+restore succeeded immediately before latest force edit/recompile, exact SHA25651C5630337B45FF9F73A52216E0539D70F72D6D985DB05D2BA07FAD7E173B214. Restore authoritative `.codex/encounter-v006/backups/save.json` to `C:/Users/pinyu/AppData/LocalLow/DefaultCompany/Dark Brine_Rot/dark-brine-rot-save.json` AFTER STOP before any end/yield. Runtime tests' finally restore cannot prevent later scene/shutdown flush. Save/source backups preserved. Automation still required, task not complete. Unity forced recompile status often returns before actual40s domain finalization; allow editor_status/recompile settle before Play, and warm Play before scripts. A 60s RPC timeout leaves async native test running; saved pose metrics indicate finished first take even if natural missed-finale retry starts later. No overlapping verifiers.
## LATEST — 2026-10-08 19:17 Vancouver: user steering implemented; encounter INCOMPLETE

Continue solo without questions, commits or pushes. Two new user requirements override older locomotion plans:
1. Only small surfboard-shaped real ship timber is drivable, with leg paddling. Larger hull pieces remain walk/jump/combat platforms.
2. **Keep the PREVIOUS walking, running and rolling animations.** Do not reintroduce PLAYER_Walk/Run_v006 to ordinary locomotion. Original Locomotion blend-tree and original Roll references/playback are restored from SahurGrounded.controller to active SahurGameplay.controller. Ground procedural lean/bank is disabled; swimming response remains. Cinematic walking prefers the existing Walk_Loop. Native read-only verification `.codex/encounter-v018/legacy-locomotion-verification.json` confirms original references, original Roll speed1.85, active LIVE SahurGameplay and retained surf/climb states.

Unity is STOPPED in Edit Mode. No detached tests or observers remain. Authoritative save restored AFTER STOP, SHA25651C5630337B45FF9F73A52216E0539D70F72D6D985DB05D2BA07FAD7E173B214. Restore again after future Play tests. Original rig/Avatar/history/saves protected.

### New integrated assets and controls

- v018 authored original UAL2 rig copy: ArtSource/Encounter/v018/sahur-board-paddle-v018.blend, PLAYER_BoardPaddle_v018 and Reverse, 1.2sec/60FPS/frames1–73, original rig and left source ankle preserved. Native valid Humanoid import under Assets/Resources/Encounter/v018. Active Wreck Surf tree uses new forward/reverse leg strokes with BoardStrokePhase shared with physical thrust, old idle/turns retained. Physical drive recovery coasts; unchanged10/4.5m/s caps and90deg/s target. Surf direction follows the actual LONG hull axis, including long-X timber. Authored source first-pass reviewed; native leg placement still needs polish (below).
- Story1WreckPlank.SurfboardShape: width1.15–3.8m, length3.2–9.5m, aspect>=1.6, thickness<=min(.9,.4width). Both rider Mount AND plank Drive reject large pieces. Larger board support remains allowed. Original 202 whole hull pieces had NO eligible surfboards; native source audit found six eligible existing cut splinters. Story1WreckBattle now releases all existing real cut shards of those specific original sources at INITIAL ship impact, at original ship transforms/velocity+spin, with real gravity, mesh collision and native buoyancy. No staged wood or manufactured boards. InitialFragmentCount/ExpectedInitialFragments track genuine source replacements. Backups under .codex/encounter-v018/backups.
- Story1WreckRider.ClimbClear now uses actual shoulder/arm reach instead of fixed feet-to-deck3.8m cutoff, and correctly scaled CharacterController landing capsule. Sustained .2s swim intent,1.2m edge distance and.35s regrab unchanged. SahurMovementPose measures actual skin sole once and leaves right paddle leg free while left uses bounded support IK.
- Tools/VerifyTraversalV006.Hold now waits SIMULATION time (with bounded wall timeout), not fixed wall time: native recording previously shortened 1.7s input to <1s simulation and falsely failed acceleration. Current real controls test PASSED `.codex/encounter-v018/traversal-verification.json`: auto small-board mount,3.07509m forward/3.06473m/s,35.8681deg steering DURING light attack, physical brake1.16342m/s/reverse−2.14196m/s, jump3.21473m with inherited board velocity, sprint swim3.81665m/.6sec, continuous swimming, MOVING-EDGE automatic climb, broken support release. Native153frame continuous movie `.codex/encounter-v011-controls/v018-native-simulation-timing/native-controls-review.mp4` encoded. Test isolates an actual released original splinter only for controls fixture; production never does this.
- **Native paddle art defect remains:** readonly actual skin-sole trace `.codex/encounter-v018/actual-soles-foot-trace.json` shows right stroke sole still OVER deck footprint during .35–.65 power, water depth sometimes−.77m. Do not call this a physically convincing water stroke yet: needs actual player stance near side edge / reachable outward leg arc without sliding supporting foot, NOT fake splash/thrust. Left support settles to <1mm but first blend samples up to10.5cm; inspect source/transition. Camera still frontal of actor in fixture, normal locomotion verified by actual asset references rather than new art replacement.

### v019 shark tail source and current failed native encounter

- New versioned source ArtSource/Encounter/v019/shark-lift-v019.blend copies corrected original v016 caudal skin/rest rig, creates CH1_SharkTail_v019 and CH1_SharkShipSmash_v019. Both1.5sec/60FPS/1–91/.65contact, body remains level while rear spine/tail sequentially lift. Source front/side/3q reviewed first-pass; original16bones/binds/weights preserved. Native Generic import under Assets/Resources/Encounter/v019, active v006 EncounterShark.controller Tail_Strike/Ship_Smash use new clips. Tools/AnimationPipeline/InstallV019.cs backups previous controller/animation set and calibrates actual .65 tail skin point. Tiny contact metadata values are mesh-filter-local CENTIMETRES under original100x parent; do NOT rescale.
- TralaleroSwimAnimator.ActionDuration now recognizes CH1_Shark{suffix}_v* instead of only v006; otherwise1.5sec clip fell back1.15 and sampled wrong .65 pose. Story1WreckFinale reversal guard contact uses actual elevated15%-in shaft point, not downward far endpoint; tail root pitch limited±25deg (bites/breach unchanged). Final tail remains actual skin vs torso; reversal actual skin vs held right-hand weapon. No fake hit or expanded threshold.
- Latest CURRENT full NORMAL native `.codex/encounter-v019-original-locomotion-normal/preview/pose-metrics.json`: **FAILED first parry and landing counter**, ContactAttemptGaps[3.1562645,0,2.592756,0,0,0], Hero2/Shark2, no actual hops (valid same-board fallback), breach1. Both actual tails and throw/counter genuine0gap. Actual support1.158mm/slide1.057mm, chest+.9175m, film large escapes0, wood penetration2.627mm, floating head+6cm. Ship contact/real initial fragments assertions PASSED before film. Original source remains dense random timber; first/landing head approach obstructed by real occupied hull15.17×.80×20.40. NO verification.json, do NOT call film complete.
- RPC60s timeout left async verifier detached; it subsequently wrote first-take metrics and completed/failed, then native game naturally reached island. It is now stopped; NO overlapping pending verifier.
- All FIVE current continuous movies encoded (game, ship-impact, front, side, three-quarter) under .codex/encounter-v019-original-locomotion-normal. ACTUALLY reviewed game and side16-key-pose sheets: rear tail lift gives contact while body remains mostly horizontal, removing old whole-body80deg upright reversal. Last defeat/face-up horizontal held poses observed19.66/20.64sec. First/landing attack misses remain major mechanics/art defects; clutter and intermittent timber immersion still visible. Native front/3q continuous review outstanding. These are failed current evidence, not final approved output.

### Next work

Preserve latest restored original walk/run/roll throughout. Fix right water-stroke placement/left transition support using actual source/skin/board geometry; prove large platforms cannot be driven. Resolve native first parry and landing counter approach against real occupied timber without fake hits, staged wood, root jumps, collider shrink, or tolerance relaxation. Then CURRENT full NORMAL, WATER+LOWHP, LOWFPS with genuine reachable hop and board fracture (or valid same-board fallback), safe airborne/charge deadline/unconfirmed-tail and island bridge regressions; inspect full game/front/side/3q/ship movies and revise real actions/support/contact. Historical v017 lowFPS full4+2 numerical pass predates v018/v019 and is NOT current acceptance. All acceptance limits and original ocean/float/audio/UI/save protections remain. Automation must stay because overall task incomplete; its resume file now contains latest steering.


## LATEST — 2026-10-09 04:23 Vancouver (heartbeat continuation)

Overall INCOMPLETE. Continue solo, no questions, commits or pushes. Keep automation. Latest user steering remains: original walk/run/roll; only small real surfboard-shaped timber is driven by actual leg strokes. Original locomotion references and roll speed 1.85 were reverified natively this turn. Detailed evidence is now in review-v024.md.

### Proven and active this turn

- v022 forward/reverse leg-paddle Actions and original-Avatar import: ArtSource/Encounter/v022/sahur-board-paddle-v022.blend, Resources/Encounter/v022. The active original v006 Wreck Surf tree uses these two clips only; idle/turns and restored legacy walk/run/roll retained. Source leg sweep/pelvis revised, not a rig rebuild. Native stroke force requires actual right sole outside actual hull footprint and below sampled ocean, with actual left support. Power phase is shared between animation and physical thrust; recovery coasts. Only narrow surfboard longitudinal drag .28 changes; lateral drag 1.15, vertical buoyancy and original waves retained. Rider release uses actual torso immersion instead of canceling strokes while the torso is high above water.
- `.codex/encounter-v022/traversal-supported-verification.json` PASS: auto mount, 3.165 m forward/1.804 m/s, 38.54-degree steering during light attack, brake .352 m/s, reverse -2.612 m/s, jump 2.277 m with inherited 5.762 m/s, continuous fast swim and moving-edge climb, broken-support release. Continuous actual native movie `.codex/encounter-v011-controls/v022-real-supported-paddle/native-controls-review.mp4` encoded and key sheet reviewed.
- `.codex/encounter-v022/native-paddle-contact-verification.json` PASS: 86 paddling samples, no unsupported stroke; left max 1.392 mm. All 26 actual water-engaged samples had left <=1.093 mm, right >=14.967 cm outside mesh and >=1.445 cm underwater. v021 source IK experiment was rejected; never activate it.
- `.codex/encounter-v020/large-platform-verification.json` PASS: an actual 10.24 x 2.91 x 17.04 m released hull rejects rider/throttle/steer and remains an ordinary movement/combat platform. Test-only isolated genuine timber; production debris is not staged.
- v023 authored head rise: ArtSource/Encounter/v023/shark-bite-v023.blend, original v016 corrected caudal skin/16-bone rig, Head rises 20 degrees with fin balance. Native valid Generic CH1_SharkBite_v023, active EncounterShark Bite_Lunge. Ordinary bite root pitch bounded +/-35; v019 original rear-tail lift/ship smash retained. Native import proof under .codex/encounter-v023.
- v024 safe handoff: actual CC grounding with nonpositive vertical speed, already riding, or swimming authorizes takeover. Grace near a board does not authorize airborne takeover. Motion context refreshes actual physical state. Native airborne-handoff, charged-handoff (1.0181 s) and unconfirmed-tail PASS under .codex/encounter-v024. Withheld lethal contact returned control and positive health, never invented defeat.
- v025 planning performance: four separate ENABLED trigger planning capsules at y=-10000; explicit native ComputePenetration poses and exact capsule AABB cull. Original live anatomy untouched. Native 90 poses/71 blocked/19 clear matched; max aggregate equivalent depth difference .078 mm. Check microbenchmark 47.51 to 31.38 ms, not a whole-game FPS claim. Disabled planning proxy experiment REJECTED. Proof .codex/encounter-v025/planning-geometry-verification.json.
- v026 actual Tail_Tip fin compliance: only near actual final contact, actual skin vertex shift <=.15 m/real bone <=5 degrees, actual wood checks, no root/body movement or distant auto-hit. Native geometry proof PASS: real 7.5 cm gap ->0, real 80 cm miss remains. Current full water take did not need adjustment; do not claim full-game improvement proved. .codex/encounter-v026/tail-compliance-verification.json.

### Full encounter failures remain genuine; no tolerances relaxed

- v024 additive near-edge experiment REJECTED after 10.38 cm slide and reverted to conservative circle. Both rejected v023/v024 sources are preserved; do not casually reactivate them.
- Current v026 water: first and reversal misses (4.163 m/.476 m), final real .35 mm contact and held face-up wave motion, but actual supporting 12.87 x .66 x 20.03 m deck capsized to 167.5 degrees and torso dipped 2.408 m underwater at 5.63 s. Major outstanding physics/support issue. All five movies under .codex/encounter-v026-tail-fin-water; game and side sheets reviewed, front/3q still pending.
- Current v026 low FPS: 518 actual frames, 102.87 ms mean/9.72 FPS, foot 2.35 mm/slide 2.71 mm, actual wood solve held. Three contacts missed (2.284 m/.625 m/.378 m). No final defeat invented. Five movies under .codex/encounter-v026-current-lowfps encoded, game and side sheets reviewed. Low-FPS sampling file writes even on failure; native frame settings restored. Any late support trace from this take was armed after its first failure and is natural retry only.

### v027 current cinematic blocking trial

- Source Tools/AnimationPipeline/blender/narrow_combat_stance_v027.py; ArtSource/Encounter/v027/sahur-supported-combat-v027.blend. Four NEW Guard/Parry/Slash/Brace Actions retain original Mixamo rest hierarchy/Avatar/right-arm local arcs and history. Narrow stance .67, actual knees solve, hips drop 5 cm plus 3 cm loading in SOURCE CENTIMETRES/Y-up. Correct 60 FPS 1.6/1.1/1.4/1.3 s. Source ankle error <.00005 cm, arm quaternion difference zero. Temporary diagnostic bone tubes never saved/exported as Sahur. Source-blocking-review.json documents limited blocking acceptance.
- Export complete, original-Avatar native imports valid, scale1, proper timings/loops. Native Sahur mesh 156 front/side/3q frames sampled and all three sheets reviewed (.codex/encounter-v027/native-preview). Isolated ankle/root radius max .912 m; actual skin-sole radius is larger (~1.259 m), and actual support is authoritative. native-blocking-review.json explicitly NOT final approval.
- InstallV027 backs up original EncounterDefinition to Resources/Encounter/v027/PreviousEncounterDefinition.asset and maps ONLY cinematic Guard/Dive/Parry/Slash/Miss/Brace clips to v027. Ordinary gameplay controller/attacks/walk/run/roll untouched. WreckFinale slash planning now follows the actual Slash beat clip instead of hardcoded v006; own backup .codex/encounter-v027/Story1WreckFinale-before-definition-slash.cs.
- v027 first water trial FAILED: chosen real 2.30 m wide splinter had no full stance circle; foot Infinity/slide52.3 cm, first slash .341 m/landing2.362 m misses. First parry, throw, both tails genuine. Full game/side sheets reviewed; all five movies encoded under .codex/encounter-v027-supported-stance-water.
- New SelectWaterSupport attempts another actual nearby flat mesh ONLY if water-entry target has no valid actual stance candidates. It verifies existing conservative source-contact geometry, recomputes moving swim/climb anchors and true swim distance; no moving fragments or teleport. Native second water trial .codex/encounter-v027-validated-water-support FAILED contacts [0,2.480,2.439,0,.498,.211], but valid actual foot1.349 mm/slide1.229 mm and torso+1.652 m, film large escapes0/wood0. Initial large target already had candidates; fallback BRANCH not independently proven. All five movies encoded, sheets generated but not yet reviewed.

### v028 experiment NOW running — inspect before any next verifier

Current Story1WreckFinale source adds optional .6 m candidate searches while retaining original conservative candidates. Optional candidates must pass SupportsContinuousStances: EACH 60 FPS frame of actual guard/parry/slash/brace/throw, BOTH actual soles on actual mesh within3cm, and native planted-foot anchor <=5cm through fixed support. This corrects v024's four-contact-frame-only test. Original candidate routes remain, no changed hit/support thresholds, no collider shrinking. The accepted candidate count now excludes failed sampled candidates. Backup BEFORE this experiment: .codex/encounter-v028/Story1WreckFinale-before-exhaustive-support.cs.

A native FULL normal-entry/low-health take `.codex/encounter-v028-continuous-support-normal` is currently running through Tools/VerifyEncounterV006, with readonly TraceFightSupportV017 armed BEFORE it. CLI session70898 may report 30s timeout; the native async CONTINUES. Do NOT start overlapping verifiers. Wait for verification.json or verification-failure.json completedUtc/pose-metrics. Then STOP support trace immediately; later natural retry rows are not first take. If exhaustive experiment fails support, preserve failed source/evidence and revert exact owned backup. Current v027 assets are a blocking TRIAL, not accepted production; consider restoring PreviousEncounterDefinition if their full native support/contact remains unaccepted. Keep valid versioned source assets regardless.

### Tools and protection

Unity6000.3.23f1 Pipeline7800, CLI C:/Users/pinyu/AppData/Local/Unity/bin/unity.exe, autotick16ms. Blender MCP9876 refused; portable CLI C:/Users/pinyu/.codex/tools/blender-workflow/portable/blender-5.2.2-windows-x64/blender.exe works (5.2.2). Python C:/Users/pinyu/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe. Do not use UI captures outside voice. Play/recompile calls acknowledge scheduling before actual transition; confirm editor_status and new assembly reflection first. RPC run_script is hard30s regardless timeout_ms, native Task continues. Independent encode/review scripts are safe while verifier runs.

SAVE: authoritative .codex/encounter-v006/backups/save.json. Live C:/Users/pinyu/AppData/LocalLow/DefaultCompany/Dark Brine_Rot/dark-brine-rot-save.json. SHA25651C5630337B45FF9F73A52216E0539D70F72D6D985DB05D2BA07FAD7E173B214. Last restore before v028 test; MUST STOP Unity THEN restore and hash AGAIN before final yield, because test-finally does not prevent subsequent game save flushing. No active recording helper beyond the readonly support trace.

Outstanding: reliable current four hero/two shark genuine contacts, deeply submerged/capsized support recovery, real reachable neighbor hop or accepted same-duration supported variant, actual fracture, complete normal/water/lowFPS regressions and continuous game/front/side/3q/ship review plus island bridge. Preserve actual gravity/mesh collisions/natural original ocean, noaudio/colored images/BWUI. Never claim completion from isolated rig or old successful takes. Automation remains ACTIVE because overall task incomplete.

## LATEST — 2026-10-10 03:48 Vancouver (continuation; native trial pending)

Overall INCOMPLETE, solo, no questions/commit/push; retain automation. Original walking/running/rolling and actual v022 leg paddles remain unchanged. Detailed current findings in review-v029.md supersede the older v028 RUNNING section above. That old task completed long ago; no old support trace remains.

- v029 active support admission/release rejects invalid physical support without pose displacement, invented damage or control lock. Native no-support-admission and broken-film-support both PASS under `.codex/encounter-v029`. Fracture fixture uses an actual original released broad deck, isolated only for the negative test; no production debris staging.
- v028 optional candidates retain conservative candidates and sample actual full stance clips: both soles must be over actual mesh with no excessive penetration, while the actual supporting sole must remain within 3 cm; recovering sole can lift. The old requirement that BOTH soles always stay within 3 cm was rejected and is not current. Candidate fixed-slide tolerance remains 5 cm.
- v030 landing anticipation active: shark starts its approach at 4.55 s during escape instead of only 6.4 s, same actual 7.03 contact. Native shark coordinate audit PASS (maximum .126 mm error). v031 remaining-route experiment worsened real tail gaps and was REJECTED/reverted to v030 exact backup; preserve rejected evidence without reactivation.
- v032 revised source torso/one right arm and retained original rig/rest/Avatar. Native review exposed that existing .63 s strike cue sampled a backward follow-through; v032 was NEVER activated. v033 makes a new actual Blender Action that maps source .600 s forward/downward strike to unchanged .63 s cue and spaces follow-through across .63–.92 s. Original rig/history retained, 1.4 s/60 FPS, original compatible Avatar. Native 96-image original-mesh/front/side/3q comparison reviewed. Active Resources/v033 Slash/Miss references only; ordinary attacks/controller/legacy locomotion untouched. Source `ArtSource/Encounter/v033/sahur-timed-slash-v033.blend` and `Tools/AnimationPipeline/blender/retime_slash_v033.py`.
- Current integrated `.codex/encounter-v033-timed-slash-water-owned` completed 10:40:20 UTC: all FOUR Sahur and TWO shark genuine contacts, gaps [0,0,0,0,.000160262,0] m. FAILED support 3.376 cm @13.563 s and fixed slide .78986 m. Chest +1.729 m, wood .799 mm, no large cinematic escape, held horizontal face-up head +4 cm. Same-board fallback, no hop. Five movies encoded and game/front/side/3q key sheets actually reviewed; artistic/full-speed acceptance remains outstanding. Extended static middle guarding and exposed shark still need work.
- Verifier now tracks first-film actual ownership rather than following a destroyed managed object into a later natural retry. Persists ownership-exit and fails on ownership loss. Earlier `.codex/encounter-v033-timed-slash-water` is NOT a valid single take; do not combine its frames. Native valid-owned take above proved first-film retained through20.807 s.
- **CURRENT v034 trial pending** `.codex/encounter-v034-actual-blend-anchor-water`, native `VerifyEncounterV006.Verify(true,true,false,true,true,revision)` RPC timed out as expected; native Task CONTINUES. Check verification.json or verification-failure.json before any next mutating Unity task. No support trace. Source now waits for ACTUAL crossfade age in addition to planned pose age before capturing a fixed guard foot. This targets stale walking sole captured on late transition. Unproven until this run finishes. Backup `.codex/encounter-v034/Story1WreckFinale-before-actual-blend-anchor.cs`, new worst-plant diagnostics in preview/plant-diagnostics.json. Unity actual playing.
- Prepared but NOT run dense native source review `Tools/AnimationPipeline/ReviewTimedSlashContinuousV033.cs`, entry Review, produces85 actual60FPS fixed-camera samples for three views under `.codex/encounter-v033/native-continuous`. Encoder `encode_timed_slash_v033.py` makes84-frame/1.4sec continuous movies after all85 samples exist. Run only after full encounter native task finishes. Regular old ReviewTimedSlashV033 points to moved staging folder; use this dense current Resources helper instead.

NEXT: wait actual v034 completion; inspect support/plant/contact metrics and all views, then revise exact diagnosed issue without relaxing tolerances. Run current normal/lowFPS plus physical neighbor hop/fallback and fracture/safe-ownership/island regressions; continue actual character and shark artistry. Overall not complete from one six-contact take.

SAVE mandatory: `.codex/encounter-v006/backups/save.json` authoritative → `C:/Users/pinyu/AppData/LocalLow/DefaultCompany/Dark Brine_Rot/dark-brine-rot-save.json`. STOP Unity then restore and hash before yield. SHA256 `51C5630337B45FF9F73A52216E0539D70F72D6D985DB05D2BA07FAD7E173B214`. Latest pre-v034 restore/hash matched; native game is now running and must be stopped/restored again. Unity6000.3.23f1 Pipeline7800, autoplay may need a second request after a delayed stopped status, portable Blender5.2.2/Python paths unchanged. No pending task besides v034; no observers.

## LATEST — 2026-10-10 04:13 Vancouver (all native tasks finished; safe stopped state)

**Overall INCOMPLETE; retain automation.** This section supersedes all earlier RUNNING/pending sections. Solo; no questions, commit or push. Original walk/run/roll and proven v022 small-board leg-paddles/control unchanged. Detailed findings appended in review-v029.md.

### Active now and proven limitations

- Active cinematic Slash/Miss now **v036**, all other cinematic Guard/Parry/Brace remain v027. Active shark still v023 Bite/v019 Tail/ShipSmash; **v037 shark is STAGED ONLY**. Ordinary attack controller/Avatar/legacy locomotion untouched.
- v034 full water `.codex/encounter-v034-actual-blend-anchor-water/verification.json` PASS all six genuine contacts, support .985 mm/slide1.044 mm/chest+1.433 m, wood .913 mm, real timber break, held face-up head+4 cm/pause/unarmed unconscious island bridge. Five movies encoded; all four key sheets inspected. This remains SAME-BOARD fallback, not final art/reliable scatter acceptance. v034 normal/timer first take sank and safely released ownership at1.800 s with health100; ownership-exit proves controls restored.
- Correction: v034 plant gate alone did not use actual crossfade time because Animate still set blendStart=plannedStart. **v035 fixes blendStart=Elapsed**, retaining authored poseStart/cues. Backup .codex/encounter-v035/Story1WreckFinale-before-crossfade-clock.cs. Actual water support/slide .838/1.054 mm; lowFPS1.092/.930 mm. Water reversal missed1.758 m; lowFPS first parry4.843 m/reversal1.032 m. LowFPS553 native frames/9.53 FPS/104.93 ms mean, settings restored. Both take movies encoded; game/side sheets inspected, front/3q/full-speed art pending. No tolerance relaxation.
- **v036 actual Blender windup revision**, `ArtSource/Encounter/v036/sahur-spaced-slash-v036.blend`, script `space_windup_v036.py`. Dense actual native v033 review exposed47.63-degree single-frame overhead shaft rotation at60 FPS. Earlier shoulder/elbow preparation in a new original-rig Action reduces peak to27.46 degrees and peak hand speed44.68→28.14 m/s. Native original-Avatar510 actual frames/three fixed views, near-cue hand/shaft identical; all post-contact sampled hand/feet/hips match within .000000270 m. Three-view dense sheets through .983 s inspected; unchanged recovery compared against separately reviewed v033. Six1.4 s/60FPS movies under .codex/encounter-v036/native-continuous. Native blocking review accepts full trial only; not final art. InstallV036 backs up prior definition under Resources/v036 and maps Slash/Miss only. Native reviewer handles staged OR installed assets.
- **v038 bounded water entry ACTIVE**, backup .codex/encounter-v038/Story1WreckFinale-before-bounded-water-entry.cs. A v036 first take exposed indefinite water entry: unready real9.68x25.63x16.92 m tall hull extended lead-in to287.5 s. Readonly `.codex/encounter-v037/entry-inspection.json`; manually released ONLY that stuck test through existing missed-finale, root0/health20/controls restored (.codex/encounter-v038/stalled-test-release.json). Not an automatic production proof.
- Production v038 now searches real thin/flat/nearby water support or releases retained initial pose, and bounds actual unready-board settlement wait to one simulation second. **Native .codex/encounter-v038/bounded-water-entry.json PASS**, real original deck isolated only in fixture, valid swimming admission then selected support held8 m above water: automatically released after1.0071 simulation seconds, movement/attack/CC restored, health100. First fixture lacked a ready selected edge; corrected fixture stabilizes its actual edge before later invalidation. Tool has failure JSON/finally cleanup; no pending invocation.

### Latest full CURRENT failed take — next priority

`.codex/encounter-v038-bounded-entry-water` completed11:10:25 UTC. Five genuine contacts: all three offensive counters and both tails; **first parry misses4.207 m against actual crowded timber**. No false hit. Slide1.245 mm/no film large escape/wood0, but **foot metric Infinity during entry at-3.087 s and chest1.084 m underwater on first core frame(.00059 s)**. Pre-Advance support safety currently checks FilmTime>=0; inspect the negative→zero crossing before accepting main film. Also inspect actual entry foot ray/pose; do not dismiss Infinity or alter thresholds. First-film ownership retained through legitimate defeat. Five movies encoded, game/side sheets inspected; front/3q and full-speed art remain. No actual neighbor hop.

Next: fix actual entry/first-film support transition and lower/rework the genuine first parry approach/weapon pose so the shark can reach it around real wood. Inspect native Parry source/held-shaft timing before speculative coordinates (v030 coordinate audit already PASS). Improve same-board variant's extended static guarding with authored supported evasion; prove real reachable neighbor hop without moving debris. Retest current normal/timer/water/HP/lowFPS/pause/ownership/island and inspect continuous all-view movies. Single historical pass is insufficient.

### New shark source prepared; do not activate blindly

`ArtSource/Encounter/v037/shark-lift-v037.blend`, script `polish_shark_lift_v037.py`, original v019 corrected skin/16 bone rest matrices/history retained. Actual rear spine/base/mid lift increased moderately, sequential delays retained; Body/head level. .65 s source tail-tip rise3.805→4.490 m (Tail),3.892→4.546 m (ShipSmash). Both7-pose actual source mesh front/side/3q sheets inspected (.codex/encounter-v037/previews). 1.5 s/60FPS/.65 cue unchanged. Both one-Action FBXs exported and now **Assets/AnimationStaging/CH1_SharkTail_v037 and CH1_SharkShipSmash_v037**: native valid Generic Avatars,60FPS/1.50000012s/frames1–91/scale1/rootTralalero_Rig/Body/170 curves. No runtime installation or native skin/calibration yet. Next compare actual native skin/contact with v019, then if accepted trial use versioned backup/calibration; do not rescale mesh-local centimetre contact metadata. Source acceptance is limited to staging, not final art.

### Shutdown/protection state

Unity6000.3.23f1 Pipeline7800 currently **STOPPED**, ready/no compile/domain reload. All native verifiers, bounded-entry fixture, source previews and import tasks finished; no support observer, recording helper or async verifier pending. RPC30s timeout does not cancel native work; latest completion files confirmed. Save restored AFTER stopping and SHA256 verified at04:12: `51C5630337B45FF9F73A52216E0539D70F72D6D985DB05D2BA07FAD7E173B214`. Restore authoritative .codex/encounter-v006/backups/save.json after EVERY later play/stop. Preserve all prior uncommitted project changes and original resources. No commit/push/schedule change; automation remains active. Portable Blender5.2.2 and bundled Python paths unchanged. Many failed raw frames (including long stalled entry) retained as evidence, do not treat them as a valid completed film.

