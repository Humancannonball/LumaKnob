using LumaKnob.Core.Brightness;
using LumaKnob.Engine.Coordination;
using LumaKnob.Engine.Displays;
using LumaKnob.Engine.Platform;

namespace LumaKnob.Engine.Tests;

public sealed class BrightnessEngineTests
{
    private static readonly BrightnessValue Pivot = BrightnessValue.FromPercent(10m);

    [Fact]
    public async Task DiscoveryCreatesPlatformNeutralDisplaySessions()
    {
        var target = new FakeBrightnessTarget();
        var platform = new FakePlatform(target, "native discovery was healthy");
        using var engine = new BrightnessEngine(platform);

        var diagnostics = await engine.DiscoverAsync(TestContext.Current.CancellationToken);

        var display = Assert.Single(engine.Displays);
        Assert.Equal("test-display", display.Id);
        Assert.Equal("TEST PANEL", display.Name);
        Assert.Equal("DDC/CI + shade", display.BackendLabel);
        Assert.Equal("DDC/CI", display.HardwareBackendLabel);
        Assert.Equal("native discovery was healthy", Assert.Single(diagnostics));
    }

    [Fact]
    public async Task DiscoveryCannotAccidentallyDuplicateOwnedTargets()
    {
        var target = new FakeBrightnessTarget();
        using var engine = new BrightnessEngine(new FakePlatform(target));
        await engine.DiscoverAsync(TestContext.Current.CancellationToken);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => engine.DiscoverAsync(TestContext.Current.CancellationToken));

        Assert.Single(engine.Displays);
    }

    [Fact]
    public async Task NewApplyCancelsPendingHardwareWriteAndKeepsTheLastValue()
    {
        var target = new FakeBrightnessTarget();
        using var engine = await CreateEngineAsync(target);
        var display = Assert.Single(engine.Displays);

        var first = engine.CreatePlan(display, BrightnessValue.FromPercent(40m), Pivot);
        var second = engine.CreatePlan(display, BrightnessValue.FromPercent(65m), Pivot);
        var applied = WaitForReportAsync(engine, "Applied 65.00% through DDC/CI.");

        engine.Apply(display, first, Pivot, TimeSpan.FromMilliseconds(250));
        engine.Apply(display, second, Pivot, TimeSpan.FromMilliseconds(10));
        await applied.WaitAsync(TimeSpan.FromSeconds(2), TestContext.Current.CancellationToken);

        Assert.Equal([65], target.NativeWrites);
    }

    [Fact]
    public async Task HardwareFailureFallsBackToShadeOnlyForThatDisplay()
    {
        var target = new FakeBrightnessTarget { WriteException = new IOException("test transport failure") };
        using var engine = await CreateEngineAsync(target);
        var display = Assert.Single(engine.Displays);
        var fallbackShade = new TaskCompletionSource<decimal>(TaskCreationOptions.RunContinuationsAsynchronously);
        var stateChanged = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        engine.ShadeRequested += (_, args) =>
        {
            if (args.Opacity > 0m)
            {
                fallbackShade.TrySetResult(args.Opacity);
            }
        };
        engine.DisplayStateChanged += (_, args) =>
        {
            if (args.TargetId == display.Id)
            {
                stateChanged.TrySetResult();
            }
        };

        var plan = engine.CreatePlan(display, BrightnessValue.FromPercent(40m), Pivot);
        engine.Apply(display, plan, Pivot, TimeSpan.Zero);

        var opacity = await fallbackShade.Task.WaitAsync(
            TimeSpan.FromSeconds(2),
            TestContext.Current.CancellationToken);
        await stateChanged.Task.WaitAsync(TimeSpan.FromSeconds(2), TestContext.Current.CancellationToken);
        var nextPlan = engine.CreatePlan(display, BrightnessValue.FromPercent(40m), Pivot);

        Assert.Equal(0.6m, opacity);
        Assert.True(display.IsHardwareFaulted);
        Assert.Equal(BrightnessMechanism.Shade, nextPlan.Mechanisms);
        Assert.Null(nextPlan.HardwareValue);
    }

    [Fact]
    public void PlanDescriptionDistinguishesDiscreteStepsFromTheLowFloor()
    {
        var discreteHardware = new HardwareBrightnessCapability(0, 100, [0, 20, 40, 60, 80, 100]);
        var discrete = HybridBrightnessPolicy.CreatePlan(BrightnessValue.FromPercent(51m), Pivot, discreteHardware);
        var belowFloor = HybridBrightnessPolicy.CreatePlan(BrightnessValue.FromPercent(5m), Pivot, discreteHardware);

        Assert.StartsWith("DISCRETE HARDWARE STEP", BrightnessEngine.DescribePlanDetail(discrete, Pivot));
        Assert.StartsWith("BELOW HARDWARE FLOOR", BrightnessEngine.DescribePlanDetail(belowFloor, Pivot));
    }

    [Fact]
    public async Task NonCancellableHardwareWritesRemainSerialized()
    {
        var target = new FakeBrightnessTarget { WriteDelay = TimeSpan.FromMilliseconds(80) };
        using var engine = await CreateEngineAsync(target);
        var display = Assert.Single(engine.Displays);
        var first = engine.CreatePlan(display, BrightnessValue.FromPercent(40m), Pivot);
        var second = engine.CreatePlan(display, BrightnessValue.FromPercent(65m), Pivot);
        var applied = WaitForReportAsync(engine, "Applied 65.00% through DDC/CI.");

        engine.Apply(display, first, Pivot, TimeSpan.Zero);
        await target.FirstWriteStarted.WaitAsync(
            TimeSpan.FromSeconds(2),
            TestContext.Current.CancellationToken);
        engine.Apply(display, second, Pivot, TimeSpan.Zero);
        await applied.WaitAsync(TimeSpan.FromSeconds(2), TestContext.Current.CancellationToken);

        Assert.Equal(1, target.MaximumConcurrentWrites);
        Assert.Equal([40, 65], target.NativeWrites);
    }

    [Fact]
    public async Task SafetyCancellationDuringShadeRequestPreventsTheHardwareWrite()
    {
        var target = new FakeBrightnessTarget();
        using var engine = await CreateEngineAsync(target);
        var display = Assert.Single(engine.Displays);
        engine.ShadeRequested += (_, _) => engine.CancelPendingApplies();

        var plan = engine.CreatePlan(display, BrightnessValue.FromPercent(4m), Pivot);
        engine.Apply(display, plan, Pivot, TimeSpan.Zero);
        await Task.Delay(100, TestContext.Current.CancellationToken);

        Assert.Empty(target.NativeWrites);
    }

    [Fact]
    public async Task CancellingOneDisplayLeavesTheOtherDisplayPending()
    {
        var firstTarget = new FakeBrightnessTarget("first-display", "FIRST PANEL");
        var secondTarget = new FakeBrightnessTarget("second-display", "SECOND PANEL");
        using var engine = new BrightnessEngine(new FakePlatform(firstTarget, secondTarget));
        await engine.DiscoverAsync(TestContext.Current.CancellationToken);
        var firstDisplay = Assert.Single(engine.Displays, display => display.Id == "first-display");
        var secondDisplay = Assert.Single(engine.Displays, display => display.Id == "second-display");
        var secondApplied = WaitForReportAsync(engine, "Applied 60.00% through DDC/CI.");

        engine.Apply(
            firstDisplay,
            engine.CreatePlan(firstDisplay, BrightnessValue.FromPercent(40m), Pivot),
            Pivot,
            TimeSpan.FromMilliseconds(150));
        engine.Apply(
            secondDisplay,
            engine.CreatePlan(secondDisplay, BrightnessValue.FromPercent(60m), Pivot),
            Pivot,
            TimeSpan.FromMilliseconds(20));
        engine.CancelPendingApply(firstDisplay);

        await secondApplied.WaitAsync(TimeSpan.FromSeconds(2), TestContext.Current.CancellationToken);
        Assert.Empty(firstTarget.NativeWrites);
        Assert.Equal([60], secondTarget.NativeWrites);
    }

    [Fact]
    public async Task EmergencyRestoreClearsShadeAndRequestsMidpointHardware()
    {
        var target = new FakeBrightnessTarget();
        using var engine = await CreateEngineAsync(target);
        var shadeRequests = new List<decimal>();
        engine.ShadeRequested += (_, args) => shadeRequests.Add(args.Opacity);

        await engine.EmergencyRestoreAllAsync(TestContext.Current.CancellationToken);

        Assert.Equal([50], target.NativeWrites);
        Assert.Equal([0m], shadeRequests);
    }

    [Fact]
    public async Task ColorAdjustmentUsesTheInjectedControllerAndEmergencyRestoresIt()
    {
        var target = new FakeBrightnessTarget();
        var colors = new FakeColorController();
        using var engine = new BrightnessEngine(new FakePlatform(target, colors));
        await engine.DiscoverAsync(TestContext.Current.CancellationToken);
        var display = Assert.Single(engine.Displays);
        var adjustment = new DisplayColorAdjustment(1.2m, 35m);

        var applied = engine.TryApplyColor(display, adjustment);
        await engine.EmergencyRestoreAllAsync(TestContext.Current.CancellationToken);

        Assert.True(applied);
        Assert.Equal(adjustment, colors.LastAdjustment);
        Assert.Equal("test-display", colors.LastTargetId);
        Assert.Equal(1, colors.RestoreCount);
    }

    private static async Task<BrightnessEngine> CreateEngineAsync(FakeBrightnessTarget target)
    {
        var engine = new BrightnessEngine(new FakePlatform(target));
        await engine.DiscoverAsync(TestContext.Current.CancellationToken);
        return engine;
    }

    private static Task WaitForReportAsync(BrightnessEngine engine, string expected)
    {
        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        engine.OperationReported += (_, args) =>
        {
            if (args.Message == expected)
            {
                completion.TrySetResult();
            }
        };
        return completion.Task;
    }

    private sealed class FakePlatform(params object[] items) : IDisplayPlatform
    {
        private readonly IReadOnlyList<IBrightnessTarget> _targets = items.OfType<IBrightnessTarget>().ToArray();
        private readonly IReadOnlyList<string> _diagnostics = items.OfType<string>().ToArray();
        private readonly IDisplayColorController _colors =
            items.OfType<IDisplayColorController>().FirstOrDefault() ?? new FakeColorController();

        public string Name => "Test platform";

        public bool SupportsShadeWindows => true;

        public Task<DisplayDiscoveryReport> DiscoverAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(new DisplayDiscoveryReport(_targets, _diagnostics));

        public ISystemInputService CreateSystemInputService() => new FakeSystemInput();

        public IDisplayColorController CreateColorController() => _colors;

        public void ConfigureShadeWindow(nint nativeWindowHandle)
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
        public DisplayColorAdjustment? LastAdjustment { get; private set; }

        public string? LastTargetId { get; private set; }

        public int RestoreCount { get; private set; }

        public string BackendLabel => "FAKE COLOR";

        public bool CanApply(BrightnessTargetInfo target) => true;

        public string DescribeAvailability(BrightnessTargetInfo target) => "Fake color is ready.";

        public void Apply(BrightnessTargetInfo target, DisplayColorAdjustment adjustment)
        {
            LastTargetId = target.Id;
            LastAdjustment = adjustment;
        }

        public void RestoreAll()
        {
            RestoreCount++;
        }

        public void Dispose()
        {
        }
    }

    private sealed class FakeBrightnessTarget : IBrightnessTarget
    {
        private readonly object _gate = new();
        private readonly List<int> _nativeWrites = [];
        private readonly TaskCompletionSource _firstWriteStarted =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int _activeWrites;
        private int _maximumConcurrentWrites;

        public FakeBrightnessTarget(string id = "test-display", string name = "TEST PANEL") =>
            Info = new BrightnessTargetInfo(
                id,
                name,
                "DDC/CI + shade",
                "DDC/CI",
                new DisplayPixelBounds(0, 0, 2560, 1440),
                new HardwareBrightnessCapability(0, 100));

        public BrightnessTargetInfo Info { get; }

        public Exception? WriteException { get; init; }

        public TimeSpan WriteDelay { get; init; }

        public Task FirstWriteStarted => _firstWriteStarted.Task;

        public int MaximumConcurrentWrites => Volatile.Read(ref _maximumConcurrentWrites);

        public IReadOnlyList<int> NativeWrites
        {
            get
            {
                lock (_gate)
                {
                    return _nativeWrites.ToArray();
                }
            }
        }

        public ValueTask<BrightnessValue> ReadBrightnessAsync(CancellationToken cancellationToken) =>
            ValueTask.FromResult(BrightnessValue.FromPercent(50m));

        public async ValueTask SetNativeBrightnessAsync(int nativeValue, CancellationToken cancellationToken)
        {
            var active = Interlocked.Increment(ref _activeWrites);
            InterlockedExtensions.Max(ref _maximumConcurrentWrites, active);
            _firstWriteStarted.TrySetResult();

            try
            {
                if (WriteDelay > TimeSpan.Zero)
                {
                    // Deliberately ignore cancellation to model a native call
                    // which cannot be interrupted after it has begun.
                    await Task.Delay(WriteDelay, CancellationToken.None);
                }

                if (WriteException is not null)
                {
                    throw WriteException;
                }

                lock (_gate)
                {
                    _nativeWrites.Add(nativeValue);
                }
            }
            finally
            {
                Interlocked.Decrement(ref _activeWrites);
            }
        }

        public void Dispose()
        {
        }
    }

    private static class InterlockedExtensions
    {
        public static void Max(ref int location, int value)
        {
            var current = Volatile.Read(ref location);
            while (current < value)
            {
                var observed = Interlocked.CompareExchange(ref location, value, current);
                if (observed == current)
                {
                    return;
                }

                current = observed;
            }
        }
    }
}
