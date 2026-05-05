using TwentyFiveSlicer.Desktop.Models;

namespace TwentyFiveSlicer.Desktop.Services;

public static class TwentyFiveSliceLayoutCalculator
{
    private static readonly bool[] FixedColumns = { true, false, true, false, true };
    private static readonly bool[] FixedRows = { true, false, true, false, true };

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

        double[] xBordersPercent = BuildAxisBorders(sliceData.VerticalBorders);
        double[] yBordersPercent = BuildAxisBorders(sliceData.HorizontalBorders);

        double[] sourceWidths = GetOriginalSizes(xBordersPercent, sourceWidth);
        double[] sourceHeights = GetOriginalSizes(yBordersPercent, sourceHeight);
        double[] targetWidths = GetAdjustedSizes(Math.Max(0d, targetWidth), sourceWidths, FixedColumns);
        double[] targetHeights = GetAdjustedSizes(Math.Max(0d, targetHeight), sourceHeights, FixedRows);

        double[] sourceXPositions = GetPositions(0d, sourceWidths);
        double[] sourceYPositions = GetPositions(0d, sourceHeights);
        double[] targetXPositions = GetPositions(0d, targetWidths);
        double[] targetYPositions = GetPositions(0d, targetHeights);

        var regions = new List<SliceRegion>(25);
        for (int row = 0; row < 5; row++)
        {
            int sourceRow = flipY ? 4 - row : row;
            for (int column = 0; column < 5; column++)
            {
                int sourceColumn = flipX ? 4 - column : column;

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

    private static double[] BuildAxisBorders(IReadOnlyList<double> borders)
    {
        return
        [
            0d,
            borders.Count > 0 ? borders[0] : 20d,
            borders.Count > 1 ? borders[1] : 40d,
            borders.Count > 2 ? borders[2] : 60d,
            borders.Count > 3 ? borders[3] : 80d,
            100d
        ];
    }

    private static double[] GetOriginalSizes(IReadOnlyList<double> bordersPercent, double totalSize)
    {
        return
        [
            (bordersPercent[1] - bordersPercent[0]) * totalSize / 100d,
            (bordersPercent[2] - bordersPercent[1]) * totalSize / 100d,
            (bordersPercent[3] - bordersPercent[2]) * totalSize / 100d,
            (bordersPercent[4] - bordersPercent[3]) * totalSize / 100d,
            (bordersPercent[5] - bordersPercent[4]) * totalSize / 100d
        ];
    }

    private static double[] GetAdjustedSizes(double totalSize, IReadOnlyList<double> originalSizes, IReadOnlyList<bool> fixedSizes)
    {
        double totalFixedSize = 0d;
        double totalStretchableSourceSize = 0d;

        for (int index = 0; index < 5; index++)
        {
            if (fixedSizes[index])
            {
                totalFixedSize += originalSizes[index];
            }
            else
            {
                totalStretchableSourceSize += originalSizes[index];
            }
        }

        var adjustedSizes = new double[5];
        if (totalSize < totalFixedSize && totalFixedSize > 0d)
        {
            double scaleRatio = totalSize / totalFixedSize;
            for (int index = 0; index < 5; index++)
            {
                adjustedSizes[index] = fixedSizes[index] ? originalSizes[index] * scaleRatio : 0d;
            }

            return adjustedSizes;
        }

        double totalStretchableTargetSize = Math.Max(0d, totalSize - totalFixedSize);
        for (int index = 0; index < 5; index++)
        {
            if (fixedSizes[index])
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
        var positions = new double[6];
        positions[0] = start;
        for (int index = 1; index < positions.Length; index++)
        {
            positions[index] = positions[index - 1] + sizes[index - 1];
        }

        return positions;
    }
}