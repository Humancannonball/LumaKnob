<p align="center">
  <img src="src/LumaKnob.App/Assets/lumaknob-tray.svg" width="80" alt="LumaKnob" />
</p>

<h1 align="center">LumaKnob</h1>

<p align="center">Windows brightness control below the hardware floor.</p>

<p align="center">
  <a href="https://github.com/Humancannonball/LumaKnob/actions/workflows/ci.yml"><img src="https://github.com/Humancannonball/LumaKnob/actions/workflows/ci.yml/badge.svg" alt="CI" /></a>
  <a href="LICENSE"><img src="https://img.shields.io/badge/license-MIT-a7ed79.svg" alt="MIT" /></a>
  <img src="https://img.shields.io/badge/Windows-x64-5aa9e6.svg" alt="Windows x64" />
  <img src="https://img.shields.io/badge/status-alpha-f3a923.svg" alt="Alpha" />
</p>

<p align="center">
  <a href="https://github.com/Humancannonball/LumaKnob/releases/download/v0.1.0-alpha.2/LumaKnob.exe"><strong>Download LumaKnob.exe</strong></a>
</p>

![LumaKnob control deck](design/device-test-ui.png)

LumaKnob combines the controls available for each display:

- WMI for laptop panels;
- DDC/CI for external monitors;
- a software shade for continuous dimming below the hardware minimum;
- optional gamma and warmth controls for SDR displays.

Every monitor gets its own brightness control. A smaller **STEP** knob chooses
the increment, from 0.01% to 25%.

## Run it

1. [Download `LumaKnob.exe`](https://github.com/Humancannonball/LumaKnob/releases/download/v0.1.0-alpha.2/LumaKnob.exe).
2. Open it.

That is all. No installer, extraction, administrator rights, or separate .NET
runtime is required.

This is an unsigned alpha, so Windows may show a SmartScreen warning. Do not
disable Windows security features.

## Controls

- The large knobs control brightness per monitor.
- **STEP** controls dial, wheel, keyboard, gamma, and warmth increments.
- Laptop brightness buttons use STEP while LumaKnob is running.
- Closing the deck leaves quick controls in the notification area.
- Right-click the tray icon for Restore, Control Deck, and Exit.

### Recovery

Press **Ctrl+Alt+Home** to remove all shades, restore color ramps, and request
50% hardware brightness. If needed, **Ctrl+Alt+Delete** opens the undimmed
secure desktop.

## Current limits

- Windows 11 x64 is the primary test target.
- DDC/CI support depends on the monitor, cable, adapter, and dock.
- Gamma and warmth are experimental and disabled for HDR targets.
- Secure desktop, protected video, and exclusive fullscreen content may appear
  above a software shade.
- Settings, startup, hot-plug, and resume recovery are still being built.

Use the [device report](https://github.com/Humancannonball/LumaKnob/issues/new?template=device-report.yml)
for monitor results and [issues](https://github.com/Humancannonball/LumaKnob/issues)
for bugs.

## Build

Requires the .NET 10 SDK:

```shell
dotnet restore LumaKnob.slnx
dotnet build LumaKnob.slnx --configuration Release
dotnet test LumaKnob.slnx --configuration Release --no-build
dotnet run --project src/LumaKnob.Host.Simulator --configuration Release
```

On Windows, `scripts\publish-win-x64.ps1` produces
`artifacts\LumaKnob.exe`. The simulator never touches a physical display.

See [PROJECT.md](PROJECT.md) for architecture and hardware testing, and
[CONTRIBUTING.md](CONTRIBUTING.md) before opening a pull request.

<details>
<summary>Optional download verification</summary>

```powershell
Get-FileHash .\LumaKnob.exe -Algorithm SHA256
```

Compare the result with the SHA-256 value hidden in the matching release notes.
</details>

Original code and artwork are [MIT licensed](LICENSE). Dependency notices are
in [THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md).
