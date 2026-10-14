# v166 — experimental oriented-part broadphase REJECTED

Whole task incomplete. Source compiled, but native167 disproved the proposed rejection assumption.4000 actual original-convex-mesh pairs/1664 native positive penetrations/959 extra rejected pairs included TWO predicted noncontacts with native positive penetration (maximum88.304mm). Test failed. The report did not record whether those two also failed the old broadphase, so cause/old-path classification remains undefined. Do not infer that padding fixes it or re-enable this candidate without better evidence.

Original Editor stopped/clean; candidate source preserved as backups/v166/RejectedOrientedBroadphase.cs. Its exact hash was checked before removing ONLY this experiment and restoring the immediately preceding v163 source. New recompilation requested; inspect status. Diagnostic save167 preserved, AB authority restored. No Player/Blender/capture active; autotick TRUE. Current ACTIVE collision revisions are158/162/163, not166.

## Change

After the existing world-AABB and segment-vs-expanded-world-AABB checks, capsule queries also test the mesh part's oriented local bounding box. Long rotated timber has a large empty world AABB; this extra conservative rejection can avoid PhysX calls in its empty corners. The capsule centre segment is projected onto the part's actual orthonormal axes and clipped against mesh bounds expanded by its original radius plus the same1mm conservative padding. Original native convex geometry still decides every surviving pair. Fin mesh queries retain their old path.

The rejected experiment cached boxes in the same immutable per-frame wreck snapshot, with fallback for non-positive/mirrored scale, zero scale and non-orthogonal/sheared axes. No collider substitution, geometry/tolerance change, actor/wood repositioning or resource/rig/Avatar modification. Backup: backups/v166/Story1SharkWreckContact.cs. Do not call the hypothetical oriented-box assumption proven merely because a box encloses render vertices; actual PhysX equivalence was not established.

## Latest prior-source independent165

Build succeeded20.709s/zero errors/one warning, actual1080p/DX12/4060,2999completed GPU readbacks/zero errors. This case found NO reachable supported combat surface and used the existing honest unconscious exit after19.014s entry preparation. Counters0/0 and all six contact gaps999(unattempted). Its reported Finale59.87FPS covers only the fallback, **not** a complete20.8s fight. Do not present it as contact or full-film performance acceptance. No large escapes in this case. Playable1%low46.205FPS/max45.828ms still below target. Save AB unchanged after Player quit; original Editor autotick restored TRUE.

v160 actual settled-mesh handoff and v159 bounded-original equivalence passed their explicit diagnostics. v164 finite-radius positive branch passed24actual-mesh hypothetical cases, but its full native film had three contact misses and one0.239m motion-budget overrun. Source162/163 remains active; no universal acceptance claim. Review163 contains details. Latest complete four-view all-six-pass movies remain146, final artistic review unobserved.
