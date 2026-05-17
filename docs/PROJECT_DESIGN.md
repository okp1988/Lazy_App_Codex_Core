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
3. App registers global hotkeys when the main window is active/restored.
4. App checks whether an ADB server is already listening on port `5037`.
5. If ADB server is present, app starts a background `adb track-devices` monitor.
6. Current ready devices appear in the Device dropdown.
7. If Wireless ADB is paired but not connected, the user may open Pair / Connect and manually pair or connect with the current phone IP and port.
8. User selects a Script or Sequence.
9. Optional default offset from the selected entry auto-selects the main offset dropdown.
10. User clicks Run or presses the configured start hotkey.
11. App refreshes ADB readiness, then runs only when a ready device is selected.
12. User clicks Stop, presses Escape, or presses the configured stop/toggle hotkey to cancel.

## Main Concepts

### Script

A Script is a reusable automation unit. It contains metadata, loop settings, interval settings, action groups, and steps.

Scripts can be hidden from the main picker while remaining valid inside Sequences.

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

### Offset

Offsets are selected in the main window and applied only to left-click actions. The offset axis comes from the main selector unless a step overrides it with `offset` or `o`.

Offset profiles are named `s<number>` and selected by matching digits in the runnable name.

### Tag

Tags are configured under `settings.tag`. Scripts and Sequences may have one tag or a blank tag.

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
