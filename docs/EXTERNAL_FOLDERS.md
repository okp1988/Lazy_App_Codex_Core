# External Folders

| External root | Known used subpaths | Purpose and evidence |
| --- | --- | --- |
| `C:\adb` | `adb.exe` | Runtime ADB executable default in `AdbShellController.cs`, used for actions, monitoring, touch tracking, and Pair / Connect. On 2026-10-07 the user approved a five-second passive tracker capture against the existing local server; only its temporary diagnostic tracker was stopped afterward. No server restart, connection change, phone input, or folder write was performed. This entry is not standing permission to run ADB. |
| `C:\Users\okp19\.codex` | `AGENTS.md`; `memories/MEMORY.md`; `skills/cleanup-discussion/SKILL.md`; `skills/doc-checkpoint/SKILL.md` | Global working rules, read-only historical context, and the user-invoked documentation skill definitions. The user separately approved the acceptance-rule update to `AGENTS.md` on 2026-10-07. This is a tooling location, not an app deployment/runtime dependency; reading skills does not authorize editing them. |

These entries record use only. They do not grant permission to write, launch tools, change global configuration, or clean external data. Project-local config, backup, logs, and primary build outputs are not external roots when located inside the repository. If the app is run from a different working/output directory, record an actual external location when known rather than inventing one from a relative path.
