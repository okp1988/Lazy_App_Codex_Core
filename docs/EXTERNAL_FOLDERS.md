# External Folders

| External root | Known used subpaths | Purpose and evidence |
| --- | --- | --- |
| `C:\adb` | `adb.exe` | Runtime ADB executable default in `AdbShellController.cs`. The app uses it for device actions, monitoring, touch tracking, and manual Pair / Connect. The folder was identified from source; its contents were not inspected in this documentation task. |
| `C:\Users\okp19\.codex` | `AGENTS.md`; `memories/MEMORY.md` | Global working rules and read-only historical context used during this documentation consolidation. The user separately approved the acceptance-rule update to `AGENTS.md` on 2026-10-07. This is a tooling location, not an app deployment/runtime dependency. |

These entries record use only. They do not grant permission to write, launch tools, change global configuration, or clean external data. Project-local config, backup, logs, and primary build outputs are not external roots when located inside the repository. If the app is run from a different working/output directory, record an actual external location when known rather than inventing one from a relative path.
