using System.Collections.ObjectModel;
using System.Globalization;

namespace LumaKnob.Core.Brightness;

/// <summary>
/// The independent amount by which one brightness detent changes the value.
/// </summary>
public readonly record struct BrightnessStep
{
    public const int MinimumBasisPoints = 1;
    public const int MaximumBasisPoints = 2_500;

    private static readonly ReadOnlyCollection<BrightnessStep> BuiltInPresets = Array.AsReadOnly(
    new BrightnessStep[]
    {
        new(1),
        new(2),
        new(5),
        new(10),
        new(20),
        new(50),
        new(100),
        new(200),
        new(500),
        new(1_000),
        new(2_000),
        new(2_500),
    });

    public BrightnessStep(int basisPoints)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(basisPoints, MinimumBasisPoints);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(basisPoints, MaximumBasisPoints);
        BasisPoints = basisPoints;
    }

    public int BasisPoints { get; }

    public decimal Percent => BasisPoints / 100m;

    public static IReadOnlyList<BrightnessStep> Presets => BuiltInPresets;

    public override string ToString() => Percent.ToString("0.00'%'", CultureInfo.InvariantCulture);
}
