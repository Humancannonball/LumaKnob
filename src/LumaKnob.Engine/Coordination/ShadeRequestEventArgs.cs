using LumaKnob.Engine.Displays;

namespace LumaKnob.Engine.Coordination;

public sealed class ShadeRequestEventArgs(
    string targetId,
    DisplayPixelBounds bounds,
    decimal opacity) : EventArgs
{
    public string TargetId { get; } = targetId;

    public DisplayPixelBounds Bounds { get; } = bounds;

    public decimal Opacity { get; } = opacity;
}
