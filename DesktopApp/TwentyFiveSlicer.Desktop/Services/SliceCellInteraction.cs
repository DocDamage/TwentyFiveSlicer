using System.Windows;
using TwentyFiveSlicer.Desktop.Models;

namespace TwentyFiveSlicer.Desktop.Services;

public static class SliceCellInteraction
{
    public static SliceCellHit HitTest(Rect outputRect, double scale, IReadOnlyList<SliceRegion> regions, Point point)
    {
        if (scale <= 0d || outputRect.IsEmpty || !outputRect.Contains(point))
        {
            return SliceCellHit.None;
        }

        foreach (SliceRegion region in regions)
        {
            var destinationRect = new Rect(
                outputRect.X + (region.Destination.X * scale),
                outputRect.Y + (region.Destination.Y * scale),
                region.Destination.Width * scale,
                region.Destination.Height * scale);
            if (destinationRect.Width <= 0d || destinationRect.Height <= 0d)
            {
                continue;
            }

            if (destinationRect.Contains(point))
            {
                return new SliceCellHit(region.Column, region.Row);
            }
        }

        return SliceCellHit.None;
    }
}