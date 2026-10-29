# Neal fairy with turquoise and violet wings

Updated from the GLB you uploaded, `light_fairyfairy_light__neal.glb`. The original low-poly character geometry, UVs, painted texture and pose are preserved. The model has been uniformly scaled to 1.10 m tall and centered for convenient editing.

New wings recreate the main features of your video reference: turquoise membranes, indigo/purple borders and tips, pointed upper wings, narrow side blades, lower petals and long swept tails. This is newly constructed geometry inspired by the recording, rather than the exact Sketchfab wing asset shown in the video.

- `Neal_Fairy_With_Wings.blend`: native project with the original packed texture, editable quad wing panels, editable vein/border curves, studio lights and camera.
- `Neal_Fairy_With_Wings.glb`: portable version with embedded texture, wing vertex colors, transparency and evaluated wing geometry.
- `Neal_Wings_Portrait.png`, `Neal_Wings_Front.png`, `Neal_Wings_Back.png`, `Neal_Wings_Side.png`: renders of the actual model.
- `add_reference_wings.py`: repeatable Blender 4.5 modeling/export script.
- `wing_model_info.json` and `wing_validation.json`: model statistics and file checks.

The wings are fitted to the upper back and grouped under `Wing hinge.L` and `Wing hinge.R` in the `02 Turquoise & violet wings` collection. Rotate these controls to pose the wings. The delivered model is static: the wing-flapping animation visible in your recording has not been included, and the original body has no character rig.

Run the script using Blender 4.5 LTS:

```powershell
& 'C:\path\blender.exe' --background --python 'C:\path\add_reference_wings.py' -- --input 'C:\path\light_fairyfairy_light__neal.glb' --out 'C:\path\new_output_folder' --samples 48
```

Use a new output folder to keep any manual edits. In GLB, curves become meshes. Transparency and color can vary with the viewer's lighting and color management. The original body remains the supplied low-poly model; this edit adds the wings and does not sculpt a replacement character.

## Original character attribution

**Light Fairy★★★Fairy Light => Neal** by **judasben29**, licensed **CC BY 4.0**, according to the source GLB metadata.

- [Original character on Sketchfab](https://sketchfab.com/3d-models/light-fairyfairy-light-neal-e3e2f87d42e84992a3585b1c169baafb)
- [Creative Commons Attribution 4.0](https://creativecommons.org/licenses/by/4.0/)

Changes: added new wing geometry, materials and pose controls; uniformly rescaled/recentered the original character; added a Blender presentation scene. Please retain this attribution when sharing the character or modified model.

Wing style reference: the user-supplied screen recording showing “Fairy Wings - Animated - V03”.
