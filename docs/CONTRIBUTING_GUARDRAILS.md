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
- Hidden Scripts remaining valid in Sequences.
- Tag filter behavior with `All` at index 0.
- Separate hotkey and ADB status dots.
- ADB monitor color meanings.
- Wireless ADB Pair / Connect and Restart Server as manual helpers, not a background reconnect loop.
- Saved Wi-Fi device keys without ports, with current `IP:Port` stored in `lastSerial`.
- Cancellation-based Run/Stop behavior.
- ADB OFF path where commands are skipped but status/timing still update.
- Daily logs under `AppContext.BaseDirectory\logs`.
- 7-day log retention.

## Must Do

- Run `dotnet build Lazy_App_Codex_Core.sln` before delivering code changes when possible.
- Keep UI changes compatible with Windows Forms designer metadata.
- Keep `ConfigEditorForm` button rows from clipping by using explicit `TableLayoutPanel` row heights and docked buttons.
- Leave bottom padding in scrollable/editor panels.
- Validate Run against ADB status before executing commands.
- Log ADB decision points when changing monitor or run gating behavior.
- Stop long-running ADB child processes on close or state loss.
- Preserve user changes in the working tree.

## Must Not Do

- Do not reintroduce `hotkeyStartStopToggle` as canonical.
- Do not make positive duration mean seconds.
- Do not apply UI offsets to drag or back actions.
- Do not let Sequences reference other Sequences.
- Do not remove alias compatibility accidentally.
- Do not convert unknown actions into real device touches.
- Do not start `adb track-devices` unless ADB server is already listening.
- Do not add a separate polling or health-check ADB status path.
- Do not hide blank-tag entries when a configured tag is selected.
- Do not remove hidden Scripts from Sequence script pickers.
- Do not write unrelated formatting or metadata churn.

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
- ADB dot shows dark gray/yellow/green/red correctly.
- Run blocks with no ready device.
- Run blocks with multiple ready devices until a device is selected.
- Run starts with a selected ready device.
- Run stops and notifies when the selected device disappears.
- Device dropdown uses saved friendly names and refreshes after Config saves.
- Pair / Connect opens Wireless ADB helper, Manual Input clears IP/Port, saved Wi-Fi devices prefill IP, successful Connect refreshes ADB status, and Restart Server refreshes ADB status.
- Devices tab allows rename/delete for saved devices and Sync only for currently connected ready devices.
- Stop cancels promptly.
- Config Editor saves only when requested or confirmed.
- Script picker search clears on close.
- Script picker highlights the current selected item when opened.
- Tag filtering shows selected tag plus blank-tag entries.
- Hidden Scripts are absent from main picker and present for Sequence references.
- Track Touch starts only when the selected ADB device is ready and stops when that device is lost.
