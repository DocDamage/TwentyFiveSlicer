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
        SliceSegmentDefinition[] xSegments = TwentyFiveSliceData.NormalizeSegments(sliceData.XSegments, vertical.Length);
        SliceSegmentDefinition[] ySegments = TwentyFiveSliceData.NormalizeSegments(sliceData.YSegments, horizontal.Length);
        int columns = vertical.Length;
        int rows = horizontal.Length;
        double fixedColumns = GetFixedSize(vertical, xSegments);
        double fixedRows = GetFixedSize(horizontal, ySegments);

        if (columns > TwentyFiveSliceData.MaxSegmentsPerAxis || rows > TwentyFiveSliceData.MaxSegmentsPerAxis)
        {
            messages.Add(new SliceValidationMessage(SliceValidationSeverity.Warning, $"The grid is {columns} x {rows}, above the {TwentyFiveSliceData.MaxSegmentsPerAxis} x {TwentyFiveSliceData.MaxSegmentsPerAxis} safety cap."));
        }
        else if (columns != 5 || rows != 5)
        {
            messages.Add(new SliceValidationMessage(SliceValidationSeverity.Info, $"Variable grid is {columns} x {rows} cells."));
        }

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

        int hiddenColumns = xSegments.Count(segment => segment.Mode == SliceSegmentMode.Hidden);
        int hiddenRows = ySegments.Count(segment => segment.Mode == SliceSegmentMode.Hidden);
        if (hiddenColumns > 0 || hiddenRows > 0)
        {
            messages.Add(new SliceValidationMessage(SliceValidationSeverity.Info, $"Hidden segments: {hiddenColumns} columns, {hiddenRows} rows."));
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
        double[] stops = [0d, .. normalized, 100d];
        var sizes = new double[stops.Length - 1];
        for (int index = 0; index < sizes.Length; index++)
        {
            sizes[index] = (stops[index + 1] - stops[index]) * totalSize / 100d;
        }

        return sizes;
    }

    private static double GetFixedSize(IReadOnlyList<double> sizes, IReadOnlyList<SliceSegmentDefinition> segments)
    {
        double total = 0d;
        for (int index = 0; index < sizes.Count; index++)
        {
            if (segments[index].Mode == SliceSegmentMode.Fixed)
            {
                total += sizes[index];
            }
        }

        return total;
    }
}
