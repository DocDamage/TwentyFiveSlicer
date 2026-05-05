using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using TwentyFiveSlicer.Desktop.Models;
using TwentyFiveSlicer.Desktop.Services;

namespace TwentyFiveSlicer.Desktop.Controls;

public sealed class SliceSkinPanel : ContentControl
{
    public static readonly DependencyProperty CandySkinEnabledProperty = DependencyProperty.RegisterAttached(
        "CandySkinEnabled",
        typeof(bool),
        typeof(SliceSkinPanel),
        new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.Inherits | FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty SourceProperty = DependencyProperty.Register(
        nameof(Source),
        typeof(string),
        typeof(SliceSkinPanel),
        new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty SliceDataSourceProperty = DependencyProperty.Register(
        nameof(SliceDataSource),
        typeof(string),
        typeof(SliceSkinPanel),
        new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty UseSkinProperty = DependencyProperty.Register(
        nameof(UseSkin),
        typeof(bool),
        typeof(SliceSkinPanel),
        new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty FallbackBackgroundProperty = DependencyProperty.Register(
        nameof(FallbackBackground),
        typeof(Brush),
        typeof(SliceSkinPanel),
        new FrameworkPropertyMetadata(Brushes.Transparent, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty FallbackBorderBrushProperty = DependencyProperty.Register(
        nameof(FallbackBorderBrush),
        typeof(Brush),
        typeof(SliceSkinPanel),
        new FrameworkPropertyMetadata(Brushes.Transparent, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty FallbackBorderThicknessProperty = DependencyProperty.Register(
        nameof(FallbackBorderThickness),
        typeof(Thickness),
        typeof(SliceSkinPanel),
        new FrameworkPropertyMetadata(new Thickness(0d), FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty FallbackCornerRadiusProperty = DependencyProperty.Register(
        nameof(FallbackCornerRadius),
        typeof(CornerRadius),
        typeof(SliceSkinPanel),
        new FrameworkPropertyMetadata(new CornerRadius(0d), FrameworkPropertyMetadataOptions.AffectsRender));

    public static void SetCandySkinEnabled(DependencyObject element, bool value)
    {
        element.SetValue(CandySkinEnabledProperty, value);
    }

    public static bool GetCandySkinEnabled(DependencyObject element)
    {
        return (bool)element.GetValue(CandySkinEnabledProperty);
    }

    public string Source
    {
        get => (string)GetValue(SourceProperty);
        set => SetValue(SourceProperty, value);
    }

    public string SliceDataSource
    {
        get => (string)GetValue(SliceDataSourceProperty);
        set => SetValue(SliceDataSourceProperty, value);
    }

    public bool UseSkin
    {
        get => (bool)GetValue(UseSkinProperty);
        set => SetValue(UseSkinProperty, value);
    }

    public Brush FallbackBackground
    {
        get => (Brush)GetValue(FallbackBackgroundProperty);
        set => SetValue(FallbackBackgroundProperty, value);
    }

    public Brush FallbackBorderBrush
    {
        get => (Brush)GetValue(FallbackBorderBrushProperty);
        set => SetValue(FallbackBorderBrushProperty, value);
    }

    public Thickness FallbackBorderThickness
    {
        get => (Thickness)GetValue(FallbackBorderThicknessProperty);
        set => SetValue(FallbackBorderThicknessProperty, value);
    }

    public CornerRadius FallbackCornerRadius
    {
        get => (CornerRadius)GetValue(FallbackCornerRadiusProperty);
        set => SetValue(FallbackCornerRadiusProperty, value);
    }

    protected override void OnRender(DrawingContext drawingContext)
    {
        base.OnRender(drawingContext);

        if (!UseSkin)
        {
            DrawFallback(drawingContext);
            return;
        }

        BitmapSource? image = LoadBitmap(Source);
        TwentyFiveSliceData? sliceData = LoadSliceData(SliceDataSource);
        if (image is null || sliceData is null || ActualWidth <= 0d || ActualHeight <= 0d)
        {
            DrawFallback(drawingContext);
            return;
        }

        IReadOnlyList<SliceRegion> regions = TwentyFiveSliceLayoutCalculator.CalculateRegions(
            image.PixelWidth,
            image.PixelHeight,
            ActualWidth,
            ActualHeight,
            sliceData);

        foreach (SliceRegion region in regions)
        {
            int sourceX = Math.Clamp((int)Math.Round(region.Source.X), 0, image.PixelWidth);
            int sourceY = Math.Clamp((int)Math.Round(region.Source.Y), 0, image.PixelHeight);
            int sourceRight = Math.Clamp((int)Math.Round(region.Source.X + region.Source.Width), 0, image.PixelWidth);
            int sourceBottom = Math.Clamp((int)Math.Round(region.Source.Y + region.Source.Height), 0, image.PixelHeight);
            int sourceWidth = sourceRight - sourceX;
            int sourceHeight = sourceBottom - sourceY;
            if (sourceWidth <= 0 || sourceHeight <= 0)
            {
                continue;
            }

            var sourceRect = new Int32Rect(sourceX, sourceY, sourceWidth, sourceHeight);
            var cropped = new CroppedBitmap(image, sourceRect);
            drawingContext.DrawImage(cropped, new Rect(
                region.Destination.X,
                region.Destination.Y,
                region.Destination.Width,
                region.Destination.Height));
        }
    }

    private void DrawFallback(DrawingContext drawingContext)
    {
        if (ActualWidth <= 0d || ActualHeight <= 0d)
        {
            return;
        }

        double borderWidth = Math.Max(0d, Math.Max(
            Math.Max(FallbackBorderThickness.Left, FallbackBorderThickness.Top),
            Math.Max(FallbackBorderThickness.Right, FallbackBorderThickness.Bottom)));
        Pen? pen = borderWidth > 0d ? new Pen(FallbackBorderBrush, borderWidth) : null;
        CornerRadius radius = FallbackCornerRadius;
        double radiusX = Math.Max(0d, Math.Max(radius.TopLeft, radius.BottomLeft));
        double radiusY = Math.Max(0d, Math.Max(radius.TopRight, radius.BottomRight));
        drawingContext.DrawRoundedRectangle(
            FallbackBackground,
            pen,
            new Rect(borderWidth / 2d, borderWidth / 2d, Math.Max(0d, ActualWidth - borderWidth), Math.Max(0d, ActualHeight - borderWidth)),
            radiusX,
            radiusY);
    }

    private static BitmapSource? LoadBitmap(string resourcePath)
    {
        if (string.IsNullOrWhiteSpace(resourcePath))
        {
            return null;
        }

        string assemblyName = Assembly.GetExecutingAssembly().GetName().Name ?? "TwentyFiveSlicer.Desktop";
        var resourceUri = new Uri($"/{assemblyName};component/{resourcePath}", UriKind.Relative);
        System.Windows.Resources.StreamResourceInfo? resource = Application.GetResourceStream(resourceUri);

        if (resource is not null)
        {
            using Stream stream = resource.Stream;
            return LoadBitmap(stream);
        }

        string filePath = Path.Combine(AppContext.BaseDirectory, resourcePath.Replace('/', Path.DirectorySeparatorChar));
        if (!File.Exists(filePath))
        {
            return null;
        }

        using FileStream fileStream = File.OpenRead(filePath);
        return LoadBitmap(fileStream);
    }

    private static BitmapSource LoadBitmap(Stream stream)
    {
        var image = new BitmapImage();
        image.BeginInit();
        image.CacheOption = BitmapCacheOption.OnLoad;
        image.StreamSource = stream;
        image.EndInit();
        image.Freeze();
        return image;
    }

    private static TwentyFiveSliceData? LoadSliceData(string resourcePath)
    {
        if (string.IsNullOrWhiteSpace(resourcePath))
        {
            return null;
        }

        string assemblyName = Assembly.GetExecutingAssembly().GetName().Name ?? "TwentyFiveSlicer.Desktop";
        var resourceUri = new Uri($"/{assemblyName};component/{resourcePath}", UriKind.Relative);
        System.Windows.Resources.StreamResourceInfo? resource = Application.GetResourceStream(resourceUri);

        if (resource is not null)
        {
            using Stream stream = resource.Stream;
            return JsonSerializer.Deserialize<TwentyFiveSliceData>(stream);
        }

        string filePath = Path.Combine(AppContext.BaseDirectory, resourcePath.Replace('/', Path.DirectorySeparatorChar));
        return File.Exists(filePath)
            ? JsonSerializer.Deserialize<TwentyFiveSliceData>(File.ReadAllText(filePath))
            : null;
    }
}
