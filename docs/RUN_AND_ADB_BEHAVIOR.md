# Run And ADB Behavior

## Run Rules

- Start and stop must be cancellation-token based.
- Each run set owns its own cancellation token and task. `StopRunAsync` cancels the selected set and waits for that set's task.
- Avoid fire-and-forget script execution.
- `Duration <= 0` means run indefinitely.
- Positive `Duration` means loop count, not seconds.
- Step sleeps and loop interval sleeps are randomized inclusively.
- Inverted min/max values are swapped by `ScriptRunner.RandomBetween`.
- Optional Script and Sequence `emin` enforces a minimum cycle time and must not exceed the displayed max cycle time.
- Run Plan item repeat counts override the referenced target's saved loop count only for that item.
- ADB OFF mode skips ADB commands but still updates status and waits through configured sleeps.
- Delay is a no-ADB planned action whose `t` range supplies a randomized cancellable wait.
- Skip is a pre-run UI option only. It is disabled while running and reset after completion or cancellation.
- Skip is disabled for infinite direct Scripts/Sequences. The last finite loop is never offered as a skip option.

## Script Execution

For a Script:

1. If finite Skip is selected, start at loop `skip + 1`.
2. Expand action groups into steps.
3. Apply selected UI offset only to the first planned left-click step; leading Delay steps do not consume it.
4. Execute each planned step.
5. Sleep for each step's randomized sleep.
6. Sleep for the Script interval after each loop when interval max is positive.
7. If Script `emin` is set, adjust the planned waits before the first action so the cycle lasts at least `emin` seconds.

## Sequence Execution

For a Sequence:

1. If finite Skip is selected, start at sequence loop `skip + 1`.
2. Expand each Sequence item in order.
3. For Script items, resolve the referenced Script by `scriptId`.
4. Ignore the Script's own loop count and interval inside the Sequence.
5. Use the Sequence item's `Repeat`.
6. Add the Sequence item's delay to the last expanded step for that item.
7. For direct action items, use the selected Sequence/main offset context.
8. Sleep for the Sequence interval after each Sequence cycle when interval max is positive.
9. If Sequence `emin` is set, adjust the planned waits before the first action so the cycle lasts at least `emin` seconds.

The Sequence total shown in the editor is one Sequence cycle and does not multiply by Sequence loop count.

## Run Plan Execution

For a Run Plan:

1. Execute items in the configured order.
2. For Script items, resolve the referenced Script by stable ID.
3. For Sequence items, resolve the referenced Sequence by stable ID.
4. Run each item for that item's repeat count.
5. Ignore the referenced target's saved loop count for that item only.
6. Preserve the target's own cycle internals: Script/Sequence `emin`, interval min/max, step sleeps, Sequence item delays, direct actions, and ADB OFF behavior.
7. Use the same run-set cancellation token and live status panel as direct Script/Sequence runs.
8. If Skip is selected, consume the flattened plan item-repeat order before executing. For example, `A A A A A B B B` with skip 3 starts at the fourth global plan cycle; skip 5 starts at the first `B`.

Run Plan totals shown in the editor add each referenced target's min/max cycle time multiplied by that item repeat. Broken or deleted targets are shown as missing and are excluded from the computed total.

## Live Progress

Runtime status updates include:

- Current action, step, cycle, next action, next action time, and estimated end.
- Countdown start/end timestamps and countdown length for waits.
- A six-chip action timeline used by the main window.

Delay appears as `DELAY` in the current/next action and timeline surfaces, and its `t` wait drives the normal countdown fields.

The main form updates countdown display once per second only while at least one run set is active. While idle, the timer is stopped.

## Cycle Enforcement

When `emin` is greater than zero, the runner computes the whole cycle plan before execution. If the randomized plan is shorter than `emin`, it re-randomizes the lowest flexible wait upward, including Delay steps, action sleeps, Sequence item delays folded into their item, and the cycle interval. This repeats until the plan reaches `emin` or max cycle time.

If `emin` equals max cycle time, the runner skips extra random attempts and uses every flexible wait at its maximum value.

## Offset Application

- Only left-click steps consume offsets.
- Delay, drag, and back actions do not consume offsets.
- The selected main offset is applied to the first left-click step in a Script run.
- For Sequence Script items, offset lookup uses the script item's own Script name.
- Direct Sequence action items use the selected Sequence/main offset context.
- For Run Plan Script items, the Script default offset wins when enabled; otherwise the run set's selected offset is used.
- For Run Plan Sequence items, the Sequence default offset wins when enabled; otherwise the run set's selected offset is used.
- A Run Plan can switch offsets per item, so different Sequence defaults are preserved inside the same plan.
- A per-step `offset` or `o` value of `x` or `y` overrides the selected axis.

## Delay Behavior

- `delay` performs no ADB command.
- `wait` is accepted from stored configuration and normalizes to `delay`.
- The step `t` minimum/maximum controls the randomized delay in seconds.
- Delay is cancellable through the same run-slot token as action and interval waits.
- A leading Delay does not consume the selected offset; the first following applicable left-click receives it.
- Delay participates in cycle totals, `emin` enforcement, live countdowns, and the action timeline.

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

The controller also exposes captured Wireless ADB helpers for `adb pair`, `adb connect`, `adb kill-server`, and `adb start-server`, so UI callers can display command output and detect success.

## ADB Status Dot

`adbStatusDot` reports background ADB/device status:

- Dark gray: no ADB server.
- Red: server running, no ready device.
- Green: exactly one ready device.
- Yellow: multiple ready devices.

Each visible run-set Device dropdown is populated only from currently ready `device` rows in the latest `adb track-devices` snapshot. One ready device is auto-selected when it is not already selected by the other visible/running set. With multiple devices, the same serial must not be selectable in both visible/running run sets. Wi-Fi serials are shown without their port in the dropdown, while commands still use the full serial internally.

Device display names come from `settings.devices`. New devices are synced from ADB properties and default to `manufacturer : model`. If a current connected device's detected manufacturer/model conflicts with saved metadata, the dropdown item is highlighted red instead of overwriting the saved entry.

This is separate from `statusDot`, which reports global hotkey registration: gold for both primary and secondary, green for primary only, blue for secondary only, and red for none.

## ADB Monitoring Rules

- First check localhost port `5037`.
- Start `adb track-devices` only when the server is already listening.
- While no server is detected, retry every 30 seconds.
- Refresh status on Run and Config actions.
- Refresh status after closing the Wireless ADB Pair / Connect window, including after ADB server restart.
- Run should trust cached no-device status and prompt immediately.
- Only dark gray should attempt a fresh ADB monitor start/check before showing a message or running.
- A run set starts only when its selected ready device is available.
- If a selected device disappears while running, cancel only the affected run set and notify the user.
- If `adb track-devices` starts successfully but emits no initial device block before the Run timeout, treat it as red/no ready device.
- Do not add a separate polling or health-check path to cover `track-devices` bugs.

ADB status should be driven by:

- `track-devices` process output blocks
- stdout close
- stderr
- process exit
- no-server retry timer

## Wireless ADB Pair / Connect

Wireless Debugging pairing and connecting are separate. Pairing authorizes the computer, but connecting still requires the current connect port shown by Android. Because the wireless debugging connect port may change, saved Wi-Fi devices store only the IP address as the key and keep the most recent `IP:Port` in `lastSerial`.

The main Pair / Connect button opens a Wireless ADB window with:

- Action dropdown containing Pair and Connect.
- Device dropdown with Manual Input at index 0.
- Saved Wi-Fi device entries from `settings.devices`.
- Fixed-separator IPv4 input where each segment validates from 0 through 255.
- Numeric-only Port field.
- Numeric-only Pair Code field when Pair is selected.
- Try button that runs the selected Pair or Connect action.
- Enter invokes the Try button.
- Restart button that runs `adb kill-server` followed by `adb start-server`.

Selecting a saved Wi-Fi device fills the IP field. Selecting Manual Input clears IP and Port. A successful Connect updates `settings.devices[ip].lastSerial` and `lastSeen`, then the main ADB monitor refreshes. Pair success is displayed but does not mark the device connected. Restart Server success is displayed and also refreshes the main ADB monitor.

## Required ADB Logging

ADB monitor and run gating changes use the existing `LogAdbWarning(...)` helper, which prefixes `[ADB]` and writes through `AppLogger.LogWarning`:

```text
LogAdbWarning("...")
```

Important decision points:

- Trigger source.
- Tracker process state.
- Port check result.
- `track-devices` output blocks.
- Process exit.
- Run allow or block.

## Track Touch

The Config Editor opens one owned Track Touch window from the Scripts and Sequences tabs when a ready ADB device is available.

Rules:

- The Device dropdown lists all currently ready devices by saved friendly name while retaining the full ADB serial internally.
- The initially selected main-window device is selected when the window opens.
- Changing Device stops the previous tracking process, resets live gesture state, and starts tracking the new selection. Point history remains visible and records the device name for each entry.
- It prefers `wm size` Override size when present, otherwise Physical size, then uses the controller fallback.
- It reads touch ABS ranges per `/dev/input/event*` device with `adb shell getevent -lp`.
- It starts one long-running `adb shell getevent -l` process for the selected device.
- It maps coordinates only when the event line comes from an input device with a known matching range.
- It parses `ABS_MT_POSITION_X`/`ABS_MT_POSITION_Y` and supports `ABS_X`/`ABS_Y`.
- Live coordinates and gesture state continue to update for both points and drags, with raw coordinate/range/device diagnostics shown below.
- Only completed point gestures are appended to history; drags are shown live but are not recorded.
- Double-clicking a history row copies its X/Y values into the test fields.
- Test Tap sends the entered coordinates through `adb shell input tap` to the selected ready device.
- Tracking stops on selected-device loss, device switch, Track Touch close, or Config Editor close. Device loss leaves the window available for selecting another ready device.
