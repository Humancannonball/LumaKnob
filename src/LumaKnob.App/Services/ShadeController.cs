using Avalonia.Controls;
using LumaKnob.Engine.Displays;
using LumaKnob.Engine.Platform;

namespace LumaKnob.App.Services;

internal sealed class ShadeController(Window owner, IDisplayPlatform platform) : IDisposable
{
    private readonly Dictionary<string, ShadeWindow> _windows = [];

    public void Apply(string targetId, DisplayPixelBounds bounds, decimal opacity)
    {
        if (!platform.SupportsShadeWindows || bounds.IsEmpty)
        {
            return;
        }

        if (!_windows.TryGetValue(targetId, out var shade))
        {
            shade = new ShadeWindow(bounds, FindScaling(bounds), platform);
            _windows.Add(targetId, shade);
        }

        shade.SetOpacity(opacity);
        UpdateOwnerState();
    }

    public void Clear(string targetId)
    {
        if (_windows.TryGetValue(targetId, out var shade))
        {
            shade.SetOpacity(0m);
        }

        UpdateOwnerState();
    }

    public void ClearAll()
    {
        foreach (var shade in _windows.Values)
        {
            shade.SetOpacity(0m);
        }

        owner.Topmost = false;
    }

    public void Dispose()
    {
        ClearAll();
        foreach (var shade in _windows.Values)
        {
            shade.Close();
        }

        _windows.Clear();
    }

    private void UpdateOwnerState()
    {
        owner.Topmost = _windows.Values.Any(window => window.IsVisible);
        if (owner.Topmost && owner.IsVisible)
        {
            owner.Activate();
        }
    }

    private double FindScaling(DisplayPixelBounds bounds)
    {
        var bestArea = 0L;
        var bestScaling = 1d;

        foreach (var screen in owner.Screens.All)
        {
            var left = Math.Max(bounds.X, screen.Bounds.X);
            var top = Math.Max(bounds.Y, screen.Bounds.Y);
            var right = Math.Min(bounds.X + bounds.Width, screen.Bounds.Right);
            var bottom = Math.Min(bounds.Y + bounds.Height, screen.Bounds.Bottom);
            var area = (long)Math.Max(0, right - left) * Math.Max(0, bottom - top);
            if (area > bestArea)
            {
                bestArea = area;
                bestScaling = screen.Scaling;
            }
        }

        return bestScaling;
    }
}
