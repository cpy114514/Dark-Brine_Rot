# v075–v078 continued animation production — incomplete

The backwards cinematic throw correction v064 and previous original light
attacks v062 remain active and natively verified. No surfing, original walk/run/
roll, mobile single-hand charge, original rigs/Avatars and current saves remain.

v075 attempted to frame a crown measured in an upright baked pose. The actual
native screen-framing diagnostic rejected it: its tracked head-local crown did
not follow retargeted rendered geometry,295/541 frames outside the screen.
That measurement/baking code was removed; do not restore it from the backup.

v076 instead includes the retained character renderers' current world bounds
after camera aim/distance smoothing. All eight corners are included in the lens
calculation, with112% breathing space and85degree maximum lens. No mesh baking,
model replacement, scene save or visibility-hiding workaround. In the actual
game throw frame0268 the entire visible head is now in frame. Native bound
screen test:541frames, zero foot cropping; all vertical bound coordinates stay
within.0655–.9464. Nine frames still have some horizontal AABB corner outside
screen, so strict whole-bound framing test FAILED. Actual geometry clipping in
those frames is not yet observed. Do not claim that the movie is finally approved.

Full native-paired-v076 has625frames/20.8s in game/front/side/three-quarter views.
All six physical contacts passed in this actual floating layout:0,0,0,.040083,
.009018,0metres. Four hero and two shark contacts; foot1.300mm/slide1.435mm.
Zero safe-neighbour hops, so it uses the same-board variant. Movies/contact sheet
encoded. Earlier v074/v075 and normal runs still failed other layouts; this
single passing run does not prove universal contact reliability. No fake hits.

The ending observer was repaired after the v074 diagnostic lost its destroyed
film object before writing. Native ending-v075.json now confirms278rendered
frames, zero upright frames,24paused frames, zero clock drift/paused motion,
stick lost and unconscious unarmed island arrival. Body follows existing waves
(chest range3.109m, max rock11.12degrees, head minimum clearance.04m). Pose and
wave tests PASS in that native run. Broad swell artistic magnitude is not a
standalone artistic approval. Old enforce=true ending tool expects obsolete
three-shark/two-jump counts; do not use those as current acceptance requirements.

v077 body-led bite source used the wrong sign for Body's local X, which is
opposite to Head's local X. It lowered relative nose reach and was REJECTED,
never exported/imported/activated. Keep it as inactive provenance only.

v078 corrects that on a versioned copy of original v057. It authors Body, Head
and Spine_Mid local rotations: a small body wind-up then four-degree lift leads
the smaller four-degree head bend; the rear spine counterbends80ms later and
settles exactly at the ending. Jaw and all other original local rotation channels
are unchanged. Original16bones, parent hierarchy, rest/bind and mesh preserved;
1.3s/60FPS and .52s impact unchanged. This is actual bone animation production.
The head/body relative reach is1.200source units higher than v057, versus.727
in v069. Body peak.904degrees/frame, Head4.407, Spine_Mid1.992.

Actual source side/three-quarter impact and side recovery previews were inspected
and accepted only for FIRST PASS staging. Original-rig native import comparison
seven times PASS: unchanged rotations0degrees, local position differences<=2.211
micrometres, native nose rise.012002m in the original small prefab before runtime
scale. Generic Avatar valid,170curves, original paths,1.30000007s/60FPS.

v078 is an ACTIVE INTEGRATION TRIAL in EncounterShark.controller Bite_Lunge.
Backup Resources/Encounter/v078/PreviousEncounterShark.controller preserves
v069; never rerun installers over it. Native normal1080p gameplay regression is
about to run in the original Editor; inspect actual status/results. Body exposure,
contact reliability and final continuous art acceptance are still pending.

Evidence: .codex/opening-optimization-20261012/native-paired-v075 and-v076,
framing-v075.json/framing-v076.json, ending-v075.json, shark-source-v078,
bite-import-v078.json. Source: ArtSource/Encounter/v078/shark-bite-v078.blend,
source-audit.json, source-review.json and exported FBX/manifest.

Protection: ONLY original EditorPID22012/7800, no new Player/build/Editor, no
commit/push. Newer save AB575DF9…EC0F3 restored after actual Play=false, with
diagnostic saves separately preserved through save-after-v076-capture.json.
Remaining random-layout contact failures, real hop, strict horizontal framing,
full artistic review and independent performance acceptance keep work incomplete.
