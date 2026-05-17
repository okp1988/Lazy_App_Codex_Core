# Lazy App

Lazy App runs ADB-based tap, back, and drag workflows for Android devices. No app or root access is needed on the phone.

## Documentation

- [Project Design](docs/PROJECT_DESIGN.md)
- [Architecture](docs/ARCHITECTURE.md)
- [Config and Settings](docs/CONFIG_AND_SETTINGS.md)
- [Run and ADB Behavior](docs/RUN_AND_ADB_BEHAVIOR.md)
- [Contributing Guardrails](docs/CONTRIBUTING_GUARDRAILS.md)
- [Current Code Review](docs/CODE_REVIEW.md)

## Requirements

1. Use Windows with the .NET 8 SDK installed.
2. For Visual Studio development, use Visual Studio 2022 version 17.8 or newer, or Visual Studio 2026, with the .NET desktop development workload installed. Visual Studio can read `.vsconfig` from this repository to install the required workload.
3. Install ADB, for example at `C:\adb`.
4. Enable USB Debugging or Wireless Debugging on the phone.
5. Connect the device before running a Script or Sequence.

Open `Lazy_App_Codex_Core.sln` in Visual Studio. The project is an SDK-style Windows Forms app targeting `net8.0-windows`, so no legacy project migration is required. The WinForms designer metadata for `Form1`, `ConfigEditorForm`, `SearchableDropdown`, and `WirelessAdbConnectForm` is stored in the shared project file instead of user-local `.csproj.user` settings.

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
- Opening the Script/Sequence dropdown highlights the current selected item when it is still in the filtered list.
- Selecting an item closes the dropdown, clears the search box, and keeps only the selected item in the main field.
- Scripts display as `[S] NAME`.
- Sequences display as `[Q] NAME`.
- The first small status dot reports global hotkey registration. The second small status dot reports ADB status through a background `adb track-devices` monitor: dark gray means no ADB server, red means server running with no ready device, green means one ready device, and yellow means more than one ready device.
- The Device dropdown lists only currently ready devices from `adb track-devices`. One ready device is auto-selected; when multiple devices are ready, select the device before running. Wi-Fi devices are shown without the port, but ADB commands still use the full serial internally.
- The Pair / Connect button opens the Wireless ADB window for manual `adb pair`, `adb connect`, or ADB server restart. Its Action dropdown contains Pair and Connect, its Device dropdown starts with Manual Input, selecting a saved Wi-Fi device fills the IP address, choosing Manual Input clears IP and Port, and the Port and Pair Code fields accept numbers only. The IP control uses fixed dot separators and validates each IPv4 segment from 0 through 255. The Try button runs the selected Pair or Connect action, and Restart restarts the ADB server.
- Offset options are:

```text
No offset, Y offset up 3 steps, Y offset up 2 steps, Y offset up 1 step,
Y offset down 1 step, Y offset down 2 steps, Y offset down 3 steps,
X offset left 3 steps, X offset left 2 steps, X offset left 1 step,
X offset right 1 step, X offset right 2 steps, X offset right 3 steps
```

The live status panel shows current action, step, cycle, next action, next action time, and estimated end. If ADB or the device is not available, the panel shows an error state instead of relying on a visible log box.

## Config Editor

The config editor uses in-memory changes while open. Switching tabs or selecting another Script/Sequence does not write `config.json`. The file is written only when using `Save All & Close`, confirming save on close, or restoring a backup.

Available tabs:

- Settings: start/stop hotkeys and the tag list used by Scripts and Sequences.
- Devices: saved ADB device names and detected manufacturer/model data.
- Offset: offset profiles such as `s26` or `s13`.
- Scripts: script info, tag, hide-from-main toggle, default offset, action groups, and step rows.
- Sequences: sequence info, tag, default offset, and mixed script/action items.

The Devices tab stores friendly names under `settings.devices`. New connected devices are synced from ADB properties and default to `manufacturer : model`; names can be edited manually. Wi-Fi device keys are stored without the port. Connected devices can be synced, while disconnected saved devices can still be renamed or deleted.

Successful Wireless ADB Connect updates the saved Wi-Fi device `lastSerial` with the current `IP:Port` and refreshes `lastSeen`, then the main ADB monitor refreshes. Pairing only authorizes the computer; connecting still requires the current wireless debugging connect port.

The Scripts and Sequences tabs share a `Track Touch` toggle. It is enabled only when the selected ADB device is ready. While enabled, the editor reads touch ranges per `/dev/input/event*` device, runs `adb shell getevent -l` in the background for the selected device, scales raw touch events from the matching event device into screen coordinates, and shows the latest screen coordinate in the status line. The button turns bright green while active. If the app cannot match a touch range, it warns instead of showing raw values as screen coordinates. The process stops when toggled off, when the selected device is no longer ready, or when the config editor closes.

## Scripts

A Script has:

- Name
- Tag
- Hide from Main toggle
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

Scripts and Sequences may have one tag or a blank tag. The main window tag filter is below the offset selector. `All` is always the first filter option and shows all visible Scripts plus all Sequences. Selecting a configured tag shows entries with that tag plus entries without a tag. Hidden Scripts stay available for Sequence items, but do not appear in the main Script/Sequence dropdown.

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
