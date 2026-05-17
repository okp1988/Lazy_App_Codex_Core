# Architecture

## Module Map

### `Program.cs`

Application entry point. It initializes logging, configures global exception handlers, initializes WinForms, and opens `Form1`.

### `Form1.cs`

Main window and runtime coordinator.

Responsibilities:

- Load and reload configuration.
- Build the Script/Sequence run target list.
- Apply tag filtering.
- Apply default offsets.
- Register and unregister global hotkeys.
- Route Run, Stop, Escape, and hotkey actions.
- Own run cancellation state.
- Own live run status labels.
- Own taskbar overlay icons.
- Own ADB status monitor state.
- Own current ADB device dropdown state.
- Open `ConfigEditorForm`.
- Open `WirelessAdbConnectForm`.

### `Form1.Designer.cs`

Designer-managed main-window controls. Keep layout edits compatible with WinForms designer expectations.

### `ConfigEditorForm.cs`

Hand-built config editor for:

- Settings
- Devices
- Offset profiles
- Scripts
- Sequences

The editor works in memory while open. It writes `config.json` only on explicit save, confirmed close-save, or restore.

It also owns the shared Script/Sequence `Track Touch` toggle.

### `WirelessAdbConnectForm.cs`

Hand-built Wireless ADB helper window for manual Pair, Connect, and ADB server restart flows. It uses saved `settings.devices` entries for friendly device choices, supports Manual Input, validates IPv4 address segments and numeric ports/pairing codes, runs `adb pair` or `adb connect`, can restart the ADB server with `adb kill-server` plus `adb start-server`, and updates saved Wi-Fi device `lastSerial`/`lastSeen` after a successful Connect.

### `SearchableDropdown.cs`

Custom main Script/Sequence picker. The popup owns search text and clears it on close. The main field displays only the selected item. When opened, the popup highlights the current selected item if it is present in the current filtered list.

### `ScriptConfigRespository.cs`

Configuration repository. It loads, migrates, normalizes, and saves `config.json`.

Important: the filename currently contains `Respository`, not `Repository`. Rename only as a deliberate refactor.

### `ScriptModel.cs`

In-memory config models:

- `ConfigLibrary`
- `ScriptModel`
- `ActionGroup`
- `SequenceModel`
- `SequenceItem`
- `StepAction`

### `ScriptRunner.cs`

Runtime planner and executor. It expands Scripts and Sequences into planned ADB commands, applies random sleeps, applies offsets, updates live status, and respects cancellation.

### `AdbShellController.cs`

ADB command wrapper. It shells out to `C:\adb\adb.exe` by default and works in physical device pixels.

It exposes captured Pair, Connect, Kill Server, and Start Server helpers for Wireless ADB UI flows so success/failure output can be shown in the dialog.

### `HotKeyManager.cs`

Global hotkey parser, registration, unregistration, and `WM_HOTKEY` routing.

### `OffsetDisplayOption.cs`

Main offset dropdown values and labels.

### `MouseHelper.cs`

Coordinate randomization helper.

### `AppLogger.cs`

Daily file logger under `AppContext.BaseDirectory\logs`. It deletes logs older than 7 days at startup.

## Runtime Data Flow

```mermaid
flowchart TD
    A["config.json"] --> B["ScriptConfigRepository"]
    B --> C["ConfigLibrary"]
    C --> D["Form1 run target list"]
    D --> E["User selects Script or Sequence"]
    E --> F["Form1 validates ADB status"]
    F --> G["ScriptRunner expands plan"]
    G --> H["AdbShellController executes adb commands"]
    G --> I["Form1 live status labels"]
```

## ADB Status Flow

```mermaid
flowchart TD
    A["Trigger: load, retry, Run, Config, Wireless ADB"] --> B["Check localhost:5037"]
    B -->|not listening| C["NoServer: dark gray"]
    B -->|listening| D["Start adb track-devices"]
    D --> E["Parse device blocks"]
    E -->|0 ready devices| F["NoDevice: red"]
    E -->|1 ready device| G["OneDevice: green"]
    E -->|2+ ready devices| H["MultipleDevices: yellow"]
    E --> I["Refresh Device dropdown"]
    D -->|stdout close, stderr, exit| C
```

## Wireless ADB Flow

```mermaid
flowchart TD
    A["User Clicks Pair / Connect"] --> B["WirelessAdbConnectForm Opens"]
    B --> C["User Chooses Pair, Connect, Manual Input, Or Restart Server"]
    C --> D["Validate IP, Port, And Optional Pair Code"]
    D --> E["Run adb pair Or adb connect"]
    E -->|Connect Succeeds| F["Save lastSerial And lastSeen"]
    E -->|Pair Succeeds| G["Show Pair Status"]
    E -->|Restart Server Succeeds| I["Show Restart Status"]
    F --> H["Refresh Main ADB Monitor"]
    G --> H
    I --> H
```

## Run Flow

```mermaid
flowchart TD
    A["Run requested"] --> B["Refresh ADB status"]
    B --> C["Validate selected target and selected device"]
    C --> D{"Selected ready device?"}
    D -->|no| E["Show ADB warning and do not run"]
    D -->|yes| F["Create CancellationTokenSource"]
    F --> G["Disable editable UI"]
    G --> H["Run Script or Sequence"]
    H --> I["Update live status each step"]
    I --> J["Complete or cancel"]
    J --> K["Re-enable UI and reset title"]
```
