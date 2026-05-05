namespace TwentyFiveSlicer.Desktop.Models;

public sealed record SliceSuggestionReview(
    TwentyFiveSliceData CurrentSliceData,
    TwentyFiveSliceData ProposedSliceData,
    double Score,
    bool SafeToApply,
    IReadOnlyList<double> VerticalPixelDeltas,
    IReadOnlyList<double> HorizontalPixelDeltas,
    IReadOnlyList<SliceValidationMessage> CurrentValidationMessages,
    IReadOnlyList<SliceValidationMessage> ProposedValidationMessages,
    IReadOnlyList<string> Improvements,
    IReadOnlyList<string> Risks);
