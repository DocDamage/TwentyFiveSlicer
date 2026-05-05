namespace TwentyFiveSlicer.Desktop.Models;

public sealed record IdeAssistantInput(
    TwentyFiveSliceData SliceData,
    double SourceWidth,
    double SourceHeight,
    double TargetWidth,
    double TargetHeight);
