using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using LumaKnob.Engine.Displays;
using LumaKnob.Windows.Interop;

namespace LumaKnob.Windows.Displays;

[SupportedOSPlatform("windows")]
internal static class LogicalMonitorEnumerator
{
    public static IReadOnlyList<LogicalMonitor> Enumerate()
    {
        var monitors = new List<LogicalMonitor>();
        MonitorNative.MonitorEnumCallback callback = (
            nint monitor,
            nint _,
            ref NativeRect bounds,
            nint _) =>
        {
            var info = new NativeMonitorInfo
            {
                Size = (uint)Marshal.SizeOf<NativeMonitorInfo>(),
                DeviceName = string.Empty,
            };

            if (!MonitorNative.GetMonitorInfo(monitor, ref info))
            {
                return true;
            }

            monitors.Add(new LogicalMonitor(
                monitor,
                info.DeviceName,
                new DisplayPixelBounds(
                    bounds.Left,
                    bounds.Top,
                    bounds.Right - bounds.Left,
                    bounds.Bottom - bounds.Top),
                (info.Flags & MonitorNative.MonitorInfoPrimary) != 0));

            return true;
        };

        if (!MonitorNative.EnumDisplayMonitors(0, 0, callback, 0))
        {
            throw new Win32Exception(Marshal.GetLastWin32Error(), "EnumDisplayMonitors failed.");
        }

        return monitors;
    }
}

internal sealed record LogicalMonitor(
    nint Handle,
    string DeviceName,
    DisplayPixelBounds Bounds,
    bool IsPrimary);
