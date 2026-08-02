using LumaKnob.App.ViewModels;
using LumaKnob.Core.Brightness;
using LumaKnob.Engine.Coordination;
using LumaKnob.Engine.Displays;
using LumaKnob.Engine.Platform;

namespace LumaKnob.App.Tests;

public sealed class MainWindowViewModelTests
{
    [Fact]
    public async Task BrightnessKeyAdjustmentUsesTheCurrentStep()
    {
        using var viewModel = await CreateViewModelAsync();
        viewModel.StepIndex = 8; // 5.00%

        viewModel.AdjustBrightnessByStep(1);
        Assert.Equal(55, viewModel.BrightnessPercent);

        viewModel.AdjustBrightnessByStep(-1);
        Assert.Equal(50, viewModel.BrightnessPercent);

        viewModel.StepIndex = 3; // 0.10%
        viewModel.AdjustBrightnessByStep(1);
        Assert.Equal(50.1, viewModel.BrightnessPercent);
    }

    [Fact]
    public async Task BrightnessKeyAdjustmentRespectsTheSafetyFloorWhenRestoreHotkeyFails()
    {
        using var viewModel = await CreateViewModelAsync();
        viewModel.ConfigureSystemInput(new SystemInputRegistration(false, 1409, true, 0));
        viewModel.StepIndex = 11; // 25.00%
        viewModel.BrightnessPercent = 1;

        viewModel.AdjustBrightnessByStep(-1);

        Assert.Equal(1, viewModel.BrightnessPercent);
        Assert.Equal(1, viewModel.MinimumBrightnessPercent);
    }

    [Fact]
    public async Task EveryDisplayKeepsAnIndependentLogicalBrightness()
    {
        using var viewModel = await CreateViewModelAsync(displayCount: 2);
        var first = viewModel.Displays[0];
        var second = viewModel.Displays[1];

        first.BrightnessPercent = 31.25;
        second.BrightnessPercent = 72.5;

        viewModel.SelectedDisplay = second;
        Assert.Equal(72.5, viewModel.BrightnessPercent);
        Assert.Equal(31.25, first.BrightnessPercent);

        viewModel.SelectedDisplay = first;
        Assert.Equal(31.25, viewModel.BrightnessPercent);
        Assert.Equal(72.5, second.BrightnessPercent);
    }

    [Fact]
    public async Task BrightnessKeysAdjustOnlyTheSelectedDisplay()
    {
        using var viewModel = await CreateViewModelAsync(displayCount: 2);
        var first = viewModel.Displays[0];
        var second = viewModel.Displays[1];
        viewModel.StepIndex = 8; // 5.00%
        viewModel.SelectedDisplay = second;

        viewModel.AdjustBrightnessByStep(1);

        Assert.Equal(50, first.BrightnessPercent);
        Assert.Equal(55, second.BrightnessPercent);
    }

    private static async Task<MainWindowViewModel> CreateViewModelAsync(int displayCount = 1)
    {
        var engine = new BrightnessEngine(new FakePlatform(displayCount));
        var viewModel = new MainWindowViewModel(engine);
        await viewModel.InitializeAsync();
        return viewModel;
    }

    private sealed class FakePlatform(int displayCount) : IDisplayPlatform
    {
        public string Name => "Keyboard test";

        public bool SupportsShadeWindows => false;

        public Task<DisplayDiscoveryReport> DiscoverAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var targets = Enumerable.Range(1, displayCount)
                .Select(index => (IBrightnessTarget)new FakeTarget(index))
                .ToArray();
            return Task.FromResult<DisplayDiscoveryReport>(new(targets, []));
        }

        public ISystemInputService CreateSystemInputService() => new FakeSystemInput();

        public IDisplayColorController CreateColorController() => new FakeColorController();

        public void ConfigureShadeWindow(nint nativeWindowHandle)
        {
        }
    }

    private sealed class FakeTarget(int index) : IBrightnessTarget
    {
        public BrightnessTargetInfo Info { get; } = new(
            $"keyboard-test-{index}",
            $"Keyboard test display {index}",
            "SIMULATOR",
            "simulated hardware",
            new DisplayPixelBounds(0, 0, 1920, 1080),
            new HardwareBrightnessCapability(0, 100),
            IsSimulator: true);

        public ValueTask<BrightnessValue> ReadBrightnessAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return ValueTask.FromResult(BrightnessValue.FromPercent(50m));
        }

        public ValueTask SetNativeBrightnessAsync(int nativeValue, CancellationToken cancellationToken) =>
            ValueTask.CompletedTask;

        public void Dispose()
        {
        }
    }

    private sealed class FakeSystemInput : ISystemInputService
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

        public SystemInputRegistration Start() => new(true, 0, true, 0);

        public void Dispose()
        {
        }
    }

    private sealed class FakeColorController : IDisplayColorController
    {
        public string BackendLabel => "NONE";

        public bool CanApply(BrightnessTargetInfo target) => false;

        public string DescribeAvailability(BrightnessTargetInfo target) => "Color unavailable.";

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
