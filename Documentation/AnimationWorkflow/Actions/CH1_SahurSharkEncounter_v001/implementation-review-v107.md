# v105–v107 — ongoing physical validation

Native v105 completed: first five genuine contacts PASS [0,0,0,0,0.027179], final tail miss0.517779m caused honest exhaustion. Native residual max2.938mm, no large escape records across any phase, 1000 articulated advances/13 partial, no genuine v086 neighbour hop. Staging all-swim-cycle clear flag was false in this dense evolving layout; default setback2m retained, actual admission delay0.0333s. Do not claim that staging selection alone solved all-layout contact.

v106 gathers cheap supported-edge geometry in2ms slices, sorts actual clearance/walk/wave scores before costly pose tests. Native v106 instead found no valid supported combat surface in its different layout: one standing-board attempt, five candidate records, best wood0.0005570661/pose9.149128, preparation12.078s, honest exhaustion instead of fake setup. No main-film acceptance. Preserve this failure, rather than presenting its67 ending samples as a full film.

v107 corrects a ranking defect in the sliced search: a geometrically prettier blocked candidate could stay the weighted-score best and prevent acceptance of a later actually clear candidate. Clear validity now takes precedence, then score ranks candidates in the same validity class. This is a source-level correctness repair; the preceding v106 failure is not proof that this defect caused every rejected layout.

v105 also exposed a frame movement issue at fallback/cue18.6: observed root moved1.390m in a frame whose existing diagnostic travel allowance was1.10m. The solver runs manually at crossed contact cues and again in LateUpdate; it previously granted the full frame allowance to every call. v107 shares remaining translation/rotation allowance across all calls within that actual Time.frameCount, including bounded correction margin. Real moving-wreck cases can still require an exceptional native escape; keep those explicitly reported, never hide them.

Compilation PASS. Original Editor native v107 benchmark plus actual solved-pose observer started. Outputs pending native-profile-v107.json, pose-motion-v107.json, solid-stages-v107.json. No screenshot capture/Blender/Player currently running; no Asset edits until this test completes.

v103 four encoded movies and contact sheet now exist,625frames/native/30recordedFPS/60sourceFPS. Source106/107 changes have not yet received a new four-view review or independent FPS validation. Latest independent evidence remains v102 (main film1%low44.500FPS/max26.256ms, five contacts pass/reversal misses, other opening phases still >50ms stalls).

Original stopped/Story1 clean verified before v107; v106 diagnostic save separately preserved and authoritative AB575 restored. Original Editor only, no commits/pushes/scene saves. Original rigs/Avatar/locomotion/light attacks/throw064, active tail097 and source history preserved; no surf/paddle/audio changes. Whole objective incomplete.
