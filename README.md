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
5. Connect the device before running a Script, Sequence, or Run Plan.

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

- Use the Script/Sequence/Run Plan dropdown to select what to run.
- Click the Script/Sequence/Run Plan dropdown to open a custom picker.
- The first dropdown row is a search box; matching Scripts, Sequences, and Run Plans are shown below.
- Opening the Script/Sequence/Run Plan dropdown highlights the current selected item when it is still in the filtered list.
- Selecting an item closes the dropdown, clears the search box, and keeps only the selected item in the main field.
- Scripts display as `[S] NAME`.
- Sequences display as `[Q] NAME`.
- Run Plans display as `[P] NAME`.
- The Skip dropdown sits below Run. It must be chosen before pressing Run, is locked while the run is active, and resets to `No Skip` after the run finishes or is stopped. Changing the selected Script, Sequence, or Run Plan also resets Skip to `No Skip`.
- Skip is offered only when there is something meaningful to skip. Infinite direct Scripts/Sequences cannot be skipped, and the final loop is not offered because there would be nothing left to run. For Run Plans, skip counts through the flattened item-repeat order.
- The Skip popup keeps the selected field compact while showing details in the popup. Details use separate `Skip:` and `Start:` lines so long Script/Sequence names remain readable.
- The first small status dot reports global hotkey registration: gold means both primary and secondary hotkeys registered, green means primary only, blue means secondary only, and red means no hotkey is registered. The taskbar overlay repeats this hotkey state with a small status dot, and shows one or two run-set identifiers depending on whether Set 2 is open. The second small status dot reports ADB status through a background `adb track-devices` monitor: dark gray means no ADB server, red means server running with no ready device, green means one ready device, and yellow means more than one ready device.
- `Alt+1` opens or closes Set 2, a second independent run control set. Set 1 uses primary hotkeys. Set 2 uses secondary/backup hotkeys only while Set 2 is open and omits Config and Pair / Connect because those actions are shared. The main window uses fixed one-set and two-set client sizes so 100% and 125% display scaling keep the same usable app layout; users cannot resize or maximize it, and Run/Stop actions must not resize either run set. `Alt+2` opens Config, and `Alt+3` opens Pair / Connect.
- Each run set has its own Device dropdown listing currently ready devices from `adb track-devices`. A device selected in one visible or running set is not selectable in the other set. One ready device is auto-selected when available; Wi-Fi devices are shown without the port, but ADB commands still use the full serial internally.
- The Pair / Connect button opens the Wireless ADB window for manual `adb pair`, `adb connect`, or ADB server restart. Its Action dropdown contains Pair and Connect, its Device dropdown starts with Manual Input, selecting a saved Wi-Fi device fills the IP address, choosing Manual Input clears IP and Port, and the Port and Pair Code fields accept numbers only. The IP control uses fixed dot separators and validates each IPv4 segment from 0 through 255. The Try button runs the selected Pair or Connect action, and Restart restarts the ADB server.
- Offset options are:

```text
No offset, Y offset up 3 steps, Y offset up 2 steps, Y offset up 1 step,
Y offset down 1 step, Y offset down 2 steps, Y offset down 3 steps,
X offset left 3 steps, X offset left 2 steps, X offset left 1 step,
X offset right 1 step, X offset right 2 steps, X offset right 3 steps
```

Each live status panel shows current action, step, cycle, next action, next action time, estimated end, a six-chip action timeline, and a countdown progress bar for the current wait. If ADB or the device is not available, the affected panel shows an error state instead of relying on a visible log box.

## Config Editor

The config editor uses in-memory changes while open. Switching tabs or selecting another Script, Sequence, or Run Plan does not write `config.json`. The file is written only when using `Save All & Close`, confirming save on close, or restoring a backup. Pressing `Esc` calls the same close flow, so unsaved changes still prompt.

Available tabs:

- Settings: primary start/stop hotkeys, optional backup start/stop hotkeys, and the tag list used by Scripts, Sequences, and Run Plans.
- Devices: saved ADB device names and detected manufacturer/model data.
- Offset: offset profiles such as `s26` or `s13`.
- Scripts: script info, tag, hide-from-main toggle, default offset, action groups, and step rows.
- Sequences: sequence info, tag, hide-from-main toggle, default offset, and mixed script/action items.
- Run Plans: ordered Script/Sequence targets with per-item repeat counts.

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
- Enforce Min (`emin`)
- Optional default offset
- Action groups saved under `config`

Enforce Min is a per-cycle minimum time in seconds. It must be less than or equal to the displayed max cycle time. When a cycle plan randomizes below Enforce Min, the runner re-randomizes the lowest flexible waits upward until the planned cycle reaches the enforced time. If Enforce Min equals the max cycle time, all flexible waits use their maximum values.

Script names must be unique. Cloned scripts are named with `_copy`, `_copy2`, `_copy3`, and so on. Scripts can also be manually ordered; the main window follows the saved order.

## Sequences

A Sequence can run:

- Script items
- Direct action items

A Sequence cannot contain another Sequence.

Sequence script items store only the script internal ID, so renaming a Script does not break Sequence references.

Scripts, Sequences, and Run Plans may have one tag or a blank tag. The main window tag filter is below the offset selector. `All` is always the first filter option and shows all visible Scripts, visible Sequences, and Run Plans. Selecting a configured tag shows entries with that tag plus entries without a tag. Hidden Scripts stay available for Sequence items, and hidden Sequences stay available for Run Plan items, but they do not appear in the main Script/Sequence/Run Plan dropdown.

When a Script is run inside a Sequence:

- The Script's own loop count and interval are ignored.
- The Sequence item's Repeat controls how many times that Script's expanded steps run.
- The Sequence item's Delay Min/Delay Max waits after that script item finishes.

When running a Sequence directly:

- The Sequence loop count controls sequence cycles.
- The Sequence interval min/max waits after each sequence cycle.
- The Sequence Enforce Min (`emin`) applies to each sequence cycle and follows the same max-time limit as Scripts.
- The Sequence total shown in the config editor is one sequence cycle and does not multiply by Sequence loop count.
- The main window expands the whole sequence into planned actions, so current step, next action, and estimated end include repeated Script items.

Deleting a Script that is used by Sequences asks for confirmation and deletes the dependent Sequences only after confirmation.

## Run Plans

A Run Plan is a runnable ordered list of existing Scripts and Sequences. It can repeat targets in any order, for example:

```text
Seq A x1
Seq B x1
Seq A x1
Seq B x2
Seq A x1
```

Run Plan item order is preserved exactly. Each item stores:

- Target type: Script or Sequence
- Target ID
- Repeat count

The item repeat count overrides the target's saved loop count only for that Run Plan item. The target's internal cycle behavior still applies: Script and Sequence `emin`, `imin`, `imax`, sleeps, direct Sequence action delays, offsets, ADB OFF mode, live status updates, and cancellation all use the same rules as direct Script/Sequence runs. The Run Plans editor shows total min/max time by adding each referenced target cycle time with item repeats.

Hidden Scripts may be used by Sequences, and hidden Sequences may be used by Run Plans. Run Plans cannot contain another Run Plan.

## Offset Behavior

Scripts and Sequences can optionally bind a default offset. If enabled, selecting that Script/Sequence auto-selects the saved offset in the main window.

Offset profile lookup is based on the runnable name. For example, a script named `ENE_26` uses `s26` if that offset profile exists.

For Sequence script items, offset lookup uses the script item's own script name. Direct action items use the selected Sequence/main offset context.

For Run Plans, Script items use that Script's default offset when enabled, otherwise the run set's selected offset. Sequence items use that Sequence's default offset when enabled, otherwise the run set's selected offset. A Run Plan may switch offsets from item to item, so `Seq A` with X left 1 and `Seq B` with X right 1 keep their own defaults when both appear in the same plan.

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
- `emin` = minimum enforced cycle time
- `s` / `s2` = `[x,y]` / `[x2,y2]`
- `r` = `[randX,randY]`
- `t` = `[sleepMin,sleepMax]`
- `a` = action
- `o` = offset axis

Action aliases:

- `leftclick` = `left`
- `rightclick` / `back` = `right`
- `updrag`, `downdrag`, `leftdrag`, `rightdrag` = `drag`
