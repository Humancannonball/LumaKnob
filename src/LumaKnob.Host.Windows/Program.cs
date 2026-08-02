using Avalonia;
using Avalonia.Skia;
using Avalonia.Win32;
using LumaKnob.App;
using LumaKnob.Windows;

namespace LumaKnob.Host.Windows;

internal static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        if (!OperatingSystem.IsWindows())
        {
            throw new PlatformNotSupportedException("The LumaKnob Windows host requires Windows.");
        }

        using var singleInstance = new WindowsSingleInstance();
        if (!singleInstance.IsPrimary)
        {
            return;
        }

        var appBuilder = AppBuilder.Configure<LumaKnob.App.App>()
            .UseWin32()
            .UseSkia()
            .UseHarfBuzz()
            .LogToTrace();
        DesktopBootstrap.Run(args, new WindowsDisplayPlatform(), appBuilder);
    }
}
