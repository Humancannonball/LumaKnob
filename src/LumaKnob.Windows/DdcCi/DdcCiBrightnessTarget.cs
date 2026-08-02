using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using LumaKnob.Core.Brightness;
using LumaKnob.Engine.Displays;
using LumaKnob.Windows.Interop;

namespace LumaKnob.Windows.DdcCi;

[SupportedOSPlatform("windows")]
internal sealed class DdcCiBrightnessTarget : IBrightnessTarget
{
    private readonly object _gate = new();
    private nint _physicalMonitor;

    public DdcCiBrightnessTarget(
        nint physicalMonitor,
        BrightnessTargetInfo info)
    {
        _physicalMonitor = physicalMonitor;
        Info = info;
    }

    public BrightnessTargetInfo Info { get; }

    public ValueTask<BrightnessValue> ReadBrightnessAsync(CancellationToken cancellationToken) =>
        new(Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();

            lock (_gate)
            {
                var handle = GetLiveHandle();
                if (!MonitorNative.GetMonitorBrightness(handle, out var minimum, out var current, out var maximum))
                {
                    throw new Win32Exception(Marshal.GetLastWin32Error(), $"DDC/CI read failed for {Info.Name}.");
                }

                return ToLogicalBrightness(minimum, current, maximum);
            }
        }, cancellationToken));

    public ValueTask SetNativeBrightnessAsync(int nativeValue, CancellationToken cancellationToken) =>
        new(Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();

            lock (_gate)
            {
                var handle = GetLiveHandle();
                if (!MonitorNative.SetMonitorBrightness(handle, checked((uint)nativeValue)))
                {
                    throw new Win32Exception(Marshal.GetLastWin32Error(), $"DDC/CI write failed for {Info.Name}.");
                }
            }
        }, cancellationToken));

    public void Dispose()
    {
        lock (_gate)
        {
            if (_physicalMonitor == 0)
            {
                return;
            }

            _ = MonitorNative.DestroyPhysicalMonitor(_physicalMonitor);
            _physicalMonitor = 0;
        }
    }

    private nint GetLiveHandle()
    {
        ObjectDisposedException.ThrowIf(_physicalMonitor == 0, this);
        return _physicalMonitor;
    }

    private static BrightnessValue ToLogicalBrightness(uint minimum, uint current, uint maximum)
    {
        if (maximum <= minimum)
        {
            return BrightnessValue.Maximum;
        }

        var normalizedPercent = ((decimal)current - minimum) * 100m / (maximum - minimum);
        return BrightnessValue.FromPercent(Math.Clamp(normalizedPercent, 0m, 100m));
    }
}
