using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.InteropServices;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LumaKnob.App.Services;
using LumaKnob.Core.Brightness;
using LumaKnob.Engine.Coordination;
using LumaKnob.Engine.Displays;
using LumaKnob.Engine.Platform;

namespace LumaKnob.App.ViewModels;

public sealed class MainWindowViewModel : ObservableObject, IDisposable
{
    private readonly BrightnessEngine _engine;
    private CancellationTokenSource? _colorApplyCancellation;
    private DisplayControlViewModel? _selectedDisplay;
    private int _stepIndex = 6;
    private bool _disposed;
    private bool _isLoading = true;
    private double _minimumBrightnessPercent = 1d;
    private double _gammaPercent = 100d;
    private double _warmth;
    private string _discoveryStatus = "Waiting for display discovery…";
    private string _operationStatus = "Launch is read-only; move BRIGHTNESS to apply a value.";
    private string _safetyStatus = "SAFETY NOT READY";

    public MainWindowViewModel(BrightnessEngine engine)
    {
        _engine = engine;
        _engine.ShadeRequested += OnEngineShadeRequested;
        _engine.OperationReported += OnEngineOperationReported;
        _engine.DisplayStateChanged += OnEngineDisplayStateChanged;

        IncreaseStepCommand = new RelayCommand(() => StepIndex++);
        DecreaseStepCommand = new RelayCommand(() => StepIndex--);
        RestoreCommand = new AsyncRelayCommand(() => EmergencyRestoreAllAsync());
    }

    public event EventHandler<ShadeRequestEventArgs>? ShadeRequested;

    public ObservableCollection<DisplayControlViewModel> Displays { get; } = [];

    public DisplayControlViewModel? SelectedDisplay
    {
        get => _selectedDisplay;
        set
        {
            if (ReferenceEquals(_selectedDisplay, value))
            {
                return;
            }

            if (_selectedDisplay is not null)
            {
                _selectedDisplay.PropertyChanged -= OnSelectedDisplayPropertyChanged;
                _selectedDisplay.IsSelected = false;
                CancelColorApply();
                _engine.RestoreColorAdjustments();
                ResetColorValues();
            }

            if (!SetProperty(ref _selectedDisplay, value))
            {
                return;
            }

            if (_selectedDisplay is not null)
            {
                _selectedDisplay.IsSelected = true;
                _selectedDisplay.PropertyChanged += OnSelectedDisplayPropertyChanged;
            }

            NotifySelectedDisplayProperties();
        }
    }

    public bool CanControl => !IsLoading && SelectedDisplay?.CanControl == true;

    public bool CanAdjustColor =>
        CanControl &&
        SelectedDisplay is not null &&
        _engine.CanApplyColor(SelectedDisplay.Session);

    public bool IsLoading
    {
        get => _isLoading;
        private set
        {
            if (SetProperty(ref _isLoading, value))
            {
                OnPropertyChanged(nameof(CanControl));
                OnPropertyChanged(nameof(CanAdjustColor));
            }
        }
    }

    public string DiscoveryStatus
    {
        get => _discoveryStatus;
        private set
        {
            if (SetProperty(ref _discoveryStatus, value))
            {
                DiagnosticLog.Write($"DISCOVERY {value}");
            }
        }
    }

    public string OperationStatus
    {
        get => _operationStatus;
        private set
        {
            if (SetProperty(ref _operationStatus, value))
            {
                DiagnosticLog.Write($"OPERATION {value}");
            }
        }
    }

    public string SelectedBackend => SelectedDisplay?.BackendLabel ?? "DISCOVERING";

    public string TargetNotice => SelectedDisplay?.Notice ?? "No target selected.";

    public string SafetyStatus
    {
        get => _safetyStatus;
        private set => SetProperty(ref _safetyStatus, value);
    }

    public double MinimumBrightnessPercent
    {
        get => _minimumBrightnessPercent;
        private set
        {
            if (SetProperty(ref _minimumBrightnessPercent, value))
            {
                foreach (var display in Displays)
                {
                    display.NotifySharedSettingsChanged();
                }
            }
        }
    }

    public double BrightnessPercent
    {
        get => SelectedDisplay?.BrightnessPercent ?? 50d;
        set
        {
            if (SelectedDisplay is null)
            {
                return;
            }

            SelectedDisplay.BrightnessPercent = value;
        }
    }

    public double StepIndex
    {
        get => _stepIndex;
        set
        {
            var next = Math.Clamp((int)Math.Round(value, MidpointRounding.AwayFromZero), 0, BrightnessStep.Presets.Count - 1);
            if (!SetProperty(ref _stepIndex, next, nameof(StepIndex)))
            {
                return;
            }

            OnPropertyChanged(nameof(StepPercent));
            OnPropertyChanged(nameof(StepReadout));
            foreach (var display in Displays)
            {
                display.NotifySharedSettingsChanged();
            }
        }
    }

    public double GammaPercent
    {
        get => _gammaPercent;
        set
        {
            var next = Math.Round(Math.Clamp(value, 50d, 150d), 2, MidpointRounding.AwayFromZero);
            if (SetProperty(ref _gammaPercent, next))
            {
                OnPropertyChanged(nameof(GammaReadout));
                OnPropertyChanged(nameof(ColorStatus));
                if (CanAdjustColor)
                {
                    ScheduleColorApply();
                }
            }
        }
    }

    public double Warmth
    {
        get => _warmth;
        set
        {
            var next = Math.Round(Math.Clamp(value, 0d, 100d), 2, MidpointRounding.AwayFromZero);
            if (SetProperty(ref _warmth, next))
            {
                OnPropertyChanged(nameof(WarmthReadout));
                OnPropertyChanged(nameof(ColorStatus));
                if (CanAdjustColor)
                {
                    ScheduleColorApply();
                }
            }
        }
    }

    public double StepPercent => (double)CurrentStep.Percent;

    public string StepReadout => CurrentStep.ToString();

    public string GammaReadout => $"{GammaPercent:0.00}%";

    public string WarmthReadout => $"{Warmth:0.00}%";

    public string BrightnessReadout => SelectedDisplay?.BrightnessReadout ?? "--";

    public string HardwareStatus => SelectedDisplay?.HardwareStatus ?? "HW OFF";

    public string ShadeStatus => SelectedDisplay?.ShadeStatus ?? "SHADE OFF";

    public string ColorStatus => !CanAdjustColor
        ? "COLOR LOCKED"
        : GammaPercent == 100d && Warmth == 0d
            ? "COLOR NEUTRAL"
            : $"GAMMA {GammaPercent:0.##}% · WARM {Warmth:0.##}%";

    public string ColorControlNotice => SelectedDisplay is null
        ? "Select a display to inspect color-ramp support."
        : _engine.DescribeColorAvailability(SelectedDisplay.Session);

    public string PlanDetail => SelectedDisplay?.PlanDetail ?? "NO DISPLAY PLAN";

    public IRelayCommand IncreaseStepCommand { get; }

    public IRelayCommand DecreaseStepCommand { get; }

    public IAsyncRelayCommand RestoreCommand { get; }

    public async Task InitializeAsync()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        DiagnosticLog.Write(
            $"START {Environment.OSVersion}; process architecture {RuntimeInformation.ProcessArchitecture}; " +
            $"platform adapter {_engine.Platform.Name}.");
        IsLoading = true;

        try
        {
            var diagnostics = await _engine.DiscoverAsync().ConfigureAwait(true);
            foreach (var session in _engine.Displays)
            {
                Displays.Add(
                    new DisplayControlViewModel(
                        _engine,
                        session,
                        () => CurrentStep,
                        () => MinimumBrightnessPercent,
                        display => SelectedDisplay = display));
                var capability = session.HardwareCapability is null
                    ? "none"
                    : $"{session.HardwareCapability.Minimum}..{session.HardwareCapability.Maximum}";
                DiagnosticLog.Write(
                    $"TARGET id={session.Id}; name=\"{session.Name}\"; " +
                    $"backend={session.BackendLabel}; bounds={session.Info.Bounds}; " +
                    $"hardware={capability}; display-device={session.Info.PlatformDisplayName ?? "unknown"}; " +
                    $"advanced-color={session.Info.AdvancedColorEnabled?.ToString() ?? "unknown"}; " +
                    $"notice=\"{session.Info.Notice}\".");
            }

            foreach (var diagnostic in diagnostics)
            {
                DiagnosticLog.Write($"PROBE {diagnostic}");
            }

            DiscoveryStatus =
                $"Detected {_engine.Displays.Count} controllable target(s) through {_engine.Platform.Name}. " +
                "Full probe details are in COPY LOG.";

            if (Displays.Count == 0)
            {
                DiscoveryStatus += " No controllable target was created.";
                OperationStatus = "Display discovery failed. No changes can be applied.";
                return;
            }

            SelectedDisplay = Displays[0];
            await Task.WhenAll(Displays.Select(display => display.InitializeAsync())).ConfigureAwait(true);

            var readFailures = Displays.Count(display => display.InitializationError is not null);
            OperationStatus = readFailures == 0
                ? "All display controls are ready; each monitor can now be adjusted independently."
                : $"Display controls are ready with {readFailures} initial read warning(s); affected monitors started at 50%.";
        }
        catch (Exception exception)
        {
            DiscoveryStatus = $"Display discovery failed: {exception.Message}";
            OperationStatus = "No display changes were made.";
        }
        finally
        {
            IsLoading = false;
        }
    }

    public void ConfigureSystemInput(SystemInputRegistration registration)
    {
        MinimumBrightnessPercent = registration.EmergencyRestoreRegistered ? 0d : 1d;
        if (!registration.EmergencyRestoreRegistered &&
            !registration.BrightnessKeysListening &&
            registration.EmergencyRestoreError == 0 &&
            registration.BrightnessKeysError == 0)
        {
            SafetyStatus = "SYSTEM INPUT UNAVAILABLE · MIN 1%";
            OperationStatus = "System input is unavailable on this platform; the test floor is restricted to 1%.";
            return;
        }

        SafetyStatus = (registration.EmergencyRestoreRegistered, registration.BrightnessKeysListening) switch
        {
            (true, true) => "BRIGHTNESS KEYS · HOME READY",
            (true, false) => $"RESTORE READY · KEY LISTENER FAILED {registration.BrightnessKeysError}",
            (false, true) => $"BRIGHTNESS KEYS · SAFETY FAILED {registration.EmergencyRestoreError} · MIN 1%",
            (false, false) => $"SYSTEM INPUT FAILED {registration.EmergencyRestoreError} · MIN 1%",
        };

        OperationStatus = (registration.EmergencyRestoreRegistered, registration.BrightnessKeysListening) switch
        {
            (true, true) =>
                "Physical brightness-button listener ready: each press adjusts BRIGHTNESS by STEP; Ctrl+Alt+Home restores.",
            (true, false) =>
                $"Emergency restore is ready, but the brightness-button listener failed ({registration.BrightnessKeysError}).",
            (false, true) =>
                $"Brightness-button listener is ready, but emergency restore failed ({registration.EmergencyRestoreError}); minimum is 1%.",
            (false, false) =>
                $"System input failed; restore error {registration.EmergencyRestoreError}, " +
                $"brightness listener error {registration.BrightnessKeysError}; minimum is 1%.",
        };

        DiagnosticLog.Write(
            $"INPUT emergency-restore={registration.EmergencyRestoreRegistered} " +
            $"(error {registration.EmergencyRestoreError}); brightness-hid-listener={registration.BrightnessKeysListening} " +
            $"(error {registration.BrightnessKeysError}).");
    }

    public void AdjustBrightnessByStep(int direction)
    {
        if (!CanControl || direction == 0)
        {
            return;
        }

        SelectedDisplay?.AdjustByStep(direction);
    }

    public void ReportDiagnosticsCopy(bool copied, string detail)
    {
        OperationStatus = copied
            ? $"Diagnostics copied. Paste them into the test report ({detail})."
            : $"Could not copy diagnostics: {detail}";
    }

    public async Task EmergencyRestoreAllAsync()
    {
        CancelColorApply();

        _gammaPercent = 100d;
        _warmth = 0d;
        OnPropertyChanged(nameof(GammaPercent));
        OnPropertyChanged(nameof(GammaReadout));
        OnPropertyChanged(nameof(Warmth));
        OnPropertyChanged(nameof(WarmthReadout));
        OnPropertyChanged(nameof(ColorStatus));

        foreach (var display in Displays)
        {
            display.RestoreLogicalBrightness();
        }

        await _engine.EmergencyRestoreAllAsync().ConfigureAwait(true);
        OperationStatus = "Emergency restore complete: shades and color ramps clear; hardware requested at 50%.";
        NotifySelectedDisplayProperties();
    }

    public void ReportShadeFailure(string targetId, Exception exception)
    {
        var affectedDisplay = Displays.FirstOrDefault(display => display.Id == targetId);
        if (affectedDisplay is not null)
        {
            _engine.CancelPendingApply(affectedDisplay.Session);
            affectedDisplay.HandleShadeFailure();
        }
        else
        {
            _engine.CancelPendingApplies();
        }

        OperationStatus = $"Shade failed safely for {affectedDisplay?.Name ?? targetId} and was removed; " +
            $"no hardware retry was sent: {exception.Message}";
        NotifySelectedDisplayProperties();
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        CancelColorApply();
        if (_selectedDisplay is not null)
        {
            _selectedDisplay.PropertyChanged -= OnSelectedDisplayPropertyChanged;
        }

        _engine.ShadeRequested -= OnEngineShadeRequested;
        _engine.OperationReported -= OnEngineOperationReported;
        _engine.DisplayStateChanged -= OnEngineDisplayStateChanged;
        _engine.Dispose();
    }

    private BrightnessStep CurrentStep => BrightnessStep.Presets[_stepIndex];

    private void ScheduleColorApply()
    {
        var selected = SelectedDisplay;
        if (!CanAdjustColor || selected is null)
        {
            return;
        }

        CancelColorApply();
        _colorApplyCancellation = new CancellationTokenSource();
        var adjustment = new DisplayColorAdjustment((decimal)(GammaPercent / 100d), (decimal)Warmth);
        _ = ApplyColorAfterDelayAsync(selected, adjustment, _colorApplyCancellation.Token);
    }

    private async Task ApplyColorAfterDelayAsync(
        DisplayControlViewModel selected,
        DisplayColorAdjustment adjustment,
        CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(60, cancellationToken).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
            var applied = _engine.TryApplyColor(selected.Session, adjustment);
            if (!applied)
            {
                Dispatcher.UIThread.Post(() =>
                {
                    ResetColorValues();
                    OnPropertyChanged(nameof(ColorStatus));
                });
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (ObjectDisposedException)
        {
            // Normal window shutdown can race the end of the short debounce.
        }
    }

    private void CancelColorApply()
    {
        _colorApplyCancellation?.Cancel();
        _colorApplyCancellation?.Dispose();
        _colorApplyCancellation = null;
    }

    private void ResetColorValues()
    {
        _gammaPercent = 100d;
        _warmth = 0d;
        OnPropertyChanged(nameof(GammaPercent));
        OnPropertyChanged(nameof(GammaReadout));
        OnPropertyChanged(nameof(Warmth));
        OnPropertyChanged(nameof(WarmthReadout));
        OnPropertyChanged(nameof(ColorStatus));
    }

    private void OnEngineShadeRequested(object? sender, ShadeRequestEventArgs e)
    {
        if (Dispatcher.UIThread.CheckAccess())
        {
            ShadeRequested?.Invoke(this, e);
        }
        else
        {
            Dispatcher.UIThread.Post(() => ShadeRequested?.Invoke(this, e));
        }
    }

    private void OnEngineOperationReported(object? sender, OperationReportedEventArgs e)
    {
        if (Dispatcher.UIThread.CheckAccess())
        {
            OperationStatus = e.Message;
        }
        else
        {
            Dispatcher.UIThread.Post(() => OperationStatus = e.Message);
        }
    }

    private void OnEngineDisplayStateChanged(object? sender, DisplayStateChangedEventArgs e)
    {
        void Refresh()
        {
            var display = Displays.FirstOrDefault(candidate => candidate.Id == e.TargetId);
            display?.RefreshPlan();
            if (ReferenceEquals(display, SelectedDisplay))
            {
                NotifySelectedDisplayProperties();
            }
        }

        if (Dispatcher.UIThread.CheckAccess())
        {
            Refresh();
        }
        else
        {
            Dispatcher.UIThread.Post(Refresh);
        }
    }

    private void OnSelectedDisplayPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(DisplayControlViewModel.BrightnessPercent) or
            nameof(DisplayControlViewModel.BrightnessReadout) or
            nameof(DisplayControlViewModel.HardwareStatus) or
            nameof(DisplayControlViewModel.ShadeStatus) or
            nameof(DisplayControlViewModel.PlanDetail) or
            nameof(DisplayControlViewModel.BackendLabel) or
            nameof(DisplayControlViewModel.CanControl))
        {
            NotifySelectedDisplayProperties();
        }
    }

    private void NotifySelectedDisplayProperties()
    {
        OnPropertyChanged(nameof(CanControl));
        OnPropertyChanged(nameof(SelectedBackend));
        OnPropertyChanged(nameof(TargetNotice));
        OnPropertyChanged(nameof(BrightnessPercent));
        OnPropertyChanged(nameof(BrightnessReadout));
        OnPropertyChanged(nameof(HardwareStatus));
        OnPropertyChanged(nameof(ShadeStatus));
        OnPropertyChanged(nameof(PlanDetail));
        OnPropertyChanged(nameof(CanAdjustColor));
        OnPropertyChanged(nameof(ColorControlNotice));
        OnPropertyChanged(nameof(ColorStatus));
    }
}
