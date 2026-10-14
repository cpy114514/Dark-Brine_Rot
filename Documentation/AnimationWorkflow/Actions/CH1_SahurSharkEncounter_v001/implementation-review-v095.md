# v093–v095 continuation — work in progress

Resumed the one-time 2026-10-13 05:30 Vancouver continuation in the original Editor PID 22012. No additional Editor, commits, pushes, save migration or original rig/Avatar replacement.

## Collision diagnosis and conservative rejection

v093 adds separate native profiler scopes for solid sweep, nearest-clear search, retained placement, volume updates, narrow-phase overlap, solved damage and residual penetration. The opt-in development benchmark records these costs; normal gameplay remains unchanged.

Actual original-Editor v093 diagnostic reproduced a 120.788 ms solid solve: 70.156 ms sweep, 66.004 ms overlap (nested), and 50.584 ms escape/50.535 ms nearest-clear search. Other fighting frames reached 116.957 ms. This is diagnostic evidence, not independent FPS acceptance.

v094 adds a conservative segment-versus-expanded-mesh-AABB rejection for capsules before the existing native ComputePenetration calls. It respects actual capsule scale, centre, axis and supplied hypothetical pose. The extra 1 mm broadphase padding can only retain additional candidates; it cannot weaken the native penetration/contact thresholds. Caudal mesh geometry is unchanged. Existing part order, maximum-depth choice, minimum translations and escape ordering remain unchanged.

Native read-only regression in `capsule-bounds-v094.json`: five live poses, 2,061 actual native pair tests (including rotated and translated hypothetical anatomy), 857 rejected pairs, 95 real penetrations, zero false rejections. PASS for this geometry regression; no all-layout or FPS claim.

v094/v094b diagnostics still show failed contacts on some freely evolving layouts. Native v094 has one 1.884 m miss; v094b misses its first two contacts. Neither take is accepted as complete choreography. The v094b shape-snapshot scope measured 6.094 ms in a fighting frame, but that take did not reproduce the earlier 195 ms standalone stall. Do not claim that snapshot rebuilding alone caused all stalls, or compare unmatched wreck layouts as a controlled A/B test.

## Progressive approach search v095

The expensive approach search still evaluates at most four new ranked endpoints per planning pass. If these approach routes are obstructed, subsequent passes continue down the ranking. A previously clear endpoint is re-graded and retained when better, rather than discarded during exploration. New strokes and genuine remaining-stroke rebases reset the cursor. Exact final geometry still controls feedback and damage; no marker, board, collision threshold or hit is fabricated.

Compilation PASS. Development build v095 Succeeded in 23.249 s, zero errors, one warning. Independent 1080p/DX12 seed14 validation is running at this checkpoint; results must be appended after actual completion.

## Remaining

Independent FPS, robust contacts across layouts, genuine new v086 hop on natural neighbouring timber and camera visibility are still incomplete. The previous game contact sheet shows foreground water/wood occlusion despite passing screen bounds. Do not equate screen inclusion with visible action, or label an unobserved normal-speed full film as approved.

Authoritative user save remains the AB575DF9484F0327E8FC7AD7B7BD9B123ADB85D13F4E9A627F3F40A38D1EC0F3 backup `save-user-after-play-052419.json`; restore it only after confirmed stopped diagnostic Play, preserving each diagnostic save separately. Native v093/v094/v094b diagnostic saves are retained separately. No surfing, propulsion or leg paddling was restored.
