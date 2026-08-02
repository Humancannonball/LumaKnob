namespace LumaKnob.Core.Brightness;

/// <summary>
/// Converts the user's logical value into hardware and shade targets.
/// </summary>
public static class HybridBrightnessPolicy
{
    public static BrightnessPlan CreatePlan(
        BrightnessValue logicalBrightness,
        BrightnessValue hardwareShadePivot,
        HardwareBrightnessCapability? hardware)
    {
        if (hardwareShadePivot == BrightnessValue.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(hardwareShadePivot), "The hybrid pivot must be above zero.");
        }

        if (hardware is null)
        {
            var shadeOnlyTransmission = logicalBrightness.BasisPoints / (decimal)BrightnessValue.MaximumBasisPoints;
            var shadeOnlyMechanisms = logicalBrightness == BrightnessValue.Maximum
                ? BrightnessMechanism.None
                : BrightnessMechanism.Shade;

            return new BrightnessPlan(logicalBrightness, null, shadeOnlyTransmission, shadeOnlyMechanisms);
        }

        var logicalFraction = logicalBrightness.BasisPoints / (decimal)BrightnessValue.MaximumBasisPoints;
        var pivotFraction = hardwareShadePivot.BasisPoints / (decimal)BrightnessValue.MaximumBasisPoints;
        var hardwareFraction = Math.Max(logicalFraction, pivotFraction);
        var hardwareValue = hardware.MapNormalized(hardwareFraction);

        if (hardware.IsDiscrete)
        {
            var actualHardwareFraction = hardware.NormalizeNative(hardwareValue);
            var discreteTransmission = actualHardwareFraction == 0m
                ? 0m
                : Math.Clamp(logicalFraction / actualHardwareFraction, 0m, 1m);
            var discreteMechanisms = BrightnessMechanism.Hardware;
            if (discreteTransmission < 1m)
            {
                discreteMechanisms |= BrightnessMechanism.Shade;
            }

            return new BrightnessPlan(
                logicalBrightness,
                hardwareValue,
                discreteTransmission,
                discreteMechanisms);
        }

        if (logicalBrightness >= hardwareShadePivot)
        {
            return new BrightnessPlan(
                logicalBrightness,
                hardwareValue,
                1m,
                BrightnessMechanism.Hardware);
        }

        var shadeTransmission = logicalBrightness.BasisPoints / (decimal)hardwareShadePivot.BasisPoints;
        return new BrightnessPlan(
            logicalBrightness,
            hardwareValue,
            shadeTransmission,
            BrightnessMechanism.Hardware | BrightnessMechanism.Shade);
    }
}
