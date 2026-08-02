namespace LumaKnob.Core.Brightness;

[Flags]
public enum BrightnessMechanism
{
    None = 0,
    Hardware = 1,
    Shade = 2,
}

/// <summary>
/// A pure plan. Platform adapters decide how and when to apply it.
/// </summary>
public sealed record BrightnessPlan(
    BrightnessValue LogicalBrightness,
    int? HardwareValue,
    decimal ShadeTransmission,
    BrightnessMechanism Mechanisms)
{
    public decimal ShadeOpacity => 1m - ShadeTransmission;
}
