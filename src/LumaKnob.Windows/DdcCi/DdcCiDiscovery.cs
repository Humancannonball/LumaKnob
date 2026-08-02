using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using LumaKnob.Core.Brightness;
using LumaKnob.Engine.Displays;
using LumaKnob.Windows.Displays;
using LumaKnob.Windows.Interop;

namespace LumaKnob.Windows.DdcCi;

[SupportedOSPlatform("windows")]
internal static class DdcCiDiscovery
{
    public static IReadOnlyList<IBrightnessTarget> Discover(
        LogicalMonitor logicalMonitor,
        string? friendlyName,
        bool? advancedColorEnabled,
        ICollection<string> diagnostics)
    {
        var targets = new List<IBrightnessTarget>();

        if (!MonitorNative.GetNumberOfPhysicalMonitorsFromHMONITOR(logicalMonitor.Handle, out var count) || count == 0)
        {
            diagnostics.Add($"{logicalMonitor.DeviceName}: no physical-monitor handles were exposed for DDC/CI.");
            return targets;
        }

        var physicalMonitors = new NativePhysicalMonitor[checked((int)count)];
        if (!MonitorNative.GetPhysicalMonitorsFromHMONITOR(logicalMonitor.Handle, count, physicalMonitors))
        {
            diagnostics.Add($"{logicalMonitor.DeviceName}: physical-monitor enumeration failed ({Marshal.GetLastWin32Error()}).");
            return targets;
        }

        for (var index = 0; index < physicalMonitors.Length; index++)
        {
            var physical = physicalMonitors[index];
            try
            {
                if (!MonitorNative.GetMonitorBrightness(physical.Handle, out var minimum, out var current, out var maximum))
                {
                    diagnostics.Add($"{logicalMonitor.DeviceName} physical target {index + 1}: DDC/CI brightness probe failed ({Marshal.GetLastWin32Error()}).");
                    _ = MonitorNative.DestroyPhysicalMonitor(physical.Handle);
                    continue;
                }

                if (maximum < minimum || current < minimum || current > maximum)
                {
                    diagnostics.Add($"{logicalMonitor.DeviceName} physical target {index + 1}: DDC/CI returned an invalid range.");
                    _ = MonitorNative.DestroyPhysicalMonitor(physical.Handle);
                    continue;
                }

                var fallbackName = string.IsNullOrWhiteSpace(physical.Description)
                    ? $"External display {index + 1}"
                    : physical.Description.Trim();
                var name = string.IsNullOrWhiteSpace(friendlyName)
                    ? fallbackName
                    : physicalMonitors.Length == 1
                        ? friendlyName.Trim()
                        : $"{friendlyName.Trim()} {index + 1}";
                var id = $"ddc:{logicalMonitor.DeviceName}:{index}";
                var capability = new HardwareBrightnessCapability(checked((int)minimum), checked((int)maximum));
                var currentPercent = maximum > minimum
                    ? ((decimal)current - minimum) * 100m / (maximum - minimum)
                    : 100m;

                targets.Add(new DdcCiBrightnessTarget(
                    physical.Handle,
                    new BrightnessTargetInfo(
                        id,
                        name,
                        "DDC/CI + SHADE",
                        "DDC/CI",
                        logicalMonitor.Bounds,
                        capability,
                        $"Initial DDC/CI reading: {currentPercent:0.##}%",
                        PlatformDisplayName: logicalMonitor.DeviceName,
                        AdvancedColorEnabled: advancedColorEnabled)));
            }
            catch (Exception exception)
            {
                diagnostics.Add($"{logicalMonitor.DeviceName} physical target {index + 1}: probe rejected ({exception.Message}).");
                _ = MonitorNative.DestroyPhysicalMonitor(physical.Handle);
            }
        }

        return targets;
    }
}
