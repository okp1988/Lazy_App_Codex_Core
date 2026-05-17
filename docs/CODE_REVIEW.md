# Code Review

Review date: 2026-05-18

Scope: current working tree for the Windows Forms ADB automation runner.

## Summary

The project builds successfully when using a separate output path while a local app instance is running. The current code is coherent overall: config migration, the editor model, sequence support, primary/backup hotkey routing, ADB status dots, cycle enforcement, and cancellation-based run/stop behavior are all in place.

The main remaining risks are behavioral edge cases in action normalization and legacy directional drags. ADB tracking exposes ready device serials to the main Device dropdown so runs can target the selected device, and `settings.devices` stores friendly device names plus synced manufacturer/model metadata.

Recent changes:

- Scripts and Sequences now support `emin`, an enforced minimum cycle time capped by max cycle time.
- Settings now support optional backup hotkeys through `hotkeyBackupStart` and `hotkeyBackupStop`.
- The hotkey dot is green for primary registration, yellow for backup registration, and red when no hotkey is registered.

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
```

Result: separate-output build succeeded with 0 warnings and 0 errors after sandbox escalation for local Windows SDK access. Normal output may be blocked when `Lazy App.exe` is already running and locking `bin`.
