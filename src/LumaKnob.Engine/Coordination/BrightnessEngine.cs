using LumaKnob.Core.Brightness;
using LumaKnob.Engine.Displays;
using LumaKnob.Engine.Platform;

namespace LumaKnob.Engine.Coordination;

public sealed class BrightnessEngine(IDisplayPlatform platform) : IDisposable
{
    private readonly IDisplayColorController _colorController = platform.CreateColorController();
    private readonly List<BrightnessDisplay> _displays = [];
    private bool _disposed;

    public event EventHandler<ShadeRequestEventArgs>? ShadeRequested;

    public event EventHandler<OperationReportedEventArgs>? OperationReported;

    public event EventHandler<DisplayStateChangedEventArgs>? DisplayStateChanged;

    public IReadOnlyList<BrightnessDisplay> Displays => _displays;

    public IDisplayPlatform Platform { get; } = platform;

    public async Task<IReadOnlyList<string>> DiscoverAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (_displays.Count != 0)
        {
            throw new InvalidOperationException("Display discovery has already completed for this engine instance.");
        }

        var report = await Platform.DiscoverAsync(cancellationToken).ConfigureAwait(false);
        foreach (var target in report.Targets)
        {
            _displays.Add(new BrightnessDisplay(target));
        }

        return report.Diagnostics;
    }

    public ValueTask<BrightnessValue> ReadBrightnessAsync(
        BrightnessDisplay display,
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return display.Target.ReadBrightnessAsync(cancellationToken);
    }

    public BrightnessPlan CreatePlan(
        BrightnessDisplay display,
        BrightnessValue logicalBrightness,
        BrightnessValue pivot)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        var hardware = display.IsHardwareFaulted ? null : display.HardwareCapability;
        return HybridBrightnessPolicy.CreatePlan(logicalBrightness, pivot, hardware);
    }

    public bool CanApplyColor(BrightnessDisplay display)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return _colorController.CanApply(display.Info);
    }

    public string DescribeColorAvailability(BrightnessDisplay display)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return _colorController.DescribeAvailability(display.Info);
    }

    public bool TryApplyColor(BrightnessDisplay display, DisplayColorAdjustment adjustment)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (!_colorController.CanApply(display.Info))
        {
            Report(_colorController.DescribeAvailability(display.Info));
            return false;
        }

        try
        {
            _colorController.Apply(display.Info, adjustment);
            Report(
                $"Applied color ramp: gamma {adjustment.Gamma * 100m:0.##}%, " +
                $"warmth {adjustment.Warmth:0.##}% through {_colorController.BackendLabel}.");
            return true;
        }
        catch (Exception exception)
        {
            Report($"Color ramp failed safely and was restored: {exception.Message}");
            try
            {
                _colorController.RestoreAll();
            }
            catch (Exception restoreException)
            {
                Report($"Color-ramp restore warning: {restoreException.Message}");
            }

            return false;
        }
    }

    public void RestoreColorAdjustments()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        try
        {
            _colorController.RestoreAll();
            Report("Gamma and warmth restored to the captured system ramps.");
        }
        catch (Exception exception)
        {
            Report($"Color-ramp restore warning: {exception.Message}");
        }
    }

    public void Apply(
        BrightnessDisplay display,
        BrightnessPlan plan,
        BrightnessValue pivot,
        TimeSpan responseDelay)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        var request = display.ReplacePendingApply();
        RequestShade(display, plan.ShadeOpacity);
        if (request.Token.IsCancellationRequested)
        {
            return;
        }

        if (display.IsSimulator)
        {
            Report($"Simulated plan: {DescribePlanDetail(plan, pivot)}.");
            return;
        }

        if (plan.HardwareValue is int nativeValue)
        {
            ScheduleHardwareApply(display, plan, pivot, nativeValue, responseDelay, request);
            return;
        }

        Report(DescribeAppliedPlan(display, plan));
    }

    public async Task EmergencyRestoreAllAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        foreach (var display in _displays)
        {
            display.CancelPendingApply();
            RequestShade(display, 0m);
        }

        try
        {
            _colorController.RestoreAll();
        }
        catch (Exception exception)
        {
            Report($"Color-ramp restore warning: {exception.Message}");
        }

        var restoreTasks = _displays
            .Where(display => !display.IsSimulator && display.HardwareCapability is not null)
            .Select(display => RestoreHardwareTargetAsync(display, cancellationToken))
            .ToArray();

        await Task.WhenAll(restoreTasks).ConfigureAwait(false);
        foreach (var display in _displays)
        {
            display.IsHardwareFaulted = false;
            DisplayStateChanged?.Invoke(this, new DisplayStateChangedEventArgs(display.Id));
        }

        Report("Emergency restore complete: shades and color ramps clear, hardware requested at 50%.");
    }

    public void CancelPendingApplies()
    {
        foreach (var display in _displays)
        {
            display.CancelPendingApply();
        }
    }

    public void CancelPendingApply(BrightnessDisplay display)
    {
        ArgumentNullException.ThrowIfNull(display);
        if (!_displays.Contains(display))
        {
            throw new ArgumentException("The display does not belong to this engine.", nameof(display));
        }

        display.CancelPendingApply();
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        CancelPendingApplies();
        try
        {
            _colorController.RestoreAll();
        }
        catch
        {
            // Normal shutdown must still dispose display handles even when a
            // third-party driver refuses the final color-ramp restore.
        }
        finally
        {
            try
            {
                _colorController.Dispose();
            }
            catch
            {
            }
        }

        foreach (var display in _displays)
        {
            display.Target.Dispose();
        }

        _displays.Clear();
    }

    public static string DescribePlanDetail(BrightnessPlan plan, BrightnessValue pivot) =>
        plan.Mechanisms.HasFlag(BrightnessMechanism.Shade)
            ? plan.LogicalBrightness < pivot
                ? $"BELOW HARDWARE FLOOR · LIGHT {decimal.Round(plan.ShadeTransmission * 100m)}%"
                : $"DISCRETE HARDWARE STEP · LIGHT {decimal.Round(plan.ShadeTransmission * 100m)}%"
            : "HARDWARE RANGE · SHADE CLEAR";

    private void ScheduleHardwareApply(
        BrightnessDisplay display,
        BrightnessPlan plan,
        BrightnessValue pivot,
        int nativeValue,
        TimeSpan responseDelay,
        (long Version, CancellationToken Token) request)
    {
        var safeDelay = TimeSpan.FromMilliseconds(Math.Clamp(responseDelay.TotalMilliseconds, 0, 2_000));
        _ = ApplyHardwareAfterDelayAsync(
            display,
            plan,
            pivot,
            nativeValue,
            safeDelay,
            request.Version,
            request.Token);
    }

    private async Task ApplyHardwareAfterDelayAsync(
        BrightnessDisplay display,
        BrightnessPlan plan,
        BrightnessValue pivot,
        int nativeValue,
        TimeSpan responseDelay,
        long version,
        CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(responseDelay, cancellationToken).ConfigureAwait(false);
            await display.HardwareWriteGate.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                Report($"Writing {HardwareBackendName(display)} native value {nativeValue}…");
                await display.Target.SetNativeBrightnessAsync(nativeValue, cancellationToken).ConfigureAwait(false);
                if (display.IsCurrent(version))
                {
                    Report(DescribeAppliedPlan(display, plan));
                }
            }
            finally
            {
                display.HardwareWriteGate.Release();
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception exception)
        {
            if (!display.IsCurrent(version))
            {
                return;
            }

            display.IsHardwareFaulted = true;
            DisplayStateChanged?.Invoke(this, new DisplayStateChangedEventArgs(display.Id));
            Report($"Hardware failed; switched this target to shade-only: {exception.Message}");
            var fallback = HybridBrightnessPolicy.CreatePlan(plan.LogicalBrightness, pivot, hardware: null);
            RequestShade(display, fallback.ShadeOpacity);
        }
    }

    private async Task RestoreHardwareTargetAsync(
        BrightnessDisplay display,
        CancellationToken cancellationToken)
    {
        try
        {
            await display.HardwareWriteGate.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                var nativeValue = display.HardwareCapability!.MapNormalized(0.5m);
                await display.Target.SetNativeBrightnessAsync(nativeValue, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                display.HardwareWriteGate.Release();
            }
        }
        catch (Exception exception)
        {
            Report($"Restore warning for {display.Name}: {exception.Message}");
        }
    }

    private void RequestShade(BrightnessDisplay display, decimal opacity) =>
        ShadeRequested?.Invoke(
            this,
            new ShadeRequestEventArgs(display.Id, display.Info.Bounds, opacity));

    private void Report(string message) =>
        OperationReported?.Invoke(this, new OperationReportedEventArgs(message));

    private static string DescribeAppliedPlan(BrightnessDisplay display, BrightnessPlan plan)
    {
        if (plan.Mechanisms == BrightnessMechanism.None)
        {
            return $"Applied {plan.LogicalBrightness}; software shade is clear.";
        }

        var hardware = plan.Mechanisms.HasFlag(BrightnessMechanism.Hardware);
        var shade = plan.Mechanisms.HasFlag(BrightnessMechanism.Shade);
        var method = (hardware, shade) switch
        {
            (true, true) => $"{HardwareBackendName(display)} + software shade",
            (true, false) => HardwareBackendName(display),
            (false, true) => "software shade",
            _ => "configured mechanisms",
        };

        return $"Applied {plan.LogicalBrightness} through {method}.";
    }

    private static string HardwareBackendName(BrightnessDisplay display) =>
        display.HardwareBackendLabel ?? "hardware";
}
