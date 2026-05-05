namespace TwentyFiveSlicer.Desktop.Models;

public sealed record SliceAssistantAnalysis(
    SliceAssetKind AssetKind,
    double Confidence,
    string Summary,
    IReadOnlyList<string> Observations,
    IReadOnlyList<SliceValidationMessage> ValidationMessages,
    IReadOnlyList<SliceAssistantAction> RecommendedActions);
