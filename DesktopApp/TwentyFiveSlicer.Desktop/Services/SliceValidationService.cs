using TwentyFiveSlicer.Desktop.Models;

namespace TwentyFiveSlicer.Desktop.Services;

public static class SliceValidationService
{
    public static IReadOnlyList<SliceValidationMessage> Validate(
        double sourceWidth,
        double sourceHeight,
        double targetWidth,
        double targetHeight,
        TwentyFiveSliceData sliceData)
    {
        var messages = new List<SliceValidationMessage>();

        if (sourceWidth <= 0d || sourceHeight <= 0d)
        {
            messages.Add(new SliceValidationMessage(SliceValidationSeverity.Info, "Load an image to enable validation."));
            return messages;
        }

        double[] vertical = BuildSizes(sliceData.VerticalBorders, sourceWidth);
        double[] horizontal = BuildSizes(sliceData.HorizontalBorders, sourceHeight);
        double fixedColumns = vertical[0] + vertical[2] + vertical[4];
        double fixedRows = horizontal[0] + horizontal[2] + horizontal[4];

        if (fixedColumns > targetWidth)
        {
            messages.Add(new SliceValidationMessage(SliceValidationSeverity.Warning, $"The fixed columns need {fixedColumns:0}px, more than the {targetWidth:0}px target width."));
        }

        if (fixedRows > targetHeight)
        {
            messages.Add(new SliceValidationMessage(SliceValidationSeverity.Warning, $"The fixed rows need {fixedRows:0}px, more than the {targetHeight:0}px target height."));
        }

        if (vertical.Any(size => size > 0d && size < 2d) || horizontal.Any(size => size > 0d && size < 2d))
        {
            messages.Add(new SliceValidationMessage(SliceValidationSeverity.Warning, "One or more slice regions is thinner than 2 pixels."));
        }

        if (messages.Count == 0)
        {
            messages.Add(new SliceValidationMessage(SliceValidationSeverity.Info, "No slice warnings for this target."));
        }

        return messages;
    }

    private static double[] BuildSizes(IReadOnlyList<double> borders, double totalSize)
    {
        double[] normalized = TwentyFiveSliceData.NormalizeAxis(borders);
        double[] stops = [0d, normalized[0], normalized[1], normalized[2], normalized[3], 100d];
        return
        [
            (stops[1] - stops[0]) * totalSize / 100d,
            (stops[2] - stops[1]) * totalSize / 100d,
            (stops[3] - stops[2]) * totalSize / 100d,
            (stops[4] - stops[3]) * totalSize / 100d,
            (stops[5] - stops[4]) * totalSize / 100d
        ];
    }
}
