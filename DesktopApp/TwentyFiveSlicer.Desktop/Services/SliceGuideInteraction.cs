using System.Windows;
using TwentyFiveSlicer.Desktop.Models;

namespace TwentyFiveSlicer.Desktop.Services;

public static class SliceGuideInteraction
{
    public static SliceGuideHit HitTest(Rect previewRect, TwentyFiveSliceData sliceData, Point point, double threshold = 9d)
    {
        if (previewRect.IsEmpty || !previewRect.Contains(point))
        {
            return SliceGuideHit.None;
        }

        int verticalIndex = FindGuideIndex(previewRect.X, previewRect.Width, sliceData.VerticalBorders, point.X, threshold);
        int horizontalIndex = FindGuideIndex(previewRect.Y, previewRect.Height, sliceData.HorizontalBorders, point.Y, threshold);

        if (verticalIndex >= 0 && horizontalIndex >= 0)
        {
            return new SliceGuideHit(SliceGuideHitKind.Intersection, verticalIndex, horizontalIndex);
        }

        if (verticalIndex >= 0)
        {
            return new SliceGuideHit(SliceGuideHitKind.Vertical, verticalIndex, -1);
        }

        return horizontalIndex >= 0
            ? new SliceGuideHit(SliceGuideHitKind.Horizontal, -1, horizontalIndex)
            : SliceGuideHit.None;
    }

    public static double ToVerticalPercent(Rect previewRect, Point point)
    {
        return ToPercent(previewRect.X, previewRect.Width, point.X);
    }

    public static double ToHorizontalPercent(Rect previewRect, Point point)
    {
        return ToPercent(previewRect.Y, previewRect.Height, point.Y);
    }

    private static int FindGuideIndex(double start, double length, IReadOnlyList<double> borders, double position, double threshold)
    {
        if (length <= 0d)
        {
            return -1;
        }

        for (int index = 0; index < Math.Min(4, borders.Count); index++)
        {
            double guidePosition = start + (length * borders[index] / 100d);
            if (Math.Abs(position - guidePosition) <= threshold)
            {
                return index;
            }
        }

        return -1;
    }

    private static double ToPercent(double start, double length, double position)
    {
        if (length <= 0d)
        {
            return 0d;
        }

        return Math.Clamp(((position - start) / length) * 100d, 0d, 100d);
    }
}
