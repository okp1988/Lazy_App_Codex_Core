# Discussion

## Active Work

### D-008 — Temporary Input Freeze — Unresolved / Diagnostics Proposed

- Completed recovery/10-second retry and the separable capture/parser correction are saved in [Progress](PROGRESS.md). Parser implementation and checks are closed, not awaiting acknowledgement; live post-fix reconnection/UI responsiveness is unverified.
- Remaining report: Panel 1 ran X13; enabling/connecting S26 Wireless Debugging was followed by Panel 1 changing to No Device and temporary input unresponsiveness. User clarified buttons/shortcuts had no effect, later returned to normal, possibly when both devices reconnected; no ADB Device Removed warning was observed. Reconnect causality and the freeze mechanism remain unconfirmed; no permanent deadlock or post-parser-fix recurrence is established.
- The 2026-10-07 log records parser failure, selected-device clearing/stop and recovery about ten seconds later; monitor failure is not proof a phone disconnected. Keep complete/empty snapshots, genuine loss handling, independent captured run assignments and the existing 10-second recovery. D-009 sharing/auto-assignment changes are documented separately in Progress.
- Source candidate: AdbShellController.RunCoreAsync awaits a worker delegate blocking in WaitForExit; cancellation does not interrupt an already-started delegate, so Stop can remain pending. StopRunAsync awaits asynchronously and the delegate is off the UI thread, so that defect alone does not explain all input failing. Shared ADB command-wait repair affects other consumers and is not approved.
- No warning was observed, so do not present a hidden modal device-loss warning as the cause. Synchronous metadata/config/log I/O remains an unproven candidate; the WinForms recovery timer fired during the logged interval. Parser correctness/build success does not establish UI responsiveness.
- Proposed next scope, not approved: bounded timing logs through existing logging around device-list application, run-stop completion and metadata/config work, without changing run/selection/connection behavior. Proposed verification is primary build/source review only; no live app/device actions, polling/heartbeat, shared cancellation repair, dependency change, server/app restart, connection/phone input or runtime-data write.
- Next decision: **Keep D-008** for future clarification/diagnostic-scope approval, or **Remove D-008** as dropped/not implemented. Retain until an explicit decision; this is concrete unfinished investigation, not a manual-retest acknowledgement task.

### D-001 — Automatic PC/Laptop Check Workflow — Proposed / Deferred

- User goal: reduce manual, device-by-device UI checking for future changes while preserving the currently working PC and laptop layouts.
- Completed part: the main-panel checker and recorded PC/laptop profiles are saved in [Progress](PROGRESS.md). It automatically evaluates covered assertions once invoked; it does not run after every build or automatically on both machines.
- Unresolved proposal: whether to add build-triggered checks and coordinate same-version native checks across PC/laptop, and how project files/build identity would be synchronized. One editing Lead with read-only checking on the other host was suggested, not established as a permanent rule.
- No implementation approval exists for this broader workflow: no remote setup/access, cross-chat messaging, CI integration, global policy/settings changes, or new test execution is authorized. Actual laptop-native 125% coverage remains a recorded verification limitation, not a manual-acknowledgement task.
- Next decision: **Keep D-001** for future clarification, or **Remove D-001** as dropped/not implemented. Until answered, retain it as proposed/deferred; do not implement or silently withdraw it.

## Last Checkpoint

- 2026-10-09: user invoked cleanup-discussion and doc-checkpoint. Completed D-009 and the separable D-008 capture/parser evidence were saved and re-read in [Progress](PROGRESS.md) before completed Discussion details were removed. Earlier dated history remains preserved.
- [AGENTS](../AGENTS.md) and Progress now reflect shared-device selection, event-only one-panel auto-assignment, whole-finite-run completion beep, CRLF/LF-byte parser handling and isolated parser checks. Four-record structure preserved; [External Folders](EXTERNAL_FOLDERS.md) unchanged because existing tooling/ADB roots cover this work.
- D-008 input-freeze investigation and D-001 automatic PC/laptop workflow remain unfinished. Ask Keep/Remove for both; silence means Keep. No unfinished item was dropped and no manual-test success or acceptance inferred. Unperformed live checks remain limitations, not acknowledgement gates.
- Documentation validation passed: source/config hashes unchanged from checkpoint start, eight local links, four-file inventory and `git diff --check` (line-ending notices only). Independent consistency review found no actionable corrections and confirmed accurate transfer, historical/current distinctions and retention of both unfinished items. No build/app/checker/device/audio test, external write, network operation or Git mutation was performed for this checkpoint; pre-existing source changes are preserved.

## Queue Boundaries

- Earlier checkpoint evidence remains in Progress; current approved work is recorded here until an authorized checkpoint. Unperformed visual/device/input checks are limitations, not acknowledgement gates.
- Earlier observed code limitations are reference findings, not automatically added bug-fix tasks.
- Cleanup/checkpoint does not authorize application/source/config changes, new tests/builds, desktop/device interaction, external setup, global edits, physical cleanup, or Git mutations.
- Reopen completed work only when the user reports a bug or requests further work.
