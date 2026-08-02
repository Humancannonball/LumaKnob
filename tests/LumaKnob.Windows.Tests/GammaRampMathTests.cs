using LumaKnob.Engine.Displays;
using LumaKnob.Windows.Color;

namespace LumaKnob.Windows.Tests;

public sealed class GammaRampMathTests
{
    [Fact]
    public void NeutralAdjustmentPreservesTheCapturedRamp()
    {
        var original = CreateIdentityRamp();

        var adjusted = GammaRampMath.CreateAdjustedRamp(original, DisplayColorAdjustment.Neutral);

        Assert.Equal(original, adjusted);
    }

    [Fact]
    public void HigherGammaRaisesMidtonesWithoutMovingBlackOrRedWhite()
    {
        var original = CreateIdentityRamp();

        var adjusted = GammaRampMath.CreateAdjustedRamp(
            original,
            new DisplayColorAdjustment(1.5m, 0m));

        Assert.Equal(0, adjusted[0]);
        Assert.True(adjusted[128] > original[128]);
        Assert.Equal(ushort.MaxValue, adjusted[255]);
    }

    [Fact]
    public void WarmthPreservesRedAndReducesBlueMoreThanGreen()
    {
        var original = CreateIdentityRamp();

        var adjusted = GammaRampMath.CreateAdjustedRamp(
            original,
            new DisplayColorAdjustment(1m, 100m));

        Assert.Equal(original[255], adjusted[255]);
        Assert.True(adjusted[511] < original[511]);
        Assert.True(adjusted[767] < adjusted[511]);
    }

    private static ushort[] CreateIdentityRamp()
    {
        var ramp = new ushort[GammaRampMath.RampLength];
        for (var channel = 0; channel < 3; channel++)
        {
            for (var index = 0; index < GammaRampMath.ChannelLength; index++)
            {
                ramp[(channel * GammaRampMath.ChannelLength) + index] =
                    checked((ushort)(index * 257));
            }
        }

        return ramp;
    }
}
