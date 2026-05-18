# AGENTS.md

## Project Shape

- This is a single-project Windows Forms app targeting `net8.0-windows` with `UseWindowsForms=true`; the solution contains no separate test project.
- The app is an ADB-backed Android automation runner. `config.json` is the editable runtime script source and is intentionally not copied to build/publish output by the `.csproj`.
- `Form1.cs` owns the main window, global hotkey routing, two independent run-slot states, offset selection, taskbar overlay icons, and opening the config editor.
- Main-window status dots are separate: `statusDot` is global hotkey registration, and `adbStatusDot` is ADB server/device status from the background `adb track-devices` monitor. Hotkey status colors are gold for both primary and secondary hotkeys registered, green for primary only, blue for secondary only, and red for no registered hotkey. ADB status colors are dark gray for no server, red for server with no ready device, green for one ready device, and yellow for multiple ready devices.
- The main window supports two run control sets. Set 1 is always visible and uses primary hotkeys. Set 2 is toggled with `Alt+1`, uses secondary/backup hotkeys, and omits Config and Pair / Connect because those actions are shared.
- Set 2 secondary hotkeys are registered only while Set 2 is open and are unregistered when Set 2 closes.
- Each run set has its own Device dropdown populated only from currently ready `adb track-devices` rows. A device selected in one visible/running set must not be selectable in the other set. Hidden, stopped Set 2 must not reserve a device. Wi-Fi devices are displayed without the port, but ADB commands use the full selected serial internally.
- `SearchableDropdown.cs` is the custom main Script/Sequence picker. Its popup owns search text and clears it on close; the main field should only show the selected item. When the popup opens, highlight the current selected item if it is still present in the filtered list.
- `ConfigEditorForm.cs` is a hand-built WinForms editor for the four config categories: `settings`, `offset`, `scripts`, and `sequences`.
- `WirelessAdbConnectForm.cs` is the manual Wireless ADB Pair / Connect helper opened from the main window below Config. It supports Pair and Connect actions, Restart Server, a Device dropdown with Manual Input at index 0, fixed-separator IPv4 entry, numeric-only Port and Pair Code fields, and Title Case display/status text except for user-entered values.
- The Config Editor also exposes a Devices tab backed by `settings.devices`. Device names default to `manufacturer : model`, Wi-Fi device keys are stored without the port, user names are editable, Sync is only enabled for currently connected ready devices, and disconnected saved devices may still be renamed or deleted.
- In `ConfigEditorForm`, new button rows are easy to clip at the bottom. Prefer `TableLayoutPanel` rows with explicit heights and docked buttons over auto-sized or tight `FlowLayoutPanel` rows, and leave enough bottom padding when placing buttons inside scrollable/editor panels.
- `ConfigEditorForm` owns a shared Script/Sequence `Track Touch` toggle. It should only run while the selected ADB device is ready, reads display size plus touch ABS ranges per `/dev/input/event*` device, starts one long-running `adb shell getevent -l` process for the selected device, maps coordinates using the same event device that emitted the touch line, parses `ABS_MT_POSITION_X`/`ABS_MT_POSITION_Y` or `ABS_X`/`ABS_Y`, displays scaled screen coordinates only, warns instead of displaying raw values when mapping fails, gives the active button a strong visible ON style, and must kill the process on toggle-off, loss of selected ADB device readiness, or editor close.
- `ScriptConfigRespository.cs` loads, migrates, normalizes, and saves `config.json`; preserve its alias support when changing script/config behavior.
- `ScriptRunner.cs` expands normalized steps into ADB commands and sleeps; it supports an `ADB OFF` path where commands are skipped but statuses/timing still update.
- `AdbShellController.cs` shells out to `C:\adb\adb.exe` by default and works in physical device-pixel coordinates.
- `AppLogger.cs` writes daily logs under `AppContext.BaseDirectory\logs` and deletes logs older than 7 days at startup.

## Config Rules

- `config.json` supports both top-level legacy scripts and the current `{ settings, offset, scripts, sequences }` shape; repository loading skips those category keys when scanning legacy scripts.
- Settings are migrated from `hotkeyStartStopToggle` to `hotkeyStart`; do not reintroduce the old setting as the canonical key. Backup hotkeys use `hotkeyBackupStart` and `hotkeyBackupStop`, may be blank, and control Set 2 while Set 2 is open. They are registered independently from the primary Set 1 hotkeys.
- Offset profiles are named `s<number>` and selected by matching digits in the script name; fallback keys include `offsetX`/`offsetY`, `ox`/`oy`, `x`/`y`, and `s`.
- Script aliases are meaningful API: `d`, `imin`, `imax`, `i`, `config`, `steps`, nested `steps` with `repeat`/`rep`, `a`, `s`, `s2`, `p`, `p2`, `r`, `t`, and `o` must remain compatible unless intentionally migrated.
- Runtime action normalization maps `left` to `leftclick`, `right` to `rightclick`, and leaves directional drag names usable; unknown actions are logged and skipped.
- Only left-click steps consume the selected UI offset. A per-step `offset`/`o` value of `x` or `y` overrides the axis selected in the main window.
- The config editor currently writes compact script output (`d`, `imin`, `imax`, `emin`, `config`, `a`, `s`, `s2`, `r`, `t`) and only exposes `left`, `right`, and `drag` in its action grids.
- Sequences are first-class config entries. Sequence items may reference scripts by `scriptId` or contain direct actions; sequences must not reference other sequences.
- `settings.tag` is the canonical tag list and may be empty. Scripts and sequences may store one configured `tag` or a blank tag; selecting a tag in the main filter shows that tag plus blank-tag entries. The main window tag filter always includes `All` at index 0 before configured tags.
- `settings.devices` is the canonical device history/name map. It stores `name`, `manufacturer`, `model`, `lastSerial`, and `lastSeen`; automatic sync may fill missing data for new devices but must not overwrite existing manufacturer/model conflicts silently.
- Wireless ADB Connect may create or update a Wi-Fi device entry by IP address. Successful Connect updates `lastSerial` to the current `IP:Port` and refreshes `lastSeen`; Pair success alone does not mean the device is connected.
- Script `hide` controls whether a script appears in the main Script/Sequence dropdown. Hidden scripts still remain valid sequence script items.

## Run Behavior

- Start/stop is cancellation-token based. Each run set owns its own cancellation token and task; `StopRunAsync` cancels the selected set and waits for that set's task. Avoid fire-and-forget script execution paths.
- `Duration <= 0` means run indefinitely. Positive duration is a loop count, not seconds.
- Step sleeps and loop interval sleeps are randomized inclusively; inverted min/max values are swapped in `ScriptRunner.RandomBetween`.
- Script and Sequence `emin` is an optional minimum cycle time in seconds. It cannot exceed the displayed max cycle time; runtime computes a cycle plan before execution and re-randomizes the lowest flexible waits upward until the plan reaches `emin`. If `emin` equals max cycle time, all flexible waits use their maximum values.
- Drag steps use `s2`/`scrX2`/`scrY2` when supplied. Without an explicit end point, directional drag aliases derive the end point from `RandX`/`RandY`.
- Global hotkeys are unregistered when the window is minimized and re-registered when restored/activated; this behavior is logged in the UI log and warning log. Secondary hotkeys are included in registration only while Set 2 is open.
- If active start and stop hotkeys are the same, only one hotkey is registered and it toggles start/stop.
- In-app shortcuts are `Alt+1` for open/close Set 2, `Alt+2` for Config, and `Alt+3` for Pair / Connect. `Esc` stops active runs from the main window, calls the Config close-button flow in Config, and closes Pair / Connect.

## Build and Run

- Restore packages: `dotnet restore Lazy_App_Codex_Core.sln`
- Build debug/default: `dotnet build Lazy_App_Codex_Core.sln`
- Build release like CI: `dotnet build Lazy_App_Codex_Core.sln --configuration Release --no-restore`
- Publish release files locally: `dotnet publish Lazy_App_Codex_Core.csproj --configuration Release --no-restore --output publish`
- Run the WinForms app locally: `dotnet run --project Lazy_App_Codex_Core.csproj`
- CI uses `.github/workflows/build.yml` on `windows-latest` with `actions/setup-dotnet@v4` and `dotnet-version: 8.0.x`.
- Pushes to `main` or `master` replace the single GitHub Release tagged `latest`; the release zip includes publish output plus the repository `config.json`.
- No automated test command is detectable in this repository; verify changes with `dotnet build` and manual WinForms/ADB checks.
- Never update Markdown files unless the user explicitly says to update Markdown/docs in that request. If code changes make docs stale, mention it in the final response instead of editing docs proactively.

## Local Runtime Assumptions

- ADB is expected at `C:\adb\adb.exe` unless code is changed to pass a different path.
- ADB status checks should not block opening Config. First check localhost port `5037`; only start the background `adb track-devices` monitor when the server is already listening. While no server is detected, retry every 30 seconds and also refresh on Run/Config actions. Pressing Run should refresh ADB status even when no script/sequence is selected, then validate selection. Run should trust cached no-device status and prompt immediately; only dark gray should attempt a fresh ADB monitor start/check before showing a message or running. Run starts only when a ready device is selected, and a running script/sequence must stop and notify the user if the selected device disappears.
- If `adb track-devices` starts successfully but does not emit an initial device block before the Run timeout, treat that as red/no ready device rather than dark gray/no server, because the server is known to be running.
- ADB monitor/run gating changes should log decision points with `AppLogger.LogInfo("[ADB] ...")`: trigger source, tracker process state, port check result, `track-devices` output blocks, process exit, and Run allow/block.
- Do not add a separate polling/health-check path to cover `track-devices` bugs. `adbStatusDot` should be driven by the `track-devices` process output close, stderr, or exit events plus the no-server retry timer.
- Wireless ADB setup is manual; README documents `adb pair`, `adb connect`, `adb devices`, `adb disconnect`, `adb start-server`, and `adb kill-server`.
- Pair / Connect is a manual helper for changing wireless debugging ports and restarting the ADB server. Do not add automatic background reconnect loops or port scanning.
- `config.json` in the repo is the file loaded by `Form1` via `new ScriptConfigRepository("config.json")`; changing the working directory changes which config file is used.
- The app can be used without executing ADB actions when `IsAdbActionEnabled` is false, but the UI still plans steps and waits through configured sleeps.
