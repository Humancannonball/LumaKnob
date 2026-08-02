using System.Globalization;

namespace LumaKnob.Core.Brightness;

/// <summary>
/// A logical brightness command stored as integer basis points (0.01%).
/// This is intentionally not a claim about physical luminance.
/// </summary>
public readonly record struct BrightnessValue : IComparable<BrightnessValue>
{
    public const int MinimumBasisPoints = 0;
    public const int MaximumBasisPoints = 10_000;

    public BrightnessValue(int basisPoints)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(basisPoints, MinimumBasisPoints);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(basisPoints, MaximumBasisPoints);
        BasisPoints = basisPoints;
    }

    public int BasisPoints { get; }

    public decimal Percent => BasisPoints / 100m;

    public static BrightnessValue Zero => new(MinimumBasisPoints);

    public static BrightnessValue Maximum => new(MaximumBasisPoints);

    public static BrightnessValue FromPercent(decimal percent)
    {
        if (percent is < 0m or > 100m)
        {
            throw new ArgumentOutOfRangeException(nameof(percent), percent, "Brightness must be between 0 and 100 percent.");
        }

        var basisPoints = decimal.ToInt32(decimal.Round(percent * 100m, 0, MidpointRounding.AwayFromZero));
        return new BrightnessValue(basisPoints);
    }

    public BrightnessValue Adjust(BrightnessStep step, int detents)
    {
        var candidate = (long)BasisPoints + ((long)step.BasisPoints * detents);
        return new BrightnessValue((int)Math.Clamp(candidate, MinimumBasisPoints, MaximumBasisPoints));
    }

    public int CompareTo(BrightnessValue other) => BasisPoints.CompareTo(other.BasisPoints);

    public static bool operator <(BrightnessValue left, BrightnessValue right) =>
        left.BasisPoints < right.BasisPoints;

    public static bool operator <=(BrightnessValue left, BrightnessValue right) =>
        left.BasisPoints <= right.BasisPoints;

    public static bool operator >(BrightnessValue left, BrightnessValue right) =>
        left.BasisPoints > right.BasisPoints;

    public static bool operator >=(BrightnessValue left, BrightnessValue right) =>
        left.BasisPoints >= right.BasisPoints;

    public override string ToString() => Percent.ToString("0.00'%'", CultureInfo.InvariantCulture);
}
