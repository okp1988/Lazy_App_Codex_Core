# Lazy App

Lazy App runs ADB-based tap, back, and drag workflows for Android devices. No app or root access is needed on the phone.

## Requirements

1. Install ADB, for example at `C:\adb`.
2. Enable USB Debugging or Wireless Debugging on the phone.
3. Connect the device before running a Script or Sequence.

Useful ADB commands:

```text
adb devices
adb connect <IP_ADDRESS>:PORT
adb pair <IP_ADDRESS>:PORT
adb disconnect <IP_ADDRESS>:PORT
adb start-server
adb kill-server
```

`scrcpy` is optional, but useful for mirroring the phone screen while setting coordinates.

## Main Window

- Use the Script/Sequence dropdown to select what to run.
- Click the Script/Sequence dropdown to open a custom picker.
- The first dropdown row is a search box; matching Scripts and Sequences are shown below.
- Selecting an item closes the dropdown, clears the search box, and keeps only the selected item in the main field.
- Scripts display as `[S] NAME`.
- Sequences display as `[Q] NAME`.
- The status dot is green when idle/ready and red while running.
- Offset options are:

```text
-2:y, -1:y, 0, 1:y, 2:y, -2:x, -1:x, 1:x, 2:x
```

The live status panel shows current action, step, cycle, next action, next action time, and estimated end. If ADB or the device is not available, the panel shows an error state instead of relying on a visible log box.

## Config Editor

The config editor uses in-memory changes while open. Switching tabs or selecting another Script/Sequence does not write `config.json`. The file is written only when using `Save All & Close`, confirming save on close, or restoring a backup.

Available tabs:

- Settings: start/stop hotkeys.
- Offset: offset profiles such as `s26` or `s13`.
- Scripts: script info, default offset, action groups, and step rows.
- Sequences: sequence info, default offset, and mixed script/action items.

## Scripts

A Script has:

- Name
- Loop Count (`d`)
- Interval Min (`imin`)
- Interval Max (`imax`)
- Optional default offset
- Action groups saved under `config`

Script names must be unique. Cloned scripts are named with `_copy`, `_copy2`, `_copy3`, and so on. Scripts can also be manually ordered; the main window follows the saved order.

## Sequences

A Sequence can run:

- Script items
- Direct action items

A Sequence cannot contain another Sequence.

Sequence script items store only the script internal ID, so renaming a Script does not break Sequence references.

When a Script is run inside a Sequence:

- The Script's own loop count and interval are ignored.
- The Sequence item's Repeat controls how many times that Script's expanded steps run.
- The Sequence item's Delay Min/Delay Max waits after that script item finishes.

When running a Sequence directly:

- The Sequence loop count controls sequence cycles.
- The Sequence interval min/max waits after each sequence cycle.
- The Sequence total shown in the config editor is one sequence cycle and does not multiply by Sequence loop count.
- The main window expands the whole sequence into planned actions, so current step, next action, and estimated end include repeated Script items.

Deleting a Script that is used by Sequences asks for confirmation and deletes the dependent Sequences only after confirmation.

## Offset Behavior

Scripts and Sequences can optionally bind a default offset. If enabled, selecting that Script/Sequence auto-selects the saved offset in the main window.

Offset profile lookup is based on the runnable name. For example, a script named `ENE_26` uses `s26` if that offset profile exists.

For Sequence script items, offset lookup uses the script item's own script name. Direct action items use the selected Sequence/main offset context.

Only left-click actions apply the offset. Drag and back actions do not.

## Config Files

Lazy App keeps the editable configuration in `config.json` next to the app. The editor provides:

- Open Config Folder
- Backup Config
- Restore Config

Backups are written under the config folder's `backup` directory with timestamped filenames.

## Compact JSON Aliases

The config remains backward compatible with compact aliases:

- `d` = loop count
- `imin` / `imax` = interval min/max
- `s` / `s2` = `[x,y]` / `[x2,y2]`
- `r` = `[randX,randY]`
- `t` = `[sleepMin,sleepMax]`
- `a` = action
- `o` = offset axis

Action aliases:

- `leftclick` = `left`
- `rightclick` / `back` = `right`
- `updrag`, `downdrag`, `leftdrag`, `rightdrag` = `drag`
