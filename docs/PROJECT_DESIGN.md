# Project Design

## Purpose

Lazy App is a Windows desktop automation runner for Android devices. It uses ADB commands to run configured taps, back actions, and drags against the connected phone screen.

The app is designed for manual Android workflows where the user wants repeatable device actions without installing an app on the phone and without root access.

## Project Shape

- Single Windows Forms project.
- Target framework: `net8.0-windows`.
- UI framework: Windows Forms with `UseWindowsForms=true`.
- Solution file: `Lazy_App_Codex_Core.sln`.
- Main project file: `Lazy_App_Codex_Core.csproj`.
- No separate automated test project currently exists.
- Main runtime configuration file: `config.json`.

## Primary User Flows

1. Start the app.
2. App loads `config.json` from the current working directory.
3. App registers Set 1 primary global hotkeys when the main window is active/restored. Set 2 secondary hotkeys are registered only while Set 2 is open.
4. App checks whether an ADB server is already listening on port `5037`.
5. If ADB server is present, app starts a background `adb track-devices` monitor.
6. Current ready devices appear in the visible run-set Device dropdowns, with the same device excluded from the other visible/running set.
7. If Wireless ADB is paired but not connected, the user may open Pair / Connect and manually pair or connect with the current phone IP and port.
8. User selects a Script, Sequence, or Run Plan in Set 1, or opens Set 2 with `Alt+1` and selects a second Script, Sequence, or Run Plan there.
9. Optional default offset from the selected entry auto-selects that run set's offset dropdown.
10. User clicks Run or presses that set's configured start hotkey.
11. App refreshes ADB readiness, then runs that set only when a ready device is selected.
12. User clicks Stop, presses Escape from the main window, or presses the configured stop/toggle hotkey to cancel.

## Main Concepts

### Script

A Script is a reusable automation unit. It contains metadata, loop settings, interval settings, action groups, and steps.

Scripts can be hidden from the main picker while remaining valid inside Sequences.

Scripts can also define an enforced minimum cycle time, which raises randomized waits within the cycle until the planned cycle meets the configured minimum.

### Action Group

An Action Group contains one or more steps and a repeat count. The group expands into repeated step actions at runtime.

### Step

A Step is one device action plus optional randomization, sleep timing, second drag coordinate, and per-step offset axis override.

Supported editor actions:

- `left`
- `right`
- `drag`

Supported runtime aliases must remain backward compatible:

- `leftclick`
- `rightclick`
- `back`
- `leftdrag`
- `rightdrag`
- `updrag`
- `downdrag`

### Sequence

A Sequence is a first-class runnable entry. It may contain Script items and direct action items.

Sequences must not contain other Sequences.

Sequences can define the same enforced minimum cycle time as Scripts. The limit applies to one sequence cycle and does not multiply by loop count.

Sequences can be hidden from the main picker while remaining valid inside Run Plans.

### Run Plan

A Run Plan is a first-class runnable entry that executes an ordered list of existing Scripts and Sequences.

Run Plan items store the target type, target ID, and repeat count. The repeat count overrides the target's saved loop count only for that item, while the referenced target keeps its own internal cycle rules such as `emin`, `imin`, `imax`, sleeps, Sequence item delays, default offsets, ADB OFF mode, status updates, and cancellation.

Run Plans may repeat the same target multiple times and preserve the configured item order exactly. They must not contain other Run Plans.

### Hotkey

Primary start and stop hotkeys are configured in Settings and control Set 1. Optional backup start and stop hotkeys control Set 2 while Set 2 is open.

The main hotkey status dot is gold when both primary and secondary hotkeys register, green when primary only registers, blue when secondary only registers, and red when no hotkey registers.

The taskbar overlay mirrors the hotkey registration state with a small status identifier and shows one or two run-set identifiers depending on whether Set 2 is open.

### Run Set

The main window has two independent run sets. Set 1 is always visible. Set 2 opens and closes with `Alt+1`, has its own Script/Sequence/Run Plan picker, offset, tag, device, run button, and live status panel, but does not duplicate Config or Pair / Connect.

The same ADB device cannot be selected in both visible/running sets. Closing a stopped Set 2 releases its selected device and unregisters Set 2 secondary hotkeys.

The main window uses fixed sizes for one-set and two-set layouts. User resize and maximize are disabled, and run actions should not stretch or shrink either set; only opening or closing Set 2 changes the window size.

### Offset

Offsets are selected in the main window and applied only to left-click actions. The offset axis comes from the main selector unless a step overrides it with `offset` or `o`.

Offset profiles are named `s<number>` and selected by matching digits in the runnable name.

### Tag

Tags are configured under `settings.tag`. Scripts, Sequences, and Run Plans may have one tag or a blank tag.

The main filter always includes `All`. Selecting a configured tag shows matching entries plus blank-tag entries.

### Device

Devices are discovered from `adb track-devices` ready rows. The main Device dropdown lists only currently ready devices and uses friendly names stored under `settings.devices`.

Wi-Fi device keys use the IP address without the port for friendlier naming, while ADB commands still use the full current serial. New device metadata can be synced from Android properties, and users can rename saved devices in the Config Editor Devices tab.

The main window also provides a Pair / Connect Wireless ADB helper. It supports Pair and Connect actions, ADB server restart, Manual Input, saved device IP prefill, fixed-separator IPv4 entry, numeric-only ports and pairing codes, and updates a Wi-Fi device's last serial after a successful Connect.

## Non-Goals

- Do not install anything on the phone.
- Do not require root access.
- Do not make `config.json` part of build or publish output.
- Do not add a separate health-check poller for ADB status.
- Do not make hidden Scripts disappear from Sequence references.
- Do not make hidden Sequences disappear from Run Plan references.
