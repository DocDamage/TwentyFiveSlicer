using TwentyFiveSlicer.Desktop.Models;

namespace TwentyFiveSlicer.Desktop.Services;

public static class SliceSuggestionReviewService
{
    public static SliceSuggestionReview Review(
        TwentyFiveSliceData current,
        TwentyFiveSliceData proposed,
        double sourceWidth,
        double sourceHeight,
        double targetWidth,
        double targetHeight)
    {
        TwentyFiveSliceData normalizedCurrent = new(current.VerticalBorders, current.HorizontalBorders);
        TwentyFiveSliceData normalizedProposed = new(proposed.VerticalBorders, proposed.HorizontalBorders);

        IReadOnlyList<SliceValidationMessage> currentMessages = SliceValidationService.Validate(sourceWidth, sourceHeight, targetWidth, targetHeight, normalizedCurrent);
        IReadOnlyList<SliceValidationMessage> proposedMessages = SliceValidationService.Validate(sourceWidth, sourceHeight, targetWidth, targetHeight, normalizedProposed);
        int currentWarnings = CountWarnings(currentMessages);
        int proposedWarnings = CountWarnings(proposedMessages);
        IReadOnlyList<double> verticalDeltas = BuildPixelDeltas(normalizedCurrent.VerticalBorders, normalizedProposed.VerticalBorders, sourceWidth);
        IReadOnlyList<double> horizontalDeltas = BuildPixelDeltas(normalizedCurrent.HorizontalBorders, normalizedProposed.HorizontalBorders, sourceHeight);
        double maxDelta = verticalDeltas.Concat(horizontalDeltas).Select(Math.Abs).DefaultIfEmpty(0d).Max();
        double symmetryPenalty = GetSymmetryPenalty(normalizedProposed);

        var improvements = new List<string>();
        var risks = new List<string>();

        if (proposedWarnings < currentWarnings)
        {
            improvements.Add($"Reduced validation warnings from {currentWarnings} to {proposedWarnings}.");
        }
        else if (proposedWarnings == 0)
        {
            improvements.Add("Keeps the target free of validation warnings.");
        }

        if (proposedWarnings > currentWarnings)
        {
            risks.Add($"Increases validation warnings from {currentWarnings} to {proposedWarnings}.");
        }

        foreach (SliceValidationMessage message in proposedMessages.Where(message => message.Severity == SliceValidationSeverity.Warning))
        {
            risks.Add(message.Text);
        }

        if (maxDelta > Math.Max(sourceWidth, sourceHeight) * 0.35d)
        {
            risks.Add($"Moves at least one guide by {maxDelta:0}px, which is a large change for this source asset.");
        }

        if (symmetryPenalty > 0.08d)
        {
            risks.Add("Leaves noticeably uneven opposite fixed bands.");
        }
        else
        {
            improvements.Add("Keeps opposite fixed bands balanced.");
        }

        double score = 0.82d
            + ((currentWarnings - proposedWarnings) * 0.14d)
            - (proposedWarnings * 0.22d)
            - symmetryPenalty
            - GetLargeMovePenalty(maxDelta, sourceWidth, sourceHeight);
        score = Math.Clamp(score, 0d, 1d);
        bool safeToApply = proposedWarnings == 0 && score >= 0.78d && risks.Count == 0;

        return new SliceSuggestionReview(
            normalizedCurrent,
            normalizedProposed,
            Math.Round(score, 3),
            safeToApply,
            verticalDeltas,
            horizontalDeltas,
            currentMessages,
            proposedMessages,
            improvements,
            risks);
    }

    private static IReadOnlyList<double> BuildPixelDeltas(IReadOnlyList<double> current, IReadOnlyList<double> proposed, double totalPixels)
    {
        double[] normalizedCurrent = TwentyFiveSliceData.NormalizeAxis(current);
        double[] normalizedProposed = TwentyFiveSliceData.NormalizeAxis(proposed);
        var deltas = new double[4];
        for (int index = 0; index < deltas.Length; index++)
        {
            deltas[index] = Math.Round((normalizedProposed[index] - normalizedCurrent[index]) * totalPixels / 100d, 2);
        }

        return deltas;
    }

    private static int CountWarnings(IReadOnlyList<SliceValidationMessage> messages)
    {
        return messages.Count(message => message.Severity == SliceValidationSeverity.Warning);
    }

    private static double GetSymmetryPenalty(TwentyFiveSliceData data)
    {
        double leftBand = data.VerticalBorders[0];
        double rightBand = 100d - data.VerticalBorders[3];
        double topBand = data.HorizontalBorders[0];
        double bottomBand = 100d - data.HorizontalBorders[3];
        return Math.Min(0.2d, (Math.Abs(leftBand - rightBand) + Math.Abs(topBand - bottomBand)) / 240d);
    }

    private static double GetLargeMovePenalty(double maxDelta, double sourceWidth, double sourceHeight)
    {
        double reference = Math.Max(1d, Math.Max(sourceWidth, sourceHeight));
        double ratio = maxDelta / reference;
        return ratio <= 0.15d ? 0d : Math.Min(0.18d, (ratio - 0.15d) * 0.6d);
    }
}
