namespace LumaKnob.Core.Brightness;

/// <summary>
/// The native brightness domain exposed by one hardware backend.
/// </summary>
public sealed class HardwareBrightnessCapability
{
    private readonly int[]? _supportedLevels;

    public HardwareBrightnessCapability(int minimum, int maximum, IEnumerable<int>? supportedLevels = null)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThan(minimum, maximum);

        Minimum = minimum;
        Maximum = maximum;

        if (supportedLevels is null)
        {
            return;
        }

        _supportedLevels = supportedLevels
            .Distinct()
            .Order()
            .ToArray();

        if (_supportedLevels.Length == 0 || _supportedLevels.Any(level => level < minimum || level > maximum))
        {
            throw new ArgumentException("Supported levels must be non-empty and inside the native range.", nameof(supportedLevels));
        }
    }

    public int Minimum { get; }

    public int Maximum { get; }

    public IReadOnlyList<int>? SupportedLevels => _supportedLevels;

    public bool IsDiscrete => _supportedLevels is not null;

    public int MapNormalized(decimal normalized)
    {
        if (normalized is < 0m or > 1m)
        {
            throw new ArgumentOutOfRangeException(nameof(normalized));
        }

        var continuous = Minimum + ((Maximum - Minimum) * normalized);
        var desired = decimal.ToInt32(decimal.Round(continuous, 0, MidpointRounding.AwayFromZero));

        if (_supportedLevels is null)
        {
            return desired;
        }

        // A shade can reduce a slightly brighter hardware level, but it cannot
        // add light to a darker one. Choose the first level at or above the
        // ideal target so the policy can fill the discrete gap continuously.
        return _supportedLevels.FirstOrDefault(level => level >= desired, _supportedLevels[^1]);
    }

    public decimal NormalizeNative(int nativeValue)
    {
        if (nativeValue < Minimum || nativeValue > Maximum)
        {
            throw new ArgumentOutOfRangeException(nameof(nativeValue));
        }

        if (Minimum == Maximum)
        {
            return 1m;
        }

        return (nativeValue - Minimum) / (decimal)(Maximum - Minimum);
    }
}
