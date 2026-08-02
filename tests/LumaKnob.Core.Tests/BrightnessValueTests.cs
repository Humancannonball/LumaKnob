using LumaKnob.Core.Brightness;

namespace LumaKnob.Core.Tests;

public sealed class BrightnessValueTests
{
    [Theory]
    [InlineData("0", 0)]
    [InlineData("0.01", 1)]
    [InlineData("7.50", 750)]
    [InlineData("99.999", 10_000)]
    [InlineData("100", 10_000)]
    public void FromPercentUsesExactBasisPointStorage(string percent, int expectedBasisPoints)
    {
        var subject = BrightnessValue.FromPercent(decimal.Parse(percent, System.Globalization.CultureInfo.InvariantCulture));

        Assert.Equal(expectedBasisPoints, subject.BasisPoints);
    }

    [Fact]
    public void AdjustClampsAtBothEndsForEveryValueAndPreset()
    {
        foreach (var step in BrightnessStep.Presets)
        {
            for (var basisPoints = 0; basisPoints <= BrightnessValue.MaximumBasisPoints; basisPoints++)
            {
                var subject = new BrightnessValue(basisPoints);

                Assert.InRange(subject.Adjust(step, -1).BasisPoints, 0, BrightnessValue.MaximumBasisPoints);
                Assert.InRange(subject.Adjust(step, 1).BasisPoints, 0, BrightnessValue.MaximumBasisPoints);
            }
        }
    }

    [Fact]
    public void AdjustUpThenDownReturnsToStartAwayFromBoundaries()
    {
        foreach (var step in BrightnessStep.Presets)
        {
            for (var basisPoints = step.BasisPoints;
                 basisPoints <= BrightnessValue.MaximumBasisPoints - step.BasisPoints;
                 basisPoints++)
            {
                var subject = new BrightnessValue(basisPoints);

                Assert.Equal(subject, subject.Adjust(step, 1).Adjust(step, -1));
            }
        }
    }

    [Fact]
    public void PresetsFollowTheDocumentedEngineeringSeries()
    {
        Assert.Equal(
            [1, 2, 5, 10, 20, 50, 100, 200, 500, 1_000, 2_000, 2_500],
            BrightnessStep.Presets.Select(step => step.BasisPoints));
    }
}
