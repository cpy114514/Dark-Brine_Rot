# Sahur diagonal charged strike

Historical asset, currently disconnected. Both charge and release now use
the user's supplied `SahurStandingDownwardCharge.anim`; see its adjacent README.
The description below records how this earlier animation was authored.

Source: the project's existing Mixamo **Sword And Shield Slash** animation,
`Animations/Source/SwordAndShieldSlash_ThreeHit.fbx`, first strike (`SahurSwordCombo1`).

Online library: https://www.mixamo.com/
Official usage information: https://helpx.adobe.com/creative-cloud/faq/mixamo-faq.html

The previous original attack was restored after comparing it with the Quaternius
Universal Animation Library 2 sword strikes. Only the right arm and fingers play
on the charge layer; locomotion, the left arm and the collision root remain under
their existing controllers.

Anticipation adapts the wrist to hold the club up and behind the right shoulder.
The downloaded shoulder, upper arm and elbow motion are retained. The source's
rapid forearm and wrist reversals made the club wobble during the downswing.
Three grip muscles now interpolate smoothly from the held pose to a downward
follow-through at phase 0.82, then remain steady during recovery. Interpolation
uses muscle space to avoid wrapped wrist rotations. Fingers hold Sahur's club.
The hold point is phase 0.47. Raising the weapon takes 0.42 seconds;
damage continues charging to 1.8 seconds independently. Release resumes the same
clip from the currently held phase. The hit window is 0.62–0.84.

Reproduce through Unity Pipeline with `Tools/RestoreSahurDiagonalCharge.cs`.
`Tools/VerifyDiagonalCharge.cs` checks continuous playback, release continuity,
late downward club orientation, wrist velocity, hit timing, stamina and the arm mask.
Its preview runs the actual charge/release path without restarting each animation frame.
