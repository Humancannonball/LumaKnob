# Contributing

Use the simulator for normal development:

```shell
dotnet restore LumaKnob.slnx
dotnet build LumaKnob.slnx --configuration Release
dotnet test LumaKnob.slnx --configuration Release --no-build
dotnet run --project src/LumaKnob.Host.Simulator --configuration Release
```

Keep the boundaries simple:

- Core: values and brightness policy.
- Engine: display state and coordination.
- Windows: native APIs.
- App: UI only.

Native calls stay off the UI thread and failures must remain recoverable. Do
not add elevation, services, drivers, telemetry, or nonvolatile monitor writes.

Before a pull request:

```shell
dotnet format LumaKnob.slnx --verify-no-changes
dotnet build LumaKnob.slnx --configuration Release
dotnet test LumaKnob.slnx --configuration Release --no-build
```

Sign commits with `git commit -s`. For hardware changes, follow the short test
in `PROJECT.md` and state what was physically tested. Never publish EDID serial
numbers or usernames.

Visual work must be original or permissively licensed. Record every bundled
third-party file in `THIRD_PARTY_NOTICES.md`.
