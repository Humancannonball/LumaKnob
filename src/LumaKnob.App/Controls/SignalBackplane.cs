using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;

namespace LumaKnob.App.Controls;

/// <summary>
/// A lightweight, code-drawn equipment backplane. It deliberately depends on
/// no display backend and uses a throttled UI timer so it can be reused by a
/// future Linux host or disabled by an appearance preset.
/// </summary>
public sealed class SignalBackplane : Control
{
    public static readonly StyledProperty<bool> MotionEnabledProperty =
        AvaloniaProperty.Register<SignalBackplane, bool>(nameof(MotionEnabled), true);

    public static readonly StyledProperty<double> MotionSpeedProperty =
        AvaloniaProperty.Register<SignalBackplane, double>(nameof(MotionSpeed), 0.72);

    public static readonly StyledProperty<double> MotionIntensityProperty =
        AvaloniaProperty.Register<SignalBackplane, double>(nameof(MotionIntensity), 0.62);

    public static readonly StyledProperty<double> FramesPerSecondProperty =
        AvaloniaProperty.Register<SignalBackplane, double>(nameof(FramesPerSecond), 30);

    public static readonly StyledProperty<IBrush> BaseBrushProperty =
        AvaloniaProperty.Register<SignalBackplane, IBrush>(
            nameof(BaseBrush),
            new SolidColorBrush(Color.Parse("#D90C1112")));

    public static readonly StyledProperty<IBrush> GridBrushProperty =
        AvaloniaProperty.Register<SignalBackplane, IBrush>(
            nameof(GridBrush),
            new SolidColorBrush(Color.Parse("#283F5A4B")));

    public static readonly StyledProperty<IBrush> SignalBrushProperty =
        AvaloniaProperty.Register<SignalBackplane, IBrush>(
            nameof(SignalBrush),
            new SolidColorBrush(Color.Parse("#7C87C569")));

    public static readonly StyledProperty<IBrush> GlowBrushProperty =
        AvaloniaProperty.Register<SignalBackplane, IBrush>(
            nameof(GlowBrush),
            new SolidColorBrush(Color.Parse("#204FD77D")));

    public static readonly StyledProperty<IBrush> PulseBrushProperty =
        AvaloniaProperty.Register<SignalBackplane, IBrush>(
            nameof(PulseBrush),
            new SolidColorBrush(Color.Parse("#D9C7F39D")));

    public static readonly StyledProperty<IBrush> ScanBrushProperty =
        AvaloniaProperty.Register<SignalBackplane, IBrush>(
            nameof(ScanBrush),
            new SolidColorBrush(Color.Parse("#0FFFFFFF")));

    private readonly DispatcherTimer _timer = new(DispatcherPriority.Background);
    private long _lastTimestamp;
    private double _elapsedSeconds;
    private bool _attached;
    private bool _hostActive = true;

    static SignalBackplane()
    {
        AffectsRender<SignalBackplane>(
            MotionEnabledProperty,
            MotionSpeedProperty,
            MotionIntensityProperty,
            BaseBrushProperty,
            GridBrushProperty,
            SignalBrushProperty,
            GlowBrushProperty,
            PulseBrushProperty,
            ScanBrushProperty);
    }

    public SignalBackplane()
    {
        IsHitTestVisible = false;
        ClipToBounds = true;
        _timer.Tick += OnAnimationTick;
    }

    public bool MotionEnabled
    {
        get => GetValue(MotionEnabledProperty);
        set => SetValue(MotionEnabledProperty, value);
    }

    public double MotionSpeed
    {
        get => GetValue(MotionSpeedProperty);
        set => SetValue(MotionSpeedProperty, value);
    }

    public double MotionIntensity
    {
        get => GetValue(MotionIntensityProperty);
        set => SetValue(MotionIntensityProperty, value);
    }

    public double FramesPerSecond
    {
        get => GetValue(FramesPerSecondProperty);
        set => SetValue(FramesPerSecondProperty, value);
    }

    public IBrush BaseBrush
    {
        get => GetValue(BaseBrushProperty);
        set => SetValue(BaseBrushProperty, value);
    }

    public IBrush GridBrush
    {
        get => GetValue(GridBrushProperty);
        set => SetValue(GridBrushProperty, value);
    }

    public IBrush SignalBrush
    {
        get => GetValue(SignalBrushProperty);
        set => SetValue(SignalBrushProperty, value);
    }

    public IBrush GlowBrush
    {
        get => GetValue(GlowBrushProperty);
        set => SetValue(GlowBrushProperty, value);
    }

    public IBrush PulseBrush
    {
        get => GetValue(PulseBrushProperty);
        set => SetValue(PulseBrushProperty, value);
    }

    public IBrush ScanBrush
    {
        get => GetValue(ScanBrushProperty);
        set => SetValue(ScanBrushProperty, value);
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);
        if (Bounds.Width <= 0 || Bounds.Height <= 0)
        {
            return;
        }

        context.DrawRectangle(BaseBrush, null, Bounds);

        var intensity = Math.Clamp(MotionIntensity, 0, 1);
        using (context.PushOpacity(intensity))
        {
            DrawMovingGrid(context);
            DrawSignalBuses(context);
            DrawScopeTrace(context);
            DrawPulses(context);
            DrawScanSheen(context);
        }
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _attached = true;
        UpdateTimer();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        _attached = false;
        _timer.Stop();
        base.OnDetachedFromVisualTree(e);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == MotionEnabledProperty || change.Property == FramesPerSecondProperty)
        {
            UpdateTimer();
        }
    }

    internal void SetHostActive(bool active)
    {
        if (_hostActive == active)
        {
            return;
        }

        _hostActive = active;
        UpdateTimer();
    }

    private void UpdateTimer()
    {
        if (!_attached || !_hostActive || !MotionEnabled)
        {
            _timer.Stop();
            InvalidateVisual();
            return;
        }

        var framesPerSecond = Math.Clamp(FramesPerSecond, 10, 60);
        _timer.Interval = TimeSpan.FromSeconds(1 / framesPerSecond);
        _lastTimestamp = Stopwatch.GetTimestamp();
        _timer.Start();
    }

    private void OnAnimationTick(object? sender, EventArgs e)
    {
        var now = Stopwatch.GetTimestamp();
        if (!IsEffectivelyVisible)
        {
            _lastTimestamp = now;
            return;
        }

        var elapsed = Stopwatch.GetElapsedTime(_lastTimestamp, now).TotalSeconds;
        _lastTimestamp = now;
        _elapsedSeconds += Math.Min(elapsed, 0.1) * Math.Clamp(MotionSpeed, 0.1, 3);
        InvalidateVisual();
    }

    private void DrawMovingGrid(DrawingContext context)
    {
        const double spacing = 24;
        var pen = new Pen(GridBrush, 1);
        var offset = MotionEnabled ? (_elapsedSeconds * 7) % spacing : 0;

        for (var x = offset - spacing; x < Bounds.Width + spacing; x += spacing)
        {
            context.DrawLine(pen, new Point(x, 0), new Point(x, Bounds.Height));
        }

        for (var y = 18d; y < Bounds.Height; y += spacing)
        {
            context.DrawLine(pen, new Point(0, y), new Point(Bounds.Width, y));
        }
    }

    private void DrawSignalBuses(DrawingContext context)
    {
        var signalPen = new Pen(SignalBrush, 1.1);
        var glowPen = new Pen(GlowBrush, 5);
        var rows = new[] { 41d, 114d, 233d, Math.Max(54, Bounds.Height - 54) };

        for (var index = 0; index < rows.Length; index++)
        {
            var y = Math.Min(rows[index], Bounds.Height - 12);
            var notch = 80 + (index * 97);
            var points = new[]
            {
                new Point(0, y),
                new Point(Math.Min(notch, Bounds.Width * 0.42), y),
                new Point(Math.Min(notch + 20, Bounds.Width * 0.48), y + (index % 2 == 0 ? 14 : -14)),
                new Point(Bounds.Width, y + (index % 2 == 0 ? 14 : -14)),
            };

            for (var point = 1; point < points.Length; point++)
            {
                context.DrawLine(glowPen, points[point - 1], points[point]);
                context.DrawLine(signalPen, points[point - 1], points[point]);
            }
        }
    }

    private void DrawScopeTrace(DrawingContext context)
    {
        var yCenter = Bounds.Height * 0.76;
        var amplitude = Math.Clamp(Bounds.Height * 0.035, 7, 14);
        var glowPen = new Pen(GlowBrush, 6);
        var signalPen = new Pen(SignalBrush, 1.6);
        var previous = ScopePoint(0, yCenter, amplitude);

        for (var x = 6d; x <= Bounds.Width + 6; x += 6)
        {
            var next = ScopePoint(x, yCenter, amplitude);
            context.DrawLine(glowPen, previous, next);
            context.DrawLine(signalPen, previous, next);
            previous = next;
        }
    }

    private Point ScopePoint(double x, double yCenter, double amplitude)
    {
        var phase = _elapsedSeconds * 2.2;
        var normalized = x / Math.Max(1, Bounds.Width);
        var carrier = Math.Sin((normalized * Math.PI * 8) - phase);
        var modulation = Math.Sin((normalized * Math.PI * 2.4) + (phase * 0.37));
        return new Point(x, yCenter + (carrier * amplitude * (0.42 + (modulation * 0.36))));
    }

    private void DrawPulses(DrawingContext context)
    {
        var rows = new[] { 41d, 128d, Math.Max(56, Bounds.Height - 40) };
        for (var index = 0; index < rows.Length; index++)
        {
            var progress = (_elapsedSeconds * (0.105 + (index * 0.018)) + (index * 0.31)) % 1;
            var x = progress * (Bounds.Width + 50) - 25;
            var y = Math.Min(rows[index], Bounds.Height - 10);
            context.DrawLine(
                new Pen(GlowBrush, 8),
                new Point(Math.Max(0, x - 28), y),
                new Point(x, y));
            context.DrawEllipse(GlowBrush, null, new Point(x, y), 7, 7);
            context.DrawEllipse(PulseBrush, null, new Point(x, y), 2.4, 2.4);
        }
    }

    private void DrawScanSheen(DrawingContext context)
    {
        var progress = (_elapsedSeconds * 0.045) % 1;
        var x = progress * (Bounds.Width + 120) - 60;
        context.DrawRectangle(ScanBrush, null, new Rect(x, 0, 42, Bounds.Height));
    }
}
