# v065–v069 targeted animation continuation — incomplete

v064 cinematic throw orientation correction remains active. Original light
Combo1/2/3 appearance at rest and mobile attack/charge behavior from v062 remain.

v065 diagnostic waits use simulation time during fixed-rate offline review,
while normal real-time benchmarks still use wall-clock waits. Fixed fracture
seeds alone do not establish identical layouts: physics advances while budgeted
entry planning runs, so CPU speed still affects the eventual layout. Do not credit
contact changes across unmatched layouts to one revision.

One v065 fixed-clock native run completed all six real contacts, with foot
error1.136mm/slide1.261mm (native-profile-v065-fixed-clock.json). Another seed
failed the reversal tail by10.7878m (native-profile-v065-seed13.json). Inspection
of support-contact-v065.json shows a roughly2cm predicted-vs-actual parry marker
difference near1.184s in the passing run; the suspected planted-anchor cache bug
is not proven and was not patched speculatively.

v066 adds real deeper/sideway swimming arc candidates around submerged spars.
Native anatomy still scores them and the solid solver still owns final movement.
The immutable combined contact/cue schedule is cached once per encounter, removing
the per-frame LINQ/hash-set/sort work. No hitboxes or contact tolerance are enlarged.

v067 commits a short180ms bone transition only for actual rendered shark poses,
after temporary planning samples and before solid/contact evaluation. This prevents
abrupt Breach→Tail, Bite→Recoil and Smash→Swim pose changes. It retains exact
planning samples and original settled animation poses. Native isolated original-rig
verification (shark-transition-v067.json) passes: all three joins0degrees/0metres,
settled pose0degrees from the native clip. This does not prove final artistic quality.

native-paired-v067 completed625frames/full20.8s across four views. Four movies and
contact-sheet.jpg were encoded; contact sheet and selected full-size game frames
were inspected. Four hero and one shark contact verified; reversal tail still misses
by1.22243142m. Final strike reached0.0343653m and defeat completed. Foot error1.679mm,
slide1.495mm. Zero safe-neighbor hops, one breach. No fake stagger was generated.
Foreground timber still occludes some fixed side views; the shark appears overly
exposed during some elevated bites. Full normal-speed movies, true-neighbor jump
and final shore regressions remain unaccepted. This capture is not FPS evidence.

v068 source head-reach trial was reviewed and REJECTED before import: the extra
twelve-degree Head bend creates a sharp neck/gill fold. Its original sixteen-bone
versioned source is retained, inactive. v069 reduces the extra Head bend to six
degrees. Three actual source views were inspected and accepted only for isolated
staging; rig/bind, jaw closure, .52s impact and1.3s duration remain. All other local
rotation channels remain exact. Native original-rig v069 import passes seven sample
times: unchanged rotations0degrees, position error<2micrometres in the original
small prefab, nose rise0.007277m before runtime scaling. This is not proof that all
bites remain adequately submerged in actual random layouts.

v069 is an active integration trial in EncounterShark.controller's Bite_Lunge.
PreviousEncounterShark.controller in Resources/Encounter/v069 preserves v057.
Source v068 was not activated; neither original meshes, rig nor Avatar was replaced.
InstallBiteTrialV069 must not overwrite its existing backup. Native real-time
1080p original-Editor regression v069-normal is running; inspect its result before
any further runtime mutation. Final performance/contacts/art remain incomplete.

Save protection changed: after our v067 capture was stopped/restored, a separate
Play session entered the island and subsequently stopped. Its newer save was
preserved as backups/save-user-after-play-052419.json with SHA256
AB575DF9484F0327E8FC7AD7B7BD9B123ADB85D13F4E9A627F3F40A38D1EC0F3.
This newer save supersedes the old9BDCCB checkpoint. Never restore the old file
over newer user progress. Stop and confirm our current diagnostic isPlaying=false
before restoring this newer backup. Preserve dirty scenes, no commits/pushes.
