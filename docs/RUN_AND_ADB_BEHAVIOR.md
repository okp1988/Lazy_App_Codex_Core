# Run And ADB Behavior

## Run Rules

- Start and stop must be cancellation-token based.
- `StopRunAsync` cancels `_runCts` and waits for `_runTask`.
- Avoid fire-and-forget script execution.
- `Duration <= 0` means run indefinitely.
- Positive `Duration` means loop count, not seconds.
- Step sleeps and loop interval sleeps are randomized inclusively.
- Inverted min/max values are swapped by `ScriptRunner.RandomBetween`.
- ADB OFF mode skips ADB commands but still updates status and waits through configured sleeps.

## Script Execution

For a Script:

1. Expand action groups into steps.
2. Apply selected UI offset only to the first planned left-click step.
3. Execute each planned step.
4. Sleep for each step's randomized sleep.
5. Sleep for the Script interval after each loop when interval max is positive.

## Sequence Execution

For a Sequence:

1. Expand each Sequence item in order.
2. For Script items, resolve the referenced Script by `scriptId`.
3. Ignore the Script's own loop count and interval inside the Sequence.
4. Use the Sequence item's `Repeat`.
5. Add the Sequence item's delay to the last expanded step for that item.
6. For direct action items, use the selected Sequence/main offset context.
7. Sleep for the Sequence interval after each Sequence cycle when interval max is positive.

The Sequence total shown in the editor is one Sequence cycle and does not multiply by Sequence loop count.

## Offset Application

- Only left-click steps consume offsets.
- Drag and back actions do not consume offsets.
- The selected main offset is applied to the first left-click step in a Script run.
- For Sequence Script items, offset lookup uses the script item's own Script name.
- Direct Sequence action items use the selected Sequence/main offset context.
- A per-step `offset` or `o` value of `x` or `y` overrides the selected axis.

## Drag Behavior

- Drag uses `s` as the start point.
- Drag uses `s2` as the end point when supplied.
- Without explicit `s2`, directional drag aliases should derive an endpoint from `RandX` and `RandY`.
- Drag duration is currently a fixed ADB swipe duration in `ScriptRunner`.

## ADB Controller

Default ADB path:

```text
C:\adb\adb.exe
```

ADB commands are executed in physical device-pixel coordinates.

## ADB Status Dot

`adbStatusDot` reports background ADB/device status:

- Dark gray: no ADB server.
- Red: server running, no ready device.
- Green: exactly one ready device.
- Yellow: multiple ready devices.

The Device dropdown is populated only from currently ready `device` rows in the latest `adb track-devices` snapshot. One ready device is auto-selected. With multiple ready devices, the user must select which serial receives ADB commands. Wi-Fi serials are shown without their port in the dropdown, while commands still use the full serial internally.

Device display names come from `settings.devices`. New devices are synced from ADB properties and default to `manufacturer : model`. If a current connected device's detected manufacturer/model conflicts with saved metadata, the dropdown item is highlighted red instead of overwriting the saved entry.

This is separate from `statusDot`, which reports global hotkey registration.

## ADB Monitoring Rules

- First check localhost port `5037`.
- Start `adb track-devices` only when the server is already listening.
- While no server is detected, retry every 30 seconds.
- Refresh status on Run and Config actions.
- Run should trust cached no-device status and prompt immediately.
- Only dark gray should attempt a fresh ADB monitor start/check before showing a message or running.
- Run starts only when a selected ready device is available.
- If the selected device disappears while running, cancel the run and notify the user.
- If `adb track-devices` starts successfully but emits no initial device block before the Run timeout, treat it as red/no ready device.
- Do not add a separate polling or health-check path to cover `track-devices` bugs.

ADB status should be driven by:

- `track-devices` process output blocks
- stdout close
- stderr
- process exit
- no-server retry timer

## Required ADB Logging

ADB monitor and run gating changes should log decision points with:

```text
AppLogger.LogInfo("[ADB] ...")
```

Important decision points:

- Trigger source.
- Tracker process state.
- Port check result.
- `track-devices` output blocks.
- Process exit.
- Run allow or block.

## Track Touch

The Config Editor owns a shared Script/Sequence `Track Touch` toggle.

Rules:

- It is enabled only while the selected ADB device is ready.
- It reads display size.
- It reads touch ABS ranges per `/dev/input/event*` device.
- It starts one long-running `adb shell getevent -l` process.
- It maps coordinates using the same event device that emitted the touch line.
- It parses `ABS_MT_POSITION_X` and `ABS_MT_POSITION_Y`.
- It also supports `ABS_X` and `ABS_Y`.
- It displays scaled screen coordinates only.
- If mapping fails, it warns instead of displaying raw values as screen coordinates.
- The active button must have a strong visible ON style.
- The process must stop on toggle-off, loss of selected ADB device readiness, or editor close.
