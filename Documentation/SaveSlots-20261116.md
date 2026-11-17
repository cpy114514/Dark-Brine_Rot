# Selectable saves and session exits

The title screen now offers **New Game** and **Load Game** with three save slots.
The pause menu offers **Resume**, **Settings**, **Save Game**, **Quit to Main Menu**
and **Quit to Desktop**. All added UI is English and uses the existing neutral
black-and-white palette. Slot summaries show the island and local save time.

## Save compatibility and behavior

- Slot 1 retains `dark-brine-rot-save.json` and its existing `.bak` file. No migration
  or deletion of a player's previous save is required. Version 1 and 2 remain readable.
- Slots 2 and 3 use separate `dark-brine-rot-save-slot-N.json` files and backups.
- Loading or successfully saving a slot selects it for subsequent autosaves.
  Failed writes do not change the selected slot. Existing capture/restore logic
  for world progress, equipment, health and both islands is retained.
- Saving over an occupied slot, or starting a new game there, requires confirmation.
  New Game archives the replaced files with unique `.previous-*` suffixes before
  beginning the existing Story1 prologue. Back/Escape preserves the slot.
- Both pause-menu exit actions require confirmation. When the current state is
  savable, they save first and cancel the exit on a write failure. During sections
  excluded by the existing save rules, the confirmation warns about unsaved progress.
- Returning to the title screen releases pause and pending continue/autosave state.
  The slot picker confines keyboard/controller navigation to its own buttons.

## Verification

Unity 6000.3.23f1, live Editor, isolated Play sessions:

- Created independent saves, loaded version 1 data, identified a corrupt primary
  and recovered its backup; saving another slot preserved existing slot contents.
- Invoked the actual pause-menu buttons to save with overwrite confirmation,
  return to the title screen, select Load Game, and restore Slot 2's distinct health.
- Cancelled an occupied New Game selection, then confirmed another occupied slot;
  verified its archive, the Story1 scene, unaffected other slots, and subsequent load.
- Held the temporary save file locked to force writes to fail. Both exit and manual
  save retained the current session and original save bytes. The two intentional
  `[Save]` write-failure messages from this check are expected.
- Invoked Quit to Desktop's confirmation and execution in the Editor; it stopped
  Play through the existing Editor branch. The standalone Application.Quit branch
  has not been exercised in a rebuilt executable for this change.
- Inspected actual pause/load/save screenshots. Fixed the slot buttons' initial tint
  so text remains legible on the first paused frame.
- Recompiled successfully. Formal save bytes and the original Editor scene setup
  were verified unchanged after leaving the isolated session.

Re-run with a stopped Editor on Second Island:

1. `unity command run_script --file Assets/Editor/VerifyTwinBladeMovingSession.cs --entry VerifyTwinBladeMovingSession.Prepare`
2. `unity command run_script --file Tools/VerifySaveSlots.cs --entry VerifySaveSlots.Run`
3. Run `VerifySaveSlots.NewGame`, then `VerifySaveSlots.SaveFailure` with the same tool.
4. Run `VerifySaveSlots.Desktop` to stop Play and trigger preservation checks.

Local screenshots and JSON results are under `.codex/save-slots/`. The existing
session guard records preservation results under `.codex/twin-blade-moving-attacks/`.
