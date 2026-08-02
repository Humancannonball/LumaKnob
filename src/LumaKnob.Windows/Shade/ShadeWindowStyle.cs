using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace LumaKnob.Windows.Shade;

public static class ShadeWindowStyle
{
    private const int ExtendedStyleIndex = -20;
    private const long TransparentStyle = 0x00000020L;
    private const long ToolWindowStyle = 0x00000080L;
    private const long LayeredStyle = 0x00080000L;
    private const long NoActivateStyle = 0x08000000L;
    private const uint NoSize = 0x0001;
    private const uint NoMove = 0x0002;
    private const uint NoActivate = 0x0010;
    private const uint ShowWindow = 0x0040;
    private static readonly nint Topmost = new(-1);

    [SupportedOSPlatform("windows")]
    public static void ApplyClickThroughTopmost(nint window)
    {
        var currentStyle = GetWindowLongPtr(window, ExtendedStyleIndex).ToInt64();
        var newStyle = new nint(currentStyle | TransparentStyle | ToolWindowStyle | LayeredStyle | NoActivateStyle);

        Marshal.SetLastPInvokeError(0);
        var previousStyle = SetWindowLongPtr(window, ExtendedStyleIndex, newStyle);
        if (previousStyle == 0 && Marshal.GetLastPInvokeError() != 0)
        {
            throw new Win32Exception(Marshal.GetLastPInvokeError(), "Could not make the shade window click-through.");
        }

        if (!SetWindowPos(window, Topmost, 0, 0, 0, 0, NoSize | NoMove | NoActivate | ShowWindow))
        {
            throw new Win32Exception(Marshal.GetLastWin32Error(), "Could not position the shade window.");
        }
    }

    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW", SetLastError = true)]
    private static extern nint GetWindowLongPtr(nint window, int index);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW", SetLastError = true)]
    private static extern nint SetWindowLongPtr(nint window, int index, nint newLong);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetWindowPos(
        nint window,
        nint insertAfter,
        int x,
        int y,
        int width,
        int height,
        uint flags);
}
