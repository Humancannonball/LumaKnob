using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using LumaKnob.App.Services;

namespace LumaKnob.App.Views;

public sealed partial class TrayPanelWindow : Window
{
    public TrayPanelWindow()
    {
        InitializeComponent();
        Opened += OnOpened;
        Deactivated += OnDeactivated;
        Closing += OnClosing;
    }

    public event EventHandler? OpenControlDeckRequested;

    internal void ToggleNearTaskbar()
    {
        if (IsVisible)
        {
            Hide();
            return;
        }

        ShowNearTaskbar();
    }

    internal void ShowNearTaskbar()
    {
        if (!IsVisible)
        {
            Show();
        }

        PositionNearTaskbar();
        Activate();
        DiagnosticLog.Write("TRAY quick control opened.");
    }

    private void OnOpened(object? sender, EventArgs e) => PositionNearTaskbar();

    private void OnDeactivated(object? sender, EventArgs e)
    {
        if (IsVisible)
        {
            Hide();
        }
    }

    private void OnClosing(object? sender, WindowClosingEventArgs e)
    {
        if (e.CloseReason is WindowCloseReason.ApplicationShutdown or WindowCloseReason.OSShutdown)
        {
            return;
        }

        e.Cancel = true;
        Hide();
    }

    private void OpenControlDeck(object? sender, RoutedEventArgs e)
    {
        Hide();
        OpenControlDeckRequested?.Invoke(this, EventArgs.Empty);
    }

    private void HidePanel(object? sender, RoutedEventArgs e) => Hide();

    private void PositionNearTaskbar()
    {
        var screen = Screens.ScreenFromWindow(this) ?? Screens.Primary;
        if (screen is null)
        {
            return;
        }

        const int edgeGap = 12;
        var widthInPixels = checked((int)Math.Ceiling(Width * screen.Scaling));
        var heightInPixels = checked((int)Math.Ceiling(Height * screen.Scaling));
        Position = new PixelPoint(
            screen.WorkingArea.Right - widthInPixels - edgeGap,
            screen.WorkingArea.Bottom - heightInPixels - edgeGap);
    }
}
