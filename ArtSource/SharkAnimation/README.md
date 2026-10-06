# Tralalero shark animation source

`tralalero_animated.blend` contains the original shark mesh, its packed original
texture, an anatomical armature and seven editable actions. No replacement
character or external animation is used. Geometry and UVs are retained.

The 16 bones include a body/head, five articulated rear-body/tail segments,
jaw, paired fins, dorsal fin, three shoe feet and two contact markers. Smooth
vertex weights keep the head readable and move the rear body, fins and feet
together. The rig uses Euler rotation curves baked at 60 fps, with no world-space
travel; Unity's existing story/encounter code owns navigation and collision.

| Action | Length | Use |
| --- | --- | --- |
| Swim_Cruise | 1.6 s, looping | Normal swimming |
| Swim_Fast | 1.0 s, looping | Faster pursuit |
| Tail_Strike | 1.15 s | Windup, tail sweep, recovery |
| Ship_Smash | 1.15 s | Stronger ship and finishing strikes |
| Bite_Lunge | 1.1 s | Open jaw, lunge, snap and recover |
| Hit_Recoil | 0.8 s | Flinch, counter-twist and settle |
| Threat | 1.8 s | Telegraph and final threatening pose |

Tail contact is authored at 0.65 s and bite contact at 0.45 s. The Unity importer
samples the tail marker from both actual strike clips to align the rig's contact
with the ship, warning area and cinematic target. It normalizes the imported
visual to the original mesh coordinates and retains the original game materials.

Rebuild using Blender in background with `Tools/RunBlenderShark.py -- --build`.
Then run `ImportBlenderSharkAnimation.Setup` through Unity Pipeline in Edit Mode.
The generated FBX, generic controller, rig prefab and contact data live in
`Assets/Resources/SharkAnimation`. Original character FBX and prefab are preserved.

`VerifyBlenderSharkRig` checks loop poses, valid skinned vertices, jaw/tail motion,
contact samples and runtime playback. `VerifyStorySharkLiveliness` checks natural
pursuit and collision. Existing wreck battle/finale verifiers check real weapon
hits, boss threshold, lethal finish and unconscious island arrival.
