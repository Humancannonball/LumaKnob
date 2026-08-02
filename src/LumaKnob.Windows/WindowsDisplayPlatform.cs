using System.Runtime.Versioning;
using LumaKnob.Engine.Displays;
using LumaKnob.Engine.Platform;
using LumaKnob.Windows.Color;
using LumaKnob.Windows.Displays;
using LumaKnob.Windows.Shade;
using LumaKnob.Windows.Shell;

namespace LumaKnob.Windows;

[SupportedOSPlatform("windows")]
public sealed class WindowsDisplayPlatform : IDisplayPlatform
{
    public string Name => "Windows";

    public bool SupportsShadeWindows => true;

    public Task<DisplayDiscoveryReport> DiscoverAsync(CancellationToken cancellationToken = default) =>
        WindowsBrightnessDiscovery.DiscoverAsync(cancellationToken);

    public ISystemInputService CreateSystemInputService() => new WindowsSystemInputService();

    public IDisplayColorController CreateColorController() => new WindowsGammaRampColorController();

    public void ConfigureShadeWindow(nint nativeWindowHandle) =>
        ShadeWindowStyle.ApplyClickThroughTopmost(nativeWindowHandle);
}
