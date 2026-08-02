# Contributing to LumaKnob

Thanks for helping make low-light display control safer and more useful.
Focused bug fixes, device reports, tests, documentation, accessibility work,
and original visual improvements are welcome.

## Start safely

Most development does not need a real monitor backend. Use the simulator:

```shell
dotnet restore LumaKnob.slnx
dotnet build LumaKnob.slnx --configuration Release
dotnet test LumaKnob.slnx --configuration Release --no-build
dotnet run --project src/LumaKnob.Host.Simulator --configuration Release
```

Only run `LumaKnob.Host.Windows` after reading section 18 of `PROJECT.md`. Keep
`Ctrl+Alt+Home` and the tray Exit command available. Never begin hardware tests
inside Remote Desktop, a fullscreen game, protected video, or an unsaved work
session.

## Architecture rules

- `LumaKnob.Core` contains deterministic values and brightness policy only.
- `LumaKnob.Engine` coordinates portable display sessions and never references
  Avalonia or Windows.
- `LumaKnob.Windows` owns WMI, DDC/CI, DisplayConfig, GDI, shade styling, and Raw
  Input.
- `LumaKnob.App` owns presentation and consumes platform-neutral contracts.
- Native hardware work stays off the UI thread, is serialized per display, and
  must remain recoverable when it fails.
- Never add elevation, a service, kernel driver, telemetry, or monitor
  nonvolatile-setting writes.

## Pull requests

1. Open or reference a focused issue when the behavior or design needs
   discussion.
2. Create a small branch and keep unrelated changes separate.
3. Add tests for policy, state, parsing, or failure behavior where practical.
4. Run:

   ```shell
   dotnet format LumaKnob.slnx --verify-no-changes
   dotnet build LumaKnob.slnx --configuration Release
   dotnet test LumaKnob.slnx --configuration Release --no-build
   ```

5. Sign every commit for the [Developer Certificate of Origin](https://developercertificate.org/):

   ```shell
   git commit -s -m "Describe the change"
   ```

6. Explain user impact, recovery implications, and physical hardware tested in
   the pull request.

## Hardware reports

Use the device-report issue form. Include Windows version, GPU/driver, monitor
model, connection path, DDC/CI state, HDR state, and the precise test sequence.
Do not publish EDID serial numbers, usernames, or unrelated diagnostic data.

## Visual contributions

The interface uses original, code-drawn equipment controls. Do not submit
copied Winamp skins, Windows artwork, proprietary fonts, or look-alike branded
assets. Any third-party file must have a redistribution-compatible license and
an entry in `THIRD_PARTY_NOTICES.md`.
