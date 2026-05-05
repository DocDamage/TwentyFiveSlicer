namespace TwentyFiveSlicer.Desktop.Models;

public sealed record SliceChatContext(
    TwentyFiveSliceData CurrentSliceData,
    double SourceWidth,
    double SourceHeight,
    double TargetWidth,
    double TargetHeight);
