from pathlib import Path
root=Path(r'D:/indiegameDev/unity/Dark Brine_Rot')
path=root/'Documentation/AnimationWorkflow/Actions/CH1_SahurSharkEncounter_v001/resume-at-0250.md'
old=path.read_text(encoding='utf-8-sig')
current,history=old.split('## HISTORICAL NOTES',1)
current=current.replace('v051 INCOMPLETE','v053 INCOMPLETE',1).replace('Snapshot editor6 PID10644','Snapshot editor7 PID33784')
begin=current.index('Latest benchmark RUNNING')
end=current.index('Completed logs retained',begin)
current=current[:begin]+'''Latest native v051 completed, one real natural-water-entry layout: full20.8156s,
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

''' + current[end:]
current=current.replace('Next: inspect running v051 full contact/entry/camera results;','Next: inspect v053 continuous native capture/contact/entry/camera results;')
path.write_text(current+'## HISTORICAL NOTES'+history,encoding='utf-8')
print(path)
