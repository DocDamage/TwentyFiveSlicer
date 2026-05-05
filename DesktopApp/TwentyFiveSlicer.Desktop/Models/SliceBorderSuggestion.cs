namespace TwentyFiveSlicer.Desktop.Models;

public sealed record SliceBorderSuggestion(TwentyFiveSliceData SliceData, double Confidence, IReadOnlyList<string> Reasons);
