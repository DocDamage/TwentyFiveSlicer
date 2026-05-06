namespace TwentyFiveSlicer.Desktop.Models;

public sealed class SliceGuideSelectionEventArgs : EventArgs
{
    public SliceGuideSelectionEventArgs(bool isVertical, int index)
    {
        IsVertical = isVertical;
        Index = index;
    }

    public bool IsVertical { get; }

    public int Index { get; }
}
