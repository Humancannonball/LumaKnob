using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using LumaKnob.Engine.Displays;
using LumaKnob.Engine.Platform;

namespace LumaKnob.Windows.Color;

[SupportedOSPlatform("windows")]
internal sealed class WindowsGammaRampColorController : IDisplayColorController
{
    private const int RampLength = GammaRampMath.RampLength;
    private readonly object _gate = new();
    private readonly Dictionary<string, ushort[]> _originalRamps = new(StringComparer.OrdinalIgnoreCase);
    private string? _activeDisplayName;
    private bool _disposed;

    public string BackendLabel => "GDI GAMMA RAMP";

    public bool CanApply(BrightnessTargetInfo target) =>
        !_disposed &&
        !string.IsNullOrWhiteSpace(target.PlatformDisplayName) &&
        target.AdvancedColorEnabled != true;

    public string DescribeAvailability(BrightnessTargetInfo target)
    {
        if (target.AdvancedColorEnabled == true)
        {
            return "COLOR LOCKED · HDR is enabled for this display; switch it to SDR before using gamma or warmth.";
        }

        if (string.IsNullOrWhiteSpace(target.PlatformDisplayName))
        {
            return "COLOR UNAVAILABLE · Windows did not expose a display-device name for this target.";
        }

        return "COLOR READY · changes apply immediately; SDR only and potentially shared by displays on one GPU.";
    }

    public void Apply(BrightnessTargetInfo target, DisplayColorAdjustment adjustment)
    {
        lock (_gate)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            if (string.IsNullOrWhiteSpace(target.PlatformDisplayName) || target.AdvancedColorEnabled == true)
            {
                throw new InvalidOperationException(DescribeAvailability(target));
            }

            var displayName = target.PlatformDisplayName!;
            if (_activeDisplayName is not null &&
                !string.Equals(_activeDisplayName, displayName, StringComparison.OrdinalIgnoreCase))
            {
                RestoreDisplay(_activeDisplayName);
                _activeDisplayName = null;
            }

            var original = CaptureOriginal(displayName);
            if (adjustment.IsNeutral)
            {
                SetRamp(displayName, original);
                _originalRamps.Remove(displayName);
                _activeDisplayName = null;
                return;
            }

            var adjusted = GammaRampMath.CreateAdjustedRamp(original, adjustment);
            _activeDisplayName = displayName;
            try
            {
                SetRamp(displayName, adjusted);
            }
            catch
            {
                RestoreDisplay(displayName);
                _activeDisplayName = null;
                throw;
            }
        }
    }

    public void RestoreAll()
    {
        lock (_gate)
        {
            if (_disposed)
            {
                return;
            }

            if (_activeDisplayName is not null)
            {
                RestoreDisplay(_activeDisplayName);
                _activeDisplayName = null;
            }
        }
    }

    public void Dispose()
    {
        lock (_gate)
        {
            if (_disposed)
            {
                return;
            }

            if (_activeDisplayName is not null)
            {
                RestoreDisplay(_activeDisplayName);
                _activeDisplayName = null;
            }

            _disposed = true;
        }
    }

    private ushort[] CaptureOriginal(string displayName)
    {
        if (_originalRamps.TryGetValue(displayName, out var existing))
        {
            return existing;
        }

        var original = GetRamp(displayName);
        _originalRamps.Add(displayName, original);
        return original;
    }

    private void RestoreDisplay(string displayName)
    {
        if (_originalRamps.TryGetValue(displayName, out var original))
        {
            SetRamp(displayName, original);
            _originalRamps.Remove(displayName);
        }
    }

    private static ushort[] GetRamp(string displayName)
    {
        var ramp = new ushort[RampLength];
        WithDisplayDc(displayName, dc =>
        {
            if (!NativeMethods.GetDeviceGammaRamp(dc, ramp))
            {
                throw new Win32Exception(
                    Marshal.GetLastWin32Error(),
                    $"Windows could not read the color ramp for {displayName}.");
            }
        });
        return ramp;
    }

    private static void SetRamp(string displayName, ushort[] ramp) =>
        WithDisplayDc(displayName, dc =>
        {
            if (!NativeMethods.SetDeviceGammaRamp(dc, ramp))
            {
                throw new Win32Exception(
                    Marshal.GetLastWin32Error(),
                    $"Windows rejected the color ramp for {displayName}.");
            }
        });

    private static void WithDisplayDc(string displayName, Action<nint> action)
    {
        var dc = NativeMethods.CreateDC(displayName, displayName, null, 0);
        if (dc == 0)
        {
            throw new Win32Exception(
                Marshal.GetLastWin32Error(),
                $"Windows could not open a display context for {displayName}.");
        }

        try
        {
            action(dc);
        }
        finally
        {
            _ = NativeMethods.DeleteDC(dc);
        }
    }

    private static class NativeMethods
    {
        [DllImport("gdi32.dll", EntryPoint = "CreateDCW", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern nint CreateDC(
            string driver,
            string device,
            string? output,
            nint initializationData);

        [DllImport("gdi32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool DeleteDC(nint deviceContext);

        [DllImport("gdi32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetDeviceGammaRamp(nint deviceContext, [Out] ushort[] ramp);

        [DllImport("gdi32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SetDeviceGammaRamp(nint deviceContext, [In] ushort[] ramp);
    }
}
