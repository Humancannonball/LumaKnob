using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using LumaKnob.App.Appearance;
using LumaKnob.App.Services;
using LumaKnob.App.ViewModels;
using LumaKnob.App.Views;
using LumaKnob.Engine.Coordination;

namespace LumaKnob.App;

public sealed partial class App : Application
{
    private IClassicDesktopStyleApplicationLifetime? _desktop;
    private MainWindow? _mainWindow;
    private TrayPanelWindow? _quickControl;
    private bool _exitRequested;

    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        AppearancePresetLoader.TryApply(this, DesktopBootstrap.AppearancePresetPath);
        if (DesktopBootstrap.MotionDisabled)
        {
            Resources["BackplaneMotionEnabled"] = false;
        }

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            _desktop = desktop;
            desktop.ShutdownMode = ShutdownMode.OnExplicitShutdown;

            var engine = new BrightnessEngine(DesktopBootstrap.Platform);
            var viewModel = new MainWindowViewModel(engine);
            _mainWindow = new MainWindow
            {
                DataContext = viewModel,
            };
            _quickControl = new TrayPanelWindow
            {
                DataContext = viewModel,
            };
            _quickControl.OpenControlDeckRequested += OnOpenControlDeckRequested;
            desktop.MainWindow = _mainWindow;
            DesktopBootstrap.ActivationRequested += OnActivationRequested;
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void OpenQuickControlFromTray(object? sender, EventArgs e) =>
        _quickControl?.ToggleNearTaskbar();

    private void OpenQuickControlFromMenu(object? sender, EventArgs e) =>
        _quickControl?.ShowNearTaskbar();

    private void OpenControlDeckFromMenu(object? sender, EventArgs e) => ShowControlDeck();

    private async void RestoreFromTray(object? sender, EventArgs e)
    {
        try
        {
            if (_mainWindow is not null)
            {
                await _mainWindow.EmergencyRestoreAsync();
            }
        }
        catch (Exception exception)
        {
            DiagnosticLog.Write($"TRAY restore failed: {exception.Message}");
        }
    }

    private void ExitFromTray(object? sender, EventArgs e)
    {
        if (_exitRequested || _desktop is null)
        {
            return;
        }

        _exitRequested = true;
        DiagnosticLog.Write("LIFETIME explicit exit requested from notification area.");
        DesktopBootstrap.ActivationRequested -= OnActivationRequested;
        if (_quickControl is not null)
        {
            _quickControl.OpenControlDeckRequested -= OnOpenControlDeckRequested;
        }

        var icons = TrayIcon.GetIcons(this);
        if (icons is not null)
        {
            foreach (var icon in icons)
            {
                icon.IsVisible = false;
            }
        }

        _desktop.Shutdown();
    }

    private void OnOpenControlDeckRequested(object? sender, EventArgs e) => ShowControlDeck();

    private void OnActivationRequested(object? sender, EventArgs e) =>
        Dispatcher.UIThread.Post(() => _quickControl?.ShowNearTaskbar());

    private void ShowControlDeck()
    {
        _quickControl?.Hide();
        _mainWindow?.ShowFromTray();
    }
}
