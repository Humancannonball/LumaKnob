# LumaKnob project notes

LumaKnob is a local Windows display utility. It combines hardware brightness
with a per-display software shade so the logical range can reach 0–100%, even
when a monitor's hardware minimum is too bright.

## Behavior

- Each monitor has an independent brightness value.
- STEP uses `0.01, 0.02, 0.05, 0.10, 0.20, 0.50, 1, 2, 5, 10, 20, 25%`.
- Changing STEP never changes or snaps the current value.
- Above the provisional 10% pivot, hardware does the work.
- Below the pivot, hardware stays low and the shade adds continuous dimming.
- Hardware writes are asynchronous, serialized per monitor, and last-value-wins.
- Gamma and warmth are explicit SDR controls, never automatic brightness
  fallbacks. Neutral or Exit restores the captured color ramp.

## Backends

| Display | Primary path | Fallback |
| --- | --- | --- |
| Integrated panel | WMI | Shade |
| External monitor | DDC/CI | Shade |
| Unsupported hardware | Shade | — |

DDC/CI is unreliable across some monitors, docks, adapters, and cables. The UI
shows the backend actually used rather than assuming hardware succeeded.

## Structure

```text
Host.Windows
├── App       Avalonia UI, tray, shade windows
├── Windows   WMI, DDC/CI, DisplayConfig, GDI, Raw Input
└── Engine    sessions, coalescing, recovery
    └── Core  values, steps, pure brightness policy

Host.Simulator  same App and Engine, fake displays only
```

Core and Engine do not depend on Avalonia or Windows. That keeps a future Linux
adapter or headless host possible without duplicating brightness policy.

## Safety rules

- Never require administrator rights, a service, or a driver.
- Never write monitor settings to nonvolatile storage.
- Launch is read-only; the first display write follows user input or Restore.
- `Ctrl+Alt+Home` clears shades, restores color ramps, and requests 50% hardware.
- Normal Exit removes software state.
- A hardware failure affects only that monitor and falls back to shade.
- Logs stay local and omit usernames and EDID serial numbers.

Software shades cannot guarantee coverage of the secure desktop, hardware
cursor, protected video, HDR paths, or exclusive fullscreen content.

## Hardware test

Use a normal local Windows session and save work first.

1. Enable DDC/CI in the monitor menu where applicable.
2. Start LumaKnob and confirm launch does not change the displays.
3. Confirm every monitor name and backend.
4. Test brightness above and below 10% one monitor at a time.
5. Confirm another monitor does not change.
6. Test the tray flyout and physical brightness buttons where available.
7. On SDR only, test gamma/warmth and return both to neutral.
8. Test `Ctrl+Alt+Home` before trying 0%.
9. Try 0%, restore immediately, then Exit.
10. Confirm all shades and color changes are gone.

Stop at the first recovery failure. Do not test through Remote Desktop, inside
fullscreen content, or while unsaved work is open.

## Current status

Implemented:

- WMI, DDC/CI, shade fallback, and friendly display names;
- per-monitor controls, shared STEP, tray flyout, and brightness buttons;
- reversible gamma/warmth with HDR guard;
- simulator, appearance presets, Windows/Linux CI, and one-file publishing.

Next:

- persistent settings and startup;
- hot-plug, resume, and unlock recovery;
- accessibility, DPI, HDR, multi-GPU, Arm64, and broader monitor testing;
- signing and a normal installer/store channel.

The original code and artwork use MIT. Third-party licenses remain in
`THIRD_PARTY_NOTICES.md` and are embedded in the release executable.
