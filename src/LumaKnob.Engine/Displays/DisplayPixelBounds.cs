namespace LumaKnob.Engine.Displays;

public readonly record struct DisplayPixelBounds(int X, int Y, int Width, int Height)
{
    public bool IsEmpty => Width <= 0 || Height <= 0;
}
