using Avalonia;
using LumaKnob.App;
using LumaKnob.Core.Brightness;
using LumaKnob.Engine.Displays;
using LumaKnob.Engine.Platform;

namespace LumaKnob.Host.Simulator;

internal static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        var appBuilder = AppBuilder.Configure<LumaKnob.App.App>()
            .UsePlatformDetect()
            .LogToTrace();
        DesktopBootstrap.Run(args, new SimulatorDisplayPlatform(), appBuilder);
    }
}

internal sealed class SimulatorDisplayPlatform : IDisplayPlatform
{
    public string Name => "Simulator";

    public bool SupportsShadeWindows => false;

    public Task<DisplayDiscoveryReport> DiscoverAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        IBrightnessTarget[] targets = [new SimulatorBrightnessTarget()];
        return Task.FromResult(
            new DisplayDiscoveryReport(targets, ["Non-Windows host: simulator loaded."]));
    }

    public ISystemInputService CreateSystemInputService() => new UnavailableSystemInput();

    public IDisplayColorController CreateColorController() => new SimulatorColorController();

    public void ConfigureShadeWindow(nint nativeWindowHandle)
    {
    }

    private sealed class SimulatorBrightnessTarget : IBrightnessTarget
    {
        private BrightnessValue _value = BrightnessValue.FromPercent(50m);

        public BrightnessTargetInfo Info { get; } = new(
            "simulator",
            "Simulated display",
            "SIMULATOR",
            "simulated hardware",
            new DisplayPixelBounds(0, 0, 1920, 1080),
            new HardwareBrightnessCapability(0, 100),
            "This host exercises the portable Engine without physical display writes.",
            IsSimulator: true);

        public ValueTask<BrightnessValue> ReadBrightnessAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return ValueTask.FromResult(_value);
        }

        public ValueTask SetNativeBrightnessAsync(int nativeValue, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _value = BrightnessValue.FromPercent(nativeValue);
            return ValueTask.CompletedTask;
        }

        public void Dispose()
        {
        }
    }

    private sealed class UnavailableSystemInput : ISystemInputService
    {
        public event EventHandler? EmergencyRestorePressed
        {
            add { }
            remove { }
        }

        public event EventHandler? BrightnessIncreasePressed
        {
            add { }
            remove { }
        }

        public event EventHandler? BrightnessDecreasePressed
        {
            add { }
            remove { }
        }

        public SystemInputRegistration Start() => new(false, 0, false, 0);

        public void Dispose()
        {
        }
    }

    private sealed class SimulatorColorController : IDisplayColorController
    {
        public string BackendLabel => "SIMULATED COLOR RAMP";

        public bool CanApply(BrightnessTargetInfo target) => true;

        public string DescribeAvailability(BrightnessTargetInfo target) =>
            "COLOR READY · simulator only; no physical color ramp will change.";

        public void Apply(BrightnessTargetInfo target, DisplayColorAdjustment adjustment)
        {
        }

        public void RestoreAll()
        {
        }

        public void Dispose()
        {
        }
    }
}
