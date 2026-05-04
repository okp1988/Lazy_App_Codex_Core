# AGENTS.md

## Project Shape

- This is a single-project Windows Forms app targeting `net8.0-windows` with `UseWindowsForms=true`; the solution contains no separate test project.
- The app is an ADB-backed Android automation runner. `config.json` is the editable runtime script source and is intentionally not copied to build/publish output by the `.csproj`.
- `Form1.cs` owns the main window, global hotkey routing, run/stop state, offset selection, taskbar overlay icons, and opening the config editor.
- `ConfigEditorForm.cs` is a hand-built WinForms editor for the three config categories: `settings`, `offset`, and `scripts`.
- `ScriptConfigRespository.cs` loads, migrates, normalizes, and saves `config.json`; preserve its alias support when changing script/config behavior.
- `ScriptRunner.cs` expands normalized steps into ADB commands and sleeps; it supports an `ADB OFF` path where commands are skipped but statuses/timing still update.
- `AdbShellController.cs` shells out to `C:\adb\adb.exe` by default and works in physical device-pixel coordinates.
- `AppLogger.cs` writes daily logs under `AppContext.BaseDirectory\logs` and deletes logs older than 7 days at startup.

## Config Rules

- `config.json` supports both top-level legacy scripts and the current `{ settings, offset, scripts }` shape; repository loading skips those category keys when scanning scripts.
- Settings are migrated from `hotkeyStartStopToggle` to `hotkeyStart`; do not reintroduce the old setting as the canonical key.
- Offset profiles are named `s<number>` and selected by matching digits in the script name; fallback keys include `offsetX`/`offsetY`, `ox`/`oy`, `x`/`y`, and `s`.
- Script aliases are meaningful API: `d`, `imin`, `imax`, `i`, `config`, `steps`, nested `steps` with `repeat`/`rep`, `a`, `s`, `s2`, `p`, `p2`, `r`, `t`, and `o` must remain compatible unless intentionally migrated.
- Runtime action normalization maps `left` to `leftclick`, `right` to `rightclick`, and leaves directional drag names usable; unknown actions are logged and skipped.
- Only left-click steps consume the selected UI offset. A per-step `offset`/`o` value of `x` or `y` overrides the axis selected in the main window.
- The config editor currently writes compact script output (`d`, `imin`, `imax`, `config`, `a`, `s`, `s2`, `r`, `t`) and only exposes `left`, `right`, and `drag` in its grid.

## Run Behavior

- Start/stop is cancellation-token based. `StopRunAsync` cancels `_runCts` and waits for `_runTask`; avoid fire-and-forget script execution paths.
- `Duration <= 0` means run indefinitely. Positive duration is a loop count, not seconds.
- Step sleeps and loop interval sleeps are randomized inclusively; inverted min/max values are swapped in `ScriptRunner.RandomBetween`.
- Drag steps use `s2`/`scrX2`/`scrY2` when supplied. Without an explicit end point, directional drag aliases derive the end point from `RandX`/`RandY`.
- Global hotkeys are unregistered when the window is minimized and re-registered when restored/activated; this behavior is logged in the UI log and warning log.
- If start and stop hotkeys are the same, only the primary hotkey is registered and it toggles start/stop.

## Build and Run

- Restore packages: `dotnet restore Lazy_App_Codex_Core.sln`
- Build debug/default: `dotnet build Lazy_App_Codex_Core.sln`
- Build release like CI: `dotnet build Lazy_App_Codex_Core.sln --configuration Release --no-restore`
- Run the WinForms app locally: `dotnet run --project Lazy_App_Codex_Core.csproj`
- CI uses `.github/workflows/build.yml` on `windows-latest` with `actions/setup-dotnet@v4` and `dotnet-version: 8.0.x`.
- No automated test command is detectable in this repository; verify changes with `dotnet build` and manual WinForms/ADB checks.

## Local Runtime Assumptions

- ADB is expected at `C:\adb\adb.exe` unless code is changed to pass a different path.
- Wireless ADB setup is manual; README documents `adb pair`, `adb connect`, `adb devices`, `adb disconnect`, `adb start-server`, and `adb kill-server`.
- `config.json` in the repo is the file loaded by `Form1` via `new ScriptConfigRepository("config.json")`; changing the working directory changes which config file is used.
- The app can be used without executing ADB actions when `IsAdbActionEnabled` is false, but the UI still plans steps and waits through configured sleeps.
