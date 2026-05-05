using TwentyFiveSlicer.Desktop.Models;
using System.Windows.Media.Imaging;

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
        return Review(current, proposed, sourceWidth, sourceHeight, targetWidth, targetHeight, imageDerivedSliceData: null);
    }

    public static SliceSuggestionReview Review(
        TwentyFiveSliceData current,
        TwentyFiveSliceData proposed,
        BitmapSource image,
        double targetWidth,
        double targetHeight)
    {
        SliceBorderSuggestion imageSuggestion = ImageBorderSuggestionService.SuggestBordersDetailed(image);
        return Review(
            current,
            proposed,
            image.PixelWidth,
            image.PixelHeight,
            targetWidth,
            targetHeight,
            imageSuggestion.SliceData);
    }

    private static SliceSuggestionReview Review(
        TwentyFiveSliceData current,
        TwentyFiveSliceData proposed,
        double sourceWidth,
        double sourceHeight,
        double targetWidth,
        double targetHeight,
        TwentyFiveSliceData? imageDerivedSliceData)
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

        double imageFitPenalty = 0d;
        if (imageDerivedSliceData is not null)
        {
            imageFitPenalty = GetImageFitPenalty(normalizedProposed, imageDerivedSliceData, sourceWidth, sourceHeight, improvements, risks);
        }

        double score = 0.82d
            + ((currentWarnings - proposedWarnings) * 0.14d)
            - (proposedWarnings * 0.22d)
            - symmetryPenalty
            - GetLargeMovePenalty(maxDelta, sourceWidth, sourceHeight)
            - imageFitPenalty;
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

    private static double GetImageFitPenalty(
        TwentyFiveSliceData proposed,
        TwentyFiveSliceData imageDerived,
        double sourceWidth,
        double sourceHeight,
        List<string> improvements,
        List<string> risks)
    {
        double leftDelta = PercentToPixels(Math.Abs(proposed.VerticalBorders[0] - imageDerived.VerticalBorders[0]), sourceWidth);
        double rightDelta = PercentToPixels(Math.Abs(proposed.VerticalBorders[3] - imageDerived.VerticalBorders[3]), sourceWidth);
        double topDelta = PercentToPixels(Math.Abs(proposed.HorizontalBorders[0] - imageDerived.HorizontalBorders[0]), sourceHeight);
        double bottomDelta = PercentToPixels(Math.Abs(proposed.HorizontalBorders[3] - imageDerived.HorizontalBorders[3]), sourceHeight);
        double maxDelta = new[] { leftDelta, rightDelta, topDelta, bottomDelta }.Max();
        double tolerance = Math.Max(2d, Math.Max(sourceWidth, sourceHeight) * 0.025d);

        if (maxDelta <= tolerance)
        {
            improvements.Add("Outer guides align with detected opaque content bounds.");
            return 0d;
        }

        risks.Add($"Outer guides differ from detected opaque content bounds by up to {maxDelta:0}px.");
        return Math.Min(0.32d, (maxDelta - tolerance) / Math.Max(1d, Math.Max(sourceWidth, sourceHeight)) * 1.5d);
    }

    private static double PercentToPixels(double percent, double totalPixels)
    {
        return percent * Math.Max(1d, totalPixels) / 100d;
    }
}
