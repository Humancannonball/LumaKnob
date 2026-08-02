using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using LumaKnob.Engine.Displays;
using LumaKnob.Engine.Platform;

namespace LumaKnob.App.Services;

internal sealed class ShadeWindow : Window
{
    private Exception? _activationFailure;
    private decimal _requestedOpacity;

    public ShadeWindow(DisplayPixelBounds bounds, double scaling, IDisplayPlatform platform)
    {
        Position = new PixelPoint(bounds.X, bounds.Y);
        Width = bounds.Width / scaling;
        Height = bounds.Height / scaling;
        WindowStartupLocation = WindowStartupLocation.Manual;
        WindowDecorations = WindowDecorations.None;
        CanResize = false;
        ShowInTaskbar = false;
        ShowActivated = false;
        Topmost = true;
        Background = Brushes.Black;
        Opacity = 0;
        IsHitTestVisible = false;

        Opened += (_, _) =>
        {
            try
            {
                var handle = TryGetPlatformHandle();
                if (handle is not null && platform.SupportsShadeWindows)
                {
                    platform.ConfigureShadeWindow(handle.Handle);
                }

                Opacity = (double)_requestedOpacity;
            }
            catch (Exception exception)
            {
                _activationFailure = exception;
                Opacity = 0;
                Hide();
            }
        };
    }

    public void SetOpacity(decimal opacity)
    {
        _requestedOpacity = Math.Clamp(opacity, 0m, 1m);

        if (_requestedOpacity <= 0m)
        {
            Opacity = 0;
            if (IsVisible)
            {
                Hide();
            }

            return;
        }

        if (!IsVisible)
        {
            Opacity = 0;
            Show();
        }

        if (_activationFailure is not null)
        {
            throw new InvalidOperationException("The click-through shade window could not be activated.", _activationFailure);
        }

        Opacity = (double)_requestedOpacity;
    }
}
