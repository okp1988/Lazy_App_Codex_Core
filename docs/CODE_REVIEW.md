# Code Review

Review date: 2026-06-08

Scope: current working tree after the main-window skip picker, countdown progress, action timeline, and fixed layout updates.

## Findings

### Medium: Unknown actions still normalize to left click

Files:

- `ScriptRunner.cs`, `NormalizeAction`
- `ScriptConfigRespository.cs`, action parsing/normalization path

Unknown action names still fall back toward a left-click path instead of staying unknown and becoming a no-op.

Impact: a malformed config typo can perform an unintended tap.

Recommended fix: preserve unknown action names through repository parsing, then have `ScriptRunner` log unknown actions and create a no-op planned step.

### Medium: Directional drag aliases still need an explicit endpoint path

Files:

- `ScriptConfigRespository.cs`, action parsing/normalization path
- `ScriptRunner.cs`, `NormalizeAction` and `GetDragEndPoint`

Directional drag aliases such as `leftdrag`, `rightdrag`, `updrag`, and `downdrag` are documented as runtime-compatible, but the runner only plans generic `drag` and derives no directional endpoint when `s2` is missing.

Impact: legacy directional drag configs may not behave as documented unless they already include an explicit end point.

Recommended fix: preserve the directional alias or store a direction field, then derive `s2` from `RandX`/`RandY` when no explicit endpoint is supplied.

### Low: Invalid hotkey text can fall back to defaults

File:

- `HotKeyManager.cs`, `TryParseHotkey`

Empty hotkey text disables a hotkey, but invalid non-empty text can leave default key/modifier values in place.

Impact: a typo in settings can unexpectedly register a default hotkey.

Recommended fix: return `false` when non-empty input contains no valid key, or validate hotkey text in the config editor before saving.

## Reviewed Changes

- `RunSetControl` now uses the current fixed progress layout: one-set client `606 x 292`, two-set client `1200 x 292`, `RunSetControl` `594 x 284`, content column `410`, action column `184`, and set gap `12`.
- The action column has seven fixed rows: Run, Skip, Offset, Tag, Device, Config, and Pair / Connect, with remaining space assigned to the spacer row.
- `SkipPickerControl` owns skip option display. The closed field stays compact; the popup shows short options and a larger bottom detail area with `Skip:` and `Start:` lines.
- The old inline main-window skip detail label was removed because the popup now carries the explanation.
- Skip options reset to `No Skip` when the selected Script, Sequence, or Run Plan changes, and the selected skip cannot change while running.
- Infinite direct Scripts/Sequences expose only `No Skip`; the final finite loop is not offered as a skip option.
- Run Plan skip consumes the flattened item-repeat order globally.
- `CountdownProgressControl` shows wait countdown progress without resizing the main window.
- `ScriptRunner` now includes timeline and countdown fields in live status updates.
- The normal Offset, Tag, and Device dropdowns use system ComboBox arrows again.

## Handoff Notes

- Keep `RunSetControl.Designer.cs` designer-backed with explicit controls only. Fixed layout and runtime item population belong in `RunSetControl.cs`.
- Keep `SkipPickerControl.cs` and `CountdownProgressControl.cs` tracked with the rest of the run-set UI files.
- For another development platform, treat the current Windows PC layout as the baseline. If the platform needs different DPI/font/screen dimensions, add a guarded layout profile instead of overwriting the baseline constants.
- The Skip popup detail area is now the only skip explanation surface in the main window.

## Verification

Commands run:

```text
dotnet build Lazy_App_Codex_Core.sln --no-restore
git diff --check
```

Result: normal build succeeded with 0 warnings and 0 errors. `git diff --check` was clean except the repository's normal CRLF warning.
