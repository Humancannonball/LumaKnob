using LumaKnob.Core.Brightness;

namespace LumaKnob.Engine.Displays;

public interface IBrightnessTarget : IDisposable
{
    BrightnessTargetInfo Info { get; }

    ValueTask<BrightnessValue> ReadBrightnessAsync(CancellationToken cancellationToken);

    ValueTask SetNativeBrightnessAsync(int nativeValue, CancellationToken cancellationToken);
}
