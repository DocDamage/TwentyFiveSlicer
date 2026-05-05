using System.Windows;
using System.Windows.Media;
using TwentyFiveSlicer.Desktop.Services;

namespace TwentyFiveSlicer.Desktop.Controls;

public sealed class SvgIcon : FrameworkElement
{
    public static readonly DependencyProperty SourceProperty = DependencyProperty.Register(
        nameof(Source),
        typeof(string),
        typeof(SvgIcon),
        new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty IconBrushProperty = DependencyProperty.Register(
        nameof(IconBrush),
        typeof(Brush),
        typeof(SvgIcon),
        new FrameworkPropertyMetadata(Brushes.White, FrameworkPropertyMetadataOptions.AffectsRender));

    public string Source
    {
        get => (string)GetValue(SourceProperty);
        set => SetValue(SourceProperty, value);
    }

    public Brush IconBrush
    {
        get => (Brush)GetValue(IconBrushProperty);
        set => SetValue(IconBrushProperty, value);
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        double width = double.IsNaN(Width) ? 18d : Width;
        double height = double.IsNaN(Height) ? 18d : Height;
        return new Size(width, height);
    }

    protected override void OnRender(DrawingContext drawingContext)
    {
        base.OnRender(drawingContext);

        if (string.IsNullOrWhiteSpace(Source) || ActualWidth <= 0d || ActualHeight <= 0d)
        {
            return;
        }

        DrawingGroup drawing = SvgIconRenderer.LoadResource(Source, IconBrush);
        Rect bounds = drawing.Bounds;
        if (bounds.Width <= 0d || bounds.Height <= 0d)
        {
            return;
        }

        double scale = Math.Min(ActualWidth / bounds.Width, ActualHeight / bounds.Height);
        double width = bounds.Width * scale;
        double height = bounds.Height * scale;
        double x = (ActualWidth - width) / 2d;
        double y = (ActualHeight - height) / 2d;

        drawingContext.PushTransform(new TranslateTransform(x, y));
        drawingContext.PushTransform(new ScaleTransform(scale, scale));
        drawingContext.DrawDrawing(drawing);
        drawingContext.Pop();
        drawingContext.Pop();
    }
}
