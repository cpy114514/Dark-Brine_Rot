# First-island flock and diagnostic qualification — Batch021

Continued from Git07ed6d9 / Plasticcs204. No new gameplay defect was confirmed and no production code/asset was changed. The completed batch020 chicken repairs and40% duck enlargement remain intact.

## Native world coverage

Tools/WorldFlock021.cs loads the actual first-island Environment/Lighting with a fresh immutable save/backup snapshot. It selects dry clear terrain at(42.45,62.64,570.29) on Sunset Island without removing scenery, builds the same72×36×72m physics-collider NavMeshSurface volume used by the native encounter, and spawns5 current chicken and3 enlarged duck prefabs into one real ChickenBroFlock. The target is an immune3.7m diagnostic capsule with PlayerHealth, not a full Sahur/narrative session.

During360 sampled native frames (90 each in Realistic,Dark Fairytale,Origami,Monochrome) an orbit camera renders the moving flock and the target relocates twice. All8 remain alive/registered. The maximum simultaneous attack count is2, with0 slot violations,0 ground-miss samples,0 motor-sole penetrations deeper than0.35m and0 exceptions captured during the fixture run. Sampled motor sole clearance ranges0.0508461–0.09234619m. Four actual1280×720 native end-camera PNGs were inspected. These are different times/poses and do not establish temporal flicker absence, exact visual foot contact, UI, full combat or FPS.

Eight actual native paths (one3-corner path and seven2-corner paths) produce the expected next-corner steering direction. Repeated corner property reads were suspected of creating arrays. A separate native in-memory NavMesh control proves repeated getters return the same array reference; real replanning replaces it. The proposed extra production cache is unnecessary and was not implemented.

## Allocation counter limitation

The original raw report contains zeros from GC.GetAllocatedBytesForCurrentThread. Qualification fails: both an actual fresh native corner array and256 managed new byte[64] allocations also report0. This counter is unavailable in this Editor Mono context. The proof record therefore exposes allocation bytes as null/unqualified. No allocation-free, GC reduction or performance-speedup claim is accepted. The first counter-control failure is retained as rejected evidence, not a gameplay failure. Cache identity/invalidation is independently verified and remains valid.

## Safety and continuation

Only an external helper and QA documentation/evidence enter this checkpoint. No original model,clip,rig,material,scene,setting or save is written. The same Editor is stopped, original5 scenes restored clean, both fresh saves unchanged and running marker cleared. Restore returned a30s timeout but completed; state was checked before further action. Temporary native NavMesh data is removed and the pipeline/style/intensity/pixel/time/random state is restored. Existing606 missing plants and unrelated Plastic settings/recovery/other checkouts stay untouched.

Next: qualify uncapped renderScale1 standalone timings and longer all-style movement, then actual Sahur combat/UI/tutorial and precision foot planting. Batch020's small walk-phase toe intersection remains open: this batch measures collision soles, not skinned-mesh contact. Do not repeat the successful flock/cache checks unchanged or interpret raw counter zeros as performance evidence.

Evidence: AUTO_QA/Evidence/batch021/{proof,native-report,allocation-control,final-native-state}.json and4 native PNGs. Checkpoint title: Record first-island flock and allocation counter qualification.
