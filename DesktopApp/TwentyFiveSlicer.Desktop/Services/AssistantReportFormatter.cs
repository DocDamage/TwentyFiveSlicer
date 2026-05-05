using System.Text;
using TwentyFiveSlicer.Desktop.Models;

namespace TwentyFiveSlicer.Desktop.Services;

public static class AssistantReportFormatter
{
    public static string FormatAnalysis(SliceAssistantAnalysis analysis)
    {
        var builder = new StringBuilder();
        builder.AppendLine(analysis.Summary);
        foreach (string observation in analysis.Observations)
        {
            builder.AppendLine($"- {observation}");
        }

        builder.AppendLine("Recommended actions:");
        foreach (SliceAssistantAction action in analysis.RecommendedActions)
        {
            builder.AppendLine($"- {action.Label}: {action.Reason}");
        }

        return builder.ToString().Trim();
    }

    public static string FormatBorderSuggestion(SliceBorderSuggestion suggestion)
    {
        var builder = new StringBuilder();
        builder.AppendLine($"Suggested borders with {suggestion.Confidence:P0} confidence.");
        foreach (string reason in suggestion.Reasons)
        {
            builder.AppendLine($"- {reason}");
        }

        return builder.ToString().Trim();
    }

    public static string FormatCandidateSuggestions(IReadOnlyList<SliceCandidateSuggestion> candidates)
    {
        if (candidates.Count == 0)
        {
            return "No candidate suggestions were generated.";
        }

        var builder = new StringBuilder();
        SliceCandidateSuggestion best = candidates[0];
        builder.AppendLine($"Applied {best.Name} ({best.Score:P0}).");
        foreach (string reason in best.Reasons)
        {
            builder.AppendLine($"- {reason}");
        }

        builder.AppendLine("Other candidates:");
        foreach (SliceCandidateSuggestion candidate in candidates.Skip(1).Take(4))
        {
            builder.AppendLine($"- {candidate.Name}: {candidate.Score:P0}");
        }

        return builder.ToString().Trim();
    }
}
