# LumaKnob appearance presets

The included presets contain data only: they cannot execute code or replace
Avalonia markup. Start LumaKnob with one directly:

```powershell
LumaKnob.exe --appearance=presets\amber-rack.json
```

Copy the chosen file to
`%LocalAppData%\LumaKnob\appearance.json` to load it automatically. The header's
`FX LIVE` button pauses motion for the current session. `--no-motion` disables
it at startup regardless of the preset.

Schema 2 accepts `#RRGGBB` and `#AARRGGBB` colors for the named tokens in the
example files. Alpha allows subdued backplane layers. Unknown token names and
invalid colors are ignored.

The optional `visuals` object accepts:

- `motionEnabled`: `true` or `false`;
- `motionSpeed`: `0.1` through `3.0`;
- `motionIntensity`: `0.0` through `1.0`;
- `framesPerSecond`: `10` through `60`;
- `knobDepth`: `0.5` through `2.5`;
- `panelDepth`: `0.5` through `3.0`;
- `cornerRadius`: `0` through `12`.

Out-of-range values fall back to the built-in value without discarding the
rest of the preset. Color-only schema-1 presets remain compatible.
