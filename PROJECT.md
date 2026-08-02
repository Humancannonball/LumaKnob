# LumaKnob — product, architecture, and delivery plan

Status: M2.5 engine-boundary test build, first color-lab and 3D-panel revision<br>
Last reviewed: 2026-08-02<br>
Working name: **LumaKnob** (rename before the first public release if trademark research requires it)<br>
License: **MIT** for original source and artwork; third-party assets retain their own licenses

This is the project's single authoritative planning document. It records the product contract, technical decisions, constraints, milestones, and research. If code and this document disagree, either the code is wrong or this document must be deliberately amended in the same change.

## 1. Product in one sentence

LumaKnob is a small, local-first display utility with a dominant physical-style brightness control, a smaller step control, and optional secondary tuning controls, combining every safe display-control method available to reach levels below the normal hardware floor.

## 2. Problem and principles

Windows and monitor controls commonly have two problems:

1. Their lowest hardware setting can still be painfully bright in a dark room.
2. Their fixed increments are either too coarse at low levels or too slow at high levels.

LumaKnob's principles are:

- **One obvious primary control.** BRIGHTNESS is visually and behaviorally primary; STEP changes its adjustment precision, while advanced tuning remains smaller and secondary.
- **No artificial low floor.** The logical range is 0.00–100.00%. A recovery hotkey makes zero safe to expose.
- **Use the best available mechanism.** Probe each display and compose hardware and software control instead of pretending all displays behave alike.
- **Never lie about the backend.** The UI reports whether hardware, shade, or an optional gamma method is active and explains degraded modes.
- **Local and reversible.** No account, cloud, analytics, service, administrator rights, or permanent monitor writes.
- **Friendly to nontechnical users.** A signed Store install is the primary distribution; a portable build remains available.
- **Old-school character, modern behavior.** The interface is inspired by late-1990s/early-2000s audio equipment and media players, but uses original art and accessible input semantics.

## 3. Scope

### Version 1 must do

- Show a compact equipment-style window and a tray icon.
- Select one display, all displays, or a saved linked group.
- Adjust logical brightness from 0.00% to 100.00%.
- Select an adjustment step from 0.01% to 25.00%.
- Apply each brightness detent as `new = clamp(current ± step, 0, 100)`; changing STEP never moves BRIGHTNESS or snaps it to a new grid.
- Accept mouse drag, mouse wheel, keyboard arrows, Page Up/Down, Home/End, and direct numeric entry.
- Use an automatic backend pipeline per display:
  - internal-panel hardware brightness where WMI exposes it;
  - DDC/CI hardware brightness for capable external monitors;
  - a click-through per-display shade for sub-floor dimming or hardware failure;
  - an explicitly enabled gamma mode only as an experimental SDR alternative.
- Show `HW`, `SHADE`, and `GAMMA` status lights per selected target.
- Coalesce hardware writes while keeping the software UI responsive.
- Persist display aliases, per-display mode, pivot, step, brightness, hotkeys, and startup preference.
- Restore software state after display topology changes, resume, and sign-in unlock.
- Provide an emergency global restore hotkey, initially `Ctrl+Alt+Home`, that immediately removes software dimming and requests 50% hardware brightness.
- Start without elevation and work completely offline.

### Deliberately not in version 1

- macOS or Linux display control. The Core library remains portable, but the first product is Windows-specific.
- Ambient-light automation, schedules, sunrise/sunset, or application profiles.
- Remote control, accounts, syncing, telemetry, or update infrastructure outside the selected package channel.
- Color temperature/Night Light replacement.
- Writing monitor settings to nonvolatile storage.
- Kernel drivers, background Windows services, or undocumented display-driver hooks.
- Guaranteed dimming of the UAC/Winlogon secure desktop, exclusive-fullscreen games, the hardware cursor, or protected video overlays.

## 4. Interaction contract

### STEP knob

The default detent series follows a familiar 1–2–5 engineering progression:

`0.01, 0.02, 0.05, 0.10, 0.20, 0.50, 1, 2, 5, 10, 20, 25%`

The selected step is exact, is displayed numerically, and is independent of the brightness value. A later release may allow a custom list, but version 1 stores one selected value from the built-in series.

### BRIGHTNESS knob

- One wheel notch, keyboard arrow, or dial detent changes the logical value by STEP.
- The keyboard's physical brightness up/down buttons apply one STEP to the
  selected display while LumaKnob is running, even when its window is not
  focused. Windows exposes the standard buttons as Consumer Control HID usages
  `0x006F` and `0x0070`; the Windows backend receives them through Raw Input.
  Holding a button repeats after a normal initial delay, and Engine coalescing
  prevents a backlog of obsolete hardware writes. Normal brightness adjustment
  never requires an arbitrary modifier chord.
- Pointer dragging follows relative circular motion around the knob, is
  quantized to STEP, and begins from the current value without jumping to the
  pointer position. The hub is a small dead zone so crossing its center cannot
  cause an accidental large change.
- `Shift` temporarily divides the selected step by ten, down to 0.01%.
- `Page Up/Down` changes by five selected steps.
- `Home` invokes the emergency-safe 50% value; `End` requests 0.00% after a short confirmation the first time.
- A text readout supports direct entry for precision and accessibility.

The value is stored as integer basis points (`0..10,000`) so repeated changes are deterministic and never accumulate floating-point drift.

### Shared STEP and color controls

STEP remains visibly secondary to the much larger BRIGHTNESS knob. The full
Panel presentation places two real color controls directly beside BRIGHTNESS
for supervised testing:

- **GAMMA** selects a 50–150% midtone curve; 100% is neutral. Higher values
  lift midtones and lower values darken them while preserving the captured
  black and white endpoints where the driver permits it.
- **WARMTH** selects 0–100% channel reduction; 0% is neutral. It keeps red,
  reduces green by up to 8%, and reduces blue by up to 30% using the captured
  Windows color ramp.

BRIGHTNESS, GAMMA, and WARMTH each change by exactly the selected STEP in their
displayed percentage units. Changing STEP does not move or snap any value.
GAMMA and WARMTH apply immediately when moved. The current ramp is captured
first and restored when both controls return to neutral, when switching targets,
on emergency restore, and on normal exit. HDR-enabled targets are locked.
Because gamma ramps may be GPU-global, the UI warns that other displays can be
affected; color adjustment never participates in brightness Auto mode.

LumaKnob does not modify the undocumented Windows Night Light state. WARMTH is
our reversible equivalent; calling it Night Light would falsely imply control
of Windows' schedule and system setting.

### Visual design

- Layered graphite faceplate, raised equipment bays, recessed LCD-style
  readouts, engraved labels, green/amber/blue lamps, metal rings, cast shadows,
  highlights, and original rotary controls inspired by early-2000s audio gear.
- BRIGHTNESS is approximately twice the diameter of STEP and the tuning knobs,
  making the action hierarchy visible before any label is read.
- Borderless main panel with a discoverable drag region and conventional close/minimize affordances.
- No copied Winamp skin, logo, bitmap, Windows XP asset, or distinctive arrangement.
- Continuous backplane motion is subtle, frame-rate-limited, independently
  pausable, and can be disabled by preset or `--no-motion`.
- The UI remains usable at 100–250% display scaling and with keyboard-only navigation.
- Every colored state also has a text or shape cue. The knob exposes a range/value to Windows UI Automation.

Candidate permissive assets, to be evaluated rather than silently bundled:

- [Silkscreen](https://github.com/google/fonts/tree/main/ofl/silkscreen), SIL Open Font License 1.1, for small labels.
- [DSEG](https://github.com/keshikan/DSEG), SIL Open Font License 1.1, for segmented numeric readouts.
- [Pixelarticons](https://github.com/halfmage/pixelarticons), MIT, for small generic actions.
- [Kenney UI Pack](https://kenney.nl/assets/ui-pack), CC0, as optional source material.

Production should prefer code-drawn panels and knobs plus a small original icon set. Any included third-party file must appear in `THIRD_PARTY_NOTICES.md` with its source, version/commit, changed status, and license text or required notice.

## 5. Brightness model

### User-space value

`L`, the logical brightness, is a basis-point value from 0 to 10,000. It is a user command, not a claim about nits. Monitor hardware values are arbitrary and two monitors at the same reported percentage may not look equally bright.

Each display has an internal **hardware/shade pivot** `P`, currently fixed at
10.00% in the device-test build. A later calibration/settings surface may make
it user-adjustable without putting backend plumbing on the everyday panel. In
automatic hybrid mode:

- If `L >= P`, shade transmission is 100% and hardware is mapped into its usable native range.
- If `0 < L < P`, hardware is held at the native value associated with `P`, while black-shade transmission is approximately `L / P`.
- If `L = 0`, the shade may become opaque. The emergency hotkey and tray restore command remain active.

The pivot is not a minimum brightness. It is the handoff point where software begins extending the hardware range. A calibration screen lets the user choose a pivot that avoids a visible jump.

### Hardware normalization

- DDC values are mapped from the device's reported `[minimum, maximum]` range into normalized `[0, 1]`.
- Internal panels may expose only a discrete list of supported levels. The pipeline selects the lowest supported level at or above the ideal target, then uses the shade to fill the gap continuously; a shade can remove light but cannot add it.
- No backend assumes that a value is photometrically linear. Optional per-display calibration curves are a post-v1 feature.

### Transition ordering and rate limits

Software shade changes render immediately. Hardware requests are last-value-wins and coalesced per physical display:

- A hidden 100 ms quiet period precedes each hardware request. A new request
  cancels a pending delay; if a native call has already begun it is allowed to
  finish, but its stale completion is ignored and the final request follows it.
- DDC/CI and WMI writes are limited to one in-flight operation per display. At
  the 100 ms default, settled input produces at most 10 new hardware writes per
  second; the final requested value is always sent.
- WMI requests are serialized per panel.
- Reads are cached briefly and refreshed after external controls, topology changes, resume, or an explicit refresh.
- Hybrid transitions temporarily bias toward the darker intermediate state, then settle hardware and shade, avoiding a bright flash.
- A failed or timed-out hardware operation trips a short circuit breaker, reports the degraded state, and continues with shade-only control. Retrying is explicit or occurs after rediscovery.

## 6. Backend negotiation

The program builds a capability record for every active display. This happens at startup and after display, power, or session events.

```text
Windows display path
    ├─ stable-ish identity and friendly name (DisplayConfig)
    ├─ internal panel? ── probe WMI levels and methods
    ├─ physical monitor handle(s)? ── probe DDC/CI capabilities/brightness
    └─ desktop bounds ── create software shade capability

Logical brightness request
    └─ policy: Auto | Hardware only | Shade only | Gamma (experimental)
        ├─ hardware target (when healthy)
        └─ shade target (sub-floor or fallback)
```

### Internal panel: WMI

Use `WmiMonitorBrightness` to read the current value and supported `Level[]`, and `WmiMonitorBrightnessMethods.WmiSetBrightness` to write. The current value must be one of the panel's exposed levels, so the Core policy must not assume every integer percentage is legal. `WmiRevertToPolicyBrightness` is used when the user requests relinquishing control.

### External display: DDC/CI

Enumerate logical monitors with `EnumDisplayMonitors`, map each `HMONITOR` to physical monitor handles with `GetPhysicalMonitorsFromHMONITOR`, and destroy every returned handle. Probe `GetMonitorCapabilities` and `GetMonitorBrightness` before enabling writes.

Microsoft warns that many monitors do not correctly implement MCCS/DDC over I²C. `GetMonitorBrightness` takes about 40 ms and `SetMonitorBrightness` about 50 ms, so these APIs must stay off the UI thread and must be coalesced. Never call `SaveCurrentMonitorSettings`; frequent writes must not touch the monitor's nonvolatile memory.

### Software shade

Create one borderless transparent black window per active display. It is non-activating, click-through, absent from Alt-Tab/taskbar, and ordered above ordinary windows but below LumaKnob's control panel. Its alpha supplies dimming without changing pixel color mappings.

Known limits are part of the product UI, not buried in documentation:

- The normal app cannot cover the Winlogon/UAC secure desktop.
- The hardware cursor may remain brighter because it is composited independently.
- Exclusive-fullscreen or protected/independent-flip content may appear above the shade.
- HDR output must be tested; shade behavior can differ by GPU/driver and content path.
- Screenshots may or may not include the visual shade depending on the capture path.

### Gamma and warmth (experimental, never automatic)

`SetDeviceGammaRamp` is not a brightness fallback. Microsoft strongly
recommends against using it: a driver can silently reject ramps, its state can
be global and overwriteable, it resets on display events, has undefined HDR
behavior, and can conflict with color calibration. It is exposed as an
explicit SDR control for users who accept those tradeoffs. The original ramp
is captured before the first adjustment and restored at neutral or on exit.

The current color controller implements this experiment conservatively:

- discovery carries the Windows GDI display-device name and advanced-color/HDR
  observation into the portable target record;
- HDR-enabled targets cannot adjust GAMMA or WARMTH;
- `GetDeviceGammaRamp` captures three 256-entry RGB ramps on first use;
- the 50–150% GAMMA UI value maps to a 0.50–1.50 power curve, while WARMTH scales the
  calibrated green and blue outputs rather than replacing the calibration with
  an identity ramp;
- only one target is active at a time, and its captured ramp is restored before
  changing target, emergency restore, or normal exit.

This cannot guarantee per-monitor isolation: two target device contexts on the
same GPU may share a hardware ramp. That behavior is part of the new physical
test protocol. A process crash can also prevent normal restoration, although
Windows commonly resets ramps on display events. Color adjustment therefore
remains absent from Auto and from the future Compact default UI.

### Capability policy

`Auto` chooses, in order:

1. healthy internal-panel hardware plus shade;
2. healthy DDC/CI hardware plus shade;
3. shade only.

Gamma is never selected by Auto. Hardware-only and shade-only modes are available per display. A backend error must not crash the app or block the other displays.

## 7. Display identity and lifecycle

Use `QueryDisplayConfig` and `DisplayConfigGetDeviceInfo` for active paths, friendly EDID names, device paths, adapter IDs, and target IDs. Persist a composite identity based on monitor device path plus EDID manufacturer/product and connector information; treat it as stable-ish, not globally permanent. If ambiguity remains—for example, two identical monitors—preserve settings only when a unique match is possible and offer user aliases.

Rediscover displays on:

- `WM_DISPLAYCHANGE` / device topology change;
- session unlock and resume;
- display power-on notification;
- explicit Refresh.

Suspend expensive probing while a display is off. Dispose DDC physical-monitor handles on every topology rebuild and app exit.

## 8. Failure recovery and safety

- Register `Ctrl+Alt+Home` as a system-wide emergency restore hotkey. If registration fails, show a persistent warning and require the user to choose another binding before allowing 0.00%.
- Register a background Raw Input listener for the Consumer Control top-level
  collection and parse brightness usage reports through the Windows HID parser;
  do not assume a device-specific report layout. Listener failure is reported
  without weakening the independent emergency-restore path. Raw Input observes
  the standard buttons rather than installing a keyboard hook or kernel driver.
- The restore path must not depend on the main window being visible. It removes all shades synchronously first, restores gamma if used, then asynchronously requests 50% hardware brightness.
- A tray menu duplicates Restore, per-display enable/disable, Show, and Exit.
- On normal exit, software shade and gamma are always removed. Hardware brightness remains at the user's selected level unless “restore hardware on exit” is enabled.
- Store a small clean-shutdown marker. After an unclean shutdown, start with shades disabled, then offer to restore the last session.
- Never run as administrator. Never install a service or driver.
- Logs contain backend kind, timings, capability results, and sanitized error codes; no screenshots, EDID serial numbers, usernames, window titles, or network transmission. Debug logging is off by default and rotates locally.

## 9. Technology choices

### Selected stack

- **C# 14 / .NET 10 LTS.** Modern language, excellent Windows interop, self-contained publishing, and support through November 2028.
- **Avalonia 12.1.1.** MIT-licensed, XAML-based, supports custom rendering, transparent Windows, per-monitor scaling, tray icons, and Linux-to-Windows cross-builds without tying the Core/UI to legacy Windows-only tooling.
- **CommunityToolkit.Mvvm 8.4.2.** Small, source-generator-based observable properties and commands.
- **Microsoft.Windows.CsWin32 0.3.298.** Generates typed P/Invoke bindings from Windows SDK metadata, limiting handwritten interop.
- **System.Management 10.0.10.** WMI access for integrated-panel brightness.
- **xUnit v3 3.2.2.** Core policy and state-machine tests.

Versions are centrally pinned and updated deliberately. Release builds use
.NET single-file compression and remain self-contained, reducing the executable
from 102.6 MB to about 48.3 MB without requiring users to install a runtime.
Trimming was measured and rejected for the current release because Avalonia
designer/runtime services and `System.Management` produce unresolved trim
analysis warnings. NativeAOT is likewise not a version-1 goal while WMI, XAML,
and real Windows display behavior cannot be verified warning-free. These modes
can be reconsidered only after a clean analysis and the physical-device matrix.

### Why not the main alternatives

- **WPF:** stable but Windows-only and less pleasant for a heavily custom, code-drawn skin; it remains the contingency if Avalonia exposes a blocking Windows-windowing bug.
- **WinUI 3:** visually modern but its Windows App SDK runtime and packaging model add weight without helping this deliberately non-Fluent interface or the native tray/backend work.
- **C++/Rust:** viable for a tiny native binary, but they substantially increase UI, accessibility, packaging, and contributor effort. The risky part here is hardware behavior, not managed runtime overhead.
- **Electron/Tauri:** the web stack helps UI iteration, but adds a bridge and process/runtime complexity to a small always-running utility whose core value is Win32/WMI integration.

## 10. Code architecture

Dependencies point inward. Core and Engine never reference Avalonia or Windows
APIs, and the shared UI never references the Windows backend.

```text
LumaKnob.Host.Windows (executable and composition root)
    ├── LumaKnob.App (shared Avalonia presentation)
    │       ├── LumaKnob.Engine
    │       └── LumaKnob.Core
    ├── LumaKnob.Windows (DDC/CI, WMI, DisplayConfig, shade, Windows input)
    │       ├── LumaKnob.Engine
    │       └── LumaKnob.Core
    └── LumaKnob.Engine (sessions, last-value-wins coordinator, events)
            └── LumaKnob.Core (value, step, capability, pure hybrid policy)

LumaKnob.Host.Simulator (cross-platform development executable)
    └── LumaKnob.App + a fake platform (never touches real displays)

LumaKnob.Core.Tests ───── Core
LumaKnob.Engine.Tests ─── Engine + fake platform
LumaKnob.App.Tests ────── shared control interaction math
```

Current repository structure:

```text
LumaKnob/
├── PROJECT.md                  this file
├── README.md                   short contributor/user entry point
├── LICENSE
├── THIRD_PARTY_NOTICES.md
├── Directory.Build.props
├── Directory.Packages.props
├── global.json
├── LumaKnob.slnx
├── design/
│   └── device-test-ui.png      current simulator capture, never packaged
├── presets/
│   ├── graphite.json           default schema-2 visual preset
│   ├── amber-rack.json         warm high-depth variant
│   ├── cryo-console.json       cool high-tech variant
│   └── quiet-graphite.json     reduced-motion variant
├── src/
│   ├── LumaKnob.Core/
│   │   └── Brightness/         basis-point value, steps, hybrid policy
│   ├── LumaKnob.Engine/
│   │   ├── Coordination/       sessions and serialized/coalesced writes
│   │   ├── Displays/           platform-neutral target contracts
│   │   └── Platform/           injected platform/input contracts
│   ├── LumaKnob.Windows/
│   │   ├── Color/              reversible experimental gamma/warmth ramp
│   │   ├── DdcCi/
│   │   ├── Displays/
│   │   ├── InternalPanel/
│   │   ├── Shade/
│   │   └── Shell/
│   ├── LumaKnob.App/
│   │   ├── Appearance/         validated visual preset loader
│   │   ├── Controls/           accessible rotary knob
│   │   ├── ViewModels/
│   │   └── Views/
│   ├── LumaKnob.Host.Windows/  minimal Windows executable/composition root
│   └── LumaKnob.Host.Simulator/ cross-platform fake-display development host
├── tests/
│   ├── LumaKnob.Core.Tests/
│   ├── LumaKnob.Engine.Tests/
│   ├── LumaKnob.App.Tests/
│   └── LumaKnob.Windows.Tests/ color math and HID usage tests
└── .github/workflows/ci.yml
```

Current contracts and responsibilities are:

- `BrightnessValue` — immutable basis-point value and safe arithmetic.
- `BrightnessStep` — validated 0.01–25.00% adjustment.
- `BrightnessPlan` — pure hardware target, shade transmission, and mechanism
  flags.
- `BrightnessTargetInfo` / `IBrightnessTarget` — platform-neutral identity,
  bounds, capability, read, and native write surface.
- `IDisplayPlatform` — injected discovery, shade-window setup, and emergency
  system-input/color-controller factories.
- `IDisplayColorController` — platform color capability, apply, and restore
  boundary; the Engine and UI never call GDI directly.
- `BrightnessDisplay` — one Engine-owned target session and operation state.
- `BrightnessEngine` — plan creation, immediate shade requests, serialized
  last-value-wins hardware work, failure fallback, and restore.

Native adapters currently communicate read/write failure through exceptions;
the Engine catches and isolates asynchronous write failures. Typed probe/write
result records remain a pre-beta hardening task so expected hardware errors do
not rely on exception text.

### Portability and presentation boundary

The M2.5 slice has removed the direct App-to-Windows dependency and moved
brightness orchestration into `LumaKnob.Engine`. The intended extension shape
is now:

```text
LumaKnob.Core
    brightness values, policy, platform-neutral contracts, configuration DTOs

LumaKnob.Engine
    display sessions, last-value-wins write coordinator, state, settings, events
        ├── LumaKnob.Platform.Windows
        └── LumaKnob.Platform.Linux                 later project

LumaKnob.App
    shared Avalonia views, controls, layout modes, appearance presets
        ├── LumaKnob.Host.Windows                   executable/composition root
        └── LumaKnob.Host.Linux                     later executable

LumaKnob.Cli                                      later project
    commands and headless control through the same Engine contracts
```

The Engine owns the meaning and coordination of brightness and never references Avalonia,
Windows, a tray icon, or a theme. A platform package owns discovery, hardware,
shade windows, and Windows system-input implementation. The App package receives
only platform-neutral display snapshots and commands. This keeps a Linux port
from forking the UI and keeps a skin from affecting hardware behavior.

There will be three startup experiences rather than treating all of them as
skins:

- **Panel:** the full equipment-style interface, including optional tuning
  controls.
- **Compact:** a small tray flyout for quick per-display adjustment, comparable
  in purpose to Twinkle Tray while retaining LumaKnob's hybrid control. The
  first implementation is complete: the app remains resident, a tray click
  opens selected-display BRIGHTNESS and STEP controls, and a second process
  activates the existing instance.
- **Headless:** no window or tray UI; hotkeys and CLI/local IPC operate the
  Engine. This is useful for scripted or minimal installations.

### Appearance presets

Visual customization belongs only in `LumaKnob.App`. Schema 2 is a small,
validated JSON format that can override an allow-list of knob, panel, display,
animated-backplane, screw, and indicator-lamp color tokens. Its `visuals`
object also controls background motion, speed, intensity, frame rate, knob
depth, panel bevel depth, and corner radius within conservative ranges. Schema
1 color-only presets remain compatible.

The permanent preset is loaded from
`%LocalAppData%\LumaKnob\appearance.json`. `--appearance=<path>` previews any
preset without copying it, `--no-motion` overrides background animation, and
the `FX LIVE` panel button pauses or resumes it at runtime. Unknown schema
versions, token names, malformed colors, out-of-range geometry/motion values,
and malformed files are ignored safely. The repository's preset directory
contains Graphite, Amber Rack, Cryo Console, and motion-free Quiet Graphite.

Layout mode and appearance preset are separate settings. For example, the same
graphite preset may support both Panel and Compact modes. Headless mode loads no
visual preset. Arbitrary third-party XAML or code is intentionally excluded at
first because it would make compatibility, accessibility, and safe theme
sharing much harder.

`RotaryKnob` consumes named brush and depth resources through styled properties
and now draws offset cast shadows, layered metal rings, a fluted inset face,
specular highlights, a recessed hub, and a shadowed marker. `SignalBackplane`
draws a low-opacity moving grid, hardware buses, data pulses, scan sheen, and
oscilloscope trace at a preset-limited 10–60 frames per second. Its timer stops
when detached or paused. The XAML panel uses layered raised/recessed borders
and tokenized palette resources. Rendering does not know anything about display
discovery or backends. Geometry, typography, and layout remain
code/XAML rather than arbitrary theme code so scaling and accessibility stay
predictable.

## 11. Settings and privacy

Use a versioned JSON document under `%LocalAppData%\LumaKnob\settings.json`, written atomically through a temporary file and replace. Unknown fields are preserved where practical; migrations are explicit and tested. Secrets do not exist. Defaults must work without a configuration wizard.

LumaKnob makes no network requests at runtime. Package-manager update checks belong to the Microsoft Store/WinGet/GitHub client, not to the app. A future opt-in update checker would require a new documented privacy decision.

## 12. Installation, updates, and licensing

Primary channel:

- Microsoft Store packaged MSIX, per-user installation, clean uninstall, Store signing and automatic updates.

Secondary channels:

- GitHub Releases with self-contained `win-x64` and `win-arm64` portable archives.
- WinGet manifest pointing to signed release artifacts after the release process is stable.

Direct downloads without a trusted signature may trigger SmartScreen. Store-submitted MSIX packages are re-signed by Microsoft; non-Store artifacts require their own trusted code-signing solution. Until signing is funded, portable builds must clearly state this limitation rather than teaching users to disable security controls.

The repository uses the MIT License for maximal reuse and easy contribution. Dependency licenses and fonts/icons are tracked in third-party notices and release review. Contributors certify origin through Developer Certificate of Origin sign-off; a CLA is unnecessary initially.

## 13. Test and release strategy

### Automated

- Current: Core value/step and hybrid-policy boundary tests at zero, pivot,
  discrete hardware levels, and 100%.
- Current: Engine tests against a fake platform for discovery,
  last-value-wins delay cancellation, serialized non-cancellable writes,
  failure fallback, and emergency restore.
- Current: circular drag math tests for direction, wraparound, and accumulated
  small movements.
- Current: color-ramp tests proving neutral preservation, gamma midtone
  behavior, and warmth channel ordering without invoking Windows APIs.
- Current: Linux and Windows CI build plus every Core, Engine, App, and Windows
  pure-math test; compiler and analyzer warnings are errors.
- Remaining: exhaustive/property coverage across all brightness values and
  presets, settings migration/atomic-write tests, and automated UI/accessibility
  smoke tests.

### Physical device matrix before public beta

- At least two integrated laptop panels from different OEMs.
- External DDC/CI monitors from at least three vendors, including one known-bad/disabled DDC device.
- Mixed internal + external setup and two identical external models.
- Dock/undock, display sleep/wake, suspend/resume, lock/unlock, hot-plug, DPI changes.
- SDR and HDR, Night Light/color profiles, hardware cursor, protected video, and borderless/exclusive-fullscreen games.
- Windows 11 x64 and Arm64 if hardware is available; Windows 10 is best-effort while its platform/runtime dependencies remain supported.

A backend is not called “supported” based only on API success. Visual behavior must be physically validated, matching Microsoft's warning about inconsistent MCCS implementations.

## 14. Delivery milestones

### M0 — architecture spike (complete)

- Freeze this brief.
- Build Core value/step types and hybrid policy with tests.
- Build the old-school two-knob UI against a simulator.
- Establish build/CI/release skeleton.

Exit: the UI can alter a simulated display, the step is independent, and Core tests pass.

### M1 — one real display (complete for the current tested hardware)

- DisplayConfig discovery and stable-ish identity.
- Integrated-panel WMI adapter.
- Emergency global restore hotkey and lifecycle cleanup.

Exit: safely control and recover one supported laptop panel.

### M2 — external and below-floor dimming (device-test implementation complete)

- DDC/CI enumeration, capability probe, rate-limited writes, handle lifecycle.
- Per-display shade windows and hybrid transitions.
- Multi-display selector and backend status/errors.

Exit: mixed displays work, including shade-only fallback and levels below the hardware floor.

### M2.5 — portable engine and presentation boundary

- Complete: move orchestration out of `MainWindowViewModel` into a tested
  Engine service, including serialized last-value-wins hardware writes.
- Complete: define platform-neutral display targets, sessions, commands, and
  events.
- Complete: make the Windows project an injected platform adapter, keep the
  Avalonia presentation free of Windows types, and introduce a tiny Windows
  executable host.
- Complete for schema 2: validate palette, motion, depth, bevel, and radius
  options; retain schema-1 compatibility; ship four presets and the first
  animated high-depth code-drawn equipment panel.
- Remaining: versioned functional settings with `Panel`, `Compact`, and
  `Headless` startup modes.

Exit: the existing Windows behavior is unchanged, the Engine runs against a
fake platform without Avalonia, and the full UI can be replaced or omitted
without changing brightness coordination. The architecture portion meets this
exit now; settings/startup-mode work remains before M2.5 is closed.

### M3 — daily-use beta

- Complete foundation: resident notification-area process, compact tray
  flyout, explicit Restore/Exit menu, hide-to-tray deck, single-instance
  activation, and an original LumaKnob tray icon.
- Complete: independent per-monitor brightness state, one rotary control per
  monitor in the full deck, and one live slider row per monitor in the compact
  flyout. STEP is shared; selecting a row chooses the gamma/warmth and physical
  brightness-key target without coupling the monitors' direct controls.
- Remaining: startup option, aliases/groups, settings migrations, and expanded
  diagnostics.
- Accessibility and scaling pass.
- Physical device matrix and fault injection.
- Original production icon/artwork and third-party audit.

Exit: no known path can strand a user with an opaque shade; beta artifacts are reproducible.

### M4 — easy public install

- MSIX identity, Store listing/privacy disclosure, signing, update and uninstall tests.
- GitHub portable archives, checksums, SBOM, WinGet submission.
- Contributor guide, issue templates, security policy, support matrix.

Exit: a nontechnical user can install, recover, update, and uninstall without a terminal.

## 15. Review gates and important unresolved choices

These are conscious gates, not missing architecture:

- Validate whether `LumaKnob` is an acceptable public name before publishing package identity or artwork.
- Test Avalonia's transparent click-through windows across HDR, scaling, multiple GPUs, and virtual desktops during M0/M2; fall back to a thin Win32-owned overlay window if needed while keeping Avalonia for the panel.
- Choose the default pivot only after device testing. 10% is a provisional, understandable value, not a claim of perceptual equivalence.
- Decide whether 0.00% requires first-use confirmation based on emergency-hotkey reliability testing.
- Choose a code-signing budget before publishing portable installers.
- Do not promise identical perceived brightness across displays until a calibration model exists.
- Keep gamma experimental unless Windows introduces a documented supported replacement.

## 16. Architecture review outcome

The plan is feasible without borrowing an abandoned brightness application's code. The selected stack is modern, permissively licensed, and optimized for contributor speed. The hardest part is not the UI or language; it is truthful capability negotiation across poorly implemented monitor hardware and Windows composition paths.

The review made five corrections that implementation must preserve:

1. Gamma is an explicit user control, never the default software fallback.
2. Hardware calls are asynchronous, coalesced, and failure-isolated because DDC reads/writes are slow and unreliable.
3. Internal brightness is modeled as discrete levels where required, while shade makes the logical UI continuous.
4. Display identity is treated as fallible and rematched conservatively.
5. Zero brightness is exposed only with an independent, tested restore path.

With those constraints, the project has progressed through M2 real-device
control and into the M2.5 portability boundary without borrowing a legacy
brightness codebase.

## 17. Primary research sources

Framework and interop:

- [Avalonia Windows platform guide](https://docs.avaloniaui.net/docs/platform-specific-guides/windows)
- [Avalonia TrayIcon guide](https://docs.avaloniaui.net/controls/navigation/trayicon)
- [Avalonia repository and MIT license](https://github.com/AvaloniaUI/Avalonia)
- [Use Win32 APIs in C# with CsWin32](https://learn.microsoft.com/en-us/windows/apps/develop/interop/call-win32-apis)
- [.NET 10 downloads and current SDK](https://dotnet.microsoft.com/en-us/download/dotnet/10.0)
- [.NET support policy](https://dotnet.microsoft.com/en-us/platform/support/policy/dotnet-core)

Brightness and display APIs:

- [WmiMonitorBrightness](https://learn.microsoft.com/en-us/windows/win32/wmicoreprov/wmimonitorbrightness)
- [WmiMonitorBrightnessMethods](https://learn.microsoft.com/en-us/windows/win32/wmicoreprov/wmimonitorbrightnessmethods)
- [Using high-level monitor configuration functions](https://learn.microsoft.com/en-us/windows/win32/monitor/using-the-high-level-monitor-configuration-functions)
- [GetMonitorBrightness](https://learn.microsoft.com/en-us/windows/win32/api/highlevelmonitorconfigurationapi/nf-highlevelmonitorconfigurationapi-getmonitorbrightness)
- [SetMonitorBrightness](https://learn.microsoft.com/en-us/windows/win32/api/highlevelmonitorconfigurationapi/nf-highlevelmonitorconfigurationapi-setmonitorbrightness)
- [Physical monitor enumeration APIs](https://learn.microsoft.com/en-us/windows/win32/api/physicalmonitorenumerationapi/)
- [QueryDisplayConfig](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-querydisplayconfig)
- [DisplayConfigGetDeviceInfo](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-displayconfiggetdeviceinfo)
- [DISPLAYCONFIG_TARGET_DEVICE_NAME and EDID fields](https://learn.microsoft.com/en-us/windows/win32/api/wingdi/ns-wingdi-displayconfig_target_device_name)
- [GetDeviceGammaRamp limitations](https://learn.microsoft.com/en-us/windows/win32/api/wingdi/nf-wingdi-getdevicegammaramp)
- [SetDeviceGammaRamp limitations](https://learn.microsoft.com/en-us/windows/win32/api/wingdi/nf-wingdi-setdevicegammaramp)
- [Windows desktop/secure desktop model](https://learn.microsoft.com/en-us/windows/win32/winstation/desktops)
- [Power setting notifications](https://learn.microsoft.com/en-us/windows/win32/power/power-setting-guids)
- [Adaptive brightness guidance and low-end levels](https://learn.microsoft.com/en-us/windows-hardware/design/device-experiences/sensors-adaptive-brightness)
- [Display brightness HID controls](https://learn.microsoft.com/en-us/windows-hardware/drivers/hid/display-brightness-control)
- [Raw Input overview](https://learn.microsoft.com/en-us/windows/win32/inputdev/about-raw-input)
- [RegisterRawInputDevices](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-registerrawinputdevices)
- [HidP_GetUsagesEx](https://learn.microsoft.com/en-us/windows-hardware/drivers/ddi/hidpi/nf-hidpi-hidp_getusagesex)

Packaging:

- [Publish Windows apps and choose a distribution channel](https://learn.microsoft.com/en-us/windows/apps/publish/get-started)
- [Distribute a Win32 app through Microsoft Store](https://learn.microsoft.com/en-us/windows/apps/distribute-through-store/how-to-distribute-your-win32-app-through-microsoft-store)
- [Open a Microsoft Store developer account](https://learn.microsoft.com/en-us/windows/apps/publish/partner-center/open-a-developer-account)
- [MSIX documentation](https://learn.microsoft.com/en-us/windows/msix/)
- [Sign an MSIX package](https://learn.microsoft.com/en-us/windows/msix/package/signing-package-overview)
- [Submit a package manifest to WinGet](https://learn.microsoft.com/en-us/windows/package-manager/package/repository)
- [GitHub Releases](https://docs.github.com/en/repositories/releasing-projects-on-github/about-releases)
- [.NET single-file deployment](https://learn.microsoft.com/en-us/dotnet/core/deploying/single-file/overview)

Package versions and the resolved Windows dependency graph were audited on
2026-08-02: [Avalonia](https://www.nuget.org/packages/Avalonia),
[CommunityToolkit.Mvvm](https://www.nuget.org/packages/CommunityToolkit.Mvvm),
[Microsoft.Windows.CsWin32](https://www.nuget.org/packages/Microsoft.Windows.CsWin32),
[System.Management](https://www.nuget.org/packages/System.Management), and
[xunit.v3](https://www.nuget.org/packages/xunit.v3). The portable package also
ships the upstream SkiaSharp/HarfBuzzSharp and ANGLE native notices.

## 18. Hands-on Windows device-test protocol

This is an unsigned development build. It is intended for supervised testing,
not daily startup use. The build probes real devices but makes no brightness
write merely by launching. The first write occurs when the user moves
BRIGHTNESS or invokes Restore. GAMMA and WARMTH begin at neutral and make their
first color change only when the user moves either knob.

This revision keeps the tested display discovery and hybrid behavior while
moving them behind an injected, platform-neutral Engine. It resolves display
names from active Windows DisplayConfig topology and EDID-friendly names before
falling back to a physical monitor's generic DDC description. A missing
`WmiMonitorBrightness` provider is a normal desktop limitation, not a discovery
failure. The footer reports the mechanisms used for the specific request:
hardware alone, software shade alone, or both.

Each monitor BRIGHTNESS knob is deliberately larger than STEP, which remains a
secondary precision control while also setting the detent size of GAMMA and
WARMTH. The previous PIVOT and RESPONSE diagnostic knobs and the confusing
Color Lab switch have been removed. GAMMA and WARMTH are real experimental
display controls placed next to BRIGHTNESS, but remain separate from brightness
Auto policy. The panel has also received its first high-depth early-2000s
equipment pass.

### Evidence from the 2026-08-01 test session

The latest supplied log is a clean single session on Windows 11 build 26200
with two 2560×1440 displays at 150% scaling:

- Lenovo `P27h-10` over DisplayPort, with HDR supported but disabled;
- ASUS `PA278CGV` over HDMI, with HDR enabled;
- both discovered by friendly model name and controlled through DDC/CI plus a
  per-display shade;
- hardware-only status above the 10% pivot and combined DDC/CI + shade status
  below it, including 0.00%, 0.30%, 1.00%, and other fractional values;
- no discovery, write, or shade errors in the captured session. The safety
  hotkey registered, but this particular log does not contain a restore action.

The log also shows a rapid 5-point sweep producing several roughly 50–70 ms
DDC writes. The Engine now handles that internally with a fixed 100 ms quiet
period, serialized native writes, and cancellation of pending intermediate
requests; this backend timing no longer occupies a user-facing control. The
new log fields expose each target's display-device name and HDR observation for
color-control safety.

### Before running

1. Use a normal Windows desktop session, not Remote Desktop.
2. Save work. Do not begin inside a fullscreen game or protected video.
3. If testing an external monitor, enable DDC/CI in its physical on-screen menu.
4. For the first integrated-panel software-shade test, make the laptop panel
   the Windows primary display. The first WMI implementation maps that shade to
   the primary display and labels this limitation in the UI.
5. Keep this recovery sequence nearby:
   - press `Ctrl+Alt+Home` to clear shades, restore captured color ramps, and
     request 50% hardware;
   - if that does not work, right-click the LumaKnob notification-area icon and
     choose `Exit LumaKnob`;
   - `Ctrl+Alt+Delete` switches to the undimmed secure desktop, from which the
     user can sign out and terminate every shade window.

The footer must say that the brightness-button listener and emergency restore
are ready. If emergency registration fails, the application displays the
Windows error and temporarily restricts the test floor to 1.00%. A brightness
listener failure is reported separately and does not disable recovery.

### Run from source

Install the .NET 10 SDK on Windows, open PowerShell in the repository root, and
run:

```powershell
dotnet restore LumaKnob.slnx
dotnet run --project src/LumaKnob.Host.Windows --configuration Release
```

To create a portable self-contained archive:

```powershell
.\scripts\publish-win-x64.ps1
```

This produces a matching `artifacts\LumaKnob-win-x64` folder and
`artifacts\LumaKnob-win-x64.zip` archive. The executable is compressed but
self-contained, so no separate .NET installation is required. Debug symbols
and historical design captures are not included. Because the executable is not
yet signed, SmartScreen may warn about it; do not disable SmartScreen globally.

### Test sequence

Perform these in order and stop at the first recovery failure.

1. **Read-only discovery**
   - Launch LumaKnob and do not touch any knob.
   - Confirm the screen does not visibly change.
   - Record every display selector entry and its backend label:
     `WMI + SHADE`, `DDC/CI + SHADE`, or `SHADE ONLY`.
   - Confirm monitor model names match Windows Settings or Twinkle Tray. If a
     generic name remains, press `COPY LOG`; the topology record will show
     whether Windows supplied a friendly EDID name.
   - Confirm the initial BRIGHTNESS reading roughly matches the hardware state.

2. **Recovery before dimming**
   - Press `Ctrl+Alt+Home` once.
   - Confirm shades clear, color controls return to neutral, and capable
     displays move to about 50%.
   - Confirm the footer reports a completed emergency restore.
   - Do not test 0.00% if this step fails.

3. **Control hierarchy and interaction**
   - Note BRIGHTNESS, then change STEP from 1.00% to 0.10%.
   - Confirm BRIGHTNESS does not move.
   - Confirm each monitor's BRIGHTNESS looks like a primary action and STEP is visibly
     secondary. GAMMA and WARMTH should sit directly beside BRIGHTNESS without
     visually competing with the primary control.
   - Move each monitor's BRIGHTNESS by one detent and confirm only that monitor
     changes, by exactly 0.10%.
   - Switch STEP back to 1.00% and confirm the next detent is exactly 1.00%.
   - With the pointer, drag clockwise around each knob to increase and
     counter-clockwise to decrease. Confirm pressing initially causes no value
     jump and crossing the center hub does not cause a large change.
   - Confirm the mouse wheel and arrow keys still change exactly one selected
     STEP at a time.
   - Put another application in the foreground. Press the keyboard's real
     brightness-up and brightness-down buttons; confirm the selected LumaKnob
     display changes by exactly one STEP without bringing LumaKnob forward.
   - Change STEP, minimize LumaKnob, and repeat to confirm the physical buttons
     use the new STEP immediately. Hold each button briefly and confirm repeat
     starts after a short delay and stops on release.
   - On a battery-powered device, report whether Windows also changes an
     internal panel independently; Raw Input is intentionally non-invasive, so
     this hardware-specific interaction must be measured before public beta.

4. **Shared STEP precision**
   - Select the Lenovo `P27h-10`, which the last log observed in SDR.
   - Set STEP to 1.00%. Move BRIGHTNESS, GAMMA, and WARMTH by one mouse-wheel
     notch and confirm each readout changes by exactly 1.00 percentage point.
   - Set STEP to 0.10% and repeat; each readout must change by exactly 0.10.
   - Set STEP to 0.01% and confirm WARMTH changes by 0.01 rather than being
     rounded back to a whole number.
   - Changing STEP itself must never move or snap any of the three values.
   - Return GAMMA to 100.00% and WARMTH to 0.00% before the visual tests.

5. **Gamma experiment on SDR**
   - Keep brightness at a comfortable visible value and Windows Night Light
     off so it cannot confuse the observation.
   - Move GAMMA from 100% to 80%. The display should change immediately; there
     is no enable switch. Compare 80%, 100%, and 120%, and confirm midtones
     change while black and white endpoints remain usable.
   - Return GAMMA to 100% with WARMTH at 0% and confirm the captured system
     appearance returns. Choose 80%, then press `Ctrl+Alt+Home`; confirm the
     ramp restores as well as brightness.
   - Report whether the Lenovo alone changes or both GPU-connected displays
     change. Shared behavior is a known GDI gamma-ramp limitation.

6. **Warmth experiment on SDR**
   - With GAMMA at 100%, compare WARMTH at 0%, 25%, and 50%. Each movement
     should affect the display immediately.
   - Confirm whites become progressively warmer without a colored veil over
     black pixels. Report obvious banding, clipping, or color-profile changes.
   - Return WARMTH to 0% and confirm the captured system color returns.

7. **HDR guard**
   - Select the ASUS `PA278CGV` while HDR remains enabled as in the last log.
   - Confirm `COLOR LOCKED`, the notice explains HDR, and both color knobs are
     disabled.
   - Do not disable this guard or test a gamma ramp on HDR in this revision.

8. **Hardware zone**
   - Choose one hardware-capable target and remain above the 10% pivot.
   - Move between approximately 50%, 40%, and 20% slowly.
   - Confirm only the selected display changes, `HW` changes, and `SHADE OFF`
     remains visible.
   - Confirm the footer says `through DDC/CI` or `through WMI` and does not say
     shade while `SHADE OFF` is visible.
   - If the monitor's own OSD shows brightness, compare its native value with
     the `HW` value displayed by LumaKnob.

9. **Below the hardware floor**
   - Move slowly through 10%, 9%, 5%, 2%, and 1%.
   - Confirm the hardware value stops at/above the pivot while SHADE increases.
   - Confirm the footer now names both active mechanisms, for example
     `through DDC/CI + software shade`.
   - Confirm the transition through 10% has no bright flash or large jump.
   - Confirm only the selected display is shaded and ordinary mouse clicks pass
     through the shade normally.
   - A brighter hardware cursor is a known overlay limitation; record it but do
     not treat it as a failed recovery path.

10. **Zero and emergency recovery**
   - Only after step 2 succeeds, set BRIGHTNESS to 0.00%.
   - Press `Ctrl+Alt+Home` immediately.
   - Confirm the desktop becomes visible and returns to roughly 50%.
   - Repeat once using the on-screen `RESTORE 50%` button from 1%; the
     button and hotkey must clear the same shade state.

11. **Multiple displays**
   - Repeat brightness steps 3, 8, and 9 with every monitor knob and every
     Quick Control slider row.
   - Set clearly different values on two monitors, switch the color target,
     and confirm both brightness values remain independent.
   - Switch targets and confirm earlier per-display shade state is preserved.
   - Apply a visible color adjustment on an SDR target, switch away, and confirm
     its captured ramp is restored and the color knobs reset to neutral.
   - Test one display placed left of the primary display and one with non-100%
     scaling if available; confirm shade bounds align with that display.

12. **Failure fallback**
   - For an external monitor, exit LumaKnob from its tray menu, disable DDC/CI in the monitor OSD,
     and relaunch.
   - Confirm the target becomes `SHADE ONLY`, or a DDC write failure switches it
     to shade-only with an explicit status instead of crashing.
   - Re-enable DDC/CI before further hardware tests.

13. **Lifecycle cleanup**
   - Apply visible shade, then close the Control Deck. Confirm the deck hides,
     the tray icon remains, and Quick Control can reopen the app. Confirm it
     lists a live slider for every attached monitor.
   - Apply a mild non-neutral GAMMA or WARMTH setting on SDR, then choose
     `Exit LumaKnob` from the tray menu. Confirm all shade disappears and the
     original system color returns.
   - Relaunch, apply shade, lock and unlock Windows, then report whether the
     shade remains correctly aligned. Automatic topology/resume restoration is
     not complete yet, so this observation informs M3 work.
   - Turn Windows Night Light on only after the isolated warmth test and report
     whether returning GAMMA and WARMTH to neutral leaves Night Light intact.

### What to report

Press `COPY LOG` after completing or stopping the test, then paste the result
into the test report. This automatically captures:

- Windows version/build and process architecture;
- computer manufacturer/model and GPU names/driver versions where WMI permits;
- active display bounds, primary display, and Avalonia scaling;
- Windows DisplayConfig friendly name, connector type, non-serial EDID
  manufacturer/product IDs, and advanced-color/HDR state;
- every selector entry, backend, initial hardware reading, and exact operation
  status/error.

Still provide these observations because Windows exposes no suitable supported
read-only API for all of them:

- which steps passed, failed, or behaved ambiguously;
- Night Light state and whether HDR content was actually playing;
- whether the hardware OSD value followed `HW`;
- whether GAMMA and WARMTH affected one display or both;
- whether returning to neutral, switching displays, closing, and
  restored the captured colors;
- any banding, clipping, flashing, ignored-ramp warning, or profile conflict;
- whether the raised/recessed 3D panel feels convincingly early-2000s and which
  parts still look too flat, modern, or crowded;
- whether shade covered the correct bounds, cursor, taskbar, video, and games;
- the copied diagnostic log.

The current launch is written to `session.log`; starting LumaKnob moves the
prior launch to `session.previous.log`, and a very long session rotates at
1 MB. Logs stay local and contain discovery/operation results. They do not
contain screenshots, window titles, usernames, or EDID serials.

## 19. Twinkle Tray source audit and parity roadmap

Twinkle Tray is an active open-source project under the MIT License, not an
abandoned proprietary dependency. The audit on 2026-08-02 inspected upstream
commit `f6541e3062d9d3c1e2f6d106a3c5ebfb02c06d81` (package version
`1.18.0-beta2`); the latest stable release shown upstream was 1.17.2. Its
license permits use, modification, redistribution, and sublicensing provided
the copyright and permission notice remain with copied substantial portions.

LumaKnob will not import Twinkle Tray wholesale. Twinkle Tray is an Electron /
React application with native Node modules, while LumaKnob deliberately uses a
portable C# Engine and Avalonia presentation. We will port isolated algorithms
or native API knowledge only when it is materially useful, keep the original
notice beside substantial copied code, record it in `THIRD_PARTY_NOTICES.md`,
and write C#/platform-neutral implementations for the rest. No Twinkle Tray
source or branded artwork is included in the current build.

| Capability observed upstream | LumaKnob status | Product decision |
| --- | --- | --- |
| Resident tray process and click-to-open flyout | Complete with one live slider row per display | Keep; add optional linked groups and an All Displays row after settings persistence. |
| Single-instance activation and command-line control | Activation complete; control IPC missing | Add authenticated local named-pipe commands for list, set, offset, restore, and show. |
| DDC/CI and WMI brightness | Complete on tested hardware | Keep LumaKnob's typed adapters, coalescing, and shade fallback. |
| Monitor names, ordering, hiding, renaming, linked levels | Names/selecting complete | Add aliases, ordering, hidden displays, linked groups, and all-display control. |
| Per-monitor min/max normalization and calibration points | Hardware normalization only | Add monotonic piecewise calibration curves after physical photometric testing. |
| DDC VCP contrast, audio volume, power state, input source, custom codes | Brightness VCP `0x10` only | Add capability-probed, allow-listed controls; keep risky power/input writes opt-in with warnings. |
| SDR brightness controls while HDR is active | HDR is detected and gamma is locked | Research the supported Windows advanced-color path before implementation. |
| Time-of-day and sun-position schedules with transitions | Not implemented | Add after settings persistence and resume/topology recovery. Keep coordinates local by default. |
| Idle dimming with fullscreen/media guards | Not implemented | Add as an optional automation policy, never as a hidden default. |
| Windows/USB ambient-light sensors | Not implemented | Add through a platform sensor contract after schedules. |
| Profiles and focused-application triggers | Not implemented | Add opt-in profiles; never log window titles or executable paths in diagnostics. |
| User-configurable hotkeys and overlay | Physical brightness HID keys plus emergency restore | Keep physical brightness buttons canonical; optional user shortcuts may be configured but will have no arbitrary default chord. |
| Mouse wheel over tray icon | Not implemented | Evaluate as an optional quick interaction using the current exact STEP. |
| Startup launch and last brightness restore | Not implemented | Add installer-managed per-user startup and a crash-safe clean-shutdown marker. |
| Localization | Not implemented | Externalize strings before public beta and accept community translations. |
| Themes, Windows-style flyouts, acrylic/mica | Code-drawn presets and compact equipment flyout | Preserve LumaKnob's identity; offer a restrained native preset later rather than cloning Twinkle Tray visuals. |
| Updates, Store/installer, portable build | Portable build only | Complete signed installer/MSIX, Store, WinGet, checksums, and update policy in M4. |
| Optional analytics in upstream defaults | LumaKnob has no telemetry | Do not copy; remain offline and telemetry-free by default. |

LumaKnob already has meaningful differentiators that are not parity work:
logical 0.00–100.00% hybrid brightness below the hardware floor, exact shared
STEP control, reversible per-display shade, experimental gamma/warmth, the
physical brightness-button listener, and the configurable high-depth equipment
interface. Parity work must preserve those rather than turning LumaKnob into an
Electron-compatible clone.
