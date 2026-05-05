namespace TwentyFiveSlicer.Desktop.Models;

public sealed class SliceGuideEditEventArgs : EventArgs
{
    public SliceGuideEditEventArgs(bool isVertical, int index, double percent, bool isFinal)
    {
        IsVertical = isVertical;
        Index = index;
        Percent = percent;
        IsFinal = isFinal;
    }

    public bool IsVertical { get; }

    public int Index { get; }

    public double Percent { get; }

    public bool IsFinal { get; }
}
