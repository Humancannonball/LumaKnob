using LumaKnob.Core.Brightness;
using LumaKnob.Engine.Displays;

namespace LumaKnob.Windows.Shade;

internal sealed class ShadeOnlyBrightnessTarget : IBrightnessTarget
{
    public ShadeOnlyBrightnessTarget(BrightnessTargetInfo info) => Info = info;

    public BrightnessTargetInfo Info { get; }

    public ValueTask<BrightnessValue> ReadBrightnessAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return ValueTask.FromResult(BrightnessValue.Maximum);
    }

    public ValueTask SetNativeBrightnessAsync(int nativeValue, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return ValueTask.CompletedTask;
    }

    public void Dispose()
    {
    }
}
