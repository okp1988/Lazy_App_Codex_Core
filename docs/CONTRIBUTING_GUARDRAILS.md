# Contributing Guardrails

## Must Preserve

- Single-project WinForms shape.
- `net8.0-windows` target.
- `UseWindowsForms=true`.
- `config.json` as editable runtime source.
- `config.json` not copied to build or publish output.
- Backward-compatible config aliases.
- Script aliases listed in `CONFIG_AND_SETTINGS.md`.
- Sequence support as first-class runnable entries.
- Run Plans as first-class runnable entries.
- Hidden Scripts remaining valid in Sequences.
- Hidden Sequences remaining valid in Run Plans.
- Tag filter behavior with `All` at index 0.
- Separate hotkey and ADB status dots.
- Hotkey dot color meanings: gold for both primary and secondary hotkeys, green for primary only, blue for secondary only, red for no registered hotkey.
- Taskbar overlay identifiers: mirror hotkey registration color and show one run-set identifier when only Set 1 is visible, or two identifiers when Set 2 is open.
- ADB monitor color meanings.
- Optional secondary Set 2 hotkeys through `hotkeyBackupStart` and `hotkeyBackupStop`; register them only while Set 2 is open.
- Two independent run sets, with Set 2 opened by `Alt+1` and no duplicate Config or Pair / Connect buttons.
- Fixed one-set and two-set client sizes with user resize and maximize disabled; Run/Stop must not resize or stretch run-set controls.
- The compact progress main-window layout fixes usable client area instead of outer window chrome: one-set client `606 x 292`, two-set client `1200 x 292`, `RunSetControl` `594 x 284`, content column `410`, action column `184`, and set gap `12`. Preserve this profile when adjusting laptop or next-platform design unless an explicit alternate profile is added.
- The main Skip picker is a custom `SkipPickerControl` between Run and Offset. It must reset to `No Skip` when the selected runnable changes, stay disabled while running, keep the closed field compact, and keep the detailed explanation in the popup rather than a separate main-window label.
- The countdown progress bar and six-chip timeline are part of the main run-set layout and should remain visible without resizing the window during Run/Stop.
- Device dropdown exclusion between visible/running run sets so both sets cannot target the same ADB serial.
- Script and Sequence `emin` minimum cycle enforcement.
- Run Plan item repeat overriding only the referenced target's saved loop count.
- Run Plans preserving referenced Script/Sequence `emin`, `imin`, `imax`, sleeps, offsets, ADB OFF mode, cancellation, and live status behavior.
- Wireless ADB Pair / Connect and Restart Server as manual helpers, not a background reconnect loop.
- Saved Wi-Fi device keys without ports, with current `IP:Port` stored in `lastSerial`.
- Cancellation-based Run/Stop behavior.
- ADB OFF path where commands are skipped but status/timing still update.
- Daily logs under `AppContext.BaseDirectory\logs`.
- 7-day log retention.

## Must Do

- Run `dotnet build Lazy_App_Codex_Core.sln` before delivering code changes when possible.
- Keep UI changes compatible with Windows Forms designer metadata.
- Preserve fixed run-set column widths when changing the main-window layout.
- Keep `RunSetControl` designer-safe: explicit controls in `RunSetControl.Designer.cs`; fixed layout and data population in `RunSetControl.cs`.
- Keep `AutoScaleMode.None` for `Form1` and `RunSetControl` unless there is a deliberate, tested replacement for the fixed-size main-window behavior.
- Keep `Form1` sizing client-area based by setting `ClientSize` and deriving the fixed outer size with `SizeFromClientSize(...)`; do not go back to pinning only outer `Size`, because 100% and 125% display scaling can leave different usable content heights.
- Keep the RunSetControl action column as seven fixed `34px` control rows plus an explicit bottom spacer row. Extra vertical room must go to the spacer, not to the Pair / Connect button.
- When moving development to another platform or display context, compare screenshots against the current PC layout before and after. If spacing differs, add a guarded layout profile keyed by DPI/screen/font context instead of overwriting the baseline constants.
- Keep `ConfigEditorForm` button rows from clipping by using explicit `TableLayoutPanel` row heights and docked buttons.
- Leave bottom padding in scrollable/editor panels.
- Validate Run against ADB status before executing commands.
- Keep Set 1 and Set 2 cancellation state, selected device, and live status independent.
- Log ADB decision points when changing monitor or run gating behavior.
- Stop long-running ADB child processes on close or state loss.
- Preserve user changes in the working tree.

## Must Not Do

- Do not reintroduce `hotkeyStartStopToggle` as canonical.
- Do not make positive duration mean seconds.
- Do not allow Script or Sequence `emin` to exceed the displayed max cycle time.
- Do not apply UI offsets to drag or back actions.
- Do not let Sequences reference other Sequences.
- Do not let Run Plans reference other Run Plans.
- Do not remove alias compatibility accidentally.
- Do not convert unknown actions into real device touches.
- Do not start `adb track-devices` unless ADB server is already listening.
- Do not add a separate polling or health-check ADB status path.
- Do not hide blank-tag entries when a configured tag is selected.
- Do not remove hidden Scripts from Sequence script pickers.
- Do not remove hidden Sequences from Run Plan target pickers.
- Do not write unrelated formatting or metadata churn.
- Do not use helper-created status rows or data-populated combo items inside `RunSetControl.Designer.cs`; Visual Studio 2026 may remove or rewrite them.
- Do not tune laptop dimensions by overwriting the compact client-size dimensions without preserving the current profile.

## Build Commands

Restore packages:

```text
dotnet restore Lazy_App_Codex_Core.sln
```

Build debug/default:

```text
dotnet build Lazy_App_Codex_Core.sln
```

Build release like CI:

```text
dotnet build Lazy_App_Codex_Core.sln --configuration Release --no-restore
```

Publish release files locally:

```text
dotnet publish Lazy_App_Codex_Core.csproj --configuration Release --no-restore --output publish
```

Run the app locally:

```text
dotnet run --project Lazy_App_Codex_Core.csproj
```

## Manual Verification Checklist

- App opens.
- Global hotkey dot reflects registration.
- Primary-only hotkeys show a green dot, secondary-only hotkeys show a blue dot, both registered shows a gold dot, and no registered hotkey shows a red dot.
- Taskbar overlay shows the matching hotkey-status identifier and the correct one/two run-set identifiers.
- The main window cannot be manually resized or maximized.
- Opening Set 2 switches to the fixed two-set client size, closing Set 2 switches to the fixed one-set client size, and Run/Stop does not resize either layout.
- `Alt+1` opens and closes Set 2; Set 2 secondary hotkeys register only while Set 2 is open and unregister when it closes.
- `Alt+2` opens Config, `Alt+3` opens Pair / Connect, `Esc` closes Config through its close flow, and `Esc` closes Pair / Connect.
- ADB dot shows dark gray/yellow/green/red correctly.
- Run blocks with no ready device.
- Run blocks with multiple ready devices until a device is selected.
- Run starts with a selected ready device.
- Each run set starts with its selected ready device.
- Each run set stops and notifies independently when its selected device disappears.
- Device dropdown uses saved friendly names and refreshes after Config saves.
- Device dropdowns prevent Set 1 and visible/running Set 2 from selecting the same device.
- Pair / Connect opens Wireless ADB helper, Manual Input clears IP/Port, saved Wi-Fi devices prefill IP, successful Connect refreshes ADB status, and Restart Server refreshes ADB status.
- Devices tab allows rename/delete for saved devices and Sync only for currently connected ready devices.
- Stop cancels promptly.
- Config Editor saves only when requested or confirmed.
- Script picker search clears on close.
- Script picker highlights the current selected item when opened.
- Tag filtering shows selected tag plus blank-tag entries.
- Hidden Scripts are absent from main picker and present for Sequence references.
- Hidden Sequences are absent from main picker and present for Run Plan references.
- Script and Sequence Enforce Min rejects values above max cycle time and stretches randomized cycles up to the configured minimum.
- Run Plans execute repeated alternating items in exact configured order, use each item repeat as the item loop count, and keep each target's `emin`, `imin`, and `imax`.
- Skip dropdown resets to `No Skip` when Script/Sequence/Run Plan selection changes.
- Skip dropdown is disabled while running, re-enabled after stop/finish when applicable, and infinite direct Scripts/Sequences expose only `No Skip`.
- Skip popup shows short options plus readable `Skip:` and `Start:` detail lines without stretching the main action column.
- Direct Script/Sequence skip starts at loop `skip + 1`; Run Plan skip consumes the flattened item-repeat order.
- Countdown progress bar updates during waits and the six-chip timeline shows current/upcoming actions.
- Track Touch starts only when the selected ADB device is ready and stops when that device is lost.
- Open/rebuild in Visual Studio 2026 does not remove RunSetControl status rows, blank the Offset dropdown, shrink controls unexpectedly, or reintroduce large PC whitespace.
- Laptop layout tuning keeps the PC layout visually intact when retested on the PC, allowing only small outer-window differences from Windows DPI/title-bar metrics.
