# Shark cinematic upright correction — task 041 / 2026-11-01

Target: the user's screenshot and report that the opening shark repeatedly looks crooked. This is a targeted integration revision of the existing approved encounter, not a new action, model revision or human FINAL approval.

The root was usually unbanked. Pitching the actor toward the counterattack while locally yawing the anatomy nevertheless produced up to 16.735 degrees of world-space head roll. High, oblique close-up cameras also projected a level head's transverse axis diagonally. A zero root Z rotation did not address either cause.

`TralaleroSwimAnimator` now softly stabilizes Body then Head in their actual anatomical frames during surface cinematic poses. Bind-pose axes come from the immutable prefab and mesh bind matrices. Banks through two degrees remain untouched; larger banks smoothly approach four degrees. Forward-axis twist correction retains turning/nodding, and runs before the existing nose placement, posed-mesh separation and real contact tests. Breach, Threat, Ship_Smash and LeapCrush preserve their authored banking. No LateUpdate-only visual correction hides a different collision pose. Cache allocation happens once; the correction performs constant work on two bones.

`Story1WreckBattle.Redesign` lowers the head-shake, feint and tail-preparation shots 38–40 toward their focal eye line. Existing FOV, action order, timing, original scale, tutorial controls and physical wood/contact ownership remain. `OpeningTailReview` adds opt-in anatomical world-roll, projected transverse-axis slope and facing diagnostics; it still never boots in ordinary play.

## Accepted targeted revision

Evidence: `.codex/shark041/after-stabilized/`.

- Existing Editor compiled successfully. Full native 30 Hz capture: 3,044 consecutive frames, eight-second helm included, real virtual-controller tutorial input, both confirmed tutorial hits, all three native cinematic contacts, 70 remaining health and zero recorder errors.
- Console inspection retained four `Converting invalid MinMaxAABB` assertions from `BrineVfxGeometry.ContactWake` initialization. That effects source was not changed by this task; the recorder's zero-error result does not mean the whole project's console is clean.
- Full pursuit/duel maximum measured head world roll: **3.585 degrees**, versus **16.735 degrees** in the original baseline audit. The camera-only intermediate take still reached 13.385 degrees; correcting local pose interpolation alone also failed to address the underlying pitched-parent/local-yaw combination. Those takes are retained rather than presented as accepted results.
- Shots 38–40: 109 frames; 101 front-facing frames have projected transverse-axis slope at most **2.763 degrees**. This is a skeletal-frame metric, not a claim that the shaped forehead silhouette is horizontal in every perspective. The large slope extrema during the departing tail turn are back/edge views and are not front-facing quality measurements.
- Independent posed-skin/wood/hero audit: 76 samples, minimum hero-to-shark gap 0.003120 m and no sampled shark/wood triangle intersections. Counts and sampled gaps do not certify every possible floating-wood layout.
- Isolated production-sampler regression at 15/30/60 Hz, with a 40-degree pitched actor and bite/recoil transitions: maximum Body/Head world roll 3.618/3.637/3.640 degrees. The authored 12.000-degree breach bank is unchanged. This is a low-rate pose test, not a full low-rate gameplay or hardware performance benchmark.
- Actual contact events: overhead at Duel 6.177 s, thrown stick 16.810 s, tail 21.610 s. Contact feedback still requires measured native contact. The counter's placement-dependent late contact is an existing broader choreography limitation, not newly certified timing polish.

Review method: inspected front and both three-quarter native rig renders, the full stage contact sheets, closely spaced recoil frames and 0.2-second-spaced incoming/head-shake/feint/tail/outgoing frames. The continuous silent `SharkFight.mp4` and frame/clock metadata are retained. No claim of watching the whole movie at normal speed, audio review or human final approval.

`BeforeAfter.jpg` compares corresponding narrative times from separate live runs; freely floating wreck layouts differ. `Proof.jpg` is a smaller native-frame comparison. No animation frames were synthesized.

## Provenance and restoration

Fresh pre-task source revisions: `.codex/shark041/source-before/`. Final source bytes and SHA-256 manifest: `.codex/shark041/source-after/` and `source-sha256.json`. Original animation clips, FBXs, rig, Avatar, materials and source scenes were not rewritten. Rejected recordings remain in `before`, `after-eye-line` and `final` (the latter is the rejected interpolation experiment; **after-stabilized is the accepted take**).

The original clean Story1 scene was restored, stopped and unpaused. Physical keyboard/mouse are enabled and synthetic replay devices removed. The fresh human save and its backups were byte-identical before restoration; SHA-256 comparison is retained in `restore-proof.json`. No new Editor, Player build, commit or push. Other pending grass, guidance, effects, controller and character work is preserved.
