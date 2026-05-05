namespace TwentyFiveSlicer.Desktop.Models;

public sealed record SliceEditorState(double[] VerticalBorders, double[] HorizontalBorders, double TargetWidth, double TargetHeight)
{
    public SliceEditorState Snapshot()
    {
        return new SliceEditorState(
            (double[])VerticalBorders.Clone(),
            (double[])HorizontalBorders.Clone(),
            TargetWidth,
            TargetHeight);
    }
}
