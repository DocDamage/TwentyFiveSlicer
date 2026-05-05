namespace TwentyFiveSlicer.Desktop.Models;

public sealed record SliceGuideHit(SliceGuideHitKind Kind, int VerticalIndex, int HorizontalIndex)
{
    public static SliceGuideHit None { get; } = new(SliceGuideHitKind.None, -1, -1);
}
