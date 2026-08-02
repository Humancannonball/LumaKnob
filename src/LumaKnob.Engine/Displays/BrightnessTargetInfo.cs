using LumaKnob.Core.Brightness;

namespace LumaKnob.Engine.Displays;

public sealed record BrightnessTargetInfo(
    string Id,
    string Name,
    string BackendLabel,
    string? HardwareBackendLabel,
    DisplayPixelBounds Bounds,
    HardwareBrightnessCapability? HardwareCapability,
    string? Notice = null,
    bool IsSimulator = false,
    string? PlatformDisplayName = null,
    bool? AdvancedColorEnabled = null);
