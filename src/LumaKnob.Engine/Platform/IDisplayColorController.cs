using LumaKnob.Engine.Displays;

namespace LumaKnob.Engine.Platform;

public interface IDisplayColorController : IDisposable
{
    string BackendLabel { get; }

    bool CanApply(BrightnessTargetInfo target);

    string DescribeAvailability(BrightnessTargetInfo target);

    void Apply(BrightnessTargetInfo target, DisplayColorAdjustment adjustment);

    void RestoreAll();
}
