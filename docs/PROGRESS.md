# Progress

## Record Purpose

This is the approved project reference and implementation/verification history consolidated on 2026-10-07. Global working rules take priority; project constraints are maintained in [AGENTS.md](../AGENTS.md). This record requires explicit approval for updates. [Discussion](DISCUSSION.md) contains active work and [External Folders](EXTERNAL_FOLDERS.md) records external roots.

Implementation completion, build results, review findings, and manual-test evidence are recorded separately. Completed work does not stay in Discussion waiting for acknowledgement; silence is not a passed test result. Known limitations below are reference findings, not new approved bug-fix tasks.

## 2026-10-07 — Markdown Rules And Consolidation

- User approved four maintained project Markdown records: AGENTS, Discussion, Progress, and External Folders.
- Only Discussion and External Folders may update automatically. AGENTS and Progress require an explicit request or approval.
- Global completion/testing rules were updated so implementation and bug fixes close after reporting actual checks and limitations, without requesting manual-test or retest acknowledgement.
- Project rules now defer to global approval and computer-control boundaries. Described build/run/ADB commands are not permission to execute external or protected operations.
- Useful content from the superseded README, architecture, configuration, design, run/ADB, contributor, and dated review records is consolidated below. The superseded files are Git-tracked; their full prior text remains recoverable from repository history.
- Documentation review identified inaccurate claims about unknown-action handling and directional drag endpoints. The current behavior is recorded below; application source was not changed.
- Verification: independent Documentation / Consistency review passed; exactly four maintained project Markdown files remain; relative links, whitespace, and superseded-reference checks passed; `git diff --check` passed with only the normal LF/CRLF warning. Workspace status confirms only the approved Markdown changes. No application build, GUI/device interaction, or manual test was performed for this documentation-only change.

## Project And Development Reference

Lazy App is a Windows Forms Android automation runner for configured taps, back actions, drags, and waits. Ordinary automation does not require an app installed on the phone or root access; touch event access remains dependent on the device's permissions and event format.

- Solution: `Lazy_App_Codex_Core.sln`; project: `Lazy_App_Codex_Core.csproj`.
- Single SDK-style Windows Forms project targeting `net8.0-windows` with `UseWindowsForms=true`; no separate automated test project.
- Development uses Windows and the .NET 8 SDK. Existing setup guidance supports Visual Studio 2022 17.8+ or Visual Studio 2026 with the .NET desktop development workload; `.vsconfig` records the workload.
- Designer metadata for Form1, Config Editor, Searchable Dropdown, and Wireless ADB is in the project file rather than user-local `.csproj.user` settings.
- ADB defaults to `C:\adb\adb.exe`. The user enables USB or Wireless Debugging and connects a ready device. `scrcpy` is optional for user-operated mirroring.

Established command reference, subject to global approval rules:

| Purpose | Command |
| --- | --- |
| Build with restored packages | `dotnet build Lazy_App_Codex_Core.sln --no-restore` |
| Default build | `dotnet build Lazy_App_Codex_Core.sln` |
| Restore when authorized | `dotnet restore Lazy_App_Codex_Core.sln` |
| Release build | `dotnet build Lazy_App_Codex_Core.sln --configuration Release --no-restore` |
| Local release publish | `dotnet publish Lazy_App_Codex_Core.csproj --configuration Release --no-restore --output publish` |
| Launch when separately authorized | `dotnet run --project Lazy_App_Codex_Core.csproj` |

CI is defined in `.github/workflows/build.yml`, using `windows-latest`, `actions/setup-dotnet@v4`, and .NET `8.0.x`. Pushes to `main` or `master` replace the release tagged `latest`; its zip combines publish output with repository `config.json`. The project file itself intentionally excludes `config.json` from build/publish copying. These descriptions do not authorize restore, network access, publish, launch, commit, or push.

## Architecture And Ownership

| Source | Responsibility |
| --- | --- |
| `Program.cs` | Logging startup, global exception handlers, WinForms initialization, main window. |
| `Form1.cs` | Config reload, runnable/tag/offset/skip selection, two run slots, hotkeys, live status, taskbar overlay, fixed client sizing, ADB monitor/device readiness, opening Config and Pair / Connect. |
| `Form1.Designer.cs` | Designer shell containing `mainLayout`; repeated run UI belongs to RunSetControl. |
| `RunSetControl.cs` / `.Designer.cs` / `.resx` | Shared run-set control; explicit designer controls plus runtime layout, sizing, and dropdown population. Set 2 hides shared Config, Pair / Connect, and status controls. |
| `ConfigEditorForm.cs` | Staged Settings, Devices, Offset, Scripts, Sequences, and Run Plans editing; save/backup/restore and owned Track Touch window. |
| `TrackTouchForm.cs` | Friendly-device touch tracker, mapped live coordinates and diagnostics, completed-point history, coordinate test tap, tracker lifecycle. |
| `WirelessAdbConnectForm.cs` | Manual Pair, Connect, and Restart Server, input validation, command output, saved Wi-Fi serial updates, Enter-to-Try and Escape close. |
| `SearchableDropdown.cs` | Main runnable picker; popup search and selected-item highlighting. |
| `SkipPickerControl.cs` | Compact Skip field and popup detail; hover previews, click commits. |
| `CountdownProgressControl.cs` | Fixed-size wait progress and caption. |
| `ScriptConfigRespository.cs` | Config loading, migration, normalization, and saving. The filename intentionally retains the existing `Respository` spelling. |
| `ScriptModel.cs` | ConfigLibrary, ScriptModel, ActionGroup, SequenceModel/SequenceItem, RunPlanModel/RunPlanItem, and StepAction. |
| `ScriptRunner.cs` | Cycle planning, expansion, randomized timing, Delay, offsets, finite Skip, ADB command execution, cancellation, and live progress. |
| `AdbShellController.cs` | ADB command wrapper and captured Pair/Connect/Kill/Start Server helpers; physical pixel coordinates. |
| `HotKeyManager.cs` | Parsing, independent primary/secondary registration, unregistration, and WM_HOTKEY routing. |
| `OffsetDisplayOption.cs` / `MouseHelper.cs` | Offset choices and coordinate randomization. |
| `AppLogger.cs` | Daily logs at `AppContext.BaseDirectory\logs`; startup deletes logs older than seven days. |

Runtime flow: working-directory `config.json` loads into ConfigLibrary; Form1 builds/filter/selects runnable targets and validates the selected ready device; ScriptRunner expands and plans a cycle, enforces timing, then sends ADB commands and live progress to the selected run slot. Completion or cancellation restores editable controls, resets Skip, and resets the title. Each slot owns its own task and cancellation token.

## Main Window And UI Reference

- Set 1 is always visible and uses primary hotkeys. `Alt+1` toggles Set 2, which uses optional backup hotkeys only while open. `Alt+2` opens Config and `Alt+3` opens Pair / Connect.
- Escape stops runs from the main window, follows the Config close/save flow, and closes Pair / Connect.
- Scripts display as `[S] NAME`, Sequences as `[Q] NAME`, and Run Plans as `[P] NAME`.
- Runnable picker search belongs to its popup, clears on close, and highlights the current item when it is still present. The closed field displays the selected item only.
- Main client size is `606 x 292` for one set and `1200 x 292` for two. RunSetControl is `594 x 284`, with content width `410`, action width `184`, and set gap `12`. Outer size derives from `SizeFromClientSize(...)` so DPI-dependent chrome does not squeeze usable content.
- Resize/maximize are disabled. Run/Stop does not change size; only Set 2 visibility changes the client size. Form1 and RunSetControl use `AutoScaleMode.None` for this fixed profile.
- The action column uses seven fixed `34px` rows: Run, Skip, Offset, Tag, Device, Config, Pair / Connect, followed by an explicit spacer. Extra height goes to the spacer.
- Visual Studio 2026 previously rewrote scaling metadata, combo item heights, helper-created status rows, and offset items. Explicit controls stay in the designer; stable pixel layout and runtime item population stay in RunSetControl.cs after `InitializeComponent()`.
- Skip details live in the popup as separate `Skip:` and `Start:` lines, with a larger bottom explanation area. There is no separate inline main-window skip detail label.
- Alternate DPI/font/screen layouts need an explicit guarded profile preserving the existing baseline. Comparisons use user-provided screenshots or separately approved capture.
- Config button rows need explicit TableLayoutPanel heights, docked buttons, and bottom padding to avoid clipping.
- Each run slot shows action, step, cycle, next action/time, estimated end, a six-chip timeline, and countdown progress. The main clock timer runs only while a slot is active; ADB/device errors appear in the affected status panel.
- Offset choices are No Offset, Y up/down one through six steps, and X left/right one through three steps.

Hotkey status is separate from ADB status: gold means primary and secondary registered, green primary only, blue secondary only, and red none. The taskbar overlay mirrors hotkey state and shows one or two run-set identifiers. Primary/backup registration failures do not disable the other successful group. Matching start/stop hotkeys use one registration and toggle that set. Minimize unregisters hotkeys; restore/activation registers them, with UI/warning logging.

## Configuration And Compatibility

The app creates `new ScriptConfigRepository("config.json")`; the process working directory determines the active file. Config Editor changes remain in memory while selecting entries or switching tabs. Writes occur only through Save All & Close, confirmed close-save, or backup restore. Escape follows the same close flow. Open Config Folder, Backup Config, and Restore Config are available; timestamped backups live under the config folder's `backup` directory.

Current shape:

```json
{
  "settings": {},
  "offset": {},
  "scripts": {},
  "sequences": {},
  "runPlans": {}
}
```

Legacy top-level scripts remain supported; category keys are skipped when scanning them.

### Settings, Tags, And Devices

- Canonical settings: `hotkeyStart`, `hotkeyStop`, `hotkeyBackupStart`, `hotkeyBackupStop`, `tag`, `devices`.
- `hotkeyStartStopToggle` migrates to `hotkeyStart`; `settings.tags` migrates to `settings.tag`. Backup hotkeys may be blank; empty hotkey text disables that side.
- Tags are deduplicated case-insensitively. `All` is reserved for the main filter, not a saved tag. The tag list may be empty; each Script/Sequence/Run Plan has one tag or a blank tag.
- The filter always begins with All. Selecting a configured tag shows matches plus blank-tag entries.
- `settings.devices` stores `name`, `manufacturer`, `model`, `lastSerial`, and `lastSeen` per device key. Wi-Fi keys omit the port; USB/mDNS serials retain ADB identity. Default names are `manufacturer : model`.
- The Devices tab permits friendly-name editing, rename/delete for disconnected saved devices, and Sync only for currently connected ready devices. Automatic sync may create/fill missing data but must not overwrite conflicting saved manufacturer/model values; the main dropdown highlights conflicts red.
- Successful Connect saves the current `IP:Port` in `lastSerial` and updates `lastSeen`. Pair alone is not a ready connection.

### Offset Profiles

Profiles use `s<number>` keys, such as `s26: [5,5]`. Each digit group in the runnable name is tried for a matching profile; selected axis chooses X or Y. Fallback keys include `offsetX`, `offsetY`, `ox`, `oy`, `x`, `y`, and `s`.

Script/Sequence defaults auto-select a saved main offset when enabled. Sequence Script items use their own Script names for lookup; direct actions use Sequence/main context. Run Plan items prefer their referenced Script/Sequence default when enabled, otherwise the run-set selection. Defaults can change from item to item. Only the first applicable left-click consumes the selected offset; Delay, back, and drag do not. Step `offset`/`o` of `x` or `y` overrides the selected axis.

### Script, Sequence, And Run Plan Data

| Compact key | Meaning |
| --- | --- |
| `d` | Loop count; non-positive runs indefinitely. |
| `imin` / `imax` | Cycle interval minimum/maximum seconds. |
| `emin` | Optional enforced minimum cycle seconds. |
| `config` | Script action groups. |
| `a` | Action. |
| `s` / `s2` | Start `[x,y]` / drag end `[x2,y2]`. |
| `r` | Randomization `[randX,randY]`. |
| `t` | Sleep range `[sleepMin,sleepMax]`; Delay uses this as its wait. |
| `o` | Offset axis override. |

Compatibility also includes `i`, `steps`, nested `steps` with `repeat`/`rep`, `p`, and `p2`. Editor actions are left, right, drag, and delay. Input aliases include leftclick, rightclick/back, directional drag names, and wait; see observed normalization limitations below.

- Script fields: `id`, `name`, `tag`, `hide`, `order`, `d`, `imin`, `imax`, `emin`, `defaultOffsetEnabled`, `defaultOffset`, `config`. Action groups expand their repeated steps. Names are unique, clones use `_copy`, `_copy2`, etc., and saved manual order is reflected in the main picker.
- Sequence fields: `id`, `name`, `tag`, `hide`, `order`, `d`, `imin`, `imax`, `emin`, `defaultOffsetEnabled`, `defaultOffset`, `items`. Items either reference a Script by `scriptId` or store direct actions. Script renames preserve stable-ID references. Sequences cannot contain Sequences.
- Hidden Scripts stay usable in Sequences; hidden Sequences stay usable in Run Plans while absent from the main picker. Deleting a Script used by Sequences requires confirmation before dependent Sequence deletion.
- Run Plan fields: `id`, `name`, `tag`, `order`, `items`. Item fields: `type` (`script`/`sequence`), stable `targetId`, `repeat`. Run Plans cannot reference Run Plans; configured order and repeated alternation are preserved.

## Execution And Timing

- Each run slot has its own task/token. Stop cancels and awaits that slot; execution is not fire-and-forget.
- Positive Duration is a loop count, not seconds. Random waits are inclusive, and inverted bounds are swapped by `RandomBetween`.
- Direct Script runs expand action groups, apply offset to the first applicable left-click, execute steps and their sleeps, and apply the Script interval after each loop when interval max is positive.
- Inside a Sequence, Script loop count and interval are ignored; the Sequence item's Repeat controls expanded repetition. Item Delay is folded into its last expanded step. Direct actions use Sequence/main context; Sequence interval applies after each cycle.
- A direct Sequence uses its own cycle count. Its displayed total is one cycle, not total multiplied by loop count; repeated Script items contribute to planned current/next step and estimated end.
- Run Plan item repeat overrides only the referenced target's saved Duration. Targets retain their internal `emin`, intervals, sleeps, Sequence item delays/direct actions, offsets, ADB OFF, cancellation, and live status. Totals sum target min/max cycle times multiplied by item repeats; missing targets are displayed and excluded from totals.
- ADB OFF skips commands but still plans, updates status, and waits.
- Delay (`wait` read alias) sends no ADB command and uses `t` seconds as a randomized cancellable wait. It appears as DELAY in current/next action and timeline and drives normal countdowns. A leading Delay leaves offset for the first following applicable left-click.
- Optional `emin` applies to one Script/Sequence cycle and must not exceed its displayed maximum. The runner plans the full cycle before execution and re-randomizes the lowest flexible wait upward until the minimum or maximum is reached. Flexible waits include Delay, action sleeps, folded Sequence item delays, and interval. When `emin` equals the maximum, every flexible wait uses its maximum.
- Drag uses the supplied start and explicit end point, with a fixed ADB swipe duration. Without an explicit endpoint it currently uses the start point; directional derivation is not implemented.

Finite Skip is selected before Run, locked during execution, reset to No Skip on target change and completion/cancellation. Infinite direct Scripts/Sequences expose only No Skip; the final finite cycle cannot be skipped. Direct Skip starts at `skip + 1`. Run Plan Skip consumes flattened item repeats globally: `A A A A A B B B` with skip 3 starts at the fourth A, and skip 5 starts at the first B.

## ADB, Devices, And Wireless Helper

ADB status colors: dark gray no server, red server with no ready device, green one ready device, yellow multiple ready devices. A ready row has ADB state `device`. Each visible/running set excludes the other's selected serial; hidden stopped Set 2 reserves no device. One available device is auto-selected. Friendly names come from settings; Wi-Fi display omits port while commands retain the full serial.

Monitoring starts with localhost port 5037. Only an already-listening server starts `adb track-devices`. With no server, the app retries every 30 seconds and refreshes on Run/Config and Wireless helper close/restart. Config opening is not blocked by status checks. Run refreshes status before target validation, trusts cached no-device status, and only dark gray attempts a fresh monitor check. A running slot cancels and notifies if its selected device disappears. If tracking starts but no initial block arrives before Run timeout, status is red rather than dark gray.

Status is driven by track-devices output blocks, stdout close, stderr/exit, and the no-server timer; no separate health poller or background wireless reconnect/port scan. Monitor/gating decisions use `LogAdbWarning(...)`, prefix `[ADB]`, and `AppLogger.LogWarning`, including trigger, process/port state, output blocks, exit, and allow/block decisions.

Manual command reference:

```text
adb devices
adb pair <IP_ADDRESS>:PAIR_PORT
adb connect <IP_ADDRESS>:CONNECT_PORT
adb disconnect <IP_ADDRESS>:PORT
adb start-server
adb kill-server
```

Pair authorizes the computer; Connect uses the phone's current connect port, which may change. The helper has Pair/Connect actions, Manual Input at device index 0, saved Wi-Fi IP prefill, fixed dot-separated IPv4 segments (0–255), numeric Port and Pair Code, Try, and Restart Server. Pair Code is shown for Pair. Manual Input clears IP/Port. Enter invokes Try for the selected action; Escape closes. Restart runs kill-server then start-server. Results show captured output; successful Connect updates lastSerial/lastSeen and main ADB status, while Pair success alone does not mark connected. Display/status text uses Title Case except user-entered values.

## Track Touch Reference

- Scripts and Sequences open one owned Track Touch window when a ready device exists; the initially selected main device is chosen when available.
- Ready devices display saved friendly names with full serial binding. Switching stops the previous getevent process, resets live gesture state, restarts tracking, and preserves point history.
- Effective display size prefers `wm size` Override, then Physical, then controller fallback. ABS ranges come from `adb shell getevent -lp` per `/dev/input/event*`.
- One `adb shell getevent -l` process streams the selected device. Mapping uses the matching input-device range and ABS_MT_POSITION_X/Y or ABS_X/Y.
- Mapping is `round((raw - min) / (max - min) * (screenSize - 1))`. Saved Script coordinates are absolute; the tracker formula does not rescale existing config points.
- Live point/drag coordinates and gesture state update with raw/range/device diagnostics. Completed point gestures alone enter history with device name/time; drag motion stays live-only.
- Double-click history loads X/Y into test fields. Test Tap uses `adb shell input tap` against the selected ready device.
- Tracking stops on device loss, switch, tracker close, or Config Editor close. Device loss leaves the window available for choosing another ready device.
- Different phones may expose different event devices/ranges, display overrides, rotation, or input permissions. Source review/build evidence does not establish real-device mapping correctness.

## Observed Code Limitations — Not Approved Fixes

The dated 2026-06-08 review reported the following findings. Read-only source inspection on 2026-10-07 still supports them. Earlier documentation incorrectly described the first two as working behavior; this consolidation corrects the descriptions without changing code or adding active fix tasks.

| Finding | Evidence and current behavior |
| --- | --- |
| Unknown actions become left clicks | `ScriptRunner.NormalizeAction` and `ScriptConfigRepository.NormalizeAction` default unrecognized actions to left. A config typo can produce a tap rather than a logged no-op. The old recommendation was preserving unknown input and planning a logged no-op. |
| Directional drags lose direction | Repository normalization converts leftdrag/rightdrag/updrag/downdrag to generic drag; `ScriptRunner.GetDragEndPoint` uses the start coordinate when no explicit endpoint is supplied. RandX/RandY-based directional derivation was a recommendation, not implemented behavior. |
| Invalid hotkey text may use defaults | `HotKeyManager.TryParseHotkey` returns true after nonempty input with no recognized key, retaining default key/modifier values. The old recommendation was rejecting/validating invalid text. Empty input correctly disables the hotkey. |

## Historical Implementation And Verification Evidence

### 2026-06-08 — Main Window Review

The original dated review covered fixed client layout, seven-row action column/spacer, compact Skip popup/details, infinite/final-loop skip exclusions, flattened Run Plan Skip, countdown/timeline, and normal system arrows for Offset/Tag/Device. Reported verification at that time: `dotnet build Lazy_App_Codex_Core.sln --no-restore` succeeded with zero warnings/errors; `git diff --check` had no errors, with a normal CRLF warning. That dated result does not validate later changes.

### Earlier Track Touch, Delay, And Device-Name Work

The session implemented the dedicated tracker, point-only history, live drag coordinates, X/Y tests, device switching/friendly names, Delay/wait normalization, leading Delay offset handling, and Enter-to-Try. Historical session memory records an earlier successful build, followed by a final build blocked by the running `Lazy App.exe` after the friendly-name change (`MSB3027`/`MSB3021`). No later final build or real-device/manual-test success is established by that record. This is preserved evidence, not an acknowledgement task in Discussion.

The earlier documentation handoff updated seven living documents and left the dated review unchanged. Its recorded diff check passed. The present consolidation carries that useful content into the four approved records.

## Optional Manual Check Reference

These checks are available when the user requests testing or reports a bug. They are not an active acknowledgement queue or permission for Codex to launch/control the application or devices.

- Main UI: fixed one/two-set layout, Run/Stop size stability, hotkey/taskbar colors, Alt+1/2/3 and Escape, runtime Offset item population after designer rewrites, readable Skip popup, countdown/timeline while active.
- Run/config: independent cancellation and selected devices, no-device gating/loss notification, correct finite Skip, exact Run Plan order/repeats, min/max/emin timing, leading Delay offset, staged save/close/restore behavior, stable IDs, hidden references, tags plus blank entries.
- Wireless/devices: Manual Input clearing, saved IP prefill, numeric/IP validation, Pair vs Connect status, Restart Server refresh, Enter-to-Try, names/conflict highlighting, disconnected rename/delete and ready-only Sync.
- Tracker: matching-event/display mapping on the selected device, friendly names and safe switch, live points/drags, completed-point-only history, double-click/test tap, process stop on switch/loss/close.
