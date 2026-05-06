namespace TwentyFiveSlicer.Desktop.Models;

public sealed record SliceEditorState(
    double[] VerticalBorders,
    double[] HorizontalBorders,
    double TargetWidth,
    double TargetHeight,
    SliceSegmentDefinition[]? XSegments = null,
    SliceSegmentDefinition[]? YSegments = null,
    SliceCellOverrideDefinition[]? CellOverrides = null)
{
    public SliceEditorState Snapshot()
    {
        return new SliceEditorState(
            (double[])VerticalBorders.Clone(),
            (double[])HorizontalBorders.Clone(),
            TargetWidth,
            TargetHeight,
            XSegments?.ToArray(),
            YSegments?.ToArray(),
            CellOverrides?.Select(overrideDefinition => overrideDefinition.Clone()).ToArray());
    }
}
