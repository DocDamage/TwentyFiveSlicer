using System.Windows;

namespace TwentyFiveSlicer.Desktop.Services;

public readonly record struct PreviewViewport(Rect PreviewRect, double PanX, double PanY);

public static class PreviewViewportCalculator
{
    public static PreviewViewport Calculate(
        Rect availableRect,
        double targetWidth,
        double targetHeight,
        double previewZoom,
        double requestedPanX,
        double requestedPanY)
    {
        double safeTargetWidth = Math.Max(1d, targetWidth);
        double safeTargetHeight = Math.Max(1d, targetHeight);
        double scale = Math.Min(availableRect.Width / safeTargetWidth, availableRect.Height / safeTargetHeight);
        if (double.IsNaN(scale) || double.IsInfinity(scale) || scale <= 0d)
        {
            scale = 1d;
        }

        scale *= Math.Clamp(previewZoom, 0.5d, 2d);
        double width = safeTargetWidth * scale;
        double height = safeTargetHeight * scale;
        double panX = ClampPan(requestedPanX, width, availableRect.Width);
        double panY = ClampPan(requestedPanY, height, availableRect.Height);

        var previewRect = new Rect(
            availableRect.X + ((availableRect.Width - width) / 2d) + panX,
            availableRect.Y + ((availableRect.Height - height) / 2d) + panY,
            width,
            height);

        return new PreviewViewport(previewRect, panX, panY);
    }

    private static double ClampPan(double requestedPan, double contentSize, double viewportSize)
    {
        double overflow = Math.Max(0d, contentSize - viewportSize);
        if (overflow <= 0d)
        {
            return 0d;
        }

        double maxPan = overflow / 2d;
        return Math.Clamp(requestedPan, -maxPan, maxPan);
    }
}
