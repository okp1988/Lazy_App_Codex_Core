# Progress

## Record Purpose

This is the approved project reference and implementation/verification history consolidated on 2026-10-07. Global working rules take priority; project constraints are maintained in [AGENTS.md](../AGENTS.md). This record requires explicit approval for updates. [Discussion](DISCUSSION.md) contains active work and [External Folders](EXTERNAL_FOLDERS.md) records external roots.

Implementation completion, build results, review findings, and manual-test evidence are recorded separately. Completed work does not stay in Discussion waiting for acknowledgement; silence is not a passed test result. Known limitations below are reference findings, not new approved bug-fix tasks. User-invoked documentation checkpoints and discussion cleanup reconcile completed work below; current behavior sections also serve the requirements role without adding another Markdown file. The latest checkpoint is 2026-10-09.

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
| `Program.cs` | Isolated `--check-adb-snapshots` and `--check-layout` branches before normal logging/config startup; otherwise logging, exception handlers, WinForms initialization, main window. |
| `Form1.cs` | Config reload, runnable/tag/offset/Remaining selection, two independent run slots and captured run state, completion beep, hotkeys, live status, taskbar overlay, fixed client sizing, ADB monitor/shared-device selection/event-only auto-assignment, opening Config and Pair / Connect. |
| `Form1.Designer.cs` | Designer shell containing `mainLayout`; repeated run UI belongs to RunSetControl. |
| `RunSetControl.cs` / `.Designer.cs` / `.resx` | Shared run-set control; explicit designer controls plus runtime layout, sizing, and dropdown population. Set 2 hides shared Config, Pair / Connect, and status controls. |
| `ConfigEditorForm.cs` | Staged Settings, Devices, Offset, Scripts, Sequences, and Run Plans editing; save/backup/restore and owned Track Touch window. |
| `TrackTouchForm.cs` | Friendly-device touch tracker, mapped live coordinates and diagnostics, completed-point history, coordinate test tap, tracker lifecycle. |
| `WirelessAdbConnectForm.cs` | Manual Pair, Connect, and Restart Server, input validation, command output, saved Wi-Fi serial updates, Enter-to-Try and Escape close. |
| `SearchableDropdown.cs` | Main runnable picker; popup search and selected-item highlighting. |
| `SkipPickerControl.cs` | Retained unused legacy Skip field/popup source; removal was not authorized. |
| `CenteredNumericUpDown.cs` | Vertically centers the Remaining native text editor while preserving outer size and spinner behavior. |
| `RunSetLayout.cs` | Shared single-/dual-panel composition, gap, and client sizes for Form1 and diagnostic fixtures. |
| `LayoutCheckRunner.cs` / `tools/check-layout.ps1` | Isolated main-panel geometry/text-fit diagnostics and primary-Debug launcher/report display. |
| `AdbDeviceSnapshotReader.cs` | Complete tracker snapshots, including empty snapshots; LF-byte framed lengths with CRLF-expanded output, split-read buffering, plain mode, and no partial per-row device-loss updates. |
| `AdbSnapshotCheckRunner.cs` | Opt-in synthetic in-memory parser regression checks, console output only; no normal app/UI/log/config/ADB startup. |
| `CountdownProgressControl.cs` | Fixed-size wait progress and caption. |
| `ScriptConfigRespository.cs` | Config loading, migration, normalization, and saving. The filename intentionally retains the existing `Respository` spelling. |
| `ScriptModel.cs` | ConfigLibrary, ScriptModel, ActionGroup, SequenceModel/SequenceItem, RunPlanModel/RunPlanItem, and StepAction. |
| `ScriptRunner.cs` | Cycle planning, expansion, randomized timing, Delay, offsets, runtime cycle count/Infinity, internal Plan tail skipping, completed-cycle callbacks, ADB execution, cancellation, and live progress. |
| `AdbShellController.cs` | ADB command wrapper and captured Pair/Connect/Kill/Start Server helpers; physical pixel coordinates. |
| `HotKeyManager.cs` | Parsing, independent primary/secondary registration, unregistration, and WM_HOTKEY routing. |
| `OffsetDisplayOption.cs` / `MouseHelper.cs` | Offset choices and coordinate randomization. |
| `AppLogger.cs` | Daily logs at `AppContext.BaseDirectory\logs`; startup deletes logs older than seven days. |

Runtime flow: working-directory `config.json` loads into ConfigLibrary; Form1 builds/filter/selects runnable targets and validates the selected ready device; each Start captures the run's target/library, offsets, device, timing, and options. ScriptRunner expands/plans cycles, enforces timing, sends ADB commands/live status, and reports completed cycles. Completion/cancellation restores editable controls and the title; manual Stop retains Remaining, while normal finite completion reloads its configured default. Each slot owns its own task and cancellation token.

## Main Window And UI Reference

- Set 1 is always visible and uses primary hotkeys. `Alt+1` toggles Set 2, which uses optional backup hotkeys only while open. `Alt+2` opens Config and `Alt+3` opens Pair / Connect.
- Escape stops runs from the main window, follows the Config close/save flow, and closes Pair / Connect.
- Scripts display as `[S] NAME`, Sequences as `[Q] NAME`, and Run Plans as `[P] NAME`.
- Runnable picker search belongs to its popup, clears on close, and highlights the current item when it is still present. The closed field displays the selected item only.
- Main client size is `606 x 292` for one set and `1200 x 292` for two. RunSetControl is `594 x 284`, with content width `410`, action width `184`, and set gap `12`. Outer size derives from `SizeFromClientSize(...)` so DPI-dependent chrome does not squeeze usable content.
- Resize/maximize are disabled. Run/Stop does not change size; only Set 2 visibility changes the client size. Form1 and RunSetControl use `AutoScaleMode.None` for this fixed profile.
- The action column uses seven fixed `34px` rows: Run, Remaining / Infinity, Offset, Tag, Device, Config, Pair / Connect, followed by an explicit spacer. Extra height goes to the spacer.
- Visual Studio 2026 previously rewrote scaling metadata, combo item heights, helper-created status rows, and offset items. Explicit controls stay in the designer; stable pixel layout and runtime item population stay in RunSetControl.cs after `InitializeComponent()`.
- Remaining supports typing and native up/down buttons. Its label uses a baseline width of 70 pixels, widening only when its current font requires it; the number field gives up that width while retaining its right edge at 134 and Infinity at X=140. Layout/font/handle/parent-DPI changes recalculate this row. CenteredNumericUpDown vertically centers the native editor without changing the outer control, right alignment, or spinner/input behavior. There is no active Skip popup or inline skip explanation.
- Alternate DPI/font/screen layouts need an explicit guarded profile preserving the existing baseline. Comparisons use user-provided screenshots or separately approved capture.
- Config button rows need explicit TableLayoutPanel heights, docked buttons, and bottom padding to avoid clipping.
- Each run slot shows action, step, cycle, next action/time, estimated end, a six-chip timeline, and countdown progress. The main clock timer runs only while a slot is active; ADB/device errors appear in the affected status panel.
- Offset choices are No Offset, Y up/down one through six steps, and X left/right one through three steps.

Hotkey status is separate from ADB status: gold means primary and secondary registered, green primary only, blue secondary only, and red none. The taskbar overlay mirrors hotkey state and shows one or two run-set identifiers. Primary/backup registration failures do not disable the other successful group. Matching start/stop hotkeys use one registration and toggle that set. Minimize unregisters hotkeys; restore/activation registers them, with UI/warning logging.

### Layout Check Mode And Coverage

- The approved checker is an opt-in `--check-layout` branch in the existing executable before AppLogger initialization. It creates/disposes hidden neutral native-control fixtures without constructing Form1, showing/activating the normal app, capturing the desktop, loading live config, registering hotkeys, calling ADB, or changing global display/settings. It adds no package/test project, alternate build output, or CI integration.
- RunSetLayout shares production composition/client sizes with the fixtures; the fixture's font, padding, and window settings mirror Form1 but do not test full Form1 behavior. Each case checks idle/running/infinite fixtures: labels/buttons, native number digits/centering/spinner, Remaining/Infinity separation, control containment, selector/dropdown/countdown text heights, six timeline chips and their separation. Variable-value/long-name horizontal ellipsis is allowed, but fixed captions and all label heights are checked.
- Eight labelled synthetic font/geometry cases cover nominal 96/120 DPI, resolutions 1920 x 1200 / 1920 x 1080, and one-/two-panel modes. Native-host cases cover both panel modes on each actual monitor and report effective DeviceDpi/font/working area. Synthetic font enlargement is not genuine Windows 125% rendering; synthetic resolution fit uses the client envelope, not simulated taskbar/chrome.
- Unique report JSON files are written under `AppContext.BaseDirectory/layout-check`; the wrapper uses the established `bin/Debug/net8.0-windows` executable output. Reports include runtime/Windows versions and the main assembly's SHA256 build identity so different runs can be compared against the same build.
- User-provided profiles: laptop displays 1920 x 1200 and 1920 x 1080 at 125% Scale; PC displays the same resolutions at 100% Scale; Windows Text Size is 100% on both. The user reported both UIs working after the fetched-version rebuild. That report is distinct from automated coverage and does not establish all visual/input behavior.
- The checker automates assertions once invoked; it is not automatically attached to every build or launched on both machines. Native laptop coverage needs the same checker to run on that host, not manual inspection of every control. Automatic build-trigger/two-machine orchestration remains an unconfirmed, unimplemented discussion proposal (D-001); it is not an ongoing instruction or approval.
- Recorded limitations: no shown-window pixel comparison, laptop-native 125% evidence, popup/Config/Pair/Track Touch coverage, input testing, or real monitor DPI-transition checks. The PowerShell wrapper was blocked by script execution policy, which was not changed; direct executable diagnostics succeeded. Commands below are reference only and do not grant new launch/test permission:

```powershell
# From the project root after an approved primary build:
.\tools\check-layout.ps1 -SkipBuild
# If scripts are blocked, do not change policy automatically; the executable supports:
Start-Process '.\bin\Debug\net8.0-windows\Lazy App.exe' -ArgumentList '--check-layout' -WindowStyle Hidden -Wait
```

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

### Remaining Count, Infinity, And Run Isolation

- Remaining is runtime state, not a saved-config rewrite. Explicit Script/Sequence/Plan selection, including reselecting the same target, loads its configured count. Ordinary Start/Stop and automatic refresh preserving the same kind/stable ID retain it; app-restart persistence was not requested.
- Finite manual input is 1–99. Saved defaults above 99 are capped in the runtime UI only; saved data is unchanged. Normal finite completion reaches transient zero, automatically stops, then reloads the configured default (subject to the same cap). Manual Stop does not reload it.
- One asynchronous system beep per panel follows its entire normal finite Script, Sequence or Run Plan completion at zero after stopping and default reload. Do not beep per cycle/Plan item, manual Stop, cancellation, device loss, error, Infinity or app closing. Form1's completion path checks cancellation/loss/closing, calls `SystemSounds.Beep.Play()` once, and logs sound failure without failing the completed run. Windows sound settings govern audibility; live sound behavior remains unverified.
- Decrement once per fully completed cycle, including its waits. An interrupted cycle is not deducted and restarts on the next Start. Count/Infinity are disabled during running, but the displayed finite count still updates programmatically; editing returns after Stop.
- Saved non-positive Script/Sequence duration means Infinity and automatically checks the checkbox; positive configured counts uncheck it. The user may check before Start. Infinity does not decrement; unchecking it from zero restores the last positive value or 1. Run Plans have no requested infinite mode.
- Plan counts mean the sum of individual target repeats, not whole-plan repetitions. Input maximum is `min(total, 99)`; an empty/zero-cycle Plan has no runnable positive count. Execute the final N cycles in configured flattened order using internal skip = total minus Remaining. Plan 26 with total 20 and Remaining 5 skips 15 and runs cycles 16–20; after one completion Remaining 4 resumes cycles 17–20 after Stop/Start. Increasing/decreasing Remaining selects a longer/shorter tail without wrapping.
- Config and Pair / Connect stay enabled during runs, with existing modal ownership/focus unchanged. Dialogs use separate repositories; active runs retain captured targets/library, offsets, device serial, timing, and run-control options. Stopped slots read saved changes without rebuilding active slots; hotkey remapping waits until both slots stop.
- Only Restart Server is disabled while either slot runs, with an execution-time guard as well as button availability. Restart affects the server globally and would otherwise trigger device-loss/error cancellation.
- Each slot has independent count, target, token/task, and device state. Preserve action/interval/emin timing, offsets, cancellation, ADB OFF, saved configuration, and stable-ID restoration outside these confirmed changes.

## ADB, Devices, And Wireless Helper

ADB status colors: dark gray no server/unavailable tracker, red server with no ready device, green one ready device, yellow multiple ready devices. Dark gray alone does not prove the server stopped. A ready row has ADB state `device`. Each active dropdown contains all ready devices plus `No Device`; both panels may manually select the same device. Counts/tasks/tokens and captured running serials remain independent, but commands on one phone can interleave; no phone-level queue or synchronization is requested. `No Device` permits an empty panel, including Set 1 while Set 2 runs S26. If X13 disappears from Set 1, clear/stop only that affected slot; keep S26 selected/running in Set 2 without transferring it. If both slots use a disappearing phone, each is affected independently. Friendly names come from settings; Wi-Fi display omits port while commands retain the full serial.

Compare prior/current ready serials before replacing cached status. A newly-ready event (including reconnect/offline-to-ready) fills only one visible, idle empty panel, including manually chosen No Device: Set 1 first, otherwise visible Set 2. Existing selections and running assignments stay unchanged; hidden stopped Set 2 is ineligible. Startup/simultaneous arrivals choose the first listed newly-ready device, not a claimed reliable connection timestamp. Ordinary metadata/config/Stop/visibility refreshes do not auto-fill either empty panel. Do not fill both panels with the same arrival automatically; shared manual selection is allowed. This D-009 decision supersedes the old cross-panel reservations/exclusions described in dated history.

Monitoring starts with localhost port 5037. Only an already-listening server starts `adb track-devices`; the local TCP probe is bounded by 600ms. When the server/monitor is unavailable (NoServer), the existing recovery timer retries every 10 seconds; Run/Config and Wireless helper close/restart can request recovery sooner. This is not a regular device-list polling interval: healthy device changes stream continuously, and a healthy status stops the retry timer even for a valid empty list. Config opening is not blocked by status checks. Run refreshes status before target validation and trusts cached no-device status only with a healthy monitor; an unhealthy monitor attempts recovery regardless of the old cached state. A running slot cancels and notifies if its selected device disappears. If a healthy tracker starts but no initial block arrives before Run timeout, status is red rather than dark gray.

Tracker reuse requires both a live process and an active stdout reader task that is not marked stopped. Reader EOF/error or process exit marks the monitor unavailable on the UI thread so existing recovery paths can replace the app-owned tracker even if its process remains alive. Stopped/replaced tracker callbacks are rejected, and the initial-status wait uses current status rather than an older first-snapshot result; terminal failure must not be overwritten by stale success. This does not restart the ADB server, reconnect the phone, or prove that every stalled-but-still-active stream is detected.

Status is driven by complete track-devices output snapshots (including empty snapshots), stdout close/reader error, process exit, and the existing NoServer recovery timer; stderr is diagnostic logging, not by itself a status transition. AdbDeviceSnapshotReader avoids applying partial per-row device lists. Framed payload lengths count logical UTF-8/LF bytes even when Windows ADB stdout expands LF to CRLF; the reader consumes the actual expanded bytes and buffers an unresolved trailing CR across feeds before publishing a complete frame. Preserve LF-framed, CRLF/mixed-framed, plain and empty snapshots. No separate health poller or background wireless reconnect/port scan was added. Monitor/gating decisions use `LogAdbWarning(...)`, prefix `[ADB]`, and `AppLogger.LogWarning`, including trigger/start/reuse/recovery, process/reader state, port result, complete snapshot rows/ready count/state, stdout EOF/error byte counts, stderr/exit, and allow/block decisions. A shorter retry interval is expected to have low cost from source inspection, but persistent tracker failures may cause repeated process starts/logging; overhead has not been measured.

The opt-in `--check-adb-snapshots` branch runs synthetic in-memory fixtures through the production parser before normal app/log/config/UI/hotkey/ADB startup and prints console results without report/data files. Recorded execution: `dotnet '.\bin\Debug\net8.0-windows\Lazy App.dll' --check-adb-snapshots`, exit 0, 196 cases / 4,073 assertions. This is parser-only evidence, not live reconnection/UI responsiveness proof or permission to execute again. The reported temporary input freeze remains unresolved in Discussion; the shared command-wait cancellation weakness is not approved for repair.

Manual command reference:

```text
adb devices
adb pair <IP_ADDRESS>:PAIR_PORT
adb connect <IP_ADDRESS>:CONNECT_PORT
adb disconnect <IP_ADDRESS>:PORT
adb start-server
adb kill-server
```

Pair authorizes the computer; Connect uses the phone's current connect port, which may change. The helper has Pair/Connect actions, Manual Input at device index 0, saved Wi-Fi IP prefill, fixed dot-separated IPv4 segments (0–255), numeric Port and Pair Code, Try, and Restart Server. Pair Code is shown for Pair. Manual Input clears IP/Port. Enter invokes Try for the selected action; Escape closes. Restart runs kill-server then start-server and is blocked while either main run slot is active; Pair/Connect remain available. Results show captured output; successful Connect updates lastSerial/lastSeen and main ADB status, while Pair success alone does not mark connected. Display/status text uses Title Case except user-entered values.

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

### 2026-10-07 — Remaining Count And Independent Run State (D-002)

- Implemented the confirmed Remaining/Infinity, Plan final-N cycles, explicit-reselection/default reload, finite-completion reload, manual-stop retention, completed-cycle deduction, saved-config preservation, accessible Config/Pair, frozen active runs, Restart Server guard, No Device selection, and complete tracker snapshot requirements recorded above.
- Connected source: Form1.cs (`RefreshRemainingCount`, `OnRunCycleCompleted`, Start cleanup/captured options, config/device refresh), RunSetControl.cs / Designer, SearchableDropdown.cs (`SelectionCommitted` including same-item selection), ScriptRunner.cs (`RunExecutionOptions` / completed-cycle callbacks / Plan skip), WirelessAdbConnectForm.cs, and AdbDeviceSnapshotReader.cs. Retained SkipPickerControl source was not deleted.
- Independent regression review identified simultaneous idle auto-assignment collisions and stale device lists; coordinated exclusions, list reconciliation, and reserved-device selection guards were added and re-reviewed with no remaining concrete in-scope issues. Those exclusive-device policies were later explicitly superseded by D-009; this is historical evidence, not the current selection rule.
- Recorded checks at implementation: primary `dotnet build Lazy_App_Codex_Core.sln --no-restore` passed with zero warnings/errors; `git diff --check` passed with line-ending notices only. Source review covered persistence/completion/cancellation, Plan tails, active-run snapshots, device assignment/framing, and Restart Server restrictions. No GUI/device test, runtime automation suite, or acceptance was performed/inferred. Status: Implemented; limitations are not acknowledgement tasks.

### 2026-10-07 — Remaining Editor Centering And Label Width (D-003, D-004)

- D-003: CenteredNumericUpDown.cs and the Remaining Designer type center the native editor after layout/font changes with recursion guards. Outer dimensions, right alignment, typing/spinner controls, count/Infinity logic, and unrelated UI were preserved. Primary build and diff check passed; independent source review found no concrete regressions. No live visual/input/DPI-transition test was performed at that stage.
- D-004: RunSetControl measures Remaining's preferred label width after layout/handle/font/parent-DPI changes, preserving the baseline where it fits, numeric right edge, Infinity position, designer values, and fixed client sizes. Primary build/scoped review/diff check passed with zero build warnings/errors. No app launch or device-specific visual confirmation was performed at that stage. Both changes are Implemented, not awaiting acknowledgement.

### 2026-10-07 — Fetched-Version Rebuild And User Report (D-005)

- User fetched another GitHub version and overwrote prior Discussion Markdown, then requested a rebuild first. The current checkout/notes were preserved; no old notes, source/config changes, or Git mutations were restored.
- Both `dotnet build Lazy_App_Codex_Core.sln --no-restore` and `dotnet build Lazy_App_Codex_Core.sln --no-restore -t:Rebuild` passed in the primary Debug output with zero warnings/errors. The app was not launched for those commands. The user subsequently reported both PC/laptop UIs working; this is user-reported layout evidence, not all-behavior acceptance or an automated laptop-native result.

### 2026-10-07 — Main-Panel Layout Checker (D-006)

- Implemented the bounded, opt-in checker and shared unchanged composition in Program.cs, LayoutCheckRunner.cs, RunSetLayout.cs, Form1.cs, and tools/check-layout.ps1. No dependency/test project, CI/Git changes, alternate build output, remote setup, global settings change, or live config/log/ADB operation was made.
- Independent impact review established that RunSetControl can be instantiated without normal Form1 startup. Post-change review found gaps for ellipsized-label heights, fixed-caption widths, native dropdown/countdown text heights, and fixture font disposal; checker-only corrections were applied. Re-review found no blocking issues or normal-startup/UI behavior regressions.
- Recorded final primary build: zero warnings/errors; `git diff --check` passed. Direct hidden diagnostic execution exited 0 in about two seconds, passing all 12 cases / 5,694 assertions: four native cases (two local monitors, both panel modes) at effective 96 DPI, plus eight labelled synthetic cases at nominal 96/120 DPI. This is geometry/text-fit evidence, not shown-window pixel or input testing.
- Final report: `bin/Debug/net8.0-windows/layout-check/report-20261007-045139-814-af1b39275bc34b58ab7126921bb4ff1b.json`; assembly SHA256 `6F29A51F26AD06B14294810ED99753A8588B225AE9A6FF6658FD4E6DBB838AD2`. PowerShell parsing had zero errors, but wrapper execution was blocked by script policy and remains unverified; no policy was bypassed or changed. Laptop-native 125%, other windows/popups, input, shown pixels, and monitor DPI transitions remain unverified. Status: Implemented, with these separate limitations.

### 2026-10-07 — Wireless Monitor Recovery And 10-Second Retry (D-008)

- User reported the phone absent from the opened Device dropdown for about one minute after Wireless Debugging reconnect; closing/reopening Lazy App made it appear. The ADB status dot color is unknown. This was an absent row, not merely deliberate No Device selection retention. Slot reservations were preserved at that implementation stage, then explicitly superseded by D-009.
- Source investigation and independent impact review established a dead-reader/live-process recovery gap: reader EOF/parser failure could mark NoServer while Ensure kept reusing the still-live process. Available logs did not establish this as the original incident's cause, and the reconnect was not reproduced.
- Separately approved diagnostic before the fix: `C:\adb\adb.exe -H 127.0.0.1 -P 5037 track-devices` against the already-running local server for 5,039ms. Stdout was exactly four bytes `30-30-30-30` (`0000`), with no newline/stderr. The user confirmed the device list genuinely was empty, so the frame was expected/correct. The newline-framing hypothesis was withdrawn. Only that temporary tracker was started/stopped; no capture file, app/server restart, connection change, or phone input was performed. This diagnostic was not a post-fix test.
- User approved the narrow recovery/logging fix. Connected Form1.cs paths: `_adbTrackReaderTask` / `_adbTrackReaderStopped`, `IsAdbTrackMonitorHealthy`, `EnsureAdbTrackMonitorAsync`, `RefreshAdbStatusForRunAsync`, `ReadTrackDeviceSnapshotsAsync`, `MarkTrackProcessStopped`, `ApplyTrackedStatus`, `WaitForTrackDevicesStatusAsync`, and `StopTrackDevicesProcess`. Reuse/recovery now accounts for reader health, safely detaches old tracker ownership, rejects stale callbacks, and logs relevant monitor/snapshot/failure state. Complete/empty snapshots, healthy cached NoDevice rejection, independent run slots, intentional empty selections, UI/config/dependencies and parser format were preserved. No new polling, automatic reconnect, or ADB server restart was added.
- Independent regression review found an older first-snapshot task result could overwrite a newer terminal failure and stop recovery. The wait now rechecks monitor health and returns current status; final independent source re-review reported no remaining actionable in-scope issues. Primary `dotnet build Lazy_App_Codex_Core.sln --no-restore` after the correction passed with 0 warnings / 0 errors; `git diff --check` passed.
- User subsequently approved changing only the existing retry interval from 30 to 10 seconds. `_adbRetryTimer.Interval` is 10000; normal streamed updates, timer gating and recovery logic were unchanged. The same primary build passed with 0 warnings / 0 errors, and source-value/diff checks passed. No separate independent review was run for this one-line interval adjustment.
- Status: Implemented. No post-fix app launch, automated/runtime test, layout checker, or live ADB/device test was run, and no manual success/acceptance was inferred. Original incident cause/reproduction and real reconnect recovery remain unverified; these limitations are not acknowledgement tasks. Earlier unrelated dirty changes were preserved.

### 2026-10-09 Checkpoint — CRLF Tracker Parser Correction (D-008 Completed Subitem)

- Subsequent incident: Panel 1 ran X13; enabling/connecting S26 Wireless Debugging led to a temporary input freeze and Panel 1 becoming No Device. `logs/app-2026-10-07.log` lines 60–66 record invalid length prefix after 178 stdout bytes at 23:07:19, selection clearing/run stop, then a two-ready-device snapshot after 10-second recovery at 23:07:29. Earlier failures had 125/126 bytes. This supports parser failure/stop behavior, not a confirmed whole-window freeze mechanism.
- Separately approved approximately five-second in-memory capture against the existing localhost:5037 server used `C:\adb\adb.exe -H 127.0.0.1 -P 5037 track-devices`. The first bounded attempt's extra PowerShell return value prevented result parsing; the same capture was repeated with the wrapper corrected. Successful capture lasted 5,024ms, stdout 115 bytes, empty stderr, temporary tracker confirmed stopped. Only temporary diagnostic trackers were started/stopped; no capture files, server/app restart, connect/disconnect, phone input or other process stop. This approval was diagnostic-only, not implementation or shared command-wait repair.
- Two ready rows had header `006d` declaring 109 bytes, but the CRLF-expanded payload was 111 bytes; LF normalization gives exactly 109. The old reader consumed raw declared length, leaving `0D 0A` for the next prefix. This establishes the nonempty-frame format mismatch, unlike the earlier valid empty-only capture; simply skipping separator bytes could truncate a final device row in larger frames.
- User subsequently approved the focused parser correction and isolated regression entry point/checks. Implemented: AdbDeviceSnapshotReader counts logical UTF-8 payload bytes while consuming CRLF-expanded output, buffers split trailing CR, and emits only complete snapshots. Program.cs connects `--check-adb-snapshots` to AdbSnapshotCheckRunner.cs before normal startup. Synthetic fixtures cover captured 109/111-byte shape, every two-chunk split, LF/CRLF/mixed framing, five-device lists, UTF-8, empty/adjacent frames, plain mode, malformed/truncated input and feed-after-completion.
- Recorded implementation checks: primary `dotnet build Lazy_App_Codex_Core.sln --no-restore` passed with 0 warnings / 0 errors; `dotnet '.\bin\Debug\net8.0-windows\Lazy App.dll' --check-adb-snapshots` exited 0 with 196 cases / 4,073 assertions; independent source regression review found no actionable issues; diff check passed with line-ending notices only. No normal app/UI/device test or new post-fix capture was performed. These checks were not rerun by this documentation checkpoint.
- Status: parser subitem Implemented / Closed, not awaiting manual acknowledgement. Selection/loss policy, Form1/runner, 10-second retry and shared WaitForExit were unchanged by that parser fix. Later D-009 changes are separate. User subsequently clarified buttons/shortcuts were inert temporarily, no ADB Device Removed warning was seen, and recovery possibly coincided with both devices reconnecting. Input-freeze cause and shared command cancellation remain unresolved/not approved in Discussion; parser correctness does not prove responsiveness or live recovery.

### 2026-10-09 — New-Device Auto-Assign, Shared Selection And Completion Beep (D-009)

- User confirmed all ready devices stay selectable in both panels and the same phone may be selected/run in both; actions can interleave. New-ready auto-assignment fills only one visible idle empty panel, including a manual No Device choice: Set 1 first, otherwise Set 2. Startup/ties use the first listed newly-ready device. Existing/running assignments stay unchanged, hidden stopped Set 2 is ineligible, and cached metadata/config/run-state/visibility refreshes leave empty selections alone. The earlier fill-both-panels proposal was rejected; no phone-level serialization was requested.
- User confirmed one beep per panel after its whole finite Script, Sequence or Run Plan reaches zero and stops, not per cycle/Plan item or on manual Stop, cancellation, device loss, errors, Infinity or closing. Finite completion still reloads configured count. Explicit implementation approval covered narrow Form1 changes plus primary build/independent source review, not live UI/device/audio tests or other pending work.
- Connected Form1.cs: SlotDeviceChanged and UpdateDeviceDropdown/ForSlot remove reservation/rejection/filtering; ApplyAdbDeviceStatus compares ready serials before cache replacement; only a newly-ready event selects one eligible empty panel. Captured full serials, per-slot task/count/loss handling, parser and 10-second recovery stay intact. StartRunAsync central completion guards and one `SystemSounds.Beep.Play()` after cleanup cover the entire target; sound exceptions are logged without failing completion. No audio asset/dependency or synchronization queue was added.
- Recorded checks: `dotnet build Lazy_App_Codex_Core.sln --no-restore` passed, 0 warnings / 0 errors; independent source regression review found no actionable issues; `git diff --check` passed. No automated test execution, normal app launch or live UI/device/audio check was performed. Windows sound settings govern audibility. Status: Implemented / Closed with live behavior unverified, not an acknowledgement gate; silence is not a passed test or acceptance.

### 2026-10-09 — Documentation Checkpoint And Discussion Cleanup

- Explicit cleanup-discussion/doc-checkpoint invocation authorizes updates to the existing AGENTS, Progress and Discussion; no additional Markdown or structural migration. Saved completed D-009 and the separable D-008 capture/parser evidence above before clearing eligible Discussion details. Reconciled current device-sharing/auto-assignment/beep and parser/checker references; preserved dated implementation history and unperformed live-test limitations.
- Retain D-008 temporary input-freeze investigation (cause unconfirmed; timing logs proposed, not approved) and D-001 automatic PC/laptop workflow (proposed/deferred; no orchestration approval). No unfinished item is dropped, and no acknowledgement-only retest task is added. External Folders is unchanged because existing roots/subpaths cover this work. Documentation-only: no build, test/app/device/audio execution, external write, network operation or Git mutation; pre-existing source edits are preserved.
- Documentation checks: source/config hashes unchanged from checkpoint start; eight local links and four-file inventory passed; `git diff --check` passed with line-ending notices only. Independent Documentation / Consistency review found no actionable corrections and confirmed requirement/source alignment, historical supersession, transferred parser/completion evidence and retention of both unfinished items. No implementation/runtime verification was rerun for this checkpoint.

### 2026-10-07 — Documentation Checkpoint And Discussion Cleanup

- User invoked cleanup-discussion and doc-checkpoint. Requirements/reference and historical evidence for D-002–D-006 were saved and re-read before completed Discussion entries were removed. AGENTS current-state descriptions were reconciled, and External Folders recorded the skill-definition subpaths. No new Markdown file or structure migration was introduced.
- Verification: independent documentation re-review found no remaining concrete inconsistencies; seven local Markdown links and the four-file inventory passed; `git diff --check` passed with line-ending notices only. Application/checker source hashes remained unchanged during cleanup, and existing unrelated/source edits were preserved. No build, application/checker test execution, GUI/device interaction, network operation, or Git mutation was performed for this documentation task. D-001 remains proposed/deferred in Discussion for the user's Keep/Remove decision; no unfinished item was silently dropped or manual-test acknowledgement task added.
- Later same-day invocation transferred D-008 and its approved 10-second retry follow-up into the current ADB reference/history above, then removed the completed Discussion entry only after saving/re-reading it. AGENTS recovery/cache/status/logging descriptions were reconciled; External Folders was unchanged because existing entries already cover the tooling paths. Seven local links, the four-file inventory, source comparison across 29 files, and diff checks passed. Independent documentation review confirmed complete transfer and preserved D-001; its timeout wording correction was applied so red fallback requires a still-healthy tracker. No new build/runtime/device check or source change was made by this checkpoint; original incident/live-test limitations and proposed/deferred D-001 were preserved.

### 2026-06-08 — Main Window Review

The original dated review covered fixed client layout, seven-row action column/spacer, compact Skip popup/details, infinite/final-loop skip exclusions, flattened Run Plan Skip, countdown/timeline, and normal system arrows for Offset/Tag/Device. Reported verification at that time: `dotnet build Lazy_App_Codex_Core.sln --no-restore` succeeded with zero warnings/errors; `git diff --check` had no errors, with a normal CRLF warning. That dated result does not validate later changes.

### Earlier Track Touch, Delay, And Device-Name Work

The session implemented the dedicated tracker, point-only history, live drag coordinates, X/Y tests, device switching/friendly names, Delay/wait normalization, leading Delay offset handling, and Enter-to-Try. Historical session memory records an earlier successful build, followed by a final build blocked by the running `Lazy App.exe` after the friendly-name change (`MSB3027`/`MSB3021`). No later final build or real-device/manual-test success is established by that record. This is preserved evidence, not an acknowledgement task in Discussion.

The earlier documentation handoff updated seven living documents and left the dated review unchanged. Its recorded diff check passed. The present consolidation carries that useful content into the four approved records.

## Optional Manual Check Reference

These checks are available when the user requests testing or reports a bug. They are not an active acknowledgement queue or permission for Codex to launch/control the application or devices.

- Main UI: fixed one/two-set layout, Run/Stop size stability, hotkey/taskbar colors, Alt+1/2/3 and Escape, runtime Offset population after designer rewrites, readable/centered Remaining and Infinity, countdown/timeline while active. The bounded checker covers only the documented geometry/text-fit subset when execution is separately approved.
- Run/config: Remaining/Infinity defaults/reselection/manual-stop/completion, fully completed versus interrupted cycles, Plan final-N order and count caps, independent cancellation/devices including empty Set 1, config edits isolated from active runs, no-device gating/loss notification, exact Plan order/repeats, min/max/emin timing, leading Delay offset, staged save/close/restore, stable IDs, hidden references, tags plus blank entries.
- Wireless/devices: Manual Input clearing, saved IP prefill, numeric/IP validation, Pair vs Connect status, Restart Server refresh, Enter-to-Try, names/conflict highlighting, disconnected rename/delete and ready-only Sync.
- Tracker: matching-event/display mapping on the selected device, friendly names and safe switch, live points/drags, completed-point-only history, double-click/test tap, process stop on switch/loss/close.
