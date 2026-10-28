# Held moving charge and Nailong rifle unlock — v261

## Moving charge

The original flat-floor v255 regression still passed on the current source:
895 native 60 Hz frames, including club sprint charge, direction changes, moving
blade charge, partial/full rushes and rifle walk/aim/sprint/fire/reload.

A further native test reproduced the reported lost input: start holding RMB
while moving through a fall/landing. The original press buffer expired before
the character became eligible; keeping RMB down afterwards never started a
charge. `landing-before/result.json` fails at frame 102 after returning to the
floor with the button still held.

`SahurAttack.BufferChargeInput` now honors held RMB once ground charge/attack
eligibility returns, instead of relying only on the original press event. It
clears the intention for UI, unlocked cursor, release, disabled movement,
external movement/control ownership and swimming. The existing aerial, dodge,
stamina, cooldown and animation gates remain. A genuine fall still ends the
current charge; holding RMB through landing can begin a new charge. Grounded
movement and direction changes preserve the existing charge clock.

No clips, Animator assets, controller movement tuning or scene overrides changed.

## Rifle progression

First-island M4A1 acquisition and use now require the existing persistent Nailong
defeat record. Cappuccino's defeat alone is insufficient.

- A shared item policy guards `EquipmentInventory.Collect`, placed pickup and
  dynamic pickup entry points. Rejected pickups retain their objects, inventory
  and world-save identity; no collection event or loot reward is generated.
- Rifle equipment switching and `SahurRifleCombat.CanOperate` enforce the same
  prerequisite, including an already-held rifle if progress is being restored.
- Old save ownership is restored with `RestoreStack` and retained in inventory,
  but a locked saved rifle selection falls back to the stick. Existing rifles
  are not erased. Normal snapshot inventory restoration remains non-acquisitive.
- The existing placed rifle stays in its authored location. Its caption and
  marker indicate the prerequisite until the defeat, then return to collection
  instructions. Equipment details, hotkey hints, objectives and gun guidance
  respect the lock. Nailong does not gain a duplicate automatic rifle drop.
- Other items and outside-island combat previews retain their existing behavior.

## Actual verification

`final-moving-charge/result.json`: 1,165 native frames at 60 Hz, **31 passing
assertions plus one partial/full blade measurement**. Added checks cover held
input across a moving landing and external control/movement handoffs; the earlier
club, blade, real-fall cancellation and rifle input regressions still pass.
Native input events drive the actor; the review does not manually invoke movement
or attack Update. Hardware input is isolated inside the review process.

`rifle-result.json`: **21 passing checks**, using the real Enemies scene, native
Nailong, the existing Cappuccino prefab with its stable ID, an excluded player,
the real M4A1 item and both pickup types. Verified lock rejection without events
or consumed pickups, old ownership preservation, Cappuccino-only rejection,
actual Nailong death, valid pickup/equip/fire after unlock, no repeated reward,
immediate fire rejection during progress reset without losing ammo, snapshot
roundtrip, static pickup persistence and outside-island compatibility. No runtime
errors in this verification.

Evidence: `.codex/moving-charge-rifle-v261/`.
Helpers: `Tools/InspectChargeV261.cs`, `Tools/TestWeaponFlowV261.cs`,
`Tools/VerifyRifleUnlockV261.cs`.

The human Play session was saved and backed up before stopping for compilation.
Stopping the human Play session performed one additional autosave: only
`savedAtUtcTicks` changed; position, health, inventory and world progress were
identical. This occurred before the first test, and the resulting file remained
unchanged during testing. The comparison is retained in `save-audit.json`.
Existing pending
tail contact, boss order, pixel-mode and PlayerSettings edits are retained. Source
changes were made in the Unity project; no Windows rebuild or VCS upload was
requested for this turn.
