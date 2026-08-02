using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Media;

namespace LumaKnob.App.Controls;

/// <summary>
/// A code-drawn dial with detented circular pointer, wheel, and keyboard input.
/// NumericUpDown companions in the view provide direct entry and a native
/// range automation peer while the custom peer is completed.
/// </summary>
public sealed class RotaryKnob : Control
{
    public static readonly StyledProperty<double> MinimumProperty =
        AvaloniaProperty.Register<RotaryKnob, double>(nameof(Minimum), 0d);

    public static readonly StyledProperty<double> MaximumProperty =
        AvaloniaProperty.Register<RotaryKnob, double>(nameof(Maximum), 100d);

    public static readonly StyledProperty<double> ValueProperty =
        AvaloniaProperty.Register<RotaryKnob, double>(
            nameof(Value),
            0d,
            defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<double> StepProperty =
        AvaloniaProperty.Register<RotaryKnob, double>(nameof(Step), 1d);

    public static readonly StyledProperty<double> FineStepProperty =
        AvaloniaProperty.Register<RotaryKnob, double>(nameof(FineStep), 0.01d);

    public static readonly StyledProperty<double> DepthScaleProperty =
        AvaloniaProperty.Register<RotaryKnob, double>(nameof(DepthScale), 1.2d);

    public static readonly StyledProperty<double> HomeValueProperty =
        AvaloniaProperty.Register<RotaryKnob, double>(nameof(HomeValue), double.NaN);

    public static readonly StyledProperty<double> EndValueProperty =
        AvaloniaProperty.Register<RotaryKnob, double>(nameof(EndValue), double.NaN);

    public static readonly StyledProperty<IBrush> OuterBrushProperty =
        AvaloniaProperty.Register<RotaryKnob, IBrush>(
            nameof(OuterBrush),
            new SolidColorBrush(Color.Parse("#090B0C")));

    public static readonly StyledProperty<IBrush> FaceBrushProperty =
        AvaloniaProperty.Register<RotaryKnob, IBrush>(
            nameof(FaceBrush),
            new SolidColorBrush(Color.Parse("#34393B")));

    public static readonly StyledProperty<IBrush> HubBrushProperty =
        AvaloniaProperty.Register<RotaryKnob, IBrush>(
            nameof(HubBrush),
            new SolidColorBrush(Color.Parse("#171A1B")));

    public static readonly StyledProperty<IBrush> RimBrushProperty =
        AvaloniaProperty.Register<RotaryKnob, IBrush>(
            nameof(RimBrush),
            new SolidColorBrush(Color.Parse("#697174")));

    public static readonly StyledProperty<IBrush> ShadowBrushProperty =
        AvaloniaProperty.Register<RotaryKnob, IBrush>(
            nameof(ShadowBrush),
            new SolidColorBrush(Color.Parse("#050606")));

    public static readonly StyledProperty<IBrush> TickBrushProperty =
        AvaloniaProperty.Register<RotaryKnob, IBrush>(
            nameof(TickBrush),
            new SolidColorBrush(Color.Parse("#757D80")));

    public static readonly StyledProperty<IBrush> ActiveTickBrushProperty =
        AvaloniaProperty.Register<RotaryKnob, IBrush>(
            nameof(ActiveTickBrush),
            new SolidColorBrush(Color.Parse("#B4D99B")));

    public static readonly StyledProperty<IBrush> MarkerBrushProperty =
        AvaloniaProperty.Register<RotaryKnob, IBrush>(
            nameof(MarkerBrush),
            new SolidColorBrush(Color.Parse("#E2E8DB")));

    public static readonly StyledProperty<IBrush> FocusBrushProperty =
        AvaloniaProperty.Register<RotaryKnob, IBrush>(
            nameof(FocusBrush),
            new SolidColorBrush(Color.Parse("#E2B659")));

    public static readonly StyledProperty<IBrush> HighlightBrushProperty =
        AvaloniaProperty.Register<RotaryKnob, IBrush>(
            nameof(HighlightBrush),
            new SolidColorBrush(Color.Parse("#A6B0B2")));

    private bool _dragging;
    private double? _lastPointerAngle;
    private double _dragAngleRemainder;

    static RotaryKnob()
    {
        AffectsRender<RotaryKnob>(
            MinimumProperty,
            MaximumProperty,
            ValueProperty,
            DepthScaleProperty,
            OuterBrushProperty,
            FaceBrushProperty,
            HubBrushProperty,
            RimBrushProperty,
            ShadowBrushProperty,
            TickBrushProperty,
            ActiveTickBrushProperty,
            MarkerBrushProperty,
            FocusBrushProperty,
            HighlightBrushProperty);
        FocusableProperty.OverrideDefaultValue<RotaryKnob>(true);
    }

    public double Minimum
    {
        get => GetValue(MinimumProperty);
        set => SetValue(MinimumProperty, value);
    }

    public double Maximum
    {
        get => GetValue(MaximumProperty);
        set => SetValue(MaximumProperty, value);
    }

    public double Value
    {
        get => GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    public double Step
    {
        get => GetValue(StepProperty);
        set => SetValue(StepProperty, value);
    }

    public double FineStep
    {
        get => GetValue(FineStepProperty);
        set => SetValue(FineStepProperty, value);
    }

    public double DepthScale
    {
        get => GetValue(DepthScaleProperty);
        set => SetValue(DepthScaleProperty, value);
    }

    public double HomeValue
    {
        get => GetValue(HomeValueProperty);
        set => SetValue(HomeValueProperty, value);
    }

    public double EndValue
    {
        get => GetValue(EndValueProperty);
        set => SetValue(EndValueProperty, value);
    }

    public IBrush OuterBrush
    {
        get => GetValue(OuterBrushProperty);
        set => SetValue(OuterBrushProperty, value);
    }

    public IBrush FaceBrush
    {
        get => GetValue(FaceBrushProperty);
        set => SetValue(FaceBrushProperty, value);
    }

    public IBrush HubBrush
    {
        get => GetValue(HubBrushProperty);
        set => SetValue(HubBrushProperty, value);
    }

    public IBrush RimBrush
    {
        get => GetValue(RimBrushProperty);
        set => SetValue(RimBrushProperty, value);
    }

    public IBrush ShadowBrush
    {
        get => GetValue(ShadowBrushProperty);
        set => SetValue(ShadowBrushProperty, value);
    }

    public IBrush TickBrush
    {
        get => GetValue(TickBrushProperty);
        set => SetValue(TickBrushProperty, value);
    }

    public IBrush ActiveTickBrush
    {
        get => GetValue(ActiveTickBrushProperty);
        set => SetValue(ActiveTickBrushProperty, value);
    }

    public IBrush MarkerBrush
    {
        get => GetValue(MarkerBrushProperty);
        set => SetValue(MarkerBrushProperty, value);
    }

    public IBrush FocusBrush
    {
        get => GetValue(FocusBrushProperty);
        set => SetValue(FocusBrushProperty, value);
    }

    public IBrush HighlightBrush
    {
        get => GetValue(HighlightBrushProperty);
        set => SetValue(HighlightBrushProperty, value);
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);

        var diameter = Math.Max(0, Math.Min(Bounds.Width, Bounds.Height) - 20);
        var radius = diameter / 2;
        var center = new Point(Bounds.Width / 2, Bounds.Height / 2);
        var depth = Math.Clamp(DepthScale, 0.5, 2.5);

        var rimPen = new Pen(RimBrush, 1.4 + (depth * 0.6));
        var fineRimPen = new Pen(RimBrush, 1);
        var highlightPen = new Pen(HighlightBrush, 1.2);
        var fluteShadowPen = new Pen(OuterBrush, 1.5);
        var fluteHighlightPen = new Pen(HighlightBrush, 0.65);
        var tickPen = new Pen(TickBrush, 2);
        var activeTickPen = new Pen(ActiveTickBrush, 3);
        var markerShadowPen = new Pen(ShadowBrush, 4 + (depth * 1.7));
        var markerPen = new Pen(MarkerBrush, 4);
        var focusPen = new Pen(FocusBrush, 2);

        context.DrawEllipse(
            ShadowBrush,
            null,
            new Point(center.X + (3.4 * depth), center.Y + (4.8 * depth)),
            radius + (1.5 * depth),
            radius + (1.5 * depth));
        context.DrawEllipse(OuterBrush, rimPen, center, radius, radius);
        context.DrawEllipse(RimBrush, null, center, radius - 4, radius - 4);
        context.DrawEllipse(ShadowBrush, null, new Point(center.X + depth, center.Y + depth), radius - 7, radius - 7);
        context.DrawEllipse(
            FaceBrush,
            fineRimPen,
            new Point(center.X - (0.7 * depth), center.Y - (1.25 * depth)),
            radius - 10,
            radius - 10);
        context.DrawEllipse(
            null,
            highlightPen,
            new Point(center.X - (1.4 * depth), center.Y - (2.2 * depth)),
            radius - 13,
            radius - 13);

        for (var flute = 0; flute < 18; flute++)
        {
            var fluteAngle = DegreesToRadians(flute * 20);
            var start = Polar(center, radius * 0.74, fluteAngle);
            var end = Polar(center, radius * 0.86, fluteAngle);
            context.DrawLine(fluteShadowPen, new Point(start.X + depth, start.Y + depth), new Point(end.X + depth, end.Y + depth));
            context.DrawLine(fluteHighlightPen, start, end);
        }

        context.DrawEllipse(
            ShadowBrush,
            null,
            new Point(center.X + (1.6 * depth), center.Y + (2.2 * depth)),
            radius * 0.29,
            radius * 0.29);
        context.DrawEllipse(RimBrush, null, center, radius * 0.28, radius * 0.28);
        context.DrawEllipse(
            HubBrush,
            fineRimPen,
            new Point(center.X - (0.6 * depth), center.Y - (0.7 * depth)),
            radius * 0.23,
            radius * 0.23);
        context.DrawEllipse(
            HighlightBrush,
            null,
            new Point(center.X - (radius * 0.08), center.Y - (radius * 0.09)),
            Math.Max(1, radius * 0.025),
            Math.Max(1, radius * 0.018));

        var range = Math.Max(double.Epsilon, Maximum - Minimum);
        var normalized = Math.Clamp((Value - Minimum) / range, 0d, 1d);

        for (var tick = 0; tick <= 10; tick++)
        {
            var angle = DegreesToRadians(-135 + (tick * 27));
            var start = Polar(center, radius + 3, angle);
            var end = Polar(center, radius + (tick % 5 == 0 ? 11 : 8), angle);
            context.DrawLine(tick / 10d <= normalized ? activeTickPen : tickPen, start, end);
        }

        var markerAngle = DegreesToRadians(-135 + (normalized * 270));
        context.DrawLine(
            markerShadowPen,
            Polar(new Point(center.X + 1, center.Y + 2), radius * 0.32, markerAngle),
            Polar(new Point(center.X + 1, center.Y + 2), radius * 0.73, markerAngle));
        context.DrawLine(
            markerPen,
            Polar(center, radius * 0.32, markerAngle),
            Polar(center, radius * 0.73, markerAngle));

        if (IsFocused)
        {
            context.DrawEllipse(null, focusPen, center, radius + 14, radius + 14);
        }
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);

        var point = e.GetCurrentPoint(this);
        if (!point.Properties.IsLeftButtonPressed)
        {
            return;
        }

        Focus();
        _dragging = true;
        _lastPointerAngle = GetPointerAngle(point.Position);
        _dragAngleRemainder = 0;
        e.Pointer.Capture(this);
        e.Handled = true;
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);

        if (!_dragging)
        {
            return;
        }

        var currentAngle = GetPointerAngle(e.GetPosition(this));
        if (currentAngle is null)
        {
            // Direction is unstable close to the hub. Re-anchor when the
            // pointer leaves it instead of producing a large accidental jump.
            _lastPointerAngle = null;
            e.Handled = true;
            return;
        }

        if (_lastPointerAngle is null)
        {
            _lastPointerAngle = currentAngle;
            e.Handled = true;
            return;
        }

        var angleDelta = CircularDragMath.NormalizeAngularDelta(currentAngle.Value - _lastPointerAngle.Value);
        _lastPointerAngle = currentAngle;

        var increment = e.KeyModifiers.HasFlag(KeyModifiers.Shift) ? FineStep : Step;
        var detents = CircularDragMath.ConsumeDetents(
            ref _dragAngleRemainder,
            angleDelta,
            Maximum - Minimum,
            increment);
        if (detents != 0)
        {
            Adjust(detents, e.KeyModifiers);
        }

        e.Handled = true;
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        _dragging = false;
        _lastPointerAngle = null;
        _dragAngleRemainder = 0;
        e.Pointer.Capture(null);
        e.Handled = true;
    }

    protected override void OnPointerCaptureLost(PointerCaptureLostEventArgs e)
    {
        base.OnPointerCaptureLost(e);
        _dragging = false;
        _lastPointerAngle = null;
        _dragAngleRemainder = 0;
    }

    protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
    {
        base.OnPointerWheelChanged(e);
        Adjust(Math.Sign(e.Delta.Y), e.KeyModifiers);
        e.Handled = true;
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);

        switch (e.Key)
        {
            case Key.Up:
            case Key.Right:
                Adjust(1, e.KeyModifiers);
                break;
            case Key.Down:
            case Key.Left:
                Adjust(-1, e.KeyModifiers);
                break;
            case Key.PageUp:
                Adjust(5, e.KeyModifiers);
                break;
            case Key.PageDown:
                Adjust(-5, e.KeyModifiers);
                break;
            case Key.Home:
                SetKnobValue(double.IsNaN(HomeValue) ? Minimum : HomeValue);
                break;
            case Key.End:
                SetKnobValue(double.IsNaN(EndValue) ? Maximum : EndValue);
                break;
            default:
                return;
        }

        e.Handled = true;
    }

    private void Adjust(int detents, KeyModifiers modifiers)
    {
        if (detents == 0)
        {
            return;
        }

        var increment = modifiers.HasFlag(KeyModifiers.Shift) ? FineStep : Step;
        SetKnobValue(Value + (increment * detents));
    }

    private void SetKnobValue(double value) =>
        SetCurrentValue(ValueProperty, Math.Clamp(value, Minimum, Maximum));

    private double? GetPointerAngle(Point position)
    {
        var center = new Point(Bounds.Width / 2, Bounds.Height / 2);
        var horizontal = position.X - center.X;
        var vertical = center.Y - position.Y;
        var distance = Math.Sqrt((horizontal * horizontal) + (vertical * vertical));
        var hubDeadZone = Math.Max(8, Math.Min(Bounds.Width, Bounds.Height) * 0.13);

        return distance < hubDeadZone
            ? null
            : Math.Atan2(horizontal, vertical);
    }

    private static Point Polar(Point center, double radius, double angle) =>
        new(center.X + (Math.Sin(angle) * radius), center.Y - (Math.Cos(angle) * radius));

    private static double DegreesToRadians(double degrees) => degrees * Math.PI / 180d;
}
