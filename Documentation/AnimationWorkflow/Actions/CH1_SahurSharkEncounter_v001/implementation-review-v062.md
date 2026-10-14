# Previous left-mouse attack restored — v062

Latest human steering: continue production and return ordinary left-mouse attacks
to their previous appearance. No-surfing and the original walk/run/roll remain
authoritative. Work uses only the original running Unity Editor.

The original SahurGrounded.controller and active SahurGameplay.controller both
reference SahurSwordCombo1/2/3 from SwordAndShieldSlash_ThreeHit.fbx, at speeds
1.25/1.25/1.55. The recent mobile-light implementation put them on the charge's
single right-arm mask, discarding their original torso and lower-body action.

v062 restores the original whole-body base states for stationary light attacks,
including their existing source travel. When movement begins, the same clip
continues at the current normalized phase on a separate torso/head/arms mask,
and locomotion takes ownership of the legs and displacement. Moving at attack
start selects this layer immediately. Movement, sprint and direction changes
do not restart a strike or its damage window. Charge and heavy release keep
their existing separate right-arm layer and tuning. Twin-blade behavior is unchanged.

The controller before this revision is preserved at
Assets/Resources/Encounter/v062/PreviousSahurGameplay.controller. The original
controller, FBX clips, rig, Avatar and walk/run/roll assets were not overwritten.

Native verification in .codex/opening-optimization-20261012:

- light-animation-v062.json: PASS. Nine samples covering all three stationary
  clips match the original controller exactly across humanoid bone local
  positions/rotations. Three stationary-to-mobile transfers keep hand/arm/chest
  orientation and phase unchanged. This is pose evidence, not full gameplay QA.
- light-input-v062.json: PASS with the prefab's actual PlayerInput left-mouse
  binding. Standing full-body attack, starting W+sprint, changing running direction
  through all three stages, physical movement11.45498m, completion and layer fadeout.
  Phase progressed from.24963434 to.328045 rather than restarting.
- charge-validation.json: updated native input PASS for charge while starting
  sprint, turning and reversing, release, and mobile light. Full charge1.8s,
  first damaging sample.1999054s, release.9004383s. Input capture's join-angle
  sample includes elapsed frame animation; do not use it as a held-pose seam test.
- charge-core-validation.json: updated native isolated timing PASS, held-pose
  join0degrees, monotonic release, first hit.179702759s, release.8836746s.

The four-view original-Editor paired capture native-paired-v062 completed625frames.
It is offline30FPS simulation and cannot establish hardware frame-rate acceptance.
Contact sheet and selected full-size frames were inspected: see review-v062.md.
Six-contact acceptance failed (four hero counters, one shark counter; reversal
tail missed by4.249316m). Four continuous previews were encoded; complete normal
speed artistic review is still pending. v064 later fixes cinematic throw orientation
and trials a raised camera; see implementation-review-v064.md.
v061's previous one-frame capture is invalid; see implementation-review-v061.md.
Opening contact/admission/performance and final artistic review remain incomplete.

No extra Editor, scene save/revert, commit or push. Restore the authoritative save
only after confirmed Play Mode exit. Current diagnostics may be playing; check live
state rather than trusting an older checkpoint.
