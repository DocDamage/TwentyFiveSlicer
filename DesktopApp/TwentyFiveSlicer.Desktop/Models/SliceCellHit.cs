namespace TwentyFiveSlicer.Desktop.Models;

public sealed record SliceCellHit(int Column, int Row)
{
    public static SliceCellHit None { get; } = new(-1, -1);

    public bool HasValue => Column >= 0 && Row >= 0;
}