using TwentyFiveSlicer.Desktop.Models;

namespace TwentyFiveSlicer.Desktop.Services;

public static class TwentyFiveSliceLayoutCalculator
{
    public static IReadOnlyList<SliceRegion> CalculateRegions(
        double sourceWidth,
        double sourceHeight,
        double targetWidth,
        double targetHeight,
        TwentyFiveSliceData sliceData,
        bool flipX = false,
        bool flipY = false)
    {
        if (sourceWidth <= 0d || sourceHeight <= 0d)
        {
            return Array.Empty<SliceRegion>();
        }

        TwentyFiveSliceData.EnsureWithinGridCap(sliceData);

        double[] xBordersPercent = BuildAxisStops(sliceData.XGuidesPercent);
        double[] yBordersPercent = BuildAxisStops(sliceData.YGuidesPercent);

        double[] sourceWidths = GetOriginalSizes(xBordersPercent, sourceWidth);
        double[] sourceHeights = GetOriginalSizes(yBordersPercent, sourceHeight);
        SliceSegmentDefinition[] xSegments = TwentyFiveSliceData.NormalizeSegments(sliceData.XSegments, sourceWidths.Length);
        SliceSegmentDefinition[] ySegments = TwentyFiveSliceData.NormalizeSegments(sliceData.YSegments, sourceHeights.Length);
        double[] targetWidths = GetAdjustedSizes(Math.Max(0d, targetWidth), sourceWidths, xSegments);
        double[] targetHeights = GetAdjustedSizes(Math.Max(0d, targetHeight), sourceHeights, ySegments);

        double[] sourceXPositions = GetPositions(0d, sourceWidths);
        double[] sourceYPositions = GetPositions(0d, sourceHeights);
        double[] targetXPositions = GetPositions(0d, targetWidths);
        double[] targetYPositions = GetPositions(0d, targetHeights);

        var regions = new List<SliceRegion>(sourceWidths.Length * sourceHeights.Length);
        for (int row = 0; row < sourceHeights.Length; row++)
        {
            if (ySegments[row].Mode == SliceSegmentMode.Hidden)
            {
                continue;
            }

            int sourceRow = flipY ? sourceHeights.Length - 1 - row : row;
            for (int column = 0; column < sourceWidths.Length; column++)
            {
                if (xSegments[column].Mode == SliceSegmentMode.Hidden)
                {
                    continue;
                }

                int sourceColumn = flipX ? sourceWidths.Length - 1 - column : column;

                var sourceRect = new FloatRect(
                    sourceXPositions[sourceColumn],
                    sourceYPositions[sourceRow],
                    sourceWidths[sourceColumn],
                    sourceHeights[sourceRow]);

                var destinationRect = new FloatRect(
                    targetXPositions[column],
                    targetYPositions[row],
                    targetWidths[column],
                    targetHeights[row]);

                if (sourceRect.IsEmpty || destinationRect.IsEmpty)
                {
                    continue;
                }

                regions.Add(new SliceRegion(sourceRect, destinationRect, column, row));
            }
        }

        return regions;
    }

    private static double[] BuildAxisStops(IReadOnlyList<double> guides)
    {
        double[] normalized = TwentyFiveSliceData.NormalizeAxis(guides);
        double[] stops = new double[normalized.Length + 2];
        stops[0] = 0d;
        for (int index = 0; index < normalized.Length; index++)
        {
            stops[index + 1] = normalized[index];
        }

        stops[^1] = 100d;
        return stops;
    }

    private static double[] GetOriginalSizes(IReadOnlyList<double> bordersPercent, double totalSize)
    {
        var sizes = new double[bordersPercent.Count - 1];
        for (int index = 0; index < sizes.Length; index++)
        {
            sizes[index] = (bordersPercent[index + 1] - bordersPercent[index]) * totalSize / 100d;
        }

        return sizes;
    }

    private static double[] GetAdjustedSizes(double totalSize, IReadOnlyList<double> originalSizes, IReadOnlyList<SliceSegmentDefinition> segments)
    {
        double totalFixedSize = 0d;
        double totalStretchableSourceSize = 0d;

        for (int index = 0; index < originalSizes.Count; index++)
        {
            SliceSegmentMode mode = segments[index].Mode;
            if (mode == SliceSegmentMode.Hidden)
            {
                continue;
            }

            if (mode == SliceSegmentMode.Fixed)
            {
                totalFixedSize += originalSizes[index];
            }
            else
            {
                totalStretchableSourceSize += originalSizes[index];
            }
        }

        var adjustedSizes = new double[originalSizes.Count];
        if (totalSize < totalFixedSize && totalFixedSize > 0d)
        {
            double scaleRatio = totalSize / totalFixedSize;
            for (int index = 0; index < originalSizes.Count; index++)
            {
                adjustedSizes[index] = segments[index].Mode == SliceSegmentMode.Fixed ? originalSizes[index] * scaleRatio : 0d;
            }

            return adjustedSizes;
        }

        double totalStretchableTargetSize = Math.Max(0d, totalSize - totalFixedSize);
        for (int index = 0; index < originalSizes.Count; index++)
        {
            SliceSegmentMode mode = segments[index].Mode;
            if (mode == SliceSegmentMode.Hidden)
            {
                adjustedSizes[index] = 0d;
                continue;
            }

            if (mode == SliceSegmentMode.Fixed)
            {
                adjustedSizes[index] = originalSizes[index];
                continue;
            }

            adjustedSizes[index] = totalStretchableSourceSize <= 0d
                ? 0d
                : totalStretchableTargetSize * (originalSizes[index] / totalStretchableSourceSize);
        }

        return adjustedSizes;
    }

    private static double[] GetPositions(double start, IReadOnlyList<double> sizes)
    {
        var positions = new double[sizes.Count + 1];
        positions[0] = start;
        for (int index = 1; index < positions.Length; index++)
        {
            positions[index] = positions[index - 1] + sizes[index - 1];
        }

        return positions;
    }
}
