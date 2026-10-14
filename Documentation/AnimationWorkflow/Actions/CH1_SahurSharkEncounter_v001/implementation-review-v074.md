# v070–v074 physical approach revisions — INCOMPLETE

The latest human instruction is to finish the animation and fix Sahur facing
backwards during the cinematic throw. The v064 import correction is active:
original-rig native head-facing samples and an actual game release frame were
checked. Previous normal left-click attacks v062, original walk/run/roll and the
no-surfing v055 override remain. Original rigs/Avatars/history are preserved.

The v070 native probe showed that the intended tail marker reaches its requested
target, but solid solving can leave the actual root metres away. Cached versus
fresh collision geometry did not establish a different obstruction cost; native
same-time pose reset also passed. Neither unproven hypothesis was patched.

v071 replans the remaining tail wind-up from the actual solved position after a
large correction. v072 bounds this to three attempts per stroke, at least120ms
apart; no late rebase after .5s. Both native normal runs failed contacts and
performance. v072's coarse search on only the rebase frame still resumed the
expensive full search on subsequent frames; its film maximum attack search was
162.26ms. These runs are rejected as whole-stage acceptance.

v073 retains the coarse native-tested search throughout a rebased tail stroke.
The original-Editor normal1080p run completed both real tail contacts and three
hero contacts; the landing counter missed by5.00637m. Two tail replans occurred.
Film mean20.52ms/p99 40.23ms/max146.38ms, maximum attack search20.01ms. Entry had
an unexplained1074.24ms spike outside the recorded attack/entry planning markers.
This remains a failed full regression, and is not standalone performance evidence.

v074 applies actual-root remaining-route planning to bites too. It preserves the
authored .52s bite pose and all six contact windows. Up to three retries per bite
are allowed, at least120ms apart, only before .37s. The native future anatomy is
resampled over the remaining wind-up; the solid solver still owns final position.
The normal1080p run completed both real tail contacts, the first and landing
counter and throw; the probing parry missed by4.74973m. Seven bite and three tail
replans occurred. Film mean20.73ms/p99 49.82ms/max145.27ms; maximum attack72.39ms
and solid38.77ms. Contact/art/performance acceptance remains FAILED, despite the
scoped improvement in reaching the last strikes. Changing CPU timing changes
floating layouts even with the same scatter seed, so these runs are not a
controlled before/after comparison. No enlarged hitboxes or invented reactions.

Native reports are in .codex/opening-optimization-20261012/: native-profile-v071-normal.json,
native-profile-v072-normal.json, native-profile-v073-normal.json,
native-profile-v074-normal.json and matching entry-observer files. Full latest
four-view native-paired-v074 capture and ending-v074 observation are running;
read actual live status before restarting. Fixed capture time is NOT FPS evidence.

Backups before v071 and v074 preserve the source versions. The active original-rig
v069 head-reach trial remains only staged/integration reviewed, not final approved.
Remaining work: dense-layout probing contact, real safe-neighbour hops, body
exposure/art, full continuous review, pause/shore ending and standalone performance.

Use only original Editor PID22012/port7800. Do not open another Editor. Preserve
the newer user save backups/save-user-after-play-052419.json (SHA256 AB575DF9…EC0F3),
never the superseded old9BDCCB backup. Stop/confirm actual isPlaying=false, back up
the diagnostic save, then restore this newer checkpoint. No scene saves, commits
or pushes. Whole task and its continuation automation remain unfinished.
