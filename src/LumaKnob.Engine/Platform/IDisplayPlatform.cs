using LumaKnob.Engine.Displays;

namespace LumaKnob.Engine.Platform;

public interface IDisplayPlatform
{
    string Name { get; }

    bool SupportsShadeWindows { get; }

    Task<DisplayDiscoveryReport> DiscoverAsync(CancellationToken cancellationToken = default);

    ISystemInputService CreateSystemInputService();

    IDisplayColorController CreateColorController();

    void ConfigureShadeWindow(nint nativeWindowHandle);
}
