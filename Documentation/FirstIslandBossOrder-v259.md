# First-island boss prerequisites — v259

Requested progression for the second-island preparation:

1. Defeat Cappuccino Assassino.
2. Nailong becomes available; defeat him.
3. Bombardino becomes available.

## Implementation

`FirstIslandBossProgression` shares one prerequisite rule between health, AI
and the existing objective UI. The original first-island `WorldSaveIdentity`
defeat records are authoritative. Registration retains IDs after a boss object
is disabled or destroyed; no new save format or per-frame scene search is needed.

- Nailong waits in idle and rejects incoming damage until Cappuccino is defeated.
  His direct melee damage entry also rejects attacks while locked.
- Bombardino keeps its patrol, but cannot acquire a target or start an attack
  before both preceding bosses are defeated. Generic `Health` rejects its damage
  while locked, including lethal `Kill()` calls.
- Blocked damage emits no health, hit-reaction or death events and grants no loot.
- The objective selects the next unlocked encounter instead of the nearest boss,
  lists prerequisite reasons, and includes the next boss in the combat lessons.
  Near a locked boss it shows the complete sequence. Nailong's existing world
  health label explains his prerequisite, then returns to HP after unlocking.
- Names use the existing localization consistently (the current English
  `enemy.name` entry is "Karen Fairy").
- Outside `Assets/Scenes/First Island/`, the policy permits existing previews,
  combat test scenes and later-island reuse of these boss scripts.

## Saves

Existing world snapshots already serialize defeat IDs. Restore additionally
imports confirmed defeated entries from older objective-only snapshots into
world changes, without replaying damage/death/reward events. A saved stage alone
does not grant defeats. Out-of-order historical defeats are retained, but the
aircraft still requires both earlier defeats. Respawning does not reset progress.

Canonical identities were audited in the live Editor:

| Boss | Scene | Stable ID |
| --- | --- | --- |
| Cappuccino | Environment | `12a1b157107240fbb1d7512b8cc1d3c9` |
| Nailong | Enemies | `5eb24e3476de4bcebd6d4560ef4f69c5` |
| Bombardino | Enemies | `ab3f6fe8a4e2452181436b44d583c00c` |

## Verification

Compiled and ran native Play Mode checks using the real Enemies scene, its
Nailong/aircraft instances, the existing Cappuccino prefab with its canonical ID,
and an excluded Sahur fixture. An unsaved empty companion scene isolates this
from the user's campaign and the large Environment scene.

22 functional checks passed: fresh locks; normal/charged/lethal damage rejection;
no locked engagement or direct Nailong hit; objective order; real Cappuccino and
Nailong deaths unlocking only the next encounter; actual unlocked engagement and
damage; actual player death/respawn; JSON snapshot roundtrips; all-three completion;
legacy objective-only and out-of-order saves; no duplicate death events; outside
island compatibility; zero runtime errors in the verification.

A fresh Play session loaded the checkpoint JSON from disk and passed 7 further
checks: no stale session cache, defeated Cappuccino restored without death events,
only the earned unlock restored, later bosses alive, correct UI prerequisite text,
objective text fitting its card, and Nailong's world label returning to HP.

Evidence and replay helper:

- `.codex/boss-order-v259/result.json`
- `.codex/boss-order-v259/fresh-session-reload.json`
- `.codex/boss-order-v259/checkpoint-roundtrip.json`
- `Tools/VerifyBossOrderV259.cs`

The first fixture run lacked the player health normally installed by the gameplay
scene; the isolated fixture was completed and the checks rerun. A UI assertion
also initially assumed an English display name of "Nailong"; it was corrected to
the project's existing localized name, and prerequisite labels made consistent.

The existing human save remained byte-identical. Startup scene recovery was
preserved in `Assets/_Recovery/0.unity` and additionally backed up in the evidence
folder. Existing tail-contact work and the user's PlayerSettings change were
retained. No canonical scene or prefab was rewritten, and no VCS submission or
Windows rebuild was part of this request.
