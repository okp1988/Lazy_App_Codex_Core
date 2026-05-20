# Code Review

Review date: 2026-05-19

Scope: current working tree for the Windows Forms ADB automation runner.

## Handoff: Main Window RunSetControl UI

This session converted the main run-set UI to a shared `RunSetControl` so Set 1 and Set 2 are built from the same control. The compact layout is now client-area based: one-set client `484 x 224`, two-set client `968 x 224`, and each `RunSetControl` `472 x 216`. The outer window size is derived with `SizeFromClientSize(...)`, so Windows DPI/title-bar differences may add a few outer pixels without squeezing the app contents.

Problems encountered:

- Panel/UserControl conversion initially made Set 2 look different from Set 1 because the repeated layout was still partly hand-built or designer-incompatible.
- Visual Studio 2026 rewrote WinForms designer metadata after opening/rebuilding. It changed autoscale values, removed helper-created status rows from `RunSetControl.Designer.cs`, shrank ComboBox heights, and left the Offset dropdown blank when designer-owned items disappeared.
- Shrinking Device dropdown height affected nearby buttons when row heights and button heights were adjusted together. Config/Pair text clipped when buttons were reduced too far.
- Large whitespace was reduced only after shrinking both the fixed form size and the actual run-set/control dimensions. Small outer size changes alone were barely noticeable.

Current fixes:

- `RunSetControl.Designer.cs` now uses explicit status labels/colon labels, not helper-created status rows.
- `Form1` and `RunSetControl` use `AutoScaleMode.None` for the fixed-size main window.
- `RunSetControl.cs` applies stable runtime layout after `InitializeComponent()` and repopulates `ddlOffset` from `OffsetDisplayOption.All`, so Visual Studio designer rewrites should not blank the Offset dropdown or shrink the control stack at runtime.
- Buttons are kept at `32px` height to avoid text clipping; Offset/tag combos target `28px`; Device combo targets `26px`.
- The action column uses six fixed `34px` rows plus a `12px` bottom spacer row so laptop-scale layouts can keep visible bottom space without stretching the Pair / Connect button.
- `Form1` now fixes `ClientSize` rather than only outer `Size`, which keeps the usable layout consistent between 100% and 125% display scaling.

Laptop-design follow-up:

- Start by preserving the client-size baseline. Take a PC screenshot before changing laptop values, then compare after changes.
- If laptop needs different spacing/sizing, prefer an explicit layout profile selected by DPI/screen/font context rather than replacing the current client-size constants.
- Keep all new run-set files in Git: `RunSetControl.cs`, `RunSetControl.Designer.cs`, and `RunSetControl.resx`.

## Summary

The project builds successfully when using a separate output path while a local app instance is running. The current code is coherent overall: config migration, the editor model, sequence support, primary/secondary hotkey routing, two independent run sets, ADB status dots, cycle enforcement, and cancellation-based run/stop behavior are all in place.

The main remaining risks are behavioral edge cases in action normalization and legacy directional drags. ADB tracking exposes ready device serials to the run-set Device dropdowns so each run can target its selected device, and `settings.devices` stores friendly device names plus synced manufacturer/model metadata.

Recent changes:

- Run Plans are now first-class runnable entries with ordered Script/Sequence items, stable target IDs, per-item repeat counts, main picker support, editor support, and independent run-set cancellation/status behavior.
- Run Plan item repeat overrides the referenced target loop count only for that item while preserving the target's `emin`, `imin`, `imax`, sleeps, offsets, ADB OFF behavior, cancellation, and live status updates.
- Run Plans can be tagged and show total min/max time in the Config Editor.
- Sequences now support Hide from Main while remaining valid Run Plan targets.
- Scripts and Sequences now support `emin`, an enforced minimum cycle time capped by max cycle time.
- Settings support optional secondary Set 2 hotkeys through `hotkeyBackupStart` and `hotkeyBackupStop`.
- Set 2 opens/closes with `Alt+1`; secondary hotkeys register only while Set 2 is open.
- The hotkey dot is gold for both registrations, green for primary-only, blue for secondary-only, and red when no hotkey is registered.
- The taskbar overlay distinguishes Set 1/Set 2 run state and includes a hotkey-registration status identifier.
- The main window uses fixed one-set and two-set client sizes, disables user resize/maximize, and does not resize on Run/Stop actions.

## Findings

### Medium: Unknown actions normalize to left click

Files:

- `ScriptConfigRespository.cs:532`
- `ScriptRunner.cs:330`

Both normalization methods map unknown actions to `left`. This means a typo or future action name can become a tap instead of being logged and skipped.

Impact: a malformed config can perform unintended touches on the device.

Recommended fix: preserve unknown action names through repository parsing, and have `ScriptRunner` log unknown actions and create a no-op planned step.

### Medium: Directional drag aliases are not preserved

Files:

- `ScriptConfigRespository.cs:532`
- `ScriptRunner.cs:247`

The repository maps `leftdrag`, `rightdrag`, `updrag`, and `downdrag` to `drag`, and the runner derives no directional endpoint when `s2` is missing. Without an explicit `s2`, a directional drag alias becomes a swipe from a point to the same point.

Impact: legacy directional drag configs do not behave as documented.

Recommended fix: either preserve the directional alias until planning, or store a normalized direction field. In the runner, derive `s2` from `RandX`/`RandY` when direction aliases are used and no explicit endpoint is supplied.

### Low: Invalid hotkey text silently falls back to defaults

File: `HotKeyManager.cs`

If a hotkey string has no parseable key, `TryParseHotkey` still returns `true`, leaving default key/modifier values in place. Empty text disables a hotkey, but invalid non-empty text enables the default.

Impact: a typo in settings can unexpectedly register `Ctrl+Alt+S` or `Ctrl+Alt+D`.

Recommended fix: return `false` when non-empty input contains no valid key, or make the config editor validate and reject invalid hotkey strings before save.

## Verification

Commands run:

```text
dotnet build Lazy_App_Codex_Core.sln --configuration Release
dotnet build Lazy_App_Codex_Core.sln -p:OutputPath=D:\Misc_Project\Lazy_App_Codex_Core\build_check\
dotnet build Lazy_App_Codex_Core.sln --no-restore -p:OutputPath=build_check\
```

Result: separate-output build succeeded with 0 warnings and 0 errors after sandbox escalation for local Windows SDK access. Normal output may be blocked when `Lazy App.exe` is already running and locking `bin`.
