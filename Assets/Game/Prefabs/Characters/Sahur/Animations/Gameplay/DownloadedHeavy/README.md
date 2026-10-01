# Sahur charged swing

Downloaded 2026-09-30 from Quaternius's Universal Animation Library 2 Standard.
Author: Quaternius. License: CC0 (see License.txt).
Author page: https://quaternius.com/packs/universalanimationlibrary2.html
Author download: https://opengameart.org/content/universal-animation-library-2
Source FBX SHA256: D4A2DD67BB12BF0C01891BC59EE697E04DB679D26883D30BD937C2F3FB6FEC90

Sword_Regular_A and Sword_Regular_A_Rec are joined through Unity animation curves
into SahurChargedSwing.anim. In-place humanoid retargeting; no animation events.
Windup holds at 37.5% of the strike section, releases from the held time, and then
plays the authored strike and recovery. Damage and stamina remain gameplay-owned.
Both windup and release now run on an override layer masked to the right arm and
right fingers only. Left arm, torso, head and legs stay on base locomotion, with
no charged-strike root motion. Tools/ConfigureRightArmHeavy.cs configures this layer.
Tools/ImportDownloadedHeavy.cs and Tools/ConfigureDownloadedCharge.cs reproduce clips.
