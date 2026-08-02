using LumaKnob.Windows.Shell;

namespace LumaKnob.Windows.Tests;

public sealed class BrightnessHidUsageTests
{
    [Theory]
    [InlineData(BrightnessHidUsage.BrightnessIncrement, 1)]
    [InlineData(BrightnessHidUsage.BrightnessDecrement, -1)]
    public void RecognizesStandardConsumerBrightnessUsages(ushort usage, int expectedDirection)
    {
        HidUsageAndPage[] usages = [new(usage, BrightnessHidUsage.ConsumerUsagePage)];

        var direction = BrightnessHidUsage.GetDirection(usages);

        Assert.Equal(expectedDirection, direction);
    }

    [Fact]
    public void IgnoresOtherConsumerControlsAndUsagePages()
    {
        HidUsageAndPage[] usages =
        [
            new(0x00E9, BrightnessHidUsage.ConsumerUsagePage),
            new(BrightnessHidUsage.BrightnessIncrement, 0x0001),
        ];

        var direction = BrightnessHidUsage.GetDirection(usages);

        Assert.Equal(0, direction);
    }

    [Fact]
    public void EmptyUsageListRepresentsButtonRelease()
    {
        var direction = BrightnessHidUsage.GetDirection([]);

        Assert.Equal(0, direction);
    }
}
