# Appearance presets

Preview a preset from the repository:

```powershell
LumaKnob.exe --appearance=presets\amber-rack.json
```

Copy it to `%LocalAppData%\LumaKnob\appearance.json` to load it by default.
Use `--no-motion` to disable animation.

Presets are data only. They may set known colors plus motion, frame rate, knob
depth, panel depth, and corner radius within safe limits. Invalid values are
ignored. The JSON examples define the complete schema.
