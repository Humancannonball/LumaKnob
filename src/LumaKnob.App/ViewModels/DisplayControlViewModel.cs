using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LumaKnob.Core.Brightness;
using LumaKnob.Engine.Coordination;

namespace LumaKnob.App.ViewModels;

public sealed class DisplayControlViewModel : ObservableObject
{
    private static readonly BrightnessValue HybridPivot = BrightnessValue.FromPercent(10m);
    private static readonly TimeSpan HardwareSettleDelay = TimeSpan.FromMilliseconds(100);
    private readonly BrightnessEngine _engine;
    private readonly Func<BrightnessStep> _currentStep;
    private readonly Func<double> _minimumBrightnessPercent;
    private readonly Action<DisplayControlViewModel> _select;
    private BrightnessValue _brightness = BrightnessValue.FromPercent(50m);
    private BrightnessPlan _plan;
    private bool _suppressApply = true;
    private bool _canControl;
    private bool _isSelected;
    private string? _initializationError;

    internal DisplayControlViewModel(
        BrightnessEngine engine,
        BrightnessDisplay session,
        Func<BrightnessStep> currentStep,
        Func<double> minimumBrightnessPercent,
        Action<DisplayControlViewModel> select)
    {
        _engine = engine;
        Session = session;
        _currentStep = currentStep;
        _minimumBrightnessPercent = minimumBrightnessPercent;
        _select = select;
        _plan = _engine.CreatePlan(Session, _brightness, HybridPivot);

        IncreaseBrightnessCommand = new RelayCommand(() => AdjustByStep(1));
        DecreaseBrightnessCommand = new RelayCommand(() => AdjustByStep(-1));
        SelectCommand = new RelayCommand(() => _select(this));
    }

    public BrightnessDisplay Session { get; }

    public string Id => Session.Id;

    public string Name => Session.Name;

    public string BackendLabel => Session.IsHardwareFaulted
        ? "SHADE ONLY · HARDWARE FAULT"
        : Session.BackendLabel;

    public string? Notice => Session.Info.Notice;

    public bool CanControl
    {
        get => _canControl;
        private set => SetProperty(ref _canControl, value);
    }

    public bool IsSelected
    {
        get => _isSelected;
        internal set
        {
            if (SetProperty(ref _isSelected, value))
            {
                OnPropertyChanged(nameof(SelectionStatus));
            }
        }
    }

    public string SelectionStatus => IsSelected ? "COLOR + KEY TARGET" : "SELECT COLOR / KEYS";

    public string? InitializationError
    {
        get => _initializationError;
        private set => SetProperty(ref _initializationError, value);
    }

    public double MinimumBrightnessPercent => _minimumBrightnessPercent();

    public double StepPercent => (double)_currentStep().Percent;

    public double BrightnessPercent
    {
        get => (double)_brightness.Percent;
        set
        {
            var clamped = Math.Clamp((decimal)value, (decimal)MinimumBrightnessPercent, 100m);
            var next = BrightnessValue.FromPercent(clamped);
            if (!SetProperty(ref _brightness, next, nameof(BrightnessPercent)))
            {
                return;
            }

            OnPropertyChanged(nameof(BrightnessReadout));
            UpdatePlan(scheduleApply: !_suppressApply);
        }
    }

    public string BrightnessReadout => _brightness.ToString();

    public string HardwareStatus => _plan.Mechanisms.HasFlag(BrightnessMechanism.Hardware)
        ? $"HW {_plan.HardwareValue}"
        : "HW OFF";

    public string ShadeStatus => _plan.Mechanisms.HasFlag(BrightnessMechanism.Shade)
        ? $"SHADE {decimal.Round(_plan.ShadeOpacity * 100m)}%"
        : "SHADE OFF";

    public string PlanDetail => BrightnessEngine.DescribePlanDetail(_plan, HybridPivot);

    public IRelayCommand IncreaseBrightnessCommand { get; }

    public IRelayCommand DecreaseBrightnessCommand { get; }

    public IRelayCommand SelectCommand { get; }

    internal async Task InitializeAsync()
    {
        _suppressApply = true;
        try
        {
            _brightness = await _engine.ReadBrightnessAsync(Session).ConfigureAwait(true);
            InitializationError = null;
        }
        catch (Exception exception)
        {
            _brightness = BrightnessValue.FromPercent(50m);
            InitializationError = exception.Message;
        }
        finally
        {
            OnPropertyChanged(nameof(BrightnessPercent));
            OnPropertyChanged(nameof(BrightnessReadout));
            UpdatePlan(scheduleApply: false);
            CanControl = true;
            _suppressApply = false;
        }
    }

    internal void AdjustByStep(int direction)
    {
        if (!CanControl || direction == 0)
        {
            return;
        }

        BrightnessPercent = (double)_brightness.Adjust(_currentStep(), Math.Sign(direction)).Percent;
    }

    internal void RestoreLogicalBrightness()
    {
        _suppressApply = true;
        try
        {
            _brightness = BrightnessValue.FromPercent(50m);
            OnPropertyChanged(nameof(BrightnessPercent));
            OnPropertyChanged(nameof(BrightnessReadout));
            UpdatePlan(scheduleApply: false);
        }
        finally
        {
            _suppressApply = false;
        }
    }

    internal void HandleShadeFailure()
    {
        _suppressApply = true;
        try
        {
            _brightness = BrightnessValue.Maximum;
            OnPropertyChanged(nameof(BrightnessPercent));
            OnPropertyChanged(nameof(BrightnessReadout));
            UpdatePlan(scheduleApply: false);
        }
        finally
        {
            _suppressApply = false;
        }
    }

    internal void NotifySharedSettingsChanged()
    {
        OnPropertyChanged(nameof(MinimumBrightnessPercent));
        OnPropertyChanged(nameof(StepPercent));
    }

    internal void RefreshPlan()
    {
        UpdatePlan(scheduleApply: false);
        OnPropertyChanged(nameof(BackendLabel));
    }

    public override string ToString() => $"{Name}  [{BackendLabel}]";

    private void UpdatePlan(bool scheduleApply)
    {
        _plan = _engine.CreatePlan(Session, _brightness, HybridPivot);
        OnPropertyChanged(nameof(HardwareStatus));
        OnPropertyChanged(nameof(ShadeStatus));
        OnPropertyChanged(nameof(PlanDetail));

        if (scheduleApply)
        {
            _engine.Apply(Session, _plan, HybridPivot, HardwareSettleDelay);
        }
    }
}
