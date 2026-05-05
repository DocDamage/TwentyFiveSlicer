using TwentyFiveSlicer.Desktop.Models;
using System.Windows.Media.Imaging;

namespace TwentyFiveSlicer.Desktop.Services;

public sealed class SliceChatService
{
    private readonly SliceAssistantService _assistant = new();

    public static SliceChatCommand ParseCommand(string userMessage)
    {
        string normalized = userMessage.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return SliceChatCommand.None;
        }

        if ((normalized.Contains("preview", StringComparison.Ordinal) || normalized.Contains("show", StringComparison.Ordinal)) &&
            (normalized.Contains("it", StringComparison.Ordinal) || normalized.Contains("proposal", StringComparison.Ordinal) || normalized.Contains("suggestion", StringComparison.Ordinal)))
        {
            return SliceChatCommand.PreviewProposal;
        }

        if ((normalized.Contains("apply", StringComparison.Ordinal) || normalized.Contains("accept", StringComparison.Ordinal) || normalized.Contains("use it", StringComparison.Ordinal)) &&
            (normalized.Contains("it", StringComparison.Ordinal) || normalized.Contains("proposal", StringComparison.Ordinal) || normalized.Contains("suggestion", StringComparison.Ordinal) || normalized.Contains("that", StringComparison.Ordinal)))
        {
            return SliceChatCommand.ApplyProposal;
        }

        if (normalized.Contains("reject", StringComparison.Ordinal) ||
            normalized.Contains("discard", StringComparison.Ordinal) ||
            normalized.Contains("cancel", StringComparison.Ordinal))
        {
            return SliceChatCommand.RejectProposal;
        }

        if (normalized.Contains("risk", StringComparison.Ordinal) ||
            normalized.Contains("why", StringComparison.Ordinal) ||
            normalized.Contains("explain", StringComparison.Ordinal))
        {
            return SliceChatCommand.ExplainRisk;
        }

        if (normalized.Contains("safer", StringComparison.Ordinal) ||
            normalized.Contains("less risky", StringComparison.Ordinal) ||
            normalized.Contains("safer version", StringComparison.Ordinal))
        {
            return SliceChatCommand.TrySaferProposal;
        }

        return SliceChatCommand.None;
    }

    public SliceChatResponse Send(string userMessage, SliceChatContext context)
    {
        string prompt = userMessage.Trim();
        if (string.IsNullOrWhiteSpace(prompt))
        {
            return new SliceChatResponse("Tell me what you want to change about the slice.", null, null);
        }

        if (IsAnalysisPrompt(prompt))
        {
            SliceAssistantAnalysis analysis = _assistant.AnalyzeDetailed(
                context.SourceWidth,
                context.SourceHeight,
                context.TargetWidth,
                context.TargetHeight,
                context.CurrentSliceData);
            return new SliceChatResponse(AssistantReportFormatter.FormatAnalysis(analysis), null, null);
        }

        SliceAssistantResult result = _assistant.Apply(prompt, context.CurrentSliceData);
        if (!result.Applied)
        {
            return new SliceChatResponse(result.Message, null, null);
        }

        SliceSuggestionReview review = SliceSuggestionReviewService.Review(
            context.CurrentSliceData,
            result.SliceData,
            context.SourceWidth,
            context.SourceHeight,
            context.TargetWidth,
            context.TargetHeight);
        string status = review.SafeToApply
            ? "The proposal passed deterministic review and is ready to apply."
            : "The proposal needs review before applying.";
        string message = $"{result.Message} {status} Score: {review.Score:P0}.";

        if (review.Risks.Count > 0)
        {
            message += $" Risk: {review.Risks[0]}";
        }

        return new SliceChatResponse(message, result.SliceData, review);
    }

    public SliceChatResponse FromCloudAdvice(string advice, SliceChatContext context, string providerName, BitmapSource? image = null)
    {
        if (!CloudAiAdviceParser.TryParseSliceData(advice, out TwentyFiveSliceData? proposed) || proposed is null)
        {
            return new SliceChatResponse($"{providerName}: {advice}", null, null);
        }

        SliceSuggestionReview review = image is null
            ? SliceSuggestionReviewService.Review(
                context.CurrentSliceData,
                proposed,
                context.SourceWidth,
                context.SourceHeight,
                context.TargetWidth,
                context.TargetHeight)
            : SliceSuggestionReviewService.Review(
                context.CurrentSliceData,
                proposed,
                image,
                context.TargetWidth,
                context.TargetHeight);
        string status = review.SafeToApply
            ? "The cloud proposal passed deterministic review and is ready to apply."
            : "The cloud proposal needs review before applying.";
        string message = $"{providerName}: I found exact border JSON. {status} Score: {review.Score:P0}.";

        if (review.Risks.Count > 0)
        {
            message += $" Risk: {review.Risks[0]}";
        }

        return new SliceChatResponse(message, proposed, review);
    }

    private static bool IsAnalysisPrompt(string prompt)
    {
        string normalized = prompt.ToLowerInvariant();
        return normalized.Contains("what do you think", StringComparison.Ordinal) ||
               normalized.Contains("analyze", StringComparison.Ordinal) ||
               normalized.Contains("explain", StringComparison.Ordinal) ||
               normalized.Contains("why", StringComparison.Ordinal) ||
               normalized.Contains("risk", StringComparison.Ordinal);
    }
}
