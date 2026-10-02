# Native single-handed charged slash

Historical asset, currently disconnected. Charge and release now both use
the user's supplied `../SahurStandingDownwardCharge.anim`.

Source: the existing Quaternius Universal Animation Library 2 FBX
`UAL2_HeavySource.fbx`, `Armature|Sword_Regular_B` and `Sword_Regular_B_Rec`.
CC0; author and license links are in this folder's README.md and License.txt.

The damaging stroke uses source frames 7.5–10 at 30 fps. It plays at 0.35 of
source speed; native recovery plays at 2.4 speed. The combined clip is 0.789 s.
An 80 ms ready-pose lead permits the charge-to-strike blend, and a 40 ms blend
joins the trimmed strike to the native recovery. The right shoulder, arm,
forearm and wrist curves come directly from the imported animations; fingers
are held closed around the club. No hand-authored wrist trajectory is applied.

Charge Windup still samples SahurDiagonalChargedStrike.anim, with its existing
0.42 s raise, phase 0.47 hold and 1.8 s damage charge. Heavy Attack separately
plays SahurNativeDiagonalSlash.anim from the beginning, using the existing
right-arm mask. Its damage window is normalized phase 0.18–0.36. The left arm,
torso and legs remain on locomotion, and the strike applies no root motion.

Tools/InstallNativeChargedSlash.cs imports, builds and installs this animation
through Unity. Its build verifies the damaging arm curves against the native
source (maximum observed difference 0.00000012). Tools/VerifyDiagonalCharge.cs
verifies hold stability, release continuity, damage, hit timing, stamina,
locomotion and cancellation, and renders the actual charge/release flow.
