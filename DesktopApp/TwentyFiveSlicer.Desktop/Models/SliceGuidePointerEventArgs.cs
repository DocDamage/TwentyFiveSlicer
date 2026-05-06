namespace TwentyFiveSlicer.Desktop.Models;

public sealed class SliceGuidePointerEventArgs : EventArgs
{
    public SliceGuidePointerEventArgs(double xPercent, double yPercent)
    {
        XPercent = xPercent;
        YPercent = yPercent;
    }

    public double XPercent { get; }

    public double YPercent { get; }
}
