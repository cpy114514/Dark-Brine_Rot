# Freestyle swimming source

Motion: Carnegie Mellon University Graphics Lab Motion Capture Database, subject 125, trial 06, **Free Style** (120 fps).

- Original listing: http://mocap.cs.cmu.edu/search.php?subjectnumber=125
- Original usage terms: http://mocap.cs.cmu.edu/
- BVH conversion: Bruce Hahne, cgspeed MotionBuilder-friendly CMU release (2010).
- Download mirror: https://github.com/Shriinivas/cmubvh/blob/main/Sequence-113-128/125/Data/125_06.zip

CMU permits inclusion in commercially sold products, but prohibits reselling the motion data directly, including converted data. Bruce Hahne places no additional restrictions on the BVH conversion. The fetched original terms are preserved in `cmu-license-source.html`; the conversion documentation is in `READMEFIRST.txt`.

Acknowledgment requested by CMU:

> The data used in this project was obtained from mocap.cs.cmu.edu.
> The database was created with funding from NSF EIA-0196217.

`Tools/ImportSahurFreestyle.cs` retargets the original BVH to a Unity humanoid clip. It uses source frames 3348–3554, removes translation and heading drift, closes the loop seam, aligns the body height with the existing swim, and sets a 1.38-second stroke cadence. The supported knees in the studio capture are adapted to a small alternating flutter kick. Output: `Assets/Game/Prefabs/Characters/Sahur/Animations/Gameplay/SahurFastFreestyle.anim`.
