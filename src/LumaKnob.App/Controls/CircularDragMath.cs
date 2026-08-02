namespace LumaKnob.App.Controls;

internal static class CircularDragMath
{
    private const double ActiveArcRadians = Math.PI * 1.5;

    public static double NormalizeAngularDelta(double angle)
    {
        if (angle > Math.PI)
        {
            return angle - (Math.PI * 2);
        }

        if (angle < -Math.PI)
        {
            return angle + (Math.PI * 2);
        }

        return angle;
    }

    public static int ConsumeDetents(
        ref double remainder,
        double angleDelta,
        double valueRange,
        double increment)
    {
        if (!double.IsFinite(angleDelta) ||
            !double.IsFinite(valueRange) ||
            valueRange <= 0 ||
            !double.IsFinite(increment) ||
            increment <= 0)
        {
            return 0;
        }

        remainder += angleDelta;
        var radiansPerDetent = ActiveArcRadians * increment / valueRange;
        var detents = (int)(remainder / radiansPerDetent);
        remainder -= detents * radiansPerDetent;
        return detents;
    }
}
