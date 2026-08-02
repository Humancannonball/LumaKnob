using LumaKnob.Engine.Displays;

namespace LumaKnob.Windows.Color;

internal static class GammaRampMath
{
    public const int ChannelLength = 256;
    public const int RampLength = ChannelLength * 3;

    public static ushort[] CreateAdjustedRamp(
        IReadOnlyList<ushort> original,
        DisplayColorAdjustment adjustment)
    {
        ArgumentOutOfRangeException.ThrowIfNotEqual(original.Count, RampLength);
        var adjusted = new ushort[RampLength];
        var warmth = (double)(adjustment.Warmth / 100m);
        var channelScales = new[] { 1d, 1d - (warmth * 0.08d), 1d - (warmth * 0.30d) };
        var inverseGamma = 1d / (double)adjustment.Gamma;

        for (var channel = 0; channel < 3; channel++)
        {
            for (var index = 0; index < ChannelLength; index++)
            {
                var normalized = index / 255d;
                var sourceIndex = (int)Math.Round(
                    Math.Pow(normalized, inverseGamma) * 255d,
                    MidpointRounding.AwayFromZero);
                var calibrated = original[(channel * ChannelLength) + sourceIndex];
                adjusted[(channel * ChannelLength) + index] = checked(
                    (ushort)Math.Clamp(
                        Math.Round(calibrated * channelScales[channel], MidpointRounding.AwayFromZero),
                        ushort.MinValue,
                        ushort.MaxValue));
            }
        }

        return adjusted;
    }
}
