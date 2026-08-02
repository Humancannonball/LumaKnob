using LumaKnob.App.Controls;

namespace LumaKnob.App.Tests;

public sealed class CircularDragMathTests
{
    [Fact]
    public void ClockwiseMotionConsumesPositiveDetents()
    {
        var remainder = 0d;
        var onePercentAngle = DegreesToRadians(2.7);

        var detents = CircularDragMath.ConsumeDetents(
            ref remainder,
            onePercentAngle,
            valueRange: 100,
            increment: 1);

        Assert.Equal(1, detents);
        Assert.Equal(0, remainder, precision: 10);
    }

    [Fact]
    public void CounterClockwiseMotionConsumesNegativeDetents()
    {
        var remainder = 0d;

        var detents = CircularDragMath.ConsumeDetents(
            ref remainder,
            DegreesToRadians(-27),
            valueRange: 100,
            increment: 5);

        Assert.Equal(-2, detents);
        Assert.Equal(0, remainder, precision: 10);
    }

    [Fact]
    public void SmallMovementsAccumulateWithoutLosingPrecision()
    {
        var remainder = 0d;

        var first = CircularDragMath.ConsumeDetents(
            ref remainder,
            DegreesToRadians(1),
            valueRange: 100,
            increment: 1);
        var second = CircularDragMath.ConsumeDetents(
            ref remainder,
            DegreesToRadians(2),
            valueRange: 100,
            increment: 1);

        Assert.Equal(0, first);
        Assert.Equal(1, second);
        Assert.InRange(remainder, 0, DegreesToRadians(0.31));
    }

    [Theory]
    [InlineData(358, -2)]
    [InlineData(-358, 2)]
    public void AngleWrapUsesTheShortDirection(double inputDegrees, double expectedDegrees)
    {
        var normalized = CircularDragMath.NormalizeAngularDelta(DegreesToRadians(inputDegrees));

        Assert.Equal(DegreesToRadians(expectedDegrees), normalized, precision: 10);
    }

    private static double DegreesToRadians(double degrees) => degrees * Math.PI / 180d;
}
