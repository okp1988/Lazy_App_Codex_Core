# Config And Settings

## Config File Location

The app loads `config.json` using:

```text
new ScriptConfigRepository("config.json")
```

This means the active config depends on the process working directory. The repository `config.json` is the editable source file during local development.

The project file intentionally does not copy `config.json` to build or publish output.

## Current Config Shape

```json
{
  "settings": {},
  "offset": {},
  "scripts": {},
  "sequences": {},
  "runPlans": {}
}
```

Top-level legacy scripts are supported by repository migration rules. Category keys must be skipped when scanning legacy scripts.

## Settings

Canonical keys:

- `hotkeyStart`
- `hotkeyStop`
- `hotkeyBackupStart`
- `hotkeyBackupStop`
- `tag`
- `devices`

Rules:

- `hotkeyStartStopToggle` is legacy and migrates to `hotkeyStart`.
- Do not reintroduce `hotkeyStartStopToggle` as canonical.
- `hotkeyBackupStart` and `hotkeyBackupStop` are optional secondary hotkeys for Set 2. Blank values disable that secondary side.
- `settings.tag` is the canonical tag list.
- `settings.tags` is legacy and migrates to `settings.tag`.
- `All` is reserved for the main filter and must not be stored as a configured tag.
- Duplicate tags are removed case-insensitively.
- Blank tags are allowed on Scripts, Sequences, and Run Plans.

## Devices

Device history and friendly names live under `settings.devices`.

Example:

```json
{
  "settings": {
    "devices": {
      "192.168.50.147": {
        "name": "Samsung : SM-S948B",
        "manufacturer": "samsung",
        "model": "SM-S948B",
        "lastSerial": "192.168.50.147:33487",
        "lastSeen": "2026-05-16T16:39:49+08:00"
      }
    }
  }
}
```

Rules:

- Wi-Fi ADB keys use the IP address without the port.
- USB and mDNS-style serials use the serial as reported by ADB.
- Default names are built as `manufacturer : model`.
- Users may edit only the friendly name in the Devices tab.
- Sync refreshes manufacturer/model only when the device key is currently connected and ready.
- Automatic sync may create missing entries or fill blank fields, but existing conflicting manufacturer/model data is not silently overwritten.
- Wireless ADB Connect may create or update a Wi-Fi device entry by IP address.
- Successful Wireless ADB Connect updates `lastSerial` to the current `IP:Port` and refreshes `lastSeen`.
- Wireless ADB Pair does not imply the device is connected and should not update `lastSerial` as a ready device.

## Hotkey Behavior

- Empty hotkey text disables that hotkey.
- Primary start/stop hotkeys control Set 1.
- Backup start/stop hotkeys control Set 2 and are registered only while Set 2 is open.
- Primary and backup hotkeys are registered independently; a backup registration failure should not disable a successful primary registration.
- If a set's active start and stop hotkeys are the same, only one hotkey is registered for that set and it toggles start and stop.
- Hotkeys are unregistered when the main window is minimized.
- Hotkeys are re-registered when the window is restored or activated.
- Registration state is shown by `statusDot`: gold for both primary and secondary, green for primary only, blue for secondary only, and red for no registered hotkey.

## Offset Profiles

Offset profiles live under `offset`.

Preferred profile names:

```text
s<number>
```

Examples:

```json
{
  "offset": {
    "s26": [5, 5],
    "s13": [8, 4]
  }
}
```

Fallback keys remain supported:

- `offsetX`
- `offsetY`
- `ox`
- `oy`
- `x`
- `y`
- `s`

Lookup rules:

- The runnable name is scanned for digits.
- Each digit group is tried as `s<number>`.
- The selected axis chooses the profile X or Y value.
- If no matching profile exists, fallback X/Y offset values are used.

## Script JSON

Compact script output uses:

- `d`: duration or loop count
- `imin`: interval minimum seconds
- `imax`: interval maximum seconds
- `emin`: enforced minimum cycle seconds
- `config`: action groups
- `a`: action
- `s`: start coordinate `[x, y]`
- `s2`: drag end coordinate `[x2, y2]`
- `r`: randomization `[randX, randY]`
- `t`: sleep range `[sleepMin, sleepMax]`
- `o`: offset axis override

Supported editor actions are `left`, `right`, `drag`, and `delay`. For `delay`, no ADB command is sent and `t` is the randomized delay range in seconds. A leading Delay does not consume the selected offset; the first following applicable left-click still receives it.

Script fields:

- `id`
- `name`
- `tag`
- `hide`
- `order`
- `d`
- `imin`
- `imax`
- `emin`
- `defaultOffsetEnabled`
- `defaultOffset`
- `config`

Script names must be unique. Clones use `_copy`, `_copy2`, `_copy3`, and so on.

## Sequence JSON

Sequence fields:

- `id`
- `name`
- `tag`
- `hide`
- `order`
- `d`
- `imin`
- `imax`
- `emin`
- `defaultOffsetEnabled`
- `defaultOffset`
- `items`

Sequence item types:

- `script`: references a Script by `scriptId`
- `action`: stores a direct action

Rules:

- Sequences must not reference other Sequences.
- Sequence Script items store only Script IDs so Script renames do not break references.
- Hidden Scripts remain valid for Sequence Script items.
- Hidden Sequences remain valid for Run Plan items.
- Deleting a Script used by Sequences must ask for confirmation before deleting dependent Sequences.

## Run Plan JSON

Run Plan fields:

- `id`
- `name`
- `tag`
- `order`
- `items`

Run Plan item fields:

- `type`: `script` or `sequence`
- `targetId`: stable Script or Sequence ID
- `repeat`: item loop count

Rules:

- Run Plans are stored under top-level `runPlans`.
- Run Plans are first-class runnable entries, separate from Scripts and Sequences.
- Run Plans may reference Scripts or Sequences, including hidden entries.
- Run Plans must not reference other Run Plans.
- Item order is preserved exactly as configured.
- Item `repeat` overrides the referenced target's saved `d` only for that item.
- Referenced targets keep their own internal timing rules, including `emin`, `imin`, `imax`, step sleeps, Sequence item delays, and Sequence direct actions.
- Run Plans may have one configured `tag` or a blank tag and participate in the main tag filter.

## Alias Compatibility

These aliases are meaningful API and must remain compatible unless a deliberate migration is performed:

- `d`
- `imin`
- `imax`
- `emin`
- `i`
- `config`
- `steps`
- nested `steps` with `repeat` or `rep`
- `a`
- `s`
- `s2`
- `p`
- `p2`
- `r`
- `t`
- `o`

Action aliases:

- `left` maps to `leftclick` behavior.
- `right` maps to `rightclick` or back behavior.
- `back` maps to right/back behavior.
- Directional drag names remain usable.
- `delay` is the canonical no-ADB wait action.
- `wait` is accepted as a legacy/read alias and normalizes to `delay`.
- Unknown actions should be logged and skipped, not converted to device touches.

## Cycle Enforcement

Scripts and Sequences may set `emin` to force each cycle to last at least that many seconds.

Rules:

- `emin` is optional; `0` means disabled.
- `emin` cannot be larger than the displayed max cycle time in the editor.
- The runner computes the full cycle plan before executing the first action.
- If the randomized plan is shorter than `emin`, the runner re-randomizes the currently lowest flexible sleep, delay, or interval upward until the plan reaches `emin`.
- If `emin` equals the max cycle time, each flexible sleep, delay, and interval uses its maximum value.
