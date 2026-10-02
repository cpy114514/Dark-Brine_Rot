# Supplied downward charge and attack

Uses the user's `Standing Melee Attack Downward.fbx`, copied unchanged to
`Animations/Source/StandingMeleeAttackDownward.fbx`.
SHA256: FAFB1A38ACA783851F74C10596B2311B2B1A045D384FFFD1674EC006B0607FAC.

The source is 68 frames at 30 fps (2.267 s). The charge plays the beginning
through frame 19.04 (normalized phase 0.28), then freezes at that raised pose.
Raising takes 0.48 s; damage independently charges to maximum over 1.8 s.
On release, Heavy Attack resumes this same clip from the held phase with a
0.09 s blend. Playback eases from 1.0 to 0.8 during the anticipation, stays
at 0.8 through the hit, then eases to 1.6 by phase 0.68 for recovery.
Its hit window is source frames 23.8–29.2 (phase 0.35–0.43).
Early releases continue the remaining anticipation before the downward attack.

SahurStandingDownwardCharge.anim copies the original right shoulder, arm,
forearm and wrist curves. Only finger bends are adapted to grip the club.
Both Charge Windup and Heavy Attack use this asset on the right-arm layer.
There is no separate attack clip, procedural wrist path or attack root motion;
left arm, torso and legs remain on locomotion. Walking, steering and sprinting
continue through both charge and release. Playback speed only affects the
right-arm Heavy Attack state, through the HeavyPlaybackSpeed float parameter.

Reproduce through Unity Pipeline with Tools/ImportUserDownwardCharge.cs:
Import, Build, then Install(0.28, 0.35, 0.43, 0.48, 1.0).
Tools/VerifyUserDownwardCharge.cs compares the retargeted arm against the FBX
at 121 poses: observed joint rotation and hand position errors are both zero.
Tools/VerifyDiagonalCharge.cs checks hold stability, release continuity,
hit timing, damage, stamina, masking and cancellation, and renders the preview.
Tools/VerifyMovingChargedAttack.cs exercises the actual CharacterController
with forward, strafe and sprint input throughout release and recovery.
