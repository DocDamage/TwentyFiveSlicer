namespace TwentyFiveSlicer.Desktop.Models;

public sealed record SliceChatResponse(
    string AssistantMessage,
    TwentyFiveSliceData? ProposedSliceData,
    SliceSuggestionReview? Review);
