# Eight-second helm and visible shark patrol — v264

This supersedes the 18-second voyage timing in v263. Other tutorial and Cappuccino changes from v263 remain.

- Default playable helm interval is now eight seconds. Both natural impact triggers still respect that interval.
- The shark patrol remains active until cinematic takeover, rather than switching to the legacy bow approach in the middle of the short voyage.
- Initial orbit staging scores a bounded set of safe-radius arcs against the existing camera, real hull raycasts and sail occluders. It prefers open flanks and avoids the ship silhouette. Staging happens once before the first voyage frame; the shark is not teleported to follow the viewer and camera controls remain available.
- The short opening now waterlines the actual animated snout near the surface. The previous static import bounds left the snout submerged even after increasing the nominal exposed-height fraction. Original shark scale, mesh, swim clip and hull clearance radius are retained.
- The separate dramatic breach/crush and three-second club fidget are retained.

Verification artifacts are under `.codex/opening-v264/`. Earlier pilot takes are retained for comparison; the final visible-patrol take is `visible/review.json`. Native frame capture is a functional check, not a performance benchmark. Existing scenes and campaign save are preserved by `Tools/ReviewOpeningV264.cs`.

Final verification: 3,201 native 30 Hz frames; takeover at 8.019994 seconds; three confirmed cinematic contacts and two tutorial hits; final health 70; no runtime errors. Reviewed the default-view patrol at 0, 1, 2, 3, 4, 6 and 7.5 seconds. The surfaced shark is visibly circling along the open left flank before passing behind the ship. Preview: `.codex/opening-v264/visible/eight-second-helm.mp4`. Five acceptance checks passed.

Save audit: the completed unattended pilot continued into First Island and caused a test autosave. The exact pre-test save has been restored; the post-test file is retained as `.codex/opening-v264/after-test-save.json`. The opt-in Editor recording utility now pauses after finishing and cleaning up its recording, preventing that unattended scene transition. Original five-scene workspace restored.
