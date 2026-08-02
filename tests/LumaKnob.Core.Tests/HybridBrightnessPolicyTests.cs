using LumaKnob.Core.Brightness;

namespace LumaKnob.Core.Tests;

public sealed class HybridBrightnessPolicyTests
{
    private static readonly BrightnessValue Pivot = BrightnessValue.FromPercent(10m);
    private static readonly HardwareBrightnessCapability Hardware = new(0, 100);

    [Fact]
    public void AtPivotUsesHardwareWithoutShade()
    {
        var plan = HybridBrightnessPolicy.CreatePlan(Pivot, Pivot, Hardware);

        Assert.Equal(10, plan.HardwareValue);
        Assert.Equal(1m, plan.ShadeTransmission);
        Assert.Equal(BrightnessMechanism.Hardware, plan.Mechanisms);
    }

    [Fact]
    public void BelowPivotHoldsHardwareAndExtendsRangeWithShade()
    {
        var plan = HybridBrightnessPolicy.CreatePlan(BrightnessValue.FromPercent(2.5m), Pivot, Hardware);

        Assert.Equal(10, plan.HardwareValue);
        Assert.Equal(0.25m, plan.ShadeTransmission);
        Assert.Equal(0.75m, plan.ShadeOpacity);
        Assert.Equal(BrightnessMechanism.Hardware | BrightnessMechanism.Shade, plan.Mechanisms);
    }

    [Fact]
    public void ZeroIsRepresentableAndRecoverableByTheCoordinator()
    {
        var plan = HybridBrightnessPolicy.CreatePlan(BrightnessValue.Zero, Pivot, Hardware);

        Assert.Equal(10, plan.HardwareValue);
        Assert.Equal(0m, plan.ShadeTransmission);
        Assert.Equal(1m, plan.ShadeOpacity);
    }

    [Fact]
    public void MissingHardwareProducesShadeOnlyPlan()
    {
        var plan = HybridBrightnessPolicy.CreatePlan(BrightnessValue.FromPercent(40m), Pivot, null);

        Assert.Null(plan.HardwareValue);
        Assert.Equal(0.4m, plan.ShadeTransmission);
        Assert.Equal(BrightnessMechanism.Shade, plan.Mechanisms);
    }

    [Fact]
    public void DiscreteHardwareChoosesUpperLevelAndShadesTheGap()
    {
        var discreteHardware = new HardwareBrightnessCapability(0, 100, [0, 20, 40, 60, 80, 100]);

        var plan = HybridBrightnessPolicy.CreatePlan(BrightnessValue.FromPercent(51m), Pivot, discreteHardware);

        Assert.Equal(60, plan.HardwareValue);
        Assert.Equal(0.85m, plan.ShadeTransmission);
        Assert.Equal(BrightnessMechanism.Hardware | BrightnessMechanism.Shade, plan.Mechanisms);
    }

    [Fact]
    public void DiscreteHardwareAlsoKeepsSubPivotOutputContinuous()
    {
        var discreteHardware = new HardwareBrightnessCapability(0, 100, [0, 20, 40, 60, 80, 100]);

        var plan = HybridBrightnessPolicy.CreatePlan(BrightnessValue.FromPercent(2.5m), Pivot, discreteHardware);

        Assert.Equal(20, plan.HardwareValue);
        Assert.Equal(0.125m, plan.ShadeTransmission);
    }

    [Fact]
    public void ZeroPivotIsRejectedBecauseTheHybridRatioWouldBeUndefined()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => HybridBrightnessPolicy.CreatePlan(BrightnessValue.FromPercent(50m), BrightnessValue.Zero, Hardware));
    }
}
