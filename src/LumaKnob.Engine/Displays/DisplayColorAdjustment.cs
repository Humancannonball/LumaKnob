namespace LumaKnob.Engine.Displays;

public readonly record struct DisplayColorAdjustment
{
    public DisplayColorAdjustment(decimal gamma, decimal warmth)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(gamma, 0.5m);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(gamma, 1.5m);
        ArgumentOutOfRangeException.ThrowIfLessThan(warmth, 0m);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(warmth, 100m);

        Gamma = gamma;
        Warmth = warmth;
    }

    public decimal Gamma { get; }

    public decimal Warmth { get; }

    public bool IsNeutral => Gamma == 1m && Warmth == 0m;

    public static DisplayColorAdjustment Neutral => new(1m, 0m);
}
