namespace TwentyFiveSlicer.Desktop.Models;

public readonly record struct FloatRect(double X, double Y, double Width, double Height)
{
    public bool IsEmpty => Width <= 0d || Height <= 0d;
}