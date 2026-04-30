Important: You just need adb for this. (You can use scrcpy to mirror screen to your PC)

Note: Use adb to control your phone from PC without installing any app on your phone. (No root needed)

1) Download adb and put at C:\adb
2) Use USB Debugging / Wireless Debugging to connect

	i) Wireless debugging : Important Command

		a) adb connect <IP_ADDRESS>:PORT (Connect device)

		b) adb pairs <IP_ADDRESS>:PORT (Pair device)

		c) adb devices (Check connected device list)

		d) adb disconnect <IP_ADDRESS>:PORT (Remove certain device)

		e) adb start-server (Start adb server)

		f) adb kill-server (Kill adb server)

## config.json format

The app now supports editable hotkeys and a shorter script format.

### Hotkey settings

```json
"settings": {
  "hotkeyStartStopToggle": "CTRL+ALT+S",
  "hotkeyStop": "CTRL+ALT+D"
}
```

### Script aliases (short format)

- Script: `d` (duration), `imin`, `imax`, `steps` (same as `config`), `defaults`.
- Step: `a` (act), `sx/sy`, `sx2/sy2`, `x/y`, `x2/y2`, `rx/ry`, `smin/smax`.

The old long keys are still supported.
You do **not** need to shrink or delete existing scripts. Keep your full config list (all 20 scripts), and only use short aliases for new edits if you want.



### Even shorter per-step format

You can also use array pairs to shorten repeated keys:

```json
{
  "d": 0,
  "i": [0, 2],
  "steps": [
    { "s": [919, 1171], "a": "leftclick", "r": [30, 10], "t": [62, 67] },
    { "s": [561, 1104], "a": "rightclick", "r": [200, 200], "t": [2, 3] }
  ]
}
```

Aliases in this compact mode:
- `i` = `[interval_min, interval_max]`
- `s`/`s2` = `[scrX,scrY]` / `[scrX2,scrY2]`
- `p`/`p2` = `[posX,posY]` / `[posX2,posY2]`
- `r` = `[randX,randY]`
- `t` = `[sleep_min,sleep_max]`


### Remove repeated step blocks

If you have the same sequence repeated many times, use a group object with `steps` + `repeat` (or `rep`):

```json
{
  "steps": [
    { "a": "updrag", "s": [537, 1680], "s2": [547, 1072], "r": [100, 20], "t": [3, 4] },
    { "a": "downdrag", "s": [544, 1120], "s2": [544, 1557], "r": [100, 20], "t": [3, 4] }
  ],
  "repeat": 4
}
```

This expands to 8 real steps at runtime, so behavior stays the same but `config.json` is much shorter.