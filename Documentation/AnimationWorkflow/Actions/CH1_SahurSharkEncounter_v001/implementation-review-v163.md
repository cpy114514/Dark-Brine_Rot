# v163 — bounded escape coverage and redundant search removal

Whole objective remains incomplete. Source compilation passed. Native164 finite-radius branch passed its targeted diagnostic; whole-film regression still FAILS in this dense layout.

## Current production changes

v162 avoids repeating an identical failed retained-pose/radius search inside one synchronous ResolvePose call. No physics step, retained-bone change or wreck snapshot rebuild occurs between these calls; a previous failure remains a failure. Successful searches keep the original pose-restoration path. Original collision and movement limits are unchanged.

v163 fixes incomplete coverage at the finite escape radius. Previously a0.55m movement allowance only tested the fixed radii through0.5m;0.75m exceeded the allowance and was skipped. After all original ordered probes fail, the search now tests the original directions at the actual allowance minus0.1mm. Every added candidate still requires all original native anatomical volumes to clear the unchanged3mm tolerance. It cannot accept a shift outside the allowance. This supplies more real bounded escape candidates before the old distant emergency fallback, without moving wood or relaxing geometry. It does not guarantee that every dynamically trapped pose has a feasible solution.

Backups are separate: v162 is source before redundant-search removal; v163 is source before boundary coverage. Original resources/rigs/Avatar/scene/save preserved.

## Independent v161, source v158

Build succeeded18.594s/zero errors/one warning. Actual1080p/DX12/i5-14400F/RTX4060;4534 completed GPU readbacks/zero errors. Natural timer20.3896s. Film20.8059s, THREE hero and TWO shark contacts: first probe missed1.204m, remaining five contacts0/33.995/9.256/44.328/0mm. No fake hit. All active phases had zero frames>50ms, but film1%low36.835FPS/max34.245ms FAIL>=50. Entry preparation21.846s is also unsatisfactory. Different freely evolving layout from157 means no controlled causal FPS claim.

Four large emergency escapes remain: three during playable Fighting and one film11.925s/2.809m. Native residual maximum0.500mm does not excuse whole-body displacement. The new v162/v163 work targets redundant failed searches and unnecessary distant fallback; correctness and runtime impact must be checked honestly. Source158 recent-blocker equivalence is proven in3200 native comparisons, not universal whole-game performance acceptance.

## Native164 actual results

Targeted hypothetical query-volume fixture uses original released mesh parts, temporarily shifts only the collision query shells and restores all shell transforms/bounds in finally; actor rig/root and wood are untouched.24/24 cases found an actually clear placement between the old discrete radii where the old bounded search failed. All returned candidates passed all native anatomy checks; zero old-result mismatches and zero radius excess. This proves the added positive branch, not whole movement acceptance.

Full native164 film20.814s: gaps3.525/0.760/0.009962/0/9.127/0m, two hero/one shark counters; three misses remain and never generate fake contact.625 observed motion samples: maximum residual0.522mm, one travel overrun0.239m (actual1.339m at15.481s) and one recorded emergency escape. Whole film movement/contact acceptance FAILS. Production entry1.301s in this freely evolving case. Editor diagnostic, not independent FPS. Native ended, original Editor actually stopped/clean, diagnostic save164 preserved and AB authority restored.

## Evidence still required

Independent latest-source FPS; robust full-film movement/contact regression across dense layouts, genuine safe086 neighbour hop and normal-speed continuous artistic review. Latest four-view all-contact successful movies remain v146. Settled-board handoff160 passed its explicitly actor-set-up real-mesh fixture, while156 was inconclusive/water-only. Keep those distinct from natural admission proof. Major remaining slow frames in161 include attack planning25.692ms; cold/worst-frame and entry time remain variable. Do not claim completion or delete the continuation task.
