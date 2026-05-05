using System.Globalization;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using TwentyFiveSlicer.Desktop.Models;
using TwentyFiveSlicer.Desktop.Services;

namespace TwentyFiveSlicer.Desktop.Controls;

public sealed class TwentyFiveSlicePreviewControl : FrameworkElement
{
    private static readonly Brush SurfaceBrush = CreateBrush(12, 17, 22);
    private static readonly Brush SurfaceHighlightBrush = CreateBrush(20, 29, 35, 160);
    private static readonly Brush CheckerDarkBrush = CreateBrush(33, 42, 48);
    private static readonly Brush CheckerLightBrush = CreateBrush(49, 60, 67);
    private static readonly Brush EmptyStateTitleBrush = CreateBrush(241, 234, 219);
    private static readonly Brush EmptyStateBodyBrush = CreateBrush(145, 165, 166);
    private static readonly Brush GuideHitBrush = CreateBrush(60, 199, 186, 42);
    private static readonly Pen FramePen = CreatePen(70, 199, 190, 180, 1.5);
    private static readonly Pen SliceOutlinePen = CreatePen(255, 255, 255, 28, 0.75);
    private static readonly Pen GuidePen = CreatePen(60, 199, 186, 220, 1.25);
    private static readonly Pen ActiveGuidePen = CreatePen(255, 239, 190, 240, 2.25);
    private static readonly Pen ShadowPen = CreatePen(0, 0, 0, 60, 18d);

    private BitmapSource? _sourceImage;
    private TwentyFiveSliceData _sliceData = TwentyFiveSliceData.CreateDefault();
    private double _targetWidth = 640d;
    private double _targetHeight = 360d;
    private double _previewZoom = 1d;
    private bool _debuggingView;
    private bool _flipX;
    private bool _flipY;
    private bool _guideEditingEnabled = true;
    private bool _showSourceGuides;
    private Rect _lastPreviewRect;
    private int _activeVerticalGuideIndex = -1;
    private int _activeHorizontalGuideIndex = -1;

    public event EventHandler<SliceGuideEditEventArgs>? GuideEditChanged;
    public event EventHandler<double>? PreviewZoomChanged;

    public TwentyFiveSlicePreviewControl()
    {
        SnapsToDevicePixels = true;
        UseLayoutRounding = true;
    }

    public BitmapSource? SourceImage
    {
        get => _sourceImage;
        set
        {
            _sourceImage = value;
            InvalidateVisual();
        }
    }

    public TwentyFiveSliceData SliceData
    {
        get => _sliceData;
        set
        {
            _sliceData = value ?? TwentyFiveSliceData.CreateDefault();
            InvalidateVisual();
        }
    }

    public double TargetWidth
    {
        get => _targetWidth;
        set
        {
            _targetWidth = Math.Max(1d, value);
            InvalidateVisual();
        }
    }

    public double TargetHeight
    {
        get => _targetHeight;
        set
        {
            _targetHeight = Math.Max(1d, value);
            InvalidateVisual();
        }
    }

    public bool DebuggingView
    {
        get => _debuggingView;
        set
        {
            _debuggingView = value;
            InvalidateVisual();
        }
    }

    public bool FlipX
    {
        get => _flipX;
        set
        {
            _flipX = value;
            InvalidateVisual();
        }
    }

    public bool FlipY
    {
        get => _flipY;
        set
        {
            _flipY = value;
            InvalidateVisual();
        }
    }

    public double PreviewZoom
    {
        get => _previewZoom;
        set
        {
            double zoom = Math.Round(Math.Clamp(value, 0.5d, 2d), 4);
            if (Math.Abs(_previewZoom - zoom) < 0.0001d)
            {
                return;
            }

            _previewZoom = zoom;
            InvalidateVisual();
            PreviewZoomChanged?.Invoke(this, _previewZoom);
        }
    }

    public void AdjustZoomFromMouseWheel(int wheelDelta)
    {
        PreviewZoom += (wheelDelta / 120d) * 0.05d;
    }

    public void ResetPreviewZoom()
    {
        PreviewZoom = 1d;
    }

    public bool GuideEditingEnabled
    {
        get => _guideEditingEnabled;
        set
        {
            _guideEditingEnabled = value;
            InvalidateVisual();
        }
    }

    public bool ShowSourceGuides
    {
        get => _showSourceGuides;
        set
        {
            _showSourceGuides = value;
            InvalidateVisual();
        }
    }

    public BitmapSource RenderOutputBitmap(bool includeDebugOverlay = false)
    {
        int pixelWidth = Math.Max(1, (int)Math.Round(TargetWidth));
        int pixelHeight = Math.Max(1, (int)Math.Round(TargetHeight));
        var target = new RenderTargetBitmap(pixelWidth, pixelHeight, 96, 96, PixelFormats.Pbgra32);

        if (_sourceImage is null)
        {
            return target;
        }

        var visual = new DrawingVisual();
        using (DrawingContext drawingContext = visual.RenderOpen())
        {
            DrawSlicedImage(drawingContext, new Rect(0d, 0d, pixelWidth, pixelHeight), 1d, includeDebugOverlay, includeOutlines: includeDebugOverlay);
        }

        target.Render(visual);
        target.Freeze();
        return target;
    }

    protected override void OnRender(DrawingContext drawingContext)
    {
        base.OnRender(drawingContext);

        var fullRect = new Rect(0d, 0d, ActualWidth, ActualHeight);
        drawingContext.DrawRectangle(SurfaceBrush, null, fullRect);
        drawingContext.DrawRectangle(SurfaceHighlightBrush, null, new Rect(14d, 14d, Math.Max(0d, ActualWidth - 28d), 92d));

        if (_sourceImage is null)
        {
            DrawEmptyState(drawingContext, fullRect);
            return;
        }

        double safeTargetWidth = Math.Max(1d, TargetWidth);
        double safeTargetHeight = Math.Max(1d, TargetHeight);
        var availableRect = new Rect(28d, 28d, Math.Max(0d, ActualWidth - 56d), Math.Max(0d, ActualHeight - 56d));
        if (availableRect.Width <= 0d || availableRect.Height <= 0d)
        {
            return;
        }

        double scale = Math.Min(availableRect.Width / safeTargetWidth, availableRect.Height / safeTargetHeight);
        if (double.IsNaN(scale) || double.IsInfinity(scale) || scale <= 0d)
        {
            scale = 1d;
        }

        scale *= PreviewZoom;

        var previewRect = new Rect(
            availableRect.X + (availableRect.Width - (safeTargetWidth * scale)) / 2d,
            availableRect.Y + (availableRect.Height - (safeTargetHeight * scale)) / 2d,
            safeTargetWidth * scale,
            safeTargetHeight * scale);
        _lastPreviewRect = previewRect;

        DrawShadow(drawingContext, previewRect);
        DrawCheckerboard(drawingContext, previewRect, Math.Clamp(18d * scale, 8d, 34d));

        if (ShowSourceGuides)
        {
            drawingContext.DrawRectangle(new ImageBrush(_sourceImage) { Stretch = Stretch.Fill }, null, previewRect);
        }
        else
        {
            DrawSlicedImage(drawingContext, previewRect, scale, DebuggingView, includeOutlines: true);
        }

        DrawEditableGuides(drawingContext, previewRect);

        drawingContext.DrawRectangle(null, FramePen, previewRect);
        DrawPreviewLabel(drawingContext, previewRect, scale);
    }

    protected override void OnMouseDown(System.Windows.Input.MouseButtonEventArgs e)
    {
        base.OnMouseDown(e);
        if (!GuideEditingEnabled || _sourceImage is null || !_lastPreviewRect.Contains(e.GetPosition(this)))
        {
            return;
        }

        Point point = e.GetPosition(this);
        SliceGuideHit hit = HitTestGuide(point);
        if (hit.Kind == SliceGuideHitKind.None)
        {
            return;
        }

        _activeVerticalGuideIndex = hit.VerticalIndex;
        _activeHorizontalGuideIndex = hit.HorizontalIndex;
        CaptureMouse();
        Cursor = GetCursor(hit);
        UpdateActiveGuides(point, isFinal: false);
        e.Handled = true;
    }

    protected override void OnMouseMove(System.Windows.Input.MouseEventArgs e)
    {
        base.OnMouseMove(e);
        Point point = e.GetPosition(this);

        if (HasActiveGuide() && IsMouseCaptured)
        {
            UpdateActiveGuides(point, isFinal: false);
            e.Handled = true;
            return;
        }

        Cursor = GetCursor(HitTestGuide(point));
    }

    protected override void OnMouseUp(System.Windows.Input.MouseButtonEventArgs e)
    {
        base.OnMouseUp(e);
        if (!HasActiveGuide())
        {
            return;
        }

        UpdateActiveGuides(e.GetPosition(this), isFinal: true);
        _activeVerticalGuideIndex = -1;
        _activeHorizontalGuideIndex = -1;
        ReleaseMouseCapture();
        Cursor = null;
        e.Handled = true;
    }

    protected override void OnMouseWheel(System.Windows.Input.MouseWheelEventArgs e)
    {
        base.OnMouseWheel(e);
        AdjustZoomFromMouseWheel(e.Delta);
        e.Handled = true;
    }

    private void DrawEditableGuides(DrawingContext drawingContext, Rect previewRect)
    {
        if (!GuideEditingEnabled || _sourceImage is null)
        {
            return;
        }

        for (int index = 0; index < 4; index++)
        {
            double x = previewRect.X + (previewRect.Width * SliceData.VerticalBorders[index] / 100d);
            Pen pen = _activeVerticalGuideIndex == index ? ActiveGuidePen : GuidePen;
            drawingContext.DrawRectangle(GuideHitBrush, null, new Rect(x - 3d, previewRect.Y, 6d, previewRect.Height));
            drawingContext.DrawLine(pen, new Point(x, previewRect.Y), new Point(x, previewRect.Bottom));

            double y = previewRect.Y + (previewRect.Height * SliceData.HorizontalBorders[index] / 100d);
            pen = _activeHorizontalGuideIndex == index ? ActiveGuidePen : GuidePen;
            drawingContext.DrawRectangle(GuideHitBrush, null, new Rect(previewRect.X, y - 3d, previewRect.Width, 6d));
            drawingContext.DrawLine(pen, new Point(previewRect.X, y), new Point(previewRect.Right, y));
        }
    }

    private SliceGuideHit HitTestGuide(Point point)
    {
        return GuideEditingEnabled
            ? SliceGuideInteraction.HitTest(_lastPreviewRect, SliceData, point)
            : SliceGuideHit.None;
    }

    private void UpdateActiveGuides(Point point, bool isFinal)
    {
        if (!HasActiveGuide() || _lastPreviewRect.IsEmpty)
        {
            return;
        }

        if (_activeVerticalGuideIndex >= 0)
        {
            GuideEditChanged?.Invoke(this, new SliceGuideEditEventArgs(
                true,
                _activeVerticalGuideIndex,
                SliceGuideInteraction.ToVerticalPercent(_lastPreviewRect, point),
                isFinal));
        }

        if (_activeHorizontalGuideIndex >= 0)
        {
            GuideEditChanged?.Invoke(this, new SliceGuideEditEventArgs(
                false,
                _activeHorizontalGuideIndex,
                SliceGuideInteraction.ToHorizontalPercent(_lastPreviewRect, point),
                isFinal));
        }
    }

    private bool HasActiveGuide()
    {
        return _activeVerticalGuideIndex >= 0 || _activeHorizontalGuideIndex >= 0;
    }

    private static System.Windows.Input.Cursor? GetCursor(SliceGuideHit hit)
    {
        return hit.Kind switch
        {
            SliceGuideHitKind.Intersection => System.Windows.Input.Cursors.SizeAll,
            SliceGuideHitKind.Vertical => System.Windows.Input.Cursors.SizeWE,
            SliceGuideHitKind.Horizontal => System.Windows.Input.Cursors.SizeNS,
            _ => null
        };
    }

    private void DrawSlicedImage(DrawingContext drawingContext, Rect outputRect, double scale, bool includeDebugOverlay, bool includeOutlines)
    {
        if (_sourceImage is null)
        {
            return;
        }

        IReadOnlyList<SliceRegion> regions = TwentyFiveSliceLayoutCalculator.CalculateRegions(
            _sourceImage.PixelWidth,
            _sourceImage.PixelHeight,
            outputRect.Width / scale,
            outputRect.Height / scale,
            SliceData,
            FlipX,
            FlipY);

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

            drawingContext.DrawRectangle(CreateSourceBrush(region.Source), null, destinationRect);

            if (includeDebugOverlay)
            {
                drawingContext.DrawRectangle(CreateDebugOverlay(region.Column, region.Row), null, destinationRect);
            }

            if (includeOutlines)
            {
                drawingContext.DrawRectangle(null, SliceOutlinePen, destinationRect);
            }
        }
    }

    private void DrawEmptyState(DrawingContext drawingContext, Rect bounds)
    {
        double pixelsPerDip = VisualTreeHelper.GetDpi(this).PixelsPerDip;

        var title = new FormattedText(
            "Load an image to preview the 25-slice layout.",
            CultureInfo.InvariantCulture,
            FlowDirection.LeftToRight,
            new Typeface("Bahnschrift SemiBold"),
            26d,
            EmptyStateTitleBrush,
            pixelsPerDip);

        var body = new FormattedText(
            "The desktop port preserves the original fixed/stretch column logic and renders all 25 regions live.",
            CultureInfo.InvariantCulture,
            FlowDirection.LeftToRight,
            new Typeface("Bahnschrift"),
            15d,
            EmptyStateBodyBrush,
            pixelsPerDip)
        {
            MaxTextWidth = Math.Max(240d, bounds.Width - 120d),
            TextAlignment = TextAlignment.Center
        };

        Point titlePosition = new((bounds.Width - title.Width) / 2d, (bounds.Height / 2d) - 36d);
        Point bodyPosition = new((bounds.Width - body.Width) / 2d, titlePosition.Y + 46d);

        drawingContext.DrawText(title, titlePosition);
        drawingContext.DrawText(body, bodyPosition);
    }

    private void DrawShadow(DrawingContext drawingContext, Rect previewRect)
    {
        drawingContext.DrawRoundedRectangle(null, ShadowPen, new Rect(previewRect.X + 10d, previewRect.Y + 12d, previewRect.Width, previewRect.Height), 18d, 18d);
    }

    private void DrawCheckerboard(DrawingContext drawingContext, Rect previewRect, double cellSize)
    {
        int rowCount = (int)Math.Ceiling(previewRect.Height / cellSize);
        int columnCount = (int)Math.Ceiling(previewRect.Width / cellSize);

        for (int row = 0; row < rowCount; row++)
        {
            for (int column = 0; column < columnCount; column++)
            {
                double x = previewRect.X + (column * cellSize);
                double y = previewRect.Y + (row * cellSize);
                double width = Math.Min(cellSize, previewRect.Right - x);
                double height = Math.Min(cellSize, previewRect.Bottom - y);
                if (width <= 0d || height <= 0d)
                {
                    continue;
                }

                Brush fill = ((row + column) % 2 == 0) ? CheckerDarkBrush : CheckerLightBrush;
                drawingContext.DrawRectangle(fill, null, new Rect(x, y, width, height));
            }
        }
    }

    private void DrawPreviewLabel(DrawingContext drawingContext, Rect previewRect, double scale)
    {
        double pixelsPerDip = VisualTreeHelper.GetDpi(this).PixelsPerDip;
        string label = $"{TargetWidth:0} x {TargetHeight:0} preview at {scale * 100d:0}%";
        var text = new FormattedText(
            label,
            CultureInfo.InvariantCulture,
            FlowDirection.LeftToRight,
            new Typeface("Bahnschrift SemiBold"),
            13d,
            EmptyStateTitleBrush,
            pixelsPerDip);

        var labelRect = new Rect(previewRect.X, previewRect.Y - 34d, Math.Max(180d, text.Width + 18d), 26d);
        drawingContext.DrawRoundedRectangle(SurfaceHighlightBrush, null, labelRect, 12d, 12d);
        drawingContext.DrawText(text, new Point(labelRect.X + 9d, labelRect.Y + 5d));
    }

    private ImageBrush CreateSourceBrush(FloatRect sourceRect)
    {
        return new ImageBrush(_sourceImage)
        {
            AlignmentX = AlignmentX.Left,
            AlignmentY = AlignmentY.Top,
            Stretch = Stretch.Fill,
            Viewbox = new Rect(sourceRect.X, sourceRect.Y, sourceRect.Width, sourceRect.Height),
            ViewboxUnits = BrushMappingMode.Absolute
        };
    }

    private static Brush CreateDebugOverlay(int column, int row)
    {
        byte red = (byte)(45 + (column * 42));
        byte green = (byte)(55 + (row * 38));
        byte blue = (byte)(90 + ((column + row) * 18));
        return CreateBrush(red, green, blue, 110);
    }

    private static SolidColorBrush CreateBrush(byte red, byte green, byte blue, byte alpha = 255)
    {
        var brush = new SolidColorBrush(Color.FromArgb(alpha, red, green, blue));
        brush.Freeze();
        return brush;
    }

    private static Pen CreatePen(byte red, byte green, byte blue, byte alpha, double thickness)
    {
        var pen = new Pen(CreateBrush(red, green, blue, alpha), thickness)
        {
            LineJoin = PenLineJoin.Round
        };
        pen.Freeze();
        return pen;
    }
}
