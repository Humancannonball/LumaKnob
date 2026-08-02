using System.Diagnostics.CodeAnalysis;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Interactivity;
using Avalonia.Threading;
using LumaKnob.App.Services;
using LumaKnob.App.ViewModels;
using LumaKnob.Engine.Coordination;
using LumaKnob.Engine.Platform;

namespace LumaKnob.App.Views;

[SuppressMessage(
    "Design",
    "CA1001:Types that own disposable fields should be disposable",
    Justification = "Avalonia owns Window lifetime; the Closed handler disposes both owned services.")]
public sealed partial class MainWindow : Window
{
    private readonly IDisplayPlatform _platform;
    private ShadeController? _shadeController;
    private ISystemInputService? _systemInput;
    private bool _initialized;

    public MainWindow()
        : this(DesktopBootstrap.Platform)
    {
    }

    internal MainWindow(IDisplayPlatform platform)
    {
        _platform = platform;
        InitializeComponent();
        Opened += OnOpened;
        Closing += OnClosing;
        Closed += OnClosed;
        PropertyChanged += OnWindowPropertyChanged;
    }

    private void DragWindow(object? sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            BeginMoveDrag(e);
        }
    }

    private void MinimizeWindow(object? sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;

    private void CloseWindow(object? sender, RoutedEventArgs e) => Close();

    private void ToggleMotion(object? sender, RoutedEventArgs e)
    {
        SignalBackplane.MotionEnabled = !SignalBackplane.MotionEnabled;
        UpdateMotionButton();
        DiagnosticLog.Write(
            $"APPEARANCE Background motion {(SignalBackplane.MotionEnabled ? "resumed" : "paused")} by user.");
    }

    private async void CopyDiagnostics(object? sender, RoutedEventArgs e)
    {
        var viewModel = DataContext as MainWindowViewModel;
        try
        {
            var log = DiagnosticLog.ReadSession();
            var clipboard = Clipboard ?? throw new InvalidOperationException("The system clipboard is unavailable.");
            await clipboard.SetTextAsync(log);
            await clipboard.FlushAsync();
            viewModel?.ReportDiagnosticsCopy(copied: true, "session.log");
        }
        catch (Exception exception)
        {
            viewModel?.ReportDiagnosticsCopy(copied: false, exception.Message);
        }
    }

    private async void OnOpened(object? sender, EventArgs e)
    {
        if (_initialized || DataContext is not MainWindowViewModel viewModel)
        {
            return;
        }

        _initialized = true;

        _shadeController = new ShadeController(this, _platform);
        viewModel.ShadeRequested += OnShadeRequested;
        UpdateMotionButton();
        UpdateBackplaneActivity();

        foreach (var screen in Screens.All)
        {
            DiagnosticLog.Write(
                $"SCREEN bounds={screen.Bounds}; working-area={screen.WorkingArea}; " +
                $"scale={screen.Scaling:0.##}; primary={screen.IsPrimary}.");
        }

        _systemInput = _platform.CreateSystemInputService();
        _systemInput.EmergencyRestorePressed += OnEmergencyRestorePressed;
        _systemInput.BrightnessIncreasePressed += OnBrightnessIncreasePressed;
        _systemInput.BrightnessDecreasePressed += OnBrightnessDecreasePressed;
        viewModel.ConfigureSystemInput(_systemInput.Start());

        await viewModel.InitializeAsync();
    }

    private void UpdateMotionButton()
    {
        MotionToggleButton.Content = SignalBackplane.MotionEnabled ? "FX LIVE" : "FX PAUSED";
    }

    private void OnClosed(object? sender, EventArgs e)
    {
        PropertyChanged -= OnWindowPropertyChanged;
        if (DataContext is MainWindowViewModel viewModel)
        {
            viewModel.ShadeRequested -= OnShadeRequested;
        }

        _shadeController?.Dispose();
        _shadeController = null;

        if (_systemInput is not null)
        {
            _systemInput.EmergencyRestorePressed -= OnEmergencyRestorePressed;
            _systemInput.BrightnessIncreasePressed -= OnBrightnessIncreasePressed;
            _systemInput.BrightnessDecreasePressed -= OnBrightnessDecreasePressed;
            _systemInput.Dispose();
            _systemInput = null;
        }

        (DataContext as IDisposable)?.Dispose();
    }

    private void OnWindowPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == IsVisibleProperty || e.Property == WindowStateProperty)
        {
            UpdateBackplaneActivity();
        }
    }

    private void UpdateBackplaneActivity() =>
        SignalBackplane.SetHostActive(IsVisible && WindowState != WindowState.Minimized);

    private void OnClosing(object? sender, WindowClosingEventArgs e)
    {
        if (e.CloseReason is WindowCloseReason.ApplicationShutdown or WindowCloseReason.OSShutdown)
        {
            return;
        }

        e.Cancel = true;
        Hide();
        DiagnosticLog.Write("LIFETIME control deck hidden; background tray process remains active.");
    }

    private void OnShadeRequested(object? sender, ShadeRequestEventArgs e)
    {
        try
        {
            _shadeController?.Apply(e.TargetId, e.Bounds, e.Opacity);
        }
        catch (Exception exception)
        {
            _shadeController?.Clear(e.TargetId);
            (DataContext as MainWindowViewModel)?.ReportShadeFailure(e.TargetId, exception);
        }
    }

    private void OnEmergencyRestorePressed(object? sender, EventArgs e) =>
        Dispatcher.UIThread.Post(() =>
        {
            DiagnosticLog.Write("INPUT emergency restore hotkey pressed.");
            _ = EmergencyRestoreAsync();
        });

    private void OnBrightnessIncreasePressed(object? sender, EventArgs e) =>
        Dispatcher.UIThread.Post(() => HandleBrightnessButton(1));

    private void OnBrightnessDecreasePressed(object? sender, EventArgs e) =>
        Dispatcher.UIThread.Post(() => HandleBrightnessButton(-1));

    private void HandleBrightnessButton(int direction)
    {
        DiagnosticLog.Write(
            $"INPUT physical brightness button {(direction > 0 ? "increment" : "decrement")}.");
        (DataContext as MainWindowViewModel)?.AdjustBrightnessByStep(direction);
    }

    internal async Task EmergencyRestoreAsync()
    {
        _shadeController?.ClearAll();
        if (DataContext is MainWindowViewModel viewModel)
        {
            await viewModel.EmergencyRestoreAllAsync();
        }
    }

    internal void ShowFromTray()
    {
        if (!IsVisible)
        {
            Show();
        }

        WindowState = WindowState.Normal;
        UpdateBackplaneActivity();
        Activate();
    }
}
