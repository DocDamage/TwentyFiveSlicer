namespace TwentyFiveSlicer.Desktop.Models;

public sealed record SliceCandidateSuggestion(
    string Name,
    TwentyFiveSliceData SliceData,
    double Score,
    IReadOnlyList<string> Reasons);
